using Android.Content;
using VectronTablet.Services;

namespace VectronTablet;

/// <summary>
/// Restarts the alert-polling foreground service after a reboot or app update —
/// without this, notifications stay dead until the waiter opens the app again.
/// </summary>
[BroadcastReceiver(Name = "com.vectron.waiter.BootReceiver", Enabled = true, Exported = true)]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        var action = intent?.Action ?? "(null)";
        Android.Util.Log.Info("VectronPOS", "BootReceiver onReceive: " + action);
        if (context == null) return;
        if (action != Intent.ActionBootCompleted &&
            action != "android.intent.action.QUICKBOOT_POWERON" &&
            action != Intent.ActionMyPackageReplaced)
            return;
        try
        {
            // Nothing configured yet (no server/key saved) → nothing to poll.
            if (!AppState.IsLoggedIn)
            {
                Android.Util.Log.Info("VectronPOS", "boot: skipped (no saved session)");
                return;
            }
            AlertPollingService.Start(context);
            // Independent backup: persisted job, survives this broadcast being a no-op.
            AlertJobService.Arm(context, TimeSpan.Zero);
            Android.Util.Log.Info("VectronPOS", "boot: alert polling restarted (" + action + ")");
        }
        catch (Exception ex) { Android.Util.Log.Error("VectronPOS", "boot start: " + ex); }
    }
}
