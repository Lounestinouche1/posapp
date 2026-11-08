using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace VectronTablet.Services;

/// <summary>
/// Foreground service that polls /api/tablet/alerts every 3 seconds and raises system
/// notifications — keeps working with the app backgrounded and the screen off.
/// </summary>
// specialUse (Android 14+) has no 6-hour timeout like dataSync, and may be started
// from BOOT_COMPLETED on Android 15+ — both required for a 24/7 alert poller.
[Service(Name = "com.vectron.waiter.AlertPollingService", Exported = false,
    ForegroundServiceType = Android.Content.PM.ForegroundService.TypeDataSync | Android.Content.PM.ForegroundService.TypeSpecialUse)]
public class AlertPollingService : Service
{
    private const int ServiceNotifId = 4471;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    private CancellationTokenSource? _cts;
    private bool _loopStarted;
    private PowerManager.WakeLock? _cpuWakeLock;

    public static bool IsRunning { get; private set; }

    public static void Start(Context ctx)
    {
        try
        {
            var intent = new Intent(ctx, typeof(AlertPollingService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O) ctx.StartForegroundService(intent);
            else ctx.StartService(intent);
        }
        catch (Exception ex) { Android.Util.Log.Error("VectronPOS", "start polling service: " + ex); }
    }

    public static void Stop(Context ctx)
    {
        try { ctx.StopService(new Intent(ctx, typeof(AlertPollingService))); }
        catch { }
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        IsRunning = true;
        try { ShowServiceNotification(); }
        catch (Exception ex) { Android.Util.Log.Error("VectronPOS", "foreground start: " + ex); }

        // Keep the CPU awake while the screen is off — otherwise the poll loop freezes
        // and alerts only arrive once the device is woken by something else.
        try
        {
            var pm = GetSystemService(Context.PowerService) as PowerManager;
            if (pm != null && (_cpuWakeLock == null || !_cpuWakeLock.IsHeld))
            {
                _cpuWakeLock = pm.NewWakeLock(WakeLockFlags.Partial, "VectronPOS:alertPoll");
                _cpuWakeLock.SetReferenceCounted(false);
                _cpuWakeLock.Acquire();
                Android.Util.Log.Info("VectronPOS", "partial wake lock acquired (screen-off polling)");
            }
        }
        catch (Exception ex) { Android.Util.Log.Error("VectronPOS", "wake lock: " + ex); }

        // Re-arm the reboot-proof watchdog while we are alive; if this service ever dies
        // (or the device reboots and the boot broadcast gets swallowed by the ROM),
        // AlertJobService starts it again.
        try { AlertJobService.Arm(this); } catch { }

        if (!_loopStarted)
        {
            _loopStarted = true;
            _cts ??= new CancellationTokenSource();
            var ct = _cts.Token;
            _ = Task.Run(() => PollLoopAsync(ct));
        }
        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        IsRunning = false;
        try { _cts?.Cancel(); } catch { }
        try
        {
            if (_cpuWakeLock != null && _cpuWakeLock.IsHeld) _cpuWakeLock.Release();
            _cpuWakeLock = null;
        }
        catch { }
        base.OnDestroy();
    }

    private void ShowServiceNotification()
    {
        NotificationHelper.EnsureChannels();
        var host = AppState.Api.BaseUrl.Replace("http://", "").Replace("https://", "").TrimEnd('/');
        var notif = new NotificationCompat.Builder(this, NotificationHelper.ChannelService)
            .SetSmallIcon(Resource.Drawable.ic_stat_notify)
            .SetContentTitle("Vectron Waiter · connected")
            .SetContentText(host)
            .SetPriority((int)NotificationPriority.Low)
            .SetOngoing(true)
            .SetShowWhen(false)
            .Build();
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            StartForeground(ServiceNotifId, notif, ForegroundType());
        else
            StartForeground(ServiceNotifId, notif);
    }

    /// <summary>
    /// specialUse on Android 14+ (no 6-hour kill like dataSync, startable from boot);
    /// dataSync on older versions (specialUse doesn't exist there).
    /// </summary>
    private static ForegroundService ForegroundType() =>
        Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake
            ? ForegroundService.TypeSpecialUse
            : ForegroundService.TypeDataSync;

    private async Task PollLoopAsync(CancellationToken ct)
    {
        string? since = null;
        var seen = new HashSet<string>();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var res = await AppState.Api.Alerts(since);
                if (res.Ok)
                {
                    foreach (var a in res.Alerts)
                    {
                        if (!seen.Add(a.Id)) continue;
                        var (title, body) = a.Kind switch
                        {
                            "waiter" => ("Waiter called", $"{a.Room} · {a.Table}"),
                            "ready" => ("Order ready", $"{a.Room} · {a.Table}"),
                            _ => ("Guest alert", $"{a.Room} · {a.Table} — {a.Kind}")
                        };
                        NotificationHelper.Notify(a.Id, NotificationHelper.IdFor(a.Id), title, body);
                    }
                    if (res.Alerts.Count > 0) since = res.Alerts[^1].Time;
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("VectronPOS", "alert poll failed: " + ex.Message);
            }

            try { await Task.Delay(PollInterval, ct); }
            catch (TaskCanceledException) { break; }
        }
    }
}
