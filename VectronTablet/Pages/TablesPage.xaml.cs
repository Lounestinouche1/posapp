using VectronTablet.Models;
using VectronTablet.Services;

using Microsoft.Maui.Controls.Shapes;
namespace VectronTablet.Pages;

public partial class TablesPage : ContentPage
{
    private MapInfo? _map;
    private string _room = "";
    private bool _alive;
    private bool _busy;
    private bool _roomChipsBuilt;
    private bool _revealed;
    private string _host = "";

    public TablesPage()
    {
        InitializeComponent();
        whoLabel.Text = AppState.Operator;
        _host = AppState.Api.BaseUrl.Replace("http://", "").Replace("https://", "");
        SetServerStatus(_host);

        // P1.4 flat pressed feedback — see OrderPage constructor note. P1.5: the click
        // travels through PressBind (a recognizer consumes the touch stream, so the XAML
        // Clicked would be dead — see Theme.PressBind).
        Theme.PressBind(exitBtn, () => OnLogout(exitBtn, EventArgs.Empty));
        Theme.PressBind(appearanceBtn, () => _ = ShowAppearance());
    }

    // P1.7 Flat-HUD LED status (Windows sidebar footer parity): the leading dot alone
    // carries the state colour (mint healthy / amber warning), the rest stays dim and is
    // uppercased HUD micro-copy. FormattedString — no owner-draw, no extra views.
    private void SetServerStatus(string text, bool warn = false)
    {
        var fs = new FormattedString();
        fs.Spans.Add(new Span { Text = "● ", TextColor = Theme.C(warn ? "Amber" : "Mint") });
        fs.Spans.Add(new Span { Text = text.ToUpperInvariant(), TextColor = Theme.C("TxtDim") });
        serverLabel.FormattedText = fs;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _alive = true;
        // P1.9 High-tier entrance (one shared 150 ms fade+rise; a no-op on Lite, so the
        // flat baseline paints the final frame immediately). Once per page instance —
        // the 3 s poll rebuilds the grid and must never re-animate.
        if (!_revealed)
        {
            _revealed = true;
            Theme.Reveal(tableGrid);
        }
        AppState.AlertsNavRequested += OnAlertsNav;
        if (AppState.PendingAlertsNav) OnAlertsNav();
        if (AppState.IsLoggedIn && !AlertPollingService.IsRunning) AppState.StartAlertPolling();
        // P1.6 — the session auto-restore after an app restart skips LoginPage.OnConnect,
        // the only place that fetches the bootstrap, so Features (the currency suffix on
        // every total, etc.) stayed null → totals rendered bare ("7.00" instead of
        // "7.00 EUR") until the next full connect. One HTTP call, only when missing.
        if (AppState.Features == null) _ = EnsureFeatures();
        _ = FlushAndReload();
        Dispatcher.StartTimer(TimeSpan.FromSeconds(3), () =>
        {
            if (!_alive) return false;
            _ = Reload();
            return true;
        });
    }

    // P1.6 — bootstrap fetch for the session-restore path (see OnAppearing).
    private async Task EnsureFeatures()
    {
        var bs = await AppState.Api.Bootstrap();
        if (bs.Ok) AppState.Features = bs;
    }

    private async Task FlushAndReload()
    {
        if (OfflineQueue.Count > 0)
        {
            var (sent, error) = await AppState.FlushQueue();
            if (!_alive) return;
            if (error != null) SetServerStatus($"{_host} · ⚠ {OfflineQueue.Count} stuck", warn: true);
            else if (sent > 0) SetServerStatus($"{_host} · ✓ {sent} sent");
            else SetServerStatus(_host);
        }
        else SetServerStatus(_host);
        await Reload();
    }

    protected override void OnDisappearing()
    {
        _alive = false;
        AppState.AlertsNavRequested -= OnAlertsNav;
        base.OnDisappearing();
    }

    /// <summary>Notification tap → open the alerts list.</summary>
    private void OnAlertsNav()
    {
        if (!_alive || !AppState.PendingAlertsNav) return;
        AppState.PendingAlertsNav = false;
        if (Navigation.NavigationStack.LastOrDefault() is AlertsPage) return;
        _ = Navigation.PushAsync(new AlertsPage());
    }

    private async Task Reload()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            var map = await AppState.Api.Map();
            var alerts = await AppState.Api.Alerts();
            if (!_alive) return;
            if (map.Ok) _map = map;
            UpdateBell(alerts);
            BuildRoomChips();
            BuildTables();
        }
        finally { _busy = false; }
    }

    private void UpdateBell(AlertsResult alerts)
    {
        var count = alerts.Alerts.Count + alerts.Ready.Count;
        bellLabel.Text = count > 0 ? $"🔔 {count}" : "🔔";
        bellBorder.BackgroundColor = count > 0 ? Theme.C("Amber") : Theme.C("Kpi");
    }

    private void BuildRoomChips()
    {
        if (_map == null) return;
        if (!_roomChipsBuilt || roomRow.Children.Count != _map.Rooms.Count)
        {
            roomRow.Children.Clear();
            foreach (var room in _map.Rooms)
            {
                var name = room.Name;
                var chip = new Button { Text = name, Style = Theme.S("ChipBtn") };
                // P1.4 flat pressed feedback — reads the live base colour, so it coexists with
                // RefreshChips' active/inactive repaint. P1.5: the click travels through
                // PressBind (a recognizer consumes the touch stream, so a Clicked handler
                // would be dead — see Theme.PressBind).
                Theme.PressBind(chip, () => { _room = name; RefreshChips(); BuildTables(); });
                roomRow.Children.Add(chip);
            }
            _roomChipsBuilt = true;
            if (_map.Rooms.Count > 0 && _room.Length == 0) _room = _map.Rooms[0].Name;
        }
        RefreshChips();
    }

    private void RefreshChips()
    {
        foreach (var child in roomRow.Children)
        {
            if (child is not Button chip) continue;
            var active = chip.Text == _room;
            // P1.9 pill language (Windows nav pills): active = accent fill + ink on accent,
            // inactive = the quiet Kpi fill + dim text.
            chip.BackgroundColor = active ? Theme.C("Mint") : Theme.C("Kpi");
            chip.TextColor = active ? Theme.C("Ink") : Theme.C("TxtDim");
        }
    }

    private void BuildTables()
    {
        tableGrid.Children.Clear();
        tableGrid.RowDefinitions.Clear();
        var room = _map?.Rooms.FirstOrDefault(r => r.Name == _room);
        if (room == null) return;
        const int cols = 4;
        var rows = (room.Tables.Count + cols - 1) / cols;
        for (var i = 0; i < rows; i++) tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (var i = 0; i < room.Tables.Count; i++)
        {
            var t = room.Tables[i];
            var border = MakeTable(t);
            Grid.SetRow(border, i / cols);
            Grid.SetColumn(border, i % cols);
            tableGrid.Children.Add(border);
        }
    }

    private Border MakeTable(TableInfo t)
    {
        Color bg, accent;
        string line2;
        if (t.ReadyOrder())
        {
            bg = Theme.C("ReadyBg"); accent = Theme.C("Green");
            line2 = $"✓ ready #{t.Order!.Number}";
        }
        else if (t.Taken)
        {
            bg = Theme.C("TakenBg"); accent = Theme.C("Amber");
            line2 = $"#{t.Order!.Number} · {t.Order.Items} · {t.Order.Total:0.##}";
        }
        else
        {
            bg = Theme.C("Kpi"); accent = Theme.C("TxtDim");
            line2 = "free";
        }

        var stack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        stack.Children.Add(new Label
        {
            Text = t.Name,
            TextColor = accent,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center
        });
        stack.Children.Add(new Label
        {
            Text = line2,
            TextColor = Theme.C("TxtDim"),
            FontSize = 12,
            HorizontalOptions = LayoutOptions.Center
        });
        if (t.Taken && t.Order!.PendingCourse)
            stack.Children.Add(new Label
            {
                Text = "course pending",
                TextColor = Theme.C("Danger"),
                FontSize = 11,
                HorizontalOptions = LayoutOptions.Center
            });

        var border = new Border
        {
            BackgroundColor = bg,
            Stroke = Theme.C("HudLine"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(6),
            Content = stack
        };
        // P1.9: soft layered depth on High only — Lite keeps the flat 1px edge.
        Theme.Depth(border);
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await OpenTable(t);
        border.GestureRecognizers.Add(tap);
        return border;
    }

    private async Task OpenTable(TableInfo t)
    {
        await Navigation.PushAsync(new OrderPage(_room, t.Name));
    }

    private async void OnBell(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AlertsPage());
    }

    /// <summary>
    /// P1.9 Appearance — the tablet's Settings entry, mirroring Windows Settings → Appearance:
    /// Theme (Dark / Light) and Graphics (Auto / High / Lite), with the resolved tier and its
    /// reason shown in the sheet title. Both persist to Preferences and rebuild the shell, which
    /// re-parses every page against the new palette. Native action sheets on purpose — no custom
    /// gesture-driven surface (see the Android defect backlog in PLAN §9).
    /// </summary>
    private async Task ShowAppearance()
    {
        var themeName = Theme.Mode == ThemeMode.Dark ? "Dark" : "Light";
        var qualityName = Theme.QualityChoice switch
        {
            "high" => "High",
            "lite" => "Lite",
            _ => "Auto"
        };
        var pick = await DisplayActionSheet(
            $"Appearance · {themeName} · {Theme.QualityReason}",
            "Close", null,
            $"Theme: {themeName}",
            $"Graphics: {qualityName}");

        if (pick is "Theme: Dark" or "Theme: Light")
        {
            var chosen = await DisplayActionSheet("Theme", "Cancel", null, "Dark", "Light");
            if (chosen is "Dark" or "Light" && chosen != themeName)
                Theme.SetTheme(chosen.ToLowerInvariant());
            return;
        }
        if (pick != null && pick.StartsWith("Graphics: ", StringComparison.Ordinal))
        {
            var chosen = await DisplayActionSheet($"Graphics · {Theme.QualityReason}", "Cancel", null,
                "Auto", "High", "Lite");
            if (chosen is "Auto" or "High" or "Lite")
                Theme.SetQuality(chosen.ToLowerInvariant());
        }
    }

    private async void OnRefresh(object sender, EventArgs e)
    {
        await Reload();
        refreshView.IsRefreshing = false;
    }

    private void OnLogout(object sender, EventArgs e)
    {
        AppState.StopAlertPolling();
        AppState.Logout();
        AppState.Api.ClearConnection();
        App.ShowLogin();
    }
}

internal static class TableInfoExt
{
    public static bool ReadyOrder(this TableInfo t) => t.Taken && t.Order != null && t.Order.Ready;
}
