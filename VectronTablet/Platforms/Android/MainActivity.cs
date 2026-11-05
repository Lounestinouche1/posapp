using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;

using VectronTablet.Services;

namespace VectronTablet;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    public const string OpenAlertsExtra = "open_alerts";

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        ApplySystemBars();
        HandleAlertShortcut(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleAlertShortcut(intent);
    }

    /// <summary>
    /// P1.9 — the status / navigation bars follow the app palette instead of the MAUI template
    /// purple (they were #2B0B98 from the untouched template colours). Icons flip to dark glyphs
    /// on the light palette so they stay legible. System chrome stays native; we only colour it.
    /// Called on activity creation and again from <c>Theme.Apply()</c> after a theme switch
    /// (the activity is not recreated for a UiMode change, so OnCreate alone would go stale).
    /// </summary>
    internal static void ApplySystemBars()
    {
        try
        {
            var window = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;
            if (window == null) return;
            var bar = VectronTablet.Theme.C("Bg");
            var argb = Android.Graphics.Color.Argb(
                (int)(bar.Alpha * 255), (int)(bar.Red * 255), (int)(bar.Green * 255), (int)(bar.Blue * 255));
            window.SetStatusBarColor(argb);
            window.SetNavigationBarColor(argb);

            var decor = window.DecorView;
            if (decor == null || Build.VERSION.SdkInt < BuildVersionCodes.M) return;
            // View.SYSTEM_UI_FLAG_LIGHT_STATUS_BAR (API 23). The binding exposes the flag
            // through two different enums (SystemUiFlags / StatusBarVisibility) and the
            // property takes the latter, so the raw value is used here.
            const int LightStatusBar = 0x2000;
            var flags = (int)decor.SystemUiVisibility;
            flags = VectronTablet.Theme.IsLight ? flags | LightStatusBar : flags & ~LightStatusBar;
            decor.SystemUiVisibility = (StatusBarVisibility)flags;
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("VectronPOS", "system bars: " + ex.Message);
        }
    }

    /// <summary>Notification tap → land on the alerts list (now, or as soon as the map page shows).</summary>
    private static void HandleAlertShortcut(Intent? intent)
    {
        if (intent?.GetBooleanExtra(OpenAlertsExtra, false) != true) return;
        AppState.PendingAlertsNav = true;
        var go = AppState.AlertsNavRequested;
        if (go != null)
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(go);
    }
}
