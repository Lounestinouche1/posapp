using VectronTablet.Services;

using Microsoft.Maui.Controls.Shapes;
namespace VectronTablet.Pages;

public partial class AlertsPage : ContentPage
{
    private bool _alive;

    public AlertsPage()
    {
        InitializeComponent();

        // P1.4 flat pressed feedback — see OrderPage constructor note. P1.5: the clicks
        // travel through PressBind (a recognizer consumes the touch stream, so the XAML
        // Clicked would be dead — see Theme.PressBind).
        Theme.PressBind(backBtn, () => OnBack(backBtn, EventArgs.Empty));
        Theme.PressBind(clearBtn, () => OnClear(clearBtn, EventArgs.Empty));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _alive = true;
        _ = Reload();
        Dispatcher.StartTimer(TimeSpan.FromSeconds(3), () =>
        {
            if (!_alive) return false;
            _ = Reload();
            return true;
        });
    }

    protected override void OnDisappearing()
    {
        _alive = false;
        base.OnDisappearing();
    }

    private async Task Reload()
    {
        var res = await AppState.Api.Alerts();
        if (!_alive) return;
        list.Children.Clear();

        if (res.Ready.Count > 0)
        {
            list.Children.Add(Header("Kitchen says ready"));
            foreach (var r in res.Ready)
                list.Children.Add(Card($"{r.Room} · {r.Table}",
                    $"order #{r.Order} is ready · {r.Total:0.00}", "Green", null));
        }
        if (res.Alerts.Count > 0)
        {
            list.Children.Add(Header("Guest calls"));
            foreach (var a in res.Alerts)
            {
                var kind = a.Kind == "waiter" ? "🔔 waiter called" : a.Kind;
                list.Children.Add(Card($"{a.Room} · {a.Table}", kind, "Amber", a.Id));
            }
        }
        if (res.Ready.Count == 0 && res.Alerts.Count == 0)
            list.Children.Add(new Label
            {
                Text = "Nothing new. You'll be notified here when a guest calls\nor the kitchen marks an order ready.",
                TextColor = Theme.C("TxtDim"),
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(20, 40, 20, 0)
            });
    }

    private Label Header(string text) => new()
    {
        Text = text,
        TextColor = Theme.C("Txt"),
        FontSize = 16,
        FontAttributes = FontAttributes.Bold,
        Margin = new Thickness(2, 8, 0, 2)
    };

    private Border Card(string title, string subtitle, string colorKey, string? alertId)
    {
        var grid = new Grid { ColumnDefinitions = Theme.Cols("*,Auto"), ColumnSpacing = 8 };
        var left = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
        left.Children.Add(new Label { Text = title, TextColor = Theme.C("Txt"), FontSize = 16, FontAttributes = FontAttributes.Bold });
        left.Children.Add(new Label { Text = subtitle, TextColor = Theme.C("TxtDim"), FontSize = 13 });
        grid.Children.Add(left);

        var border = new Border
        {
            BackgroundColor = Theme.C("Panel"),
            Stroke = Theme.C(colorKey),
            StrokeThickness = 1.5,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(14, 10),
            Content = grid
        };

        if (alertId != null)
        {
            var dismiss = new Button
            {
                Text = "Done",
                Style = Theme.S("GhostBtn"),
                HeightRequest = 38
            };
            var id = alertId;
            dismiss.Clicked += async (_, _) =>
            {
                await AppState.Api.Dismiss(new Models.DismissRequest { Id = id });
                NotificationHelper.Cancel(id);
                await Reload();
            };
            Grid.SetColumn(dismiss, 1);
            grid.Children.Add(dismiss);
        }
        return border;
    }

    private async void OnBack(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnClear(object sender, EventArgs e)
    {
        var res = await AppState.Api.Alerts();
        foreach (var a in res.Alerts)
        {
            await AppState.Api.Dismiss(new Models.DismissRequest { Id = a.Id });
            NotificationHelper.Cancel(a.Id);
        }
        await Reload();
    }

    private async void OnRefresh(object sender, EventArgs e)
    {
        await Reload();
        ((RefreshView)sender).IsRefreshing = false;
    }
}
