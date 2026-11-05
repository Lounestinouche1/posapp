using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Provider;
using AndroidX.Core.App;

using AndroidApp = Android.App.Application;

namespace VectronTablet.Services;

/// <summary>Android system notifications: heads-up banner, sound, vibration, screen wake, tray entry.</summary>
public static class NotificationHelper
{
    public const string ChannelAlerts = "vectron_alerts";
    public const string ChannelService = "vectron_service";

    private static bool _channelsReady;
    private static PowerManager.WakeLock? _wakeLock;

    public static bool NotificationsEnabled
    {
        get
        {
            try { return NotificationManagerCompat.From(AndroidApp.Context).AreNotificationsEnabled(); }
            catch { return false; }
        }
    }

    public static void EnsureChannels()
    {
        if (_channelsReady) return;
        _channelsReady = true;
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var mgr = (NotificationManager?)AndroidApp.Context.GetSystemService(Context.NotificationService);
        if (mgr == null) return;

        var sound = RingtoneManager.GetDefaultUri(RingtoneType.Notification);
        var attrs = new AudioAttributes.Builder()
            .SetUsage(AudioUsageKind.Notification)
            .SetContentType(AudioContentType.Sonification)
            .Build();

        var alerts = new NotificationChannel(ChannelAlerts, "Guest alerts", NotificationImportance.High)
        {
            Description = "Waiter calls and ready orders",
        };
        alerts.SetSound(sound, attrs);
        alerts.EnableVibration(true);
        alerts.SetVibrationPattern([0, 300, 150, 300, 150, 300]);
        mgr.CreateNotificationChannel(alerts);

        var service = new NotificationChannel(ChannelService, "Alert polling", NotificationImportance.Low)
        {
            Description = "Keeps guest alerts arriving in the background",
        };
        service.SetShowBadge(false);
        service.SetSound(null, null);
        service.EnableVibration(false);
        mgr.CreateNotificationChannel(service);
    }

    /// <summary>Stable cross-process id so the same alert updates its notification instead of stacking.</summary>
    public static int IdFor(string tag)
    {
        unchecked
        {
            var h = unchecked((int)0x811C9DC5);
            foreach (var c in tag) { h ^= c; h *= 16777619; }
            return h == 0 ? 1 : h;
        }
    }

    /// <summary>Posts an alert: banner + sound + vibrate + wake screen; it stays in the notification tray.</summary>
    public static void Notify(string tag, int id, string title, string body)
    {
        EnsureChannels();
        if (!NotificationsEnabled) return;

        var ctx = AndroidApp.Context;
        var open = new Intent(ctx, typeof(MainActivity))
            .SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop)
            .PutExtra(MainActivity.OpenAlertsExtra, true);
        var flags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
        var req = Math.Abs(id);
        var content = PendingIntent.GetActivity(ctx, req, open, flags);
        var fullScreen = PendingIntent.GetActivity(ctx, req + 100000, open, flags);

        var builder = new NotificationCompat.Builder(ctx, ChannelAlerts)
            .SetSmallIcon(Resource.Drawable.ic_stat_notify)
            .SetContentTitle(title)
            .SetContentText(body)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(body))
            .SetAutoCancel(true)
            .SetPriority((int)NotificationPriority.Max)
            .SetVisibility(NotificationCompat.VisibilityPublic)
            .SetCategory(NotificationCompat.CategoryMessage)
            .SetContentIntent(content)
            .SetFullScreenIntent(fullScreen, true)
            .SetVibrate([0, 300, 150, 300, 150, 300]);

        // Android 14+: full-screen intent needs the special-access grant; fall back to banner + wake.
        if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake)
        {
            var nm = (NotificationManager?)ctx.GetSystemService(Context.NotificationService);
            if (nm != null && !nm.CanUseFullScreenIntent())
                builder.SetFullScreenIntent(null, false);
        }

        try { NotificationManagerCompat.From(ctx).Notify(tag, id, builder.Build()); }
        catch (Exception ex) { Android.Util.Log.Error("VectronPOS", "notify failed: " + ex); }

        WakeScreen();
    }

    /// <summary>Removes an alert from the notification tray (called when dismissed inside the app).</summary>
    public static void Cancel(string tag)
    {
        try { NotificationManagerCompat.From(AndroidApp.Context).Cancel(tag, IdFor(tag)); }
        catch { }
    }

    /// <summary>Turns the screen on briefly so the banner is seen, like a phone call.</summary>
    private static void WakeScreen()
    {
        try
        {
            var pm = (PowerManager?)AndroidApp.Context.GetSystemService(Context.PowerService);
            if (pm == null) return;
            _wakeLock ??= pm.NewWakeLock(
                WakeLockFlags.ScreenBright | WakeLockFlags.AcquireCausesWakeup, "vectron:alert");
            _wakeLock?.SetReferenceCounted(false);
            _wakeLock?.Acquire(10000);
        }
        catch { }
    }
}
