using Android.Content;

using Microsoft.Maui.ApplicationModel;

namespace VectronTablet.Services;

/// <summary>
/// Android setup that keeps screen-off notifications alive: the battery-optimisation
/// exemption (without it Android blocks the network in Doze while the screen is off,
/// so alerts never arrive). Re-asks at most once a day until it is actually granted.
/// </summary>
public static class AlertSetup
{
    private static readonly TimeSpan AskInterval = TimeSpan.FromDays(1);

    /// <summary>True when the app is exempt from battery optimisation (Doze network cut-off).</summary>
    public static bool IsBatteryExempt
    {
        get
        {
            try
            {
                var ctx = Android.App.Application.Context;
                var pkg = ctx.PackageName ?? "";
                if (pkg.Length == 0) return false;
                var pm = ctx.GetSystemService(Context.PowerService) as Android.OS.PowerManager;
                return pm != null && pm.IsIgnoringBatteryOptimizations(pkg);
            }
            catch { return false; }
        }
    }

    /// <summary>
    /// Asks for the battery exemption unless already granted. Throttled to once a day —
    /// the old code marked it "asked" even when the user tapped Later, so it never
    /// re-asked and screen-off alerts silently died.
    /// </summary>
    public static async Task EnsureBatteryExemptionAsync(Page page)
    {
        try
        {
            if (IsBatteryExempt) return;

            var lastAsk = Preferences.Default.Get("battery_last_ask", 0L);
            if (lastAsk > 0 && DateTime.UtcNow.Ticks - lastAsk < AskInterval.Ticks) return;

            var open = await page.DisplayAlert(
                "Keep alerts alive",
                "To hear guest calls and ready orders while the screen is off, Android must not put Vectron Waiter to sleep.\n\nAllow it to ignore battery optimisation?",
                "Open settings", "Later");
            Preferences.Default.Set("battery_last_ask", DateTime.UtcNow.Ticks);
            if (!open) return;

            var ctx = Android.App.Application.Context;
            var pkg = ctx.PackageName ?? "";
            if (pkg.Length == 0) return;
            var intent = new Intent("android.settings.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS",
                Android.Net.Uri.Parse($"package:{pkg}"));
            var activity = Platform.CurrentActivity;
            if (activity != null) activity.StartActivity(intent);
            else
            {
                intent.AddFlags(ActivityFlags.NewTask);
                ctx.StartActivity(intent);
            }
        }
        catch (Exception ex) { Android.Util.Log.Error("VectronPOS", "battery opt: " + ex.Message); }
    }
}
