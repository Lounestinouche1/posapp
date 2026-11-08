using VectronTablet.Pages;
using VectronTablet.Services;

namespace VectronTablet;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		// P1.9: resolve Theme (Dark/Light) + Graphics (Auto/High/Lite) and write the palette
		// BEFORE any page is parsed, so every StaticResource lookup gets the active tokens.
		Theme.Apply();
		_ = AppState.FlushQueue();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		Page page = AppState.IsLoggedIn ? new AppShell() : new NavigationPage(new LoginPage());
		var window = new Window(page);
		// Auto sign-in skips LoginPage — still verify notification + battery setup
		// (re-asks once a day until the battery exemption is actually granted).
		if (AppState.IsLoggedIn) _ = CheckAlertSetupAsync(window);
		return window;
	}

	/// <summary>
	/// Coming back to the foreground: make sure the alert poller is actually running
	/// (it can be killed by the ROM while the app is closed) and re-arm the watchdog.
	/// </summary>
	protected override void OnResume()
	{
		base.OnResume();
		try
		{
			if (AppState.IsLoggedIn)
			{
				if (!AlertPollingService.IsRunning) AppState.StartAlertPolling();
				AlertJobService.Arm(Android.App.Application.Context);
			}
		}
		catch (Exception ex) { Android.Util.Log.Error("VectronPOS", "resume: " + ex.Message); }
	}

	private static async Task CheckAlertSetupAsync(Window window)
	{
		try
		{
			await Task.Delay(1500); // let the first page render before any dialog
			var page = window.Page;
			if (page is null) return;
			await MainThread.InvokeOnMainThreadAsync(() => AlertSetup.EnsureBatteryExemptionAsync(page));
		}
		catch (Exception ex) { Android.Util.Log.Error("VectronPOS", "alert setup: " + ex.Message); }
	}

	public static void ShowLogin()
	{
		if (Current is null || Current.Windows.Count == 0) return;
		Current.Windows[0].Page = new NavigationPage(new LoginPage());
	}

	public static void ShowMain()
	{
		if (Current is null || Current.Windows.Count == 0) return;
		Current.Windows[0].Page = new AppShell();
	}
}
