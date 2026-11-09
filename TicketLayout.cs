using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace VectronPOS;

internal static class TicketLayout
{
    public static string[] CourseNames = [];
    public static string CourseTag(int c) => c <= 0 ? "" : (CourseNames.Length >= c && !string.IsNullOrWhiteSpace(CourseNames[c - 1]) ? $"[{CourseNames[c - 1]}] " : $"[C{c}] ");
    public static decimal Tendered = 0;
    public static decimal CashReceived = 0;
    public static decimal Rounding = 0;
    public static decimal TvaRate = 10m;
    public static bool PricesIncludeTva = true;
    public static int Covers = 0;

    public static readonly string[] Available = [
        "Header", "Logo", "CallNumberBox", "OrderNumber", "BillNumber", "PaymentNumber", "Operator", 
        "Room", "Table", "Covers", "Date", "Time", "Products", "Prices", "Items", 
        "OrderType", "Total", "Tendered", "Rounded", "DiscountPercent", "PriceAfterDiscount", 
        "TaxBreakdown", "BarcodeFooter", "QrFooter", 
        "Footer", "Separator", "DoubleSeparator", "DottedSeparator", "Blank line", "Cutter"
    ];

    public static List<TicketComponentStyle> PresetDefault() =>
    [
        new("Header", 14, true, false, "Center"),
        new("DoubleSeparator"),
        new("Room"),
        new("Table"),
        new("Separator"),
        new("Operator"),
        new("Date"),
        new("Time"),
        new("Separator"),
        new("Items", 10, false, false, "Left"),
        new("Separator"),
        new("Total", 14, true, false, "Right"),
        new("Tendered"),
        new("Rounded"),
        new("DoubleSeparator"),
        new("Footer", 10, false, false, "Center"),
        new("Blank line"),
        new("Cutter")
    ];

    public static List<TicketComponentStyle> PresetSupermarket() =>
    [
        new("Header", 15, true, false, "Center"),
        new("Separator"),
        new("Date"),
        new("Time"),
        new("Operator"),
        new("DoubleSeparator"),
        new("Items", 10, false, false, "Left"),
        new("DoubleSeparator"),
        new("Total", 16, true, false, "Right"),
        new("Tendered"),
        new("Rounded"),
        new("Separator"),
        new("TaxBreakdown", 9, false, false, "Left"),
        new("DoubleSeparator"),
        new("BarcodeFooter", 12, true, false, "Center"),
        new("Blank line"),
        new("Footer", 10, false, false, "Center"),
        new("Blank line"),
        new("Cutter")
    ];

    public static List<TicketComponentStyle> PresetRestaurant() =>
    [
        new("Header", 15, true, false, "Center"),
        new("DoubleSeparator"),
        new("Room"),
        new("Table"),
        new("Covers"),
        new("Operator"),
        new("Date"),
        new("Time"),
        new("DoubleSeparator"),
        new("Items", 10, false, false, "Left"),
        new("Separator"),
        new("Total", 15, true, false, "Right"),
        new("Separator"),
        new("TaxBreakdown", 9, false, false, "Left"),
        new("DoubleSeparator"),
        new("QrFooter", 10, false, false, "Center"),
        new("Blank line"),
        new("Footer", 10, false, false, "Center"),
        new("Blank line"),
        new("Cutter")
    ];

    public static List<TicketComponentStyle> PresetFastFood() =>
    [
        new("CallNumberBox", 16, true, false, "Center"),
        new("DoubleSeparator"),
        new("OrderType", 12, true, false, "Center"),
        new("Date"),
        new("Time"),
        new("Operator"),
        new("DoubleSeparator"),
        new("Items", 10, false, false, "Left"),
        new("Separator"),
        new("Total", 15, true, false, "Right"),
        new("Tendered"),
        new("Separator"),
        new("TaxBreakdown", 9, false, false, "Left"),
        new("DoubleSeparator"),
        new("Footer", 10, false, false, "Center"),
        new("Blank line"),
        new("Cutter")
    ];

    public static List<TicketComponentStyle> PresetBoutique() =>
    [
        new("Header", 15, true, false, "Center"),
        new("DoubleSeparator"),
        new("Operator"),
        new("Date"),
        new("Time"),
        new("Separator"),
        new("Items", 10, false, false, "Left"),
        new("Separator"),
        new("DiscountPercent"),
        new("PriceAfterDiscount"),
        new("Total", 16, true, false, "Right"),
        new("Tendered"),
        new("Separator"),
        new("TaxBreakdown", 9, false, false, "Left"),
        new("DoubleSeparator"),
        new("BarcodeFooter", 12, true, false, "Center"),
        new("Blank line"),
        new("Footer", 10, false, false, "Center"),
        new("Blank line"),
        new("Cutter")
    ];

    public static List<TicketComponentStyle> PresetPharmacy() =>
    [
        new("Header", 15, true, false, "Center"),
        new("DoubleSeparator"),
        new("Date"),
        new("Time"),
        new("Operator"),
        new("Separator"),
        new("Items", 10, false, false, "Left"),
        new("Separator"),
        new("Total", 16, true, false, "Right"),
        new("Tendered"),
        new("Separator"),
        new("TaxBreakdown", 9, false, false, "Left"),
        new("DoubleSeparator"),
        new("BarcodeFooter", 12, true, false, "Center"),
        new("Blank line"),
        new("Footer", 10, false, false, "Center"),
        new("Blank line"),
        new("Cutter")
    ];

    public static List<TicketComponentStyle> PresetForProfile(string profile) => (profile?.ToLowerInvariant()) switch
    {
        "supermarket" or "butchery" => PresetSupermarket(),
        "boutique" => PresetBoutique(),
        "fastfood" or "cafe" => PresetFastFood(),
        "pharmacy" => PresetPharmacy(),
        "restaurant" => PresetRestaurant(),
        _ => PresetDefault()
    };

    public static List<TicketComponentStyle> Components(PrinterDestination destination) => 
        destination.Layout is { Count: > 0 } ? destination.Layout : PresetDefault();

    public static string FormatItems(List<SoldLine> lines, int width = 36)
    {
        if (lines == null || lines.Count == 0) return "";
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            var seatPrefix = line.Seat > 0 ? $"[S{line.Seat}] " : "";
            var coursePrefix = CourseTag(line.Course);
            var name = $"{seatPrefix}{coursePrefix}{line.Product}".Trim();
            
            sb.AppendLine(name);
            
            var leftPart = $"  {line.Quantity} x {line.UnitPrice:C}";
            var rightPart = (line.UnitPrice * line.Quantity).ToString("C");
            int spaces = Math.Max(1, width - leftPart.Length - rightPart.Length);
            sb.AppendLine(leftPart + new string(' ', spaces) + rightPart);

            if (!string.IsNullOrWhiteSpace(line.Modifiers))
            {
                foreach (var mod in line.Modifiers.Split(','))
                {
                    var m = mod.Trim();
                    if (m.Length > 0) sb.AppendLine($"   + {m}");
                }
            }
            if (!string.IsNullOrWhiteSpace(line.Note))
            {
                sb.AppendLine($"   * {line.Note.Trim()}");
            }
        }
        return sb.ToString().TrimEnd();
    }

    public static string FormatTaxBreakdown(decimal total, decimal rate, int width = 36)
    {
        if (rate <= 0) return "";
        decimal ht = Math.Round(total / (1m + rate / 100m), 2);
        decimal tva = total - ht;
        var sb = new StringBuilder();
        sb.AppendLine("Taux TVA    Base HT       TVA      TTC");
        sb.AppendLine(new string('─', Math.Min(width, 36)));
        sb.AppendLine($"{rate,7:0.0}%  {ht,9:C} {tva,9:C} {total,8:C}");
        return sb.ToString().TrimEnd();
    }

    public static string FormatCallNumberBox(int num, string orderType, int width = 36)
    {
        int n = num > 0 ? num : 1;
        string line1 = $"*** {Tr.T("COMMANDE")} #{n} ***";
        string line2 = !string.IsNullOrWhiteSpace(orderType) ? orderType.ToUpperInvariant() : "";
        int w = Math.Min(Math.Max(line1.Length + 6, 32), width);
        string border = "╔" + new string('═', w - 2) + "╗";
        string bottom = "╚" + new string('═', w - 2) + "╝";
        string PadLine(string text)
        {
            if (text.Length > w - 4) text = text.Substring(0, w - 4);
            int pad = Math.Max(0, (w - 2 - text.Length) / 2);
            int rightPad = Math.Max(0, w - 2 - text.Length - pad);
            return "║" + new string(' ', pad) + text + new string(' ', rightPad) + "║";
        }
        var sb = new StringBuilder();
        sb.AppendLine(border);
        sb.AppendLine(PadLine(line1));
        if (line2.Length > 0)
        {
            sb.AppendLine(PadLine(line2));
        }
        sb.AppendLine(bottom);
        return sb.ToString().TrimEnd();
    }

    public static string Text(string component, PrinterDestination destination, string room, string table, string operatorName, List<SoldLine> lines, int orderNumber = 0, int billNumber = 0, int paymentNumber = 0, string orderType = "", decimal discountPercent = 0, decimal discountAmount = 0) => component switch
    {
        "Header" => destination.Header,
        "Operator" => Tr.T("Operator: ") + operatorName,
        "Room" => Tr.T("Room: ") + room,
        "Table" => Tr.T("Table: ") + table,
        "Covers" => Covers > 0 ? $"{Tr.T("Covers:")} {Covers}" : "",
        "Date" => DateTime.Now.ToString("dd/MM/yyyy"),
        "Time" => DateTime.Now.ToString("HH:mm:ss"),
        "CallNumberBox" => FormatCallNumberBox(orderNumber > 0 ? orderNumber : (billNumber > 0 ? billNumber : 1), orderType),
        "OrderNumber" => orderNumber > 0 ? $"{Tr.T("Order #:")} {orderNumber}" : "",
        "BillNumber" => billNumber > 0 ? $"{Tr.T("Bill #:")} {billNumber}" : "",
        "PaymentNumber" => paymentNumber > 0 ? $"{Tr.T("Payment #:")} {paymentNumber}" : "",
        "Products" => string.Join(Environment.NewLine, lines.Select(line =>
        {
            var text = (line.Seat > 0 ? $"[S{line.Seat}] " : "") + CourseTag(line.Course) + $"{line.Quantity} x {line.Product}";
            var extras = string.Join(", ", new[] { !string.IsNullOrWhiteSpace(line.Modifiers) ? "+ " + line.Modifiers : "", !string.IsNullOrWhiteSpace(line.Note) ? line.Note : "" }.Where(s => s.Length > 0));
            if (extras.Length > 0) text += $" ({extras})";
            return text;
        })),
        "Prices" => string.Join(Environment.NewLine, lines.Select(line => (line.UnitPrice * line.Quantity).ToString("C"))),
        "Items" => FormatItems(lines),
        "OrderType" => $"{Tr.T("Order type:")} {orderType}",
        "Total" => Tr.T("TOTAL") + "  " + (lines.Sum(line => line.UnitPrice * line.Quantity) - discountAmount).ToString("C"),
        "Tendered" => Tendered > 0 ? $"{Tr.T("TENDERED")}  {Tendered:C}   {Tr.T("CHANGE")}  {Tendered - CashReceived:C}" : "",
        "Rounded" => Rounding != 0 ? $"{Tr.T("ROUNDING")}  {(Rounding > 0 ? "+" : "")}{Rounding:C}" : "",
        "DiscountPercent" => discountPercent > 0 ? $"{Tr.T("DISCOUNT")}  -{discountPercent:0.#}%" : discountAmount > 0 ? $"{Tr.T("DISCOUNT")}  -{discountAmount:C}" : "",
        "PriceAfterDiscount" => Tr.T("AFTER DISCOUNT") + "  " + (lines.Sum(line => line.UnitPrice * line.Quantity) - discountAmount).ToString("C"),
        "TaxBreakdown" => FormatTaxBreakdown(lines.Sum(line => line.UnitPrice * line.Quantity) - discountAmount, TvaRate),
        "BarcodeFooter" => $"*B{(billNumber > 0 ? billNumber : (paymentNumber > 0 ? paymentNumber : 1)):D6}*",
        "QrFooter" => $"[ QR CODE • TICKET #{(billNumber > 0 ? billNumber : (orderNumber > 0 ? orderNumber : 1))} ]",
        "Footer" => destination.Footer,
        "Separator" => "────────────────────────────────",
        "DoubleSeparator" => "════════════════════════════════",
        "DottedSeparator" => "· · · · · · · · · · · · · · · · ·",
        "Logo" => "",
        _ => string.Empty
    };
}

internal sealed record TicketDragData(object Component, bool FromLayout, int SourceIndex);

internal sealed class TicketDesignerForm : SettingsSubForm
{
    private readonly ListBox available = new();
    private readonly ListBox layout = new();
    private readonly Panel stylePanel = new();
    private readonly ComboBox fontSize = new();
    private readonly CheckBox bold = new();
    private readonly CheckBox underline = new();
    private readonly ComboBox alignment = new();
    private readonly TextBox preview = new();
    private readonly Action<List<TicketComponentStyle>, string>? saveSingle;
    private readonly Action<List<PrinterDestination>>? saveAll;
    private readonly List<PrinterDestination> allDestinations = [];
    private int currentDestIndex = 0;
    private PrinterDestination destination;
    private string logoPath = "";
    private readonly Panel logoSection = new();
    private readonly Label logoPathLabel = new();
    private readonly ComboBox destPicker = new();
    private Point dragStart;
    private int dragIndex = -1;
    private bool updatingUi = false;
    private static readonly (string Name, int Size)[] FontSizes = [("8", 8), ("9", 9), ("10", 10), ("12", 12), ("14", 14), ("16", 16), ("18", 18), ("22", 22), ("26", 26), ("32", 32)];

    public TicketDesignerForm(Color background, Color panelLight, Color mint, PrinterDestination destination, Action<List<TicketComponentStyle>, string> save)
        : this(background, panelLight, mint, [destination], 0, null, save)
    {
    }

    public TicketDesignerForm(Color background, Color panelLight, Color mint, List<PrinterDestination> destinations, int initialIndex, Action<List<PrinterDestination>>? saveAll, Action<List<TicketComponentStyle>, string>? saveSingle = null)
    {
        this.allDestinations = destinations != null && destinations.Count > 0 ? destinations.ToList() : [new("Default", "", "RECEIPT", "Thank you")];
        this.currentDestIndex = Math.Max(0, Math.Min(this.allDestinations.Count - 1, initialIndex));
        this.destination = this.allDestinations[this.currentDestIndex];
        this.saveAll = saveAll;
        this.saveSingle = saveSingle;
        this.logoPath = destination.LogoPath ?? "";

        Text = Tr.T("Edit ticket layout") + " - " + destination.Name;
        Breadcrumb = "Settings / Receipts / Ticket Designer";
        ClientSize = new Size(1060, 640);
        MinimumSize = new Size(1000, 600);
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);

        var title = new Label { Text = Tr.T("Ticket Layout & Style Designer"), AutoSize = true, Font = new Font("Segoe UI Semibold", 18F), Location = new Point(24, 16) };

        // Destination picker
        var destLabel = new Label { Text = Tr.T("Printer destination:"), AutoSize = true, Location = new Point(24, 58), ForeColor = Color.FromArgb(170, 180, 195) };
        destPicker.Location = new Point(170, 54);
        destPicker.Size = new Size(200, 28);
        destPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        destPicker.BackColor = panelLight;
        destPicker.ForeColor = Color.White;
        foreach (var d in this.allDestinations) destPicker.Items.Add(d.Name);
        if (destPicker.Items.Count > currentDestIndex) destPicker.SelectedIndex = currentDestIndex;
        destPicker.SelectedIndexChanged += (s, e) =>
        {
            if (destPicker.SelectedIndex >= 0 && destPicker.SelectedIndex < this.allDestinations.Count)
            {
                SaveCurrentDestinationInMemory();
                currentDestIndex = destPicker.SelectedIndex;
                destination = this.allDestinations[currentDestIndex];
                logoPath = destination.LogoPath ?? "";
                ReloadLayout();
            }
        };

        // Preset buttons toolbar
        var presetPanel = new FlowLayoutPanel
        {
            Location = new Point(390, 52),
            Size = new Size(640, 34),
            BackColor = Color.Transparent,
            WrapContents = false
        };
        Button MakePresetBtn(string label, Func<List<TicketComponentStyle>> presetFunc)
        {
            var btn = new Button
            {
                Text = label,
                Height = 30,
                AutoSize = true,
                BackColor = panelLight,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (_, _) =>
            {
                layout.Items.Clear();
                foreach (var item in presetFunc()) layout.Items.Add(item);
                UpdatePreview();
            };
            return btn;
        }

        presetPanel.Controls.Add(MakePresetBtn("🛒 " + Tr.T("Supermarché"), TicketLayout.PresetSupermarket));
        presetPanel.Controls.Add(MakePresetBtn("🍽 " + Tr.T("Restaurant"), TicketLayout.PresetRestaurant));
        presetPanel.Controls.Add(MakePresetBtn("🍕 " + Tr.T("Fast-Food"), TicketLayout.PresetFastFood));
        presetPanel.Controls.Add(MakePresetBtn("👗 " + Tr.T("Boutique"), TicketLayout.PresetBoutique));
        presetPanel.Controls.Add(MakePresetBtn("💊 " + Tr.T("Pharmacie"), TicketLayout.PresetPharmacy));
        presetPanel.Controls.Add(MakePresetBtn("✨ " + Tr.T("Défaut"), TicketLayout.PresetDefault));

        // Available items column
        var availLabel = new Label { Text = Tr.T("Available components"), Location = new Point(24, 94), AutoSize = true, Font = new Font("Segoe UI Semibold", 10F) };
        available.Location = new Point(24, 118);
        available.Size = new Size(180, 390);
        available.BackColor = panelLight;
        available.ForeColor = Color.White;
        available.BorderStyle = BorderStyle.None;
        foreach (var item in TicketLayout.Available) available.Items.Add(item);

        var addBtn = new Button
        {
            Text = "➕ " + Tr.T("Add component"),
            Location = new Point(24, 514),
            Size = new Size(180, 32),
            BackColor = panelLight,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        addBtn.FlatAppearance.BorderSize = 0;
        addBtn.Click += (_, _) =>
        {
            if (available.SelectedItem is string compName)
            {
                var newStyle = new TicketComponentStyle(compName);
                int targetIdx = layout.SelectedIndex >= 0 ? layout.SelectedIndex + 1 : layout.Items.Count;
                layout.Items.Insert(targetIdx, newStyle);
                layout.SelectedIndex = targetIdx;
                UpdatePreview();
            }
        };

        // Layout items column
        var layoutLabel = new Label { Text = Tr.T("Ticket structure (print order)"), Location = new Point(216, 94), AutoSize = true, Font = new Font("Segoe UI Semibold", 10F) };
        layout.Location = new Point(216, 118);
        layout.Size = new Size(210, 390);
        layout.BackColor = panelLight;
        layout.ForeColor = Color.White;
        layout.BorderStyle = BorderStyle.None;
        layout.AllowDrop = true;
        foreach (var item in TicketLayout.Components(destination)) layout.Items.Add(item);

        // Move Up, Down, Delete buttons
        var moveUpBtn = new Button { Text = "▲ " + Tr.T("Move up"), Location = new Point(216, 514), Size = new Size(66, 32), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        moveUpBtn.FlatAppearance.BorderSize = 0;
        moveUpBtn.Click += (_, _) => MoveSelectedItem(-1);

        var moveDownBtn = new Button { Text = "▼ " + Tr.T("Move down"), Location = new Point(288, 514), Size = new Size(66, 32), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        moveDownBtn.FlatAppearance.BorderSize = 0;
        moveDownBtn.Click += (_, _) => MoveSelectedItem(1);

        var removeBtn = new Button { Text = "✖", Location = new Point(360, 514), Size = new Size(66, 32), BackColor = Color.FromArgb(180, 50, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        removeBtn.FlatAppearance.BorderSize = 0;
        removeBtn.Click += (_, _) =>
        {
            if (layout.SelectedIndex >= 0)
            {
                int oldIdx = layout.SelectedIndex;
                layout.Items.RemoveAt(oldIdx);
                if (layout.Items.Count > 0) layout.SelectedIndex = Math.Min(oldIdx, layout.Items.Count - 1);
                UpdatePreview();
            }
        };

        // Style Panel
        stylePanel.Location = new Point(438, 118);
        stylePanel.Size = new Size(200, 390);
        stylePanel.BackColor = panelLight;
        stylePanel.Visible = false;

        var styleTitle = new Label { Text = Tr.T("Formatting"), Font = new Font("Segoe UI Semibold", 12F), Location = new Point(14, 12), AutoSize = true };
        var fontLabel = new Label { Text = Tr.T("Font size / Height:"), Location = new Point(14, 44), AutoSize = true, ForeColor = Color.FromArgb(170, 180, 195) };

        fontSize.Location = new Point(14, 66);
        fontSize.Size = new Size(172, 28);
        fontSize.DropDownStyle = ComboBoxStyle.DropDownList;
        fontSize.BackColor = panelLight;
        fontSize.ForeColor = Color.White;
        foreach (var s in FontSizes) fontSize.Items.Add(s.Name);

        bold.Text = Tr.T("Bold (gras)");
        bold.Location = new Point(14, 104);
        bold.AutoSize = true;

        underline.Text = Tr.T("Underline (souligné)");
        underline.Location = new Point(14, 132);
        underline.AutoSize = true;

        var alignLabel = new Label { Text = Tr.T("Alignment:"), Location = new Point(14, 168), AutoSize = true, ForeColor = Color.FromArgb(170, 180, 195) };
        alignment.Location = new Point(14, 190);
        alignment.Size = new Size(172, 28);
        alignment.DropDownStyle = ComboBoxStyle.DropDownList;
        alignment.BackColor = panelLight;
        alignment.ForeColor = Color.White;
        alignment.Items.AddRange(["Left", "Center", "Right"]);

        fontSize.SelectedIndexChanged += OnStyleChanged;
        bold.CheckedChanged += OnStyleChanged;
        underline.CheckedChanged += OnStyleChanged;
        alignment.SelectedIndexChanged += OnStyleChanged;

        // Logo section
        logoSection.Location = new Point(14, 230);
        logoSection.Size = new Size(172, 140);
        logoSection.Visible = false;

        var logoLabel = new Label { Text = Tr.T("Logo image"), AutoSize = true, Location = new Point(0, 0), Font = new Font("Segoe UI Semibold", 9.5F) };
        logoPathLabel.Text = string.IsNullOrEmpty(logoPath) ? Tr.T("No logo set") : Path.GetFileName(logoPath);
        logoPathLabel.Location = new Point(0, 22);
        logoPathLabel.Size = new Size(172, 20);
        logoPathLabel.ForeColor = Color.FromArgb(160, 170, 185);

        var browseBtn = new Button { Text = Tr.T("Browse..."), Location = new Point(0, 48), Size = new Size(82, 28), BackColor = mint, ForeColor = Color.FromArgb(20, 30, 40), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        browseBtn.FlatAppearance.BorderSize = 0;
        browseBtn.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                logoPath = dlg.FileName;
                logoPathLabel.Text = Path.GetFileName(logoPath);
                UpdatePreview();
            }
        };

        var removeLogoBtn = new Button { Text = Tr.T("Remove"), Location = new Point(88, 48), Size = new Size(84, 28), BackColor = Color.FromArgb(50, 60, 75), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        removeLogoBtn.FlatAppearance.BorderSize = 0;
        removeLogoBtn.Click += (_, _) => { logoPath = ""; logoPathLabel.Text = Tr.T("No logo set"); UpdatePreview(); };
        logoSection.Controls.AddRange([logoLabel, logoPathLabel, browseBtn, removeLogoBtn]);

        stylePanel.Controls.AddRange([styleTitle, fontLabel, fontSize, bold, underline, alignLabel, alignment, logoSection]);

        layout.SelectedIndexChanged += (_, _) => LoadStyle();
        layout.MouseDown += (_, args) => { dragStart = args.Location; dragIndex = layout.IndexFromPoint(args.Location); };
        layout.MouseMove += (_, args) =>
        {
            if (args.Button == MouseButtons.Left && dragIndex >= 0 && IsDragging(dragStart, args.Location) && layout.Items[dragIndex] is TicketComponentStyle item)
            {
                layout.DoDragDrop(new TicketDragData(item, true, dragIndex), DragDropEffects.Move);
                dragIndex = -1;
            }
        };
        available.MouseDown += (_, args) => { dragStart = args.Location; dragIndex = available.IndexFromPoint(args.Location); };
        available.MouseMove += (_, args) =>
        {
            if (args.Button == MouseButtons.Left && dragIndex >= 0 && IsDragging(dragStart, args.Location) && available.Items[dragIndex] is string item)
            {
                available.DoDragDrop(new TicketDragData(item, false, -1), DragDropEffects.Copy);
                dragIndex = -1;
            }
        };

        layout.DragEnter += (_, args) => SetDropEffect(args);
        layout.DragOver += (_, args) => SetDropEffect(args);
        layout.DragDrop += (_, args) => HandleDrop(args);
        layout.DoubleClick += (_, _) =>
        {
            if (layout.SelectedIndex >= 0)
            {
                layout.Items.RemoveAt(layout.SelectedIndex);
                UpdatePreview();
            }
        };

        // Live Paper Preview
        var previewLabel = new Label { Text = "📄 " + Tr.T("Live thermal paper preview"), Location = new Point(650, 94), AutoSize = true, Font = new Font("Segoe UI Semibold", 10F) };

        preview.Location = new Point(650, 118);
        preview.Size = new Size(380, 428);
        preview.BackColor = Color.White;
        preview.ForeColor = Color.Black;
        preview.Font = new Font("Consolas", 9.5F);
        preview.Multiline = true;
        preview.ReadOnly = true;
        preview.ScrollBars = ScrollBars.Vertical;
        preview.BorderStyle = BorderStyle.FixedSingle;

        // Save layout button
        var saveButton = new Button
        {
            Text = "💾 " + Tr.T("Save layout"),
            Location = new Point(870, 560),
            Size = new Size(160, 40),
            BackColor = mint,
            ForeColor = Color.FromArgb(20, 30, 40),
            Font = new Font("Segoe UI Semibold", 11F),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        saveButton.FlatAppearance.BorderSize = 0;
        saveButton.Click += (_, _) =>
        {
            CommitAndSave();
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.AddRange([title, destLabel, destPicker, presetPanel, availLabel, available, addBtn, layoutLabel, layout, moveUpBtn, moveDownBtn, removeBtn, stylePanel, previewLabel, preview, saveButton]);

        doneBtn.Click += (_, _) =>
        {
            CommitAndSave();
            DialogResult = DialogResult.OK;
            Close();
        };

        ShiftBodyDown(72);
        UpdatePreview();
    }

    private void MoveSelectedItem(int direction)
    {
        int idx = layout.SelectedIndex;
        if (idx < 0) return;
        int target = idx + direction;
        if (target < 0 || target >= layout.Items.Count) return;
        var item = layout.Items[idx];
        layout.Items.RemoveAt(idx);
        layout.Items.Insert(target, item);
        layout.SelectedIndex = target;
        UpdatePreview();
    }

    private void SaveCurrentDestinationInMemory()
    {
        if (currentDestIndex >= 0 && currentDestIndex < allDestinations.Count)
        {
            var cur = allDestinations[currentDestIndex];
            allDestinations[currentDestIndex] = cur with
            {
                Layout = layout.Items.Cast<TicketComponentStyle>().ToList(),
                LogoPath = logoPath
            };
        }
    }

    private void ReloadLayout()
    {
        layout.Items.Clear();
        foreach (var item in TicketLayout.Components(destination)) layout.Items.Add(item);
        logoPathLabel.Text = string.IsNullOrEmpty(logoPath) ? Tr.T("No logo set") : Path.GetFileName(logoPath);
        stylePanel.Visible = false;
        UpdatePreview();
    }

    private void CommitAndSave()
    {
        SaveCurrentDestinationInMemory();
        var currentComponents = layout.Items.Cast<TicketComponentStyle>().ToList();
        saveSingle?.Invoke(currentComponents, logoPath);
        saveAll?.Invoke(allDestinations);
    }

    private void LoadStyle()
    {
        if (layout.SelectedItem is not TicketComponentStyle item) { stylePanel.Visible = false; return; }
        updatingUi = true;
        stylePanel.Visible = true;
        logoSection.Visible = item.Component == "Logo";
        var sizeIndex = Array.FindIndex(FontSizes, s => s.Size == item.FontSize);
        fontSize.SelectedIndex = sizeIndex >= 0 ? sizeIndex : 2;
        bold.Checked = item.Bold;
        underline.Checked = item.Underline;
        alignment.SelectedItem = item.Alignment;
        updatingUi = false;
    }

    private void OnStyleChanged(object? sender, EventArgs e)
    {
        if (updatingUi || layout.SelectedIndex < 0) return;
        var current = (TicketComponentStyle)layout.Items[layout.SelectedIndex];
        var sizeValue = fontSize.SelectedIndex >= 0 ? FontSizes[fontSize.SelectedIndex].Size : 10;
        var updated = current with { 
            FontSize = sizeValue, 
            Bold = bold.Checked, 
            Underline = underline.Checked, 
            Alignment = alignment.SelectedItem?.ToString() ?? "Left" 
        };
        layout.Items[layout.SelectedIndex] = updated;
        UpdatePreview();
    }

    private void HandleDrop(DragEventArgs args)
    {
        if (args.Data?.GetData(typeof(TicketDragData)) is not TicketDragData data) return;
        var point = layout.PointToClient(new Point(args.X, args.Y));
        var targetIndex = layout.IndexFromPoint(point);
        if (targetIndex < 0) targetIndex = layout.Items.Count;

        object componentToAdd = data.Component;
        if (data.FromLayout)
        {
            if (data.SourceIndex < 0 || data.SourceIndex >= layout.Items.Count) return;
            layout.Items.RemoveAt(data.SourceIndex);
            if (data.SourceIndex < targetIndex) targetIndex--;
        }
        else if (data.Component is string name)
        {
            componentToAdd = new TicketComponentStyle(name);
        }

        targetIndex = Math.Max(0, Math.Min(layout.Items.Count, targetIndex));
        layout.Items.Insert(targetIndex, componentToAdd);
        layout.SelectedIndex = targetIndex;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var sampleLines = new List<SoldLine>
        {
            new("Café Expresso", "Boissons", 1, 2.50m, "Bar"),
            new("Menu Burger Double", "Food", 1, 14.90m, "Mains", "", "", "Sauce Barbecue, Sans Oignon"),
            new("Pain au Chocolat", "Viennoiserie", 2, 1.60m, "Boulangerie")
        };
        TicketLayout.Covers = 2;
        TicketLayout.TvaRate = 10m;
        TicketLayout.Tendered = 30.00m;
        TicketLayout.CashReceived = 20.60m;
        TicketLayout.Rounding = 0.00m;

        var sb = new StringBuilder();
        int paperWidth = 36;

        foreach (TicketComponentStyle style in layout.Items)
        {
            if (style.Component == "Blank line")
            {
                sb.AppendLine();
            }
            else if (style.Component == "Cutter")
            {
                sb.AppendLine("─ ─ ─ ─ ─ ─ ─ ✂ CUT ✂ ─ ─ ─ ─ ─ ─ ─");
            }
            else if (style.Component == "Separator")
            {
                sb.AppendLine(new string('─', paperWidth));
            }
            else if (style.Component == "DoubleSeparator")
            {
                sb.AppendLine(new string('═', paperWidth));
            }
            else if (style.Component == "DottedSeparator")
            {
                sb.AppendLine("· · · · · · · · · · · · · · · · · ·");
            }
            else if (style.Component == "Logo")
            {
                var logoText = !string.IsNullOrEmpty(logoPath) ? $"[ LOGO: {Path.GetFileName(logoPath)} ]" : "[ LOGO - Vectron POS ]";
                sb.AppendLine(PadAlign(logoText, style.Alignment, paperWidth));
            }
            else
            {
                var txt = TicketLayout.Text(style.Component, destination, "Salle 1", "Table 4", "Caisse 01", sampleLines, 42, 108, 256, "Sur place", 0, 0);
                if (!string.IsNullOrWhiteSpace(txt))
                {
                    foreach (var line in txt.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                    {
                        if (style.Alignment == "Center" || style.Alignment == "Right")
                        {
                            sb.AppendLine(PadAlign(line, style.Alignment, paperWidth));
                        }
                        else
                        {
                            sb.AppendLine(line);
                        }
                    }
                }
            }
        }
        preview.Text = sb.ToString();
    }

    private static string PadAlign(string text, string alignment, int width)
    {
        if (text.Length >= width) return text;
        if (alignment == "Center")
        {
            int pad = Math.Max(0, (width - text.Length) / 2);
            return new string(' ', pad) + text;
        }
        if (alignment == "Right")
        {
            int pad = Math.Max(0, width - text.Length);
            return new string(' ', pad) + text;
        }
        return text;
    }

    private void SetDropEffect(DragEventArgs args) => args.Effect = args.Data?.GetData(typeof(TicketDragData)) is TicketDragData d ? (d.FromLayout ? DragDropEffects.Move : DragDropEffects.Copy) : DragDropEffects.None;
    private static bool IsDragging(Point start, Point current) => Math.Abs(current.X - start.X) >= SystemInformation.DragSize.Width / 2 || Math.Abs(current.Y - start.Y) >= SystemInformation.DragSize.Height / 2;

    protected override void Back()
    {
        CommitAndSave();
        base.Back();
    }
}
