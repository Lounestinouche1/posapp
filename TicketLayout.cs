using System.Text;
using System.IO;

namespace VectronPOS;

internal static class TicketLayout
{
    public static readonly string[] Available = ["Header", "Logo", "OrderNumber", "BillNumber", "PaymentNumber", "Operator", "Room", "Table", "Date", "Time", "Products", "Prices", "Items", "OrderType", "Total", "Footer", "Separator", "Blank line", "Cutter"];
    public static List<TicketComponentStyle> Components(PrinterDestination destination) => 
        destination.Layout is { Count: > 0 } ? destination.Layout : 
        [
            new("Header", 12, true, false, "Center"),
            new("Separator"),
            new("Room"),
            new("Table"),
            new("Separator"),
            new("Operator"),
            new("Date"),
            new("Time"),
            new("Separator"),
            new("Products"),
            new("Prices"),
            new("Separator"),
            new("Total", 14, true, false, "Right"),
            new("Separator"),
            new("Footer", 10, false, false, "Center"),
            new("Blank line"),
            new("Cutter")
        ];

    public static string Text(string component, PrinterDestination destination, string room, string table, string operatorName, List<SoldLine> lines, int orderNumber = 0, int billNumber = 0, int paymentNumber = 0, string orderType = "") => component switch
    {
        "Header" => destination.Header,
        "Operator" => "Operator: " + operatorName,
        "Room" => "Room: " + room,
        "Table" => "Table: " + table,
        "Date" => DateTime.Now.ToString("dd/MM/yyyy"),
        "Time" => DateTime.Now.ToString("HH:mm:ss"),
        "OrderNumber" => orderNumber > 0 ? $"Order #: {orderNumber}" : "",
        "BillNumber" => billNumber > 0 ? $"Bill #: {billNumber}" : "",
        "PaymentNumber" => paymentNumber > 0 ? $"Payment #: {paymentNumber}" : "",
        "Products" => string.Join(Environment.NewLine, lines.Select(line =>
        {
            var text = $"{line.Quantity} x {line.Product}";
            if (!string.IsNullOrWhiteSpace(line.Note)) text += $" ({line.Note})";
            return text;
        })),
        "Prices" => string.Join(Environment.NewLine, lines.Select(line => (line.UnitPrice * line.Quantity).ToString("C"))),
        "Items" => string.Join(Environment.NewLine, lines.Select(line => $"{line.Quantity} x {line.Product}  {line.UnitPrice * line.Quantity:C}")),
        "OrderType" => $"Order type: {orderType}",
        "Total" => "TOTAL  " + lines.Sum(line => line.UnitPrice * line.Quantity).ToString("C"),
        "Footer" => destination.Footer,
        "Separator" => "------------------------------",
        "Logo" => "",
        _ => string.Empty
    };
}

internal sealed record TicketDragData(object Component, bool FromLayout, int SourceIndex);

internal sealed class TicketDesignerForm : Form
{
    private readonly ListBox available = new();
    private readonly ListBox layout = new();
    private readonly Panel stylePanel = new();
    private readonly ComboBox fontSize = new();
    private readonly CheckBox bold = new();
    private readonly CheckBox underline = new();
    private readonly ComboBox alignment = new();
    private readonly Label preview = new();
    private readonly Action<List<TicketComponentStyle>, string> save;
    private readonly PrinterDestination destination;
    private string logoPath;
    private readonly Panel logoSection = new();
    private readonly Label logoPathLabel = new();
    private Point dragStart;
    private int dragIndex = -1;
    private bool updatingUi = false;
    private static readonly (string Name, int Size)[] FontSizes = [("8", 8), ("10", 10), ("12", 12), ("15", 15), ("18", 18), ("22", 22), ("26", 26), ("32", 32)];

    public TicketDesignerForm(Color background, Color panelLight, Color mint, PrinterDestination destination, Action<List<TicketComponentStyle>, string> save)
    {
        this.destination = destination;
        this.save = save;
        this.logoPath = destination.LogoPath;
        Text = "Edit ticket layout - " + destination.Name;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1000, 600);
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);

        var title = new Label { Text = "Ticket layout: " + destination.Name, AutoSize = true, Font = new Font("Segoe UI Semibold", 18F), Location = new Point(24, 20) };
        
        available.Location = new Point(24, 80); available.Size = new Size(180, 400); available.BackColor = panelLight; available.ForeColor = Color.White; available.BorderStyle = BorderStyle.None;
        foreach (var item in TicketLayout.Available) available.Items.Add(item);
        
        layout.Location = new Point(220, 80); layout.Size = new Size(220, 400); layout.BackColor = panelLight; layout.ForeColor = Color.White; layout.BorderStyle = BorderStyle.None; layout.AllowDrop = true;
        foreach (var item in TicketLayout.Components(destination)) layout.Items.Add(item);
        
        // Style Panel (same panel for ALL components including Logo)
        stylePanel.Location = new Point(455, 80); stylePanel.Size = new Size(200, 400); stylePanel.BackColor = panelLight; stylePanel.Visible = false;
        var styleTitle = new Label { Text = "Formatting", Font = new Font("Segoe UI Semibold", 12F), Location = new Point(15, 15), AutoSize = true };
        
        fontSize.Location = new Point(15, 65); fontSize.Size = new Size(170, 30); fontSize.DropDownStyle = ComboBoxStyle.DropDownList;
        fontSize.BackColor = panelLight; fontSize.ForeColor = Color.White;
        foreach (var s in FontSizes) fontSize.Items.Add(s.Name);
        bold.Text = "Bold"; bold.Location = new Point(15, 105); bold.AutoSize = true;
        underline.Text = "Underline"; underline.Location = new Point(15, 135); underline.AutoSize = true;
        alignment.Location = new Point(15, 185); alignment.Size = new Size(170, 30); alignment.DropDownStyle = ComboBoxStyle.DropDownList;
        alignment.BackColor = panelLight; alignment.ForeColor = Color.White;
        alignment.Items.AddRange(["Left", "Center", "Right"]);

        fontSize.SelectedIndexChanged += OnStyleChanged;
        bold.CheckedChanged += OnStyleChanged;
        underline.CheckedChanged += OnStyleChanged;
        alignment.SelectedIndexChanged += OnStyleChanged;
        
        // Logo browse section (shown below formatting when Logo is selected)
        logoSection.Location = new Point(15, 225); logoSection.Size = new Size(170, 100); logoSection.Visible = false;
        var logoLabel = new Label { Text = "Logo image", AutoSize = true, Location = new Point(0, 0) };
        logoPathLabel.Text = string.IsNullOrEmpty(logoPath) ? "No logo set" : Path.GetFileName(logoPath);
        logoPathLabel.Location = new Point(0, 25); logoPathLabel.Size = new Size(170, 20); logoPathLabel.ForeColor = Color.Gray;
        var browseBtn = new Button { Text = "Browse...", Location = new Point(0, 50), Size = new Size(80, 28), BackColor = Color.FromArgb(71, 214, 174), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
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
        var removeBtn = new Button { Text = "Remove", Location = new Point(85, 50), Size = new Size(80, 28), BackColor = Color.FromArgb(60, 60, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        removeBtn.Click += (_, _) => { logoPath = ""; logoPathLabel.Text = "No logo set"; UpdatePreview(); };
        logoSection.Controls.AddRange([logoLabel, logoPathLabel, browseBtn, removeBtn]);
        
        stylePanel.Controls.AddRange([styleTitle, new Label { Text = "Font size (logo height)", Location = new Point(15, 45), AutoSize = true }, fontSize, bold, underline, new Label { Text = "Alignment", Location = new Point(15, 165), AutoSize = true }, alignment, logoSection]);

        layout.SelectedIndexChanged += (_, _) => LoadStyle();
        layout.MouseDown += (_, args) => { dragStart = args.Location; dragIndex = layout.IndexFromPoint(args.Location); };
        layout.MouseMove += (_, args) => {
            if (args.Button == MouseButtons.Left && dragIndex >= 0 && IsDragging(dragStart, args.Location) && layout.Items[dragIndex] is TicketComponentStyle item)
            {
                layout.DoDragDrop(new TicketDragData(item, true, dragIndex), DragDropEffects.Move);
                dragIndex = -1;
            }
        };
        available.MouseDown += (_, args) => { dragStart = args.Location; dragIndex = available.IndexFromPoint(args.Location); };
        available.MouseMove += (_, args) => {
            if (args.Button == MouseButtons.Left && dragIndex >= 0 && IsDragging(dragStart, args.Location) && available.Items[dragIndex] is string item)
            {
                available.DoDragDrop(new TicketDragData(item, false, -1), DragDropEffects.Copy);
                dragIndex = -1;
            }
        };

        layout.DragEnter += (_, args) => SetDropEffect(args);
        layout.DragOver += (_, args) => SetDropEffect(args);
        layout.DragDrop += (_, args) => HandleDrop(args);
        layout.DoubleClick += (_, _) => { if (layout.SelectedIndex >= 0) { layout.Items.RemoveAt(layout.SelectedIndex); UpdatePreview(); } };

        preview.Location = new Point(670, 80); preview.Size = new Size(300, 400); preview.BackColor = Color.White; preview.ForeColor = Color.Black; preview.Font = new Font("Consolas", 9F);
        
        var saveButton = Button("Save layout", 840, 500, 130, mint);
        saveButton.ForeColor = background;
        saveButton.Click += (_, _) => { save(layout.Items.Cast<TicketComponentStyle>().ToList(), logoPath); DialogResult = DialogResult.OK; Close(); };

        Controls.AddRange([title, available, layout, stylePanel, preview, saveButton]);
        UpdatePreview();
    }

    private void LoadStyle()
    {
        if (layout.SelectedItem is not TicketComponentStyle item) { stylePanel.Visible = false; return; }
        updatingUi = true;
        stylePanel.Visible = true;
        logoSection.Visible = item.Component == "Logo";
        var sizeIndex = Array.FindIndex(FontSizes, s => s.Size == item.FontSize);
        fontSize.SelectedIndex = sizeIndex >= 0 ? sizeIndex : 0;
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

        targetIndex = Math.Clamp(targetIndex, 0, layout.Items.Count);
        layout.Items.Insert(targetIndex, componentToAdd);
        layout.SelectedIndex = targetIndex;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        preview.Text = "PREVIEW (Fixed font only)";
        var sample = new List<SoldLine> { new("Margherita", "Food", 1, 12.50m, "Mains") };
        var sb = new StringBuilder();
        foreach (TicketComponentStyle style in layout.Items)
        {
            if (style.Component == "Blank line") sb.AppendLine();
            else if (style.Component == "Cutter") sb.AppendLine("──────── CUT ────────");
            else if (style.Component == "Separator") sb.AppendLine("------------------------------");
            else if (style.Component == "Logo")
            {
                var logoText = !string.IsNullOrEmpty(logoPath) ? $"[ LOGO: {Path.GetFileName(logoPath)} ]" : "[ LOGO - not set ]";
                sb.AppendLine($"[{style.Alignment.ToUpper()[0]}] {logoText}");
            }
            else sb.AppendLine($"[{style.Alignment.ToUpper()[0]}] {TicketLayout.Text(style.Component, destination, "Room 1", "Table 5", "Manager", sample, 1, 1, 1, "Dine-in")}");
        }
        preview.Text = sb.ToString();
    }

    private void SetDropEffect(DragEventArgs args) => args.Effect = args.Data?.GetData(typeof(TicketDragData)) is TicketDragData d ? (d.FromLayout ? DragDropEffects.Move : DragDropEffects.Copy) : DragDropEffects.None;
    private Button Button(string text, int x, int y, int width, Color color) => new() { Text = text, Location = new Point(x, y), Size = new Size(width, 38), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
    private static bool IsDragging(Point start, Point current) => Math.Abs(current.X - start.X) >= SystemInformation.DragSize.Width / 2 || Math.Abs(current.Y - start.Y) >= SystemInformation.DragSize.Height / 2;
}