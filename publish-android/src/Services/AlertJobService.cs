using Android.App;
using Android.App.Job;
using Android.Content;

namespace VectronTablet.Services;

/// <summary>
/// Reboot-proof watchdog for <see cref="AlertPollingService"/>.
/// A persisted JobScheduler job is written to disk and re-queued by the system after a
/// restart (alarms and broadcasts are not), which makes it a second, independent way to
/// bring polling back up: some ROMs/emulators (LDPlayer, Chinese "purify" managers)
/// silently skip third-party receivers in the BOOT_COMPLETED queue — our receiver is
/// listed as "Skipped" there and never runs — but they still start persisted jobs.
/// </summary>
[Service(Name = "com.vectron.waiter.AlertJobService", Exported = true)]
public class AlertJobService : JobService
{
    public const int JobId = 4472;

    /// <summary>How often the watchdog re-checks that polling is alive.</summary>
    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Schedules (or re-schedules) the watchdog. Safe to call as often as we like —
    /// the job id is fixed, so a new schedule simply replaces the previous one.
    /// The deadline is kept short so that after a reboot the persisted job is already
    /// overdue and fires within a minute of boot.
    /// </summary>
    public static void Arm(Context ctx, TimeSpan? delay = null)
    {
        try
        {
            var js = ctx.GetSystemService(Context.JobSchedulerService) as JobScheduler;
            if (js == null) return;
            var ms = (long)(delay ?? RetryDelay).TotalMilliseconds;
            var job = new JobInfo.Builder(JobId, new ComponentName(ctx, "com.vectron.waiter.AlertJobService"))
                .SetPersisted(true)      // survives reboot (needs RECEIVE_BOOT_COMPLETED)
                .SetMinimumLatency(ms)
                .SetOverrideDeadline(ms)
                .Build();
            var ok = js.Schedule(job) == JobScheduler.ResultSuccess;
            Android.Util.Log.Info("VectronPOS", "watchdog armed (ok=" + ok + ", in " + ms + "ms)");
        }
        catch (Exception ex) { Android.Util.Log.Error("VectronPOS", "arm watchdog: " + ex); }
    }

    public override bool OnStartJob(JobParameters? parameters)
    {
        try
        {
            var ctx = Android.App.Application.Context;
            if (!AppState.IsLoggedIn)
            {
                Android.Util.Log.Info("VectronPOS", "watchdog: no saved session");
                return false;
            }
            if (!AlertPollingService.IsRunning)
            {
                Android.Util.Log.Info("VectronPOS", "watchdog: polling service was down - restarting");
                AlertPollingService.Start(ctx);
            }
            Arm(ctx);   // keep the watchdog alive for next time
        }
        catch (Exception ex) { Android.Util.Log.Error("VectronPOS", "watchdog: " + ex); }
        return false;   // work done synchronously
    }

    public override bool OnStopJob(JobParameters? parameters) => true; // interrupted → run again later
}
