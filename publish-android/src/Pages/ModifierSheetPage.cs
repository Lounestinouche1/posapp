using System.Globalization;
using VectronTablet.Models;

namespace VectronTablet.Pages;

public class ModifierSheetPage : ContentPage
{
    private readonly List<ModGroupInfo> _groups;
    private readonly string _currency;
    private readonly List<(CheckBox box, string name)> _checks = [];
    private readonly TaskCompletionSource<List<string>?> _tcs = new();

    public ModifierSheetPage(List<ModGroupInfo> groups, string productName, string currency)
    {
        _groups = groups;
        _currency = currency;
        Title = productName;
        BackgroundColor = Theme.C("Bg");

        var scroll = new ScrollView();
        var root = new VerticalStackLayout { Padding = new Thickness(18, 18, 18, 28), Spacing = 6 };

        root.Children.Add(new Label
        {
            Text = "Options",
            TextColor = Theme.C("Mint"),
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        foreach (var group in _groups)
        {
            root.Children.Add(new Label
            {
                Text = group.Name,
                TextColor = Theme.C("TxtDim"),
                FontSize = 14,
                Margin = new Thickness(0, 8, 0, 2)
            });
            foreach (var opt in group.Options)
            {
                var price = opt.Price.ToString("0.00", CultureInfo.InvariantCulture) +
                            (_currency.Length > 0 ? " " + _currency : "");
                var row = new Grid
                {
                    ColumnDefinitions = Theme.Cols("*,Auto,Auto"),
                    ColumnSpacing = 10,
                    Padding = new Thickness(4, 6)
                };
                var box = new CheckBox { Color = Theme.C("Mint") };
                var nameLbl = new Label
                {
                    Text = opt.Name,
                    TextColor = Theme.C("Txt"),
                    FontSize = 16,
                    VerticalOptions = LayoutOptions.Center
                };
                row.Children.Add(nameLbl);
                var priceLbl = new Label
                {
                    Text = opt.Price == 0 ? "" : "+ " + price,
                    TextColor = Theme.C("TxtDim"),
                    FontSize = 14,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(priceLbl, 1);
                row.Children.Add(priceLbl);
                Grid.SetColumn(box, 2);
                row.Children.Add(box);
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => box.IsChecked = !box.IsChecked;
                row.GestureRecognizers.Add(tap);
                root.Children.Add(row);
                _checks.Add((box, opt.Name));
            }
        }

        var buttons = new Grid { ColumnDefinitions = Theme.Cols("*,*"), ColumnSpacing = 10, Margin = new Thickness(0, 18, 0, 0) };
        var cancel = new Button { Text = "Cancel", Style = Theme.S("GhostBtn") };
        cancel.Clicked += async (_, _) =>
        {
            await Navigation.PopModalAsync();
            _tcs.TrySetResult(null);
        };
        var add = new Button { Text = "Add to order", Style = Theme.S("PrimaryBtn") };
        add.Clicked += async (_, _) =>
        {
            var selected = _checks.Where(c => c.box.IsChecked).Select(c => c.name).ToList();
            await Navigation.PopModalAsync();
            _tcs.TrySetResult(selected);
        };
        buttons.Children.Add(cancel);
        Grid.SetColumn(add, 1);
        buttons.Children.Add(add);
        root.Children.Add(buttons);

        scroll.Content = root;
        Content = scroll;
    }

    public Task<List<string>?> Wait() => _tcs.Task;
}
