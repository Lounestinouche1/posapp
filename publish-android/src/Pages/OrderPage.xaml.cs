using System.ComponentModel;
using System.Globalization;
using VectronTablet.Models;
using VectronTablet.Services;

using Microsoft.Maui.Controls.Shapes;
namespace VectronTablet.Pages;

public class ProductTile : INotifyPropertyChanged
{
    public string Name { get; set; } = "";
    public string PriceText { get; set; } = "";
    public Color Accent { get; set; } = Colors.Gray;
    public bool EightySix { get; set; }
    public ProductInfo? Product { get; set; }

    // Tile height: leaves ~80% of the tile for the image, the rest for the
    // name (up to 2 lines) + price stacked underneath.
    public int TileHeight { get; set; } = 180;

    // P1.9: colours come from the appearance layer, never from a literal — the tile price
    // follows the palette (Price / DangerInk) so it stays legible in Dark and Light.
    public Color PriceColor => Theme.C(EightySix ? "DangerInk" : "Price");
    public TextDecorations NameDecorations => EightySix ? TextDecorations.Strikethrough : TextDecorations.None;
    public bool ShowEightySix => EightySix;
    public bool ShowPlus => !EightySix;

    private ImageSource? _image;
    public ImageSource? Image
    {
        get => _image;
        set { _image = value; OnPropertyChanged(nameof(Image)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class BasketItem
{
    public required LineRequest Req { get; init; }
    public required ProductInfo Product { get; init; }
    public string Variant { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public string Key => $"{Req.Name}|{Variant}|{string.Join(",", Req.Modifiers ?? [])}|{Req.Note}|{Req.Course}|{Req.Seat}";
    public string Display => Variant.Length > 0 ? $"{Req.Name} ({Variant})" : Req.Name;
}

public partial class OrderPage : ContentPage
{
    private readonly string _room;
    private readonly string _table;
    private readonly List<BasketItem> _basket = [];
    private List<ProductInfo> _products = [];
    private List<ModGroupInfo> _groups = [];
    private List<string> _categories = [];
    private string _category = "";
    private OrderDetailInfo? _order;
    private bool _loaded;

    // P1.6 — swipe-vs-tap guard: a finger that travels (page scrolled while a tile is held)
    // must not add the product. The scroll event sets this between press and tap.
    private bool _tilePressed;
    private bool _tileScrolled;

    // P1.6 — draft basket per table: back-navigation used to drop unsent lines.
    // Saved on Disappearing, restored once in OnAppearing, cleared on send.
    private static readonly Dictionary<string, List<BasketItem>> Drafts = new();
    private string DraftKey => _room + "|" + _table;

    private static string Cur => AppState.Features?.Currency ?? "";

    public OrderPage(string room, string table)
    {
        InitializeComponent();
        _room = room;
        _table = table;
        titleLabel.Text = $"{room} · {table}";
        basketCountLabel.Text = "0 items to send";
        totalAmountLabel.Text = Money(0);   // P1.5: total visible from first paint (Windows parity)

        // P1.4 flat pressed feedback (a VisualStateManager Pressed state is a no-op on MAUI
        // Android — verified on device, so bind the pointer events instead). These read the live
        // base colour at press time and restore on release, so they coexist with the send
        // button's enabled/disabled repaint and the fire/bill toggle states. P1.5: the click
        // travels through PressBind (a recognizer consumes the touch stream, so XAML Clicked
        // would be dead on these buttons) — see Theme.PressBind.
        Theme.PressBind(backBtn, () => OnBack(backBtn, EventArgs.Empty));
        Theme.PressBind(fireBtn, () => OnFire(fireBtn, EventArgs.Empty));
        Theme.PressBind(billBtn, () => OnBill(billBtn, EventArgs.Empty));
        Theme.PressBind(sendBtn, () => OnSend(sendBtn, EventArgs.Empty),
            canClick: () => _basket.Count > 0 && !_sending);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded)
        {
            var menu = await AppState.Api.Menu();
            if (menu.Ok)
            {
                _products = menu.Products;
                _groups = menu.ModifierGroups;
                _categories = menu.Categories;
                if (_categories.Count == 0)
                    _categories = _products.Select(p => p.Category).Distinct().ToList();
                if (_categories.Count > 0) _category = _categories[0];
                BuildCategoryChips();
                BuildProducts();
            }
            else
            {
                lastStatusLabel.Text = menu.Error ?? "Menu unavailable.";
            }
            _loaded = true;
        }
        await LoadOrder();

        // P1.6 — restore an unsent draft basket (never over live lines: if the in-memory
        // basket already has content — e.g. returning from a modal — leave it alone).
        if (_basket.Count == 0 && Drafts.TryGetValue(DraftKey, out var draft) && draft.Count > 0)
        {
            _basket.AddRange(draft);
            RenderBasket();
            // P1.6 — bring the restored rows into view: they sit below the fold when
            // "On the table" pushes the section down (the page reopens at the top).
            // A fresh OrderPage instance scrolls before its first layout pass, so an
            // immediate ScrollToAsync works on stale positions and no-ops: wait for the
            // section to get its real size (SizeChanged — event-driven, no timer) and
            // dispatch the scroll after that traversal has completed. The immediate call
            // below stays as the best effort for the case where layout is already done.
            void ScrollAfterLayout(object? s, EventArgs e)
            {
                basketSection.SizeChanged -= ScrollAfterLayout;
                Dispatcher.Dispatch(() =>
                    _ = scroller.ScrollToAsync(basketSection, ScrollToPosition.MakeVisible, true));
            }
            basketSection.SizeChanged += ScrollAfterLayout;
            try { await scroller.ScrollToAsync(basketSection, ScrollToPosition.MakeVisible, true); }
            catch (ArgumentException) { /* not attached yet (first paint race) */ }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // P1.6 — keep the draft so back-then-reopen doesn't lose unsent lines.
        if (_basket.Count > 0) Drafts[DraftKey] = [.. _basket];
        else Drafts.Remove(DraftKey);
    }

    private string Money(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture) + (Cur.Length > 0 ? " " + Cur : "");

    // P1.6 — the bottom-bar status line is a 2-line TailTruncation label, but server-side
    // messages (printer warnings) embed "\n\n" guidance blocks. A newline mid-message leaves
    // line 2 empty, so the label rendered a bare "…" and hid the guidance. Flatten to one
    // line: the label then wraps it across its two lines normally.
    private static string OneLine(string s) =>
        string.Join(" ", (s ?? "").Split(new[] { '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries)).Trim();

    // ---------- order (sent) section ----------

    private async Task LoadOrder()
    {
        try
        {
            _order = await AppState.Api.GetOrder(_room, _table);
        }
        catch (PosApiException ex)
        {
            lastStatusLabel.Text = ex.Message;
            return;
        }
        RenderOrder();
    }

    private void RenderOrder()
    {
        sentLines.Children.Clear();
        var taken = _order != null;
        fireBtn.IsVisible = taken;
        billBtn.IsVisible = taken;
        if (!taken)
        {
            sentSection.IsVisible = false;
            statusLabel.Text = "free";
            statusLabel.TextColor = Theme.C("TxtDim");
            return;
        }
        sentSection.IsVisible = true;
        statusLabel.Text = _order!.Ready
            ? $"#{_order.Number} · ready ✓ · {_order.Operator}"
            : $"#{_order.Number} · {_order.Operator} · course {_order.FiredCourse}";
        statusLabel.TextColor = _order.Ready ? Theme.C("Green") : Theme.C("Amber");

        var idx = 0;
        foreach (var l in _order.Items)
        {
            var lineIndex = idx++;
            var captured = l;
            var border = RenderLine(l.Product, l.Qty, l.Modifiers, l.Note, l.Price, l.Course,
                onQtyDelta: delta =>
                {
                    var q = captured.Qty + delta;
                    if (q < 1) return Task.CompletedTask;
                    return SendLineEdit(new LineEditRequest
                    {
                        Room = _room,
                        Table = _table,
                        Index = lineIndex,
                        Operator = AppState.Operator,
                        Product = captured.Product,
                        Qty = q
                    });
                },
                onRemove: () => SendLineEdit(new LineEditRequest
                {
                    Room = _room,
                    Table = _table,
                    Index = lineIndex,
                    Operator = AppState.Operator,
                    Product = captured.Product,
                    Remove = true
                }));
            sentLines.Children.Add(border);
        }
        sentTotalLabel.Text = Money(_order.Total);
    }

    /// <summary>P1.5 flat enabled/disabled paint for the qty step buttons (Windows P1.4 disabled
    /// rule: fill 65% toward the background + dim text). IsEnabled is deliberately never toggled —
    /// Android's MaterialButton overrides the background with its platform grey tint in the
    /// disabled state (verified on device, the `#C7C7C7` in p14-v2-final.png), so the state is
    /// painted directly and the click handlers guard re-entry instead.</summary>
    private static void SetStepEnabled(Button b, bool on)
    {
        b.IsEnabled = true; // never platform-disabled — see note above
        if (on)
        {
            b.BackgroundColor = Theme.C("Panel2");
            b.TextColor = Theme.C("Txt");
        }
        else
        {
            b.BackgroundColor = Mix(Theme.C("Panel2"), Theme.C("Bg"), 0.65);
            b.TextColor = Theme.C("TxtDim");
        }
    }

    /// <summary>P1.5: a plain qty-step button. Press feedback + click are attached by the caller
    /// via Theme.PressBind (so a dead minus at qty=1 gets no bind at all — flat paint, no
    /// response, exactly the Windows disabled rule).</summary>
    private static Button MakeStepBtn(string text)
    {
        return new Button
        {
            Text = text,
            WidthRequest = 36,
            HeightRequest = 36,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            Padding = new Thickness(0),
            CornerRadius = 10,
            BackgroundColor = Theme.C("Panel2"),
            TextColor = Theme.C("Txt"),
            VerticalOptions = LayoutOptions.Center
        };
    }

    private Border RenderLine(string name, int qty, string mods, string note, decimal unitPrice, int course,
        Func<int, Task>? onQtyDelta = null, Func<Task>? onRemove = null, Action? onNameTap = null)
    {
        var left = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
        // P1.5 in-place qty: this title and the price below are re-written on every step
        // (the row is never rebuilt — see the curQty note further down).
        var title = new Label
        {
            Text = $"{qty} × {name}",
            TextColor = Theme.C("Txt"),
            FontSize = 15
        };
        left.Children.Add(title);
        var sub = string.Join(" · ", new[] { mods, note }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (course > 0 && AppState.Features?.CourseManagement == true)
            sub = sub.Length > 0 ? $"C{course} · {sub}" : $"course {course}";
        if (sub.Length > 0)
            left.Children.Add(new Label { Text = sub, TextColor = Theme.C("TxtDim"), FontSize = 12 });
        if (onNameTap != null)
        {
            var nameTap = new TapGestureRecognizer();
            nameTap.Tapped += (_, _) => onNameTap();
            left.GestureRecognizers.Add(nameTap);
        }

        var price = new Label
        {
            Text = Money(unitPrice * qty),
            TextColor = Theme.C("Txt"),
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End
        };

        var grid = new Grid { ColumnDefinitions = Theme.Cols("*,Auto,Auto,Auto"), ColumnSpacing = 10 };

        Button? minus = null, plus = null, del = null;
        Label? qtyLbl = null;
        // P1.5 in-place qty state: the row is NOT rebuilt on qty changes (that disconnects
        // handlers while the Touch pipeline is hot → the TapAndPanGestureDetector NRE crash),
        // so this row owns its live qty and label.
        var curQty = qty;
        if (onQtyDelta != null)
        {
            minus = MakeStepBtn("−");
            plus = MakeStepBtn("+");
            SetStepEnabled(minus, qty > 1); // P1.5: flat paint instead of IsEnabled (grey tint)
            qtyLbl = new Label
            {
                Text = qty.ToString(),
                WidthRequest = 26,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Theme.C("Txt"),
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };
            var stepper = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
            stepper.Children.Add(minus);
            stepper.Children.Add(qtyLbl);
            stepper.Children.Add(plus);
            Grid.SetColumn(stepper, 1);
            grid.Children.Add(stepper);
        }

        Grid.SetColumn(price, 2);
        grid.Children.Add(price);

        if (onRemove != null)
        {
            del = new Button
            {
                Text = "✕",
                WidthRequest = 36,
                HeightRequest = 36,
                FontSize = 14,
                Padding = new Thickness(0),
                CornerRadius = 10,
                BackgroundColor = Colors.Transparent,
                TextColor = Theme.C("Danger"),
                BorderColor = Theme.C("Danger"),
                BorderWidth = 1,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(del, 3);
            grid.Children.Add(del);
        }

        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        // P1.5: busy/disabled is painted flat (never IsEnabled — platform grey tint override),
        // so double-fire re-entry is guarded with this shared flag instead.
        var busy = false;

        void DisableAll()
        {
            if (minus != null) SetStepEnabled(minus, false);
            if (plus != null) SetStepEnabled(plus, false);
            if (del != null)
            {
                del.BackgroundColor = Theme.C("Bg");
                del.TextColor = Theme.C("TxtDim");
                del.BorderColor = Theme.C("TxtDim");
            }
        }

        void Restore()
        {
            if (minus != null) SetStepEnabled(minus, curQty > 1);
            if (plus != null) SetStepEnabled(plus, true);
            if (del != null)
            {
                del.BackgroundColor = Colors.Transparent;
                del.TextColor = Theme.C("Danger");
                del.BorderColor = Theme.C("Danger");
            }
        }

        if (minus != null && plus != null && onQtyDelta != null)
        {
            var deltaHandler = new Func<int, Task>(onQtyDelta);
            // P1.5: the click travels through PressBind (a recognizer consumes the touch stream,
            // so a Clicked handler would be dead — see Theme.PressBind). Both steppers are bound
            // for the life of the row (no rebuild re-creates them): the minus is gated at qty=1
            // by canClick (no press-light, no click — the flat disabled rule) and by the guard
            // below; its disabled/enabled PAINT follows curQty via Restore/SyncQtyPaint.
            Theme.PressBind(minus, async () =>
            {
                if (busy || curQty <= 1) return;
                busy = true;
                DisableAll();
                try
                {
                    curQty--;
                    if (qtyLbl != null) qtyLbl.Text = curQty.ToString();
                    title.Text = $"{curQty} × {name}";
                    price.Text = Money(unitPrice * curQty);
                    await deltaHandler(-1);
                }
                finally { busy = false; Restore(); }
            },
            canClick: () => curQty > 1);
            Theme.PressBind(plus, async () =>
            {
                if (busy) return;
                busy = true;
                DisableAll();
                try
                {
                    curQty++;
                    if (qtyLbl != null) qtyLbl.Text = curQty.ToString();
                    title.Text = $"{curQty} × {name}";
                    price.Text = Money(unitPrice * curQty);
                    await deltaHandler(1);
                }
                finally { busy = false; Restore(); }
            });
        }

        if (del != null && onRemove != null)
        {
            var removeHandler = onRemove;
            del.Clicked += async (_, _) =>
            {
                if (busy) return;
                busy = true;
                DisableAll();
                try { await removeHandler(); }
                finally { busy = false; Restore(); }
            };
        }

        return new Border
        {
            BackgroundColor = Theme.C("Panel"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Padding = new Thickness(12, 8),
            Content = grid
        };
    }

    // ---------- menu ----------

    // Chip base fills so the pressed step (P1.4) always restores the right active/inactive
    // colour — stored per chip, dies with the row rebuild. Event-driven, no animation.
    private readonly Dictionary<Button, Color> _chipBase = new();

    private void BuildCategoryChips()
    {
        categoryRow.Children.Clear();
        _chipBase.Clear();
        foreach (var cat in _categories)
        {
            var name = cat;
            var chip = new Button { Text = name, Style = Theme.S("ChipBtn") };
            chip.Margin = new Thickness(0);   // spacing moves to the ring below
            // P1.7 Flat-HUD: each chip sits in a 1px ring — transparent (active) or the
            // HUD outline colour (inactive). A MAUI Button has no stroke of its own.
            var ring = new Border
            {
                Content = chip,   // sets chip.Parent = ring (that's how RefreshChip finds it)
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                StrokeThickness = 1,
                Padding = 0,
                Margin = new Thickness(0, 0, 8, 0)
            };
            RefreshChip(chip, name);
            // Flat pressed step (parity with the Windows P1.4 tabs / tile pressed): one tone
            // lighter while the finger is down. The click travels through PressBind — a
            // recognizer consumes the touch stream, so a Clicked handler would be dead
            // (P1.5 fix, see Theme.PressBind).
            Theme.PressBind(chip, () => { _category = name; RefreshCategoryChips(); BuildProducts(); });
            categoryRow.Children.Add(ring);
        }
        RefreshCategoryChips();
    }

    private void RefreshChip(Button chip, string name)
    {
        var active = name == _category;
        // P1.7 Flat-HUD (Windows UpdateTabs parity): the active chip stays a solid mint fill;
        // the inactive ones drop the panel fill for a 1px #2E3A4B outline on the background
        // — the "segmented control" look. Flat pressed step still restores from _chipBase.
        var back = active ? Theme.C("Mint") : Theme.C("Bg");
        _chipBase[chip] = back;
        chip.BackgroundColor = back;
        chip.TextColor = active ? Theme.C("Bg") : Theme.C("Txt");
        if (chip.Parent is Border ring)
        {
            ring.Stroke = Theme.C("HudLine");
            ring.StrokeThickness = active ? 0 : 1;
        }
    }

    private void RefreshCategoryChips()
    {
        foreach (var child in categoryRow.Children)
        {
            if (child is Border { Content: Button chip }) RefreshChip(chip, chip.Text);   // P1.7: chips live in a ring
            else if (child is Button bare) RefreshChip(bare, bare.Text);
        }
    }

    // P2 audit note: colour *interpolation* helpers, not palette literals. They derive
    // hover/pressed/disabled steps from a live theme token (Theme.C / theme colours), the
    // same role as Skin.Shade/MixToward on Windows — Theme.Press does the same job for
    // simple press steps. Kept on the order page because the flat ramp is page-specific.
    private static Color Lighten(Color c, double t)
    {
        var r = (int)(c.Red * 255);
        var g = (int)(c.Green * 255);
        var b = (int)(c.Blue * 255);
        return Color.FromArgb($"#{(int)(r + (255 - r) * t):X2}{(int)(g + (255 - g) * t):X2}{(int)(b + (255 - b) * t):X2}");
    }

    /// <summary>P1.5 flat disabled rule (Windows P1.4 parity): the fill mixed toward another
    /// colour — used at t=0.65 toward the page background for a disabled/busy button.</summary>
    private static Color Mix(Color from, Color to, double t)
    {
        var fr = from.Red * 255; var tr = to.Red * 255;
        var fg = from.Green * 255; var tg = to.Green * 255;
        var fb = from.Blue * 255; var tb = to.Blue * 255;
        return Color.FromArgb($"#{(int)(fr + (tr - fr) * t):X2}{(int)(fg + (tg - fg) * t):X2}{(int)(fb + (tb - fb) * t):X2}");
    }

    private void BuildProducts()
    {
        // Tile height mirrors the Windows POS: image fills the tile width and ~80% of
        // its height; the name (up to 2 lines) and the price share the rows underneath.
        var viewW = productView.Width > 10 ? productView.Width : (Application.Current?.Windows.FirstOrDefault()?.Width ?? 0);
        if (viewW <= 10) viewW = 360;
        var tileW = viewW / 3 - 8;
        // Bottom block is now name (up to 2 lines) + price, so the tile is taller than
        // the old single-line layout — the image keeps its previous size.
        var tileH = tileW >= 170 ? 205 : 175;

        var list = _products.Where(p => p.Category == _category).Select(p => new ProductTile
        {
            Name = p.Name,
            PriceText = p.Eightysix ? "86'd" : Money(p.Price) + (p.Weighted ? "/kg" : ""),
            Accent = Color.FromArgb(string.IsNullOrWhiteSpace(p.Color) ? Theme.C("HudLine").ToHex() : p.Color),
            EightySix = p.Eightysix,
            TileHeight = tileH,
            Product = p,
            Image = ProductImages.Get(p.Image)
        }).ToList();
        _visibleTiles = list;
        productView.ItemsSource = list;
        productView.HeightRequest = Math.Max(90, Math.Ceiling(list.Count / 3.0) * (tileH + 8));

        // Download any images we do not have yet, then paint them into the tiles.
        foreach (var name in list.Where(t => !string.IsNullOrWhiteSpace(t.Product?.Image))
                                 .Select(t => t.Product!.Image)
                                 .Distinct())
            if (ProductImages.TryStartFetch(name)) _ = AttachImageAsync(name);
    }

    private List<ProductTile> _visibleTiles = [];

    private async Task AttachImageAsync(string name)
    {
        var src = await ProductImages.FetchAsync(name);
        if (src == null) return;
        foreach (var tile in _visibleTiles.Where(t => t.Product?.Image == name))
            tile.Image = src;
    }

    /// <summary>Flat pressed state on product tiles (P1.3 parity with Windows ProductTile):
    /// the tile fill steps one tone lighter while the finger is down — event-driven, no animation.</summary>
    private void OnTilePressed(object? sender, PointerEventArgs e)
    {
        _tilePressed = true;
        _tileScrolled = false;
        if (sender is Border b) b.BackgroundColor = Theme.C("Panel2");
    }

    private void OnTileReleased(object? sender, PointerEventArgs e)
    {
        // Only the colour: the tap flags survive until OnProductTap consumes them
        // (event order between PointerReleased and Tapped is not guaranteed).
        if (sender is Border b) b.BackgroundColor = Theme.C("Panel");
    }

    // P1.6 — records page scrolling that happens while a tile is held down: that motion is a
    // swipe (browse), not an add. Without this, the grid's scroll ended in a "tap" on the tile
    // under the finger and products got added while the user was just scrolling (defect 5).
    private void OnScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_tilePressed) _tileScrolled = true;
    }

    private async void OnProductTap(object sender, EventArgs e)
    {
        if (_tileScrolled)
        {
            // finger travelled while held -> swipe, not a tap: swallow it
            _tileScrolled = false;
            _tilePressed = false;
            return;
        }
        _tilePressed = false;
        if (sender is not BindableObject bo || bo.BindingContext is not ProductTile tile || tile.Product == null)
            return;
        if (Theme.IsHigh && sender is VisualElement ve)
        {
            await ve.ScaleTo(0.96, 65, Easing.CubicOut);
            await ve.ScaleTo(1.0, 65, Easing.CubicIn);
        }
        var product = tile.Product;
        if (product.Eightysix)
        {
            await DisplayAlert("86 board", $"{product.Name} is out of stock.", "OK");
            return;
        }

        var variant = "";
        var price = product.Price;
        if (AppState.Features?.ProductVariants == true && product.Variants.Count > 0)
        {
            var choices = product.Variants.Select(v => $"{v.Name} — {Money(v.Price)}").ToArray();
            var pick = await DisplayActionSheet(product.Name, "Cancel", null, choices);
            if (pick == null || pick == "Cancel") return;
            var idx = Array.IndexOf(choices, pick);
            if (idx < 0) return;
            variant = product.Variants[idx].Name;
            price = product.Variants[idx].Price;
        }

        if (AppState.Features?.WeightedItems == true && product.Weighted)
        {
            var wtxt = await DisplayPromptAsync($"Weight — {product.Name}", $"Price {Money(product.Price)}/kg. Enter kg:", "OK", "Cancel", keyboard: Keyboard.Numeric);
            if (wtxt == null) return;
            wtxt = wtxt.Replace(',', '.');
            if (!decimal.TryParse(wtxt, NumberStyles.Number, CultureInfo.InvariantCulture, out var w) || w <= 0)
            {
                await DisplayAlert("Weight", "Enter a valid weight in kg.", "OK");
                return;
            }
            var item = MakeBasketItem(product, variant, price, [], "", w);
            await AddToBasket(item);
            return;
        }

        List<string> mods = [];
        if (AppState.Features?.ModifiersEnabled == true && _groups.Count > 0)
        {
            var sheet = new ModifierSheetPage(_groups, product.Name, AppState.Features?.Currency ?? "");
            await Navigation.PushModalAsync(sheet);
            mods = await sheet.Wait();
            if (mods == null) return;
        }

        await AddToBasket(MakeBasketItem(product, variant, price, mods, "", null));
    }

    private BasketItem MakeBasketItem(ProductInfo product, string variant, decimal price, List<string> mods, string note, decimal? weight)
    {
        var unit = price;
        var modSum = 0m;
        foreach (var m in mods)
        {
            var opt = _groups.SelectMany(g => g.Options).FirstOrDefault(o => o.Name == m);
            if (opt != null) modSum += opt.Price;
        }
        unit += modSum;
        if (weight.HasValue) unit = Math.Round(unit * weight.Value, 2);

        var req = new LineRequest
        {
            Name = product.Name,
            Qty = 1,
            Variant = variant.Length > 0 ? variant : null,
            Modifiers = mods.Count > 0 ? mods : null,
            Note = note.Length > 0 ? note : null,
            Weight = weight,
            Course = AppState.Features?.CourseManagement == true ? 1 : null,
            Seat = null
        };
        if (weight.HasValue)
        {
            var wtxt = weight.Value.ToString("0.###", CultureInfo.InvariantCulture);
            req.Note = note.Length > 0 ? $"{note} · {wtxt} kg" : $"{wtxt} kg @ {Money(product.Price)}/kg";
        }
        return new BasketItem { Req = req, Product = product, Variant = variant, UnitPrice = unit };
    }

    private async Task AddToBasket(BasketItem item)
    {
        var existing = _basket.FirstOrDefault(b => b.Key == item.Key);
        if (existing != null) existing.Req.Qty += 1;
        else _basket.Add(item);
        RenderBasket();
        lastStatusLabel.Text = $"+ {item.Display}";
        // P1.6 — make sure the new "To send" row is fully visible above the fixed bottom
        // bar (it used to stop half-cut at the fold after an add). MakeVisible only scrolls
        // when the target is out of view, so adding from the top doesn't yank the page.
        try { await scroller.ScrollToAsync(basketSection, ScrollToPosition.MakeVisible, true); }
        catch (ArgumentException) { /* element not attached yet (first paint race) */ }
    }

    private void RenderBasket()
    {
        basketLines.Children.Clear();
        foreach (var item in _basket)
        {
            var mods = string.Join(", ", item.Req.Modifiers ?? []);
            var subParts = new List<string>();
            if (item.Req.Course is > 0) subParts.Add($"course {item.Req.Course}");
            if (item.Req.Seat is > 0) subParts.Add($"seat {item.Req.Seat}");
            if (mods.Length > 0) subParts.Add(mods);
            if (!string.IsNullOrWhiteSpace(item.Req.Note)) subParts.Add(item.Req.Note!);
            var captured = item;
            var border = RenderLine(item.Display, item.Req.Qty, string.Join(" · ", subParts), "",
                item.UnitPrice, 0,
                // P1.5 FIX: qty changes update this row IN PLACE (RenderLine owns the qty label
                // and step-button states) instead of Clear+rebuilding basketLines. The full
                // rebuild disconnected the row's handler while MAUI's Touch pipeline for its
                // PressBind'ed buttons could still receive a trailing MotionEvent — that made
                // HasAnyPointerGestures() call GetView() == null and hard-crash the app
                // (captured stack: NullReferenceException in TapAndPanGestureDetector.OnTouchEvent).
                // Only a row REMOVAL (qty can't reach 0 here; del button covers that) rebuilds.
                onQtyDelta: delta =>
                {
                    captured.Req.Qty += delta;
                    if (captured.Req.Qty < 1)
                    {
                        _basket.Remove(captured);
                        RenderBasket();
                    }
                    else
                    {
                        UpdateBasketTotals();
                    }
                    lastStatusLabel.Text = $"{captured.Display} · ×{captured.Req.Qty}";
                    return Task.CompletedTask;
                },
                onRemove: () =>
                {
                    _basket.Remove(captured);
                    RenderBasket();
                    lastStatusLabel.Text = $"− {captured.Display}";
                    return Task.CompletedTask;
                },
                onNameTap: () => EditBasketLine(captured));
            basketLines.Children.Add(border);
        }
        UpdateBasketTotals();
    }

    /// <summary>P1.5: counts, total and send-button state — the non-rebuilding half of
    /// RenderBasket, so a qty tap updates in place (no child removal → no handler
    /// disconnect → no stale Touch pipeline; see onQtyDelta note above).</summary>
    private void UpdateBasketTotals()
    {
        var total = 0m;
        var count = 0;
        foreach (var item in _basket)
        {
            total += item.UnitPrice * item.Req.Qty;
            count += item.Req.Qty;
        }
        var empty = _basket.Count == 0;
        emptyTitle.IsVisible = empty;   // P1.5: Windows-parity two-label empty state
        emptyHint.IsVisible = empty;
        basketHint.IsVisible = !empty;
        SetSendEnabled(!empty);
        basketCountLabel.Text = $"{count} item{(count == 1 ? "" : "s")} to send";
        totalAmountLabel.Text = Money(total);
    }

    private async Task EditBasketLine(BasketItem item)
    {
        var options = new List<string> { "Change note" };
        if (AppState.Features?.CourseManagement == true) options.Add("Change course");
        if (AppState.Features?.SeatOrdering == true) options.Add("Change seat");
        options.Add("Cancel");

        var pick = await DisplayActionSheet(item.Display, null, null, options.ToArray());
        switch (pick)
        {
            case "Change note":
                var note = await DisplayPromptAsync("Item note", item.Display, "OK", "Cancel",
                    initialValue: item.Req.Note ?? "");
                if (note != null)
                {
                    item.Req.Note = note.Length == 0 ? null : note;
                }
                break;
            case "Change course":
                var names = AppState.Features?.CourseNames ?? [];
                var courseCount = Math.Max(1, names.Count);
                var labels = Enumerable.Range(1, courseCount)
                    .Select(i => names.Count >= i ? $"{i} — {names[i - 1]}" : $"Course {i}").ToArray();
                var cpick = await DisplayActionSheet("Course", "Cancel", null, labels);
                if (cpick != null && cpick != "Cancel" && int.TryParse(cpick.Split(' ')[0], out var cnum))
                    item.Req.Course = cnum;
                break;
            case "Change seat":
                var spick = await DisplayActionSheet("Seat", "Cancel", null,
                    Enumerable.Range(1, 8).Select(i => $"Seat {i}").ToArray());
                if (spick != null && spick != "Cancel" && int.TryParse(spick.Replace("Seat ", ""), out var snum))
                    item.Req.Seat = snum;
                break;
        }
        RenderBasket();
    }

    // ---------- actions ----------

    private bool _sendingLine;
    private bool _sending;

    private async Task SendLineEdit(LineEditRequest req)
    {
        if (_sendingLine) return;
        _sendingLine = true;
        try
        {
            var res = await AppState.Api.LineEdit(req);
            if (!res.Ok)
            {
                await DisplayAlert("Edit line", res.Error ?? "Failed.", "OK");
                // P1.5: the qty label now updates in place BEFORE the round-trip, so any
                // failed edit must resync from the server or the row would show the
                // optimistic qty that never landed.
                await LoadOrder();
                return;
            }
            var msg = res.Removed ? "Line removed" : res.Unchanged ? "No change" : $"Quantity set to {res.Qty}";
            if (res.OrderGone) msg += " · table is now free";
            if (res.PrintWarnings.Count > 0) msg += " · ⚠ " + OneLine(res.PrintWarnings[0]);
            lastStatusLabel.Text = msg;
            await LoadOrder();
        }
        finally
        {
            _sendingLine = false;
        }
    }

    /// <summary>Flat disabled/loading state for the send button (P1.4): muted Panel2 fill and
    /// dim text instead of the platform's bright system-grey disabled tint. The button is
    /// deliberately never IsEnabled=false — Android's MaterialButton overrides BackgroundColor
    /// with its own grey in that state — so the click is guarded in OnSend instead.</summary>
    private void SetSendEnabled(bool on)
    {
        sendBtn.BackgroundColor = on ? Theme.C("Mint") : Theme.C("Panel2");
        sendBtn.TextColor = on ? Theme.C("Bg") : Theme.C("TxtDim");
    }

    private async void OnSend(object sender, EventArgs e)
    {
        if (_basket.Count == 0 || _sending) return;
        _sending = true;
        SetSendEnabled(false);
        sendBtn.Text = "Sending…";
        try
        {
            var pendingId = Guid.NewGuid().ToString("N");
            var req = new OrderRequest
            {
                Room = _room,
                Table = _table,
                Operator = AppState.Operator,
                PendingId = pendingId,
                Items = _basket.Select(b => b.Req).ToList()
            };
            var res = await AppState.Api.SendOrder(req);
            SetSendEnabled(true);
            sendBtn.Text = "Send to kitchen";

            if (!res.Ok)
            {
                if (res.Network)
                {
                    OfflineQueue.Enqueue(req, pendingId);
                    lastStatusLabel.Text = $"Offline — order saved locally ({OfflineQueue.Count} pending)";
                    _basket.Clear();
                    Drafts.Remove(DraftKey);   // P1.6: draft goes with the sent lines
                    RenderBasket();
                    SetSendEnabled(false);
                    return;
                }
                lastStatusLabel.Text = res.Error ?? "Send failed.";
                await DisplayAlert("Send failed", res.Error ?? "Unknown error.", "OK");
                SetSendEnabled(_basket.Count > 0);
                return;
            }

            var msg = $"Order #{res.Order} sent · {res.Lines} line{(res.Lines == 1 ? "" : "s")} · {Money(res.Total)}";
            if (res.PrintWarnings.Count > 0) msg += " · ⚠ " + OneLine(res.PrintWarnings[0]);
            if (res.Issues.Count > 0) msg += " · " + OneLine(string.Join("; ", res.Issues));
            lastStatusLabel.Text = msg;
            _basket.Clear();
            Drafts.Remove(DraftKey);   // P1.6: nothing unsent left
            RenderBasket();
            _ = AppState.FlushQueue();
            await LoadOrder();
        }
        finally
        {
            _sending = false;
        }
    }

    private async void OnFire(object sender, EventArgs e)
    {
        if (_order == null) return;
        var go = await DisplayAlert("Fire next course",
            $"Print the next course for {_room} · {_table}?", "Fire", "Cancel");
        if (!go) return;
        var res = await AppState.Api.Fire(_room, _table, AppState.Operator);
        if (!res.Ok)
        {
            await DisplayAlert("Fire", res.Error ?? "Failed.", "OK");
            return;
        }
        lastStatusLabel.Text = res.PrintWarnings.Count > 0
            ? $"Course {res.Course} fired · ⚠ {OneLine(res.PrintWarnings[0])}"
            : $"Course {res.Course} fired ({res.Lines} lines)";
        await LoadOrder();
    }

    private async void OnBill(object sender, EventArgs e)
    {
        if (_order == null) return;
        var go = await DisplayAlert("Print bill",
            $"Print the bill for {_room} · {_table} (order #{_order.Number})?", "Print", "Cancel");
        if (!go) return;
        var res = await AppState.Api.Bill(_room, _table, AppState.Operator);
        if (!res.Ok)
        {
            await DisplayAlert("Bill", res.Error ?? "Failed.", "OK");
            return;
        }
        lastStatusLabel.Text = $"Bill printed ({res.Bill})";
    }

    private async void OnBack(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
