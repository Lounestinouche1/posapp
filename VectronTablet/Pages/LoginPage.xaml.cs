using Android.Content;

using Microsoft.Maui.ApplicationModel;
using VectronTablet.Models;
using VectronTablet.Services;

namespace VectronTablet.Pages;

public partial class LoginPage : ContentPage
{
    private BootstrapInfo? _bootstrap;
    private OperatorInfo? _selected;
    private bool _connecting;
    private bool _signing;

    public LoginPage()
    {
        InitializeComponent();
        urlEntry.Text = AppState.Api.BaseUrl;
        keyEntry.Text = AppState.Api.ApiKey;

        // P1.4 flat pressed feedback — see OrderPage constructor note. P1.5: the clicks
        // travel through PressBind (a recognizer consumes the touch stream, so the XAML
        // Clicked would be dead — see Theme.PressBind). canClick gates the press for the
        // two buttons that paint a flat busy state while connecting/signing in.
        Theme.PressBind(connectBtn, () => OnConnect(connectBtn, EventArgs.Empty),
            canClick: () => !_connecting);
        Theme.PressBind(signInBtn, () => OnSignIn(signInBtn, EventArgs.Empty),
            canClick: () => !_signing);
        Theme.PressBind(backBtn, () => OnBack(backBtn, EventArgs.Empty));
    }

    private void ShowError(Label label, string message)
    {
        label.Text = message;
        label.IsVisible = true;
    }

    // P1.7 Flat-HUD: a 2px mint rail lights up under the focused field (event-driven,
    // mirrors the Windows opRail/pwRail) — the BoxView's own color, no animation.
    private void OnFieldFocused(object? sender, FocusEventArgs e) => SetRail(sender, true);

    private void OnFieldUnfocused(object? sender, FocusEventArgs e) => SetRail(sender, false);

    private static void SetRail(object? sender, bool on)
    {
        if (sender is not Entry entry || entry.Parent is not Grid grid || grid.Children.Count < 2) return;
        if (grid.Children[1] is BoxView rail)
            rail.BackgroundColor = on ? Theme.C("Mint") : Colors.Transparent;
    }

    private async void OnConnect(object sender, EventArgs e)
    {
        connError.IsVisible = false;
        var url = (urlEntry.Text ?? "").Trim();
        var key = (keyEntry.Text ?? "").Trim();
        if (url.Length == 0 || key.Length == 0)
        {
            ShowError(connError, "Enter the server address and API key.");
            return;
        }
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            url = "http://" + url;
        var hostPart = url[(url.IndexOf("://", StringComparison.Ordinal) + 3)..];
        if (!hostPart.Contains(':')) url += ":8080";

        if (_connecting) return;
        _connecting = true;
        // P1.4 flat busy state: never IsEnabled=false (Android's MaterialButton overrides
        // BackgroundColor with its own grey in that state) — guard re-entry with a flag.
        connectBtn.BackgroundColor = Theme.C("Panel2");
        connectBtn.TextColor = Theme.C("TxtDim");
        connectBtn.Text = "Connecting…";
        AppState.Api.BaseUrl = url;
        AppState.Api.ApiKey = key;

        var bs = await AppState.Api.Bootstrap();
        _connecting = false;
        connectBtn.BackgroundColor = Theme.C("Mint");
        connectBtn.TextColor = Theme.C("Bg");
        connectBtn.Text = "Connect";

        if (!bs.Ok)
        {
            var err = bs.Error ?? "Could not reach the main PC.";
            if (err.Contains("API key", StringComparison.OrdinalIgnoreCase))
                err = "Wrong API key — copy it from Settings → Network → API key & webhooks.";
            else if (err.Contains("disabled", StringComparison.OrdinalIgnoreCase))
                err = "REST API is off — turn on Settings → Network → REST API.";
            else
                err += $"\n\nCheck:\n• Vectron POS is running on the main PC\n• Address format: http://<PC-IP>:8080 (PC and tablet on the same Wi-Fi)\n• You typed: {url}";
            ShowError(connError, err);
            return;
        }

        AppState.Api.SaveConnection();
        AppState.Features = bs;
        _bootstrap = bs;
        BuildOperatorButtons();
        connPanel.IsVisible = false;
        loginPanel.IsVisible = true;
    }

    private void BuildOperatorButtons()
    {
        operatorRow.Children.Clear();
        foreach (var op in _bootstrap?.Operators ?? [])
        {
            var btn = new Button
            {
                Text = op.Name,
                Style = Theme.S("GhostBtn"),
                Margin = new Thickness(0, 0, 6, 0)
            };
            var captured = op;
            btn.Clicked += (_, _) => SelectOperator(captured, btn);
            operatorRow.Children.Add(btn);
        }
    }

    private void SelectOperator(OperatorInfo op, Button btn)
    {
        _selected = op;
        foreach (var child in operatorRow.Children)
            if (child is Button b) b.BackgroundColor = Theme.C("Panel2");
        btn.BackgroundColor = Theme.C("Mint");
        btn.TextColor = Theme.C("Bg");
        var needPin = op.HasPin;
        pinLbl.IsVisible = needPin;
        pinEntry.IsVisible = needPin;
        if (!needPin) pinEntry.Text = "";
    }

    private async void OnSignIn(object sender, EventArgs e)
    {
        loginError.IsVisible = false;
        if (_selected == null)
        {
            ShowError(loginError, "Pick your name first.");
            return;
        }
        if (_signing) return;
        _signing = true;
        // P1.4 flat busy state — see OnConnect.
        signInBtn.BackgroundColor = Theme.C("Panel2");
        signInBtn.TextColor = Theme.C("TxtDim");
        signInBtn.Text = "Signing in…";
        var res = await AppState.Api.Login(_selected.Name, pinEntry.Text ?? "");
        _signing = false;
        signInBtn.BackgroundColor = Theme.C("Mint");
        signInBtn.TextColor = Theme.C("Bg");
        signInBtn.Text = "Sign in";
        if (!res.Ok)
        {
            ShowError(loginError, res.Error ?? "Login failed.");
            return;
        }
        AppState.SetOperator(res.Operator, res.Permissions);
        await PrepareAlertsAsync();
        AppState.StartAlertPolling();
        App.ShowMain();
    }

    /// <summary>One-time Android setup: notification permission + battery-optimisation exemption.</summary>
    private async Task PrepareAlertsAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
                await Permissions.RequestAsync<Permissions.PostNotifications>();
        }
        catch (Exception ex) { Android.Util.Log.Error("VectronPOS", "POST_NOTIFICATIONS: " + ex.Message); }

        // Re-asks (once a day) until the exemption is actually granted — screen-off
        // alerts don't work while Android still optimises our battery.
        await AlertSetup.EnsureBatteryExemptionAsync(this);
    }

    private void OnBack(object sender, EventArgs e)
    {
        loginError.IsVisible = false;
        loginPanel.IsVisible = false;
        connPanel.IsVisible = true;
    }
}
