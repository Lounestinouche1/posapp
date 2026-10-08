using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VectronPOS;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        OnScreenKeyboard.Register();
        Application.Run(new PosForm());
    }
}

internal sealed record Product(string Name, string Category, decimal Price, string Color, string Family = "General", string ImagePath = "", string PrinterDestination = "", string Barcode = "", bool Weighted = false);
internal sealed record SoldLine(string Product, string Family, int Quantity, decimal UnitPrice, string Category = "General", string PrinterDestination = "", string Note = "", string Modifiers = "", decimal Discount = 0, string DiscountType = "", int Course = 0, int Seat = 0);
    internal sealed record Sale(DateTime Time, decimal Total, string Table, string Operator = "Unknown", List<SoldLine>? Items = null, int PaymentNumber = 0, int BillNumber = 0, decimal Tip = 0, string PaymentType = "Cash", string CustomerName = "", string OrderType = "Dine-in", decimal Discount = 0, bool IsRefund = false, int RefundOf = 0, string DeliveryAddress = "", decimal CashAmount = -1, decimal Tendered = 0, decimal Rounding = 0, decimal ServiceFee = 0, decimal CashDiscountAmt = 0, decimal SurchargeAmt = 0, string Branch = "", string Device = "");
internal sealed record OpenOrder(string Room, string Table, string Operator, DateTime Created, List<SoldLine> Items, string Note = "", int OrderNumber = 0, int BillNumber = 0, string OrderType = "Dine-in", string CustomerName = "", int SeatCount = 0, bool Ready = false, int FiredCourse = 0, string Source = "", string DeliveryAddress = "");
internal sealed record Room(string Name, int TableCount);
    internal sealed record ProductFamily(string Name, string ImagePath = "", string PrinterDestination = "");
internal sealed record OperatorAccount(string Name, List<string> Permissions, string Password = "", decimal Wage = 0);
internal sealed record ModifierGroup(string Name, List<ModifierOption> Options);
internal sealed record ModifierOption(string Name, decimal Price);
    internal sealed record Customer(string Name, string Phone = "", string Email = "", int Points = 0, decimal Balance = 0, decimal TabDue = 0, decimal CreditLimit = 0);
internal sealed record Ingredient(string Name, string Unit, decimal Stock, decimal MinStock, decimal Cost = 0, DateTime? Expiry = null);
internal sealed record RecipeItem(string Ingredient, decimal Quantity);
internal sealed record ShiftRecord(string Operator, DateTime ClockIn, DateTime? ClockOut = null, decimal OpeningBalance = 0, decimal ClosingBalance = 0);
internal sealed record AuditEntry(DateTime Time, string Operator, string Action, string Detail = "");
internal sealed record HappyHourRule(string Name, TimeSpan Start, TimeSpan End, decimal DiscountPercent, List<string> Days);
internal sealed record StockAlert(string ProductName, string Ingredient, decimal Remaining, decimal MinStock);
internal sealed record RegisterSettings(string Currency, decimal TvaRate, bool PricesIncludeTva, string NetworkPath = "", bool IsMainPc = true);

internal sealed record ZReportRecord(DateTime Time, int Transactions, decimal Gross, string Operator);
internal sealed record GiftCard(string Code, decimal Balance, decimal Sold, DateTime Created);
    internal sealed record CashMovement(DateTime Time, decimal Amount, string Kind, string Reason, string Operator);
    internal sealed record FeeSettings(decimal ServicePct = 0, decimal CashDiscPct = 0, decimal SurchargePct = 0, bool TerminalOffline = false);
    internal sealed record SpecialOrder(int Id, DateTime Time, string Customer, string Table, decimal Total, decimal Paid, string Status, int PaymentNum);
    internal sealed record OfflinePayment(DateTime Time, int PaymentNumber, decimal Amount, string Table, string Operator, string Status);
    internal sealed record VtInvoice(int Id, DateTime Time, string Name, string Phone, decimal Amount, string Note, string Status);
internal sealed record Reservation(string Name, string Phone, int PartySize, DateTime When, string Room, string Table, string Status);
internal sealed record ScheduleEntry(string Operator, DateTime Date, TimeSpan Start, TimeSpan End);
internal sealed record Supplier(string Name, string Phone = "", string Email = "");
internal sealed record POLine(string Ingredient, decimal Qty, decimal Cost);
internal sealed record StockTakeLine(string Ingredient, decimal SystemQty, decimal CountedQty);
internal sealed record StockTake(int Id, DateTime When, string By, string Note, List<StockTakeLine> Lines);
internal sealed record WasteEntry(string Ingredient, decimal Qty, string Reason, DateTime When, string By);
internal sealed record StockTransfer(int Id, string From, string To, string Ingredient, decimal Qty, DateTime When, string By);
internal sealed record ProductVariant(string Product, string Name, decimal Price, string Barcode = "");
internal sealed record PurchaseOrder(int Id, string Supplier, DateTime Created, string Status, List<POLine> Lines);
internal sealed record EmailSettings(string Host, int Port, string User, string Password, string From, bool Tls);
internal sealed record ParkedOrder(string Id, string Room, string Table, string Operator, DateTime Time, List<SoldLine> Items, string Note = "");
internal sealed record ComboComponent(string Product, int Qty);
internal sealed record ComboDeal(string Name, decimal Price, List<ComboComponent> Items);
internal sealed record GuestAlertItem(string Id, string Room, string Table, string Kind, DateTime Time);

internal sealed record ReportLine(string Text, int Style);

internal static class ReportLineStyles
{
    public const int Normal = 0, Header = 1, Title = 2, Small = 3, Rule = 4, Spacer = 5;
    public static double Height(int style) => style switch { Title => 25, Header => 16, Small => 12, Rule => 9, Spacer => 8, _ => 14 };
    public static double FontSize(int style) => style switch { Title => 14, Small => 8, _ => 10 };
    public static bool IsBold(int style) => style is Title or Header;
}

internal sealed class DayReportData
{
    public bool CloseDay;
    public DateTime Time;
    public string Operator = "";
    public string Currency = "";
    public int Transactions;
    public int ItemsSold;
    public decimal Gross;
    public decimal Discounts;
    public decimal Tips;
    public List<(string Name, int Count, decimal Total)> ByPayment = [];
    public List<(string Name, int Count, decimal Total)> ByOperator = [];
    public List<(string Name, int Qty, decimal Total)> ByProduct = [];
    public List<(string Name, int Qty, decimal Total)> ByFamily = [];
}

internal static class TicketPdf
{
    private const double PageW = 595.28, PageH = 841.89, Margin = 40;

    public static void Save(string path, IReadOnlyList<ReportLine> lines)
    {
        var pages = Paginate(lines);
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [" + string.Join(" ", Enumerable.Range(0, Math.Max(pages.Count, 1)).Select(i => $"{5 + i * 2} 0 R")) + "] /Count " + Math.Max(pages.Count, 1) + " >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold /Encoding /WinAnsiEncoding >>"
        };
        for (var i = 0; i < pages.Count; i++)
        {
            var content = BuildContent(pages[i]);
            var pageNum = 5 + i * 2;
            objects.Add(FormattableString.Invariant($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageW:0.###} {PageH:0.###}] /Resources << /Font << /FN 3 0 R /FB 4 0 R >> >> /Contents {pageNum + 1} 0 R >>"));
            objects.Add($"<< /Length {content.Length} >>\nstream\n{content}\nendstream");
        }
        if (pages.Count == 0)
        {
            var content = BuildContent([]);
            objects.Add(FormattableString.Invariant($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageW:0.###} {PageH:0.###}] /Resources << /Font << /FN 3 0 R /FB 4 0 R >> >> /Contents 6 0 R >>"));
            objects.Add($"<< /Length {content.Length} >>\nstream\n{content}\nendstream");
        }
        WriteFile(path, objects);
    }

    private static List<List<(ReportLine Line, double Top)>> Paginate(IReadOnlyList<ReportLine> lines)
    {
        var pages = new List<List<(ReportLine, double)>>();
        var page = new List<(ReportLine, double)>();
        var y = PageH - Margin;
        foreach (var line in lines)
        {
            var h = ReportLineStyles.Height(line.Style);
            if (y - h < Margin)
            {
                pages.Add(page);
                page = [];
                y = PageH - Margin;
            }
            page.Add((line, y - h));
            y -= h;
        }
        pages.Add(page);
        return pages;
    }

    private static string BuildContent(List<(ReportLine Line, double Top)> page)
    {
        var sb = new StringBuilder();
        foreach (var (line, top) in page)
        {
            if (line.Style == ReportLineStyles.Spacer) continue;
            if (line.Style == ReportLineStyles.Rule)
            {
                var ry = top + ReportLineStyles.Height(line.Style) / 2;
                sb.Append(FormattableString.Invariant($"0.6 G 0.75 w {Margin:0.##} {ry:0.##} m {PageW - Margin:0.##} {ry:0.##} l S\n"));
                continue;
            }
            var size = ReportLineStyles.FontSize(line.Style);
            var maxChars = (int)((PageW - Margin * 2) / (0.6 * size));
            var text = line.Text.Length > maxChars ? line.Text[..maxChars] : line.Text;
            var baseline = top + size;
            sb.Append(line.Style == ReportLineStyles.Small ? "0.5 g\n" : "0 g\n");
            sb.Append(FormattableString.Invariant($"BT /{(ReportLineStyles.IsBold(line.Style) ? "FB" : "FN")} {size:0.#} Tf {Margin:0.##} {baseline:0.##} Td ("));
            AppendEscaped(sb, text);
            sb.Append(") Tj ET\n");
        }
        return sb.ToString();
    }

    private static readonly Dictionary<char, byte> WinAnsiSpecials = new()
    {
        ['€'] = 0x80, ['‚'] = 0x82, ['„'] = 0x84, ['…'] = 0x85, ['†'] = 0x86, ['‡'] = 0x87,
        ['ˆ'] = 0x88, ['‰'] = 0x89, ['Š'] = 0x8A, ['‹'] = 0x8B, ['Œ'] = 0x8C, ['Ž'] = 0x8E,
        ['‘'] = 0x91, ['’'] = 0x92, ['“'] = 0x93, ['”'] = 0x94, ['•'] = 0x95, ['–'] = 0x96,
        ['—'] = 0x97, ['˜'] = 0x98, ['™'] = 0x99, ['š'] = 0x9A, ['›'] = 0x9B, ['œ'] = 0x9C,
        ['ž'] = 0x9E, ['Ÿ'] = 0x9F
    };

    private static void AppendEscaped(StringBuilder sb, string text)
    {
        foreach (var ch in text)
        {
            if (ch is '\\' or '(' or ')') { sb.Append('\\').Append(ch); continue; }
            byte b;
            if (ch < 0x80 || ch <= 0xFF) b = (byte)ch;
            else if (!WinAnsiSpecials.TryGetValue(ch, out b)) b = (byte)'?';
            if (b < 32 || b > 126) sb.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
            else sb.Append((char)b);
        }
    }

    private static void WriteFile(string path, List<string> objects)
    {
        using var ms = new MemoryStream();
        void W(string s) { var bytes = Encoding.ASCII.GetBytes(s); ms.Write(bytes, 0, bytes.Length); }
        W("%PDF-1.4\n");
        var offsets = new List<long>();
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(ms.Position);
            W($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        var xrefPos = ms.Position;
        W($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var off in offsets) W($"{off:D10} 00000 n \n");
        W($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefPos}\n%%EOF\n");
        File.WriteAllBytes(path, ms.ToArray());
    }
}

internal sealed class FeatureSettings
{
    public bool SplitBill { get; set; } = true;
    public bool TableTransfer { get; set; } = true;
    public bool Discounts { get; set; } = true;
    public bool VoidsComps { get; set; } = true;
    public bool HappyHour { get; set; } = false;
    public bool Modifiers { get; set; } = true;
    public bool ItemNotes { get; set; } = true;
    public bool ClockInOut { get; set; } = false;
    public bool CashDrawer { get; set; } = false;
    public bool ShiftReports { get; set; } = false;
    public bool CustomerDatabase { get; set; } = false;
    public bool Loyalty { get; set; } = false;
    public bool DeliveryOrders { get; set; } = false;
    public bool MultiRatePricing { get; set; } = false;
    public bool StockManagement { get; set; } = false;
    public bool CourseManagement { get; set; } = false;
    public bool Tips { get; set; } = false;
    public bool BarcodeScanning { get; set; } = false;
    public bool InventoryAlerts { get; set; } = false;
    public bool AuditLog { get; set; } = false;
    public bool EmailReceipts { get; set; } = false;
    public bool MultiLanguage { get; set; } = false;
    public bool MultiBranch { get; set; } = false;
    public bool OnScreenKeyboard { get; set; } = true;
    public bool KitchenDisplay { get; set; } = false;
    public bool EightySixBoard { get; set; } = false;
    public bool SeatOrdering { get; set; } = false;
    public bool FloorPlanEditor { get; set; } = false;
    public bool Refunds { get; set; } = false;
    public bool Scheduling { get; set; } = false;
    public bool Reservations { get; set; } = false;
    public bool GiftCards { get; set; } = false;
    public bool Purchasing { get; set; } = false;
    public bool QROrdering { get; set; } = false;
    public bool ParkOrders { get; set; } = false;
    public bool RepeatOrders { get; set; } = false;
    public bool ComboDeals { get; set; } = false;
    public bool QuickSaleMode { get; set; } = false;
    public bool KioskMode { get; set; } = false;
    public bool CustomerDisplay { get; set; } = false;
    public bool CashRounding { get; set; } = false;
    public bool HoldFire { get; set; } = false;
    public bool Auto86 { get; set; } = false;
    public bool Stocktakes { get; set; } = false;
    public bool WasteTracking { get; set; } = false;
    public bool StockTransfers { get; set; } = false;
    public bool BarcodeLabels { get; set; } = false;
    public bool WeightedItems { get; set; } = false;
    public bool ProductVariants { get; set; } = false;
    public bool ServiceCharge { get; set; } = false;
    public bool CashDiscount { get; set; } = false;
    public bool CardSurcharge { get; set; } = false;
    public bool ChargeTabs { get; set; } = false;
    public bool Layaway { get; set; } = false;
    public bool ReceiptSearch { get; set; } = false;
    public bool OfflinePayments { get; set; } = false;
    public bool VirtualTerminal { get; set; } = false;
    public bool EGiftEmail { get; set; } = false;
    public bool TrainingMode { get; set; } = false;
    public bool EmployeeDiscount { get; set; } = false;
    public decimal EmployeeDiscountPct { get; set; } = 10;
    public bool PinLogin { get; set; } = false;
    public bool PayrollExport { get; set; } = false;
    public bool ServerPerformance { get; set; } = false;
    public bool LiveDashboard { get; set; } = false;
    public bool CloseOfDay { get; set; } = false;
    public bool DiscountVoidReport { get; set; } = false;
    public bool OccupancyHeatmap { get; set; } = false;
    public bool MenuEngineering { get; set; } = false;
    public bool AccountingExport { get; set; } = false;
    public bool AiInsights { get; set; } = false;
    public bool CloudBackup { get; set; } = false;
    public string BackupPath { get; set; } = "";
    public bool BackupDaily { get; set; } = false;
    public bool ApiEnabled { get; set; } = false;
    public string ApiKey { get; set; } = "";
    public string WebhookUrl { get; set; } = "";
    public string GuestOrderUrl { get; set; } = "";
    public bool KdsLateAlarm { get; set; } = false;
    public int KdsPrepMinutes { get; set; } = 15;
    public List<string> Branches { get; set; } = [];
    public string CurrentBranch { get; set; } = "";
    public bool CloudSync { get; set; } = false;
    public string CloudSyncUrl { get; set; } = "";
    public string CloudSyncKey { get; set; } = "";
    public int CloudSyncMinutes { get; set; } = 5;
    public string CloudSyncDevice { get; set; } = "";
    public string CloudSyncLast { get; set; } = "";
    public bool CloudSyncPull { get; set; } = false;
    public string CloudSyncLastBackup { get; set; } = "";
}
internal sealed class FloorTable { public string? Name { get; set; } public int? X { get; set; } public int? Y { get; set; } }
internal sealed record TicketComponentStyle(string Component, int FontSize = 10, bool Bold = false, bool Underline = false, string Alignment = "Left")
{
    public override string ToString() => Component;
}

internal sealed class TicketComponentStyleConverter : JsonConverter<TicketComponentStyle>
{
    public override TicketComponentStyle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var name = reader.GetString() ?? "";
            return new TicketComponentStyle(name);
        }
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ref reader, options) ?? [];
            var component = dict.TryGetValue("Component", out var c) ? c.GetString() ?? "" : "";
            var fontSize = dict.TryGetValue("FontSize", out var fs) ? fs.GetInt32() : 10;
            var bold = dict.TryGetValue("Bold", out var b) ? b.GetBoolean() : false;
            var underline = dict.TryGetValue("Underline", out var u) ? u.GetBoolean() : false;
            var alignment = dict.TryGetValue("Alignment", out var a) ? a.GetString() ?? "Left" : "Left";
            return new TicketComponentStyle(component, fontSize, bold, underline, alignment);
        }
        throw new JsonException("Unexpected token for TicketComponentStyle");
    }

    public override void Write(Utf8JsonWriter writer, TicketComponentStyle value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Component", value.Component);
        writer.WriteNumber("FontSize", value.FontSize);
        writer.WriteBoolean("Bold", value.Bold);
        writer.WriteBoolean("Underline", value.Underline);
        writer.WriteString("Alignment", value.Alignment);
        writer.WriteEndObject();
    }
}

internal sealed record PrinterDestination(string Name, string WindowsPrinter, string Header = "VECTRON POS", string Footer = "Thank you", bool CutAfterPrint = false, List<TicketComponentStyle>? Layout = null, string LogoPath = "");


internal sealed class PosStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new TicketComponentStyleConverter() }
    };

    private static readonly string LocalDataFolder = Path.Combine(AppContext.BaseDirectory, "client database");
    private string dataFolder = LocalDataFolder;
    private string file => Path.Combine(dataFolder, "pos-data.json");
    private string roomsFile => Path.Combine(dataFolder, "rooms.json");
    private string operatorsFile => Path.Combine(dataFolder, "operators.json");
    private string familiesFile => Path.Combine(dataFolder, "families.json");
    private string productsFile => Path.Combine(dataFolder, "products.json");
    private string settingsFile => Path.Combine(dataFolder, "register-settings.json");
    private string featureSettingsFile => Path.Combine(dataFolder, "feature-settings.json");
    private string printersFile => Path.Combine(dataFolder, "printer-destinations.json");
    private string openOrdersFile => Path.Combine(dataFolder, "open-orders.json");
    private string modifiersFile => Path.Combine(dataFolder, "modifiers.json");
    private string customersFile => Path.Combine(dataFolder, "customers.json");
    private string ingredientsFile => Path.Combine(dataFolder, "ingredients.json");
    private string recipesFile => Path.Combine(dataFolder, "recipes.json");
    private string shiftsFile => Path.Combine(dataFolder, "shifts.json");
    private string auditFile => Path.Combine(dataFolder, "audit-log.json");
    private string happyHourFile => Path.Combine(dataFolder, "happy-hour.json");
    private string zReportsFile => Path.Combine(dataFolder, "z-reports.json");
    private string eightySixFile => Path.Combine(dataFolder, "86-board.json");
    private FileSystemWatcher? watcher;
    public event Action? DataChanged;
    private volatile bool suppressWatcher;

    public List<Sale> Sales { get; private set; } = [];
    public List<OpenOrder> OpenOrders { get; private set; } = [];
    public List<Room> Rooms { get; private set; } = [new("Main dining", 8), new("Terrace", 6), new("Private room", 4)];
    public List<ProductFamily> Families { get; private set; } = [new("General"), new("Food"), new("Drinks"), new("Desserts")];
    public List<Product> Products { get; private set; } = [
        new("Margherita", "Popular", 12.50m, "#F4B860"),
        new("Crispy Chicken", "Popular", 15.90m, "#F08A5D"),
        new("Garden Bowl", "Popular", 10.80m, "#70C1B3"),
        new("Classic Burger", "Popular", 14.50m, "#DDA15E"),
        new("Truffle Pasta", "Mains", 17.50m, "#A7C957"),
        new("Grilled Salmon", "Mains", 21.00m, "#F28482"),
        new("Steak Frites", "Mains", 24.00m, "#E07A5F"),
        new("Veggie Wrap", "Mains", 11.50m, "#81B29A"),
        new("Lemonade", "Drinks", 4.50m, "#F6BD60"),
        new("Espresso", "Drinks", 3.20m, "#9C6644"),
        new("Sparkling Water", "Drinks", 3.00m, "#5DA9E9"),
        new("House Red", "Drinks", 7.50m, "#9B2226"),
        new("Cheesecake", "Desserts", 7.00m, "#F7CAD0"),
        new("Chocolate Tart", "Desserts", 7.50m, "#8D5524"),
        new("Fruit Plate", "Desserts", 6.50m, "#90BE6D")
    ];
    public List<OperatorAccount> Operators { get; private set; } = [new("Manager", ["All permissions"]), new("Cashier", ["Take payments", "View sales"] )];
    public RegisterSettings RegisterSettings { get; private set; } = new("EUR", 20m, false);
    public FeatureSettings Features { get; private set; } = new();
    private string countersFile => Path.Combine(dataFolder, "daily-counters.json");
    private string floorPlanFile => Path.Combine(dataFolder, "floor-plan.json");
    public List<PrinterDestination> PrinterDestinations { get; private set; } = [];
    public List<ModifierGroup> ModifierGroups { get; private set; } = [];
    public List<Customer> Customers { get; private set; } = [];
    public List<Ingredient> Ingredients { get; private set; } = [];
    public Dictionary<string, List<RecipeItem>> Recipes { get; private set; } = [];
    public List<ShiftRecord> Shifts { get; private set; } = [];
    public List<AuditEntry> AuditLog { get; private set; } = [];
    public List<HappyHourRule> HappyHourRules { get; private set; } = [];
    public List<ZReportRecord> ZReports { get; private set; } = [];
    public List<string> EightySix { get; private set; } = [];
    public Dictionary<string, Dictionary<string, FloorTable>> FloorPlan { get; private set; } = [];
    public List<ParkedOrder> ParkedOrders { get; private set; } = [];
    public List<ComboDeal> ComboDeals { get; private set; } = [];
    public List<string> CourseNames { get; private set; } = [];
    public List<GuestAlertItem> GuestAlerts { get; private set; } = [];
    public event Action? GuestAlertRaised;
    public List<GiftCard> GiftCards { get; private set; } = [];
    public List<CashMovement> CashMovements { get; private set; } = [];
    public FeeSettings FeeSettings { get; private set; } = new();
    public List<SpecialOrder> SpecialOrders { get; private set; } = [];
    public List<OfflinePayment> OfflinePayments { get; private set; } = [];
    public List<VtInvoice> VtInvoices { get; private set; } = [];
    public List<Reservation> Reservations { get; private set; } = [];
    public List<ScheduleEntry> Schedule { get; private set; } = [];
    public List<Supplier> Suppliers { get; private set; } = [];
    public List<PurchaseOrder> PurchaseOrders { get; private set; } = [];
    public List<StockTake> StockTakes { get; private set; } = [];
    public List<WasteEntry> WasteLog { get; private set; } = [];
    public List<StockTransfer> StockTransfers { get; private set; } = [];
    public Dictionary<string, Dictionary<string, decimal>> LocationStock { get; private set; } = [];
    public List<string> Locations { get; private set; } = [];
    public List<ProductVariant> ProductVariants { get; private set; } = [];
    public List<string> Auto86 { get; private set; } = [];
    public EmailSettings EmailSettings { get; private set; } = new("", 587, "", "", "", true);
    private string giftCardsFile => Path.Combine(dataFolder, "gift-cards.json");
    private string cashMovementsFile => Path.Combine(dataFolder, "cash-movements.json");
    private string feeSettingsFile => Path.Combine(dataFolder, "payment-fees.json");
    private string specialOrdersFile => Path.Combine(dataFolder, "special-orders.json");
    private string offlinePaymentsFile => Path.Combine(dataFolder, "offline-payments.json");
    private string vtInvoicesFile => Path.Combine(dataFolder, "vt-invoices.json");
    private string reservationsFile => Path.Combine(dataFolder, "reservations.json");
    private string scheduleFile => Path.Combine(dataFolder, "schedule.json");
    private string suppliersFile => Path.Combine(dataFolder, "suppliers.json");
    private string purchaseOrdersFile => Path.Combine(dataFolder, "purchase-orders.json");
    private string emailSettingsFile => Path.Combine(dataFolder, "email-settings.json");
    private string courseNamesFile => Path.Combine(dataFolder, "course-names.json");
    private string parkedOrdersFile => Path.Combine(dataFolder, "parked-orders.json");
    private string comboDealsFile => Path.Combine(dataFolder, "combo-deals.json");
    private string stockTakesFile => Path.Combine(dataFolder, "stocktakes.json");
    private string wasteFile => Path.Combine(dataFolder, "waste-log.json");
    private string transfersFile => Path.Combine(dataFolder, "stock-transfers.json");
    private string locationStockFile => Path.Combine(dataFolder, "location-stock.json");
    private string locationsFile => Path.Combine(dataFolder, "locations.json");
    private string productVariantsFile => Path.Combine(dataFolder, "product-variants.json");
    private string auto86File => Path.Combine(dataFolder, "86-auto.json");
    public int OrderCounter { get; private set; }
    public int BillCounter { get; private set; }
    public int PaymentCounter { get; private set; }
    public string CounterDate { get; private set; } = "";

    public PosStore()
    {
        try
        {
            Directory.CreateDirectory(LocalDataFolder);
            if (File.Exists(settingsFile))
                RegisterSettings = JsonSerializer.Deserialize<RegisterSettings>(File.ReadAllText(settingsFile), JsonOptions) ?? RegisterSettings;
            if (!RegisterSettings.IsMainPc && !string.IsNullOrWhiteSpace(RegisterSettings.NetworkPath) && Directory.Exists(RegisterSettings.NetworkPath))
            {
                dataFolder = RegisterSettings.NetworkPath;
            }
            LoadAll();
            StartWatching();
        }
        catch
        {
            Sales = [];
        }
    }

    private void StartWatching()
    {
        watcher?.Dispose();
        try
        {
            watcher = new FileSystemWatcher(dataFolder, "*.json");
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.EnableRaisingEvents = true;
        }
        catch { }
    }

    private volatile bool isReloading;
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (suppressWatcher || isReloading) return;
        isReloading = true;
        try
        {
            Thread.Sleep(150);
            LoadAll();
            DataChanged?.Invoke();
        }
        catch { }
        finally { isReloading = false; }
    }

    public void SuppressWatcher(bool suppress) => suppressWatcher = suppress;

    private void LoadAll()
    {
        if (File.Exists(file))
            Sales = JsonSerializer.Deserialize<List<Sale>>(File.ReadAllText(file), JsonOptions) ?? [];
        if (File.Exists(openOrdersFile))
            OpenOrders = JsonSerializer.Deserialize<List<OpenOrder>>(File.ReadAllText(openOrdersFile), JsonOptions) ?? [];
        if (File.Exists(roomsFile))
            Rooms = JsonSerializer.Deserialize<List<Room>>(File.ReadAllText(roomsFile), JsonOptions) ?? Rooms;
        if (File.Exists(operatorsFile))
            Operators = JsonSerializer.Deserialize<List<OperatorAccount>>(File.ReadAllText(operatorsFile), JsonOptions) ?? Operators;
        if (File.Exists(familiesFile))
            Families = JsonSerializer.Deserialize<List<ProductFamily>>(File.ReadAllText(familiesFile), JsonOptions) ?? Families;
        if (File.Exists(productsFile))
            Products = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText(productsFile), JsonOptions) ?? Products;
        if (File.Exists(printersFile))
            PrinterDestinations = JsonSerializer.Deserialize<List<PrinterDestination>>(File.ReadAllText(printersFile), JsonOptions) ?? [];
        if (!PrinterDestinations.Any(d => d.Name == "Refund"))
        {
            var refundSource = PrinterDestinations.FirstOrDefault(d => d.Name == "Payment");
            PrinterDestinations.Add(new PrinterDestination("Refund", refundSource?.WindowsPrinter ?? "", refundSource?.Header ?? "REFUND", refundSource?.Footer ?? "Thank you", refundSource?.CutAfterPrint ?? true, refundSource?.Layout == null ? null : [.. refundSource.Layout], refundSource?.LogoPath ?? ""));
        }
        if (File.Exists(featureSettingsFile))
        {
            var loadedFeatures = JsonSerializer.Deserialize<FeatureSettings>(File.ReadAllText(featureSettingsFile), JsonOptions);
            if (loadedFeatures != null) CopyFeatures(loadedFeatures, Features);
        }
        if (File.Exists(modifiersFile))
            ModifierGroups = JsonSerializer.Deserialize<List<ModifierGroup>>(File.ReadAllText(modifiersFile), JsonOptions) ?? ModifierGroups;
        if (File.Exists(customersFile))
            Customers = JsonSerializer.Deserialize<List<Customer>>(File.ReadAllText(customersFile), JsonOptions) ?? Customers;
        if (File.Exists(ingredientsFile))
            Ingredients = JsonSerializer.Deserialize<List<Ingredient>>(File.ReadAllText(ingredientsFile), JsonOptions) ?? Ingredients;
        if (File.Exists(recipesFile))
            Recipes = JsonSerializer.Deserialize<Dictionary<string, List<RecipeItem>>>(File.ReadAllText(recipesFile), JsonOptions) ?? Recipes;
        if (File.Exists(shiftsFile))
            Shifts = JsonSerializer.Deserialize<List<ShiftRecord>>(File.ReadAllText(shiftsFile), JsonOptions) ?? Shifts;
        if (File.Exists(auditFile))
            AuditLog = JsonSerializer.Deserialize<List<AuditEntry>>(File.ReadAllText(auditFile), JsonOptions) ?? AuditLog;
        if (File.Exists(happyHourFile))
            HappyHourRules = JsonSerializer.Deserialize<List<HappyHourRule>>(File.ReadAllText(happyHourFile), JsonOptions) ?? HappyHourRules;
        if (File.Exists(zReportsFile))
            ZReports = JsonSerializer.Deserialize<List<ZReportRecord>>(File.ReadAllText(zReportsFile), JsonOptions) ?? ZReports;
        if (File.Exists(eightySixFile))
            EightySix = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(eightySixFile), JsonOptions) ?? EightySix;
        if (File.Exists(floorPlanFile))
            FloorPlan = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, FloorTable>>>(File.ReadAllText(floorPlanFile), JsonOptions) ?? FloorPlan;
        if (File.Exists(giftCardsFile))
            GiftCards = JsonSerializer.Deserialize<List<GiftCard>>(File.ReadAllText(giftCardsFile), JsonOptions) ?? GiftCards;
        if (File.Exists(cashMovementsFile))
            CashMovements = JsonSerializer.Deserialize<List<CashMovement>>(File.ReadAllText(cashMovementsFile), JsonOptions) ?? CashMovements;
        if (File.Exists(feeSettingsFile))
            FeeSettings = JsonSerializer.Deserialize<FeeSettings>(File.ReadAllText(feeSettingsFile), JsonOptions) ?? FeeSettings;
        if (File.Exists(specialOrdersFile))
            SpecialOrders = JsonSerializer.Deserialize<List<SpecialOrder>>(File.ReadAllText(specialOrdersFile), JsonOptions) ?? SpecialOrders;
        if (File.Exists(offlinePaymentsFile))
            OfflinePayments = JsonSerializer.Deserialize<List<OfflinePayment>>(File.ReadAllText(offlinePaymentsFile), JsonOptions) ?? OfflinePayments;
        if (File.Exists(vtInvoicesFile))
            VtInvoices = JsonSerializer.Deserialize<List<VtInvoice>>(File.ReadAllText(vtInvoicesFile), JsonOptions) ?? VtInvoices;
        if (File.Exists(reservationsFile))
            Reservations = JsonSerializer.Deserialize<List<Reservation>>(File.ReadAllText(reservationsFile), JsonOptions) ?? Reservations;
        if (File.Exists(scheduleFile))
            Schedule = JsonSerializer.Deserialize<List<ScheduleEntry>>(File.ReadAllText(scheduleFile), JsonOptions) ?? Schedule;
        if (File.Exists(suppliersFile))
            Suppliers = JsonSerializer.Deserialize<List<Supplier>>(File.ReadAllText(suppliersFile), JsonOptions) ?? Suppliers;
        if (File.Exists(purchaseOrdersFile))
            PurchaseOrders = JsonSerializer.Deserialize<List<PurchaseOrder>>(File.ReadAllText(purchaseOrdersFile), JsonOptions) ?? PurchaseOrders;
        if (File.Exists(stockTakesFile))
            StockTakes = JsonSerializer.Deserialize<List<StockTake>>(File.ReadAllText(stockTakesFile), JsonOptions) ?? StockTakes;
        if (File.Exists(wasteFile))
            WasteLog = JsonSerializer.Deserialize<List<WasteEntry>>(File.ReadAllText(wasteFile), JsonOptions) ?? WasteLog;
        if (File.Exists(transfersFile))
            StockTransfers = JsonSerializer.Deserialize<List<StockTransfer>>(File.ReadAllText(transfersFile), JsonOptions) ?? StockTransfers;
        if (File.Exists(locationStockFile))
            LocationStock = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, decimal>>>(File.ReadAllText(locationStockFile), JsonOptions) ?? LocationStock;
        if (File.Exists(locationsFile))
            Locations = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(locationsFile), JsonOptions) ?? Locations;
        if (File.Exists(prepFile))
            PrepTimes = JsonSerializer.Deserialize<List<PrepRecord>>(File.ReadAllText(prepFile), JsonOptions) ?? [];
        if (File.Exists(productVariantsFile))
            ProductVariants = JsonSerializer.Deserialize<List<ProductVariant>>(File.ReadAllText(productVariantsFile), JsonOptions) ?? ProductVariants;
        if (File.Exists(auto86File))
            Auto86 = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(auto86File), JsonOptions) ?? Auto86;
        if (File.Exists(emailSettingsFile))
            EmailSettings = JsonSerializer.Deserialize<EmailSettings>(File.ReadAllText(emailSettingsFile), JsonOptions) ?? EmailSettings;
        if (File.Exists(courseNamesFile))
            CourseNames = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(courseNamesFile), JsonOptions) ?? CourseNames;
        if (File.Exists(parkedOrdersFile))
            ParkedOrders = JsonSerializer.Deserialize<List<ParkedOrder>>(File.ReadAllText(parkedOrdersFile), JsonOptions) ?? ParkedOrders;
        if (File.Exists(comboDealsFile))
            ComboDeals = JsonSerializer.Deserialize<List<ComboDeal>>(File.ReadAllText(comboDealsFile), JsonOptions) ?? ComboDeals;
        SyncQueue = File.Exists(syncQueueFile)
            ? JsonSerializer.Deserialize<List<Sale>>(File.ReadAllText(syncQueueFile), JsonOptions) ?? []
            : [];
        if (File.Exists(countersFile))
        {
            var counters = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(countersFile), JsonOptions);
            if (counters != null && counters.TryGetValue("Order", out var oc) && counters.TryGetValue("Bill", out var bc) && counters.TryGetValue("Payment", out var pc) && counters.TryGetValue("Date", out var dateInt))
            {
                CounterDate = DateTime.Now.ToString("yyyyMMdd");
                var today = int.Parse(CounterDate);
                if (dateInt == today) { OrderCounter = oc; BillCounter = bc; PaymentCounter = pc; return; }
            }
        }
        CounterDate = DateTime.Now.ToString("yyyyMMdd");
        OrderCounter = 0;
        BillCounter = 0;
        PaymentCounter = 0;
    }

    public void SwitchToNetworkPath(string networkPath)
    {
        dataFolder = networkPath;
        LoadAll();
        StartWatching();
    }

    public void SwitchToLocal()
    {
        dataFolder = LocalDataFolder;
        Directory.CreateDirectory(dataFolder);
        LoadAll();
        StartWatching();
    }

    public void AddSale(Sale sale)
    {
        if (Features.MultiBranch && !string.IsNullOrWhiteSpace(Features.CurrentBranch))
            sale = sale with { Branch = Features.CurrentBranch };
        if (!string.IsNullOrWhiteSpace(Features.CloudSyncDevice))
            sale = sale with { Device = Features.CloudSyncDevice };
        Sales.Add(sale);
        suppressWatcher = true;
        File.WriteAllText(file, JsonSerializer.Serialize(Sales, JsonOptions));
        suppressWatcher = false;
        FireWebhook(sale);
        EnqueueSync(sale);
    }

    public bool HasSale(Sale sale) => Sales.Any(s => s.PaymentNumber == sale.PaymentNumber && s.Time == sale.Time && s.Total == sale.Total);

    public void SaveSales()
    {
        suppressWatcher = true;
        File.WriteAllText(file, JsonSerializer.Serialize(Sales, JsonOptions));
        suppressWatcher = false;
    }

    public void EnqueueSync(Sale sale)
    {
        if (!Features.CloudSync) return;
        SyncQueue.Add(sale);
        if (SyncQueue.Count > 2000) SyncQueue.RemoveRange(0, SyncQueue.Count - 2000);
        SaveSyncQueue();
    }

    public void SaveSyncQueue()
    {
        suppressWatcher = true;
        try { File.WriteAllText(syncQueueFile, JsonSerializer.Serialize(SyncQueue, JsonOptions)); } catch { }
        suppressWatcher = false;
    }

    private void FireWebhook(Sale sale)
    {
        var url = Features.WebhookUrl;
        if (string.IsNullOrWhiteSpace(url)) return;
        string payload;
        try
        {
            payload = JsonSerializer.Serialize(new
            {
                @event = "sale.completed",
                payment_number = sale.PaymentNumber,
                time = sale.Time.ToString("yyyy-MM-ddTHH:mm:ss"),
                total = sale.Total,
                payment_type = sale.PaymentType,
                @operator = sale.Operator,
                branch = sale.Branch,
                table = sale.Table,
                order_type = sale.OrderType,
                discount = sale.Discount,
                tip = sale.Tip
            });
        }
        catch { return; }
        _ = Task.Run(async () =>
        {
            try
            {
                using var hc = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                _ = await hc.PostAsync(url, new System.Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
            }
            catch { }
        });
    }

    public string DataFolder => dataFolder;

    internal sealed class PrepRecord { public DateTime Time { get; set; } public int Minutes { get; set; } }
    private string prepFile => Path.Combine(dataFolder, "prep-times.json");
    public List<PrepRecord> PrepTimes { get; private set; } = [];
    private string syncQueueFile => Path.Combine(dataFolder, "sync-queue.json");
    public List<Sale> SyncQueue { get; private set; } = [];

    public void AddPrepTime(int minutes)
    {
        PrepTimes.Add(new PrepRecord { Time = DateTime.Now, Minutes = minutes });
        if (PrepTimes.Count > 500) PrepTimes = [.. PrepTimes.Skip(PrepTimes.Count - 500)];
        suppressWatcher = true;
        try { File.WriteAllText(prepFile, JsonSerializer.Serialize(PrepTimes, JsonOptions)); } catch { }
        suppressWatcher = false;
    }

    public void SaveRooms(List<Room> rooms)
    {
        Rooms = rooms.Where(room => !string.IsNullOrWhiteSpace(room.Name) && room.TableCount > 0).ToList();
        suppressWatcher = true;
        File.WriteAllText(roomsFile, JsonSerializer.Serialize(Rooms, JsonOptions));
        suppressWatcher = false;
    }

    public void SaveOperators(List<OperatorAccount> operators)
    {
        Operators = operators.Where(operatorAccount => !string.IsNullOrWhiteSpace(operatorAccount.Name)).ToList();
        suppressWatcher = true;
        File.WriteAllText(operatorsFile, JsonSerializer.Serialize(Operators, JsonOptions));
        suppressWatcher = false;
    }

    public void SaveFamilies(List<ProductFamily> families)
    {
        Families = families.Where(family => !string.IsNullOrWhiteSpace(family.Name)).ToList();
        suppressWatcher = true;
        File.WriteAllText(familiesFile, JsonSerializer.Serialize(Families, JsonOptions));
        suppressWatcher = false;
    }

    public void SaveProducts(List<Product> prods)
    {
        Products = prods.Where(p => !string.IsNullOrWhiteSpace(p.Name)).ToList();
        suppressWatcher = true;
        File.WriteAllText(productsFile, JsonSerializer.Serialize(Products, JsonOptions));
        suppressWatcher = false;
    }

    public void SaveRegisterSettings(RegisterSettings settings)
    {
        RegisterSettings = settings;
        suppressWatcher = true;
        File.WriteAllText(settingsFile, JsonSerializer.Serialize(settings, JsonOptions));
        suppressWatcher = false;
    }

    public void SavePrinterDestinations(List<PrinterDestination> destinations)
    {
        PrinterDestinations = destinations.Where(destination => !string.IsNullOrWhiteSpace(destination.Name) && !string.IsNullOrWhiteSpace(destination.WindowsPrinter)).ToList();
        suppressWatcher = true;
        File.WriteAllText(printersFile, JsonSerializer.Serialize(PrinterDestinations, JsonOptions));
        suppressWatcher = false;
    }

    public void SaveFeatures(FeatureSettings features)
    {
        if (!ReferenceEquals(features, Features)) CopyFeatures(features, Features);
        suppressWatcher = true;
        File.WriteAllText(featureSettingsFile, JsonSerializer.Serialize(Features, JsonOptions));
        suppressWatcher = false;
    }

    private static void CopyFeatures(FeatureSettings src, FeatureSettings dst)
    {
        foreach (var p in typeof(FeatureSettings).GetProperties())
            if (p.CanRead && p.CanWrite) p.SetValue(dst, p.GetValue(src));
    }

    public void SaveOpenOrders()
    {
        suppressWatcher = true;
        File.WriteAllText(openOrdersFile, JsonSerializer.Serialize(OpenOrders, JsonOptions));
        suppressWatcher = false;
    }

    public void SaveModifiers(List<ModifierGroup> mods) { ModifierGroups = mods; suppressWatcher = true; File.WriteAllText(modifiersFile, JsonSerializer.Serialize(mods, JsonOptions)); suppressWatcher = false; }
    public void SaveCustomers(List<Customer> custs) { Customers = custs; suppressWatcher = true; File.WriteAllText(customersFile, JsonSerializer.Serialize(custs, JsonOptions)); suppressWatcher = false; }
    public void SaveIngredients(List<Ingredient> ingr) { Ingredients = ingr; suppressWatcher = true; File.WriteAllText(ingredientsFile, JsonSerializer.Serialize(ingr, JsonOptions)); suppressWatcher = false; }
    public void SaveRecipes(Dictionary<string, List<RecipeItem>> rec) { Recipes = rec; suppressWatcher = true; File.WriteAllText(recipesFile, JsonSerializer.Serialize(rec, JsonOptions)); suppressWatcher = false; }
    public void SaveShifts(List<ShiftRecord> shifts) { Shifts = shifts; suppressWatcher = true; File.WriteAllText(shiftsFile, JsonSerializer.Serialize(shifts, JsonOptions)); suppressWatcher = false; }
    public void SaveAuditLog(List<AuditEntry> log) { AuditLog = log; suppressWatcher = true; File.WriteAllText(auditFile, JsonSerializer.Serialize(log, JsonOptions)); suppressWatcher = false; }
    public void SaveHappyHours(List<HappyHourRule> rules) { HappyHourRules = rules; suppressWatcher = true; File.WriteAllText(happyHourFile, JsonSerializer.Serialize(rules, JsonOptions)); suppressWatcher = false; }
    public void SaveZReports(List<ZReportRecord> recs) { ZReports = recs; suppressWatcher = true; File.WriteAllText(zReportsFile, JsonSerializer.Serialize(recs, JsonOptions)); suppressWatcher = false; }
    public void RecordZReport(int transactions, decimal gross, string op) { ZReports.Add(new(DateTime.Now, transactions, gross, op)); SaveZReports(ZReports); }
    public void SaveEightySix(List<string> items) { EightySix = items; suppressWatcher = true; File.WriteAllText(eightySixFile, JsonSerializer.Serialize(items, JsonOptions)); suppressWatcher = false; }
    public void SaveGiftCards(List<GiftCard> cards) { GiftCards = cards; suppressWatcher = true; File.WriteAllText(giftCardsFile, JsonSerializer.Serialize(cards, JsonOptions)); suppressWatcher = false; }
    public void SaveCashMovements(List<CashMovement> moves) { CashMovements = moves; suppressWatcher = true; File.WriteAllText(cashMovementsFile, JsonSerializer.Serialize(moves, JsonOptions)); suppressWatcher = false; }
    public void SaveFees(FeeSettings fees) { FeeSettings = fees; suppressWatcher = true; File.WriteAllText(feeSettingsFile, JsonSerializer.Serialize(fees, JsonOptions)); suppressWatcher = false; }
    public void SaveSpecialOrders(List<SpecialOrder> orders) { SpecialOrders = orders; suppressWatcher = true; File.WriteAllText(specialOrdersFile, JsonSerializer.Serialize(orders, JsonOptions)); suppressWatcher = false; }
    public void SaveOfflinePayments(List<OfflinePayment> pays) { OfflinePayments = pays; suppressWatcher = true; File.WriteAllText(offlinePaymentsFile, JsonSerializer.Serialize(pays, JsonOptions)); suppressWatcher = false; }
    public void SaveVtInvoices(List<VtInvoice> invs) { VtInvoices = invs; suppressWatcher = true; File.WriteAllText(vtInvoicesFile, JsonSerializer.Serialize(invs, JsonOptions)); suppressWatcher = false; }
    public void SaveReservations(List<Reservation> res) { Reservations = res; suppressWatcher = true; File.WriteAllText(reservationsFile, JsonSerializer.Serialize(res, JsonOptions)); suppressWatcher = false; }
    public void SaveSchedule(List<ScheduleEntry> entries) { Schedule = entries; suppressWatcher = true; File.WriteAllText(scheduleFile, JsonSerializer.Serialize(entries, JsonOptions)); suppressWatcher = false; }
    public void SaveSuppliers(List<Supplier> sups) { Suppliers = sups; suppressWatcher = true; File.WriteAllText(suppliersFile, JsonSerializer.Serialize(sups, JsonOptions)); suppressWatcher = false; }
    public void SavePurchaseOrders(List<PurchaseOrder> pos) { PurchaseOrders = pos; suppressWatcher = true; File.WriteAllText(purchaseOrdersFile, JsonSerializer.Serialize(pos, JsonOptions)); suppressWatcher = false; }
    public void SaveStockTakes(List<StockTake> takes) { StockTakes = takes; suppressWatcher = true; File.WriteAllText(stockTakesFile, JsonSerializer.Serialize(takes, JsonOptions)); suppressWatcher = false; }
    public void SaveWasteLog(List<WasteEntry> entries) { WasteLog = entries; suppressWatcher = true; File.WriteAllText(wasteFile, JsonSerializer.Serialize(entries, JsonOptions)); suppressWatcher = false; }
    public void SaveStockTransfers(List<StockTransfer> transfers) { StockTransfers = transfers; suppressWatcher = true; File.WriteAllText(transfersFile, JsonSerializer.Serialize(transfers, JsonOptions)); suppressWatcher = false; }
    public void SaveLocationStock() { suppressWatcher = true; File.WriteAllText(locationStockFile, JsonSerializer.Serialize(LocationStock, JsonOptions)); suppressWatcher = false; }
    public void SaveLocations(List<string> locations) { Locations = locations; suppressWatcher = true; File.WriteAllText(locationsFile, JsonSerializer.Serialize(locations, JsonOptions)); suppressWatcher = false; }
    public void SaveProductVariants(List<ProductVariant> variants) { ProductVariants = variants; suppressWatcher = true; File.WriteAllText(productVariantsFile, JsonSerializer.Serialize(variants, JsonOptions)); suppressWatcher = false; }
    public void SaveAuto86(List<string> items) { Auto86 = items; suppressWatcher = true; File.WriteAllText(auto86File, JsonSerializer.Serialize(items, JsonOptions)); suppressWatcher = false; }
    public void SaveEmailSettings(EmailSettings settings) { EmailSettings = settings; suppressWatcher = true; File.WriteAllText(emailSettingsFile, JsonSerializer.Serialize(settings, JsonOptions)); suppressWatcher = false; }
    public void SaveCourseNames(List<string> names) { CourseNames = names; suppressWatcher = true; File.WriteAllText(courseNamesFile, JsonSerializer.Serialize(names, JsonOptions)); suppressWatcher = false; }
    public void SaveParkedOrders(List<ParkedOrder> orders) { ParkedOrders = orders; suppressWatcher = true; File.WriteAllText(parkedOrdersFile, JsonSerializer.Serialize(orders, JsonOptions)); suppressWatcher = false; }
    public void SaveComboDeals(List<ComboDeal> deals) { ComboDeals = deals; suppressWatcher = true; File.WriteAllText(comboDealsFile, JsonSerializer.Serialize(deals, JsonOptions)); suppressWatcher = false; }
    public void SaveFloorPlan() { suppressWatcher = true; File.WriteAllText(floorPlanFile, JsonSerializer.Serialize(FloorPlan, JsonOptions)); suppressWatcher = false; }
    public bool ZReportPrintedToday() => ZReports.Any(z => z.Time.Date == DateTime.Today);
    public void LogAudit(string op, string action, string detail = "") { AuditLog.Add(new(DateTime.Now, op, action, detail)); SaveAuditLog(AuditLog); }
    public void RaiseGuestAlert(string room, string table, string kind)
    {
        GuestAlerts.Add(new($"G{DateTime.Now:yyyyMMddHHmmssfff}", room, table, kind, DateTime.Now));
        GuestAlertRaised?.Invoke();
    }
    public void DismissGuestAlert(string id)
    {
        GuestAlerts.RemoveAll(a => a.Id == id);
        GuestAlertRaised?.Invoke();
    }
    public void ClearGuestAlerts()
    {
        GuestAlerts.Clear();
        GuestAlertRaised?.Invoke();
    }
    public void DismissGuestAlerts(string room, string table, string kind)
    {
        GuestAlerts.RemoveAll(a => a.Room == room && a.Table == table && a.Kind == kind);
        GuestAlertRaised?.Invoke();
    }

    public void AddOpenOrder(OpenOrder order)
    {
        OpenOrders.Add(order);
        SaveOpenOrders();
    }

    private void SaveCounters()
    {
        suppressWatcher = true;
        File.WriteAllText(countersFile, JsonSerializer.Serialize(new Dictionary<string, int> { ["Order"] = OrderCounter, ["Bill"] = BillCounter, ["Payment"] = PaymentCounter, ["Date"] = int.Parse(CounterDate) }, JsonOptions));
        suppressWatcher = false;
    }

    private void ResetCountersIfNeeded()
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        if (CounterDate == today) return;
        CounterDate = today;
        OrderCounter = 0;
        BillCounter = 0;
        PaymentCounter = 0;
    }

    public int NextOrderNumber()
    {
        ResetCountersIfNeeded();
        OrderCounter++;
        BillCounter++;
        SaveCounters();
        return OrderCounter;
    }

    public int NextBillNumber()
    {
        ResetCountersIfNeeded();
        SaveCounters();
        return BillCounter;
    }

    public int NextPaymentNumber()
    {
        ResetCountersIfNeeded();
        PaymentCounter++;
        SaveCounters();
        return PaymentCounter;
    }

    public void RemoveOpenOrder(string room, string table)
    {
        OpenOrders.RemoveAll(o => o.Room == room && o.Table == table);
        SaveOpenOrders();
    }

    public OpenOrder? GetOpenOrder(string room, string table) =>
        OpenOrders.FirstOrDefault(o => o.Room == room && o.Table == table);
}

internal sealed class LoginForm : Form
{
    public OperatorAccount? LoggedInOperator { get; private set; }

    public LoginForm(Color background, Color panelLight, Color mint, List<OperatorAccount> operators, bool pinMode = false)
    {
        Text = "Login";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(360, 320);
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var title = new Label { Text = "VECTRON POS", AutoSize = true, Font = new Font("Segoe UI Semibold", 20F), ForeColor = mint, Location = new Point(90, 20) };
        var subtitle = new Label { Text = pinMode ? "Select operator and enter PIN" : "Select operator and enter password", AutoSize = true, ForeColor = Color.FromArgb(143, 154, 171), Location = new Point(60, 60) };
        var operatorLabel = new Label { Text = "Operator", AutoSize = true, Location = new Point(30, 100) };
        var operatorList = new ComboBox { Location = new Point(30, 126), Size = new Size(300, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
        foreach (var op in operators) operatorList.Items.Add(op.Name);
        if (operatorList.Items.Count > 0) operatorList.SelectedIndex = 0;
        var passwordLabel = new Label { Text = pinMode ? "PIN" : "Password", AutoSize = true, Location = new Point(30, 172) };
        var passwordBox = new TextBox { Location = new Point(30, 198), Size = new Size(300, 30), BackColor = panelLight, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true };
        passwordBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) TryLogin(operators, operatorList, passwordBox, pinMode); };
        var loginButton = new Button { Text = pinMode ? "Enter" : "Login", Location = new Point(30, 250), Size = new Size(300, 40), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        loginButton.FlatAppearance.BorderSize = 0;
        loginButton.Click += (_, _) => TryLogin(operators, operatorList, passwordBox, pinMode);
        Controls.AddRange([title, subtitle, operatorLabel, operatorList, passwordLabel, passwordBox, loginButton]);
        AcceptButton = loginButton;
    }

    private void TryLogin(List<OperatorAccount> operators, ComboBox operatorList, TextBox passwordBox, bool pinMode)
    {
        if (operatorList.SelectedIndex < 0) return;
        var selected = operators[operatorList.SelectedIndex];
        if (!string.IsNullOrWhiteSpace(selected.Password) && selected.Password != passwordBox.Text)
        {
            MessageBox.Show(pinMode ? "Incorrect PIN." : "Incorrect password.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        LoggedInOperator = selected;
        DialogResult = DialogResult.OK;
        Close();
    }
}

internal sealed class NetworkSettingsForm : Form
{
    public NetworkSettingsForm(Color background, Color panelLight, Color mint, RegisterSettings settings, Action<RegisterSettings> save)
    {
        Text = "Network settings";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 280);
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var title = new Label { Text = "Network configuration", AutoSize = true, Font = new Font("Segoe UI Semibold", 16F), Location = new Point(20, 16) };
        var modeLabel = new Label { Text = "PC Mode", AutoSize = true, Location = new Point(20, 60) };
        var mainPc = new RadioButton { Text = "Main PC (server) — hosts the database", Location = new Point(20, 86), AutoSize = true, ForeColor = Color.White, Checked = settings.IsMainPc };
        var orderPc = new RadioButton { Text = "Order PC (client) — connects to main PC", Location = new Point(20, 116), AutoSize = true, ForeColor = Color.White, Checked = !settings.IsMainPc };
        var pathLabel = new Label { Text = "Network path (from main PC)", AutoSize = true, Location = new Point(20, 158) };
        var pathBox = new TextBox { Location = new Point(20, 184), Size = new Size(430, 30), BackColor = panelLight, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Text = settings.NetworkPath };
        var hint = new Label { Text = "Example: \\\\MAIN-PC\\client database", AutoSize = true, ForeColor = Color.FromArgb(143, 154, 171), Location = new Point(20, 220) };
        pathBox.Enabled = !settings.IsMainPc;
        mainPc.CheckedChanged += (_, _) => pathBox.Enabled = !mainPc.Checked;
        var saveButton = new Button { Text = "Save", Location = new Point(330, 240), Size = new Size(120, 34), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        saveButton.FlatAppearance.BorderSize = 0;
        saveButton.Click += (_, _) =>
        {
            var updated = new RegisterSettings(settings.Currency, settings.TvaRate, settings.PricesIncludeTva, pathBox.Text.Trim(), mainPc.Checked);
            save(updated);
            DialogResult = DialogResult.OK;
            Close();
        };
        Controls.AddRange([title, modeLabel, mainPc, orderPc, pathLabel, pathBox, hint, saveButton]);
    }
}

internal sealed class PosForm : Form
{
    private readonly Color background = Color.FromArgb(19, 24, 33);
    private readonly Color panel = Color.FromArgb(27, 34, 46);
    private readonly Color panelLight = Color.FromArgb(37, 47, 62);
    private readonly Color mint = Color.FromArgb(71, 214, 174);
    private readonly PosStore store = new();
    private List<Product> products;
    private readonly List<(Product Product, int Count, int Course, string Note, int Seat, string Modifiers)> basket = [];
    private bool hasOpenOrder;
    private bool isOrderPc;
    private readonly FlowLayoutPanel productGrid = new();
    private readonly FlowLayoutPanel basketList = new();
    private readonly Label totalLabel = new();
    private readonly Label orderLabel = new();
    private readonly Label salesLabel = new();
    private readonly Button operatorButton = new();
    private readonly Button noteButton = new();
    private Button payButton = new();
    private Button splitBillButton = new();
    private Button transferButton = new();
    private Button mergeButton = new();
    private Button discountButton = new();
    private decimal basketDiscountPercent;
    private readonly Label greetingLabel = new();
    private Button roomsNavButton = new();
    private Button settingsNavButton = new();
    private Button clockInOutBtn = new();
    private Button stockBtn = new();
    private Button kdsNavButton = new();
    private Button eightySixNavButton = new();
    private Button reservationsNavButton = new();
    private Button scheduleNavButton = new();
    private Button giftCardsNavButton = new();
    private Button purchasesNavButton = new();
    private Button drawerNavButton = new();
    private Button fireButton = new();
    private Button delayFireButton = new();
    private readonly List<System.Windows.Forms.Timer> delayFireTimers = [];
    private readonly Button repeatButton = new();
    private readonly Button parkButton = new();
    private readonly Button dealsButton = new();
    private readonly Button reprintButton = new();
    private (Sale Sale, List<SoldLine> Lines, decimal DiscountPercent, string Room, string Table)? lastReceipt;
    private readonly Button quickSaleBtn = new();
    private TextBox? barcodeBox;
    private readonly Panel qrPanel = new() { Dock = DockStyle.Top, Height = 34, Visible = false, BackColor = Color.FromArgb(44, 92, 70) };
    private readonly Label qrLabel = new();
    private DateTime lastDailyBackup = DateTime.MinValue;
    private QrOrderServer? qrServer;
    private readonly Panel guestBar = new() { Dock = DockStyle.Top, Height = 40, Visible = false, BackColor = Color.FromArgb(190, 130, 40) };
    private readonly Panel trainingBar = new() { Dock = DockStyle.Top, Height = 34, Visible = false, BackColor = Color.FromArgb(180, 60, 60) };
    private readonly Label trainingLabel = new();
    private readonly Label guestLabel = new() { Dock = DockStyle.Fill, ForeColor = Color.White, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 10F) };
    private readonly Button guestDismissBtn = new() { Text = "Dismiss all", Dock = DockStyle.Right, Width = 110, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(140, 60, 40), Font = new Font("Segoe UI", 9F) };
    private CustomerDisplayForm? custDisplay;
    private KioskForm? kioskForm;
    private readonly Panel menuPanel = new();
    private readonly Panel workspacePanel = new();
    private readonly Panel roomsPanel = new();
    private readonly Panel viewPanel = new();
    private Panel mainHeader = new();
    private Panel orderHeader = new();
    private bool floorPlanEdit;
    private Room? floorMapRoom;
    private readonly Panel seatBar = new() { Location = new Point(0, 80), Size = new Size(290, 28), Visible = false };
    private int activeSeat = 1;
    private readonly FlowLayoutPanel roomButtons = new();
    private readonly Panel tableMap = new();
    private string category = "Popular";
    private string selectedRoom = "Main dining";
    private string selectedTable = "Table 4";
    private OperatorAccount currentOperator;

    public PosForm()
    {
        OnScreenKeyboard.IsEnabled = () => store.Features.OnScreenKeyboard;
        ApplyCurrencyCulture(store.RegisterSettings.Currency);
        Text = "Vectron POS";
        StartPosition = FormStartPosition.Manual;
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Normal;
        var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
        Location = screen.Bounds.Location;
        Size = screen.Bounds.Size;

        if (store.Operators.Any(op => !string.IsNullOrWhiteSpace(op.Password)))
        {
            using var login = new LoginForm(background, panelLight, mint, store.Operators, store.Features.PinLogin);
            if (login.ShowDialog(this) != DialogResult.OK || login.LoggedInOperator == null)
            {
                Close();
                return;
            }
            currentOperator = login.LoggedInOperator;
        }
        else
        {
            currentOperator = store.Operators[0];
        }
        products = store.Products;
        isOrderPc = !store.RegisterSettings.IsMainPc;

        if (!isOrderPc) TcpPrintServer.Start();
        FormClosing += (_, _) => TcpPrintServer.Stop();
        FormClosing += (_, _) => StopQrServer();

        BuildUi();
        ApplyOrderPcRestrictions();
        RenderProducts();
        RenderBasket();
        TicketLayout.CourseNames = store.CourseNames.ToArray();
        if ((store.Features.QROrdering || store.Features.ApiEnabled || store.Features.CloudSync) && !isOrderPc) StartQrServer();
        StartBackupScheduler();
        StartCloudSyncScheduler();

        store.DataChanged += OnDataChanged;
        store.GuestAlertRaised += OnGuestAlertRaised;
    }

    private void OnGuestAlertRaised()
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) { try { BeginInvoke(UpdateGuestAlertBar); } catch { } return; }
        UpdateGuestAlertBar();
    }

    private void UpdateGuestAlertBar()
    {
        var alerts = store.GuestAlerts;
        guestBar.Visible = alerts.Count > 0;
        if (alerts.Count == 0) return;
        var a = alerts[^1];
        if (a.Kind == "ready")
        {
            guestBar.BackColor = Color.FromArgb(40, 140, 90);
            guestLabel.Text = $"  \U0001f389 {a.Room} / {a.Table} — ORDER READY ({a.Time:HH:mm}){(alerts.Count > 1 ? $"   +{alerts.Count - 1} more" : "")}";
        }
        else
        {
            guestBar.BackColor = Color.FromArgb(190, 130, 40);
            guestLabel.Text = $"  \U0001f514 {a.Room} / {a.Table} — WAITER CALLED ({a.Time:HH:mm}){(alerts.Count > 1 ? $"   +{alerts.Count - 1} more" : "")}";
        }
    }

    private void OnDataChanged()
    {
        if (InvokeRequired) { BeginInvoke(OnDataChanged); return; }
        ApplyCurrencyCulture(store.RegisterSettings.Currency);
        products = store.Products;
        TicketLayout.CourseNames = store.CourseNames.ToArray();
        RenderProducts();
        RenderRooms();
        RenderBasket();
        salesLabel.Text = TodaySales();
        RefreshOpenOrders();
        UpdateQrBanner();
    }

    private void RefreshOpenOrders()
    {
        if (!string.IsNullOrWhiteSpace(selectedRoom) && selectedTable != "No table selected")
        {
            var existing = store.GetOpenOrder(selectedRoom, selectedTable);
            if (existing != null && !hasOpenOrder)
            {
                hasOpenOrder = true;
                basket.Clear();
                foreach (var item in existing.Items)
                {
                    var product = products.FirstOrDefault(p => p.Name == item.Product);
                    if (product != null)
                    {
                        var idx = basket.FindIndex(b => b.Product == product && b.Note == item.Note && b.Modifiers == item.Modifiers && (!store.Features.SeatOrdering || b.Seat == item.Seat));
                        if (idx >= 0) basket[idx] = (product, basket[idx].Count + item.Quantity, basket[idx].Course, basket[idx].Note, item.Seat, JoinMods(basket[idx].Modifiers, item.Modifiers));
                        else basket.Add((product, item.Quantity, item.Course, item.Note, item.Seat, item.Modifiers));
                    }
                }
                UpdateOrderButtons();
                RenderBasket();
                orderLabel.Text = OrderContextText();
            }
            else if (existing == null && hasOpenOrder)
            {
                hasOpenOrder = false;
                basket.Clear();
                UpdateOrderButtons();
                RenderBasket();
                orderLabel.Text = OrderContextText();
            }
        }
    }

    private void ApplyOrderPcRestrictions()
    {
        payButton.Visible = !isOrderPc;
        UpdateOrderButtons();
    }

    private void UpdateOrderButtons()
    {
        noteButton.Visible = !isOrderPc && hasOpenOrder;
        splitBillButton.Visible = !isOrderPc && hasOpenOrder && store.Features.SplitBill;
        transferButton.Visible = hasOpenOrder && store.Features.TableTransfer;
        mergeButton.Visible = hasOpenOrder && store.Features.TableTransfer;
        repeatButton.Visible = store.Features.RepeatOrders;
        parkButton.Visible = store.Features.ParkOrders && (basket.Count > 0 || hasOpenOrder);
        dealsButton.Visible = store.Features.ComboDeals && store.ComboDeals.Count > 0;
        reprintButton.Visible = lastReceipt != null;
        fireButton.Visible = false;
        delayFireButton.Visible = false;
        if (store.Features.CourseManagement && hasOpenOrder)
        {
            var order = store.GetOpenOrder(selectedRoom, selectedTable);
            if (order != null && order.Items.Any(l => l.Course > order.FiredCourse))
            {
                var next = order.Items.Where(l => l.Course > order.FiredCourse).Min(l => l.Course);
                fireButton.Text = "Fire " + CourseLabel(next);
                fireButton.Visible = true;
                delayFireButton.Visible = true;
            }
        }
    }

    private void BuildUi()
    {
        var sidebarWidth = Math.Clamp((int)(ClientSize.Width * 0.16), 150, 205);
        var sidebar = new Panel { Dock = DockStyle.Left, Width = sidebarWidth, BackColor = Color.FromArgb(23, 29, 40), Padding = new Padding(18, 24, 16, 20) };
        var brand = new Label { Text = "VECTRON\nPOS", AutoSize = true, Font = new Font("Segoe UI Semibold", Math.Max(14F, ClientSize.Width / 75F)), ForeColor = mint, Location = new Point(20, 22) };
        sidebar.Controls.Add(brand);
        sidebar.Controls.Add(NavButton("Order", 112, true, ShowOrder));
        sidebar.Controls.Add(roomsNavButton = NavButton("Rooms", 166, false, ShowRooms));
        sidebar.Controls.Add(NavButton("Operator", 220, false, ShowOperator));
        sidebar.Controls.Add(settingsNavButton = NavButton("Settings", 274, false, ShowSettings));
        clockInOutBtn = NavButton("Clock", 328, false, ClockInOut);
        clockInOutBtn.Visible = store.Features.ClockInOut;
        sidebar.Controls.Add(clockInOutBtn);
        stockBtn = NavButton("Stock", 382, false, ShowStockManagement);
        stockBtn.Visible = store.Features.StockManagement;
        sidebar.Controls.Add(stockBtn);
        kdsNavButton = NavButton("Kitchen", 436, false, ShowKitchenDisplay);
        kdsNavButton.Visible = store.Features.KitchenDisplay;
        sidebar.Controls.Add(kdsNavButton);
        eightySixNavButton = NavButton("86 board", 490, false, ShowEightySixBoard);
        eightySixNavButton.Visible = store.Features.EightySixBoard;
        sidebar.Controls.Add(eightySixNavButton);
        reservationsNavButton = NavButton("Bookings", 544, false, ShowReservations);
        sidebar.Controls.Add(reservationsNavButton);
        scheduleNavButton = NavButton("Schedule", 598, false, ShowSchedule);
        sidebar.Controls.Add(scheduleNavButton);
        giftCardsNavButton = NavButton("Gift cards", 652, false, ShowGiftCards);
        sidebar.Controls.Add(giftCardsNavButton);
        purchasesNavButton = NavButton("Purchases", 706, false, ShowPurchases);
        sidebar.Controls.Add(purchasesNavButton);
        drawerNavButton = NavButton("Drawer", 760, false, ShowCashDrawer);
        sidebar.Controls.Add(drawerNavButton);
        var status = new Label { Text = isOrderPc ? $"●  ORDER PC\n\nConnected to main PC" : $"●  MAIN PC\n\nIP: {TcpPrintServer.LocalIp}\nPrint server: {(TcpPrintServer.IsRunning ? "Running on :9100" : "FAILED to start")}", AutoSize = true, ForeColor = Color.FromArgb(138, 151, 169), Dock = DockStyle.Bottom, Padding = new Padding(4, 0, 0, 10) };
        sidebar.Controls.Add(status);
        sidebar.Controls.SetChildIndex(status, 0);

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 18, 20, 18), BackColor = background };
        mainHeader = new Panel { Dock = DockStyle.Top, Height = 82 };
        greetingLabel.Text = GreetingText();
        greetingLabel.AutoSize = true;
        greetingLabel.Font = new Font("Segoe UI Semibold", Math.Max(14F, ClientSize.Width / 65F));
        greetingLabel.Location = new Point(0, 2);
        var date = new Label { Text = DateTime.Now.ToString("dddd, dd MMMM yyyy"), AutoSize = true, ForeColor = Color.FromArgb(143, 154, 171), Location = new Point(2, 39) };
        salesLabel.Text = TodaySales();
        salesLabel.AutoSize = true;
        salesLabel.ForeColor = mint;
        salesLabel.Font = new Font("Segoe UI Semibold", 11F);
        salesLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        salesLabel.Location = new Point(content.ClientSize.Width - 300, 22);
        mainHeader.Controls.AddRange([greetingLabel, date, salesLabel]);
        UpdateBranchBadge();
        operatorButton.Size = new Size(180, 34);
        operatorButton.Location = new Point(content.ClientSize.Width - 350, 14);
        operatorButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        operatorButton.BackColor = panelLight;
        operatorButton.ForeColor = Color.White;
        operatorButton.FlatStyle = FlatStyle.Flat;
        operatorButton.FlatAppearance.BorderSize = 0;
        operatorButton.Click += (_, _) => ShowOperator();
        content.Controls.Add(mainHeader);

        var orderPanel = new Panel { Dock = DockStyle.None, Width = 350, MinimumSize = new Size(275, 0), MaximumSize = new Size(390, 2000), BackColor = panel, Padding = new Padding(20, 20, 20, 18), AutoScroll = true };
        var orderTitle = new Label { Text = "Current order", AutoSize = true, Font = new Font("Segoe UI Semibold", 16F), Location = new Point(20, 20) };
        orderLabel.Text = OrderContextText();
        orderLabel.AutoSize = true;
        orderLabel.ForeColor = Color.FromArgb(143, 154, 171);
        orderLabel.Location = new Point(0, 42);
        orderLabel.MaximumSize = new Size(290, 0);
        orderHeader.Dock = DockStyle.Top;
        orderHeader.Height = store.Features.SeatOrdering ? 116 : 102;
        orderTitle.Location = new Point(0, 0);
        orderLabel.Text = OrderContextText();
        orderLabel.Location = new Point(0, 24);
        orderLabel.MaximumSize = new Size(290, 0);
        seatBar.Visible = store.Features.SeatOrdering;
        quickSaleBtn.Text = "⚡ Quick sale";
        quickSaleBtn.Location = new Point(212, 0);
        quickSaleBtn.Size = new Size(92, 26);
        quickSaleBtn.FlatStyle = FlatStyle.Flat;
        quickSaleBtn.FlatAppearance.BorderSize = 0;
        quickSaleBtn.BackColor = Color.FromArgb(55, 120, 130);
        quickSaleBtn.ForeColor = Color.White;
        quickSaleBtn.Font = new Font("Segoe UI", 8.5F);
        quickSaleBtn.Visible = store.Features.QuickSaleMode;
        quickSaleBtn.Click += (_, _) => QuickSaleStart();
        orderHeader.Controls.AddRange([orderTitle, orderLabel, seatBar, quickSaleBtn]);
        basketList.Dock = DockStyle.Fill;
        basketList.FlowDirection = FlowDirection.TopDown;
        basketList.WrapContents = false;
        basketList.AutoScroll = true;
        basketList.Padding = new Padding(0, 18, 0, 8);
        var summary = new Panel { Dock = DockStyle.Bottom, Height = 282 };
        totalLabel.AutoSize = true;
        totalLabel.Font = new Font("Segoe UI Semibold", 22F);
        totalLabel.Location = new Point(0, 8);
        var totalCaption = new Label { Text = "TOTAL", AutoSize = true, ForeColor = Color.FromArgb(143, 154, 171), Location = new Point(2, 0) };
        var orderButton = ActionButton("Order", 0, 70, 150, 48, Color.FromArgb(55, 120, 130));
        orderButton.Click += (_, _) => PlaceOrder();
        noteButton.Text = "Print bill";
        noteButton.Location = new Point(0, 126);
        noteButton.Size = new Size(150, 34);
        noteButton.FlatStyle = FlatStyle.Flat;
        noteButton.FlatAppearance.BorderSize = 0;
        noteButton.BackColor = Color.FromArgb(80, 90, 110);
        noteButton.ForeColor = Color.White;
        noteButton.Font = new Font("Segoe UI", 9F);
        noteButton.Visible = false;
        noteButton.Click += (_, _) => PrintBill();
        payButton = ActionButton("Take payment", 158, 70, 152, 48, mint);
        payButton.Click += (_, _) => CompleteSale();
        splitBillButton = ActionButton("Split bill", 0, 242, 74, 34, Color.FromArgb(100, 80, 140));
        splitBillButton.Font = new Font("Segoe UI", 8F);
        splitBillButton.Visible = false;
        splitBillButton.Click += (_, _) => SplitBill();
        transferButton = ActionButton("Transfer", 236, 166, 74, 34, Color.FromArgb(80, 100, 140));
        transferButton.Font = new Font("Segoe UI", 8F);
        transferButton.Visible = false;
        transferButton.Click += (_, _) => TransferTable();
        mergeButton = ActionButton("Merge", 0, 166, 74, 34, Color.FromArgb(60, 120, 90));
        mergeButton.Font = new Font("Segoe UI", 8F);
        mergeButton.Visible = false;
        mergeButton.Click += (_, _) => MergeTables();
        discountButton = ActionButton("Discount", 158, 126, 152, 34, Color.FromArgb(200, 140, 60));
        discountButton.Font = new Font("Segoe UI", 9F);
        discountButton.Visible = store.Features.Discounts;
        discountButton.Click += (_, _) => ShowDiscountDialog();
        fireButton = ActionButton("Fire", 80, 166, 74, 34, Color.FromArgb(170, 110, 50));
        fireButton.Font = new Font("Segoe UI", 8F);
        fireButton.Visible = false;
        fireButton.Click += (_, _) => FireNextCourse();
        delayFireButton = ActionButton("⏱ Delay", 160, 166, 74, 34, Color.FromArgb(90, 100, 140));
        delayFireButton.Font = new Font("Segoe UI", 8F);
        delayFireButton.Visible = false;
        delayFireButton.Click += (_, _) => ScheduleDelayFire();
        repeatButton.Text = "Repeat";
        repeatButton.Location = new Point(0, 204);
        repeatButton.Size = new Size(74, 34);
        repeatButton.FlatStyle = FlatStyle.Flat;
        repeatButton.FlatAppearance.BorderSize = 0;
        repeatButton.BackColor = Color.FromArgb(70, 90, 130);
        repeatButton.ForeColor = Color.White;
        repeatButton.Font = new Font("Segoe UI", 8F);
        repeatButton.Click += (_, _) => RepeatLastOrder();
        parkButton.Text = "Park";
        parkButton.Location = new Point(80, 204);
        parkButton.Size = new Size(74, 34);
        parkButton.FlatStyle = FlatStyle.Flat;
        parkButton.FlatAppearance.BorderSize = 0;
        parkButton.BackColor = Color.FromArgb(130, 100, 50);
        parkButton.ForeColor = Color.White;
        parkButton.Font = new Font("Segoe UI", 8F);
        parkButton.Click += (_, _) => ShowParkRecall();
        dealsButton.Text = "Deals";
        dealsButton.Location = new Point(160, 204);
        dealsButton.Size = new Size(74, 34);
        dealsButton.FlatStyle = FlatStyle.Flat;
        dealsButton.FlatAppearance.BorderSize = 0;
        dealsButton.BackColor = Color.FromArgb(150, 70, 100);
        dealsButton.ForeColor = Color.White;
        dealsButton.Font = new Font("Segoe UI", 8F);
        dealsButton.Click += (_, _) => ShowDealsDialog();
        reprintButton.Text = "Reprint";
        reprintButton.Location = new Point(240, 204);
        reprintButton.Size = new Size(74, 34);
        reprintButton.FlatStyle = FlatStyle.Flat;
        reprintButton.FlatAppearance.BorderSize = 0;
        reprintButton.BackColor = Color.FromArgb(60, 110, 100);
        reprintButton.ForeColor = Color.White;
        reprintButton.Font = new Font("Segoe UI", 8F);
        reprintButton.Click += (_, _) =>
        {
            if (store.Features.ReceiptSearch) { ShowFindReceipt(lastReceipt?.Sale.Time ?? DateTime.Today); return; }
            if (lastReceipt is {} r) PrintPaymentReceipt(r.Sale, r.Lines, r.DiscountPercent, r.Room, r.Table);
        };
        summary.Controls.AddRange([totalCaption, totalLabel, orderButton, noteButton, payButton, splitBillButton, transferButton, mergeButton, discountButton, fireButton, delayFireButton, repeatButton, parkButton, dealsButton, reprintButton]);
        orderPanel.Controls.Add(basketList);
        orderPanel.Controls.Add(summary);
        orderPanel.Controls.Add(orderHeader);
        workspacePanel.BackColor = background;
        workspacePanel.Dock = DockStyle.Fill;
        workspacePanel.AutoScroll = false;

        menuPanel.Dock = DockStyle.None;
        menuPanel.Padding = new Padding(0, 0, 26, 0);
        var tabs = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, WrapContents = false };
        foreach (var item in new[] { "Popular", "Mains", "Drinks", "Desserts" })
        {
            var tab = ActionButton(item, 0, 0, 112, 38, item == category ? mint : panelLight);
            tab.ForeColor = item == category ? background : Color.White;
            tab.Click += (_, _) => { category = item; RenderProducts(); UpdateTabs(tabs); };
            tabs.Controls.Add(tab);
        }
        productGrid.Dock = DockStyle.Fill;
        productGrid.FlowDirection = FlowDirection.LeftToRight;
        productGrid.WrapContents = true;
        productGrid.AutoScroll = true;
        productGrid.Padding = new Padding(0, 14, 0, 0);
        menuPanel.Controls.Add(productGrid);
        menuPanel.Controls.Add(tabs);
        var barcodeSearch = new TextBox { Dock = DockStyle.Top, Height = 36, BackColor = panelLight, ForeColor = Color.White, PlaceholderText = "Scan barcode...", Visible = store.Features.BarcodeScanning || store.Features.QuickSaleMode };
        barcodeBox = barcodeSearch;
        barcodeSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(barcodeSearch.Text))
            {
                var match = products.FirstOrDefault(p => p.Barcode == barcodeSearch.Text.Trim());
                if (match != null) AddProduct(match);
                else MessageBox.Show("No product found with this barcode", "Barcode", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                barcodeSearch.Text = "";
            }
        };
        menuPanel.Controls.Add(barcodeSearch);
        qrLabel.Dock = DockStyle.Fill;
        qrLabel.ForeColor = Color.White;
        qrLabel.Font = new Font("Segoe UI Semibold", 9F);
        qrLabel.TextAlign = ContentAlignment.MiddleLeft;
        qrLabel.Cursor = Cursors.Hand;
        qrLabel.Click += (_, _) => ShowQROrders();
        qrPanel.Controls.Add(qrLabel);
        menuPanel.Controls.Add(qrPanel);
        guestDismissBtn.FlatAppearance.BorderSize = 0;
        guestDismissBtn.Click += (_, _) => { store.ClearGuestAlerts(); };
        guestBar.Controls.Add(guestLabel);
        guestBar.Controls.Add(guestDismissBtn);
        menuPanel.Controls.Add(guestBar);
        trainingLabel.Dock = DockStyle.Fill;
        trainingLabel.ForeColor = Color.White;
        trainingLabel.Font = new Font("Segoe UI Semibold", 10F);
        trainingLabel.TextAlign = ContentAlignment.MiddleLeft;
        trainingLabel.Text = "  TRAINING MODE — sales are NOT recorded";
        trainingBar.Controls.Add(trainingLabel);
        trainingBar.Visible = store.Features.TrainingMode;
        menuPanel.Controls.Add(trainingBar);
        workspacePanel.Controls.Add(menuPanel);
        workspacePanel.Controls.Add(orderPanel);
        workspacePanel.Resize += (_, _) => LayoutWorkspace(workspacePanel, menuPanel, orderPanel);
        content.Controls.Add(workspacePanel);
        LayoutWorkspace(workspacePanel, menuPanel, orderPanel);

        roomsPanel.Dock = DockStyle.Fill;
        roomsPanel.Padding = new Padding(0, 0, 26, 0);
        var roomTitle = new Label { Text = "Choose a room", AutoSize = true, Font = new Font("Segoe UI Semibold", 18F), Location = new Point(0, 0) };
        var roomHint = new Label { Text = "Select a room to see its table layout.", AutoSize = true, ForeColor = Color.FromArgb(143, 154, 171), Location = new Point(2, 35) };
        roomButtons.Location = new Point(0, 72);
        roomButtons.Size = new Size(roomsPanel.ClientSize.Width, 50);
        roomButtons.MaximumSize = new Size(roomsPanel.ClientSize.Width, 50);
        roomButtons.WrapContents = false;
        roomButtons.AutoScroll = true;
        tableMap.Dock = DockStyle.None;
        tableMap.AutoScroll = true;
        tableMap.AutoScrollMinSize = new Size(roomsPanel.ClientSize.Width, 500);
        roomsPanel.Resize += (_, _) => {
            roomButtons.Width = roomsPanel.ClientSize.Width;
            roomButtons.MaximumSize = new Size(roomsPanel.ClientSize.Width, 50);
            tableMap.SetBounds(0, 145, roomsPanel.ClientSize.Width, Math.Max(300, roomsPanel.ClientSize.Height - 145));
        };
        roomsPanel.Controls.Add(tableMap);
        roomsPanel.Controls.Add(roomButtons);
        roomsPanel.Controls.Add(roomHint);
        roomsPanel.Controls.Add(roomTitle);
        roomsPanel.Visible = false;
        roomButtons.BringToFront();
        content.Controls.Add(roomsPanel);

        viewPanel.Dock = DockStyle.Fill;
        viewPanel.BackColor = background;
        viewPanel.Visible = false;
        content.Controls.Add(viewPanel);

        workspacePanel.BringToFront();
        Controls.Add(content);
        Controls.Add(sidebar);
        ApplyPermissions();
        RenderRooms();
    }

    private static void LayoutWorkspace(Panel workspace, Panel menuPanel, Panel orderPanel)
    {
        var paymentWidth = Math.Clamp((int)(workspace.ClientSize.Width * 0.32), 275, 360);
        var menuWidth = Math.Max(0, workspace.ClientSize.Width - paymentWidth);
        orderPanel.SetBounds(menuWidth, 0, paymentWidth, workspace.ClientSize.Height);
        menuPanel.SetBounds(0, 0, menuWidth, workspace.ClientSize.Height);
        orderPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
        menuPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
    }

    private Button NavButton(string text, int top, bool active, Action? action = null)
    {
        var button = ActionButton((active ? "▸  " : "    ") + text, 0, top, 168, 42, active ? Color.FromArgb(47, 76, 82) : Color.Transparent);
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.ForeColor = active ? mint : Color.FromArgb(177, 188, 203);
        if (action != null) button.Click += (_, _) => action();
        return button;
    }

    private Button ActionButton(string text, int x, int y, int width, int height, Color color)
    {
        var button = new Button { Text = text, Location = new Point(x, y), Size = new Size(width, height), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        button.FlatAppearance.BorderSize = 0;
        button.Font = new Font("Segoe UI Semibold", 10F);
        return button;
    }

    private void RenderProducts()
    {
        productGrid.SuspendLayout();
        productGrid.Controls.Clear();
        var tileWidth = Math.Clamp((int)(productGrid.ClientSize.Width / 4.5), 140, 220);
        var tileHeight = (int)(tileWidth * 0.80);
        foreach (var product in products.Where(item => item.Category == category))
        {
            var tile = new ProductTile(product, tileWidth, tileHeight) { Margin = new Padding(0, 0, 14, 14) };
            tile.OutOfStock = Is86(store, product.Name);
            tile.Click += (_, _) => AddProduct(product);
            productGrid.Controls.Add(tile);
        }
        productGrid.ResumeLayout();
    }

    private void RenderRooms()
    {
        roomButtons.Controls.Clear();
        foreach (var room in store.Rooms)
        {
            var button = ActionButton(room.Name, 0, 0, 145, 40, room.Name == store.Rooms[0].Name ? mint : panelLight);
            button.ForeColor = room.Name == store.Rooms[0].Name ? background : Color.White;
            button.Click += (_, _) =>
            {
                selectedRoom = room.Name;
                selectedTable = "No table selected";
                orderLabel.Text = OrderContextText();
                RenderTableMap(room);
            };
            roomButtons.Controls.Add(button);
        }
        if (floorPlanEdit && !HasPermission("Manage rooms")) floorPlanEdit = false;
        if (floorPlanEdit)
        {
            var doneButton = ActionButton("Done", 0, 0, 145, 40, Color.FromArgb(60, 120, 90));
            doneButton.Click += (_, _) =>
            {
                floorPlanEdit = false;
                RenderRooms();
                var current = store.Rooms.FirstOrDefault(r => r.Name == selectedRoom);
                if (current != null) RenderTableMap(current);
            };
            roomButtons.Controls.Add(doneButton);
            var resetButton = ActionButton("Reset layout", 0, 0, 145, 40, Color.FromArgb(180, 90, 60));
            resetButton.Click += (_, _) =>
            {
                var current = floorMapRoom ?? (store.Rooms.Count > 0 ? store.Rooms[0] : null);
                if (current == null) return;
                if (MessageBox.Show($"Reset table positions in {current.Name} to the default layout?\n(Table names are kept)", "Reset layout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                if (store.FloorPlan.TryGetValue(current.Name, out var plan))
                {
                    foreach (var entry in plan.Values) { entry.X = null; entry.Y = null; }
                    store.SaveFloorPlan();
                    if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Floor plan", $"{current.Name}: reset to default");
                }
                RenderTableMap(current);
            };
            roomButtons.Controls.Add(resetButton);
        }
        roomButtons.BringToFront();
        if (store.Rooms.Count > 0)
            RenderTableMap(store.Rooms[0]);
    }

    private void RenderTableMap(Room room)
    {
        floorMapRoom = room;
        tableMap.Controls.Clear();
        var title = new Label { Text = room.Name + "  •  " + room.TableCount + " tables" + (floorPlanEdit ? "  •  EDITING: drag to move, double-click to rename" : ""), AutoSize = true, Font = new Font("Segoe UI Semibold", 15F), Location = new Point(4, 5), ForeColor = floorPlanEdit ? Color.Orange : SystemColors.ControlText };
        tableMap.Controls.Add(title);
        for (var index = 1; index <= room.TableCount; index++)
        {
            var tableIndex = index;
            var tableName = TableName(room.Name, tableIndex);
            var openOrder = store.GetOpenOrder(room.Name, tableName);
            var isTaken = openOrder != null;
            var status = isTaken ? "Taken" : "Available";
            var reservation = store.Features.Reservations
                ? store.Reservations.FirstOrDefault(r => r.Status == "Booked" && r.Room == room.Name && r.Table == tableName && r.When.Date == DateTime.Today)
                : null;
            if (reservation != null && !isTaken) status = $"{reservation.When:HH:mm} booked";
            var color = isTaken ? Color.FromArgb(180, 90, 60) : reservation != null ? Color.FromArgb(150, 110, 50) : panelLight;
            var table = ActionButton(floorPlanEdit ? tableName : $"{tableName}\n{status}", 0, 0, 128, 72, color);
            table.Location = TableLocation(room.Name, tableIndex);
            table.ForeColor = Color.White;
            if (floorPlanEdit)
            {
                Point dragStart = default;
                Point origLoc = default;
                var dragMoved = false;
                var lastLeftUp = DateTime.MinValue;
                table.MouseDown += (_, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        RenameTable(room, tableIndex, tableName, () => RenderTableMap(room));
                        return;
                    }
                    if (e.Button != MouseButtons.Left) return;
                    dragStart = e.Location;
                    origLoc = table.Location;
                    dragMoved = false;
                    table.BringToFront();
                    table.Capture = true;
                };
                table.MouseMove += (_, _) =>
                {
                    if (!table.Capture) return;
                    var loc = tableMap.PointToClient(Cursor.Position) - new Size(dragStart);
                    loc.X = Math.Clamp(loc.X, 0, Math.Max(0, tableMap.ClientSize.Width - table.Width));
                    loc.Y = Math.Clamp(loc.Y, 0, Math.Max(0, tableMap.ClientSize.Height - table.Height));
                    table.Location = loc;
                    if (Math.Abs(loc.X - origLoc.X) > 4 || Math.Abs(loc.Y - origLoc.Y) > 4) dragMoved = true;
                };
                table.MouseUp += (_, e) =>
                {
                    if (e.Button != MouseButtons.Left) return;
                    table.Capture = false;
                    if (dragMoved)
                    {
                        SaveTableSpot(room.Name, tableIndex, table.Location);
                        lastLeftUp = DateTime.MinValue;
                        return;
                    }
                    var now = DateTime.Now;
                    if (lastLeftUp != DateTime.MinValue && (now - lastLeftUp).TotalMilliseconds <= 450)
                    {
                        lastLeftUp = DateTime.MinValue;
                        RenameTable(room, tableIndex, tableName, () => RenderTableMap(room));
                    }
                    else lastLeftUp = now;
                };
            }
            else table.Click += (_, _) =>
            {
                selectedRoom = room.Name;
                selectedTable = tableName;
                basket.Clear();
                activeSeat = 1;
                hasOpenOrder = false;
                    if (store.GetOpenOrder(room.Name, tableName) is { } existing)
                    {
                        hasOpenOrder = true;
                        foreach (var item in existing.Items)
                        {
                            var product = products.FirstOrDefault(p => p.Name == item.Product);
                            if (product != null)
                            {
                                var idx = basket.FindIndex(b => b.Product == product && b.Note == item.Note && b.Modifiers == item.Modifiers && (!store.Features.SeatOrdering || b.Seat == item.Seat));
                                if (idx >= 0) basket[idx] = (product, basket[idx].Count + item.Quantity, basket[idx].Course, basket[idx].Note, item.Seat, JoinMods(basket[idx].Modifiers, item.Modifiers));
                                else basket.Add((product, item.Quantity, item.Course, item.Note, item.Seat, item.Modifiers));
                            }
                        }
                    }
                UpdateOrderButtons();
                RenderBasket();
                orderLabel.Text = OrderContextText();
                ShowOrder();
            };
            tableMap.Controls.Add(table);
        }
    }

    private string TableName(string roomName, int index) =>
        store.FloorPlan.TryGetValue(roomName, out var plan) && plan.TryGetValue(index.ToString(), out var entry) && !string.IsNullOrWhiteSpace(entry.Name)
            ? entry.Name! : "Table " + index;

    private Point TableLocation(string roomName, int index)
    {
        if (store.FloorPlan.TryGetValue(roomName, out var plan) && plan.TryGetValue(index.ToString(), out var entry) && entry.X is int x && entry.Y is int y)
            return new Point(x, y);
        var i = index - 1;
        return new Point(10 + (i % 4) * 150, 48 + (i / 4) * 98);
    }

    private FloorTable FloorEntry(string roomName, int index)
    {
        if (!store.FloorPlan.TryGetValue(roomName, out var plan)) store.FloorPlan[roomName] = plan = [];
        if (!plan.TryGetValue(index.ToString(), out var entry)) plan[index.ToString()] = entry = new FloorTable();
        return entry;
    }

    private void SaveTableSpot(string roomName, int index, Point location)
    {
        var entry = FloorEntry(roomName, index);
        entry.X = location.X;
        entry.Y = location.Y;
        store.SaveFloorPlan();
        if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Floor plan", $"{roomName}/{TableName(roomName, index)} → ({location.X},{location.Y})");
    }

    private void RenameTable(Room room, int index, string currentName, Action refresh)
    {
        if (store.GetOpenOrder(room.Name, currentName) != null)
        {
            MessageBox.Show("This table has an open order - pay or transfer it before renaming.", "Rename table", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var form = new Form { Text = "Rename table", Size = new Size(320, 170), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var box = new TextBox { Location = new Point(15, 15), Size = new Size(285, 30), BackColor = panelLight, ForeColor = Color.White, Text = currentName };
        var ok = new Button { Text = "Rename", Location = new Point(15, 60), Size = new Size(135, 34), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        ok.FlatAppearance.BorderSize = 0;
        ok.Click += (_, _) => form.DialogResult = DialogResult.OK;
        var cancel = new Button { Text = "Cancel", Location = new Point(165, 60), Size = new Size(135, 34), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        cancel.FlatAppearance.BorderSize = 0;
        cancel.Click += (_, _) => form.DialogResult = DialogResult.Cancel;
        form.Controls.AddRange([box, ok, cancel]);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        if (form.ShowDialog() != DialogResult.OK) return;
        var newName = box.Text.Trim();
        if (string.IsNullOrWhiteSpace(newName) || newName == currentName) return;
        if (store.GetOpenOrder(room.Name, newName) != null)
        {
            MessageBox.Show($"{newName} already has an open order.", "Rename table", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        for (var i = 1; i <= room.TableCount; i++)
        {
            if (i != index && TableName(room.Name, i) == newName)
            {
                MessageBox.Show($"Another table is already named {newName}.", "Rename table", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
        FloorEntry(room.Name, index).Name = newName;
        store.SaveFloorPlan();
        if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Rename", $"{room.Name}: {currentName} → {newName}");
        refresh();
    }

    private void ShowOrder()
    {
        viewPanel.Visible = false;
        mainHeader.Visible = true;
        workspacePanel.Visible = true;
        menuPanel.Visible = true;
        roomsPanel.Visible = false;
        workspacePanel.BringToFront();
    }

    private void ShowRooms()
    {
        viewPanel.Visible = false;
        mainHeader.Visible = true;
        workspacePanel.Visible = false;
        menuPanel.Visible = false;
        roomsPanel.Visible = true;
        RenderRooms();
        roomsPanel.BringToFront();
    }

    private void ShowView(Panel content, string title, Action? onBack = null)
    {
        mainHeader.Visible = false;
        workspacePanel.Visible = false;
        menuPanel.Visible = false;
        roomsPanel.Visible = false;
        viewPanel.Controls.Clear();
        var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = panel, Padding = new Padding(16, 0, 16, 0) };
        var titleLabel = new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 16F), ForeColor = Color.White, Location = new Point(16, 14) };
        var backBtn = new Button { Text = "\u2190 Back", Location = new Point(viewPanel.ClientSize.Width - 120, 12), Size = new Size(100, 34), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        backBtn.FlatAppearance.BorderSize = 0;
        backBtn.Click += (_, _) => { if (onBack != null) onBack(); else ShowOrder(); };
        header.Controls.Add(titleLabel);
        header.Controls.Add(backBtn);
        content.Dock = DockStyle.Fill;
        content.Padding = new Padding(16);
        content.BackColor = background;
        viewPanel.Controls.Add(content);
        viewPanel.Controls.Add(header);
        viewPanel.Visible = true;
        viewPanel.BringToFront();
    }

    private void ShowProducts()
    {
        if (!HasPermission("Manage products")) return;
        var panel = new Panel { Dock = DockStyle.Fill };
        var form = new ProductManagerForm(background, panelLight, mint, products, updatedProducts =>
        {
            products.Clear();
            products.AddRange(updatedProducts);
            store.SaveProducts(products);
            RenderProducts();
        }, store.Features, store.ProductVariants, store.SaveProductVariants);
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        panel.Controls.Add(form);
        form.Show();
        ShowView(panel, "Manage products", ShowSettings);
    }

    private static string JoinMods(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a)) return b ?? "";
        if (string.IsNullOrWhiteSpace(b)) return a;
        return string.Join(", ", new[] { a, b }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    private string CourseLabel(int n)
    {
        var names = store.CourseNames;
        return n > 0 && names.Count >= n && !string.IsNullOrWhiteSpace(names[n - 1]) ? names[n - 1] : $"C{n}";
    }

    private void EditBasketItemChips((Product Product, int Count, int Course, string Note, int Seat, string Modifiers) item)
    {
        var idx = basket.IndexOf(item);
        if (idx < 0) return;
        using var dlg = new Form
        {
            Text = item.Product.Name + " - Quick options",
            Size = new Size(440, 460),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = background,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };
        var currentMods = (item.Modifiers ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        decimal ModPrice(string name) => store.ModifierGroups.SelectMany(g => g.Options).FirstOrDefault(o => o.Name == name)?.Price ?? 0m;
        var oldPrice = currentMods.Sum(ModPrice);
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoScroll = true, WrapContents = false, BackColor = background, Padding = new Padding(10) };
        var toggles = new List<(Button btn, string name, bool on)>();
        foreach (var group in store.ModifierGroups)
        {
            flow.Controls.Add(new Label { Text = group.Name, AutoSize = true, Font = new Font("Segoe UI Semibold", 10F), ForeColor = mint, Margin = new Padding(0, 8, 0, 2) });
            foreach (var opt in group.Options)
            {
                var on = currentMods.Contains(opt.Name);
                var name = opt.Name;
                var btn = new Button
                {
                    Text = $"{opt.Name}  {opt.Price:+0.00;-0.00;}",
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(14, 3, 0, 3),
                    BackColor = on ? mint : panelLight,
                    ForeColor = on ? background : Color.White,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btn.FlatAppearance.BorderSize = 0;
                var entry = (btn, name, on);
                toggles.Add(entry);
                btn.Click += (_, _) =>
                {
                    var i = toggles.FindIndex(t => t.btn == btn);
                    var t = toggles[i];
                    toggles[i] = (t.btn, t.name, !t.on);
                    btn.BackColor = !t.on ? mint : panelLight;
                    btn.ForeColor = !t.on ? background : Color.White;
                };
                flow.Controls.Add(btn);
            }
        }
        flow.Controls.Add(new Label { Text = "Free note", AutoSize = true, Font = new Font("Segoe UI Semibold", 10F), ForeColor = mint, Margin = new Padding(0, 12, 0, 2) });
        var noteBox = new TextBox { Text = item.Note ?? "", Width = 370, BackColor = panelLight, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(4, 2, 0, 2) };
        flow.Controls.Add(noteBox);
        var okBtn = new Button { Text = "Apply", Size = new Size(110, 36), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 14, 0, 0) };
        okBtn.FlatAppearance.BorderSize = 0;
        okBtn.Click += (_, _) =>
        {
            var selected = toggles.Where(t => t.on).Select(t => t.name).ToList();
            var newPrice = selected.Sum(ModPrice);
            var delta = newPrice - oldPrice;
            var newProduct = basket[idx].Product with { Price = Math.Max(0m, basket[idx].Product.Price + delta) };
            basket[idx] = (newProduct, basket[idx].Count, basket[idx].Course, noteBox.Text.Trim(), basket[idx].Seat, string.Join(", ", selected));
            dlg.DialogResult = DialogResult.OK;
        };
        flow.Controls.Add(okBtn);
        dlg.Controls.Add(flow);
        dlg.ShowDialog();
        RenderBasket();
    }

    private List<SoldLine> BasketToLines() =>
        basket.Select(item => new SoldLine(item.Product.Name, item.Product.Family, item.Count, item.Product.Price, item.Product.Category, ResolvePrinter(item.Product), item.Note, Modifiers: item.Modifiers, Seat: item.Seat, Course: item.Course)).ToList();

    private void LoadLinesIntoBasket(IEnumerable<SoldLine> lines)
    {
        foreach (var li in lines)
        {
            if (li.Quantity <= 0) continue;
            var p = products.FirstOrDefault(x => x.Name == li.Product)
                ?? new Product(li.Product, li.Category, li.UnitPrice, "#777777", li.Family, "", li.PrinterDestination, "");
            var prod = p.Price == li.UnitPrice ? p : p with { Price = li.UnitPrice };
            var idx = basket.FindIndex(b => b.Product.Name == prod.Name && b.Note == li.Note && b.Modifiers == li.Modifiers);
            if (idx >= 0) basket[idx] = (prod, basket[idx].Count + li.Quantity, basket[idx].Course, basket[idx].Note, basket[idx].Seat, basket[idx].Modifiers);
            else basket.Add((prod, li.Quantity, store.Features.CourseManagement ? li.Course : 0, li.Note, store.Features.SeatOrdering && li.Seat > 0 ? li.Seat : (store.Features.SeatOrdering ? activeSeat : 0), li.Modifiers));
        }
        RenderBasket();
        UpdateOrderButtons();
    }

    private void RepeatLastOrder()
    {
        var candidates = new List<(string label, Sale sale)>();
        var seen = new HashSet<int>();
        Sale? Add(Sale? s, string label)
        {
            if (s != null && seen.Add(s.PaymentNumber) && s.Items is { Count: > 0 }) candidates.Add((label, s));
            return s;
        }
        Add(store.Sales.LastOrDefault(s => !s.IsRefund && s.Table == selectedTable && s.Items is { Count: > 0 }), $"Last at {selectedTable}");
        Add(store.Sales.LastOrDefault(s => !s.IsRefund && s.Operator == currentOperator.Name && s.Items is { Count: > 0 }), $"My last sale");
        if (store.Features.CustomerDatabase)
        {
            foreach (var c in store.Customers)
                Add(store.Sales.LastOrDefault(s => !s.IsRefund && s.CustomerName == c.Name && s.Items is { Count: > 0 }), $"{c.Name}'s usual");
        }
        if (candidates.Count == 0)
        {
            MessageBox.Show("No previous orders found yet.", "Repeat order", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dlg = new Form { Text = "Repeat an order", Size = new Size(460, 230), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var info = new Label { Text = "Loads the selected order into the basket (nothing prints until you press Order):", AutoSize = true, MaximumSize = new Size(410, 0), Location = new Point(14, 12), ForeColor = Color.FromArgb(143, 154, 171) };
        var combo = new ComboBox { Location = new Point(14, 52), Width = 410, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
        foreach (var (label, sale) in candidates)
            combo.Items.Add($"{label}  •  {sale.Time:g}  •  {sale.Total:C}");
        combo.SelectedIndex = 0;
        var addBtn = new Button { Text = "Load into basket", Location = new Point(14, 96), Size = new Size(180, 36), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        addBtn.FlatAppearance.BorderSize = 0;
        addBtn.Click += (_, _) =>
        {
            LoadLinesIntoBasket(candidates[combo.SelectedIndex].sale.Items!);
            dlg.DialogResult = DialogResult.OK;
        };
        var cancel = new Button { Text = "Cancel", Location = new Point(204, 96), Size = new Size(120, 36), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        cancel.FlatAppearance.BorderSize = 0;
        cancel.Click += (_, _) => dlg.DialogResult = DialogResult.Cancel;
        dlg.Controls.AddRange([info, combo, addBtn, cancel]);
        dlg.AcceptButton = addBtn;
        dlg.CancelButton = cancel;
        dlg.ShowDialog();
    }

    private void ShowParkRecall()
    {
        using var dlg = new Form { Text = "Park & recall", Size = new Size(500, 440), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var parkBtn = new Button { Text = "Park current order", Location = new Point(14, 12), Size = new Size(200, 36), BackColor = Color.FromArgb(130, 100, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        parkBtn.FlatAppearance.BorderSize = 0;
        var hint = new Label { Text = "Parks it off the table; recall it any time into the current table.", AutoSize = true, Location = new Point(222, 22), ForeColor = Color.FromArgb(143, 154, 171) };
        var list = new ListView { Location = new Point(14, 60), Size = new Size(455, 260), View = View.Details, BackColor = panelLight, ForeColor = Color.White, FullRowSelect = true, GridLines = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };
        list.Columns.AddRange([new ColumnHeader { Text = "Parked", Width = 130 }, new ColumnHeader { Text = "From", Width = 150 }, new ColumnHeader { Text = "Items", Width = 60 }, new ColumnHeader { Text = "By", Width = 90 }]);
        void RefreshList()
        {
            list.Items.Clear();
            foreach (var p in store.ParkedOrders.OrderByDescending(p => p.Time))
                list.Items.Add(new ListViewItem([p.Time.ToString("dd/MM HH:mm"), $"{p.Room} / {p.Table}", p.Items.Sum(i => i.Quantity).ToString(), p.Operator]) { Tag = p.Id });
        }
        RefreshList();
        parkBtn.Click += (_, _) =>
        {
            List<SoldLine> lines;
            if (hasOpenOrder)
            {
                var order = store.GetOpenOrder(selectedRoom, selectedTable);
                if (order == null) return;
                lines = order.Items;
                store.RemoveOpenOrder(selectedRoom, selectedTable);
                hasOpenOrder = false;
            }
            else lines = BasketToLines();
            if (lines.Count == 0)
            {
                MessageBox.Show("Nothing to park - the order is empty.", "Park", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            store.ParkedOrders.Add(new ParkedOrder($"P{DateTime.Now:yyyyMMddHHmmssfff}", selectedRoom, selectedTable, currentOperator.Name, DateTime.Now, lines));
            store.SaveParkedOrders(store.ParkedOrders);
            basket.Clear();
            activeSeat = 1;
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Park order", $"{selectedRoom}/{selectedTable}: {lines.Sum(l => l.Quantity)} items");
            RenderBasket();
            UpdateOrderButtons();
            orderLabel.Text = OrderContextText();
            RefreshList();
        };
        var recallBtn = new Button { Text = "Recall selected", Location = new Point(14, 332), Size = new Size(160, 36), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        recallBtn.FlatAppearance.BorderSize = 0;
        recallBtn.Click += (_, _) =>
        {
            if (list.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select a parked order first.", "Recall", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var id = (string?)list.SelectedItems[0].Tag;
            var parked = store.ParkedOrders.FirstOrDefault(p => p.Id == id);
            if (parked == null) return;
            if (hasOpenOrder || basket.Count > 0)
            {
                if (MessageBox.Show("The current table has items. Add the parked order on top of them?", "Recall", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            }
            LoadLinesIntoBasket(parked.Items);
            store.ParkedOrders.Remove(parked);
            store.SaveParkedOrders(store.ParkedOrders);
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Recall order", $"{parked.Room}/{parked.Table} → {selectedRoom}/{selectedTable}");
            RefreshList();
        };
        var clearBtn = new Button { Text = "Discard", Location = new Point(184, 332), Size = new Size(110, 36), BackColor = Color.FromArgb(180, 40, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        clearBtn.FlatAppearance.BorderSize = 0;
        clearBtn.Click += (_, _) =>
        {
            if (list.SelectedItems.Count == 0) return;
            if (MessageBox.Show("Discard the selected parked order?", "Recall", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var id = (string?)list.SelectedItems[0].Tag;
            var parked = store.ParkedOrders.FirstOrDefault(p => p.Id == id);
            if (parked == null) return;
            store.ParkedOrders.Remove(parked);
            store.SaveParkedOrders(store.ParkedOrders);
            RefreshList();
        };
        dlg.Controls.AddRange([parkBtn, hint, list, recallBtn, clearBtn]);
        dlg.ShowDialog();
        UpdateOrderButtons();
    }

    private void ShowDealsDialog()
    {
        if (store.ComboDeals.Count == 0)
        {
            MessageBox.Show("No combo deals set up yet. Add them in Settings → Ordering → Manage combo deals.", "Deals", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dlg = new Form { Text = "Combo deals", Size = new Size(460, 360), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var list = new ListView { Location = new Point(14, 14), Size = new Size(415, 230), View = View.Details, BackColor = panelLight, ForeColor = Color.White, FullRowSelect = true, GridLines = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };
        list.Columns.AddRange([new ColumnHeader { Text = "Deal", Width = 200 }, new ColumnHeader { Text = "Price", Width = 80 }, new ColumnHeader { Text = "Includes", Width = 130 }]);
        foreach (var d in store.ComboDeals)
            list.Items.Add(new ListViewItem([d.Name, d.Price.ToString("C"), string.Join(", ", d.Items.Select(i => $"{i.Qty}× {i.Product}"))]) { Tag = d.Name });
        if (list.Items.Count > 0) list.Items[0].Selected = true;
        var addBtn = new Button { Text = "Add to order", Location = new Point(14, 260), Size = new Size(160, 36), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        addBtn.FlatAppearance.BorderSize = 0;
        addBtn.Click += (_, _) =>
        {
            if (list.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select a deal first.", "Deals", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var name = (string?)list.SelectedItems[0].Tag;
            var deal = store.ComboDeals.FirstOrDefault(d => d.Name == name);
            if (deal == null) return;
            var seat = store.Features.SeatOrdering ? activeSeat : 0;
            var course = store.Features.CourseManagement ? 1 : 0;
            basket.Add((new Product(deal.Name, "Deals", deal.Price, "#C94C7A", "Deals", "", "", ""), 1, course, "", seat, ""));
            foreach (var comp in deal.Items)
            {
                var cp = products.FirstOrDefault(x => x.Name == comp.Product);
                if (cp == null) continue;
                basket.Add((cp with { Price = 0 }, comp.Qty, course, $"in {deal.Name}", seat, ""));
            }
            RenderBasket();
            dlg.DialogResult = DialogResult.OK;
        };
        var close = new Button { Text = "Close", Location = new Point(184, 260), Size = new Size(110, 36), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        close.FlatAppearance.BorderSize = 0;
        close.Click += (_, _) => dlg.DialogResult = DialogResult.Cancel;
        dlg.Controls.AddRange([list, addBtn, close]);
        dlg.AcceptButton = addBtn;
        dlg.ShowDialog();
    }

    private void QuickSaleStart()
    {
        if (basket.Count > 0 || hasOpenOrder)
        {
            if (MessageBox.Show("Clear the current order and start a quick sale?", "Quick sale", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (hasOpenOrder && MessageBox.Show("An order is open at the current table - it will stay open (not deleted). Continue?", "Quick sale", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            basket.Clear();
            RenderBasket();
        }
        selectedRoom = "Counter";
        selectedTable = "Counter " + DateTime.Now.ToString("HHmm");
        activeSeat = 1;
        hasOpenOrder = false;
        ShowOrder();
        orderLabel.Text = OrderContextText();
        UpdateOrderButtons();
        barcodeBox?.Focus();
    }

    private void LaunchKiosk()
    {
        if (kioskForm != null && !kioskForm.IsDisposed) { kioskForm.Activate(); return; }
        kioskForm = new KioskForm(background, panelLight, mint, products.ToList(), ResolvePrinter, lines =>
        {
            var room = "Kiosk";
            var table = "Kiosk " + DateTime.Now.ToString("HHmmss");
            var orderNum = store.NextOrderNumber();
            store.AddOpenOrder(new OpenOrder(room, table, "Kiosk", DateTime.Now, lines, "", orderNum, orderNum, OrderType: "Takeaway", SeatCount: 0, FiredCourse: 0, Source: "Kiosk", DeliveryAddress: ""));
            PrintTickets(lines, orderNumber: orderNum, orderType: "Takeaway", room: room, table: table);
            if (store.Features.AuditLog) store.LogAudit("Kiosk", "Order sent", $"{table}: {lines.Sum(l => l.Quantity)} items, order #{orderNum}");
        });
        kioskForm.FormClosed += (_, _) => kioskForm = null;
        kioskForm.Show();
    }

    private void ShowCustomerDisplay()
    {
        if (custDisplay != null && !custDisplay.IsDisposed) { custDisplay.Activate(); return; }
        custDisplay = new CustomerDisplayForm(background, mint);
        custDisplay.FormClosed += (_, _) => custDisplay = null;
        custDisplay.Show();
        custDisplay.SetOrder($"{selectedRoom} / {selectedTable}", basket.Select(i => ($"{i.Count} \u00d7 {i.Product.Name}", i.Product.Price * i.Count)).ToList(), NetTotal());
    }

    private void ManageCourseNames()
    {
        using var dlg = new Form { Text = "Course names", Size = new Size(430, 360), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var info = new Label { Text = "Custom names shown on buttons, KDS and tickets. Leave blank to keep C1, C2…", AutoSize = true, MaximumSize = new Size(380, 0), Location = new Point(14, 12), ForeColor = Color.FromArgb(143, 154, 171) };
        dlg.Controls.Add(info);
        var boxes = new List<TextBox>();
        for (var i = 1; i <= 5; i++)
        {
            var lbl = new Label { Text = $"Course {i}", AutoSize = true, Location = new Point(14, 62 + (i - 1) * 42) };
            var box = new TextBox { Location = new Point(110, 58 + (i - 1) * 42), Size = new Size(260, 28), BackColor = panelLight, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            if (store.CourseNames.Count >= i) box.Text = store.CourseNames[i - 1];
            boxes.Add(box);
            dlg.Controls.AddRange([lbl, box]);
        }
        var save = new Button { Text = "Save", Location = new Point(14, 272), Size = new Size(110, 36), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        save.FlatAppearance.BorderSize = 0;
        save.Click += (_, _) =>
        {
            var names = boxes.Select(b => b.Text.Trim()).ToList();
            store.SaveCourseNames(names);
            TicketLayout.CourseNames = names.ToArray();
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Course names", string.Join(", ", names.Where(n => n.Length > 0)));
            dlg.DialogResult = DialogResult.OK;
        };
        var cancel = new Button { Text = "Cancel", Location = new Point(134, 272), Size = new Size(110, 36), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        cancel.FlatAppearance.BorderSize = 0;
        cancel.Click += (_, _) => dlg.DialogResult = DialogResult.Cancel;
        dlg.Controls.AddRange([save, cancel]);
        dlg.AcceptButton = save;
        dlg.CancelButton = cancel;
        dlg.ShowDialog();
        RenderBasket();
        UpdateOrderButtons();
    }

    private void ManageComboDeals()
    {
        using var dlg = new Form { Text = "Combo deals", Size = new Size(560, 470), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.Sizable, MaximizeBox = false, MinimizeBox = false };
        var list = new ListView { Location = new Point(14, 14), Size = new Size(250, 380), View = View.Details, BackColor = panelLight, ForeColor = Color.White, FullRowSelect = true, GridLines = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };
        list.Columns.Add(new ColumnHeader { Text = "Deal", Width = 160 });
        list.Columns.Add(new ColumnHeader { Text = "Price", Width = 80 });
        void RefreshDeals()
        {
            list.Items.Clear();
            foreach (var d in store.ComboDeals)
                list.Items.Add(new ListViewItem([d.Name, d.Price.ToString("C")]) { Tag = d.Name });
        }
        RefreshDeals();

        var nameLabel = new Label { Text = "Name", AutoSize = true, Location = new Point(286, 16) };
        var nameBox = new TextBox { Location = new Point(286, 36), Size = new Size(240, 28), BackColor = panelLight, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        var priceLabel = new Label { Text = "Bundle price", AutoSize = true, Location = new Point(286, 70) };
        var priceBox = new NumericUpDown { Location = new Point(286, 90), Size = new Size(120, 28), BackColor = panelLight, ForeColor = Color.White, DecimalPlaces = 2, Maximum = 9999, Minimum = 0 };
        var compLabel = new Label { Text = "Included products", AutoSize = true, Location = new Point(286, 126) };
        var comps = new ListView { Location = new Point(286, 146), Size = new Size(240, 150), View = View.Details, BackColor = panelLight, ForeColor = Color.White, FullRowSelect = true, GridLines = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };
        comps.Columns.Add(new ColumnHeader { Text = "Product", Width = 150 });
        comps.Columns.Add(new ColumnHeader { Text = "Qty", Width = 50 });
        var prodCombo = new ComboBox { Location = new Point(286, 304), Size = new Size(150, 28), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
        prodCombo.Items.AddRange(products.Select(p => (object)p.Name).ToArray());
        if (prodCombo.Items.Count > 0) prodCombo.SelectedIndex = 0;
        var qtyBox = new NumericUpDown { Location = new Point(444, 304), Size = new Size(50, 28), BackColor = panelLight, ForeColor = Color.White, Minimum = 1, Maximum = 99, Value = 1 };
        var addComp = new Button { Text = "+", Location = new Point(500, 303), Size = new Size(26, 28), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        addComp.FlatAppearance.BorderSize = 0;
        addComp.Click += (_, _) =>
        {
            if (prodCombo.SelectedItem is not string pn) return;
            comps.Items.Add(new ListViewItem([pn, ((int)qtyBox.Value).ToString()]));
        };
        var delComp = new Button { Text = "−", Location = new Point(500, 336), Size = new Size(26, 28), BackColor = Color.FromArgb(180, 40, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        delComp.FlatAppearance.BorderSize = 0;
        delComp.Click += (_, _) => { if (comps.SelectedItems.Count > 0) comps.SelectedItems[0].Remove(); };

        var newBtn = new Button { Text = "New deal", Location = new Point(14, 404), Size = new Size(110, 36), BackColor = Color.FromArgb(70, 90, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        newBtn.FlatAppearance.BorderSize = 0;
        newBtn.Click += (_, _) => { nameBox.Text = ""; priceBox.Value = 0; comps.Items.Clear(); };
        var saveBtn = new Button { Text = "Save deal", Location = new Point(134, 404), Size = new Size(120, 36), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        saveBtn.FlatAppearance.BorderSize = 0;
        saveBtn.Click += (_, _) =>
        {
            var nm = nameBox.Text.Trim();
            if (nm.Length == 0) { MessageBox.Show("Enter a deal name.", "Combo deals", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (comps.Items.Count == 0) { MessageBox.Show("Add at least one included product.", "Combo deals", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var items = comps.Items.Cast<ListViewItem>().Select(i => new ComboComponent(i.SubItems[0].Text, int.Parse(i.SubItems[1].Text))).ToList();
            store.ComboDeals.RemoveAll(d => d.Name == nm);
            store.ComboDeals.Add(new ComboDeal(nm, priceBox.Value, items));
            store.SaveComboDeals(store.ComboDeals);
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Combo deal", $"{nm} {priceBox.Value:C}");
            RefreshDeals();
            UpdateOrderButtons();
        };
        var loadBtn = new Button { Text = "Load selected", Location = new Point(264, 404), Size = new Size(120, 36), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        loadBtn.FlatAppearance.BorderSize = 0;
        loadBtn.Click += (_, _) =>
        {
            if (list.SelectedItems.Count == 0) return;
            var d = store.ComboDeals.FirstOrDefault(x => x.Name == (string?)list.SelectedItems[0].Tag);
            if (d == null) return;
            nameBox.Text = d.Name;
            priceBox.Value = d.Price;
            comps.Items.Clear();
            foreach (var c in d.Items) comps.Items.Add(new ListViewItem([c.Product, c.Qty.ToString()]));
        };
        var delBtn = new Button { Text = "Delete deal", Location = new Point(394, 404), Size = new Size(132, 36), BackColor = Color.FromArgb(180, 40, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        delBtn.FlatAppearance.BorderSize = 0;
        delBtn.Click += (_, _) =>
        {
            if (list.SelectedItems.Count == 0) return;
            if (MessageBox.Show("Delete the selected deal?", "Combo deals", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var nm = (string?)list.SelectedItems[0].Tag;
            store.ComboDeals.RemoveAll(d => d.Name == nm);
            store.SaveComboDeals(store.ComboDeals);
            RefreshDeals();
            UpdateOrderButtons();
        };
        dlg.Controls.AddRange([list, nameLabel, nameBox, priceLabel, priceBox, compLabel, comps, prodCombo, qtyBox, addComp, delComp, newBtn, saveBtn, loadBtn, delBtn]);
        dlg.ShowDialog();
        UpdateOrderButtons();
    }

    private void RenderBasket()
    {
        basketList.SuspendLayout();
        basketList.Controls.Clear();
        if (basket.Count == 0)
        {
            basketList.Controls.Add(new Label
            {
                Text = "No products selected yet\nChoose products from the menu.",
                AutoSize = true,
                ForeColor = Color.FromArgb(143, 154, 171),
                Margin = new Padding(0, 8, 0, 8)
            });
        }
        foreach (var item in basket)
        {
            var rowWidth = Math.Max(220, basketList.ClientSize.Width - 8);
            var hasNote = !string.IsNullOrWhiteSpace(item.Note);
            var hasMods = !string.IsNullOrWhiteSpace(item.Modifiers);
            var row = new Panel { Width = rowWidth, Height = hasNote && hasMods ? 96 : hasNote || hasMods ? 78 : 58, BackColor = panelLight, Margin = new Padding(0, 0, 0, 8), Padding = new Padding(12, 8, 8, 6) };
            var seatTag = store.Features.SeatOrdering && item.Seat > 0 ? $"[S{item.Seat}] " : "";
            var name = new Label { Text = $"{seatTag}{item.Count} \u00d7 {item.Product.Name}", AutoSize = true, Location = new Point(10, 8) };
            var price = new Label { Text = $"{item.Product.Price:C} each  \u2022  {(item.Product.Price * item.Count):C}", AutoSize = true, ForeColor = mint, Location = new Point(10, 30) };
            var subY = 52;
            if (hasMods)
            {
                var modLabel = new Label { Text = "+ " + item.Modifiers, AutoSize = true, ForeColor = Color.FromArgb(140, 200, 230), Location = new Point(10, subY), Font = new Font("Segoe UI", 8F), MaximumSize = new Size(rowWidth - 130, 0) };
                row.Controls.Add(modLabel);
                subY += 18;
            }
            if (hasNote)
            {
                var noteLabel = new Label { Text = "\u270e " + item.Note, AutoSize = true, ForeColor = Color.FromArgb(200, 180, 100), Location = new Point(10, subY), Font = new Font("Segoe UI", 8F), MaximumSize = new Size(rowWidth - 130, 0) };
                row.Controls.Add(noteLabel);
            }
            var rightEdge = hasOpenOrder ? rowWidth - 66 : rowWidth - 38;
            var cursorX = rightEdge;
            if (store.Features.CourseManagement)
            {
                var course = item.Course;
                var ctext = course > 0 ? CourseLabel(course) : "C";
                var cw = Math.Max(34, TextRenderer.MeasureText(ctext, new Font("Segoe UI Semibold", 8F)).Width + 14);
                cursorX = rightEdge - 6 - cw;
                var courseBtn = new Button { Text = ctext, Size = new Size(cw, 28), Location = new Point(cursorX, 13), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(150, 100, 50), ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 8F) };
                courseBtn.FlatAppearance.BorderSize = 0;
                courseBtn.Click += (_, _) =>
                {
                    var idx = basket.IndexOf(item);
                    if (idx < 0) return;
                    var next = basket[idx].Course >= 5 ? 1 : basket[idx].Course + 1;
                    basket[idx] = (basket[idx].Product, basket[idx].Count, next, basket[idx].Note, basket[idx].Seat, basket[idx].Modifiers);
                    RenderBasket();
                };
                row.Controls.Add(courseBtn);
            }
            cursorX -= 40;
            var chipBtn = new Button { Text = "\u270e+", Size = new Size(34, 28), Location = new Point(cursorX, 13), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(70, 90, 130), ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 8F) };
            chipBtn.FlatAppearance.BorderSize = 0;
            chipBtn.Click += (_, _) => EditBasketItemChips(item);
            row.Controls.Add(chipBtn);
            if (hasOpenOrder)
            {
                var cancel = new Button { Text = "Cancel", Size = new Size(56, 28), Location = new Point(rowWidth - 66, 13), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(180, 40, 40), ForeColor = Color.White };
                cancel.FlatAppearance.BorderSize = 0;
                cancel.Click += (_, _) => { CancelItem(item.Product); };
                row.Controls.Add(cancel);
            }
            else
            {
                var remove = new Button { Text = "×", Size = new Size(28, 28), Location = new Point(rowWidth - 38, 13), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = Color.FromArgb(190, 200, 215) };
                remove.FlatAppearance.BorderSize = 0;
                remove.Click += (_, _) => { basket.Remove(item); RenderBasket(); };
                row.Controls.Add(remove);
            }
            row.Controls.AddRange([name, price]);
            basketList.Controls.Add(row);
        }
        if (basket.Count == 0) basketDiscountPercent = 0;
        UpdateOrderButtons();
        RenderSeatBar();
        totalLabel.Text = NetTotal().ToString("C");
        custDisplay?.SetOrder($"{selectedRoom} / {selectedTable}", basket.Select(i => ($"{i.Count} \u00d7 {i.Product.Name}", i.Product.Price * i.Count)).ToList(), NetTotal());
        discountButton.Text = basketDiscountPercent > 0 ? $"Discount {basketDiscountPercent:0.#}%" : "Discount";
        discountButton.Visible = store.Features.Discounts && (currentOperator == null || HasPermission("Discounts"));
        basketList.ResumeLayout();
    }

    private string OrderContextText()
    {
        var src = "";
        if (hasOpenOrder && store.GetOpenOrder(selectedRoom, selectedTable) is { } o && o.Source.Length > 0)
            src = $"\nSource: {o.Source}";
        return $"Operator: {currentOperator.Name}{BranchSuffix()}\nRoom: {selectedRoom}  •  {selectedTable}\nDine in{src}";
    }

    private void RenderSeatBar()
    {
        seatBar.Controls.Clear();
        if (!store.Features.SeatOrdering)
        {
            seatBar.Visible = false;
            return;
        }
        seatBar.Visible = true;
        var maxSeat = Math.Min(12, Math.Max(4, Math.Max(activeSeat, basket.Count > 0 ? basket.Max(i => i.Seat) : 0)));
        var cap = new Label { Text = "SEAT", AutoSize = true, ForeColor = Color.FromArgb(143, 154, 171), Location = new Point(0, 6), Font = new Font("Segoe UI", 8.5F) };
        seatBar.Controls.Add(cap);
        var x = 36;
        for (var s = 1; s <= maxSeat; s++)
        {
            var seatNo = s;
            var chip = new Button { Text = s.ToString(), Size = new Size(30, 24), Location = new Point(x, 2), FlatStyle = FlatStyle.Flat, BackColor = s == activeSeat ? mint : panelLight, ForeColor = s == activeSeat ? background : Color.White, Font = new Font("Segoe UI Semibold", 9F), Cursor = Cursors.Hand };
            chip.FlatAppearance.BorderSize = 0;
            chip.Click += (_, _) => { activeSeat = seatNo; RenderSeatBar(); };
            seatBar.Controls.Add(chip);
            x += 34;
        }
        if (maxSeat < 12)
        {
            var add = new Button { Text = "+", Size = new Size(26, 24), Location = new Point(x, 2), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(80, 90, 110), ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 9F), Cursor = Cursors.Hand };
            add.FlatAppearance.BorderSize = 0;
            add.Click += (_, _) => { activeSeat = Math.Min(12, maxSeat + 1); RenderSeatBar(); };
            seatBar.Controls.Add(add);
        }
    }

    private void AddProduct(Product product)
    {
        if (Is86(store, product.Name))
        {
            MessageBox.Show($"{product.Name} is out of stock (86'd).", "86 board", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (store.Features.ProductVariants)
        {
            var prods = store.ProductVariants.Where(v => v.Product == product.Name).ToList();
            if (prods.Count > 0)
            {
                using var vf = new Form { Text = product.Name + " — choose variant", Size = new Size(360, 130 + prods.Count * 34), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
                var pick = new ComboBox { Location = new Point(20, 40), Size = new Size(300, 28), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
                foreach (var v in prods) pick.Items.Add($"{v.Name} — {v.Price:C}");
                pick.SelectedIndex = 0;
                var vOk = new Button { Text = "Add to order", Location = new Point(20, 84), Size = new Size(140, 34), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
                vOk.FlatAppearance.BorderSize = 0;
                vOk.Click += (_, _) => vf.DialogResult = DialogResult.OK;
                vf.Controls.AddRange([new Label { Text = "This product has variants:", AutoSize = true, Location = new Point(20, 14) }, pick, vOk]);
                vf.AcceptButton = vOk;
                if (vf.ShowDialog() != DialogResult.OK) return;
                var chosen = prods[pick.SelectedIndex];
                product = product with { Name = $"{product.Name} ({chosen.Name})", Price = chosen.Price, Barcode = chosen.Barcode.Length > 0 ? chosen.Barcode : product.Barcode };
            }
        }
        var finalPrice = product.Price;
        if (store.Features.HappyHour && store.HappyHourRules.Count > 0)
        {
            var now = DateTime.Now;
            var todayName = now.DayOfWeek.ToString();
            foreach (var rule in store.HappyHourRules)
            {
                if (rule.Days.Contains(todayName) && now.TimeOfDay >= rule.Start && now.TimeOfDay <= rule.End)
                {
                    finalPrice = product.Price * (1 - rule.DiscountPercent / 100m);
                    break;
                }
            }
        }
        var modifiers = "";
        if (store.Features.Modifiers && store.ModifierGroups.Count > 0)
        {
            var selectedMods = new List<(string name, decimal price)>();
            using var modForm = new Form { Text = product.Name + " - Options", Size = new Size(400, 300), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var modsFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoScroll = true, WrapContents = false, BackColor = background, Padding = new Padding(10) };
            var modChecks = new List<(CheckBox cb, string name, decimal price)>();
            foreach (var group in store.ModifierGroups)
            {
                var groupLabel = new Label { Text = group.Name, AutoSize = true, Font = new Font("Segoe UI Semibold", 10F), ForeColor = mint, Margin = new Padding(0, 8, 0, 2) };
                modsFlow.Controls.Add(groupLabel);
                foreach (var opt in group.Options)
                {
                    var cb = new CheckBox { Text = $"{opt.Name} (+{opt.Price:C})", AutoSize = true, ForeColor = Color.White, Margin = new Padding(16, 2, 0, 2) };
                    modChecks.Add((cb, opt.Name, opt.Price));
                    modsFlow.Controls.Add(cb);
                }
            }
            var okBtn = new Button { Text = "OK", Size = new Size(100, 34), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 10, 0, 0) };
            okBtn.FlatAppearance.BorderSize = 0;
            okBtn.Click += (_, _) => modForm.DialogResult = DialogResult.OK;
            modsFlow.Controls.Add(okBtn);
            modForm.Controls.Add(modsFlow);
            if (modForm.ShowDialog() == DialogResult.OK)
            {
                foreach (var (cb, name, price) in modChecks)
                {
                    if (cb.Checked) { selectedMods.Add((name, price)); finalPrice += price; }
                }
                modifiers = string.Join(", ", selectedMods.Select(m => m.name));
            }
            else return;
        }
        var note = "";
        if (store.Features.ItemNotes)
        {
            using var noteForm = new Form { Text = "Item note", Size = new Size(400, 180), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var noteInput = new TextBox { Location = new Point(10, 10), Size = new Size(365, 30), BackColor = panelLight, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            var noteOk = new Button { Text = "OK", Location = new Point(10, 50), Size = new Size(80, 30), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
            var noteSkip = new Button { Text = "Skip", Location = new Point(100, 50), Size = new Size(80, 30), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            noteOk.FlatAppearance.BorderSize = 0; noteSkip.FlatAppearance.BorderSize = 0;
            noteOk.Click += (_, _) => { note = noteInput.Text; noteForm.DialogResult = DialogResult.OK; };
            noteSkip.Click += (_, _) => noteForm.DialogResult = DialogResult.OK;
            noteForm.Controls.AddRange([noteInput, noteOk, noteSkip]);
            noteForm.ShowDialog();
        }
        var index = basket.FindIndex(item => item.Product.Name == product.Name && item.Product.PrinterDestination == product.PrinterDestination && item.Note == note && item.Modifiers == modifiers && (!store.Features.SeatOrdering || item.Seat == activeSeat));
        if (store.Features.WeightedItems && product.Weighted)
        {
            using var wf = new Form { Text = $"Weight — {product.Name}", Size = new Size(340, 190), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var wLabel = new Label { Text = $"Scale weight in kg ({product.Price:C}/kg):", AutoSize = true, Location = new Point(16, 16) };
            var wInput = new NumericUpDown { Location = new Point(16, 46), Size = new Size(140, 30), Minimum = 0.001m, Maximum = 10000, DecimalPlaces = 3, Increment = 0.05m, BackColor = panelLight, ForeColor = Color.White };
            var wTotal = new Label { AutoSize = true, Location = new Point(16, 88), ForeColor = mint, Font = new Font("Segoe UI Semibold", 12F) };
            void UpdateW() => wTotal.Text = $"= {Math.Round(wInput.Value * finalPrice, 2):C}";
            wInput.ValueChanged += (_, _) => UpdateW();
            UpdateW();
            var wOk = new Button { Text = "Add", Location = new Point(16, 122), Size = new Size(90, 32), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
            var wSkip = new Button { Text = "Cancel", Location = new Point(116, 122), Size = new Size(90, 32), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            wOk.FlatAppearance.BorderSize = 0; wSkip.FlatAppearance.BorderSize = 0;
            wOk.Click += (_, _) => wf.DialogResult = DialogResult.OK;
            wSkip.Click += (_, _) => wf.DialogResult = DialogResult.Cancel;
            wf.Controls.AddRange([wLabel, wInput, wTotal, wOk, wSkip]);
            wf.AcceptButton = wOk;
            if (wf.ShowDialog() != DialogResult.OK) return;
            var w = wInput.Value;
            finalPrice = Math.Round(finalPrice * w, 2);
            note = string.IsNullOrWhiteSpace(note) ? $"{w:0.###} kg @ {product.Price:C}/kg" : $"{note} · {w:0.###} kg";
            index = basket.FindIndex(item => item.Product.Name == product.Name && item.Product.PrinterDestination == product.PrinterDestination && item.Note == note && item.Modifiers == modifiers && (!store.Features.SeatOrdering || item.Seat == activeSeat));
        }
        if (index >= 0) basket[index] = (new Product(product.Name, product.Category, finalPrice, product.Color, product.Family, product.ImagePath, product.PrinterDestination, product.Barcode, product.Weighted), basket[index].Count + 1, basket[index].Course, basket[index].Note, basket[index].Seat, basket[index].Modifiers);
        else basket.Add((new Product(product.Name, product.Category, finalPrice, product.Color, product.Family, product.ImagePath, product.PrinterDestination, product.Barcode, product.Weighted), 1, store.Features.CourseManagement ? 1 : 0, note, store.Features.SeatOrdering ? activeSeat : 0, modifiers));
        RenderBasket();
    }

    private void CancelItem(Product product)
    {
        var existing = store.GetOpenOrder(selectedRoom, selectedTable);
        if (existing == null) return;

        var soldLine = existing.Items.FirstOrDefault(i => i.Product == product.Name);
        if (soldLine == null) return;

        if (!string.IsNullOrWhiteSpace(soldLine.PrinterDestination))
        {
            var productDest = store.PrinterDestinations.FirstOrDefault(d => d.Name == soldLine.PrinterDestination);
            var cancelDest = store.PrinterDestinations.FirstOrDefault(d => d.Name == "Cancel");
            if (productDest != null)
            {
                var cancelDestForPrint = cancelDest != null
                    ? new PrinterDestination(cancelDest.Name, productDest.WindowsPrinter, cancelDest.Header, cancelDest.Footer, cancelDest.CutAfterPrint, cancelDest.Layout, cancelDest.LogoPath)
                    : productDest;
                var cancelLine = new SoldLine("CANCEL: " + soldLine.Product, soldLine.Family, -soldLine.Quantity, soldLine.UnitPrice, soldLine.Category, soldLine.PrinterDestination);
                PrintCancelTicket(cancelDestForPrint, [cancelLine]);
            }
        }

        var updatedItems = existing.Items.Where(i => i.Product != product.Name).ToList();
        store.RemoveOpenOrder(selectedRoom, selectedTable);
        if (updatedItems.Count > 0)
            store.AddOpenOrder(new OpenOrder(selectedRoom, selectedTable, existing.Operator, existing.Created, updatedItems, existing.Note, existing.OrderNumber, existing.BillNumber, existing.OrderType, existing.CustomerName, existing.SeatCount));

        basket.RemoveAll(item => item.Product == product);
        hasOpenOrder = updatedItems.Count > 0;
        UpdateOrderButtons();
        RenderBasket();
        var currentRoom = store.Rooms.FirstOrDefault(r => r.Name == selectedRoom);
        if (currentRoom != null) RenderTableMap(currentRoom);
        MessageBox.Show($"Item cancelled: {product.Name}", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private decimal Total() => basket.Sum(item => item.Product.Price * item.Count);

    private decimal DiscountAmount() => basketDiscountPercent <= 0 ? 0 : Math.Round(Total() * basketDiscountPercent / 100m, 2);

    private decimal NetTotal() => Total() - DiscountAmount();

    private (string OrderType, string DeliveryAddress)? ChooseOrderType(string prefillAddress = "")
    {
        var orderType = "Dine-in";
        var deliveryAddress = "";
        var hasDelivery = store.Features.DeliveryOrders;
        using var typeForm = new Form { Text = "Order type", Size = new Size(320, hasDelivery ? 254 : 200), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 12F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var typeLabel = new Label { Text = "Select order type:", AutoSize = true, Location = new Point(30, 20) };
        var dineBtn = new Button { Text = "Dine-in", Size = new Size(260, 44), Location = new Point(30, 55), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 11F) };
        var takeBtn = new Button { Text = "Takeaway", Size = new Size(260, 44), Location = new Point(30, 105), BackColor = Color.FromArgb(55, 120, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 11F) };
        dineBtn.FlatAppearance.BorderSize = 0; takeBtn.FlatAppearance.BorderSize = 0;
        dineBtn.Click += (_, _) => { orderType = "Dine-in"; typeForm.DialogResult = DialogResult.OK; };
        takeBtn.Click += (_, _) => { orderType = "Takeaway"; typeForm.DialogResult = DialogResult.OK; };
        typeForm.Controls.AddRange([typeLabel, dineBtn, takeBtn]);
        if (hasDelivery)
        {
            var deliverBtn = new Button { Text = "Delivery", Size = new Size(260, 44), Location = new Point(30, 155), BackColor = Color.FromArgb(170, 110, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 11F) };
            deliverBtn.FlatAppearance.BorderSize = 0;
            deliverBtn.Click += (_, _) => { orderType = "Delivery"; typeForm.DialogResult = DialogResult.OK; };
            typeForm.Controls.Add(deliverBtn);
        }
        if (typeForm.ShowDialog() != DialogResult.OK) return null;
        if (orderType != "Delivery") return (orderType, "");
        using var addrForm = new Form { Text = "Delivery address", Size = new Size(440, 190), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var addrLabel = new Label { Text = "Customer delivery address:", AutoSize = true, Location = new Point(20, 15) };
        var addrInput = new TextBox { Location = new Point(20, 42), Size = new Size(385, 30), BackColor = panelLight, ForeColor = Color.White, Text = prefillAddress };
        var addrOk = new Button { Text = "OK", Location = new Point(20, 85), Size = new Size(100, 34), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        var addrSkip = new Button { Text = "Skip", Location = new Point(130, 85), Size = new Size(100, 34), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        addrOk.FlatAppearance.BorderSize = 0; addrSkip.FlatAppearance.BorderSize = 0;
        addrOk.Click += (_, _) => { deliveryAddress = addrInput.Text.Trim(); addrForm.DialogResult = DialogResult.OK; };
        addrSkip.Click += (_, _) => addrForm.DialogResult = DialogResult.OK;
        addrForm.Controls.AddRange([addrLabel, addrInput, addrOk, addrSkip]);
        addrForm.AcceptButton = addrOk;
        addrForm.ShowDialog();
        return (orderType, deliveryAddress);
    }

    private void PlaceOrder()
    {
        if (basket.Count == 0) return;
        var typeChoice = ChooseOrderType();
        if (typeChoice is null) return;
        var (orderType, deliveryAddress) = typeChoice.Value;

        var courseOn = store.Features.CourseManagement;
        var lines = basket.Select(item => new SoldLine(item.Product.Name, item.Product.Family, item.Count, item.Product.Price, item.Product.Category, ResolvePrinter(item.Product), item.Note, Modifiers: item.Modifiers, Seat: item.Seat, Course: courseOn ? item.Course : 0)).ToList();
        var existing = store.GetOpenOrder(selectedRoom, selectedTable);
        if (existing != null)
        {
            var merged = existing.Items.Concat(lines).ToList();
            store.RemoveOpenOrder(selectedRoom, selectedTable);
            store.AddOpenOrder(new OpenOrder(selectedRoom, selectedTable, currentOperator.Name, existing.Created, merged, existing.Note, existing.OrderNumber, existing.BillNumber, OrderType: orderType, SeatCount: Math.Max(existing.SeatCount, merged.Max(l => l.Seat)), FiredCourse: existing.FiredCourse, Source: existing.Source, DeliveryAddress: !string.IsNullOrWhiteSpace(deliveryAddress) ? deliveryAddress : existing.DeliveryAddress));
        }
        else
        {
            var orderNum = store.NextOrderNumber();
            store.AddOpenOrder(new OpenOrder(selectedRoom, selectedTable, currentOperator.Name, DateTime.Now, lines, "", orderNum, orderNum, OrderType: orderType, SeatCount: lines.Count > 0 ? lines.Max(l => l.Seat) : 0, DeliveryAddress: deliveryAddress));
        }
        FireNewLines(lines, orderType);
        MessageBox.Show($"Order placed for {selectedTable}\n{NetTotal():C}\n{orderType}", "Order saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        basket.Clear();
        activeSeat = 1;
        hasOpenOrder = true;
        UpdateOrderButtons();
        RenderBasket();
        var currentRoom = store.Rooms.FirstOrDefault(r => r.Name == selectedRoom);
        if (currentRoom != null) RenderTableMap(currentRoom);
    }

    private void FireNewLines(List<SoldLine> newLines, string orderType)
    {
        var order = store.GetOpenOrder(selectedRoom, selectedTable);
        if (order == null || !store.Features.CourseManagement)
        {
            PrintTickets(newLines, orderType: orderType);
            return;
        }
        if (order.FiredCourse == 0)
        {
            if (store.Features.HoldFire) return;
            var first = order.Items.Where(l => l.Course <= 1).ToList();
            if (first.Count == 0) return;
            PrintTickets(first, orderType: order.OrderType);
            SetFiredCourse(order, 1);
        }
        else
        {
            var addPrint = newLines.Where(l => l.Course == 0 || l.Course <= order.FiredCourse).ToList();
            if (addPrint.Count > 0) PrintTickets(addPrint, orderType: order.OrderType);
        }
    }

    private void FireNextCourse()
    {
        var order = store.GetOpenOrder(selectedRoom, selectedTable);
        if (order == null) return;
        var next = order.Items.Where(l => l.Course > order.FiredCourse).Select(l => l.Course).DefaultIfEmpty(0).Min();
        if (next <= 0) return;
        var toPrint = order.Items.Where(l => l.Course == next).ToList();
        if (toPrint.Count == 0) return;
        PrintTickets(toPrint, orderType: order.OrderType);
        SetFiredCourse(order, next);
        if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Fire course", $"{selectedTable} course {next} ({toPrint.Count} lines)");
        UpdateOrderButtons();
    }

    private void SetFiredCourse(OpenOrder order, int course)
    {
        store.RemoveOpenOrder(order.Room, order.Table);
        store.AddOpenOrder(order with { FiredCourse = course });
    }

    private void ScheduleDelayFire()
    {
        var order = store.GetOpenOrder(selectedRoom, selectedTable);
        if (order == null) return;
        using var f = new Form { Text = "Delay fire", Size = new Size(320, 190), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var lbl = new Label { Text = "Send the next course automatically after:", AutoSize = true, Location = new Point(16, 16) };
        var minutes = new NumericUpDown { Location = new Point(16, 46), Size = new Size(100, 30), Minimum = 1, Maximum = 180, Value = 5, BackColor = panelLight, ForeColor = Color.White };
        var lbl2 = new Label { Text = "minutes", AutoSize = true, Location = new Point(126, 49) };
        var ok = new Button { Text = "Schedule", Location = new Point(16, 92), Size = new Size(110, 34), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        ok.FlatAppearance.BorderSize = 0;
        var cancel = new Button { Text = "Cancel", Location = new Point(136, 92), Size = new Size(90, 34), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        cancel.FlatAppearance.BorderSize = 0;
        ok.Click += (_, _) => f.DialogResult = DialogResult.OK;
        cancel.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
        f.Controls.AddRange([lbl, minutes, lbl2, ok, cancel]);
        f.AcceptButton = ok;
        if (f.ShowDialog() != DialogResult.OK) return;
        var table = selectedTable;
        var room = selectedRoom;
        var timer = new System.Windows.Forms.Timer { Interval = (int)minutes.Value * 60_000 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            delayFireTimers.Remove(timer);
            timer.Dispose();
            var current = store.GetOpenOrder(room, table);
            if (current != null && current.Items.Any(l => l.Course > current.FiredCourse))
            {
                var savedTable = selectedTable; var savedRoom = selectedRoom;
                selectedTable = table; selectedRoom = room;
                FireNextCourse();
                selectedTable = savedTable; selectedRoom = savedRoom;
                if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Delay fire", $"{table} fired after {(int)minutes.Value} min");
            }
        };
        delayFireTimers.Add(timer);
        timer.Start();
        MessageBox.Show($"Course will fire automatically in {(int)minutes.Value} minutes for {table}.", "Delay fire", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static bool Is86(PosStore store, string name) => store.EightySix.Contains(name) || store.Auto86.Contains(name);

    private void Auto86Sync()
    {
        if (!store.Features.Auto86 || store.Recipes.Count == 0) return;
        var auto = store.Auto86.ToList();
        var changed = false;
        foreach (var (product, recipeItems) in store.Recipes)
        {
            var low = recipeItems.Any(ri =>
            {
                var ingr = store.Ingredients.FirstOrDefault(i => i.Name == ri.Ingredient);
                return ingr != null && ingr.MinStock > 0 && ingr.Stock <= ingr.MinStock;
            });
            var inAuto = auto.Contains(product);
            if (low && !inAuto) { auto.Add(product); changed = true; }
            else if (!low && inAuto) { auto.Remove(product); changed = true; }
        }
        if (changed) store.SaveAuto86(auto);
    }

    private void PrintBill()
    {
        var existing = store.GetOpenOrder(selectedRoom, selectedTable);
        if (existing == null) return;
        var currency = store.RegisterSettings.Currency;
        var billLines = existing.Items.Select(i => new SoldLine(i.Product, i.Family, i.Quantity, i.UnitPrice, i.Category, i.PrinterDestination, Seat: i.Seat)).ToList();
        var total = billLines.Sum(l => l.UnitPrice * l.Quantity);
        var billDiscountAmount = DiscountAmount();

        var billDest = store.PrinterDestinations.FirstOrDefault(d => d.Name == "Bill");
        if (billDest == null || string.IsNullOrWhiteSpace(billDest.WindowsPrinter))
        {
            MessageBox.Show("No Bill printer configured.\nGo to Settings > Printer routing and set a printer for Bill.", "Print bill", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            if (isOrderPc)
            {
                var escPosBytes = BuildEscPosBytes(billDest, billLines, selectedRoom, selectedTable, existing.Operator, existing.OrderNumber, existing.BillNumber, discountPercent: basketDiscountPercent, discountAmount: billDiscountAmount);
                TcpPrintClient.SendPrintJob(GetMainPcHost(), billDest.WindowsPrinter, escPosBytes);
                return;
            }
            if (billDest.CutAfterPrint)
            {
                try
                {
                    RawPrinter.PrintTicket(billDest, selectedRoom, selectedTable, existing.Operator, billLines, ResolvePrinterName(billDest), existing.OrderNumber, existing.BillNumber, discountPercent: basketDiscountPercent, discountAmount: billDiscountAmount);
                    return;
                }
                catch (InvalidOperationException) { }
            }
            PrintWindowsTicket(billDest, billLines, existing.OrderNumber, existing.BillNumber, discountPercent: basketDiscountPercent, discountAmount: billDiscountAmount);
        }
        catch (Exception error)
        {
            MessageBox.Show($"Could not print bill: {error.Message}", "Print bill", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowDiscountDialog()
    {
        if (basket.Count == 0)
        {
            MessageBox.Show("Add items to the order first.", "Discount", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (currentOperator != null && !HasPermission("Discounts"))
        {
            MessageBox.Show("You do not have permission to apply discounts.", "Discount", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var gross = Total();
        using var form = new Form
        {
            Text = "Discount",
            Size = new Size(store.Features.EmployeeDiscount ? 480 : 400, 565),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = background,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var entry = basketDiscountPercent > 0 ? ((int)Math.Round(basketDiscountPercent)).ToString() : "";
        var orderCaption = new Label { Text = $"Order total: {gross:C}", AutoSize = true, Location = new Point(20, 15), ForeColor = Color.FromArgb(143, 154, 171), Font = new Font("Segoe UI", 10F) };
        var percentCaption = new Label { Text = "DISCOUNT %", AutoSize = true, Location = new Point(20, 45), ForeColor = Color.FromArgb(143, 154, 171), Font = new Font("Segoe UI", 9F) };
        var percentValue = new Label { Text = "0%", AutoSize = true, Location = new Point(20, 66), ForeColor = Color.Orange, Font = new Font("Segoe UI Semibold", 30F) };
        var resultCaption = new Label { Text = "NEW TOTAL", AutoSize = true, Location = new Point(20, 122), ForeColor = Color.FromArgb(143, 154, 171), Font = new Font("Segoe UI", 9F) };
        var resultValue = new Label { Text = gross.ToString("C"), AutoSize = true, Location = new Point(20, 143), ForeColor = mint, Font = new Font("Segoe UI Semibold", 26F) };

        void RefreshPreview()
        {
            var hasValue = decimal.TryParse(entry, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var raw);
            var percent = hasValue ? Math.Min(raw, 100m) : 0m;
            percentValue.Text = string.IsNullOrEmpty(entry) ? "0%" : $"{entry}%";
            var net = gross - Math.Round(gross * percent / 100m, 2);
            resultValue.Text = net.ToString("C");
        }

        void KeyClick(string key)
        {
            switch (key)
            {
                case "C": entry = ""; break;
                case "<": if (entry.Length > 0) entry = entry[..^1]; break;
                default:
                    if (entry.Length < 3)
                    {
                        if (entry == "0") entry = key == "0" ? entry : key;
                        else entry += key;
                    }
                    break;
            }
            RefreshPreview();
        }

        var rows = new[] { new[] { "7", "8", "9" }, new[] { "4", "5", "6" }, new[] { "1", "2", "3" }, new[] { "C", "0", "<" } };
        for (int r = 0; r < rows.Length; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                var key = rows[r][c];
                var display = key == "<" ? "⌫" : key;
                var color = key == "C" || key == "<" ? Color.FromArgb(60, 72, 92) : panelLight;
                var btn = ActionButton(display, 20 + c * 120, 185 + r * 64, 110, 56, color);
                btn.Font = new Font("Segoe UI Semibold", 16F);
                btn.Click += (_, _) => KeyClick(key);
                form.Controls.Add(btn);
            }
        }

        var applyBtn = new Button { Text = "Apply", Location = new Point(20, 449), Size = new Size(230, 50), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 12F) };
        applyBtn.FlatAppearance.BorderSize = 0;
        applyBtn.Click += (_, _) =>
        {
            var percent = decimal.TryParse(entry, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var raw) ? Math.Min(raw, 100m) : 0m;
            basketDiscountPercent = percent > 0 ? percent : 0;
            form.DialogResult = DialogResult.OK;
        };
        var cancelBtn = new Button { Text = "Cancel", Location = new Point(store.Features.EmployeeDiscount ? 370 : 260, 449), Size = store.Features.EmployeeDiscount ? new Size(90, 50) : new Size(110, 50), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F) };
        cancelBtn.FlatAppearance.BorderSize = 0;
        cancelBtn.Click += (_, _) => form.DialogResult = DialogResult.Cancel;
        var extraButtons = new List<Control> { orderCaption, percentCaption, percentValue, resultCaption, resultValue, applyBtn, cancelBtn };
        if (store.Features.EmployeeDiscount)
        {
            var empBtn = new Button { Text = $"Employee {store.Features.EmployeeDiscountPct:0.#}%", Location = new Point(258, 449), Size = new Size(104, 50), BackColor = Color.FromArgb(200, 140, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9F) };
            empBtn.FlatAppearance.BorderSize = 0;
            empBtn.Click += (_, _) => { entry = ((int)store.Features.EmployeeDiscountPct).ToString(); RefreshPreview(); };
            extraButtons.Add(empBtn);
        }

        form.Controls.AddRange([.. extraButtons]);
        form.AcceptButton = applyBtn;
        form.CancelButton = cancelBtn;
        RefreshPreview();
        form.ShowDialog(this);
        RenderBasket();
    }

    private void CompleteSale()
    {
        if (basket.Count == 0) return;

        var typeChoice = ChooseOrderType();
        if (typeChoice is null) return;
        var (orderType, deliveryAddress) = typeChoice.Value;

        decimal discount = DiscountAmount(); decimal tip = 0; string paymentType = "Cash"; string customerName = "";

        var showGift = store.Features.GiftCards && HasPermission("Gift cards");
        var showRedeem = store.Features.Loyalty;
        var showAddress = orderType == "Delivery";
        var extraRows = (showAddress ? 1 : 0) + (showGift ? 2 : 0) + (showRedeem ? 1 : 0);
        var feeCfg = store.FeeSettings;
        var svcPct = store.Features.ServiceCharge ? feeCfg.ServicePct : 0m;
        var cashDiscPct = store.Features.CashDiscount ? feeCfg.CashDiscPct : 0m;
        var cardSurchPct = store.Features.CardSurcharge ? feeCfg.SurchargePct : 0m;
        var anyFees = svcPct > 0 || cashDiscPct > 0 || cardSurchPct > 0;
        if (anyFees) extraRows += 1;

        using var payForm = new Form { Text = "Payment", Size = new Size(420, 440 + 44 * extraRows + 72), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var yPos = 15;
        Control MakeRow(string label, Control ctrl) { var p = new Panel { Location = new Point(20, yPos), Size = new Size(370, 38) }; var l = new Label { Text = label, AutoSize = true, Location = new Point(0, 8) }; ctrl.Location = new Point(150, 4); ctrl.Size = new Size(ctrl.Width, 30); p.Controls.AddRange([l, ctrl]); yPos += 44; return p; }

        var totalBeforeDiscount = Total();
        var totalLabel = new Label { Text = $"Total: {totalBeforeDiscount - discount:C}", AutoSize = true, Font = new Font("Segoe UI Semibold", 14F), ForeColor = mint, Location = new Point(20, yPos) }; yPos += 36;

        var discountValue = new Label { Text = basketDiscountPercent > 0 ? $"-{discount:C} ({basketDiscountPercent:0.#}%)" : "None", AutoSize = true, ForeColor = basketDiscountPercent > 0 ? Color.Orange : Color.FromArgb(143, 154, 171) };
        var tipInput = new NumericUpDown { Minimum = 0, Maximum = 10000, Value = 0, Size = new Size(120, 30), BackColor = panelLight, ForeColor = Color.White, Location = new Point(150, 4) };
        var feeValue = new Label { AutoSize = true, ForeColor = Color.FromArgb(245, 180, 80), Location = new Point(150, 8) };

        var paymentCombo = new ComboBox { Size = new Size(140, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White, Location = new Point(0, 4) };
        var methodItems = new List<string> { "Cash", "Card", "Transfer", "Mobile" };
        if (store.Features.ChargeTabs) methodItems.Add("Tab");
        if (store.Features.Layaway) methodItems.Add("Deposit");
        paymentCombo.Items.AddRange([.. methodItems]);
        paymentCombo.SelectedIndex = 0;
        var splitBtn = new Button { Text = "Split…", Location = new Point(146, 4), Size = new Size(70, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(70, 90, 130), ForeColor = Color.White };
        splitBtn.FlatAppearance.BorderSize = 0;
        var paymentPanel = new Panel { Location = new Point(150, 4), Size = new Size(216, 30) };
        paymentPanel.Controls.AddRange([paymentCombo, splitBtn]);
        var splitTenders = (List<(string Method, decimal Amount)>?)null;
        decimal tenderedValue = 0;
        decimal depositAmt = 0;

        void CalcFees(string method, out decimal svc, out decimal cd, out decimal su)
        {
            svc = svcPct > 0 ? Math.Round((totalBeforeDiscount - discount) * svcPct / 100m, 2) : 0m;
            var withSvc = totalBeforeDiscount - discount + svc;
            cd = cashDiscPct > 0 && method == "Cash" ? Math.Round(withSvc * cashDiscPct / 100m, 2) : 0m;
            su = cardSurchPct > 0 && method == "Card" ? Math.Round(withSvc * cardSurchPct / 100m, 2) : 0m;
        }
        string CurrentMethod() => splitTenders != null ? "Split" : paymentCombo.Text;

        var customerCombo = new ComboBox { Size = new Size(150, 30), DropDownStyle = ComboBoxStyle.DropDown, BackColor = panelLight, ForeColor = Color.White, Location = new Point(150, 4) };
        customerCombo.Items.Add("(none)");
        foreach (var c in store.Customers) customerCombo.Items.Add(c.Name);
        customerCombo.SelectedIndex = 0;

        var rows = new List<Control> {
            totalLabel,
            MakeRow("Discount:", discountValue),
            MakeRow("Tip:", tipInput),
            MakeRow("Payment:", paymentPanel),
            MakeRow("Customer:", customerCombo)
        };
        if (anyFees) rows.Insert(2, MakeRow("Fees:", feeValue));

        var addressInput = new TextBox { BackColor = panelLight, ForeColor = Color.White, Size = new Size(216, 30), Text = deliveryAddress };
        if (showAddress) rows.Add(MakeRow("Address:", addressInput));

        string appliedGiftCode = "";
        decimal appliedGiftBalance = 0;
        Button? giftApplyBtn = null;
        if (showGift)
        {
            var giftPanel = new Panel { Location = new Point(150, 4), Size = new Size(216, 30) };
            var giftInput = new TextBox { Location = new Point(0, 0), Size = new Size(140, 30), BackColor = panelLight, ForeColor = Color.White };
            giftApplyBtn = new Button { Text = "Apply", Location = new Point(146, 0), Size = new Size(70, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 130, 90), ForeColor = Color.White };
            giftApplyBtn.FlatAppearance.BorderSize = 0;
            giftApplyBtn.Click += (_, _) =>
            {
                if (appliedGiftBalance > 0)
                {
                    appliedGiftCode = ""; appliedGiftBalance = 0;
                    giftApplyBtn.Text = "Apply"; giftInput.Enabled = true; giftInput.Text = "";
                    return;
                }
                var code = giftInput.Text.Trim();
                var card = store.GiftCards.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
                if (card == null) { MessageBox.Show($"Gift card \"{code}\" was not found.", "Gift card", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (card.Balance <= 0) { MessageBox.Show($"Gift card {card.Code} has no balance left.", "Gift card", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                appliedGiftCode = card.Code; appliedGiftBalance = card.Balance;
                giftApplyBtn.Text = "Remove"; giftInput.Enabled = false;
            };
            giftPanel.Controls.AddRange([giftInput, giftApplyBtn]);
            rows.Add(MakeRow("Gift card:", giftPanel));
        }

        var creditCheck = new CheckBox { Text = "Use store credit", AutoSize = true, ForeColor = Color.White, Location = new Point(150, 8), Enabled = false };
        var creditRow = MakeRow("Store credit:", creditCheck);
        if (showGift) rows.Add(creditRow);

        var redeemPanel = new Panel { Location = new Point(150, 4), Size = new Size(216, 30) };
        var redeemInput = new NumericUpDown { Minimum = 0, Maximum = 999999, Increment = 10, Location = new Point(0, 0), Size = new Size(90, 30), BackColor = panelLight, ForeColor = Color.White, Enabled = false };
        var redeemValue = new Label { AutoSize = true, Location = new Point(96, 7), ForeColor = mint, Text = "" };
        redeemInput.ValueChanged += (_, _) => redeemValue.Text = redeemInput.Value > 0 ? $"= {(redeemInput.Value / 10m):C}" : "";
        redeemPanel.Controls.AddRange([redeemInput, redeemValue]);
        var redeemRow = MakeRow("Points (10 = 1):", redeemPanel);
        if (showRedeem) rows.Add(redeemRow);

        void RefreshCustRows()
        {
            var c = customerCombo.Text == "(none)" ? null : store.Customers.FirstOrDefault(x => x.Name == customerCombo.Text);
            creditCheck.Enabled = c != null && c.Balance > 0;
            creditCheck.Text = c != null && c.Balance > 0 ? $"Use {c.Balance:C} credit" : "Use store credit";
            if (!creditCheck.Enabled) creditCheck.Checked = false;
            redeemInput.Enabled = c != null && c.Points >= 10;
            redeemInput.Maximum = c != null ? Math.Max(10, c.Points) : 10;
            if (!redeemInput.Enabled) { redeemInput.Value = 0; redeemValue.Text = ""; }
        }
        customerCombo.SelectedIndexChanged += (_, _) => RefreshCustRows();
        RefreshCustRows();

        var tenderedInput = new NumericUpDown { Minimum = 0, Maximum = 100000000, Value = 0, DecimalPlaces = 2, Increment = 10, Size = new Size(95, 30), BackColor = panelLight, ForeColor = Color.White, Location = new Point(110, 2) };
        var changeLabel = new Label { Location = new Point(110, 36), Size = new Size(256, 26), Font = new Font("Segoe UI Semibold", 11F), ForeColor = Color.FromArgb(126, 240, 199), TextAlign = ContentAlignment.MiddleLeft };
        Panel? tenderPanel = null;
        Button? payOk = null;

        decimal LiveDue()
        {
            CalcFees(CurrentMethod(), out var svcF, out var cdF, out var suF);
            var d = totalBeforeDiscount - discount + svcF - cdF + suF + tipInput.Value - appliedGiftBalance;
            if (showGift && creditCheck.Enabled && creditCheck.Checked)
                d -= customerCombo.Text == "(none)" ? 0 : store.Customers.FirstOrDefault(x => x.Name == customerCombo.Text)?.Balance ?? 0;
            if (showRedeem && redeemInput.Enabled) d -= redeemInput.Value / 10m;
            return d;
        }
        decimal RoundCash(decimal d)
        {
            if (!store.Features.CashRounding || splitTenders != null || paymentCombo.Text != "Cash" || d <= 0) return d;
            return Math.Round(d / 5m, 0, MidpointRounding.AwayFromZero) * 5m;
        }
        void UpdateChange()
        {
            if (changeLabel == null) return;
            CalcFees(CurrentMethod(), out var svcF, out var cdF, out var suF);
            if (anyFees) feeValue.Text = (svcF > 0 ? $"service +{svcF:C}  " : "") + (cdF > 0 ? $"cash disc -{cdF:C}  " : "") + (suF > 0 ? $"card +{suF:C}" : "");
            var d = Math.Max(0, RoundCash(LiveDue()));
            var t = tenderedInput.Value;
            if (payOk != null) payOk.Text = $"Accept payment  {d:C}";
            if (t <= 0) { changeLabel.Text = ""; return; }
            changeLabel.Text = t < d ? $"Short: {d - t:C}" : $"CHANGE:  {t - d:C}";
            changeLabel.ForeColor = t >= d ? Color.FromArgb(126, 240, 199) : Color.Orange;
        }
        void UpdateTenderVisibility()
        {
            if (tenderPanel == null) return;
            tenderPanel.Visible = splitTenders == null && paymentCombo.Text == "Cash";
            splitBtn.Text = splitTenders == null ? "Split…" : $"{splitTenders.Count} tenders ✓";
            paymentCombo.Enabled = splitTenders == null;
        }

        tenderPanel = new Panel { Location = new Point(20, yPos), Size = new Size(370, 66) };
        var cashLabel = new Label { Text = "Cash received:", AutoSize = true, Location = new Point(0, 6) };
        void SetTender(decimal v)
        {
            if (v < tenderedInput.Minimum) v = tenderedInput.Minimum;
            if (v > tenderedInput.Maximum) v = tenderedInput.Maximum;
            tenderedInput.Value = v;
        }
        decimal CeilCash(decimal step)
        {
            var d = Math.Max(0, RoundCash(LiveDue()));
            return d <= 0 ? step : (decimal)Math.Ceiling((double)(d / step)) * step;
        }
        Button QuickBtn(string text, int x, Func<decimal> value)
        {
            var b = new Button { Text = text, Location = new Point(x, 3), Size = new Size(38, 26), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(70, 90, 130), ForeColor = Color.White, Font = new Font("Segoe UI", 7.5F) };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (_, _) => { SetTender(value()); UpdateChange(); };
            return b;
        }
        tenderPanel.Controls.AddRange([cashLabel, tenderedInput,
            QuickBtn("Exact", 214, () => Math.Max(0, RoundCash(LiveDue()))),
            QuickBtn("→10", 254, () => CeilCash(10)),
            QuickBtn("→50", 294, () => CeilCash(50)),
            QuickBtn("→100", 334, () => CeilCash(100)),
            changeLabel]);
        rows.Add(tenderPanel);
        yPos += 72;

        paymentCombo.SelectedIndexChanged += (_, _) => { UpdateTenderVisibility(); UpdateChange(); };
        tenderedInput.ValueChanged += (_, _) => UpdateChange();
        tipInput.ValueChanged += (_, _) => UpdateChange();
        creditCheck.CheckedChanged += (_, _) => UpdateChange();
        redeemInput.ValueChanged += (_, _) => UpdateChange();
        if (giftApplyBtn != null) giftApplyBtn.Click += (_, _) => UpdateChange();
        splitBtn.Click += (_, _) =>
        {
            var due = LiveDue();
            if (due <= 0) { MessageBox.Show("Nothing to split.", "Split payment", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var result = ShowSplitPayment(due);
            if (result == null) return;
            splitTenders = result;
            UpdateTenderVisibility();
            UpdateChange();
        };
        UpdateTenderVisibility();
        UpdateChange();

        payForm.Controls.AddRange([.. rows]);
        payOk = new Button { Text = $"Accept payment  {totalBeforeDiscount - discount:C}", Location = new Point(20, yPos), Size = new Size(370, 48), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 12F) };
        payOk.FlatAppearance.BorderSize = 0;
        UpdateChange();
        payOk.Click += (_, _) =>
        {
            var due = LiveDue();
            if (due < -0.005m)
            {
                MessageBox.Show("Applied payments exceed the total.\nRemove the gift card, credit or points before continuing.", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var cashDue = Math.Max(0, RoundCash(due));
            if (splitTenders != null)
            {
                var sum = splitTenders.Sum(t => t.Amount);
                if (Math.Abs(sum - due) > 0.005m)
                {
                    MessageBox.Show($"Tendered amounts ({sum:C}) must equal the amount due ({due:C}).\nRemaining: {due - sum:C}", "Split payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (paymentCombo.Text == "Cash" && tenderedInput.Value > 0 && tenderedInput.Value < cashDue)
            {
                MessageBox.Show($"Cash received ({tenderedInput.Value:C}) is less than the amount due ({cashDue:C}).", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var selCust = customerCombo.Text == "(none)" ? null : store.Customers.FirstOrDefault(x => x.Name == customerCombo.Text);
            if (paymentCombo.Text == "Tab" && selCust == null)
            {
                MessageBox.Show("Select a customer before charging to a tab.", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (paymentCombo.Text == "Tab" && selCust != null && selCust.CreditLimit > 0 && selCust.TabDue + due > selCust.CreditLimit)
            {
                MessageBox.Show($"{selCust.Name}'s credit limit is {selCust.CreditLimit:C}.\nCurrent tab due: {selCust.TabDue:C} — this payment would exceed it.", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (paymentCombo.Text == "Deposit")
            {
                if (due <= 0) { MessageBox.Show("Nothing due on this order.", "Deposit", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var dv = PromptAmount("Deposit", $"Amount due: {due:C}\nEnter the deposit collected now (balance due on pickup, cash):");
                if (dv is null) return;
                if (dv.Value <= 0 || dv.Value > due + 0.005m) { MessageBox.Show($"Deposit must be between 0 and {due:C}.", "Deposit", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                depositAmt = dv.Value;
            }
            tenderedValue = tenderedInput.Value;
            payForm.DialogResult = DialogResult.OK;
        };
        payForm.Controls.Add(payOk);
        payForm.AcceptButton = payOk;

        if (payForm.ShowDialog() != DialogResult.OK) return;

        tip = tipInput.Value;
        paymentType = paymentCombo.Text;
        customerName = customerCombo.Text == "(none)" ? "" : customerCombo.Text;
        if (showAddress) deliveryAddress = addressInput.Text.Trim();
        var creditRequested = showGift && creditCheck.Enabled && creditCheck.Checked;
        var redeemPts = showRedeem && redeemInput.Enabled ? (int)redeemInput.Value : 0;
        var cust = string.IsNullOrWhiteSpace(customerName) ? null : store.Customers.FirstOrDefault(c => c.Name == customerName);

        var lines = basket.Select(item => new SoldLine(item.Product.Name, item.Product.Family, item.Count, item.Product.Price, item.Product.Category, ResolvePrinter(item.Product), item.Note, Modifiers: item.Modifiers, Seat: item.Seat, Course: store.Features.CourseManagement ? item.Course : 0)).ToList();
        var existing = store.GetOpenOrder(selectedRoom, selectedTable);
        var billNum = existing?.BillNumber ?? 0;
        var orderNum = existing?.OrderNumber ?? 0;
        var paymentNum = store.NextPaymentNumber();

        if (existing == null)
        {
            orderNum = store.NextOrderNumber();
            billNum = orderNum;
            PrintTickets(lines, orderNum, billNum, orderType);
        }

        if (store.Features.MultiRatePricing && orderType != "Dine-in")
        {
            var multiplier = orderType == "Takeaway" ? 0.9m : 0.85m;
            totalBeforeDiscount = basket.Sum(item => item.Product.Price * item.Count) * multiplier;
        }
        var methodForFees = splitTenders != null ? "Split" : paymentType;
        CalcFees(methodForFees, out var svcFee, out var cashDiscFee, out var surchFee);
        var finalTotal = totalBeforeDiscount - discount + svcFee - cashDiscFee + surchFee + tip;

        var due = finalTotal;
        decimal giftApplied = 0, creditApplied = 0, redeemAmount = 0;
        if (appliedGiftBalance > 0)
        {
            giftApplied = Math.Min(appliedGiftBalance, due);
            due -= giftApplied;
        }
        if (creditRequested && cust != null && cust.Balance > 0)
        {
            creditApplied = Math.Min(cust.Balance, due);
            due -= creditApplied;
        }
        if (redeemPts > 0 && cust != null)
        {
            redeemAmount = Math.Min(redeemPts / 10m, due);
            redeemPts = (int)(redeemAmount * 10);
            due -= redeemAmount;
        }
        if (due < 0) due = 0;

        decimal roundingOut = 0;
        if (splitTenders == null && paymentType == "Cash" && due > 0)
        {
            var rounded = RoundCash(due);
            if (rounded != due) { roundingOut = rounded - due; due = rounded; }
        }

        var payParts = new List<string>();
        if (giftApplied > 0) payParts.Add("Gift card");
        if (creditApplied > 0) payParts.Add("Store credit");
        if (redeemAmount > 0) payParts.Add($"Points ({redeemPts})");
        decimal cashAmt;
        decimal depositRemaining = 0;
        if (splitTenders is { Count: > 0 })
        {
            cashAmt = splitTenders.Where(t => t.Method == "Cash").Sum(t => t.Amount);
            if (splitTenders.Count == 1) payParts.Add(splitTenders[0].Method);
            else foreach (var part in splitTenders) payParts.Add($"{part.Method} {part.Amount:C}");
        }
        else
        {
            if (paymentType == "Deposit")
            {
                depositRemaining = Math.Max(0, due - depositAmt);
                cashAmt = depositAmt;
                payParts.Add(depositRemaining > 0 ? $"Deposit {depositAmt:C}" : "Deposit");
            }
            else
            {
                cashAmt = due > 0 && paymentType == "Cash" ? due : 0;
                if (due > 0) payParts.Add(paymentType);
            }
        }
        var finalPaymentType = payParts.Count > 0 ? string.Join(" + ", payParts) : paymentType;
        var tenderedOut = splitTenders == null && paymentType == "Cash" && cashAmt > 0 ? tenderedValue : 0m;

        var offlineQueued = false;
        if (store.Features.OfflinePayments && store.FeeSettings.TerminalOffline && due > 0)
        {
            if (splitTenders is { Count: > 0 })
            {
                foreach (var part in splitTenders.Where(t => t.Method == "Card"))
                {
                    store.OfflinePayments.Add(new OfflinePayment(DateTime.Now, paymentNum, part.Amount, selectedTable, currentOperator.Name, "Pending"));
                    offlineQueued = true;
                }
            }
            else if (paymentType == "Card")
            {
                store.OfflinePayments.Add(new OfflinePayment(DateTime.Now, paymentNum, due, selectedTable, currentOperator.Name, "Pending"));
                offlineQueued = true;
            }
            if (offlineQueued)
            {
                store.SaveOfflinePayments(store.OfflinePayments);
                finalPaymentType += " (offline)";
                if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Offline payment queued", $"{paymentNum} {due:C} — terminal offline");
            }
        }

        var saleTotal = paymentType == "Deposit" ? finalTotal - depositRemaining : finalTotal;
        var sale = new Sale(DateTime.Now, saleTotal, selectedTable, currentOperator.Name, lines, paymentNum, billNum, tip, finalPaymentType, customerName, orderType, discount, DeliveryAddress: deliveryAddress, CashAmount: cashAmt, Tendered: tenderedOut, Rounding: roundingOut, ServiceFee: svcFee, CashDiscountAmt: cashDiscFee, SurchargeAmt: surchFee);
        var trainingMode = store.Features.TrainingMode;
        if (!trainingMode) store.AddSale(sale);
        if (!trainingMode && paymentType == "Deposit" && depositRemaining > 0)
        {
            var soId = store.SpecialOrders.Count > 0 ? store.SpecialOrders.Max(s => s.Id) + 1 : 1;
            store.SpecialOrders.Add(new SpecialOrder(soId, DateTime.Now, customerName, selectedTable, finalTotal, saleTotal, "Open", paymentNum));
            store.SaveSpecialOrders(store.SpecialOrders);
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Deposit taken", $"special order #{soId} — {depositAmt:C} paid, {depositRemaining:C} due");
        }
        lastReceipt = (sale, lines, basketDiscountPercent, selectedRoom, selectedTable);
        if (!trainingMode && giftApplied > 0)
        {
            var gi = store.GiftCards.FindIndex(c => string.Equals(c.Code, appliedGiftCode, StringComparison.OrdinalIgnoreCase));
            if (gi >= 0)
            {
                store.GiftCards[gi] = store.GiftCards[gi] with { Balance = store.GiftCards[gi].Balance - giftApplied };
                store.SaveGiftCards(store.GiftCards);
            }
        }
        if (!trainingMode && cust != null)
        {
            var cIdx = store.Customers.IndexOf(cust);
            if (cIdx >= 0)
            {
                var earned = store.Features.Loyalty && paymentType != "Tab" ? (int)(paymentType == "Deposit" ? saleTotal : due) : 0;
                store.Customers[cIdx] = cust with
                {
                    Balance = cust.Balance - creditApplied,
                    Points = cust.Points - redeemPts + earned,
                    TabDue = cust.TabDue + (paymentType == "Tab" ? due : 0)
                };
                store.SaveCustomers(store.Customers);
                if (paymentType == "Tab" && due > 0 && store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Tab charge", $"{cust.Name} +{due:C} (due {cust.TabDue + due:C})");
            }
        }
        if (!trainingMode && store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Payment", $"{finalTotal:C} ({finalPaymentType}) {orderType} discount={discount:C} tip={tip:C}" + (giftApplied > 0 ? $" gift=-{giftApplied:C}" : "") + (creditApplied > 0 ? $" credit=-{creditApplied:C}" : "") + (redeemAmount > 0 ? $" points={redeemPts}" : ""));
        if (!trainingMode) TrySendEmailReceipt(customerName, finalPaymentType, lines, finalTotal, discount, tip, paymentNum, orderType, serviceFee: svcFee, cashDisc: cashDiscFee, surcharge: surchFee);
        if (!trainingMode && store.Features.StockManagement && store.Recipes.Count > 0)
        {
            var alerts = new List<StockAlert>();
            foreach (var line in lines)
            {
                if (store.Recipes.TryGetValue(line.Product, out var recipeItems))
                {
                    foreach (var ri in recipeItems)
                    {
                        var idx = store.Ingredients.FindIndex(i => i.Name == ri.Ingredient);
                        if (idx >= 0)
                        {
                            var ingr = store.Ingredients[idx];
                            var used = ri.Quantity * line.Quantity;
                            var newStock = ingr.Stock - used;
                            store.Ingredients[idx] = ingr with { Stock = newStock };
                            if (store.Features.InventoryAlerts && newStock <= ingr.MinStock)
                                alerts.Add(new StockAlert(line.Product, ri.Ingredient, newStock, ingr.MinStock));
                        }
                    }
                }
            }
            store.SaveIngredients(store.Ingredients);
            Auto86Sync();
            if (alerts.Count > 0)
                MessageBox.Show("LOW STOCK ALERTS:\n\n" + string.Join("\n", alerts.Select(a => $"• {a.Ingredient}: {a.Remaining:F1} remaining (min: {a.MinStock:F1})\n  Used by: {a.ProductName}")),
                    "Stock Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        if (!trainingMode) store.RemoveOpenOrder(selectedRoom, selectedTable);

        if (!trainingMode) PrintPaymentReceipt(sale, lines, basketDiscountPercent, selectedRoom, selectedTable);

        var changeLine = tenderedOut > 0 ? $"\nTendered: {tenderedOut:C}   Change: {tenderedOut - cashAmt:C}" : "";
        var roundLine = roundingOut != 0 ? $"\nRounded: {(roundingOut > 0 ? "+" : "")}{roundingOut:C}" : "";
        var trainingLine = trainingMode ? "\n⚠ TRAINING MODE — not recorded" : "";
        MessageBox.Show($"Payment accepted\n{finalTotal:C}  •  {selectedTable}{changeLine}{roundLine}{trainingLine}", "Sale complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        custDisplay?.ShowThankYou(finalTotal);
        basket.Clear();
        activeSeat = 1;
        hasOpenOrder = false;
        UpdateOrderButtons();
        salesLabel.Text = TodaySales();
        RenderBasket();
        var currentRoom = store.Rooms.FirstOrDefault(r => r.Name == selectedRoom);
        if (currentRoom != null) RenderTableMap(currentRoom);
    }

    private string ResolvePrinter(Product product)
    {
        var dest = store.Families.FirstOrDefault(family => family.Name == product.Family)?.PrinterDestination ?? "";
        return dest;
    }

    private void ShowPrinterManager()
    {
        if (!HasPermission("Settings")) return;
        var panel = new Panel { Dock = DockStyle.Fill };
        var form = new PrinterManagerForm(background, panelLight, mint, store.PrinterDestinations, destinations => store.SavePrinterDestinations(destinations), (current, components) =>
        {
            var idx = store.PrinterDestinations.IndexOf(current);
            if (idx >= 0)
            {
                var updated = current with { Layout = components };
                var all = store.PrinterDestinations.ToList();
                all[idx] = updated;
                store.SavePrinterDestinations(all);
            }
        });
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        panel.Controls.Add(form);
        form.Show();
        ShowView(panel, "Printer routing", ShowSettings);
    }

    private void PrintPaymentReceipt(Sale sale, List<SoldLine> lines, decimal discountPercent, string room, string table)
    {
        var payDest = store.PrinterDestinations.FirstOrDefault(d => d.Name == "Payment");
        if (payDest == null || string.IsNullOrWhiteSpace(payDest.WindowsPrinter)) return;
        TicketLayout.Tendered = sale.Tendered;
        TicketLayout.CashReceived = sale.CashAmount >= 0 ? sale.CashAmount : 0;
        TicketLayout.Rounding = sale.Rounding;
        try
        {
            if (isOrderPc)
            {
                var escPosBytes = BuildEscPosBytes(payDest, lines, room, table, sale.Operator, billNumber: sale.BillNumber, paymentNumber: sale.PaymentNumber, discountPercent: discountPercent, discountAmount: sale.Discount);
                TcpPrintClient.SendPrintJob(GetMainPcHost(), payDest.WindowsPrinter, escPosBytes);
            }
            else if (payDest.CutAfterPrint)
            {
                try { RawPrinter.PrintTicket(payDest, room, table, sale.Operator, lines, ResolvePrinterName(payDest), billNumber: sale.BillNumber, paymentNumber: sale.PaymentNumber, discountPercent: discountPercent, discountAmount: sale.Discount); }
                catch (InvalidOperationException) { PrintWindowsTicket(payDest, lines, billNumber: sale.BillNumber, paymentNumber: sale.PaymentNumber, discountPercent: discountPercent, discountAmount: sale.Discount, room: room, table: table); }
            }
            else PrintWindowsTicket(payDest, lines, billNumber: sale.BillNumber, paymentNumber: sale.PaymentNumber, discountPercent: discountPercent, discountAmount: sale.Discount, room: room, table: table);
        }
        catch (Exception error)
        {
            MessageBox.Show($"Could not print payment: {error.Message}", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            TicketLayout.Tendered = 0;
            TicketLayout.CashReceived = 0;
            TicketLayout.Rounding = 0;
        }
    }

    private List<(string Method, decimal Amount)>? ShowSplitPayment(decimal due)
    {
        var chosen = new List<(ComboBox Box, NumericUpDown Amount)>();
        using var form = new Form { Text = $"Split payment — due {due:C}", Size = new Size(470, 440), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var listFlow = new FlowLayoutPanel { Location = new Point(12, 54), Size = new Size(432, 240), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = background };
        var remainingLabel = new Label { Location = new Point(12, 302), Size = new Size(432, 30), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 11F), ForeColor = mint };
        Button? okBtn = null;
        List<(string Method, decimal Amount)>? result = null;

        void Refresh()
        {
            var sum = chosen.Sum(c => c.Amount.Value);
            remainingLabel.Text = $"Remaining: {due - sum:C}   tendered: {sum:C}";
            if (okBtn != null) okBtn.Enabled = Math.Abs(sum - due) < 0.005m && chosen.Count > 0;
        }
        void AddRow(string method, decimal amount)
        {
            var row = new Panel { Size = new Size(404, 42), Margin = new Padding(0, 0, 0, 8) };
            var box = new ComboBox { Location = new Point(0, 6), Size = new Size(120, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
            box.Items.AddRange(["Cash", "Card", "Transfer", "Mobile"]);
            box.SelectedItem = box.Items.Contains(method) ? method : "Cash";
            var amt = new NumericUpDown { Location = new Point(130, 6), Size = new Size(140, 30), Minimum = 0, Maximum = 100000000, DecimalPlaces = 2, Increment = 100, Value = Math.Clamp(amount, 0, 100000000) };
            var remove = new Button { Text = "✕", Location = new Point(280, 6), Size = new Size(34, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(160, 50, 50), ForeColor = Color.White };
            remove.FlatAppearance.BorderSize = 0;
            remove.Click += (_, _) => { chosen.Remove((box, amt)); listFlow.Controls.Remove(row); Refresh(); };
            box.SelectedIndexChanged += (_, _) => Refresh();
            amt.ValueChanged += (_, _) => Refresh();
            row.Controls.AddRange([box, amt, remove]);
            listFlow.Controls.Add(row);
            chosen.Add((box, amt));
            Refresh();
        }
        var addBtn = new Button { Text = "+ Add method", Location = new Point(330, 10), Size = new Size(110, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(70, 90, 130), ForeColor = Color.White };
        addBtn.FlatAppearance.BorderSize = 0;
        addBtn.Click += (_, _) => { var last = chosen.Count > 0 ? chosen[^1].Box.Text : "Cash"; AddRow(last, 0); };
        var head = new Label { Text = $"Amount due: {due:C}  —  methods must add up to it", AutoSize = true, ForeColor = Color.FromArgb(170, 180, 195), Location = new Point(14, 16) };
        okBtn = new Button { Text = "Accept split", Location = new Point(206, 344), Size = new Size(116, 42), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
        okBtn.FlatAppearance.BorderSize = 0;
        okBtn.Click += (_, _) =>
        {
            result = chosen.Select(c => (c.Box.Text, c.Amount.Value)).ToList();
            form.DialogResult = DialogResult.OK;
        };
        var cancelBtn = new Button { Text = "Cancel", Location = new Point(330, 344), Size = new Size(110, 42), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        cancelBtn.FlatAppearance.BorderSize = 0;
        cancelBtn.Click += (_, _) => form.DialogResult = DialogResult.Cancel;

        form.Controls.AddRange([listFlow, remainingLabel, head, addBtn, okBtn, cancelBtn]);
        form.AcceptButton = okBtn;
        form.CancelButton = cancelBtn;
        AddRow("Cash", Math.Max(0, due));
        return form.ShowDialog() == DialogResult.OK && result != null ? result : null;
    }

    private void PrintTickets(List<SoldLine> lines, int orderNumber = 0, int billNumber = 0, string orderType = "", string? room = null, string? table = null)
    {
        var r = room ?? selectedRoom;
        var t = table ?? selectedTable;
        var existing = store.GetOpenOrder(r, t);
        var orderNum = orderNumber > 0 ? orderNumber : existing?.OrderNumber ?? 0;
        var billNum = billNumber > 0 ? billNumber : existing?.BillNumber ?? 0;
        var missedDestinations = new List<string>();
        var routedAny = false;
        foreach (var group in lines.Where(line => !string.IsNullOrWhiteSpace(line.PrinterDestination)).GroupBy(line => line.PrinterDestination))
        {
            var destination = store.PrinterDestinations.FirstOrDefault(item => string.Equals(item.Name, group.Key, StringComparison.OrdinalIgnoreCase))
                ?? store.PrinterDestinations.FirstOrDefault(item => string.Equals(item.WindowsPrinter, group.Key, StringComparison.OrdinalIgnoreCase));
            if (destination == null)
            {
                if (!missedDestinations.Contains(group.Key)) missedDestinations.Add(group.Key);
                continue;
            }
            routedAny = true;
            try
            {
                var ticketLines = group.ToList();
                if (destination.CutAfterPrint)
                {
                    try
                    {
                        if (isOrderPc)
                        {
                            var escPosBytes = BuildEscPosBytes(destination, ticketLines, r, t, currentOperator.Name, orderNum, billNum, orderType: orderType);
                            try
                            {
                                TcpPrintClient.SendPrintJob(GetMainPcHost(), destination.WindowsPrinter, escPosBytes);
                            }
                            catch
                            {
                                var uncPrinter = $@"\\{GetMainPcHost()}\{destination.WindowsPrinter}";
                                if (!RawPrinter.OpenPrinterLocal(uncPrinter, out var hPrinter, 0))
                                    throw new InvalidOperationException($"Cannot reach main PC printer.\n\nMake sure:\n1. The main PC is running\n2. The printer is shared\n3. Port 9100 is open\n\nTried: TCP :9100 and UNC {uncPrinter}");
                                var docInfo = new RawPrinter.DocumentInfo { DocumentName = "Remote print", OutputFile = null, DataType = null };
                                try
                                {
                                    RawPrinter.StartDocPrinter(hPrinter, 1, docInfo);
                                    RawPrinter.StartPagePrinter(hPrinter);
                                    RawPrinter.WritePrinter(hPrinter, escPosBytes, escPosBytes.Length, out _);
                                    RawPrinter.EndPagePrinter(hPrinter);
                                    RawPrinter.EndDocPrinter(hPrinter);
                                }
                                finally { RawPrinter.ClosePrinter(hPrinter); }
                            }
                        }
                        else
                        {
                            RawPrinter.PrintTicket(destination, r, t, currentOperator.Name, ticketLines, orderNumber: orderNum, billNumber: billNum, orderType: orderType);
                        }
                        continue;
                    }
                    catch (InvalidOperationException)
                    {
                        // Some Windows thermal drivers reject RAW jobs. Keep the ticket printable.
                    }
                }
                if (isOrderPc)
                {
                    var escPosBytes = BuildEscPosBytes(destination, ticketLines, r, t, currentOperator.Name, orderNum, billNum, orderType: orderType);
                    try { TcpPrintClient.SendPrintJob(GetMainPcHost(), destination.WindowsPrinter, escPosBytes); }
                    catch
                    {
                        var uncPrinter = $@"\\{GetMainPcHost()}\{destination.WindowsPrinter}";
                        if (!RawPrinter.OpenPrinterLocal(uncPrinter, out var hPrinter, 0))
                            throw new InvalidOperationException($"Cannot reach main PC printer {destination.WindowsPrinter}.\n\nMake sure the main PC is running and the printer is shared.");
                        var docInfo = new RawPrinter.DocumentInfo { DocumentName = "Remote print", OutputFile = null, DataType = null };
                        try
                        {
                            RawPrinter.StartDocPrinter(hPrinter, 1, docInfo);
                            RawPrinter.StartPagePrinter(hPrinter);
                            RawPrinter.WritePrinter(hPrinter, escPosBytes, escPosBytes.Length, out _);
                            RawPrinter.EndPagePrinter(hPrinter);
                            RawPrinter.EndDocPrinter(hPrinter);
                        }
                        finally { RawPrinter.ClosePrinter(hPrinter); }
                    }
                }
                else
                {
                    PrintWindowsTicket(destination, ticketLines, orderNum, billNum, orderType: orderType, room: r, table: t);
                }
            }
            catch (Exception error)
            {
                MessageBox.Show($"Could not print to {destination.WindowsPrinter}: {error.Message}", "Printing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        if (missedDestinations.Count > 0)
            MessageBox.Show($"Printer destination not found: {string.Join(", ", missedDestinations)}\nThese items were NOT printed on the order ticket.\n\nFix it in Settings > Printer routing,\nor re-select the destination in Settings > Manage families.", "Printing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else if (!routedAny && lines.Count > 0)
            MessageBox.Show("No printer destination is set for these items - order ticket not printed.\n\nSet one in Settings > Manage families (printer routing).", "Printing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void PrintCancelTicket(PrinterDestination destination, List<SoldLine> lines)
    {
        if (string.IsNullOrWhiteSpace(destination.WindowsPrinter)) return;
        try
        {
            if (isOrderPc)
            {
                var escPosBytes = BuildEscPosBytes(destination, lines, selectedRoom, selectedTable, currentOperator.Name);
                TcpPrintClient.SendPrintJob(GetMainPcHost(), destination.WindowsPrinter, escPosBytes);
                return;
            }
            if (destination.CutAfterPrint)
            {
                try
                {
                    RawPrinter.PrintTicket(destination, selectedRoom, selectedTable, currentOperator.Name, lines, ResolvePrinterName(destination));
                    return;
                }
                catch (InvalidOperationException)
                {
                }
            }
            PrintWindowsTicket(destination, lines);
        }
        catch (Exception error)
        {
            MessageBox.Show($"Could not print cancel to {destination.WindowsPrinter}: {error.Message}", "Printing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private string ResolvePrinterName(PrinterDestination destination)
    {
        if (!isOrderPc || string.IsNullOrWhiteSpace(store.RegisterSettings.NetworkPath)) return destination.WindowsPrinter;
        var serverName = store.RegisterSettings.NetworkPath.TrimEnd('\\').Split('\\')[0];
        return $@"\\{serverName}\{destination.WindowsPrinter}";
    }

    private string GetMainPcHost()
    {
        return store.RegisterSettings.NetworkPath.TrimEnd('\\').Split('\\')[0];
    }

    private static byte[] BuildEscPosBytes(PrinterDestination destination, List<SoldLine> ticketLines, string room, string table, string operatorName, int orderNumber = 0, int billNumber = 0, int paymentNumber = 0, string orderType = "", decimal discountPercent = 0, decimal discountAmount = 0)
    {
        using var stream = new MemoryStream();
        void Write(string s) => stream.Write(Encoding.ASCII.GetBytes(s), 0, Encoding.ASCII.GetByteCount(s));

        Write("\x1B@");
        foreach (var style in TicketLayout.Components(destination))
        {
            if (style.Component == "Cutter") { Write("\x1B\x64\x05\x1D\x56\x00"); continue; }
            if (style.Component == "Blank line") { stream.WriteByte(0x0A); continue; }
            if (style.Component == "Logo" && (string.IsNullOrWhiteSpace(destination.LogoPath) || !File.Exists(destination.LogoPath))) continue;

            string? value = null;
            if (style.Component != "Logo")
            {
                value = TicketLayout.Text(style.Component, destination, room, table, operatorName, ticketLines, orderNumber, billNumber, paymentNumber, orderType, discountPercent, discountAmount);
                if (string.IsNullOrWhiteSpace(value)) continue;
            }

            Write(style.Alignment switch { "Center" => "\x1B\x61\x01", "Right" => "\x1B\x61\x02", _ => "\x1B\x61\x00" });
            var (w, h) = style.FontSize switch
            {
                >= 32 => ((byte)4, (byte)4),
                >= 26 => ((byte)3, (byte)3),
                >= 22 => ((byte)3, (byte)2),
                >= 18 => ((byte)2, (byte)2),
                >= 15 => ((byte)2, (byte)1),
                >= 12 => ((byte)1, (byte)2),
                _ => ((byte)1, (byte)1)
            };
            Write($"\x1D!{(char)h}{(char)w}");
            Write("\x1B\x2D" + (char)(style.Underline ? 1 : 0));
            if (style.Bold) Write("\x1B\x45\x01");
            stream.WriteByte(0x0A);

            if (style.Component == "Logo")
            {
                using var img = Image.FromFile(destination.LogoPath);
                var maxW = style.FontSize >= 32 ? 576 : 384;
                var scale = (double)maxW / img.Width;
                var drawW = Math.Min((int)(img.Width * scale), maxW);
                var drawH = (int)(img.Height * scale);
                using var bmp = new Bitmap(img, new Size(drawW, drawH));
                var imgBytes = RawPrinter.ImageToEscPosBytes(bmp);
                stream.Write(imgBytes, 0, imgBytes.Length);
            }
            else
            {
                Write(value!);
            }

            stream.WriteByte(0x0A);
            if (style.Bold) Write("\x1B\x45\x00");
            Write("\x1B\x2D\x00");
            Write("\x1D!\x01\x01");
            Write("\x1B\x61\x00");
        }
        if (!TicketLayout.Components(destination).Any(s => s.Component == "Cutter") && destination.CutAfterPrint)
            Write("\x1B\x64\x05\x1D\x56\x00");
        return stream.ToArray();
    }


        private void PrintWindowsTicket(PrinterDestination destination, List<SoldLine> ticketLines, int orderNumber = 0, int billNumber = 0, int paymentNumber = 0, string orderType = "", decimal discountPercent = 0, decimal discountAmount = 0, string? room = null, string? table = null)
    {
        var printerName = ResolvePrinterName(destination);
        using var document = new PrintDocument { PrinterSettings = { PrinterName = printerName } };
        if (!document.PrinterSettings.IsValid) return;
        document.PrintPage += (_, args) =>
        {
            if (args.Graphics == null) return;
            var graphics = args.Graphics;
            var y = 20;
            var margin = 20;
            var width = args.PageBounds.Width - (margin * 2);

            foreach (var style in TicketLayout.Components(destination))
            {
                if (style.Component == "Cutter") continue;
                if (style.Component == "Blank line") { y += 20; continue; }
                if (style.Component == "Logo" && (string.IsNullOrWhiteSpace(destination.LogoPath) || !File.Exists(destination.LogoPath))) continue;

                var fontStyle = FontStyle.Regular;
                if (style.Bold) fontStyle |= FontStyle.Bold;
                if (style.Underline) fontStyle |= FontStyle.Underline;

                using var font = new Font("Consolas", style.FontSize, fontStyle);

                if (style.Component == "Logo")
                {
                    using var img = Image.FromFile(destination.LogoPath);
                    var maxW = style.FontSize >= 32 ? width : Math.Min(width, 300);
                    var scale = (double)maxW / img.Width;
                    var drawW = (int)(img.Width * scale);
                    var drawH = (int)(img.Height * scale);
                    var x = style.Alignment switch
                    {
                        "Center" => margin + (width - drawW) / 2,
                        "Right" => margin + (width - drawW),
                        _ => margin
                    };
                    graphics.DrawImage(img, new Rectangle(x, y, drawW, drawH));
                    y += drawH + 2;
                }
                else
                {
                    var text = TicketLayout.Text(style.Component, destination, room ?? selectedRoom, table ?? selectedTable, currentOperator.Name, ticketLines, orderNumber, billNumber, paymentNumber, orderType, discountPercent, discountAmount);
                    if (string.IsNullOrEmpty(text)) continue;
                    var textSize = graphics.MeasureString(text, font, width);
                    var x = style.Alignment switch
                    {
                        "Center" => margin + (width - textSize.Width) / 2,
                        "Right" => margin + (width - textSize.Width),
                        _ => margin
                    };
                    graphics.DrawString(text, font, Brushes.Black, new RectangleF(x, y, width, textSize.Height));
                    y += (int)textSize.Height + 2;
                }
            }
        };
        document.Print();
    }


    private static void ApplyCurrencyCulture(string currency)
    {
        var symbol = currency switch { "EUR" => "€", "USD" => "$", "GBP" => "£", "DZD" => "DA", _ => currency };
        var ci = new System.Globalization.CultureInfo("fr-FR");
        ci.NumberFormat.CurrencySymbol = symbol;
        ci.NumberFormat.CurrencyPositivePattern = 3;
        ci.NumberFormat.CurrencyNegativePattern = 3;
        System.Threading.Thread.CurrentThread.CurrentCulture = ci;
        System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = ci;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = ci;
        System.Globalization.CultureInfo.CurrentCulture = ci;
        System.Globalization.CultureInfo.CurrentUICulture = ci;
    }

    private string TodaySales() => $"TODAY  {store.Sales.Where(sale => sale.Time.Date == DateTime.Today).Sum(sale => sale.Total):C}";

    private void ShowSales()
    {
        if (!HasPermission("View sales")) return;
        var panel = new Panel { Dock = DockStyle.Fill };
        var canRefund = store.Features.Refunds && HasPermission("Settings");
        var form = new SalesReportForm(background, panelLight, mint, store.Sales, store.RegisterSettings.Currency, canRefund, RefundSale,
            store.Features.MultiBranch && store.Features.Branches.Count > 0 ? store.Features.Branches : null);
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        panel.Controls.Add(form);
        form.Show();
        ShowView(panel, "Sales report", ShowSettings);
    }

    private void RefundSale(Sale sale)
    {
        if (sale.IsRefund || sale.Items == null || sale.Items.Count == 0) return;
        if (sale.PaymentNumber > 0 && store.Sales.Any(s => s.RefundOf == sale.PaymentNumber))
        {
            MessageBox.Show("This sale already has a refund.", "Refund", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        using var form = new Form { Text = "Refund sale", Size = new Size(460, 400), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var topLabel = new Label { Text = $"Refund items from sale {sale.Time:dd/MM HH:mm} • {sale.Table}", AutoSize = true, Location = new Point(15, 10), ForeColor = mint, MaximumSize = new Size(430, 0) };
        int y = 46;
        var checks = new List<(CheckBox cb, SoldLine line)>();
        foreach (var item in sale.Items)
        {
            var cb = new CheckBox { Text = $"{item.Quantity}x {item.Product} ({item.UnitPrice * item.Quantity:C})", AutoSize = true, Location = new Point(20, y), ForeColor = Color.White, Checked = true };
            checks.Add((cb, item));
            y += 28;
        }
        y += 10;
        var canCredit = store.Features.GiftCards && HasPermission("Gift cards") && !string.IsNullOrWhiteSpace(sale.CustomerName) && store.Customers.Any(c => c.Name == sale.CustomerName);
        var creditCheck = new CheckBox { Text = "Issue as store credit (money stays on the customer account)", AutoSize = true, Location = new Point(20, y), ForeColor = Color.White, Checked = false };
        if (canCredit)
        {
            form.Controls.Add(creditCheck);
            y += 34;
        }
        var okBtn = new Button { Text = "Refund selected", Location = new Point(15, y), Size = new Size(190, 38), BackColor = Color.FromArgb(180, 90, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        okBtn.FlatAppearance.BorderSize = 0;
        okBtn.Click += (_, _) => form.DialogResult = DialogResult.OK;
        var cancelBtn = new Button { Text = "Cancel", Location = new Point(220, y), Size = new Size(160, 38), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        cancelBtn.FlatAppearance.BorderSize = 0;
        cancelBtn.Click += (_, _) => form.DialogResult = DialogResult.Cancel;
        form.ClientSize = new Size(460, y + 60);
        form.Controls.AddRange([topLabel, okBtn, cancelBtn]);
        foreach (var (cb, _) in checks) form.Controls.Add(cb);
        form.AcceptButton = okBtn;
        form.CancelButton = cancelBtn;
        if (form.ShowDialog() != DialogResult.OK) return;
        var selected = checks.Where(x => x.cb.Checked).Select(x => x.line).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Select at least one item to refund.", "Refund", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var amount = selected.Sum(l => l.UnitPrice * l.Quantity);
        if (MessageBox.Show($"Refund {amount:C} for {selected.Count} item(s) from sale #{sale.PaymentNumber} ({sale.Table})?", "Confirm refund", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var refundLines = selected.Select(l => l with { Quantity = -l.Quantity }).ToList();
        var refundSale = sale with { Time = DateTime.Now, Total = -amount, Items = refundLines, PaymentNumber = store.NextPaymentNumber(), Tip = 0, Discount = 0, IsRefund = true, RefundOf = sale.PaymentNumber, CashAmount = sale.CashAmount >= 0 ? -sale.CashAmount : -1 };
        if (canCredit && creditCheck.Checked)
        {
            refundSale = refundSale with { PaymentType = "Store credit", CashAmount = 0 };
            var ci = store.Customers.FindIndex(c => c.Name == sale.CustomerName);
            if (ci >= 0)
            {
                store.Customers[ci] = store.Customers[ci] with { Balance = store.Customers[ci].Balance + amount };
                store.SaveCustomers(store.Customers);
            }
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Refund credit", $"{sale.CustomerName} +{amount:C} store credit (sale #{sale.PaymentNumber})");
        }
        store.AddSale(refundSale);

        if (store.Features.StockManagement && store.Recipes.Count > 0)
        {
            foreach (var line in selected)
            {
                if (!store.Recipes.TryGetValue(line.Product, out var recipeItems)) continue;
                foreach (var ri in recipeItems)
                {
                    var idx = store.Ingredients.FindIndex(i => i.Name == ri.Ingredient);
                    if (idx < 0) continue;
                    var ingr = store.Ingredients[idx];
                    store.Ingredients[idx] = ingr with { Stock = ingr.Stock + ri.Quantity * line.Quantity };
                }
            }
            store.SaveIngredients(store.Ingredients);
            Auto86Sync();
        }
        if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Refund", $"sale #{sale.PaymentNumber} {sale.Table} → -{amount:C} ({selected.Count} items)");

        var paymentPrinter = store.PrinterDestinations.FirstOrDefault(d => d.Name == "Payment");
        var payDest = store.PrinterDestinations.FirstOrDefault(d => d.Name == "Refund") ?? paymentPrinter;
        if (paymentPrinter != null && payDest != null)
            payDest = payDest with { WindowsPrinter = paymentPrinter.WindowsPrinter };
        if (payDest != null && !string.IsNullOrWhiteSpace(payDest.WindowsPrinter))
        {
            try
            {
                if (isOrderPc)
                {
                    var escPosBytes = BuildEscPosBytes(payDest, refundLines, "", sale.Table, currentOperator.Name, billNumber: sale.BillNumber, paymentNumber: refundSale.PaymentNumber, orderType: "Refund");
                    TcpPrintClient.SendPrintJob(GetMainPcHost(), payDest.WindowsPrinter, escPosBytes);
                }
                else if (payDest.CutAfterPrint)
                {
                    try { RawPrinter.PrintTicket(payDest, "", sale.Table, currentOperator.Name, refundLines, ResolvePrinterName(payDest), billNumber: sale.BillNumber, paymentNumber: refundSale.PaymentNumber, orderType: "Refund"); }
                    catch (InvalidOperationException) { PrintWindowsTicket(payDest, refundLines, billNumber: sale.BillNumber, paymentNumber: refundSale.PaymentNumber, orderType: "Refund", room: "", table: sale.Table); }
                }
                else PrintWindowsTicket(payDest, refundLines, billNumber: sale.BillNumber, paymentNumber: refundSale.PaymentNumber, orderType: "Refund", room: "", table: sale.Table);
            }
            catch (Exception error)
            {
                MessageBox.Show($"Could not print refund: {error.Message}", "Refund", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        salesLabel.Text = TodaySales();
        MessageBox.Show($"Refunded {amount:C}\n{selected.Count} item(s) from {sale.Table}", "Refund", MessageBoxButtons.OK, MessageBoxIcon.Information);
        ShowSales();
    }

    private void ShowSettings()
    {
        if (isOrderPc)
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            var form = new NetworkSettingsForm(background, panelLight, mint, store.RegisterSettings, settings =>
            {
                store.SaveRegisterSettings(settings);
                if (!settings.IsMainPc && !string.IsNullOrWhiteSpace(settings.NetworkPath))
                {
                    if (Directory.Exists(settings.NetworkPath))
                    {
                        store.SwitchToNetworkPath(settings.NetworkPath);
                        products = store.Products;
                        RenderProducts();
                        RenderRooms();
                        isOrderPc = false;
                        ApplyOrderPcRestrictions();
                    }
                    else
                    {
                        MessageBox.Show($"Network path not found:\n{settings.NetworkPath}", "Network", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    isOrderPc = false;
                    store.SwitchToLocal();
                    products = store.Products;
                    RenderProducts();
                    RenderRooms();
                    ApplyOrderPcRestrictions();
                }
            });
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            panel.Controls.Add(form);
            form.Show();
        ShowView(panel, "Network settings", ShowSettings);
            return;
        }
        if (!HasPermission("Settings")) return;
        var settingsPanel = new Panel { Dock = DockStyle.Fill };
        var settingsForm = new SettingsForm(background, panelLight, mint, ShowOperatorManager, ShowProducts, ShowFamilies, ShowRoomManager, ShowSales, ShowCurrencySettings, ShowPrinterManager, EndDay, ShowNetworkSettings,
            HasPermission("Manage operators"), HasPermission("Manage products"), HasPermission("Manage rooms"), HasPermission("View sales"), store.Features,
            features =>
            {
                store.SaveFeatures(features);
                ApplyPermissions();
                seatBar.Visible = features.SeatOrdering;
                orderHeader.Height = features.SeatOrdering ? 116 : 102;
                RenderSeatBar();
                if ((features.QROrdering || features.ApiEnabled || features.CloudSync) && !isOrderPc) StartQrServer();
                else if (!features.QROrdering && !features.ApiEnabled && !features.CloudSync) StopQrServer();
                quickSaleBtn.Visible = features.QuickSaleMode;
                barcodeBox!.Visible = features.BarcodeScanning || features.QuickSaleMode;
                trainingBar.Visible = features.TrainingMode;
                UpdateOrderButtons();
                UpdateBranchBadge();
            },
            ShowModifierManager, ShowCustomerManager, ShowHappyHourManager, ShowAuditLogViewer, ShowShiftReport, ShowCashDrawer,
            () => { floorPlanEdit = true; ShowRooms(); }, ShowEmailSettings, ManageCourseNames, ManageComboDeals, LaunchKiosk, ShowCustomerDisplay,
            ShowFeeSettings, () => ShowFindReceipt(), ShowSpecialOrders, ShowOfflineQueue, ShowVirtualTerminal, DispatchReport, ShowOrder);
        settingsForm.TopLevel = false;
        settingsForm.FormBorderStyle = FormBorderStyle.None;
        settingsForm.Dock = DockStyle.Fill;
        settingsPanel.Controls.Add(settingsForm);
        settingsForm.Show();
        ShowView(settingsPanel, "Settings");
    }

    private void ShowRoomManager()
    {
        if (!HasPermission("Manage rooms")) return;
        var panel = new Panel { Dock = DockStyle.Fill };
        var form = new RoomManagerForm(background, panelLight, mint, store.Rooms, updatedRooms =>
        {
            store.SaveRooms(updatedRooms);
            RenderRooms();
        });
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        panel.Controls.Add(form);
        form.Show();
        ShowView(panel, "Manage rooms", ShowSettings);
    }

    private void ShowCurrencySettings()
    {
        if (!HasPermission("Settings")) return;
        var panel = new Panel { Dock = DockStyle.Fill };
        var form = new CurrencySettingsForm(background, panelLight, mint, store.RegisterSettings, settings =>
        {
            store.SaveRegisterSettings(settings);
            ApplyCurrencyCulture(settings.Currency);
            salesLabel.Text = TodaySales();
            RenderProducts();
            RenderBasket();
            totalLabel.Text = NetTotal().ToString("C");
        });
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        panel.Controls.Add(form);
        form.Show();
        ShowView(panel, "Currency settings", ShowSettings);
        salesLabel.Text = TodaySales();
        RenderBasket();
    }

    private void ShowNetworkSettings()
    {
        if (!HasPermission("Settings")) return;
        var panel = new Panel { Dock = DockStyle.Fill };
        var form = new NetworkSettingsForm(background, panelLight, mint, store.RegisterSettings, settings =>
        {
            store.SaveRegisterSettings(settings);
            if (!settings.IsMainPc && !string.IsNullOrWhiteSpace(settings.NetworkPath))
            {
                if (Directory.Exists(settings.NetworkPath))
                {
                    store.SwitchToNetworkPath(settings.NetworkPath);
                    products = store.Products;
                    RenderProducts();
                    RenderRooms();
                }
                else
                {
                    MessageBox.Show($"Network path not found:\n{settings.NetworkPath}\n\nCheck that the main PC is on and the folder is shared.", "Network", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                store.SwitchToLocal();
                products = store.Products;
                RenderProducts();
                RenderRooms();
            }
        });
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        panel.Controls.Add(form);
        form.Show();
        ShowView(panel, "Network settings");
    }

    private void EndDay()
    {
        using var dlg = new Form
        {
            Text = "End of day",
            Size = new Size(430, 275),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = background,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };
        var info = new Label
        {
            Text = "Choose a report type:",
            Location = new Point(20, 15),
            AutoSize = true,
            ForeColor = Color.White
        };
        var xRadio = new RadioButton
        {
            Text = "X report — interim read, the day stays open",
            Location = new Point(20, 48),
            Size = new Size(385, 24),
            ForeColor = Color.White,
            Checked = true
        };
        var zRadio = new RadioButton
        {
            Text = "Z report — closes the day and records it",
            Location = new Point(20, 78),
            Size = new Size(385, 24),
            ForeColor = Color.White
        };
        var printBtn = new Button { Text = "Print", Location = new Point(20, 120), Size = new Size(190, 44), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
        printBtn.FlatAppearance.BorderSize = 0;
        var pdfBtn = new Button { Text = "Export as PDF", Location = new Point(220, 120), Size = new Size(190, 44), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Yes };
        pdfBtn.FlatAppearance.BorderSize = 0;
        var cancelBtn = new Button { Text = "Cancel", Location = new Point(20, 178), Size = new Size(390, 36), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
        cancelBtn.FlatAppearance.BorderSize = 0;
        dlg.Controls.AddRange([info, xRadio, zRadio, printBtn, pdfBtn, cancelBtn]);
        var choice = dlg.ShowDialog();
        if (choice == DialogResult.Cancel) return;
        var isZ = zRadio.Checked;
        if (isZ && store.Features.CashDrawer && HasPermission("Cash drawer"))
        {
            if (MessageBox.Show("Run the cash-up (count cash in drawer) before closing the day?", "Cash-up", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                ShowCashDrawer();
        }
        if (choice == DialogResult.OK) PrintDayReport(isZ);
        else ExportDayReportPdf(isZ);
    }

    private DayReportData? BuildDayReport(bool closeDay)
    {
        var kind = closeDay ? "Z REPORT" : "X REPORT";
        var todaySales = store.Sales.Where(s => s.Time.Date == DateTime.Today).ToList();
        if (todaySales.Count == 0)
        {
            MessageBox.Show($"No sales today — {kind} not available.", kind, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }
        var allLines = todaySales.SelectMany(s => s.Items ?? []).ToList();
        return new DayReportData
        {
            CloseDay = closeDay,
            Time = DateTime.Now,
            Operator = currentOperator.Name,
            Currency = store.RegisterSettings.Currency,
            Transactions = todaySales.Count,
            ItemsSold = allLines.Sum(l => l.Quantity),
            Gross = todaySales.Sum(s => s.Total),
            Discounts = todaySales.Sum(s => s.Discount),
            Tips = todaySales.Sum(s => s.Tip),
            ByPayment = todaySales.GroupBy(s => string.IsNullOrWhiteSpace(s.PaymentType) ? "Cash" : s.PaymentType).Select(g => (Name: g.Key, Count: g.Count(), Total: g.Sum(s => s.Total))).OrderByDescending(x => x.Total).ToList(),
            ByOperator = todaySales.GroupBy(s => s.Operator).Select(g => (Name: g.Key, Count: g.Count(), Total: g.Sum(s => s.Total))).OrderByDescending(x => x.Total).ToList(),
            ByProduct = allLines.GroupBy(l => l.Product).Select(g => (Name: g.Key, Qty: g.Sum(l => l.Quantity), Total: g.Sum(l => l.UnitPrice * l.Quantity))).OrderByDescending(x => x.Total).Take(10).ToList(),
            ByFamily = allLines.GroupBy(l => l.Family).Select(g => (Name: g.Key, Qty: g.Sum(l => l.Quantity), Total: g.Sum(l => l.UnitPrice * l.Quantity))).OrderByDescending(x => x.Total).ToList()
        };
    }

    private bool ConfirmZReport(DayReportData data)
    {
        if (!data.CloseDay) return true;
        var msg = $"Close the day with a Z report?\n\n{data.Transactions} transactions  •  {data.Gross:C} total";
        if (store.ZReportPrintedToday()) msg += "\n\nA Z report was already printed today.";
        return MessageBox.Show(msg, "Z report", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }

    private void FinalizeDayReport(DayReportData data, string action)
    {
        var detail = $"{data.Transactions} transactions  •  {data.Gross:N2} {data.Currency}";
        if (data.CloseDay)
        {
            store.RecordZReport(data.Transactions, data.Gross, currentOperator.Name);
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Z report", $"{action}: {detail}");
            MessageBox.Show($"Z report {action} — day closed.\n{detail}", "Z report", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (store.Features.CloudSync && !string.IsNullOrWhiteSpace(store.Features.CloudSyncUrl))
                UploadBackupToHome(true);
        }
        else
        {
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "X report", $"{action}: {detail}");
            MessageBox.Show($"X report {action} — day stays open.\n{detail}", "X report", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private List<ReportLine> BuildReportLines(DayReportData d, bool asPdf)
    {
        static string Trunc(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "...";
        var cur = d.Currency;
        var lines = new List<ReportLine>
        {
            new(d.CloseDay ? "Z REPORT" : "X REPORT", ReportLineStyles.Title),
            new(d.CloseDay ? "END OF DAY READ - DAY CLOSED" : "MID-DAY READ - DAY STAYS OPEN", ReportLineStyles.Header),
            new($"{d.Time:dd/MM/yyyy HH:mm}  Operator: {d.Operator}", ReportLineStyles.Normal),
            new("", ReportLineStyles.Rule),
            new("SUMMARY", ReportLineStyles.Header),
            new($"Transactions:   {d.Transactions}", ReportLineStyles.Normal),
            new($"Items sold:     {d.ItemsSold}", ReportLineStyles.Normal),
            new($"Gross:          {d.Gross:N2} {cur}", ReportLineStyles.Normal),
            new($"Discounts:      {d.Discounts:N2} {cur}", ReportLineStyles.Normal),
            new($"Tips:           {d.Tips:N2} {cur}", ReportLineStyles.Normal),
            new("", ReportLineStyles.Spacer),
            new("PAYMENT BREAKDOWN", ReportLineStyles.Header),
            new($"{"Type",-14} {"Sales",-6} {"Total",10}", ReportLineStyles.Header),
            new("", ReportLineStyles.Rule)
        };
        lines.AddRange(d.ByPayment.Select(p => new ReportLine($"{Trunc(p.Name, 14),-14} {p.Count,-6} {p.Total:N2} {cur}", ReportLineStyles.Normal)));
        lines.Add(new("", ReportLineStyles.Spacer));
        lines.Add(new("BY OPERATOR", ReportLineStyles.Header));
        lines.Add(new($"{"Sales",-6} {"Operator",-24} {"Total",10}", ReportLineStyles.Header));
        lines.Add(new("", ReportLineStyles.Rule));
        lines.AddRange(d.ByOperator.Select(o => new ReportLine($"{o.Count,-6} {Trunc(o.Name, 24),-24} {o.Total:N2} {cur}", ReportLineStyles.Normal)));
        lines.Add(new("", ReportLineStyles.Spacer));
        lines.Add(new("TOP PRODUCTS", ReportLineStyles.Header));
        lines.Add(new($"{"Qty",-6} {"Product",-24} {"Total",10}", ReportLineStyles.Header));
        lines.Add(new("", ReportLineStyles.Rule));
        lines.AddRange(d.ByProduct.Select(p => new ReportLine($"{p.Qty,-6} {Trunc(p.Name, 24),-24} {p.Total:N2} {cur}", ReportLineStyles.Normal)));
        lines.Add(new("", ReportLineStyles.Spacer));
        lines.Add(new("BY FAMILY", ReportLineStyles.Header));
        lines.Add(new($"{"Qty",-6} {"Family",-24} {"Total",10}", ReportLineStyles.Header));
        lines.Add(new("", ReportLineStyles.Rule));
        lines.AddRange(d.ByFamily.Select(f => new ReportLine($"{f.Qty,-6} {Trunc(f.Name, 24),-24} {f.Total:N2} {cur}", ReportLineStyles.Normal)));
        lines.Add(new("", ReportLineStyles.Spacer));
        lines.Add(new("", ReportLineStyles.Rule));
        lines.Add(new($"{(asPdf ? "Exported" : "Printed")}: {DateTime.Now:dd/MM/yyyy HH:mm}   {(d.CloseDay ? "Day closed" : "Day stays open")}", ReportLineStyles.Small));
        return lines;
    }

    private void PrintDayReport(bool closeDay)
    {
        var data = BuildDayReport(closeDay);
        if (data == null) return;
        if (!ConfirmZReport(data)) return;
        var kind = closeDay ? "Z REPORT" : "X REPORT";

        var payDest = store.PrinterDestinations.FirstOrDefault(d => d.Name == "Payment") ?? store.PrinterDestinations.FirstOrDefault(d => d.Name == "Bill");
        if (payDest == null || string.IsNullOrWhiteSpace(payDest.WindowsPrinter))
        {
            MessageBox.Show("No printer configured for reports.\nSet a Payment or Bill printer in Settings > Printer routing.", kind, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var renderLines = BuildReportLines(data, false);
        try
        {
            using var document = new PrintDocument { PrinterSettings = { PrinterName = payDest.WindowsPrinter } };
            if (!document.PrinterSettings.IsValid) { MessageBox.Show("Printer not available.", kind, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            document.PrintPage += (_, args) =>
            {
                if (args.Graphics == null) return;
                var g = args.Graphics;
                var y = 20;
                var margin = 20;
                var width = args.PageBounds.Width - (margin * 2);
                using var titleFont = new Font("Consolas", 14F, FontStyle.Bold);
                using var headerFont = new Font("Consolas", 10F, FontStyle.Bold);
                using var font = new Font("Consolas", 10F);
                using var smallFont = new Font("Consolas", 8F);
                using var grayPen = new Pen(Color.Gray);
                foreach (var line in renderLines)
                {
                    switch (line.Style)
                    {
                        case ReportLineStyles.Title: g.DrawString(line.Text, titleFont, Brushes.Black, margin, y); break;
                        case ReportLineStyles.Header: g.DrawString(line.Text, headerFont, Brushes.Black, margin, y); break;
                        case ReportLineStyles.Small: g.DrawString(line.Text, smallFont, Brushes.Gray, margin, y); break;
                        case ReportLineStyles.Rule: g.DrawLine(grayPen, margin, y + 4, margin + width, y + 4); break;
                        case ReportLineStyles.Spacer: break;
                        default: g.DrawString(line.Text, font, Brushes.Black, margin, y); break;
                    }
                    y += (int)ReportLineStyles.Height(line.Style);
                }
            };
            document.Print();
            FinalizeDayReport(data, "printed");
        }
        catch (Exception error)
        {
            MessageBox.Show($"Could not print report: {error.Message}", kind, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ExportDayReportPdf(bool closeDay)
    {
        var data = BuildDayReport(closeDay);
        if (data == null) return;
        if (!ConfirmZReport(data)) return;
        var kind = closeDay ? "Z REPORT" : "X REPORT";
        using var save = new SaveFileDialog
        {
            Title = "Export report as PDF",
            Filter = "PDF file|*.pdf",
            FileName = $"{(closeDay ? "Z" : "X")}-report-{DateTime.Now:yyyy-MM-dd-HHmm}.pdf"
        };
        if (save.ShowDialog() != DialogResult.OK) return;
        try
        {
            TicketPdf.Save(save.FileName, BuildReportLines(data, true));
            FinalizeDayReport(data, "exported to PDF");
        }
        catch (Exception error)
        {
            MessageBox.Show($"Could not export PDF: {error.Message}", kind, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowFamilies()
    {
        if (!HasPermission("Manage products")) return;
        var panel = new Panel { Dock = DockStyle.Fill };
        var form = new FamilyManagerForm(background, panelLight, mint, store.Families, products, store.PrinterDestinations, (updatedFamilies, updatedProducts) =>
        {
            store.SaveFamilies(updatedFamilies);
            products.Clear();
            products.AddRange(updatedProducts);
            store.SaveProducts(products);
            RenderProducts();
        });
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        panel.Controls.Add(form);
        form.Show();
        ShowView(panel, "Manage families", ShowSettings);
    }

    private void ShowOperator()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var picker = new OperatorPickerForm(background, panelLight, mint, store.Operators);
        picker.OperatorSelected += op =>
        {
            currentOperator = op;
            UpdateBranchBadge();
            greetingLabel.Text = GreetingText();
            orderLabel.Text = OrderContextText();
            ApplyPermissions();
            ShowOrder();
        };
        picker.TopLevel = false;
        picker.FormBorderStyle = FormBorderStyle.None;
        picker.Dock = DockStyle.Fill;
        panel.Controls.Add(picker);
        picker.Show();
        ShowView(panel, "Select operator");
    }

    private void ShowOperatorManager()
    {
        if (!HasPermission("Manage operators")) return;
        var panel = new Panel { Dock = DockStyle.Fill };
        var form = new OperatorManagerForm(background, panelLight, mint, store.Operators, updatedOperators => store.SaveOperators(updatedOperators));
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        panel.Controls.Add(form);
        form.Show();
        ShowView(panel, "Manage operators", ShowSettings);
    }

    private string GreetingText()
    {
        var hour = DateTime.Now.Hour;
        var greeting = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
        return $"{greeting}, {currentOperator.Name}{BranchSuffix()}";
    }

    private bool HasPermission(string permission) => currentOperator.Permissions.Contains("All permissions") || currentOperator.Permissions.Contains(permission);

    private void ApplyPermissions()
    {
        roomsNavButton.Visible = HasPermission("Manage rooms");
        settingsNavButton.Visible = HasPermission("Settings");
        clockInOutBtn.Visible = store.Features.ClockInOut;
        stockBtn.Visible = store.Features.StockManagement;
        kdsNavButton.Visible = store.Features.KitchenDisplay;
        eightySixNavButton.Visible = store.Features.EightySixBoard;
        reservationsNavButton.Visible = store.Features.Reservations && HasPermission("Reservations");
        scheduleNavButton.Visible = store.Features.Scheduling && HasPermission("Manage operators");
        giftCardsNavButton.Visible = store.Features.GiftCards && HasPermission("Gift cards");
        purchasesNavButton.Visible = store.Features.Purchasing && HasPermission("Purchasing");
        drawerNavButton.Visible = store.Features.CashDrawer && HasPermission("Cash drawer");
        UpdateQrBanner();
    }

    private void UpdateQrBanner()
    {
        var enabled = (store.Features.QROrdering || store.Features.KioskMode) && !isOrderPc;
        qrPanel.Visible = enabled;
        if (!enabled) return;
        var pending = store.OpenOrders.Count(o => o.Source is "QR" or "Kiosk");
        qrLabel.Text = $"  Guest orders live — {EffectiveGuestUrl()} — {pending} open order(s), tap to manage";
    }

    private void UpdateNavVisibility() => ApplyPermissions();

    private void UpdateTabs(FlowLayoutPanel tabs)
    {
        foreach (Button tab in tabs.Controls)
        {
            var text = tab.Text;
            tab.BackColor = text == category ? mint : panelLight;
            tab.ForeColor = text == category ? background : Color.White;
        }
    }

    private void SplitBill()
    {
        var existing = store.GetOpenOrder(selectedRoom, selectedTable);
        if (existing == null || existing.Items.Count == 0) return;
        using var form = new Form { Text = "Split bill", Size = new Size(500, 380), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var topLabel = new Label { Text = "Select the items this person pays for:", AutoSize = true, Location = new Point(15, 10), ForeColor = mint };
        var itemChecks = new List<(CheckBox cb, SoldLine line)>();
        int y = 40;
        if (store.Features.SeatOrdering && existing.Items.Any(i => i.Seat > 0))
        {
            var seats = existing.Items.Where(i => i.Seat > 0).Select(i => i.Seat).Distinct().OrderBy(s => s).ToList();
            if (seats.Count > 1)
            {
                var seatCap = new Label { Text = "Select by seat:", AutoSize = true, Location = new Point(20, y + 4), ForeColor = Color.FromArgb(143, 154, 171) };
                form.Controls.Add(seatCap);
                var sx = 130;
                var extraRows = 0;
                foreach (var s in seats)
                {
                    if (sx > 430) { sx = 130; extraRows++; }
                    var seatNo = s;
                    var chip = new Button { Text = "S" + s, Size = new Size(44, 26), Location = new Point(sx, y + extraRows * 30), FlatStyle = FlatStyle.Flat, BackColor = panelLight, ForeColor = Color.White };
                    chip.FlatAppearance.BorderSize = 0;
                    chip.Click += (_, _) => { foreach (var check in itemChecks) check.cb.Checked = check.line.Seat == seatNo; };
                    form.Controls.Add(chip);
                    sx += 50;
                }
                y += 34 + extraRows * 30;
            }
        }
        foreach (var item in existing.Items)
        {
            var seatPrefix = store.Features.SeatOrdering && item.Seat > 0 ? $"[S{item.Seat}] " : "";
            var cb = new CheckBox { Text = $"{seatPrefix}{item.Quantity}x {item.Product} ({item.UnitPrice * item.Quantity:C})", AutoSize = true, Location = new Point(20, y), ForeColor = Color.White };
            itemChecks.Add((cb, item));
            y += 28;
        }
        y += 12;
        var okBtn = new Button { Text = "Pay selected items", Location = new Point(15, y), Size = new Size(170, 38), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        okBtn.FlatAppearance.BorderSize = 0;
        okBtn.Click += (_, _) => form.DialogResult = DialogResult.OK;
        form.ClientSize = new Size(500, Math.Max(340, y + 66));
        form.Controls.AddRange([topLabel, okBtn]);
        foreach (var (cb, _) in itemChecks) form.Controls.Add(cb);
        form.AcceptButton = okBtn;
        if (form.ShowDialog() != DialogResult.OK) return;
        var selectedLines = itemChecks.Where(x => x.cb.Checked).Select(x => x.line).ToList();
        if (selectedLines.Count == 0)
        {
            MessageBox.Show("Select at least one item to pay for.", "Split bill", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var selectedTotal = selectedLines.Sum(l => l.UnitPrice * l.Quantity);
        var splitDiscount = basketDiscountPercent > 0 ? Math.Round(selectedTotal * basketDiscountPercent / 100m, 2) : 0m;
        var payDue = selectedTotal - splitDiscount;

        using var payForm = new Form { Text = "Split payment", Size = new Size(400, 268), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var payTotal = new Label { Text = $"Total: {selectedTotal:C}", AutoSize = true, Font = new Font("Segoe UI Semibold", 16F), ForeColor = mint, Location = new Point(20, 20) };
        var payInfo = new Label { Text = splitDiscount > 0 ? $"{selectedLines.Sum(l => l.Quantity)} item(s) • Discount (-{basketDiscountPercent:0.#}%): -{splitDiscount:C}" : $"{selectedLines.Sum(l => l.Quantity)} item(s) on this payment", AutoSize = true, ForeColor = Color.FromArgb(143, 154, 171), Location = new Point(22, 56) };
        var payTypeLabel = new Label { Text = "Payment type:", AutoSize = true, Location = new Point(20, 100) };
        var paymentCombo = new ComboBox { Size = new Size(210, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White, Location = new Point(150, 96) };
        paymentCombo.Items.AddRange(["Cash", "Card", "Transfer", "Mobile"]);
        paymentCombo.SelectedIndex = 0;
        var payOk = new Button { Text = $"Accept payment  {payDue:C}", Location = new Point(20, 142), Size = new Size(345, 46), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 12F) };
        payOk.FlatAppearance.BorderSize = 0;
        payOk.Click += (_, _) => payForm.DialogResult = DialogResult.OK;
        var payCancel = new Button { Text = "Cancel", Location = new Point(20, 198), Size = new Size(345, 34), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        payCancel.FlatAppearance.BorderSize = 0;
        payCancel.Click += (_, _) => payForm.DialogResult = DialogResult.Cancel;
        payForm.Controls.AddRange([payTotal, payInfo, payTypeLabel, paymentCombo, payOk, payCancel]);
        payForm.AcceptButton = payOk;
        payForm.CancelButton = payCancel;
        if (payForm.ShowDialog() != DialogResult.OK) return;
        var paymentType = paymentCombo.Text;

        var payNum = store.NextPaymentNumber();
        store.AddSale(new Sale(DateTime.Now, payDue, selectedTable, currentOperator.Name, selectedLines, payNum, existing.BillNumber, PaymentType: paymentType, OrderType: existing.OrderType, CustomerName: existing.CustomerName, Discount: splitDiscount));
        if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Payment", $"Split {payDue:C} ({paymentType}) {selectedTable}");
        if (store.Features.StockManagement && store.Recipes.Count > 0)
        {
            var alerts = new List<StockAlert>();
            foreach (var line in selectedLines)
            {
                if (store.Recipes.TryGetValue(line.Product, out var recipeItems))
                {
                    foreach (var ri in recipeItems)
                    {
                        var idx = store.Ingredients.FindIndex(i => i.Name == ri.Ingredient);
                        if (idx >= 0)
                        {
                            var ingr = store.Ingredients[idx];
                            var newStock = ingr.Stock - ri.Quantity * line.Quantity;
                            store.Ingredients[idx] = ingr with { Stock = newStock };
                            if (store.Features.InventoryAlerts && newStock <= ingr.MinStock)
                                alerts.Add(new StockAlert(line.Product, ri.Ingredient, newStock, ingr.MinStock));
                        }
                    }
                }
            }
            store.SaveIngredients(store.Ingredients);
            Auto86Sync();
            if (alerts.Count > 0)
                MessageBox.Show("LOW STOCK ALERTS:\n\n" + string.Join("\n", alerts.Select(a => $"• {a.Ingredient}: {a.Remaining:F1} remaining (min: {a.MinStock:F1})\n  Used by: {a.ProductName}")),
                    "Stock Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        var remaining = existing.Items.Except(selectedLines).ToList();
        store.RemoveOpenOrder(selectedRoom, selectedTable);
        if (remaining.Count > 0)
            store.AddOpenOrder(new OpenOrder(selectedRoom, selectedTable, currentOperator.Name, existing.Created, remaining, existing.Note, existing.OrderNumber, existing.BillNumber, existing.OrderType, existing.CustomerName, existing.SeatCount, existing.Ready));

        var payDest = store.PrinterDestinations.FirstOrDefault(d => d.Name == "Payment");
        if (payDest != null && !string.IsNullOrWhiteSpace(payDest.WindowsPrinter))
        {
            try
            {
                if (isOrderPc)
                {
                    var escPosBytes = BuildEscPosBytes(payDest, selectedLines, selectedRoom, selectedTable, currentOperator.Name, billNumber: existing.BillNumber, paymentNumber: payNum, discountPercent: basketDiscountPercent, discountAmount: splitDiscount);
                    TcpPrintClient.SendPrintJob(GetMainPcHost(), payDest.WindowsPrinter, escPosBytes);
                }
                else if (payDest.CutAfterPrint)
                {
                    try { RawPrinter.PrintTicket(payDest, selectedRoom, selectedTable, currentOperator.Name, selectedLines, ResolvePrinterName(payDest), billNumber: existing.BillNumber, paymentNumber: payNum, discountPercent: basketDiscountPercent, discountAmount: splitDiscount); }
                    catch (InvalidOperationException) { PrintWindowsTicket(payDest, selectedLines, billNumber: existing.BillNumber, paymentNumber: payNum, discountPercent: basketDiscountPercent, discountAmount: splitDiscount); }
                }
                else PrintWindowsTicket(payDest, selectedLines, billNumber: existing.BillNumber, paymentNumber: payNum, discountPercent: basketDiscountPercent, discountAmount: splitDiscount);
            }
            catch (Exception error)
            {
                MessageBox.Show($"Could not print payment: {error.Message}", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        MessageBox.Show($"Payment accepted\n{payDue:C}  •  {selectedTable}", "Split bill", MessageBoxButtons.OK, MessageBoxIcon.Information);
        basket.Clear();
        activeSeat = 1;
        hasOpenOrder = remaining.Count > 0;
        if (hasOpenOrder)
            foreach (var item in remaining)
            {
                var p = products.FirstOrDefault(pr => pr.Name == item.Product);
                if (p != null) basket.Add((p, item.Quantity, item.Course, item.Note, item.Seat, item.Modifiers));
            }
        UpdateOrderButtons();
        salesLabel.Text = TodaySales();
        RenderBasket();
        orderLabel.Text = OrderContextText();
        var currentRoom = store.Rooms.FirstOrDefault(r => r.Name == selectedRoom);
        if (currentRoom != null) RenderTableMap(currentRoom);
    }

    private void TransferTable()
    {
        var existing = store.GetOpenOrder(selectedRoom, selectedTable);
        if (existing == null) return;
        var allTables = new List<(string Room, string Table)>();
        foreach (var room in store.Rooms)
            for (int i = 1; i <= room.TableCount; i++)
            {
                var tn = TableName(room.Name, i);
                if (store.GetOpenOrder(room.Name, tn) == null)
                    allTables.Add((room.Name, tn));
            }
        using var form = new Form { Text = "Transfer to...", Size = new Size(350, 300), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var tableList = new ListBox { Location = new Point(15, 10), Size = new Size(305, 200), BackColor = panelLight, ForeColor = Color.White };
        foreach (var t in allTables) tableList.Items.Add($"{t.Room} → {t.Table}");
        if (tableList.Items.Count == 0) { tableList.Items.Add("No available tables"); }
        var okBtn = new Button { Text = "Transfer", Location = new Point(15, 218), Size = new Size(140, 34), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        okBtn.FlatAppearance.BorderSize = 0;
        okBtn.Enabled = false;
        tableList.SelectedIndexChanged += (_, _) => okBtn.Enabled = tableList.SelectedIndex >= 0;
        okBtn.Click += (_, _) => form.DialogResult = DialogResult.OK;
        form.Controls.AddRange([tableList, okBtn]);
        if (form.ShowDialog() != DialogResult.OK || tableList.SelectedIndex < 0) return;
        var (destRoom, destTable) = allTables[tableList.SelectedIndex];
        store.RemoveOpenOrder(selectedRoom, selectedTable);
        store.AddOpenOrder(existing with { Room = destRoom, Table = destTable });
        if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Transfer", $"{selectedRoom}/{selectedTable} → {destRoom}/{destTable}");
        MessageBox.Show($"Order transferred to {destTable} ({destRoom})", "Transfer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        basket.Clear();
        activeSeat = 1;
        hasOpenOrder = false;
        UpdateOrderButtons();
        RenderBasket();
        ShowRooms();
    }

    private void MergeTables()
    {
        var target = store.GetOpenOrder(selectedRoom, selectedTable);
        if (target == null) return;
        var candidates = new List<(string Room, string Table, int Items)>();
        foreach (var room in store.Rooms)
            for (int i = 1; i <= room.TableCount; i++)
            {
                var tn = TableName(room.Name, i);
                if (room.Name == selectedRoom && tn == selectedTable) continue;
                var order = store.GetOpenOrder(room.Name, tn);
                if (order != null) candidates.Add((room.Name, tn, order.Items.Count));
            }
        if (candidates.Count == 0)
        {
            MessageBox.Show("No other occupied tables to merge with.", "Merge tables", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var form = new Form { Text = "Merge into " + selectedTable, Size = new Size(350, 300), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var tableList = new ListBox { Location = new Point(15, 10), Size = new Size(305, 200), BackColor = panelLight, ForeColor = Color.White };
        foreach (var t in candidates) tableList.Items.Add($"{t.Room} → {t.Table} ({t.Items} items)");
        var okBtn = new Button { Text = "Merge", Location = new Point(15, 218), Size = new Size(140, 34), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        okBtn.FlatAppearance.BorderSize = 0;
        okBtn.Enabled = false;
        tableList.SelectedIndexChanged += (_, _) => okBtn.Enabled = tableList.SelectedIndex >= 0;
        okBtn.Click += (_, _) => form.DialogResult = DialogResult.OK;
        form.Controls.AddRange([tableList, okBtn]);
        if (form.ShowDialog() != DialogResult.OK || tableList.SelectedIndex < 0) return;
        var (srcRoom, srcTable, _) = candidates[tableList.SelectedIndex];
        var source = store.GetOpenOrder(srcRoom, srcTable);
        if (source == null) return;

        var seatOffset = 0;
        if (store.Features.SeatOrdering && source.Items.Any(i => i.Seat > 0))
            seatOffset = Math.Max(target.SeatCount, target.Items.Count > 0 ? target.Items.Max(i => i.Seat) : 0);
        var moved = source.Items.Select(i => i with { Seat = i.Seat > 0 ? i.Seat + seatOffset : i.Seat }).ToList();
        var mergedItems = target.Items.Concat(moved).ToList();
        var merged = target with
        {
            Items = mergedItems,
            SeatCount = Math.Max(target.SeatCount, mergedItems.Count > 0 ? mergedItems.Max(i => i.Seat) : 0),
            Ready = false
        };
        store.RemoveOpenOrder(srcRoom, srcTable);
        store.RemoveOpenOrder(selectedRoom, selectedTable);
        store.AddOpenOrder(merged);

        foreach (var item in moved)
        {
            var product = products.FirstOrDefault(p => p.Name == item.Product);
            if (product == null) continue;
            var idx = basket.FindIndex(b => b.Product == product && b.Note == item.Note && b.Modifiers == item.Modifiers && (!store.Features.SeatOrdering || b.Seat == item.Seat));
            if (idx >= 0) basket[idx] = (product, basket[idx].Count + item.Quantity, basket[idx].Course, basket[idx].Note, basket[idx].Seat, JoinMods(basket[idx].Modifiers, item.Modifiers));
            else basket.Add((product, item.Quantity, item.Course, item.Note, item.Seat, item.Modifiers));
        }
        hasOpenOrder = true;
        activeSeat = 1;
        UpdateOrderButtons();
        RenderBasket();
        orderLabel.Text = OrderContextText();
        if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Merge", $"{srcRoom}/{srcTable} → {selectedRoom}/{selectedTable}");
        MessageBox.Show($"Merged {source.Items.Count} items from {srcTable} ({srcRoom}) into {selectedTable}.\n{srcTable} is now free.", "Merge tables", MessageBoxButtons.OK, MessageBoxIcon.Information);
        var currentRoom = store.Rooms.FirstOrDefault(r => r.Name == selectedRoom);
        if (currentRoom != null) RenderTableMap(currentRoom);
    }

    private void ClockInOut()
    {
        using var form = new Form { Text = "Clock In / Out", Size = new Size(320, 200), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var active = store.Shifts.FirstOrDefault(s => s.Operator == currentOperator.Name && s.ClockOut == null);
        Label statusLabel;
        Button toggleBtn;
        if (active != null)
        {
            statusLabel = new Label { Text = $"Clocked in at {active.ClockIn:HH:mm}", AutoSize = true, Location = new Point(20, 20), ForeColor = mint };
            toggleBtn = new Button { Text = "Clock Out", Location = new Point(20, 60), Size = new Size(270, 40), BackColor = Color.FromArgb(180, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            toggleBtn.Click += (_, _) =>
            {
                var idx = store.Shifts.IndexOf(active);
                store.Shifts[idx] = active with { ClockOut = DateTime.Now };
                store.SaveShifts(store.Shifts);
                if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Clock Out", active.ClockIn.ToString("HH:mm"));
                form.DialogResult = DialogResult.OK;
            };
        }
        else
        {
            statusLabel = new Label { Text = "Not currently clocked in", AutoSize = true, Location = new Point(20, 20), ForeColor = Color.FromArgb(180, 140, 100) };
            toggleBtn = new Button { Text = "Clock In", Location = new Point(20, 60), Size = new Size(270, 40), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
            toggleBtn.Click += (_, _) =>
            {
                store.Shifts.Add(new ShiftRecord(currentOperator.Name, DateTime.Now));
                store.SaveShifts(store.Shifts);
                if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Clock In", DateTime.Now.ToString("HH:mm"));
                form.DialogResult = DialogResult.OK;
            };
        }
        toggleBtn.FlatAppearance.BorderSize = 0;
        form.Controls.AddRange([statusLabel, toggleBtn]);
        form.ShowDialog();
    }

    private void ShowShiftReport()
    {
        var active = store.Shifts.Where(s => s.Operator == currentOperator.Name && s.ClockOut != null && s.ClockOut.Value.Date == DateTime.Today).ToList();
        using var form = new Form { Text = "Shift Report", Size = new Size(500, 360), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var lv = new ListView { Location = new Point(15, 10), Size = new Size(455, 250), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true };
        lv.Columns.Add("Clock In", 100); lv.Columns.Add("Clock Out", 100); lv.Columns.Add("Duration", 100); lv.Columns.Add("Operator", 130);
        foreach (var s in active)
        {
            var dur = s.ClockOut.Value - s.ClockIn;
            lv.Items.Add(new ListViewItem([s.ClockIn.ToString("HH:mm"), s.ClockOut?.ToString("HH:mm") ?? "-", $"{dur.Hours}h{dur.Minutes}m", s.Operator]));
        }
        var todaySales = store.Sales.Where(s => s.Time.Date == DateTime.Today).ToList();
        var todayTotal = todaySales.Sum(s => s.Total);
        var summary = new Label { Text = $"Today: {todaySales.Count} sales  •  {todayTotal:C}\nShifts: {active.Count}", AutoSize = true, Location = new Point(15, 268), ForeColor = mint };
        form.Controls.AddRange([lv, summary]);
        form.ShowDialog();
    }

    private void DispatchReport(string id)
    {
        switch (id)
        {
            case "Payroll": ShowPayrollExport(); break;
            case "ServerPerf": ShowServerPerformance(); break;
            case "EmpPct": PromptEmployeeDiscountPct(); break;
            case "LiveDash": ShowLiveDashboard(); break;
            case "CloseDay": ShowCloseOfDayScreen(); break;
            case "DiscVoid": ShowDiscountVoidReport(); break;
            case "Heatmap": ShowOccupancyHeatmap(); break;
            case "MenuEng": ShowMenuEngineering(); break;
            case "AcctExport": ShowAccountingExport(); break;
            case "Insights": ShowAiInsights(); break;
            case "Backup": ShowBackupDialog(); break;
            case "ApiCfg": ShowApiConfigDialog(); break;
            case "Branches": ShowBranchesDialog(); break;
            case "GuestUrl": ShowGuestUrlDialog(); break;
            case "PrepAnalytics": ShowPrepAnalytics(); break;
            case "CloudSync": ShowCloudSyncDialog(); break;
        }
    }

    private Form ReportShell(string title, int w, int h)
    {
        return new Form { Text = title, Size = new Size(w, h), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
    }

    private Button ReportButton(string text, Point loc, Size size, Color back, Color fore)
    {
        var b = new Button { Text = text, Location = loc, Size = size, BackColor = back, ForeColor = fore, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    private void PromptEmployeeDiscountPct()
    {
        using var f = ReportShell("Employee discount %", 340, 210);
        var lbl = new Label { Text = "Discount % for staff purchases:", AutoSize = true, Location = new Point(20, 16) };
        var num = new NumericUpDown { Minimum = 0, Maximum = 100, DecimalPlaces = 1, Increment = 5, Value = Math.Min(100, store.Features.EmployeeDiscountPct), Location = new Point(20, 46), Size = new Size(140, 30), BackColor = panelLight, ForeColor = Color.White };
        var ok = ReportButton("Save", new Point(20, 96), new Size(120, 38), mint, background);
        ok.Click += (_, _) => { store.Features.EmployeeDiscountPct = num.Value; store.SaveFeatures(store.Features); f.DialogResult = DialogResult.OK; };
        var cancel = ReportButton("Cancel", new Point(150, 96), new Size(110, 38), panelLight, Color.White);
        cancel.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
        f.Controls.AddRange([lbl, num, ok, cancel]);
        f.ShowDialog();
    }

    private void ShowPayrollExport()
    {
        if (!store.Features.PayrollExport) return;
        using var f = ReportShell("Payroll export", 740, 530);
        var from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var head = new Label { Text = $"Pay period: {from:MMMM yyyy} (1 → today)", AutoSize = true, Location = new Point(14, 12), ForeColor = mint, Font = new Font("Segoe UI Semibold", 12F) };
        var lv = new ListView { Location = new Point(14, 42), Size = new Size(696, 340), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, GridLines = true };
        lv.Columns.AddRange([new ColumnHeader { Text = "Operator", Width = 170 }, new ColumnHeader { Text = "Shifts", Width = 70 }, new ColumnHeader { Text = "Hours", Width = 80 }, new ColumnHeader { Text = "Wage/h", Width = 90 }, new ColumnHeader { Text = "Gross pay", Width = 110 }, new ColumnHeader { Text = "Clocked in", Width = 90 }]);
        decimal grand = 0;
        var rows = new List<(string Name, int Shifts, decimal Hours, decimal Wage, decimal Pay)>();
        foreach (var op in store.Operators)
        {
            var shifts = store.Shifts.Where(s => s.Operator == op.Name && s.ClockIn >= from).ToList();
            var hours = (decimal)shifts.Sum(s => ((s.ClockOut ?? DateTime.Now) - s.ClockIn).TotalHours);
            var pay = Math.Round(hours * op.Wage, 2);
            grand += pay;
            var open = shifts.Count(s => s.ClockOut == null);
            rows.Add((op.Name, shifts.Count, hours, op.Wage, pay));
            lv.Items.Add(new ListViewItem([op.Name, shifts.Count.ToString(), hours.ToString("0.0"), op.Wage.ToString("C"), pay.ToString("C"), open.ToString()]));
        }
        var total = new Label { Text = $"Total gross payroll: {grand:C}", AutoSize = true, Location = new Point(14, 394), ForeColor = Color.Orange, Font = new Font("Segoe UI Semibold", 13F) };
        var export = ReportButton("Export CSV", new Point(14, 434), new Size(170, 40), mint, background);
        export.Click += (_, _) =>
        {
            using var dlg = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", FileName = $"payroll-{DateTime.Now:yyyy-MM}.csv" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            var sb = new StringBuilder("Operator,Shifts,Hours,WagePerHour,GrossPay\n");
            foreach (var r in rows) sb.AppendLine($"{r.Name},{r.Shifts},{r.Hours.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)},{r.Wage.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)},{r.Pay.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");
            sb.AppendLine($"TOTAL,,,{grand.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");
            File.WriteAllText(dlg.FileName, sb.ToString());
            MessageBox.Show($"Payroll CSV exported:\n{dlg.FileName}", "Payroll export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        var close = ReportButton("Close", new Point(580, 434), new Size(130, 40), panelLight, Color.White);
        close.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([head, lv, total, export, close]);
        f.ShowDialog();
    }

    private void ShowServerPerformance()
    {
        if (!store.Features.ServerPerformance) return;
        using var f = ReportShell("Server performance", 780, 530);
        var today = store.Sales.Where(s => s.Time.Date == DateTime.Today && !s.IsRefund).ToList();
        var head = new Label { Text = $"Today — {DateTime.Today:dd/MM/yyyy}", AutoSize = true, Location = new Point(14, 12), ForeColor = mint, Font = new Font("Segoe UI Semibold", 12F) };
        var lv = new ListView { Location = new Point(14, 42), Size = new Size(736, 350), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, GridLines = true };
        lv.Columns.AddRange([new ColumnHeader { Text = "Server", Width = 160 }, new ColumnHeader { Text = "Sales", Width = 80 }, new ColumnHeader { Text = "Gross", Width = 110 }, new ColumnHeader { Text = "Items", Width = 80 }, new ColumnHeader { Text = "Avg ticket", Width = 110 }, new ColumnHeader { Text = "Discounts", Width = 90 }, new ColumnHeader { Text = "Refunds", Width = 90 }]);
        var best = ""; decimal bestGross = 0;
        foreach (var g in today.GroupBy(s => s.Operator).OrderByDescending(g => g.Sum(s => s.Total)))
        {
            var gross = g.Sum(s => s.Total);
            if (gross > bestGross) { bestGross = gross; best = g.Key; }
            var items = g.Sum(s => s.Items?.Sum(i => i.Quantity) ?? 0);
            var avg = g.Count() > 0 ? gross / g.Count() : 0;
            var discs = g.Count(s => s.Discount > 0);
            var refunds = store.Sales.Count(s => s.Time.Date == DateTime.Today && s.IsRefund && s.Operator == g.Key);
            lv.Items.Add(new ListViewItem([g.Key, g.Count().ToString(), gross.ToString("C"), items.ToString(), avg.ToString("C"), discs.ToString(), refunds.ToString()]));
        }
        var summary = new Label { Text = today.Count == 0 ? "No sales yet today." : $"Top server: {best} ({bestGross:C})   •   {today.Count} sales   •   {today.Sum(s => s.Total):C} gross", AutoSize = true, Location = new Point(14, 404), ForeColor = Color.Orange, Font = new Font("Segoe UI Semibold", 11F) };
        var close = ReportButton("Close", new Point(620, 452), new Size(130, 40), panelLight, Color.White);
        close.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([head, lv, summary, close]);
        f.ShowDialog();
    }

    private void ShowLiveDashboard()
    {
        if (!store.Features.LiveDashboard) return;
        using var f = ReportShell("Live dashboard", 1000, 640);
        var today = store.Sales.Where(s => s.Time.Date == DateTime.Today).ToList();
        var gross = today.Where(s => !s.IsRefund).Sum(s => s.Total);
        var count = today.Count(s => !s.IsRefund);
        var kpi1 = new Label { Text = $"Sales\n{gross:C}", AutoSize = true, Location = new Point(20, 14), ForeColor = mint, Font = new Font("Segoe UI Semibold", 14F) };
        var kpi2 = new Label { Text = $"Transactions\n{count}", AutoSize = true, Location = new Point(200, 14), ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 14F) };
        var kpi3 = new Label { Text = $"Avg ticket\n{(count > 0 ? (gross / count).ToString("C") : 0m.ToString("C"))}", AutoSize = true, Location = new Point(380, 14), ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 14F) };
        var kpi4 = new Label { Text = $"Tips\n{today.Sum(s => s.Tip):C}", AutoSize = true, Location = new Point(560, 14), ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 14F) };
        var kpi5 = new Label { Text = $"Open tables\n{store.OpenOrders.Count}", AutoSize = true, Location = new Point(730, 14), ForeColor = Color.Orange, Font = new Font("Segoe UI Semibold", 14F) };
        var graph = new Panel { Location = new Point(20, 86), Size = new Size(720, 360), BackColor = panelLight };
        List<Sale> gToday = today;
        graph.Paint += (_, e) =>
        {
            var hours = Enumerable.Range(6, 18).Select(h => (Hour: h, Value: gToday.Where(s => s.Time.Hour == h).Sum(s => s.Total))).ToList();
            var max = Math.Max(1m, hours.Max(x => x.Value));
            var x = 14;
            foreach (var row in hours)
            {
                var bh = (int)(280.0 * (double)row.Value / (double)max);
                if (row.Value > 0) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(60, 190, 120)), new Rectangle(x, 300 - bh, 34, bh));
                if (row.Value > 0) e.Graphics.DrawString(row.Value.ToString("0"), Font, Brushes.White, x, Math.Max(2, 300 - bh - 16));
                e.Graphics.DrawString($"{row.Hour}h", Font, Brushes.LightGray, x, 306);
                x += 39;
            }
            e.Graphics.DrawString("Hourly sales — today", Font, Brushes.Gainsboro, 14, 6);
        };
        var lv = new ListView { Location = new Point(756, 86), Size = new Size(210, 360), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true };
        lv.Columns.Add("Hour", 70); lv.Columns.Add("Sales", 120);
        foreach (var h in Enumerable.Range(6, 18))
        {
            var v = today.Where(s => s.Time.Hour == h).Sum(s => s.Total);
            if (v > 0) lv.Items.Add(new ListViewItem([$"{h}h", v.ToString("C")]));
        }
        var refresh = ReportButton("Refresh", new Point(20, 462), new Size(140, 40), mint, background);
        refresh.Click += (_, _) => { f.Dispose(); ShowLiveDashboard(); };
        var close = ReportButton("Close", new Point(172, 462), new Size(130, 40), panelLight, Color.White);
        close.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([kpi1, kpi2, kpi3, kpi4, kpi5, graph, lv, refresh, close]);
        f.ShowDialog();
    }

    private void ShowCloseOfDayScreen()
    {
        if (!store.Features.CloseOfDay) return;
        using var f = ReportShell("Close of day", 720, 580);
        var today = store.Sales.Where(s => s.Time.Date == DateTime.Today).ToList();
        var head = new Label { Text = $"Close of day — {DateTime.Today:dd/MM/yyyy}", AutoSize = true, Location = new Point(14, 12), ForeColor = mint, Font = new Font("Segoe UI Semibold", 12F) };
        var lv = new ListView { Location = new Point(14, 42), Size = new Size(430, 460), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, GridLines = true };
        lv.Columns.Add("Metric", 220); lv.Columns.Add("Value", 190);
        var gross = today.Where(s => !s.IsRefund).Sum(s => s.Total);
        var refunds = today.Where(s => s.IsRefund).Sum(s => s.Total);
        void Row(string k, string v) => lv.Items.Add(new ListViewItem([k, v]));
        Row("Gross sales", gross.ToString("C"));
        Row("Refunds", refunds.ToString("C"));
        Row("Net sales", (gross - refunds).ToString("C"));
        Row("Transactions", today.Count(s => !s.IsRefund).ToString());
        Row("Avg ticket", (today.Count(s => !s.IsRefund) > 0 ? (gross / today.Count(s => !s.IsRefund)) : 0).ToString("C"));
        Row("Tips", today.Sum(s => s.Tip).ToString("C"));
        Row("Discounts given", today.Sum(s => s.Discount).ToString("C"));
        Row("Service fees", today.Sum(s => s.ServiceFee).ToString("C"));
        Row("Cash discount given", today.Sum(s => s.CashDiscountAmt).ToString("C"));
        Row("Card surcharge", today.Sum(s => s.SurchargeAmt).ToString("C"));
        Row("Rounding", today.Sum(s => s.Rounding).ToString("C"));
        var cash = today.Where(s => s.CashAmount >= 0).Sum(s => s.CashAmount);
        Row("Cash collected", cash.ToString("C"));
        var lv2 = new ListView { Location = new Point(458, 42), Size = new Size(236, 460), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, GridLines = true };
        lv2.Columns.Add("Method", 130); lv2.Columns.Add("Sum", 100);
        foreach (var g in today.GroupBy(s => s.PaymentType).OrderByDescending(g => g.Sum(s => s.Total)))
            lv2.Items.Add(new ListViewItem([g.Key.Length > 16 ? g.Key[..16] : g.Key, g.Sum(s => s.Total).ToString("C")]));
        var close = ReportButton("Close", new Point(14, 512), new Size(130, 40), panelLight, Color.White);
        close.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([head, lv, lv2, close]);
        f.ShowDialog();
    }

    private void ShowDiscountVoidReport()
    {
        if (!store.Features.DiscountVoidReport) return;
        using var f = ReportShell("Discount & void frequency", 860, 540);
        var from = DateTime.Today.AddDays(-6);
        var sales = store.Sales.Where(s => s.Time.Date >= from).ToList();
        var head = new Label { Text = $"Last 7 days ({from:dd/MM} → today)", AutoSize = true, Location = new Point(14, 12), ForeColor = mint, Font = new Font("Segoe UI Semibold", 12F) };
        var lv = new ListView { Location = new Point(14, 42), Size = new Size(410, 400), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, GridLines = true };
        lv.Columns.Add("Operator", 160); lv.Columns.Add("Disc #", 70); lv.Columns.Add("Disc amount", 100); lv.Columns.Add("Voids", 70);
        foreach (var g in sales.GroupBy(s => s.Operator))
        {
            var discSales = g.Where(s => s.Discount > 0).ToList();
            var voids = g.Count(s => s.IsRefund);
            lv.Items.Add(new ListViewItem([g.Key, discSales.Count.ToString(), discSales.Sum(s => s.Discount).ToString("C"), voids.ToString()]));
        }
        var lv2 = new ListView { Location = new Point(436, 42), Size = new Size(410, 400), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, GridLines = true };
        lv2.Columns.Add("Product", 210); lv2.Columns.Add("Item disc #", 90); lv2.Columns.Add("Item disc amt", 100);
        var itemDisc = sales.SelectMany(s => s.Items ?? []).Where(i => i.Discount > 0).GroupBy(i => i.Product).OrderByDescending(g => g.Sum(i => i.Discount)).Take(50);
        foreach (var g in itemDisc)
            lv2.Items.Add(new ListViewItem([g.Key, g.Count().ToString(), g.Sum(i => i.Discount).ToString("C")]));
        var summary = new Label { Text = $"Discounted sales: {sales.Count(s => s.Discount > 0)}/{sales.Count}   •   Voids/refunds: {sales.Count(s => s.IsRefund)}   •   Discount total: {sales.Sum(s => s.Discount):C}", AutoSize = true, Location = new Point(14, 452), ForeColor = Color.Orange };
        var close = ReportButton("Close", new Point(716, 486), new Size(130, 40), panelLight, Color.White);
        close.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([head, lv, lv2, summary, close]);
        f.ShowDialog();
    }

    private void ShowOccupancyHeatmap()
    {
        if (!store.Features.OccupancyHeatmap) return;
        using var f = ReportShell("Occupancy heatmap & dayparts", 760, 580);
        var from = DateTime.Today.AddDays(-6);
        var sales = store.Sales.Where(s => s.Time.Date >= from && !s.IsRefund).ToList();
        var head = new Label { Text = "Orders per hour — last 7 days (brighter = busier)", AutoSize = true, Location = new Point(14, 12), ForeColor = mint, Font = new Font("Segoe UI Semibold", 12F) };
        var lv = new ListView { Location = new Point(14, 42), Size = new Size(360, 470), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, GridLines = true };
        lv.Columns.Add("Hour", 80); lv.Columns.Add("Orders", 90); lv.Columns.Add("Sales", 160);
        foreach (var h in Enumerable.Range(6, 18))
        {
            var rows = sales.Where(s => s.Time.Hour == h).ToList();
            var item = new ListViewItem([$"{h}h", rows.Count.ToString(), rows.Sum(s => s.Total).ToString("C")]);
            var heat = Math.Min(255, 40 + rows.Count * 15);
            if (rows.Count > 0) item.BackColor = Color.FromArgb(heat / 3, heat, heat / 3);
            lv.Items.Add(item);
        }
        var lv2 = new ListView { Location = new Point(386, 42), Size = new Size(348, 470), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, GridLines = true };
        lv2.Columns.Add("Daypart", 150); lv2.Columns.Add("Orders", 80); lv2.Columns.Add("Sales", 110);
        (string Name, int Start, int End)[] parts = [("Morning (6-11)", 6, 11), ("Lunch (11-14)", 11, 14), ("Afternoon (14-17)", 14, 17), ("Dinner (17-21)", 17, 21), ("Night (21+)", 21, 24)];
        foreach (var p in parts)
        {
            var rows = sales.Where(s => s.Time.Hour >= p.Start && s.Time.Hour < p.End).ToList();
            var item = new ListViewItem([p.Name, rows.Count.ToString(), rows.Sum(s => s.Total).ToString("C")]);
            var heat = Math.Min(255, 40 + rows.Count * 6);
            if (rows.Count > 0) item.BackColor = Color.FromArgb(heat / 3, heat, heat / 3);
            lv2.Items.Add(item);
        }
        var close = ReportButton("Close", new Point(604, 524), new Size(130, 40), panelLight, Color.White);
        close.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([head, lv, lv2, close]);
        f.ShowDialog();
    }

    private void ShowMenuEngineering()
    {
        if (!store.Features.MenuEngineering) return;
        using var f = ReportShell("Menu engineering matrix", 860, 560);
        var sales = store.Sales.Where(s => !s.IsRefund).ToList();
        var head = new Label { Text = "Popularity vs contribution — Stars / Plowhorses / Puzzles / Dogs", AutoSize = true, Location = new Point(14, 12), ForeColor = mint, Font = new Font("Segoe UI Semibold", 12F) };
        var lv = new ListView { Location = new Point(14, 42), Size = new Size(640, 430), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, GridLines = true };
        lv.Columns.AddRange([new ColumnHeader { Text = "Product", Width = 230 }, new ColumnHeader { Text = "Qty", Width = 60 }, new ColumnHeader { Text = "Revenue", Width = 110 }, new ColumnHeader { Text = "Share %", Width = 80 }, new ColumnHeader { Text = "Class", Width = 140 }]);
        var items = sales.SelectMany(s => s.Items ?? []).GroupBy(i => i.Product).Select(g => new { Product = g.Key, Qty = g.Sum(i => i.Quantity), Rev = g.Sum(i => i.UnitPrice * i.Quantity - i.Discount) }).ToList();
        if (items.Count == 0)
        {
            lv.Items.Add(new ListViewItem(["No item sales yet", "0", "0", "0", ""]));
        }
        else
        {
            var avgQty = items.Average(i => (double)i.Qty);
            var avgRev = (double)items.Average(i => i.Rev);
            var totalRev = items.Sum(i => i.Rev);
            foreach (var i in items.OrderByDescending(i => i.Rev))
            {
                var cls = i.Qty >= avgQty && (double)i.Rev >= avgRev ? "Star ⭐" : i.Qty >= avgQty ? "Plowhorse" : (double)i.Rev >= avgRev ? "Puzzle" : "Dog";
                var share = totalRev > 0 ? (double)i.Rev / (double)totalRev * 100 : 0;
                var item = new ListViewItem([i.Product, i.Qty.ToString(), i.Rev.ToString("C"), share.ToString("0.0"), cls]);
                if (cls.StartsWith("Star")) item.ForeColor = Color.FromArgb(130, 220, 150);
                else if (cls == "Dog") item.ForeColor = Color.FromArgb(200, 120, 110);
                lv.Items.Add(item);
            }
        }
        var counts = new Label
        {
            Text = items.Count == 0 ? "" : $"⭐ Stars: {items.Count(i => i.Qty >= items.Average(x => (double)x.Qty) && (double)i.Rev >= items.Average(x => (double)x.Rev))}   •   items: {items.Count}   •   revenue: {items.Sum(i => i.Rev):C}",
            AutoSize = true,
            Location = new Point(14, 482),
            ForeColor = Color.Orange
        };
        var close = ReportButton("Close", new Point(716, 486), new Size(130, 40), panelLight, Color.White);
        close.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([head, lv, counts, close]);
        f.ShowDialog();
    }

    private void ShowAccountingExport()
    {
        if (!store.Features.AccountingExport) return;
        using var f = ReportShell("Accounting export (QuickBooks / Xero)", 560, 320);
        var title = new Label { Text = "Export sales journal as CSV", AutoSize = true, Location = new Point(20, 16), ForeColor = mint, Font = new Font("Segoe UI Semibold", 13F) };
        var hint = new Label { Text = "Columns: Date, Time, Ref, Account, Name, Memo, Payment, Amount, Tip\nCompatible with QuickBooks / Xero import (Date, Account, Memo, Amount).", AutoSize = true, Location = new Point(20, 48), ForeColor = Color.FromArgb(143, 154, 171) };
        var todayRb = new RadioButton { Text = "Today", Location = new Point(20, 106), AutoSize = true, ForeColor = Color.White, Checked = true };
        var weekRb = new RadioButton { Text = "Last 7 days", Location = new Point(140, 106), AutoSize = true, ForeColor = Color.White };
        var rowsLabel = new Label { Text = "", AutoSize = true, Location = new Point(20, 142), ForeColor = Color.Orange };
        var export = ReportButton("Export CSV…", new Point(20, 184), new Size(170, 42), mint, background);
        export.Click += (_, _) =>
        {
            var from = weekRb.Checked ? DateTime.Today.AddDays(-6) : DateTime.Today;
            var rows = store.Sales.Where(s => s.Time.Date >= from).OrderBy(s => s.Time).ToList();
            using var dlg = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", FileName = $"accounting-{DateTime.Now:yyyy-MM-dd}.csv" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            var sb = new StringBuilder("Date,Time,Ref,Account,Name,Memo,Payment,Amount,Tip\n");
            foreach (var s in rows)
                sb.AppendLine($"{s.Time:yyyy-MM-dd},{s.Time:HH:mm},{s.PaymentNumber},{(s.IsRefund ? "Sales Refund" : "Sales Income")},{s.CustomerName},{s.Table},{s.PaymentType},{(s.IsRefund ? -s.Total : s.Total).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)},{s.Tip.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");
            File.WriteAllText(dlg.FileName, sb.ToString());
            MessageBox.Show($"{rows.Count} rows exported:\n{dlg.FileName}", "Accounting export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        var close = ReportButton("Close", new Point(410, 240), new Size(130, 40), panelLight, Color.White);
        close.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([title, hint, todayRb, weekRb, rowsLabel, export, close]);
        f.ShowDialog();
    }

    private byte[]? BuildDatabaseZip()
    {
        var jsons = Directory.GetFiles(store.DataFolder, "*.json");
        if (jsons.Length == 0) return null;
        using var ms = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
            foreach (var file in jsons)
                System.IO.Compression.ZipFileExtensions.CreateEntryFromFile(archive, file, Path.GetFileName(file), System.IO.Compression.CompressionLevel.Optimal);
        return ms.ToArray();
    }

    private bool RunBackup(bool silent)
    {
        try
        {
            var dest = store.Features.BackupPath.Trim();
            if (dest.Length == 0)
            {
                if (!silent) MessageBox.Show("Set a backup folder first.", "Cloud backup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            Directory.CreateDirectory(dest);
            var bytes = BuildDatabaseZip();
            if (bytes == null)
            {
                if (!silent) MessageBox.Show("No database files found to back up.", "Cloud backup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            var zip = Path.Combine(dest, $"vectron-backup-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
            if (File.Exists(zip)) File.Delete(zip);
            File.WriteAllBytes(zip, bytes);
            if (!silent) MessageBox.Show($"Backup created:\n{zip}\n\n{bytes.Length / 1024} KB zipped.", "Cloud backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }
        catch (Exception ex)
        {
            if (!silent) MessageBox.Show($"Backup failed:\n{ex.Message}", "Cloud backup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void StartBackupScheduler()
    {
        void Check()
        {
            try
            {
                if (!store.Features.CloudBackup || !store.Features.BackupDaily) return;
                if (store.Features.BackupPath.Trim().Length == 0) return;
                if (lastDailyBackup.Date == DateTime.Today) return;
                if (RunBackup(true)) lastDailyBackup = DateTime.Today;
            }
            catch { }
        }
        Check();
        var t = new System.Windows.Forms.Timer { Interval = 30 * 60 * 1000 };
        t.Tick += (_, _) => Check();
        t.Start();
    }

    private bool syncRunning;
    private DateTime lastCloudSync = DateTime.MinValue;

    private void StartCloudSyncScheduler()
    {
        var t = new System.Windows.Forms.Timer { Interval = 60 * 1000 };
        t.Tick += (_, _) =>
        {
            try
            {
                if (!store.Features.CloudSync) return;
                var mins = Math.Clamp(store.Features.CloudSyncMinutes, 1, 1440);
                if ((DateTime.Now - lastCloudSync).TotalMinutes < mins) return;
                RunCloudSync(false);
            }
            catch { }
        };
        t.Start();
    }

    private void RunCloudSync(bool showResult)
    {
        if (syncRunning || IsDisposed || !IsHandleCreated) return;
        var url = store.Features.CloudSyncUrl.Trim().TrimEnd('/');
        if (url.Length == 0)
        {
            if (showResult)
                MessageBox.Show("Set the cloud sync server URL first\n(Settings > Network > Cloud sync settings).", "Cloud sync", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (string.IsNullOrEmpty(store.Features.CloudSyncDevice))
        {
            store.Features.CloudSyncDevice = Environment.MachineName;
            store.SaveFeatures(store.Features);
        }
        var key = string.IsNullOrEmpty(store.Features.CloudSyncKey) ? store.Features.ApiKey : store.Features.CloudSyncKey;
        var device = store.Features.CloudSyncDevice;
        var since = store.Features.CloudSyncLast;
        var doPull = store.Features.CloudSyncPull;
        var queued = store.SyncQueue.ToList();
        syncRunning = true;
        lastCloudSync = DateTime.Now;
        _ = Task.Run(async () =>
        {
            string status = "";
            int pushed = 0, added = 0;
            bool pushedOk = true;
            string serverTime = "";
            var pulled = new List<Sale>();
            try
            {
                using var hc = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                if (queued.Count > 0)
                {
                    var payload = JsonSerializer.Serialize(new { key, device, sales = queued });
                    var resp = await hc.PostAsync($"{url}/sync/sales", new System.Net.Http.StringContent(payload, Encoding.UTF8, "application/json"));
                    var text = await resp.Content.ReadAsStringAsync();
                    using var pdoc = JsonDocument.Parse(text);
                    var proot = pdoc.RootElement;
                    if (proot.TryGetProperty("ok", out var okp) && okp.ValueKind == JsonValueKind.True)
                    {
                        pushed = queued.Count;
                        status = $"Pushed {pushed} sale(s).";
                    }
                    else
                    {
                        pushedOk = false;
                        status = "Push failed: " + (proot.TryGetProperty("error", out var ep) ? ep.GetString() : $"HTTP {(int)resp.StatusCode}");
                    }
                }
                else
                {
                    status = "Nothing to push.";
                }
                if (pushedOk && doPull)
                {
                    var g = await hc.GetAsync($"{url}/sync/sales?since={Uri.EscapeDataString(since)}&device={Uri.EscapeDataString(device)}&key={Uri.EscapeDataString(key)}");
                    var gtext = await g.Content.ReadAsStringAsync();
                    using var gdoc = JsonDocument.Parse(gtext);
                    var groot = gdoc.RootElement;
                    if (groot.TryGetProperty("ok", out var gok) && gok.ValueKind == JsonValueKind.True)
                    {
                        if (groot.TryGetProperty("server_time", out var st)) serverTime = st.GetString() ?? "";
                        if (groot.TryGetProperty("sales", out var arr) && arr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in arr.EnumerateArray())
                            {
                                try
                                {
                                    var s = JsonSerializer.Deserialize<Sale>(el.GetRawText());
                                    if (s == null || s.Device == device) continue;
                                    pulled.Add(s);
                                }
                                catch { }
                            }
                        }
                        status += $" Fetched {pulled.Count} remote sale(s).";
                    }
                    else
                    {
                        status += " Pull failed: " + (groot.TryGetProperty("error", out var gep) ? gep.GetString() : $"HTTP {(int)g.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                status = "Sync error: " + ex.Message;
            }
            try
            {
                BeginInvoke((Action)(() =>
                {
                    try
                    {
                        foreach (var s in pulled)
                        {
                            if (store.HasSale(s)) continue;
                            store.Sales.Add(s);
                            added++;
                        }
                        if (added > 0)
                        {
                            store.Sales.Sort((a, b) => a.Time.CompareTo(b.Time));
                            store.SaveSales();
                        }
                        if (pushedOk && queued.Count > 0)
                        {
                            var pushedSet = new HashSet<Sale>(queued);
                            store.SyncQueue.RemoveAll(pushedSet.Contains);
                            store.SaveSyncQueue();
                        }
                        if (!string.IsNullOrEmpty(serverTime) && serverTime != store.Features.CloudSyncLast)
                        {
                            store.Features.CloudSyncLast = serverTime;
                            store.SaveFeatures(store.Features);
                        }
                        if (added > 0) status += $" Added {added} new sale(s) locally.";
                    }
                    finally { syncRunning = false; }
                    if (showResult)
                        MessageBox.Show(status, "Cloud sync", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            }
            catch { syncRunning = false; }
        });
    }

    private bool uploadRunning;
    private bool restoreRunning;

    private void UploadBackupToHome(bool showResult)
    {
        if (uploadRunning || IsDisposed || !IsHandleCreated) return;
        var url = store.Features.CloudSyncUrl.Trim().TrimEnd('/');
        if (url.Length == 0)
        {
            if (showResult)
                MessageBox.Show("Set the cloud sync server URL first\n(Settings > Network > Cloud sync settings).", "Database backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (string.IsNullOrEmpty(store.Features.CloudSyncDevice))
        {
            store.Features.CloudSyncDevice = Environment.MachineName;
            store.SaveFeatures(store.Features);
        }
        var key = string.IsNullOrEmpty(store.Features.CloudSyncKey) ? store.Features.ApiKey : store.Features.CloudSyncKey;
        var device = store.Features.CloudSyncDevice;
        uploadRunning = true;
        _ = Task.Run(async () =>
        {
            string status;
            var ok = false;
            try
            {
                var zip = BuildDatabaseZip();
                if (zip == null)
                {
                    status = "No database files found to back up.";
                }
                else
                {
                    using var hc = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(180) };
                    var payload = JsonSerializer.Serialize(new { key, store = device, data = Convert.ToBase64String(zip) });
                    var resp = await hc.PostAsync($"{url}/sync/backup", new System.Net.Http.StringContent(payload, Encoding.UTF8, "application/json"));
                    var text = await resp.Content.ReadAsStringAsync();
                    using var pdoc = JsonDocument.Parse(text);
                    var proot = pdoc.RootElement;
                    if (proot.TryGetProperty("ok", out var okp) && okp.ValueKind == JsonValueKind.True)
                    {
                        ok = true;
                        status = $"Database backup uploaded to home ({zip.Length / 1024} KB).";
                    }
                    else
                    {
                        status = "Backup upload failed: " + (proot.TryGetProperty("error", out var ep) ? ep.GetString() : $"HTTP {(int)resp.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                status = "Backup upload failed: " + ex.Message;
            }
            try
            {
                BeginInvoke((Action)(() =>
                {
                    try
                    {
                        if (ok)
                        {
                            store.Features.CloudSyncLastBackup = DateTime.Now.ToString("o");
                            store.SaveFeatures(store.Features);
                        }
                    }
                    finally { uploadRunning = false; }
                    if (showResult)
                        MessageBox.Show(status, "Database backup", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                }));
            }
            catch { uploadRunning = false; }
        });
    }

    private void RunManualPull()
    {
        if (syncRunning || uploadRunning || IsDisposed || !IsHandleCreated)
        {
            MessageBox.Show("Another sync operation is already running.", "Cloud sync", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var url = store.Features.CloudSyncUrl.Trim().TrimEnd('/');
        if (url.Length == 0)
        {
            MessageBox.Show("Set the cloud sync server URL first\n(Settings > Network > Cloud sync settings).", "Cloud sync", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (string.IsNullOrEmpty(store.Features.CloudSyncDevice))
        {
            store.Features.CloudSyncDevice = Environment.MachineName;
            store.SaveFeatures(store.Features);
        }
        var key = string.IsNullOrEmpty(store.Features.CloudSyncKey) ? store.Features.ApiKey : store.Features.CloudSyncKey;
        var device = store.Features.CloudSyncDevice;
        syncRunning = true;
        _ = Task.Run(async () =>
        {
            List<Sale>? pulled = null;
            string error = "";
            try
            {
                using var hc = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(120) };
                var g = await hc.GetAsync($"{url}/sync/sales?since=&device={Uri.EscapeDataString(device)}&key={Uri.EscapeDataString(key)}&scope=mine");
                if (!g.IsSuccessStatusCode) throw new Exception($"server returned HTTP {(int)g.StatusCode}");
                var gtext = await g.Content.ReadAsStringAsync();
                if (gtext.Length == 0) throw new Exception("empty response from server");
                pulled = [];
                using var gdoc = JsonDocument.Parse(gtext);
                var groot = gdoc.RootElement;
                if (groot.TryGetProperty("error", out var gep))
                {
                    error = gep.GetString() ?? "unknown error";
                    pulled = null;
                }
                else if (groot.TryGetProperty("sales", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in arr.EnumerateArray())
                    {
                        try
                        {
                            var s = JsonSerializer.Deserialize<Sale>(el.GetRawText());
                            if (s == null || s.Device != device) continue;
                            pulled.Add(s);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                pulled = null;
            }
            try
            {
                BeginInvoke((Action)(() =>
                {
                    var added = 0;
                    try
                    {
                        if (pulled != null)
                        {
                            foreach (var s in pulled)
                            {
                                if (store.HasSale(s)) continue;
                                store.Sales.Add(s);
                                added++;
                            }
                            if (added > 0)
                            {
                                store.Sales.Sort((a, b) => a.Time.CompareTo(b.Time));
                                store.SaveSales();
                            }
                        }
                    }
                    finally { syncRunning = false; }
                    if (pulled != null)
                        MessageBox.Show($"Pulled {pulled.Count} sale(s) from home.\n{added} new sale(s) added to this POS.", "Pull sales history", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else
                        MessageBox.Show("Pull failed: " + error, "Pull sales history", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }));
            }
            catch { syncRunning = false; }
        });
    }

    private void RestoreFromHome()
    {
        if (syncRunning || uploadRunning || restoreRunning)
        {
            MessageBox.Show("Another sync operation is already running.", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var url = store.Features.CloudSyncUrl.Trim().TrimEnd('/');
        if (url.Length == 0)
        {
            MessageBox.Show("Set the cloud sync server URL first.", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (string.IsNullOrEmpty(store.Features.CloudSyncDevice))
        {
            store.Features.CloudSyncDevice = Environment.MachineName;
            store.SaveFeatures(store.Features);
        }
        var key = string.IsNullOrEmpty(store.Features.CloudSyncKey) ? store.Features.ApiKey : store.Features.CloudSyncKey;
        var device = store.Features.CloudSyncDevice;
        var msg = $"Restore this POS from the backup stored at home?\n\nStore: {device}\n\nThis REPLACES this computer's entire database\n(products, settings, sales, shifts) with the copy uploaded from home.\n\nRun it on the MAIN PC, with the order PCs closed.\n\nContinue?";
        if (MessageBox.Show(msg, "Restore from home", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        restoreRunning = true;
        _ = Task.Run(async () =>
        {
            string? error = null;
            try
            {
                using var hc = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(180) };
                var g = await hc.GetAsync($"{url}/sync/backup?store={Uri.EscapeDataString(device)}&key={Uri.EscapeDataString(key)}");
                if (!g.IsSuccessStatusCode) throw new Exception($"server returned HTTP {(int)g.StatusCode}");
                var gtext = await g.Content.ReadAsStringAsync();
                if (gtext.Length == 0) throw new Exception("empty response from server");
                using var gdoc = JsonDocument.Parse(gtext);
                var groot = gdoc.RootElement;
                if (!groot.TryGetProperty("ok", out var gok) || gok.ValueKind != JsonValueKind.True)
                    throw new Exception(groot.TryGetProperty("error", out var gep) ? gep.GetString() : "no backup available");
                var dataB64 = groot.GetProperty("data").GetString() ?? "";
                var bytes = Convert.FromBase64String(dataB64);
                var temp = Path.Combine(Path.GetTempPath(), $"vectron-restore-{Guid.NewGuid():N}.zip");
                File.WriteAllBytes(temp, bytes);
                System.IO.Compression.ZipFile.ExtractToDirectory(temp, store.DataFolder, true);
                try { File.Delete(temp); } catch { }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            try
            {
                BeginInvoke((Action)(() =>
                {
                    restoreRunning = false;
                    if (error != null)
                    {
                        MessageBox.Show("Restore failed:\n" + error, "Restore from home", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    MessageBox.Show("Backup restored.\nThe application will now restart to load the restored data.", "Restore from home", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Application.Restart();
                }));
            }
            catch { restoreRunning = false; }
        });
    }

    private void ShowBackupDialog()
    {
        if (!store.Features.CloudBackup) return;
        using var f = ReportShell("Cloud backup & restore", 620, 400);
        var head = new Label { Text = "Backup folder (network share, NAS, or sync folder):", AutoSize = true, Location = new Point(20, 14), ForeColor = mint, Font = new Font("Segoe UI Semibold", 11F) };
        var txt = new TextBox { Text = store.Features.BackupPath, Location = new Point(20, 44), Size = new Size(470, 30), BackColor = panelLight, ForeColor = Color.White };
        var browse = ReportButton("Browse…", new Point(500, 42), new Size(95, 34), panelLight, Color.White);
        browse.Click += (_, _) =>
        {
            using var fb = new FolderBrowserDialog { Description = "Choose the backup destination folder" };
            if (fb.ShowDialog() == DialogResult.OK) txt.Text = fb.SelectedPath;
        };
        var daily = new CheckBox { Text = "Run a backup automatically once a day", Checked = store.Features.BackupDaily, AutoSize = true, Location = new Point(20, 88), ForeColor = Color.White };
        var hint = new Label
        {
            Text = "Every backup zips all database JSON files (sales, products, operators,\nshifts, features…) into a timestamped .zip you can store off-site.",
            AutoSize = true, Location = new Point(20, 122), ForeColor = Color.FromArgb(143, 154, 171), Font = new Font("Segoe UI", 9F)
        };
        var save = ReportButton("Save settings", new Point(20, 178), new Size(150, 40), mint, background);
        save.Click += (_, _) =>
        {
            store.Features.BackupPath = txt.Text.Trim();
            store.Features.BackupDaily = daily.Checked;
            store.SaveFeatures(store.Features);
            lastDailyBackup = DateTime.MinValue;
            MessageBox.Show("Backup settings saved.", "Cloud backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        var now = ReportButton("Backup now", new Point(184, 178), new Size(150, 40), mint, background);
        now.Click += (_, _) =>
        {
            store.Features.BackupPath = txt.Text.Trim();
            store.Features.BackupDaily = daily.Checked;
            store.SaveFeatures(store.Features);
            RunBackup(false);
        };
        var restore = ReportButton("Restore from backup…", new Point(348, 178), new Size(200, 40), Color.FromArgb(170, 120, 50), Color.White);
        restore.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog { Filter = "Backup zip (*.zip)|*.zip|All files (*.*)|*.*", Title = "Pick a vectron-backup zip" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            if (MessageBox.Show("Restoring will OVERWRITE the current database with the backup contents.\n\nContinue?", "Restore backup", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(dlg.FileName, store.DataFolder, true);
                MessageBox.Show("Backup restored.\nThe application will now restart to load the restored data.", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Restart();
            }
            catch (Exception ex) { MessageBox.Show($"Restore failed:\n{ex.Message}", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        };
        var lastRun = new Label { Text = "", AutoSize = true, Location = new Point(20, 236), ForeColor = Color.FromArgb(143, 154, 171), Font = new Font("Segoe UI", 9F) };
        try
        {
            var existing = Directory.Exists(store.Features.BackupPath)
                ? Directory.GetFiles(store.Features.BackupPath, "vectron-backup-*.zip").Select(File.GetLastWriteTime).OrderDescending().FirstOrDefault()
                : default;
            lastRun.Text = existing == default ? "No previous backup found in this folder." : $"Last backup in this folder: {existing:dd MMM yyyy HH:mm}";
        }
        catch { lastRun.Text = ""; }
        var close = ReportButton("Close", new Point(470, 320), new Size(125, 40), panelLight, Color.White);
        close.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([head, txt, browse, daily, hint, save, now, restore, lastRun, close]);
        f.ShowDialog();
    }

    private void ShowApiConfigDialog()
    {
        if (!store.Features.ApiEnabled) return;
        using var f = ReportShell("REST API & webhooks", 640, 470);
        var head = new Label { Text = "HTTP endpoints (JSON):", AutoSize = true, Location = new Point(20, 14), ForeColor = mint, Font = new Font("Segoe UI Semibold", 11F) };
        var ep = new Label
        {
            Text = $"GET  http://{TcpPrintServer.LocalIp}:8080/api/sales?limit=50&key=KEY\nGET  http://{TcpPrintServer.LocalIp}:8080/api/products?key=KEY\nGET  http://{TcpPrintServer.LocalIp}:8080/api/orders?key=KEY",
            AutoSize = true, Location = new Point(20, 40), ForeColor = Color.FromArgb(170, 180, 195), Font = new Font("Consolas", 9F)
        };
        var keyLbl = new Label { Text = "API key (required on every request):", AutoSize = true, Location = new Point(20, 106), ForeColor = Color.White };
        var keyTxt = new TextBox { Text = store.Features.ApiKey, Location = new Point(20, 132), Size = new Size(420, 30), BackColor = panelLight, ForeColor = Color.White };
        var gen = ReportButton("Generate", new Point(450, 130), new Size(110, 34), panelLight, Color.White);
        gen.Click += (_, _) => keyTxt.Text = Guid.NewGuid().ToString("N");
        var hookLbl = new Label { Text = "Webhook URL (POST on every completed sale):", AutoSize = true, Location = new Point(20, 178), ForeColor = Color.White };
        var hookTxt = new TextBox { Text = store.Features.WebhookUrl, Location = new Point(20, 204), Size = new Size(420, 30), BackColor = panelLight, ForeColor = Color.White };
        var test = ReportButton("Send test", new Point(450, 202), new Size(110, 34), panelLight, Color.White);
        test.Click += (_, _) =>
        {
            var url = hookTxt.Text.Trim();
            if (url.Length == 0) { MessageBox.Show("Enter a webhook URL first.", "Webhooks", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                using var hc = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var payload = "{\"event\":\"test\",\"message\":\"Vectron POS webhook test\"}";
                var resp = hc.PostAsync(url, new System.Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
                MessageBox.Show(resp.IsSuccessStatusCode ? $"Webhook delivered (HTTP {(int)resp.StatusCode})." : $"Server replied HTTP {(int)resp.StatusCode}.", "Webhooks", MessageBoxButtons.OK, resp.IsSuccessStatusCode ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex) { MessageBox.Show($"Webhook failed:\n{ex.Message}", "Webhooks", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        };
        var hookHint = new Label
        {
            Text = "Every sale completed at the till POSTs a JSON payload like\n{ \"event\": \"sale.completed\", \"total\": 12.5, \"payment_type\": \"Cash\", … }.",
            AutoSize = true, Location = new Point(20, 244), ForeColor = Color.FromArgb(143, 154, 171), Font = new Font("Segoe UI", 9F)
        };
        var save = ReportButton("Save", new Point(20, 300), new Size(130, 40), mint, background);
        save.Click += (_, _) =>
        {
            store.Features.ApiKey = keyTxt.Text.Trim();
            store.Features.WebhookUrl = hookTxt.Text.Trim();
            store.SaveFeatures(store.Features);
            MessageBox.Show("API settings saved.", "REST API", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        var close = ReportButton("Close", new Point(480, 396), new Size(125, 40), panelLight, Color.White);
        close.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([head, ep, keyLbl, keyTxt, gen, hookLbl, hookTxt, test, hookHint, save, close]);
        f.ShowDialog();
    }

    private void ShowCloudSyncDialog()
    {
        if (!store.Features.CloudSync) return;
        using var f = ReportShell("Cloud sync", 640, 530);
        var head = new Label { Text = "Sync server URL (base address, e.g. http://hq.example.com:8080):", AutoSize = true, Location = new Point(20, 14), ForeColor = mint, Font = new Font("Segoe UI Semibold", 11F) };
        var urlTxt = new TextBox { Text = store.Features.CloudSyncUrl, Location = new Point(20, 44), Size = new Size(580, 30), BackColor = panelLight, ForeColor = Color.White };
        var keyLbl = new Label { Text = "Shared key (leave empty to reuse the API key):", AutoSize = true, Location = new Point(20, 86), ForeColor = mint, Font = new Font("Segoe UI Semibold", 11F) };
        var keyTxt = new TextBox { Text = store.Features.CloudSyncKey, Location = new Point(20, 116), Size = new Size(300, 30), BackColor = panelLight, ForeColor = Color.White };
        var minLbl = new Label { Text = "Sync every (minutes):", AutoSize = true, Location = new Point(350, 86), ForeColor = mint, Font = new Font("Segoe UI Semibold", 11F) };
        var minNum = new NumericUpDown { Minimum = 1, Maximum = 1440, Value = Math.Clamp(store.Features.CloudSyncMinutes, 1, 1440), Location = new Point(350, 116), Size = new Size(110, 30), BackColor = panelLight, ForeColor = Color.White };
        var storeLbl = new Label { Text = "Store ID (this PC, e.g. RESTO-1 — used for backups and restore):", AutoSize = true, Location = new Point(20, 158), ForeColor = mint, Font = new Font("Segoe UI Semibold", 11F) };
        var storeTxt = new TextBox { Text = store.Features.CloudSyncDevice, Location = new Point(20, 184), Size = new Size(300, 30), BackColor = panelLight, ForeColor = Color.White };
        var pullChk = new CheckBox { Text = "Pull other stores' sales automatically (off = push only)", Checked = store.Features.CloudSyncPull, AutoSize = true, Location = new Point(20, 224), ForeColor = Color.White };
        var status = new Label
        {
            Text = $"Store ID: {(store.Features.CloudSyncDevice.Length == 0 ? Environment.MachineName : store.Features.CloudSyncDevice)}\n" +
                   $"Sales waiting to push: {store.SyncQueue.Count}\n" +
                   $"Last sync: {(store.Features.CloudSyncLast.Length == 0 ? "never" : store.Features.CloudSyncLast)}\n" +
                   $"Last backup upload: {(store.Features.CloudSyncLastBackup.Length == 0 ? "never" : store.Features.CloudSyncLastBackup)}",
            Location = new Point(20, 256), Size = new Size(580, 90), ForeColor = Color.FromArgb(143, 154, 171), Font = new Font("Segoe UI", 9.5F)
        };
        var hint = new Label
        {
            Text = "This till pushes every completed sale to the server (push only) and uploads its\ndaily database backup on End Day. Pull sales history only restores THIS store.",
            Location = new Point(20, 352), Size = new Size(580, 44), ForeColor = Color.FromArgb(143, 154, 171), Font = new Font("Segoe UI", 9F)
        };
        void SaveSettings()
        {
            store.Features.CloudSyncUrl = urlTxt.Text.Trim().TrimEnd('/');
            store.Features.CloudSyncKey = keyTxt.Text.Trim();
            store.Features.CloudSyncMinutes = (int)minNum.Value;
            store.Features.CloudSyncDevice = storeTxt.Text.Trim();
            if (string.IsNullOrEmpty(store.Features.CloudSyncDevice))
                store.Features.CloudSyncDevice = Environment.MachineName;
            store.Features.CloudSyncPull = pullChk.Checked;
            store.SaveFeatures(store.Features);
        }
        var save = ReportButton("Save settings", new Point(20, 406), new Size(150, 40), mint, background);
        save.Click += (_, _) =>
        {
            SaveSettings();
            MessageBox.Show("Cloud sync settings saved.", "Cloud sync", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        var now = ReportButton("Sync now", new Point(184, 406), new Size(140, 40), mint, background);
        now.Click += (_, _) =>
        {
            SaveSettings();
            RunCloudSync(true);
        };
        var upload = ReportButton("Upload backup now", new Point(338, 406), new Size(170, 40), mint, background);
        upload.Click += (_, _) =>
        {
            SaveSettings();
            UploadBackupToHome(true);
        };
        var close = ReportButton("Close", new Point(520, 406), new Size(100, 40), panelLight, Color.White);
        close.Click += (_, _) => f.DialogResult = DialogResult.OK;
        var pull = ReportButton("Pull sales history", new Point(20, 456), new Size(180, 40), Color.FromArgb(55, 120, 130), Color.White);
        pull.Click += (_, _) =>
        {
            SaveSettings();
            RunManualPull();
        };
        var restore = ReportButton("Restore from home\u2026", new Point(214, 456), new Size(210, 40), Color.FromArgb(170, 120, 50), Color.White);
        restore.Click += (_, _) =>
        {
            SaveSettings();
            RestoreFromHome();
        };
        f.Controls.AddRange([head, urlTxt, keyLbl, keyTxt, minLbl, minNum, storeLbl, storeTxt, pullChk, status, hint, save, now, upload, close, pull, restore]);
        f.ShowDialog();
    }

    private void ShowBranchesDialog()
    {
        if (!store.Features.MultiBranch) return;
        using var f = ReportShell("Branches", 480, 540);
        var head = new Label { Text = "Every sale is stamped with the current branch.", AutoSize = true, Location = new Point(20, 14), ForeColor = mint, Font = new Font("Segoe UI Semibold", 11F) };
        var lv = new ListBox { Location = new Point(20, 46), Size = new Size(300, 300), BackColor = panelLight, ForeColor = Color.White };
        foreach (var b in store.Features.Branches) lv.Items.Add(b);
        var txt = new TextBox { Location = new Point(20, 356), Size = new Size(190, 30), BackColor = panelLight, ForeColor = Color.White };
        var add = ReportButton("Add", new Point(220, 354), new Size(95, 34), mint, background);
        add.Click += (_, _) =>
        {
            var name = txt.Text.Trim();
            if (name.Length == 0) return;
            if (!lv.Items.Cast<string>().Contains(name)) lv.Items.Add(name);
            txt.Text = "";
        };
        var remove = ReportButton("Remove selected", new Point(20, 396), new Size(150, 34), Color.FromArgb(170, 50, 50), Color.White);
        remove.Click += (_, _) => { if (lv.SelectedItem != null) lv.Items.Remove(lv.SelectedItem); };
        var curLbl = new Label { Text = "Current branch for this register:", AutoSize = true, Location = new Point(20, 442), ForeColor = Color.White };
        var combo = new ComboBox { Location = new Point(20, 468), Size = new Size(300, 30), DropDownStyle = ComboBoxStyle.DropDown, BackColor = panelLight, ForeColor = Color.White };
        foreach (var b in lv.Items.Cast<string>()) combo.Items.Add(b);
        combo.Items.Add("(none)");
        combo.Text = string.IsNullOrWhiteSpace(store.Features.CurrentBranch) ? "(none)" : store.Features.CurrentBranch;
        var save = ReportButton("Save", new Point(334, 464), new Size(110, 38), mint, background);
        save.Click += (_, _) =>
        {
            store.Features.Branches = lv.Items.Cast<string>().ToList();
            store.Features.CurrentBranch = combo.Text.Trim() == "(none)" ? "" : combo.Text.Trim();
            if (store.Features.CurrentBranch.Length > 0 && !store.Features.Branches.Contains(store.Features.CurrentBranch))
                store.Features.Branches.Add(store.Features.CurrentBranch);
            store.SaveFeatures(store.Features);
            MessageBox.Show($"Branches saved.\nCurrent branch: {(store.Features.CurrentBranch.Length == 0 ? "(none)" : store.Features.CurrentBranch)}", "Branches", MessageBoxButtons.OK, MessageBoxIcon.Information);
            f.DialogResult = DialogResult.OK;
        };
        var close = ReportButton("Close", new Point(334, 20), new Size(110, 34), panelLight, Color.White);
        close.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
        f.Controls.AddRange([head, lv, txt, add, remove, curLbl, combo, save, close]);
        if (f.ShowDialog() == DialogResult.OK) UpdateBranchBadge();
    }

    private void UpdateBranchBadge()
    {
        if (IsDisposed) return;
        var b = store.Features.MultiBranch ? store.Features.CurrentBranch.Trim() : "";
        operatorButton.Text = "Operator: " + currentOperator.Name + (b.Length > 0 ? $"  •  {b}" : "");
        if (greetingLabel != null && !greetingLabel.IsDisposed) greetingLabel.Text = GreetingText();
        if (orderLabel != null && !orderLabel.IsDisposed) orderLabel.Text = OrderContextText();
    }

    private string BranchSuffix()
    {
        var b = store.Features.MultiBranch ? store.Features.CurrentBranch.Trim() : "";
        return b.Length > 0 ? $"  •  {b}" : "";
    }

    private string EffectiveGuestUrl()
    {
        var custom = store.Features.GuestOrderUrl?.Trim() ?? "";
        return custom.Length > 0 ? custom.TrimEnd('/') : $"http://{TcpPrintServer.LocalIp}:8080";
    }

    private void ShowGuestUrlDialog()
    {
        if (!(store.Features.QROrdering || store.Features.KioskMode)) return;
        using var f = ReportShell("Guest ordering URL", 580, 470);
        var head = new Label { Text = "Guests open this URL in their phone browser to browse the menu & order:", AutoSize = true, Location = new Point(20, 14), ForeColor = mint, Font = new Font("Segoe UI Semibold", 11F) };
        var defaultLbl = new Label { Text = $"Built-in (local network): http://{TcpPrintServer.LocalIp}:8080", AutoSize = true, Location = new Point(20, 44), ForeColor = Color.FromArgb(170, 180, 195), Font = new Font("Segoe UI", 9F) };
        var txtLbl = new Label { Text = "Public URL override (tunnel / domain — leave empty for local):", AutoSize = true, Location = new Point(20, 74), ForeColor = Color.White };
        var txt = new TextBox { Text = store.Features.GuestOrderUrl, Location = new Point(20, 100), Size = new Size(400, 30), BackColor = panelLight, ForeColor = Color.White };
        var pic = new PictureBox { Location = new Point(20, 146), Size = new Size(220, 220), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
        var urlLbl = new Label { Location = new Point(20, 372), Size = new Size(400, 34), ForeColor = Color.FromArgb(143, 154, 171), Font = new Font("Segoe UI", 9F), AutoEllipsis = true };
        void RefreshQr()
        {
            var url = EffectiveGuestUrl();
            urlLbl.Text = url;
            try { pic.Image?.Dispose(); pic.Image = QrCode.ToBitmap(url, 5); } catch { pic.Image = null; }
        }
        RefreshQr();
        var copy = ReportButton("Copy URL", new Point(260, 146), new Size(130, 38), panelLight, Color.White);
        copy.Click += (_, _) => { try { Clipboard.SetText(EffectiveGuestUrl()); MessageBox.Show("URL copied to clipboard.", "Guest ordering", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch { } };
        var save = ReportButton("Save", new Point(260, 196), new Size(130, 38), mint, background);
        save.Click += (_, _) =>
        {
            store.Features.GuestOrderUrl = txt.Text.Trim();
            store.SaveFeatures(store.Features);
            UpdateQrBanner();
            RefreshQr();
            MessageBox.Show("Guest ordering URL saved.", "Guest ordering", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        var hint = new Label
        {
            Text = "The banner on the order screen and the table QR labels\nuse this URL when it is set.",
            AutoSize = true, Location = new Point(260, 248), ForeColor = Color.FromArgb(143, 154, 171), Font = new Font("Segoe UI", 9F)
        };
        var close = ReportButton("Close", new Point(420, 406), new Size(125, 40), panelLight, Color.White);
        close.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([head, defaultLbl, txtLbl, txt, pic, urlLbl, copy, save, hint, close]);
        f.ShowDialog();
        pic.Image?.Dispose();
    }

    private void ShowPrepAnalytics()
    {
        if (!store.Features.KdsLateAlarm) return;
        using var f = ReportShell("KDS prep-time analytics", 640, 560);
        var target = store.Features.KdsPrepMinutes;
        var recs = store.PrepTimes;
        string stats;
        if (recs.Count == 0)
            stats = "No completed orders yet — press ✓ READY on the KDS to record prep times.";
        else
        {
            var avg = recs.Average(r => r.Minutes);
            var onTime = recs.Count(r => r.Minutes <= target) * 100.0 / recs.Count;
            stats = $"Orders: {recs.Count}   •   Avg prep: {avg:0.0} min   •   Fastest: {recs.Min(r => r.Minutes)} min   •   Slowest: {recs.Max(r => r.Minutes)} min\nOn time (≤ {target} min target): {onTime:0.0}%   •   Last: {recs.Max(r => r.Time):dd MMM HH:mm}";
        }
        var head = new Label { Text = stats, Location = new Point(14, 12), Size = new Size(430, 64), ForeColor = Color.Orange, Font = new Font("Segoe UI Semibold", 11F) };
        var targetLbl = new Label { Text = "Target prep (min):", AutoSize = true, Location = new Point(456, 14), ForeColor = Color.White };
        var targetNum = new NumericUpDown { Minimum = 1, Maximum = 180, Value = Math.Clamp(store.Features.KdsPrepMinutes, 1, 180), Location = new Point(456, 38), Size = new Size(70, 30), BackColor = panelLight, ForeColor = Color.White };
        var saveTarget = ReportButton("Save", new Point(534, 36), new Size(84, 34), mint, background);
        saveTarget.Click += (_, _) =>
        {
            store.Features.KdsPrepMinutes = (int)targetNum.Value;
            store.SaveFeatures(store.Features);
            MessageBox.Show($"Target prep time: {store.Features.KdsPrepMinutes} minutes.", "KDS settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        var lv = new ListView { Location = new Point(14, 88), Size = new Size(610, 400), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, GridLines = true };
        lv.Columns.AddRange([new ColumnHeader { Text = "Completed", Width = 190 }, new ColumnHeader { Text = "Prep minutes", Width = 110 }, new ColumnHeader { Text = "Status", Width = 110 }, new ColumnHeader { Text = "Branch", Width = 140 }]);
        foreach (var r in recs.AsEnumerable().Reverse().Take(100))
        {
            var late = r.Minutes > target;
            var item = new ListViewItem([r.Time.ToString("yyyy-MM-dd HH:mm:ss"), r.Minutes.ToString(), late ? "LATE" : "on time", store.Features.CurrentBranch]);
            if (late) item.ForeColor = Color.FromArgb(235, 120, 100);
            else item.ForeColor = Color.FromArgb(130, 220, 150);
            lv.Items.Add(item);
        }
        var close = ReportButton("Close", new Point(496, 502), new Size(128, 40), panelLight, Color.White);
        close.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([head, targetLbl, targetNum, saveTarget, lv, close]);
        f.ShowDialog();
    }

    private void ShowAiInsights()
    {
        if (!store.Features.AiInsights) return;
        using var f = ReportShell("AI insights", 760, 560);
        var from = DateTime.Today.AddDays(-6);
        var sales = store.Sales.Where(s => s.Time.Date >= from && !s.IsRefund).ToList();
        var lines = new List<string>();
        if (sales.Count == 0)
        {
            lines.Add("No sales data yet — insights appear once orders come in.");
        }
        else
        {
            var gross = sales.Sum(s => s.Total);
            lines.Add($"PERFORMANCE — {sales.Count} sales, {gross:C} over the last 7 days, avg ticket {(gross / sales.Count):C}.");
            var bestHour = sales.GroupBy(s => s.Time.Hour).OrderByDescending(g => g.Sum(s => s.Total)).First();
            lines.Add($"PEAK HOUR — {bestHour.Key}:00 generated {bestHour.Sum(s => s.Total):C} ({bestHour.Count()} sales). Consider extra staff around this hour.");
            var items = sales.SelectMany(s => s.Items ?? []).GroupBy(i => i.Product);
            if (items.Any())
            {
                var top = items.OrderByDescending(g => g.Sum(i => i.Quantity)).First();
                lines.Add($"BEST SELLER — {top.Key}: {top.Sum(i => i.Quantity)} sold, {top.Sum(i => i.UnitPrice * i.Quantity - i.Discount):C} revenue.");
                var slow = items.OrderBy(g => g.Sum(i => i.Quantity)).First();
                if (slow.Key != top.Key) lines.Add($"SLOW MOVER — {slow.Key}: only {slow.Sum(i => i.Quantity)} sold. Consider a promo or removing it.");
            }
            var cash = sales.Where(s => s.PaymentType.StartsWith("Cash")).Sum(s => s.Total);
            var card = sales.Where(s => s.PaymentType.StartsWith("Card")).Sum(s => s.Total);
            if (gross > 0) lines.Add($"PAYMENT MIX — cash {cash / gross:P0}, card {card / gross:P0}. {(cash > card ? "Card adoption is low — offer contactless." : "Card-first is healthy for queue speed.")}");
            var discSales = sales.Where(s => s.Discount > 0).ToList();
            if (sales.Count > 0) lines.Add($"DISCOUNT RATE — {discSales.Count}/{sales.Count} sales discounted ({discSales.Sum(s => s.Discount):C} given). Watch margin if this grows.");
            var refunds = store.Sales.Count(s => s.Time.Date >= from && s.IsRefund);
            lines.Add($"REFUNDS — {refunds} in 7 days." + (refunds > sales.Count / 10.0 ? " High vs sales volume — review staff training." : " Within a healthy range."));
            var daypart = sales.GroupBy(s => s.Time.Hour switch { < 11 => 0, < 14 => 1, < 17 => 2, < 21 => 3, _ => 4 }).OrderByDescending(g => g.Sum(s => s.Total)).First();
            var dpName = daypart.Key switch { 0 => "morning", 1 => "lunch", 2 => "afternoon", 3 => "dinner", _ => "night" };
            lines.Add($"BUSIEST DAYPART — {dpName} ({daypart.Sum(s => s.Total):C}). Staff and prep accordingly.");
            var bestOp = sales.GroupBy(s => s.Operator).OrderByDescending(g => g.Sum(s => s.Total)).First();
            lines.Add($"TOP SERVER — {bestOp.Key} with {bestOp.Sum(s => s.Total):C} across {bestOp.Count()} sales.");
            if (store.Operators.Any(o => o.Wage > 0))
            {
                var wageCost = store.Shifts.Where(sh => sh.ClockIn >= from).Sum(sh => ((sh.ClockOut ?? DateTime.Now) - sh.ClockIn).TotalHours);
                lines.Add($"LABOUR — {(decimal)wageCost:0.0} clocked hours in the period (wages not included in sales).");
            }
        }
        var box = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Location = new Point(14, 44), Size = new Size(716, 420), BackColor = panelLight, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), BorderStyle = BorderStyle.FixedSingle, Text = string.Join("\r\n\r\n", lines.Select(l => "• " + l)) };
        var head = new Label { Text = "Auto analysis of your sales", AutoSize = true, Location = new Point(14, 14), ForeColor = mint, Font = new Font("Segoe UI Semibold", 12F) };
        var close = ReportButton("Close", new Point(600, 478), new Size(130, 40), panelLight, Color.White);
        close.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([head, box, close]);
        f.ShowDialog();
    }

    private void ShowCashDrawer()
    {
        if (!store.Features.CashDrawer) return;
        if (!HasPermission("Cash drawer"))
        {
            MessageBox.Show("You do not have the \"Cash drawer\" permission.\nAsk your manager to assign it in Settings → Operators.", "Cash drawer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        using var form = new Form { Text = "Cash drawer & cash-up", Size = new Size(560, 600), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };

        void Rebuild()
        {
            form.Controls.Clear();
            var movements = store.CashMovements.Where(m => m.Time.Date == DateTime.Today).ToList();
            var cashSales = store.Sales.Where(s => s.Time.Date == DateTime.Today).Sum(CashPortion);
            var floatIn = movements.Where(m => m.Kind == "Float").Sum(m => m.Amount);
            var paidIn = movements.Where(m => m.Kind == "Paid in").Sum(m => m.Amount);
            var paidOut = movements.Where(m => m.Kind == "Paid out").Sum(m => m.Amount);
            var expected = cashSales + floatIn + paidIn - paidOut;

            var info = new Label
            {
                Location = new Point(20, 15),
                Size = new Size(520, 92),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F),
                Text = $"Net cash sales (incl. refunds): {cashSales:C}\nFloat: {floatIn:C}     Paid in: {paidIn:C}     Paid out: {paidOut:C}\nExpected in drawer:  {expected:C}"
            };
            var lv = new ListView { Location = new Point(20, 115), Size = new Size(520, 240), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true };
            lv.Columns.Add("Time", 70); lv.Columns.Add("Kind", 90); lv.Columns.Add("Reason", 240); lv.Columns.Add("Amount", 100);
            foreach (var m in movements.OrderByDescending(m => m.Time))
                lv.Items.Add(new ListViewItem([m.Time.ToString("HH:mm"), m.Kind, m.Reason, m.Amount.ToString("C")]));
            if (lv.Items.Count > 0) lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            Button FlatBtn(string text, int x, int y, int w, int h, Color back)
            {
                var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = back, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F) };
                b.FlatAppearance.BorderSize = 0;
                return b;
            }

            var floatBtn = FlatBtn("Open float", 20, 365, 168, 36, Color.FromArgb(55, 120, 130));
            var paidInBtn = FlatBtn("Paid in", 196, 365, 168, 36, Color.FromArgb(60, 130, 90));
            var paidOutBtn = FlatBtn("Paid out", 372, 365, 168, 36, Color.FromArgb(180, 70, 60));
            floatBtn.Click += (_, _) => { var v = PromptAmount("Open float", "Starting cash placed in the drawer:"); if (v is not null) { AddCashMovement(v.Value, "Float", "Drawer opened"); Rebuild(); } };
            paidInBtn.Click += (_, _) => { var v = PromptAmount("Paid in", "Cash taken out of the drawer (e.g. petty cash deposit):"); if (v is not null) { AddCashMovement(v.Value, "Paid in", "Cash added to drawer"); Rebuild(); } };
            paidOutBtn.Click += (_, _) => { var v = PromptAmount("Paid out", "Cash taken out of the drawer (e.g. supplier payment):"); if (v is not null) { AddCashMovement(-v.Value, "Paid out", "Cash removed from drawer"); Rebuild(); } };

            var countBtn = FlatBtn("Count cash & record cash-up", 20, 410, 520, 40, mint);
            countBtn.ForeColor = background;
            countBtn.Click += (_, _) =>
            {
                var v = PromptAmount("Cash-up", $"Enter the counted cash in the drawer.\nExpected: {expected:C}");
                if (v is null) return;
                var diff = v.Value - expected;
                store.CashMovements.Add(new CashMovement(DateTime.Now, v.Value, "Cash-up", $"Expected {expected:C} — variance {diff:C}", currentOperator.Name));
                store.SaveCashMovements(store.CashMovements);
                if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Cash-up", $"Counted {v.Value:C}, expected {expected:C}, variance {diff:C}");
                MessageBox.Show($"Counted: {v.Value:C}\nExpected: {expected:C}\n{(diff >= 0 ? "Over:" : "Short:")} {Math.Abs(diff):C}", "Cash-up result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Rebuild();
            };
            var kickBtn = FlatBtn("Test drawer open", 20, 458, 168, 36, panelLight);
            kickBtn.Click += (_, _) => TryOpenDrawerKick();
            var noSaleBtn = FlatBtn("No sale (open & log)", 196, 458, 168, 36, Color.FromArgb(120, 90, 50));
            noSaleBtn.Click += (_, _) =>
            {
                var reason = PromptText("No sale", "Reason (optional, e.g. 'cashier change'):");
                if (reason is null) return;
                TryOpenDrawerKick();
                AddCashMovement(0, "No sale", string.IsNullOrWhiteSpace(reason) ? "Drawer opened without a sale" : reason);
                if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "No sale", string.IsNullOrWhiteSpace(reason) ? "drawer opened" : reason);
                Rebuild();
            };
            var closeBtn = FlatBtn("Close", 372, 458, 168, 36, panelLight);
            closeBtn.Click += (_, _) => form.DialogResult = DialogResult.OK;

            form.Controls.AddRange([info, lv, floatBtn, paidInBtn, paidOutBtn, countBtn, kickBtn, noSaleBtn, closeBtn]);
        }

        Rebuild();
        form.ShowDialog();
    }

    private static decimal CashPortion(Sale s)
    {
        if (s.CashAmount >= 0) return s.CashAmount;
        return (s.PaymentType ?? "Cash") == "Cash" ? s.Total : 0;
    }

    private void AddCashMovement(decimal amount, string kind, string reason)
    {
        store.CashMovements.Add(new CashMovement(DateTime.Now, amount, kind, reason, currentOperator.Name));
        store.SaveCashMovements(store.CashMovements);
        if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, kind, $"{amount:C} — {reason}");
    }

    private decimal? PromptAmount(string title, string caption)
    {
        using var f = new Form { Text = title, Size = new Size(380, 210), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var cap = new Label { Text = caption, Location = new Point(20, 15), Size = new Size(335, 48) };
        var input = new NumericUpDown { Minimum = 0, Maximum = 1000000, DecimalPlaces = 2, Location = new Point(20, 70), Size = new Size(200, 30), BackColor = panelLight, ForeColor = Color.White };
        var ok = new Button { Text = "OK", Location = new Point(20, 115), Size = new Size(110, 36), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        var cancel = new Button { Text = "Cancel", Location = new Point(140, 115), Size = new Size(110, 36), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
        ok.Click += (_, _) => f.DialogResult = DialogResult.OK;
        cancel.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
        f.Controls.AddRange([cap, input, ok, cancel]);
        f.AcceptButton = ok;
        f.CancelButton = cancel;
        return f.ShowDialog() == DialogResult.OK ? input.Value : null;
    }

    private void ShowFeeSettings()
    {
        using var f = new Form { Text = "Fees & terminal", Size = new Size(430, 320), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var fees = store.FeeSettings;
        var svc = new NumericUpDown { Minimum = 0, Maximum = 50, DecimalPlaces = 1, Value = Math.Min(50, fees.ServicePct), Location = new Point(200, 20), Size = new Size(120, 30), BackColor = panelLight, ForeColor = Color.White };
        var cd = new NumericUpDown { Minimum = 0, Maximum = 50, DecimalPlaces = 1, Value = Math.Min(50, fees.CashDiscPct), Location = new Point(200, 62), Size = new Size(120, 30), BackColor = panelLight, ForeColor = Color.White };
        var su = new NumericUpDown { Minimum = 0, Maximum = 50, DecimalPlaces = 1, Value = Math.Min(50, fees.SurchargePct), Location = new Point(200, 104), Size = new Size(120, 30), BackColor = panelLight, ForeColor = Color.White };
        var offline = new CheckBox { Text = "Card terminal offline (queue card payments)", AutoSize = true, Location = new Point(20, 150), ForeColor = Color.White, Visible = store.Features.OfflinePayments, Checked = fees.TerminalOffline };
        var hint = new Label { Text = "Service charge adds % to the total. Cash discount subtracts % when paid in cash. Card surcharge adds % when paid by card.", AutoSize = false, Size = new Size(390, 56), Location = new Point(20, 178), ForeColor = Color.FromArgb(143, 154, 171) };
        var l1 = new Label { Text = "Service charge %:", AutoSize = true, Location = new Point(20, 24) };
        var l2 = new Label { Text = "Cash discount %:", AutoSize = true, Location = new Point(20, 66) };
        var l3 = new Label { Text = "Card surcharge %:", AutoSize = true, Location = new Point(20, 108) };
        var save = new Button { Text = "Save", Location = new Point(20, 240), Size = new Size(140, 38), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        var cancel = new Button { Text = "Cancel", Location = new Point(172, 240), Size = new Size(120, 38), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        save.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
        save.Click += (_, _) =>
        {
            store.SaveFees(new FeeSettings(svc.Value, cd.Value, su.Value, offline.Checked));
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Payment fees", $"service {svc.Value}% cash disc {cd.Value}% surcharge {su.Value}% offline={offline.Checked}");
            f.DialogResult = DialogResult.OK;
        };
        cancel.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
        f.Controls.AddRange([l1, svc, l2, cd, l3, su, offline, hint, save, cancel]);
        f.AcceptButton = save;
        f.CancelButton = cancel;
        f.ShowDialog();
    }

    private void ShowFindReceipt(DateTime? preselect = null)
    {
        using var f = new Form { Text = "Find receipt", Size = new Size(780, 560), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.Sizable, MaximizeBox = false, MinimizeBox = false };
        var date = new DateTimePicker { Location = new Point(14, 12), Size = new Size(150, 26), Format = DateTimePickerFormat.Short, Value = (preselect ?? DateTime.Today).Date, BackColor = panelLight, ForeColor = Color.White };
        var search = new TextBox { Location = new Point(176, 12), Size = new Size(300, 26), BackColor = panelLight, ForeColor = Color.White, PlaceholderText = "Search: #, table, customer, type..." };
        var findBtn = new Button { Text = "Find", Location = new Point(486, 10), Size = new Size(90, 30), BackColor = Color.FromArgb(55, 120, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        findBtn.FlatAppearance.BorderSize = 0;
        var lv = new ListView { Location = new Point(14, 48), Size = new Size(738, 380), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, MultiSelect = false, GridLines = true };
        lv.Columns.AddRange([new ColumnHeader { Text = "Time", Width = 130 }, new ColumnHeader { Text = "Pay #", Width = 60 }, new ColumnHeader { Text = "Bill #", Width = 60 }, new ColumnHeader { Text = "Table", Width = 120 }, new ColumnHeader { Text = "Customer", Width = 130 }, new ColumnHeader { Text = "Type", Width = 170 }, new ColumnHeader { Text = "Total", Width = 90 }]);
        void RefreshList()
        {
            lv.Items.Clear();
            var q = search.Text.Trim();
            foreach (var s in store.Sales.Where(s => s.Time.Date == date.Value.Date)
                .Where(s => q.Length == 0
                    || s.PaymentNumber.ToString().Contains(q, StringComparison.OrdinalIgnoreCase)
                    || s.BillNumber.ToString().Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (s.Table ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (s.CustomerName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (s.PaymentType ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.Time))
            {
                lv.Items.Add(new ListViewItem([s.Time.ToString("HH:mm:ss"), s.PaymentNumber.ToString(), s.BillNumber.ToString(), s.Table ?? "", s.CustomerName ?? "", s.PaymentType ?? "", s.Total.ToString("C")]) { Tag = s });
            }
            if (lv.Items.Count == 0 && preselect is { } p && p.Date == date.Value.Date)
            {
                var last = store.Sales.LastOrDefault(s => s.Time.Date == p.Date);
                if (last != null && lv.Items.Count == 0)
                    foreach (var s in store.Sales.Where(s => s.Time.Date == p.Date).OrderByDescending(s => s.Time).Take(1))
                        lv.Items.Add(new ListViewItem([s.Time.ToString("HH:mm:ss"), s.PaymentNumber.ToString(), s.BillNumber.ToString(), s.Table ?? "", s.CustomerName ?? "", s.PaymentType ?? "", s.Total.ToString("C")]) { Tag = s });
            }
        }
        findBtn.Click += (_, _) => RefreshList();
        search.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { RefreshList(); e.SuppressKeyPress = true; } };
        date.ValueChanged += (_, _) => RefreshList();
        RefreshList();
        var reprintBtn = new Button { Text = "Reprint", Location = new Point(14, 442), Size = new Size(150, 40), BackColor = Color.FromArgb(60, 110, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
        var reopenBtn = new Button { Text = "Reopen check", Location = new Point(176, 442), Size = new Size(170, 40), BackColor = Color.FromArgb(70, 90, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
        var closeBtn = new Button { Text = "Close", Location = new Point(602, 442), Size = new Size(150, 40), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
        reprintBtn.FlatAppearance.BorderSize = 0; reopenBtn.FlatAppearance.BorderSize = 0; closeBtn.FlatAppearance.BorderSize = 0;
        reprintBtn.Click += (_, _) =>
        {
            if (lv.SelectedItems.Count == 0) { MessageBox.Show("Select a receipt first.", "Reprint", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (lv.SelectedItems[0].Tag is not Sale s) return;
            PrintPaymentReceipt(s, s.Items ?? [], 0, "", s.Table ?? "");
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Receipt reprint", $"payment #{s.PaymentNumber} {s.Total:C}");
        };
        reopenBtn.Click += (_, _) =>
        {
            if (lv.SelectedItems.Count == 0) { MessageBox.Show("Select a receipt first.", "Reopen", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (lv.SelectedItems[0].Tag is not Sale s) return;
            var items = s.Items ?? [];
            if (items.Count == 0) { MessageBox.Show("This sale has no line items to reopen.", "Reopen", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (basket.Count > 0 && MessageBox.Show($"Replace the current basket ({basket.Count} line(s)) with this check?", "Reopen", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (hasOpenOrder)
            {
                MessageBox.Show("Finish or park the open order on this table before reopening a closed check.", "Reopen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            basket.Clear();
            foreach (var item in items)
            {
                var product = products.FirstOrDefault(p => p.Name == item.Product);
                if (product == null) continue;
                var idx = basket.FindIndex(b => b.Product == product && b.Note == item.Note && b.Modifiers == item.Modifiers);
                if (idx >= 0) basket[idx] = (product, basket[idx].Count + item.Quantity, basket[idx].Course, basket[idx].Note, item.Seat, JoinMods(basket[idx].Modifiers, item.Modifiers));
                else basket.Add((product, item.Quantity, item.Course, item.Note, item.Seat, item.Modifiers));
            }
            hasOpenOrder = false;
            RenderBasket();
            UpdateOrderButtons();
            orderLabel.Text = OrderContextText();
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Check reopened", $"payment #{s.PaymentNumber} into basket");
            f.DialogResult = DialogResult.OK;
        };
        closeBtn.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
        f.Controls.AddRange([date, search, findBtn, lv, reprintBtn, reopenBtn, closeBtn]);
        f.AcceptButton = findBtn;
        f.ShowDialog();
    }

    private void ShowSpecialOrders()
    {
        if (!store.Features.Layaway) return;
        using var f = new Form { Text = "Special orders & deposits", Size = new Size(760, 520), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var lv = new ListView { Location = new Point(14, 12), Size = new Size(716, 360), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, MultiSelect = false, GridLines = true };
        lv.Columns.AddRange([new ColumnHeader { Text = "#", Width = 50 }, new ColumnHeader { Text = "Created", Width = 130 }, new ColumnHeader { Text = "Customer", Width = 140 }, new ColumnHeader { Text = "Table", Width = 100 }, new ColumnHeader { Text = "Total", Width = 90 }, new ColumnHeader { Text = "Paid", Width = 90 }, new ColumnHeader { Text = "Due", Width = 90 }, new ColumnHeader { Text = "Status", Width = 90 }]);
        void RefreshList()
        {
            lv.Items.Clear();
            foreach (var o in store.SpecialOrders.OrderByDescending(o => o.Time))
            {
                var item = new ListViewItem([o.Id.ToString(), o.Time.ToString("dd/MM HH:mm"), o.Customer, o.Table, o.Total.ToString("C"), o.Paid.ToString("C"), (o.Total - o.Paid).ToString("C"), o.Status]) { Tag = o };
                if (o.Status == "Delivered") item.ForeColor = Color.FromArgb(130, 200, 150);
                else if (o.Status == "Cancelled") item.ForeColor = Color.FromArgb(150, 160, 175);
                lv.Items.Add(item);
            }
        }
        RefreshList();
        void Save() => store.SaveSpecialOrders(store.SpecialOrders);
        var collectBtn = new Button { Text = "Collect balance", Location = new Point(14, 384), Size = new Size(170, 40), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
        var cancelBtn = new Button { Text = "Cancel order", Location = new Point(196, 384), Size = new Size(150, 40), BackColor = Color.FromArgb(180, 70, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
        var closeBtn = new Button { Text = "Close", Location = new Point(596, 384), Size = new Size(134, 40), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
        collectBtn.FlatAppearance.BorderSize = 0; cancelBtn.FlatAppearance.BorderSize = 0; closeBtn.FlatAppearance.BorderSize = 0;
        collectBtn.Click += (_, _) =>
        {
            if (lv.SelectedItems.Count == 0 || lv.SelectedItems[0].Tag is not SpecialOrder o) { MessageBox.Show("Select a special order first.", "Collect", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (o.Status != "Open") { MessageBox.Show($"This order is {o.Status}.", "Collect", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var due = o.Total - o.Paid;
            if (due <= 0) return;
            using var pf = new Form { Text = "Collect balance", Size = new Size(400, 250), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var cap = new Label { Text = $"Order #{o.Id} — balance due: {due:C}", AutoSize = true, Location = new Point(20, 16) };
            var amt = new NumericUpDown { Minimum = 0.01m, Maximum = 1000000, DecimalPlaces = 2, Value = Math.Min(1000000, due), Location = new Point(20, 50), Size = new Size(170, 30), BackColor = panelLight, ForeColor = Color.White };
            var methodLabel = new Label { Text = "Paid by:", AutoSize = true, Location = new Point(20, 94) };
            var method = new ComboBox { Location = new Point(20, 118), Size = new Size(170, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
            method.Items.AddRange(["Cash", "Card", "Transfer", "Mobile"]);
            method.SelectedIndex = 0;
            var ok = new Button { Text = "Collect", Location = new Point(20, 162), Size = new Size(130, 38), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
            var cancel2 = new Button { Text = "Cancel", Location = new Point(162, 162), Size = new Size(110, 38), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            ok.FlatAppearance.BorderSize = 0; cancel2.FlatAppearance.BorderSize = 0;
            ok.Click += (_, _) => pf.DialogResult = DialogResult.OK;
            cancel2.Click += (_, _) => pf.DialogResult = DialogResult.Cancel;
            pf.Controls.AddRange([cap, amt, methodLabel, method, ok, cancel2]);
            pf.AcceptButton = ok;
            if (pf.ShowDialog() != DialogResult.OK) return;
            var oi = store.SpecialOrders.FindIndex(x => x.Id == o.Id);
            if (oi < 0) return;
            var paid = amt.Value;
            store.SpecialOrders[oi] = store.SpecialOrders[oi] with { Paid = store.SpecialOrders[oi].Paid + paid, Status = store.SpecialOrders[oi].Total - store.SpecialOrders[oi].Paid - paid <= 0.005m ? "Delivered" : "Open" };
            Save();
            store.AddSale(new Sale(DateTime.Now, paid, o.Table, currentOperator.Name, [], store.NextPaymentNumber(), o.PaymentNum, 0, $"{method.Text} (special #{o.Id})", o.Customer, "Special", 0, CashAmount: method.Text == "Cash" ? paid : 0));
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Special order balance", $"#{o.Id} +{paid:C} via {method.Text} → {store.SpecialOrders[oi].Status}");
            RefreshList();
        };
        cancelBtn.Click += (_, _) =>
        {
            if (lv.SelectedItems.Count == 0 || lv.SelectedItems[0].Tag is not SpecialOrder o) { MessageBox.Show("Select a special order first.", "Cancel", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (o.Status != "Open") { MessageBox.Show($"This order is {o.Status}.", "Cancel", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show($"Cancel special order #{o.Id}?\nDeposit of {o.Paid:C} is kept (refund separately if needed).", "Cancel order", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var oi = store.SpecialOrders.FindIndex(x => x.Id == o.Id);
            if (oi >= 0) store.SpecialOrders[oi] = store.SpecialOrders[oi] with { Status = "Cancelled" };
            Save();
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Special order cancelled", $"#{o.Id}");
            RefreshList();
        };
        closeBtn.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([lv, collectBtn, cancelBtn, closeBtn]);
        f.ShowDialog();
    }

    private void ShowOfflineQueue()
    {
        if (!store.Features.OfflinePayments) return;
        using var f = new Form { Text = "Offline payment queue", Size = new Size(680, 480), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var status = new Label { Text = store.FeeSettings.TerminalOffline ? "Terminal mode: OFFLINE — card payments are being queued." : "Terminal mode: ONLINE.", AutoSize = true, Location = new Point(14, 14), ForeColor = store.FeeSettings.TerminalOffline ? Color.Orange : Color.FromArgb(130, 200, 150) };
        var lv = new ListView { Location = new Point(14, 44), Size = new Size(636, 300), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, MultiSelect = false, GridLines = true };
        lv.Columns.AddRange([new ColumnHeader { Text = "Time", Width = 130 }, new ColumnHeader { Text = "Pay #", Width = 60 }, new ColumnHeader { Text = "Table", Width = 110 }, new ColumnHeader { Text = "Operator", Width = 110 }, new ColumnHeader { Text = "Amount", Width = 100 }, new ColumnHeader { Text = "Status", Width = 90 }]);
        void RefreshList()
        {
            lv.Items.Clear();
            foreach (var p in store.OfflinePayments.OrderByDescending(p => p.Time))
            {
                var item = new ListViewItem([p.Time.ToString("dd/MM HH:mm"), p.PaymentNumber.ToString(), p.Table, p.Operator, p.Amount.ToString("C"), p.Status]) { Tag = p };
                if (p.Status == "Pending") item.ForeColor = Color.Orange;
                lv.Items.Add(item);
            }
        }
        RefreshList();
        var settleBtn = new Button { Text = "Terminal back online — settle all", Location = new Point(14, 356), Size = new Size(250, 40), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
        var closeBtn = new Button { Text = "Close", Location = new Point(516, 356), Size = new Size(134, 40), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
        settleBtn.FlatAppearance.BorderSize = 0; closeBtn.FlatAppearance.BorderSize = 0;
        settleBtn.Click += (_, _) =>
        {
            var pending = store.OfflinePayments.Where(p => p.Status == "Pending").ToList();
            if (pending.Count == 0) { MessageBox.Show("No pending offline payments.", "Offline queue", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            foreach (var p in pending)
            {
                var i = store.OfflinePayments.FindIndex(x => x.PaymentNumber == p.PaymentNumber && x.Time == p.Time);
                if (i >= 0) store.OfflinePayments[i] = store.OfflinePayments[i] with { Status = "Cleared" };
            }
            store.SaveOfflinePayments(store.OfflinePayments);
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Offline payments settled", $"{pending.Count} payment(s), {pending.Sum(p => p.Amount):C}");
            MessageBox.Show($"{pending.Count} offline payment(s) settled ({pending.Sum(p => p.Amount):C}).\n{pending.Sum(p => p.Amount):C} should be on the card terminal batch.", "Offline queue", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshList();
        };
        closeBtn.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([status, lv, settleBtn, closeBtn]);
        f.ShowDialog();
    }

    private void ShowVirtualTerminal()
    {
        if (!store.Features.VirtualTerminal) return;
        using var f = new Form { Text = "Virtual terminal (phone orders)", Size = new Size(760, 540), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var amtLabel = new Label { Text = "Amount:", AutoSize = true, Location = new Point(14, 16) };
        var amt = new NumericUpDown { Minimum = 0.01m, Maximum = 1000000, DecimalPlaces = 2, Location = new Point(80, 12), Size = new Size(120, 30), BackColor = panelLight, ForeColor = Color.White };
        var nameLabel = new Label { Text = "Name:", AutoSize = true, Location = new Point(214, 16) };
        var nameBox = new TextBox { Location = new Point(258, 12), Size = new Size(150, 26), BackColor = panelLight, ForeColor = Color.White };
        var phoneLabel = new Label { Text = "Phone:", AutoSize = true, Location = new Point(420, 16) };
        var phoneBox = new TextBox { Location = new Point(468, 12), Size = new Size(130, 26), BackColor = panelLight, ForeColor = Color.White };
        var noteBox = new TextBox { Location = new Point(80, 48), Size = new Size(518, 26), BackColor = panelLight, ForeColor = Color.White, PlaceholderText = "Note (optional)" };
        var createBtn = new Button { Text = "Create invoice link", Location = new Point(612, 12), Size = new Size(134, 62), BackColor = Color.FromArgb(60, 130, 90), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F) };
        createBtn.FlatAppearance.BorderSize = 0;
        var lv = new ListView { Location = new Point(14, 88), Size = new Size(732, 340), BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, MultiSelect = false, GridLines = true };
        lv.Columns.AddRange([new ColumnHeader { Text = "#", Width = 50 }, new ColumnHeader { Text = "Created", Width = 130 }, new ColumnHeader { Text = "Name", Width = 130 }, new ColumnHeader { Text = "Phone", Width = 110 }, new ColumnHeader { Text = "Amount", Width = 90 }, new ColumnHeader { Text = "Status", Width = 80 }, new ColumnHeader { Text = "Note", Width = 110 }]);
        string LinkFor(int id) => $"http://{TcpPrintServer.LocalIp}:8080/vt/{id}";
        void RefreshList()
        {
            lv.Items.Clear();
            foreach (var inv in store.VtInvoices.OrderByDescending(v => v.Time))
            {
                var item = new ListViewItem([inv.Id.ToString(), inv.Time.ToString("dd/MM HH:mm"), inv.Name, inv.Phone, inv.Amount.ToString("C"), inv.Status, inv.Note]) { Tag = inv };
                if (inv.Status == "Paid") item.ForeColor = Color.FromArgb(130, 200, 150);
                lv.Items.Add(item);
            }
        }
        RefreshList();
        createBtn.Click += (_, _) =>
        {
            if (amt.Value <= 0) { MessageBox.Show("Enter an amount.", "Virtual terminal", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(nameBox.Text)) { MessageBox.Show("Enter the customer's name.", "Virtual terminal", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var id = store.VtInvoices.Count > 0 ? store.VtInvoices.Max(v => v.Id) + 1 : 1;
            store.VtInvoices.Add(new VtInvoice(id, DateTime.Now, nameBox.Text.Trim(), phoneBox.Text.Trim(), amt.Value, noteBox.Text.Trim(), "Pending"));
            store.SaveVtInvoices(store.VtInvoices);
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "VT invoice", $"#{id} {amt.Value:C} for {nameBox.Text.Trim()}");
            var link = LinkFor(id);
            try { Clipboard.SetText(link); } catch { }
            MessageBox.Show($"Invoice created.\n\nLink (copied to clipboard):\n{link}\n\nThe customer opens it on their phone and pays online.", "Virtual terminal", MessageBoxButtons.OK, MessageBoxIcon.Information);
            amt.Value = 0.01m; noteBox.Text = "";
            RefreshList();
        };
        var openBtn = new Button { Text = "Copy link", Location = new Point(14, 440), Size = new Size(130, 40), BackColor = Color.FromArgb(55, 120, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
        var markBtn = new Button { Text = "Mark paid", Location = new Point(156, 440), Size = new Size(140, 40), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
        var delBtn = new Button { Text = "Delete", Location = new Point(308, 440), Size = new Size(120, 40), BackColor = Color.FromArgb(180, 70, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
        var closeBtn = new Button { Text = "Close", Location = new Point(616, 440), Size = new Size(130, 40), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
        foreach (var b in new[] { openBtn, markBtn, delBtn, closeBtn }) b.FlatAppearance.BorderSize = 0;
        void MarkPaid(VtInvoice inv)
        {
            if (inv.Status == "Paid") return;
            var i = store.VtInvoices.FindIndex(v => v.Id == inv.Id);
            if (i >= 0) store.VtInvoices[i] = store.VtInvoices[i] with { Status = "Paid" };
            store.SaveVtInvoices(store.VtInvoices);
            store.AddSale(new Sale(DateTime.Now, inv.Amount, "VT", currentOperator.Name, [], store.NextPaymentNumber(), 0, 0, $"VT #{inv.Id} {inv.Name}", inv.Name, "Phone order", 0));
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "VT paid", $"#{inv.Id} {inv.Amount:C} {inv.Name}");
            RefreshList();
        }
        openBtn.Click += (_, _) =>
        {
            if (lv.SelectedItems.Count == 0 || lv.SelectedItems[0].Tag is not VtInvoice inv) { MessageBox.Show("Select an invoice first.", "Virtual terminal", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var link = LinkFor(inv.Id);
            try { Clipboard.SetText(link); } catch { }
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(link) { UseShellExecute = true }); } catch { }
        };
        markBtn.Click += (_, _) =>
        {
            if (lv.SelectedItems.Count == 0 || lv.SelectedItems[0].Tag is not VtInvoice inv) { MessageBox.Show("Select an invoice first.", "Virtual terminal", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (inv.Status == "Paid") { MessageBox.Show("Already paid.", "Virtual terminal", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            MarkPaid(inv);
        };
        delBtn.Click += (_, _) =>
        {
            if (lv.SelectedItems.Count == 0 || lv.SelectedItems[0].Tag is not VtInvoice inv) { MessageBox.Show("Select an invoice first.", "Virtual terminal", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (inv.Status == "Paid") { MessageBox.Show("Paid invoices cannot be deleted.", "Virtual terminal", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show($"Delete pending invoice #{inv.Id} ({inv.Amount:C})?", "Virtual terminal", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            store.VtInvoices.RemoveAll(v => v.Id == inv.Id);
            store.SaveVtInvoices(store.VtInvoices);
            RefreshList();
        };
        closeBtn.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([amtLabel, amt, nameLabel, nameBox, phoneLabel, phoneBox, noteBox, createBtn, lv, openBtn, markBtn, delBtn, closeBtn]);
        f.ShowDialog();
    }

    private void TryOpenDrawerKick()
    {
        var bytes = new byte[] { 0x1B, 0x40, 0x1B, 0x70, 0x00, 0x19, 0xFA };
        foreach (var d in store.PrinterDestinations)
        {
            if (string.IsNullOrWhiteSpace(d.WindowsPrinter)) continue;
            var name = ResolvePrinterName(d);
            if (string.IsNullOrWhiteSpace(name)) continue;
            try
            {
                if (!RawPrinter.OpenPrinterLocal(name, out var printer, 0)) continue;
                try
                {
                    var docInfo = new RawPrinter.DocumentInfo { DocumentName = "Drawer open", OutputFile = null, DataType = null };
                    RawPrinter.StartDocPrinter(printer, 1, docInfo);
                    RawPrinter.StartPagePrinter(printer);
                    RawPrinter.WritePrinter(printer, bytes, bytes.Length, out _);
                    RawPrinter.EndPagePrinter(printer);
                    RawPrinter.EndDocPrinter(printer);
                }
                finally { RawPrinter.ClosePrinter(printer); }
                return;
            }
            catch { }
        }
    }

    private string NewGiftCardCode()
    {
        string code;
        do { code = "GC-" + Random.Shared.Next(100000, 999999); } while (store.GiftCards.Any(c => c.Code == code));
        return code;
    }

    private string? PromptText(string title, string caption, string prefill = "")
    {
        using var f = new Form { Text = title, Size = new Size(380, 200), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var cap = new Label { Text = caption, Location = new Point(20, 15), Size = new Size(335, 40) };
        var input = new TextBox { Location = new Point(20, 60), Size = new Size(335, 30), BackColor = panelLight, ForeColor = Color.White, Text = prefill };
        var ok = new Button { Text = "OK", Location = new Point(20, 105), Size = new Size(110, 36), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        var cancel = new Button { Text = "Cancel", Location = new Point(140, 105), Size = new Size(110, 36), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
        ok.Click += (_, _) => f.DialogResult = DialogResult.OK;
        cancel.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
        f.Controls.AddRange([cap, input, ok, cancel]);
        f.AcceptButton = ok;
        f.CancelButton = cancel;
        return f.ShowDialog() == DialogResult.OK ? input.Text.Trim() : null;
    }

    private void ShowGiftCards()
    {
        if (!store.Features.GiftCards) return;
        if (!HasPermission("Gift cards"))
        {
            MessageBox.Show("You do not have the \"Gift cards\" permission.\nAsk your manager to assign it in Settings → Operators.", "Gift cards", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        var lv = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true };
        lv.Columns.Add("Code", 130); lv.Columns.Add("Balance", 110); lv.Columns.Add("Sold for", 110); lv.Columns.Add("Created", 150);

        void Rebuild()
        {
            lv.Items.Clear();
            foreach (var c in store.GiftCards.OrderByDescending(c => c.Created))
            {
                var item = new ListViewItem([c.Code, c.Balance.ToString("C"), c.Sold.ToString("C"), c.Created.ToString("dd/MM/yyyy HH:mm")]);
                if (c.Balance <= 0) item.ForeColor = Color.FromArgb(150, 160, 175);
                else if (c.Balance <= 5) item.ForeColor = Color.FromArgb(245, 180, 80);
                lv.Items.Add(item);
            }
        }
        Rebuild();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 6, 0, 0) };
        Button B(string text, Color back)
        {
            var b = new Button { Text = text, Size = new Size(170, 36), BackColor = back, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 9.5F) };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
        var sellBtn = B("Sell new card", Color.FromArgb(60, 130, 90));
        var checkBtn = B("Check balance", Color.FromArgb(55, 120, 130));
        var topUpBtn = B("Add funds", Color.FromArgb(200, 140, 60));
        sellBtn.Click += (_, _) =>
        {
            using var f = new Form { Text = "Sell gift card", Size = new Size(360, store.Features.EGiftEmail ? 312 : 250), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var cap = new Label { Text = "Card value:", AutoSize = true, Location = new Point(20, 18) };
            var amount = new NumericUpDown { Minimum = 1, Maximum = 100000, Value = 25, Location = new Point(20, 45), Size = new Size(160, 30), BackColor = panelLight, ForeColor = Color.White, DecimalPlaces = 2 };
            var payLabel = new Label { Text = "Paid by:", AutoSize = true, Location = new Point(20, 88) };
            var pay = new ComboBox { Location = new Point(20, 113), Size = new Size(160, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
            pay.Items.AddRange(["Cash", "Card"]); pay.SelectedIndex = 0;
            var emailLabel = new Label { Text = "E-gift to:", AutoSize = true, Location = new Point(20, 156), Visible = store.Features.EGiftEmail };
            var emailBox = new TextBox { Location = new Point(20, 181), Size = new Size(250, 26), BackColor = panelLight, ForeColor = Color.White, Visible = store.Features.EGiftEmail, PlaceholderText = "recipient@email.com (optional)" };
            var ok = new Button { Text = "Sell", Location = new Point(20, 216), Size = new Size(120, 38), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "Cancel", Location = new Point(150, 216), Size = new Size(120, 38), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            if (!store.Features.EGiftEmail) { ok.Location = new Point(20, 158); cancel.Location = new Point(150, 158); }
            ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
            ok.Click += (_, _) => f.DialogResult = DialogResult.OK;
            cancel.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
            f.Controls.AddRange(store.Features.EGiftEmail ? [cap, amount, payLabel, pay, emailLabel, emailBox, ok, cancel] : [cap, amount, payLabel, pay, ok, cancel]);
            f.AcceptButton = ok;
            if (f.ShowDialog() != DialogResult.OK) return;
            var code = NewGiftCardCode();
            store.GiftCards.Add(new GiftCard(code, amount.Value, amount.Value, DateTime.Now));
            store.SaveGiftCards(store.GiftCards);
            store.AddSale(new Sale(DateTime.Now, amount.Value, "Gift card", currentOperator.Name, [], store.NextPaymentNumber(), 0, 0, $"Gift card sold ({pay.Text})", "", "Takeaway", 0, CashAmount: pay.Text == "Cash" ? amount.Value : 0));
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Gift card sold", $"{code} for {amount.Value:C} ({pay.Text})");
            var emailSent = false;
            if (store.Features.EGiftEmail && !string.IsNullOrWhiteSpace(emailBox.Text) && emailBox.Text.Contains('@'))
            {
                emailSent = SendMailWithAttachment(emailBox.Text.Trim(), $"Your gift card — {amount.Value:C}",
                    $"Your gift card code is {code}\nValue: {amount.Value:C}\nUse it at the counter anytime.", "");
                if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "E-gift sent", $"{code} → {emailBox.Text.Trim()} ok={emailSent}");
            }
            MessageBox.Show($"Gift card created\n\nCode: {code}\nValue: {amount.Value:C}" + (store.Features.EGiftEmail && emailBox.Text.Contains('@') ? (emailSent ? $"\n\nE-gift emailed to {emailBox.Text.Trim()}" : $"\n\nEmail failed — check SMTP settings (Settings > Receipts).") : ""), "Gift card sold", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Rebuild();
        };
        checkBtn.Click += (_, _) =>
        {
            var code = PromptText("Check balance", "Enter or scan the gift card code:");
            if (string.IsNullOrWhiteSpace(code)) return;
            var card = store.GiftCards.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
            if (card == null) MessageBox.Show($"Gift card \"{code}\" was not found.", "Gift cards", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else MessageBox.Show($"Code: {card.Code}\nBalance: {card.Balance:C}\nOriginally sold for: {card.Sold:C}\nCreated: {card.Created:dd/MM/yyyy}", "Gift card balance", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        topUpBtn.Click += (_, _) =>
        {
            var code = PromptText("Add funds", "Enter the gift card code:");
            if (string.IsNullOrWhiteSpace(code)) return;
            var gi = store.GiftCards.FindIndex(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
            if (gi < 0) { MessageBox.Show($"Gift card \"{code}\" was not found.", "Gift cards", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var amount = PromptAmount("Add funds", $"Current balance: {store.GiftCards[gi].Balance:C}\nAmount to add:");
            if (amount is null) return;
            store.GiftCards[gi] = store.GiftCards[gi] with { Balance = store.GiftCards[gi].Balance + amount.Value };
            store.SaveGiftCards(store.GiftCards);
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Gift card top-up", $"{store.GiftCards[gi].Code} +{amount.Value:C} → {store.GiftCards[gi].Balance:C}");
            Rebuild();
        };
        buttons.Controls.AddRange([sellBtn, checkBtn, topUpBtn]);
        panel.Controls.Add(lv);
        panel.Controls.Add(buttons);
        ShowView(panel, "Gift cards", ShowSettings);
    }

    private void ShowReservations()
    {
        if (!store.Features.Reservations) return;
        if (!HasPermission("Reservations"))
        {
            MessageBox.Show("You do not have the \"Reservations\" permission.\nAsk your manager to assign it in Settings → Operators.", "Reservations", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        var lv = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, MultiSelect = false };
        lv.Columns.Add("Name", 150); lv.Columns.Add("Phone", 130); lv.Columns.Add("Party", 55); lv.Columns.Add("When", 135); lv.Columns.Add("Room", 100); lv.Columns.Add("Table", 110); lv.Columns.Add("Status", 90);

        void Rebuild()
        {
            lv.Items.Clear();
            foreach (var r in store.Reservations.Where(r => r.When.Date >= DateTime.Today && r.Status != "Cancelled").OrderBy(r => r.When))
            {
                var item = new ListViewItem([r.Name, r.Phone, r.PartySize.ToString(), r.When.ToString("dd/MM HH:mm"), r.Room, r.Table, r.Status]);
                if (r.Status == "Seated") item.ForeColor = mint;
                else if (r.Status == "No-show") item.ForeColor = Color.FromArgb(235, 90, 90);
                else if (r.Status == "Done") item.ForeColor = Color.FromArgb(150, 160, 175);
                lv.Items.Add(item);
            }
        }
        Rebuild();

        void SetStatus(string status)
        {
            if (lv.SelectedItems.Count == 0) { MessageBox.Show("Select a reservation first.", "Reservations", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var name = lv.SelectedItems[0].Text;
            var whenTxt = lv.SelectedItems[0].SubItems[3].Text;
            var idx = store.Reservations.FindIndex(r => r.Name == name && r.When.ToString("dd/MM HH:mm") == whenTxt);
            if (idx < 0) return;
            store.Reservations[idx] = store.Reservations[idx] with { Status = status };
            store.SaveReservations(store.Reservations);
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Reservation", $"{store.Reservations[idx].Name} → {status}");
            Rebuild();
        }

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 6, 0, 0) };
        Button B(string text, Color back)
        {
            var b = new Button { Text = text, Size = new Size(120, 36), BackColor = back, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 9.5F) };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
        var addBtn = B("Add booking", Color.FromArgb(60, 130, 90));
        var seatBtn = B("Seat", Color.FromArgb(55, 120, 130));
        var doneBtn = B("Complete", panelLight);
        var noShowBtn = B("No-show", Color.FromArgb(180, 70, 60));
        addBtn.Click += (_, _) =>
        {
            using var f = new Form { Text = "Add reservation", Size = new Size(420, 400), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var nameT = new TextBox { Location = new Point(150, 18), Size = new Size(230, 28), BackColor = panelLight, ForeColor = Color.White };
            var phoneT = new TextBox { Location = new Point(150, 56), Size = new Size(230, 28), BackColor = panelLight, ForeColor = Color.White };
            var partyN = new NumericUpDown { Location = new Point(150, 94), Size = new Size(100, 28), BackColor = panelLight, ForeColor = Color.White, Minimum = 1, Maximum = 50, Value = 2 };
            var dateP = new DateTimePicker { Location = new Point(150, 132), Size = new Size(230, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            var timeP = new DateTimePicker { Location = new Point(150, 170), Size = new Size(230, 28), Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = DateTime.Now };
            var roomC = new ComboBox { Location = new Point(150, 208), Size = new Size(230, 28), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
            foreach (var r in store.Rooms) roomC.Items.Add(r.Name);
            if (store.Rooms.Count > 0) roomC.SelectedIndex = 0;
            var tableC = new ComboBox { Location = new Point(150, 246), Size = new Size(230, 28), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
            void FillTables()
            {
                tableC.Items.Clear();
                var room = store.Rooms.FirstOrDefault(r => r.Name == roomC.Text);
                if (room == null) return;
                for (var i = 0; i < room.TableCount; i++) tableC.Items.Add(TableName(room.Name, i));
                if (tableC.Items.Count > 0) tableC.SelectedIndex = 0;
            }
            roomC.SelectedIndexChanged += (_, _) => FillTables();
            FillTables();
            void Row(string text, Control c, int y) { f.Controls.Add(new Label { Text = text, AutoSize = true, Location = new Point(20, y + 4) }); c.Location = new Point(150, y); f.Controls.Add(c); }
            Row("Name:", nameT, 18); Row("Phone:", phoneT, 56); Row("Party size:", partyN, 94); Row("Date:", dateP, 132); Row("Time:", timeP, 170); Row("Room:", roomC, 208); Row("Table:", tableC, 246);
            var ok = new Button { Text = "Save booking", Location = new Point(20, 295), Size = new Size(170, 38), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "Cancel", Location = new Point(200, 295), Size = new Size(130, 38), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
            ok.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(nameT.Text)) { MessageBox.Show("Enter the customer name.", "Reservations", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var when = dateP.Value.Date + timeP.Value.TimeOfDay;
                store.Reservations.Add(new Reservation(nameT.Text.Trim(), phoneT.Text.Trim(), (int)partyN.Value, when, roomC.Text, tableC.Text, "Booked"));
                store.SaveReservations(store.Reservations);
                if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Reservation added", $"{nameT.Text} {when:dd/MM HH:mm} {roomC.Text}/{tableC.Text}");
                f.DialogResult = DialogResult.OK;
            };
            cancel.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
            f.Controls.AddRange([ok, cancel]);
            f.AcceptButton = ok;
            if (f.ShowDialog() == DialogResult.OK) Rebuild();
        };
        seatBtn.Click += (_, _) => SetStatus("Seated");
        doneBtn.Click += (_, _) => SetStatus("Done");
        noShowBtn.Click += (_, _) => SetStatus("No-show");
        buttons.Controls.AddRange([addBtn, seatBtn, doneBtn, noShowBtn]);
        panel.Controls.Add(lv);
        panel.Controls.Add(buttons);
        ShowView(panel, "Reservations", ShowSettings);
    }

    private static DateTime WeekStart(DateTime any)
    {
        var diff = ((int)any.DayOfWeek + 6) % 7;
        return any.Date.AddDays(-diff);
    }

    private void ShowSchedule()
    {
        if (!store.Features.Scheduling) return;
        if (!HasPermission("Manage operators"))
        {
            MessageBox.Show("You do not have the \"Manage operators\" permission.\nAsk your manager to assign it in Settings → Operators.", "Schedule", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        var tabs = new TabControl { Dock = DockStyle.Fill };

        var weekStart = WeekStart(DateTime.Today);
        var scheduleTab = new TabPage { Text = "Schedule", BackColor = background, ForeColor = Color.White };
        var header = new Panel { Dock = DockStyle.Top, Height = 44 };
        var weekLabel = new Label { AutoSize = true, Location = new Point(210, 13), Font = new Font("Segoe UI Semibold", 11F), ForeColor = mint };
        var summaryLabel = new Label { AutoSize = true, Location = new Point(470, 14), ForeColor = Color.FromArgb(170, 180, 195) };
        var grid = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, MultiSelect = false };
        grid.Columns.Add("Operator", 150);
        var dayNames = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        foreach (var d in dayNames) grid.Columns.Add(d[..3], 95);
        grid.Columns.Add("Hours", 65);

        void RebuildSchedule()
        {
            weekLabel.Text = $"{weekStart:dd/MM} — {weekStart.AddDays(6):dd/MM/yyyy}";
            grid.Items.Clear();
            var weekEntries = store.Schedule.Where(e => e.Date >= weekStart && e.Date < weekStart.AddDays(7)).ToList();
            foreach (var op in store.Operators)
            {
                var mine = weekEntries.Where(e => e.Operator == op.Name).ToList();
                if (mine.Count == 0) continue;
                var item = new ListViewItem([op.Name]);
                decimal hours = 0;
                for (var d = 0; d < 7; d++)
                {
                    var day = weekStart.AddDays(d);
                    var slots = mine.Where(e => e.Date == day).OrderBy(e => e.Start).Select(e => $"{e.Start:hh\\:mm}–{e.End:hh\\:mm}");
                    var text = string.Join(", ", slots);
                    item.SubItems.Add(text);
                    foreach (var e in mine.Where(e => e.Date == day))
                    {
                        var span = e.End - e.Start;
                        if (span < TimeSpan.Zero) span += TimeSpan.FromDays(1);
                        hours += (decimal)span.TotalHours;
                    }
                }
                item.SubItems.Add(hours.ToString("0.#"));
                grid.Items.Add(item);
            }
            var totalHours = weekEntries.Sum(e => { var span = e.End - e.Start; if (span < TimeSpan.Zero) span += TimeSpan.FromDays(1); return (decimal)span.TotalHours; });
            var labor = weekEntries.Sum(e =>
            {
                var span = e.End - e.Start;
                if (span < TimeSpan.Zero) span += TimeSpan.FromDays(1);
                var wage = store.Operators.FirstOrDefault(o => o.Name == e.Operator)?.Wage ?? 0m;
                return (decimal)span.TotalHours * wage;
            });
            summaryLabel.Text = $"{totalHours:0.#} h scheduled  •  labor {labor:C}";
        }

        Button HBtn(string text, int x, Color back)
        {
            var b = new Button { Text = text, Location = new Point(x, 6), Size = new Size(90, 32), BackColor = back, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9F) };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
        var prevBtn = HBtn("◀ Prev", 6, panelLight);
        var nextBtn = HBtn("Next ▶", 102, panelLight);
        var addShiftBtn = HBtn("+ Shift", 300, Color.FromArgb(60, 130, 90));
        var delShiftBtn = HBtn("Delete", 396, Color.FromArgb(180, 70, 60));
        prevBtn.Click += (_, _) => { weekStart = weekStart.AddDays(-7); RebuildSchedule(); };
        nextBtn.Click += (_, _) => { weekStart = weekStart.AddDays(7); RebuildSchedule(); };
        addShiftBtn.Click += (_, _) =>
        {
            using var f = new Form { Text = "Add shift", Size = new Size(380, 330), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var opC = new ComboBox { Location = new Point(150, 20), Size = new Size(180, 28), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
            foreach (var o in store.Operators) opC.Items.Add(o.Name);
            if (opC.Items.Count > 0) opC.SelectedIndex = 0;
            var dateP = new DateTimePicker { Location = new Point(150, 60), Size = new Size(180, 28), Format = DateTimePickerFormat.Short, Value = weekStart };
            var startP = new DateTimePicker { Location = new Point(150, 100), Size = new Size(180, 28), Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = DateTime.Today.AddHours(9) };
            var endP = new DateTimePicker { Location = new Point(150, 140), Size = new Size(180, 28), Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = DateTime.Today.AddHours(17) };
            void Row(string t, Control c, int y) { f.Controls.Add(new Label { Text = t, AutoSize = true, Location = new Point(20, y + 4) }); c.Location = new Point(150, y); f.Controls.Add(c); }
            Row("Operator:", opC, 20); Row("Date:", dateP, 60); Row("Start:", startP, 100); Row("End:", endP, 140);
            var ok = new Button { Text = "Save", Location = new Point(20, 190), Size = new Size(120, 38), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "Cancel", Location = new Point(150, 190), Size = new Size(120, 38), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
            ok.Click += (_, _) =>
            {
                if (opC.Text == "") { MessageBox.Show("Select an operator.", "Schedule", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                store.Schedule.Add(new ScheduleEntry(opC.Text, dateP.Value.Date, startP.Value.TimeOfDay, endP.Value.TimeOfDay));
                store.SaveSchedule(store.Schedule);
                if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Shift added", $"{opC.Text} {dateP.Value:dd/MM} {startP.Value:HH:mm}–{endP.Value:HH:mm}");
                f.DialogResult = DialogResult.OK;
            };
            cancel.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
            f.Controls.AddRange([ok, cancel]);
            f.AcceptButton = ok;
            if (f.ShowDialog() == DialogResult.OK) RebuildSchedule();
        };
        delShiftBtn.Click += (_, _) =>
        {
            if (grid.SelectedItems.Count == 0) { MessageBox.Show("Select an operator row first, then the shift is deleted by day — use the audit log for history.", "Schedule", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var name = grid.SelectedItems[0].Text;
            using var f = new Form { Text = "Delete shift", Size = new Size(360, 240), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var info = new Label { Text = $"Delete a shift for {name}:", AutoSize = true, Location = new Point(20, 15) };
            var dateP = new DateTimePicker { Location = new Point(20, 45), Size = new Size(200, 28), Format = DateTimePickerFormat.Short, Value = weekStart };
            var ok = new Button { Text = "Delete", Location = new Point(20, 90), Size = new Size(120, 38), BackColor = Color.FromArgb(180, 70, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "Cancel", Location = new Point(150, 90), Size = new Size(120, 38), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
            ok.Click += (_, _) =>
            {
                var before = store.Schedule.Count;
                store.Schedule.RemoveAll(e => e.Operator == name && e.Date == dateP.Value.Date);
                if (store.Schedule.Count != before) { store.SaveSchedule(store.Schedule); if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Shift deleted", $"{name} {dateP.Value:dd/MM}"); }
                f.DialogResult = DialogResult.OK;
            };
            cancel.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
            f.Controls.AddRange([info, dateP, ok, cancel]);
            if (f.ShowDialog() == DialogResult.OK) RebuildSchedule();
        };
        header.Controls.AddRange([prevBtn, nextBtn, weekLabel, addShiftBtn, delShiftBtn, summaryLabel]);
        scheduleTab.Controls.AddRange([grid, header]);

        var tipsTab = new TabPage { Text = "Tip pool", BackColor = background, ForeColor = Color.White };
        var tipsHeader = new Panel { Dock = DockStyle.Top, Height = 44 };
        var dateP2 = new DateTimePicker { Location = new Point(6, 7), Size = new Size(140, 30), Format = DateTimePickerFormat.Short, Value = DateTime.Today, BackColor = panelLight, ForeColor = Color.White };
        var ruleC = new ComboBox { Location = new Point(156, 7), Size = new Size(150, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
        ruleC.Items.AddRange(["Equal split", "By scheduled hours", "By sales"]); ruleC.SelectedIndex = 0;
        var computeBtn = new Button { Text = "Compute pool", Location = new Point(316, 7), Size = new Size(140, 30), BackColor = Color.FromArgb(60, 130, 90), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        computeBtn.FlatAppearance.BorderSize = 0;
        var tipsGrid = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true };
        tipsGrid.Columns.Add("Operator", 180); tipsGrid.Columns.Add("Share", 120); tipsGrid.Columns.Add("Amount", 120);
        computeBtn.Click += (_, _) =>
        {
            var day = dateP2.Value.Date;
            var tips = store.Sales.Where(s => s.Time.Date == day && s.Tip > 0 && !s.IsRefund).ToList();
            var totalTips = tips.Sum(s => s.Tip);
            if (totalTips <= 0) { MessageBox.Show($"No tips recorded on {day:dd/MM/yyyy}.", "Tip pool", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var bySales = tips.GroupBy(s => s.Operator).ToDictionary(g => g.Key, g => g.Sum(s => s.Tip));
            var byHours = new Dictionary<string, decimal>();
            foreach (var e in store.Schedule.Where(e => e.Date == day))
            {
                var span = e.End - e.Start;
                if (span < TimeSpan.Zero) span += TimeSpan.FromDays(1);
                byHours[e.Operator] = byHours.GetValueOrDefault(e.Operator) + (decimal)span.TotalHours;
            }
            var participants = store.Operators.Where(o => bySales.ContainsKey(o.Name) || byHours.ContainsKey(o.Name) || store.Schedule.Any(s => s.Date == day && s.Operator == o.Name)).Select(o => o.Name).Distinct().ToList();
            if (participants.Count == 0) { MessageBox.Show("No participants for that day.", "Tip pool", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            tipsGrid.Items.Clear();
            foreach (var name in participants)
            {
                decimal share;
                var rule = ruleC.SelectedIndex;
                if (rule == 1 && byHours.Sum(kv => kv.Value) > 0) share = byHours.GetValueOrDefault(name) / byHours.Sum(kv => kv.Value);
                else if (rule == 2 && bySales.Values.Sum() > 0) share = bySales.GetValueOrDefault(name) / bySales.Values.Sum();
                else share = 1m / participants.Count;
                var amount = Math.Round(totalTips * share, 2);
                var item = new ListViewItem([name, $"{share:P0}", amount.ToString("C")]);
                tipsGrid.Items.Add(item);
            }
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Tip pool", $"{day:dd/MM} {totalTips:C} ({ruleC.Text})");
        };
        tipsHeader.Controls.AddRange([dateP2, ruleC, computeBtn]);
        tipsTab.Controls.AddRange([tipsGrid, tipsHeader]);

        tabs.TabPages.AddRange([scheduleTab, tipsTab]);
        RebuildSchedule();
        panel.Controls.Add(tabs);
        ShowView(panel, "Schedule & tips", ShowSettings);
    }

    private decimal ProductFoodCost(string productName)
    {
        if (!store.Recipes.TryGetValue(productName, out var recipeItems)) return 0;
        decimal cost = 0;
        foreach (var ri in recipeItems)
        {
            var ingr = store.Ingredients.FirstOrDefault(i => i.Name == ri.Ingredient);
            if (ingr != null) cost += ri.Quantity * ingr.Cost;
        }
        return cost;
    }

    private void ShowPurchases()
    {
        if (!store.Features.Purchasing) return;
        if (!HasPermission("Purchasing"))
        {
            MessageBox.Show("You do not have the \"Purchasing\" permission.\nAsk your manager to assign it in Settings → Operators.", "Purchases", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        var tabs = new TabControl { Dock = DockStyle.Fill };

        var poTab = new TabPage { Text = "Purchase orders", BackColor = background, ForeColor = Color.White };
        var poList = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, MultiSelect = false };
        poList.Columns.Add("#", 50); poList.Columns.Add("Supplier", 180); poList.Columns.Add("Created", 130); poList.Columns.Add("Status", 100); poList.Columns.Add("Total", 100); poList.Columns.Add("Lines", 60);
        void RebuildPo()
        {
            poList.Items.Clear();
            foreach (var po in store.PurchaseOrders.OrderByDescending(p => p.Id))
            {
                var item = new ListViewItem([po.Id.ToString(), po.Supplier, po.Created.ToString("dd/MM/yyyy"), po.Status, po.Lines.Sum(l => l.Qty * l.Cost).ToString("C"), po.Lines.Count.ToString()]);
                if (po.Status == "Received") item.ForeColor = Color.FromArgb(130, 200, 150);
                else if (po.Status == "Cancelled") item.ForeColor = Color.FromArgb(150, 160, 175);
                poList.Items.Add(item);
            }
        }
        RebuildPo();

        var poButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 6, 0, 0) };
        Button B(string text, Color back)
        {
            var b = new Button { Text = text, Size = new Size(140, 36), BackColor = back, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 9.5F) };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
        PurchaseOrder? SelectedPo()
        {
            if (poList.SelectedItems.Count == 0) { MessageBox.Show("Select a purchase order first.", "Purchases", MessageBoxButtons.OK, MessageBoxIcon.Information); return null; }
            var id = int.Parse(poList.SelectedItems[0].Text);
            return store.PurchaseOrders.FirstOrDefault(p => p.Id == id);
        }
        var newPoBtn = B("New order", Color.FromArgb(60, 130, 90));
        var receiveBtn = B("Receive", Color.FromArgb(55, 120, 130));
        var cancelPoBtn = B("Cancel order", Color.FromArgb(180, 70, 60));
        newPoBtn.Click += (_, _) =>
        {
            using var f = new Form { Text = "New purchase order", Size = new Size(600, 500), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F), FormBorderStyle = FormBorderStyle.Sizable, MaximizeBox = false, MinimizeBox = false };
            var supLabel = new Label { Text = "Supplier:", AutoSize = true, Location = new Point(20, 18) };
            var supC = new ComboBox { Location = new Point(100, 14), Size = new Size(240, 28), DropDownStyle = ComboBoxStyle.DropDown, BackColor = panelLight, ForeColor = Color.White };
            foreach (var s in store.Suppliers) supC.Items.Add(s.Name);
            if (supC.Items.Count > 0) supC.SelectedIndex = 0;
            var addLineBtn = new Button { Text = "+ Add line", Location = new Point(360, 13), Size = new Size(110, 30), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            addLineBtn.FlatAppearance.BorderSize = 0;
            var grid = new DataGridView { Location = new Point(20, 55), Size = new Size(545, 330), BackgroundColor = panelLight, BorderStyle = BorderStyle.None, ForeColor = Color.White, GridColor = Color.FromArgb(60, 70, 85), AllowUserToAddRows = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            var ingrCol = new DataGridViewComboBoxColumn { HeaderText = "Ingredient", DataSource = store.Ingredients.Select(i => i.Name).ToList(), FlatStyle = FlatStyle.Flat, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
            grid.Columns.AddRange(ingrCol,
                new DataGridViewTextBoxColumn { HeaderText = "Qty", Width = 90 },
                new DataGridViewTextBoxColumn { HeaderText = "Unit cost", Width = 100 });
            grid.CellValueChanged += (_, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex != 0) return;
                var name = grid.Rows[e.RowIndex].Cells[0].Value?.ToString();
                var ingr = store.Ingredients.FirstOrDefault(i => i.Name == name);
                var costCell = grid.Rows[e.RowIndex].Cells[2].Value;
                if (ingr != null && (costCell == null || costCell.ToString() == "0"))
                    grid.Rows[e.RowIndex].Cells[2].Value = ingr.Cost;
            };
            addLineBtn.Click += (_, _) => grid.Rows.Add();
            var ok = new Button { Text = "Save order", Location = new Point(20, 400), Size = new Size(150, 40), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
            var cancel = new Button { Text = "Cancel", Location = new Point(185, 400), Size = new Size(120, 40), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
            ok.Click += (_, _) =>
            {
                var lines = new List<POLine>();
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    var name = row.Cells[0].Value?.ToString();
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (!decimal.TryParse(row.Cells[1].Value?.ToString(), out var qty) || qty <= 0) continue;
                    if (!decimal.TryParse(row.Cells[2].Value?.ToString(), out var cost)) cost = 0;
                    lines.Add(new POLine(name, qty, cost));
                }
                if (lines.Count == 0) { MessageBox.Show("Add at least one line (ingredient, qty).", "Purchase order", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var id = store.PurchaseOrders.Count > 0 ? store.PurchaseOrders.Max(p => p.Id) + 1 : 1;
                store.PurchaseOrders.Add(new PurchaseOrder(id, supC.Text.Trim(), DateTime.Now, "Open", lines));
                store.SavePurchaseOrders(store.PurchaseOrders);
                if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "PO created", $"#{id} {supC.Text} — {lines.Count} lines, {lines.Sum(l => l.Qty * l.Cost):C}");
                f.DialogResult = DialogResult.OK;
            };
            cancel.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
            f.Controls.AddRange([supLabel, supC, addLineBtn, grid, ok, cancel]);
            f.AcceptButton = ok;
            if (f.ShowDialog() == DialogResult.OK) RebuildPo();
        };
        receiveBtn.Click += (_, _) =>
        {
            var po = SelectedPo();
            if (po == null) return;
            if (po.Status != "Open") { MessageBox.Show($"This order is already {po.Status}.", "Purchases", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var detail = string.Join("\n", po.Lines.Select(l => $"• {l.Qty} × {l.Ingredient} @ {l.Cost:C}"));
            if (MessageBox.Show($"Receive order #{po.Id} ({po.Supplier})?\n\n{detail}\n\nStock will be updated and ingredient costs set to these prices.", "Receive order", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (var l in po.Lines)
            {
                var idx = store.Ingredients.FindIndex(i => i.Name == l.Ingredient);
                if (idx >= 0)
                {
                    var ingr = store.Ingredients[idx];
                    store.Ingredients[idx] = ingr with { Stock = ingr.Stock + l.Qty, Cost = l.Cost > 0 ? l.Cost : ingr.Cost };
                }
                else store.Ingredients.Add(new Ingredient(l.Ingredient, "unit", l.Qty, 0, l.Cost));
            }
            store.SaveIngredients(store.Ingredients);
            Auto86Sync();
            var poIdx = store.PurchaseOrders.FindIndex(p => p.Id == po.Id);
            store.PurchaseOrders[poIdx] = store.PurchaseOrders[poIdx] with { Status = "Received" };
            store.SavePurchaseOrders(store.PurchaseOrders);
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "PO received", $"#{po.Id} — stock updated ({po.Lines.Count} lines)");
            MessageBox.Show($"Order #{po.Id} received.\nStock updated for {po.Lines.Count} ingredient(s).", "Purchases", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RebuildPo();
        };
        cancelPoBtn.Click += (_, _) =>
        {
            var po = SelectedPo();
            if (po == null) return;
            if (po.Status != "Open") { MessageBox.Show($"This order is already {po.Status}.", "Purchases", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show($"Cancel order #{po.Id}?", "Purchases", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var poIdx = store.PurchaseOrders.FindIndex(p => p.Id == po.Id);
            store.PurchaseOrders[poIdx] = store.PurchaseOrders[poIdx] with { Status = "Cancelled" };
            store.SavePurchaseOrders(store.PurchaseOrders);
            RebuildPo();
        };
        poButtons.Controls.AddRange([newPoBtn, receiveBtn, cancelPoBtn]);

        var reorderBtn = B("Reorder low", Color.FromArgb(140, 110, 50));
        var emailPoBtn = B("Email PO (PDF)", Color.FromArgb(70, 90, 130));
        reorderBtn.Click += (_, _) =>
        {
            var low = store.Ingredients.Where(i => i.MinStock > 0 && i.Stock <= i.MinStock).ToList();
            if (low.Count == 0)
            {
                MessageBox.Show("No ingredients are at or below their reorder point.", "Reorder", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var f = new Form { Text = "Reorder low stock", Size = new Size(600, 470), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var supLabel = new Label { Text = "Supplier:", AutoSize = true, Location = new Point(20, 18) };
            var supC = new ComboBox { Location = new Point(100, 14), Size = new Size(260, 28), DropDownStyle = ComboBoxStyle.DropDown, BackColor = panelLight, ForeColor = Color.White };
            foreach (var s in store.Suppliers) supC.Items.Add(s.Name);
            if (supC.Items.Count > 0) supC.SelectedIndex = 0;
            var grid = new DataGridView { Location = new Point(20, 55), Size = new Size(545, 320), BackgroundColor = panelLight, BorderStyle = BorderStyle.None, ForeColor = Color.White, GridColor = Color.FromArgb(60, 70, 85), AllowUserToAddRows = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            grid.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "Ingredient", ReadOnly = true },
                new DataGridViewTextBoxColumn { HeaderText = "Suggested qty", Width = 110 },
                new DataGridViewTextBoxColumn { HeaderText = "Unit cost", Width = 90 });
            foreach (var i in low)
                grid.Rows.Add(i.Name, Math.Max(i.MinStock * 2 - i.Stock, Math.Ceiling(i.MinStock)), i.Cost);
            var ok = new Button { Text = "Create PO", Location = new Point(20, 390), Size = new Size(150, 40), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
            var cancel = new Button { Text = "Cancel", Location = new Point(185, 390), Size = new Size(120, 40), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
            ok.Click += (_, _) =>
            {
                var lines = new List<POLine>();
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    var name = row.Cells[0].Value?.ToString();
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (!decimal.TryParse(row.Cells[1].Value?.ToString(), out var qty) || qty <= 0) continue;
                    if (!decimal.TryParse(row.Cells[2].Value?.ToString(), out var cost)) cost = 0;
                    lines.Add(new POLine(name, qty, cost));
                }
                if (lines.Count == 0) { MessageBox.Show("Add at least one line.", "Reorder", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var id = store.PurchaseOrders.Count > 0 ? store.PurchaseOrders.Max(p => p.Id) + 1 : 1;
                store.PurchaseOrders.Add(new PurchaseOrder(id, supC.Text.Trim(), DateTime.Now, "Open", lines));
                store.SavePurchaseOrders(store.PurchaseOrders);
                if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Reorder PO", $"#{id} {supC.Text} — {lines.Count} low-stock lines");
                f.DialogResult = DialogResult.OK;
            };
            cancel.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
            f.Controls.AddRange([supLabel, supC, grid, ok, cancel]);
            f.AcceptButton = ok;
            if (f.ShowDialog() == DialogResult.OK) RebuildPo();
        };
        emailPoBtn.Click += (_, _) =>
        {
            var po = SelectedPo();
            if (po == null) return;
            var sup = store.Suppliers.FirstOrDefault(s => s.Name == po.Supplier);
            var reportLines = new List<ReportLine>
            {
                new($"PURCHASE ORDER #{po.Id}", ReportLineStyles.Title),
                new($"Supplier: {po.Supplier}{(sup != null && sup.Email.Length > 0 ? $"  <{sup.Email}>" : "")}", ReportLineStyles.Header),
                new($"Created: {po.Created:dd/MM/yyyy HH:mm}    Status: {po.Status}", ReportLineStyles.Small),
                new("", ReportLineStyles.Rule),
            };
            foreach (var l in po.Lines)
                reportLines.Add(new($"{l.Qty,-10:0.##} {l.Ingredient,-30} {l.Cost,10:C}  = {l.Qty * l.Cost,10:C}", ReportLineStyles.Normal));
            reportLines.Add(new("", ReportLineStyles.Rule));
            reportLines.Add(new($"TOTAL: {po.Lines.Sum(l => l.Qty * l.Cost):C}", ReportLineStyles.Header));
            var tmpPdf = Path.Combine(Path.GetTempPath(), $"po-{po.Id}-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            try { TicketPdf.Save(tmpPdf, reportLines); }
            catch (Exception ex) { MessageBox.Show($"Could not build PDF: {ex.Message}", "Email PO", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (store.EmailSettings.Host.Length == 0 || sup == null || string.IsNullOrWhiteSpace(sup.Email))
            {
                using var save = new SaveFileDialog { Filter = "PDF file|*.pdf", FileName = $"PO-{po.Id}.pdf" };
                if (save.ShowDialog() == DialogResult.OK)
                {
                    File.Copy(tmpPdf, save.FileName, true);
                    MessageBox.Show($"PO #{po.Id} saved as PDF:\n{save.FileName}\n\n(Set SMTP in Settings → Receipts and a supplier email to send directly.)", "Email PO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }
            if (SendMailWithAttachment(sup.Email, $"Purchase order #{po.Id} — {po.Supplier}", $"Please find attached purchase order #{po.Id} created {po.Created:dd/MM/yyyy}.", tmpPdf))
                MessageBox.Show($"PO #{po.Id} emailed to {sup.Email}.", "Email PO", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else MessageBox.Show("Sending failed — check SMTP settings in Settings → Receipts.", "Email PO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        };
        poButtons.Controls.AddRange([reorderBtn, emailPoBtn]);
        poTab.Controls.AddRange([poList, poButtons]);

        var supTab = new TabPage { Text = "Suppliers", BackColor = background, ForeColor = Color.White };
        var supGrid = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, MultiSelect = false };
        supGrid.Columns.Add("Name", 200); supGrid.Columns.Add("Phone", 160); supGrid.Columns.Add("Email", 240);
        void RebuildSup()
        {
            supGrid.Items.Clear();
            foreach (var s in store.Suppliers) supGrid.Items.Add(new ListViewItem([s.Name, s.Phone, s.Email]));
        }
        RebuildSup();
        var supButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 6, 0, 0) };
        var addSupBtn = B("Add supplier", Color.FromArgb(60, 130, 90));
        var delSupBtn = B("Delete", Color.FromArgb(180, 70, 60));
        addSupBtn.Click += (_, _) =>
        {
            using var f = new Form { Text = "Add supplier", Size = new Size(380, 260), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var n = new TextBox { Location = new Point(120, 20), Size = new Size(220, 28), BackColor = panelLight, ForeColor = Color.White };
            var p = new TextBox { Location = new Point(120, 58), Size = new Size(220, 28), BackColor = panelLight, ForeColor = Color.White };
            var e2 = new TextBox { Location = new Point(120, 96), Size = new Size(220, 28), BackColor = panelLight, ForeColor = Color.White };
            f.Controls.AddRange([
                new Label { Text = "Name:", AutoSize = true, Location = new Point(20, 24) }, n,
                new Label { Text = "Phone:", AutoSize = true, Location = new Point(20, 62) }, p,
                new Label { Text = "Email:", AutoSize = true, Location = new Point(20, 100) }, e2
            ]);
            var ok = new Button { Text = "Save", Location = new Point(120, 145), Size = new Size(110, 36), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "Cancel", Location = new Point(240, 145), Size = new Size(100, 36), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
            ok.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(n.Text)) { MessageBox.Show("Enter the supplier name.", "Suppliers", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                store.Suppliers.Add(new Supplier(n.Text.Trim(), p.Text.Trim(), e2.Text.Trim()));
                store.SaveSuppliers(store.Suppliers);
                f.DialogResult = DialogResult.OK;
            };
            cancel.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
            f.Controls.AddRange([ok, cancel]);
            f.AcceptButton = ok;
            if (f.ShowDialog() == DialogResult.OK) RebuildSup();
        };
        delSupBtn.Click += (_, _) =>
        {
            if (supGrid.SelectedItems.Count == 0) return;
            var name = supGrid.SelectedItems[0].Text;
            if (MessageBox.Show($"Delete supplier {name}?", "Suppliers", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            store.Suppliers.RemoveAll(s => s.Name == name);
            store.SaveSuppliers(store.Suppliers);
            RebuildSup();
        };
        supButtons.Controls.AddRange([addSupBtn, delSupBtn]);
        supTab.Controls.AddRange([supGrid, supButtons]);

        var profitTab = new TabPage { Text = "Profitability", BackColor = background, ForeColor = Color.White };
        var profitHeader = new Panel { Dock = DockStyle.Top, Height = 44 };
        var periodC = new ComboBox { Location = new Point(6, 7), Size = new Size(160, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
        periodC.Items.AddRange(["Today", "Last 7 days", "Last 30 days", "All time"]); periodC.SelectedIndex = 1;
        var profitSummary = new Label { AutoSize = true, Location = new Point(340, 13), ForeColor = mint, Font = new Font("Segoe UI Semibold", 10F) };
        var refreshBtn = new Button { Text = "Refresh", Location = new Point(176, 7), Size = new Size(110, 30), BackColor = Color.FromArgb(60, 130, 90), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        refreshBtn.FlatAppearance.BorderSize = 0;
        var profitGrid = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true };
        profitGrid.Columns.Add("Product", 220); profitGrid.Columns.Add("Sold", 70); profitGrid.Columns.Add("Revenue", 110); profitGrid.Columns.Add("Food cost", 110); profitGrid.Columns.Add("Margin", 110); profitGrid.Columns.Add("Food %", 80);
        void RebuildProfit()
        {
            profitGrid.Items.Clear();
            var from = periodC.SelectedIndex switch { 0 => DateTime.Today, 1 => DateTime.Today.AddDays(-6), 2 => DateTime.Today.AddDays(-29), _ => DateTime.MinValue };
            var sales = store.Sales.Where(s => !s.IsRefund && s.Time >= from).ToList();
            var lines = sales.SelectMany(s => s.Items ?? []).ToList();
            decimal revenue = 0, food = 0;
            foreach (var g in lines.GroupBy(l => l.Product).OrderByDescending(g => g.Sum(l => l.UnitPrice * l.Quantity)))
            {
                var rev = g.Sum(l => l.UnitPrice * l.Quantity);
                var unitCost = ProductFoodCost(g.Key);
                var cost = unitCost * g.Sum(l => l.Quantity);
                revenue += rev; food += cost;
                var pct = rev > 0 ? cost / rev : 0;
                var item = new ListViewItem([g.Key, g.Sum(l => l.Quantity).ToString(), rev.ToString("C"), cost.ToString("C"), (rev - cost).ToString("C"), $"{pct:P0}"]);
                if (pct > 0.40m) item.ForeColor = Color.FromArgb(245, 140, 90);
                profitGrid.Items.Add(item);
            }
            profitSummary.Text = revenue > 0 ? $"Revenue {revenue:C}  •  food cost {food:C} ({food / revenue:P0})  •  margin {(revenue - food):C}" : "No sales in period";
        }
        periodC.SelectedIndexChanged += (_, _) => RebuildProfit();
        refreshBtn.Click += (_, _) => RebuildProfit();
        profitHeader.Controls.AddRange([periodC, refreshBtn, profitSummary]);
        profitTab.Controls.AddRange([profitGrid, profitHeader]);
        RebuildProfit();

        tabs.TabPages.AddRange([poTab, supTab, profitTab]);

        var lowIngs = store.Ingredients.Where(i => i.MinStock > 0 && i.Stock <= i.MinStock).ToList();
        if (lowIngs.Count > 0)
        {
            var lowBanner = new Label { Dock = DockStyle.Top, Height = 30, Text = $"⚠ {lowIngs.Count} ingredient(s) at/below reorder point: " + string.Join(", ", lowIngs.Select(i => $"{i.Name} ({i.Stock:0.#}/{i.MinStock:0.#})")) + "   → use Reorder low", ForeColor = Color.FromArgb(245, 180, 80), BackColor = Color.FromArgb(50, 42, 26), Padding = new Padding(8, 7, 0, 0) };
            poTab.Controls.Add(lowBanner);
        }

        if (store.Features.Stocktakes)
        {
            var stTab = new TabPage { Text = "Stocktake", BackColor = background, ForeColor = Color.White };
            var stList = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, MultiSelect = false };
            stList.Columns.Add("#", 50); stList.Columns.Add("Date", 130); stList.Columns.Add("By", 110); stList.Columns.Add("Note", 200); stList.Columns.Add("Lines", 70); stList.Columns.Add("Net variance", 110);
            void RebuildSt()
            {
                stList.Items.Clear();
                foreach (var st in store.StockTakes.OrderByDescending(s => s.Id))
                {
                    var variance = st.Lines.Sum(l => l.CountedQty - l.SystemQty);
                    var item = new ListViewItem([st.Id.ToString(), st.When.ToString("dd/MM/yyyy HH:mm"), st.By, st.Note, st.Lines.Count.ToString(), variance.ToString("+0.##;-0.##;0")]);
                    if (variance != 0) item.ForeColor = Color.FromArgb(245, 180, 80);
                    stList.Items.Add(item);
                }
            }
            RebuildSt();
            var stButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 6, 0, 0) };
            var newCountBtn = new Button { Text = "New count", Size = new Size(140, 36), BackColor = Color.FromArgb(60, 130, 90), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 9.5F) };
            newCountBtn.FlatAppearance.BorderSize = 0;
            newCountBtn.Click += (_, _) =>
            {
                if (store.Ingredients.Count == 0) { MessageBox.Show("No ingredients yet — add them in Stock management first.", "Stocktake", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                using var f = new Form { Text = "New stock count", Size = new Size(620, 520), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
                var hint = new Label { Text = "Enter the counted quantity for each ingredient (system values are pre-filled).", AutoSize = true, Location = new Point(20, 14), ForeColor = Color.FromArgb(170, 180, 195) };
                var noteBox = new TextBox { Location = new Point(20, 40), Size = new Size(380, 28), BackColor = panelLight, ForeColor = Color.White, PlaceholderText = "Note (optional, e.g. 'Friday count')" };
                var grid = new DataGridView { Location = new Point(20, 78), Size = new Size(565, 340), BackgroundColor = panelLight, BorderStyle = BorderStyle.None, ForeColor = Color.White, GridColor = Color.FromArgb(60, 70, 85), AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
                grid.Columns.AddRange(
                    new DataGridViewTextBoxColumn { HeaderText = "Ingredient", ReadOnly = true },
                    new DataGridViewTextBoxColumn { HeaderText = "System", ReadOnly = true, Width = 80 },
                    new DataGridViewTextBoxColumn { HeaderText = "Counted", Width = 90 });
                foreach (var i in store.Ingredients) grid.Rows.Add(i.Name, i.Stock.ToString("0.##"), i.Stock.ToString("0.##"));
                var okBtn = new Button { Text = "Save count", Location = new Point(20, 430), Size = new Size(150, 40), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
                var cancelBtn2 = new Button { Text = "Cancel", Location = new Point(185, 430), Size = new Size(120, 40), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                okBtn.FlatAppearance.BorderSize = 0; cancelBtn2.FlatAppearance.BorderSize = 0;
                okBtn.Click += (_, _) =>
                {
                    var stLines = new List<StockTakeLine>();
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        var name = row.Cells[0].Value?.ToString();
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        var idx = store.Ingredients.FindIndex(x => x.Name == name);
                        if (idx < 0) continue;
                        if (!decimal.TryParse(row.Cells[2].Value?.ToString(), out var counted)) counted = store.Ingredients[idx].Stock;
                        if (counted == store.Ingredients[idx].Stock) continue;
                        stLines.Add(new StockTakeLine(name, store.Ingredients[idx].Stock, counted));
                        store.Ingredients[idx] = store.Ingredients[idx] with { Stock = counted };
                    }
                    if (stLines.Count == 0) { MessageBox.Show("No differences to save — all counts match system stock.", "Stocktake", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                    store.SaveIngredients(store.Ingredients);
                    var id = store.StockTakes.Count > 0 ? store.StockTakes.Max(s => s.Id) + 1 : 1;
                    store.StockTakes.Add(new StockTake(id, DateTime.Now, currentOperator.Name, noteBox.Text.Trim(), stLines));
                    store.SaveStockTakes(store.StockTakes);
                    Auto86Sync();
                    if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Stocktake", $"#{id} — {stLines.Count} adjustments, net {(stLines.Sum(l => l.CountedQty - l.SystemQty)):0.##}");
                    f.DialogResult = DialogResult.OK;
                };
                cancelBtn2.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
                f.Controls.AddRange([hint, noteBox, grid, okBtn, cancelBtn2]);
                f.AcceptButton = okBtn;
                if (f.ShowDialog() == DialogResult.OK) RebuildSt();
            };
            stButtons.Controls.Add(newCountBtn);
            stTab.Controls.AddRange([stList, stButtons]);
            tabs.TabPages.Add(stTab);
        }

        if (store.Features.WasteTracking)
        {
            var wasteTab = new TabPage { Text = "Waste & expiry", BackColor = background, ForeColor = Color.White };
            var wasteList = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, MultiSelect = false };
            wasteList.Columns.Add("When", 130); wasteList.Columns.Add("Ingredient", 160); wasteList.Columns.Add("Qty", 80); wasteList.Columns.Add("Reason", 220); wasteList.Columns.Add("By", 110);
            void RebuildWaste()
            {
                wasteList.Items.Clear();
                foreach (var w in store.WasteLog.OrderByDescending(w => w.When))
                    wasteList.Items.Add(new ListViewItem([w.When.ToString("dd/MM/yyyy HH:mm"), w.Ingredient, w.Qty.ToString("0.##"), w.Reason, w.By]));
            }
            RebuildWaste();
            var wasteButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 6, 0, 0) };
            var logWasteBtn = new Button { Text = "Log waste", Size = new Size(130, 36), BackColor = Color.FromArgb(180, 70, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 9.5F) };
            logWasteBtn.FlatAppearance.BorderSize = 0;
            var setExpiryBtn = new Button { Text = "Set expiry date", Size = new Size(150, 36), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 9.5F) };
            setExpiryBtn.FlatAppearance.BorderSize = 0;
            logWasteBtn.Click += (_, _) =>
            {
                if (store.Ingredients.Count == 0) { MessageBox.Show("No ingredients yet.", "Waste", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                using var f = new Form { Text = "Log waste", Size = new Size(360, 280), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
                var ingrC = new ComboBox { Location = new Point(20, 34), Size = new Size(240, 28), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
                foreach (var i in store.Ingredients) ingrC.Items.Add($"{i.Name} (stock {i.Stock:0.##} {i.Unit})");
                if (ingrC.Items.Count > 0) ingrC.SelectedIndex = 0;
                var qty = new NumericUpDown { Location = new Point(20, 74), Size = new Size(110, 28), Minimum = 0.01m, Maximum = 100000, DecimalPlaces = 2, Increment = 0.1m, BackColor = panelLight, ForeColor = Color.White };
                var reason = new TextBox { Location = new Point(20, 114), Size = new Size(240, 28), BackColor = panelLight, ForeColor = Color.White, PlaceholderText = "Reason (spoiled, dropped…)" };
                var okBtn = new Button { Text = "Log it", Location = new Point(20, 156), Size = new Size(100, 32), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
                okBtn.FlatAppearance.BorderSize = 0;
                var cancelBtn3 = new Button { Text = "Cancel", Location = new Point(130, 156), Size = new Size(100, 32), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                cancelBtn3.FlatAppearance.BorderSize = 0;
                okBtn.Click += (_, _) =>
                {
                    var idx = ingrC.SelectedIndex;
                    if (idx < 0) return;
                    var ingr = store.Ingredients[idx];
                    if (qty.Value > ingr.Stock) { MessageBox.Show($"Only {ingr.Stock:0.##} {ingr.Unit} in stock.", "Waste", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    store.Ingredients[idx] = ingr with { Stock = ingr.Stock - qty.Value };
                    store.SaveIngredients(store.Ingredients);
                    store.WasteLog.Add(new WasteEntry(ingr.Name, qty.Value, string.IsNullOrWhiteSpace(reason.Text) ? "Waste" : reason.Text.Trim(), DateTime.Now, currentOperator.Name));
                    store.SaveWasteLog(store.WasteLog);
                    Auto86Sync();
                    if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Waste", $"{ingr.Name} -{qty.Value:0.##} ({reason.Text})");
                    f.DialogResult = DialogResult.OK;
                };
                cancelBtn3.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
                f.Controls.AddRange([new Label { Text = "Ingredient", AutoSize = true, Location = new Point(20, 12) }, ingrC, new Label { Text = "Quantity", AutoSize = true, Location = new Point(20, 58) }, qty, new Label { Text = "Reason", AutoSize = true, Location = new Point(20, 98) }, reason, okBtn, cancelBtn3]);
                f.AcceptButton = okBtn;
                if (f.ShowDialog() == DialogResult.OK) RebuildWaste();
            };
            setExpiryBtn.Click += (_, _) =>
            {
                if (store.Ingredients.Count == 0) { MessageBox.Show("No ingredients yet.", "Expiry", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                using var f = new Form { Text = "Set expiry date", Size = new Size(360, 230), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
                var ingrC = new ComboBox { Location = new Point(20, 34), Size = new Size(240, 28), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
                foreach (var i in store.Ingredients) ingrC.Items.Add(i.Name);
                if (ingrC.Items.Count > 0) ingrC.SelectedIndex = 0;
                var date = new DateTimePicker { Location = new Point(20, 74), Size = new Size(240, 28), Format = DateTimePickerFormat.Short, BackColor = panelLight, ForeColor = Color.White };
                var okBtn = new Button { Text = "Save", Location = new Point(20, 116), Size = new Size(100, 32), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
                okBtn.FlatAppearance.BorderSize = 0;
                okBtn.Click += (_, _) =>
                {
                    var idx = ingrC.SelectedIndex;
                    if (idx < 0) return;
                    store.Ingredients[idx] = store.Ingredients[idx] with { Expiry = date.Value.Date };
                    store.SaveIngredients(store.Ingredients);
                    if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Expiry set", $"{store.Ingredients[idx].Name} → {date.Value:dd/MM/yyyy}");
                    f.DialogResult = DialogResult.OK;
                };
                f.Controls.AddRange([new Label { Text = "Ingredient", AutoSize = true, Location = new Point(20, 12) }, ingrC, new Label { Text = "Best before", AutoSize = true, Location = new Point(20, 58) }, date, okBtn]);
                f.AcceptButton = okBtn;
                f.ShowDialog();
            };
            wasteButtons.Controls.AddRange([logWasteBtn, setExpiryBtn]);
            wasteTab.Controls.AddRange([wasteList, wasteButtons]);
            tabs.TabPages.Add(wasteTab);
        }

        if (store.Features.StockTransfers)
        {
            var trTab = new TabPage { Text = "Transfers", BackColor = background, ForeColor = Color.White };
            var trList = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, MultiSelect = false };
            trList.Columns.Add("#", 50); trList.Columns.Add("When", 130); trList.Columns.Add("From", 130); trList.Columns.Add("To", 130); trList.Columns.Add("Ingredient", 150); trList.Columns.Add("Qty", 70); trList.Columns.Add("By", 100);
            void RebuildTr()
            {
                trList.Items.Clear();
                foreach (var t in store.StockTransfers.OrderByDescending(t => t.Id))
                    trList.Items.Add(new ListViewItem([t.Id.ToString(), t.When.ToString("dd/MM/yyyy HH:mm"), t.From, t.To, t.Ingredient, t.Qty.ToString("0.##"), t.By]));
            }
            RebuildTr();
            var trButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 6, 0, 0) };
            var newTrBtn = new Button { Text = "New transfer", Size = new Size(140, 36), BackColor = Color.FromArgb(55, 120, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 9.5F) };
            newTrBtn.FlatAppearance.BorderSize = 0;
            var locBtn = new Button { Text = "Manage locations", Size = new Size(160, 36), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 9.5F) };
            locBtn.FlatAppearance.BorderSize = 0;
            string[] Locations() => ["Main", .. store.Locations];
            locBtn.Click += (_, _) =>
            {
                using var f = new Form { Text = "Locations", Size = new Size(340, 340), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
                var lb = new ListBox { Location = new Point(20, 34), Size = new Size(200, 180), BackColor = panelLight, ForeColor = Color.White };
                void FillLb() { lb.Items.Clear(); foreach (var l in Locations()) lb.Items.Add(l); }
                FillLb();
                var newLoc = new TextBox { Location = new Point(20, 224), Size = new Size(150, 28), BackColor = panelLight, ForeColor = Color.White, PlaceholderText = "New location" };
                var addB = new Button { Text = "+", Location = new Point(178, 224), Size = new Size(42, 28), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
                var delB = new Button { Text = "Remove", Location = new Point(20, 258), Size = new Size(90, 28), BackColor = Color.FromArgb(180, 70, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                addB.FlatAppearance.BorderSize = 0; delB.FlatAppearance.BorderSize = 0;
                addB.Click += (_, _) =>
                {
                    var name = newLoc.Text.Trim();
                    if (name.Length == 0 || name == "Main" || store.Locations.Contains(name)) return;
                    store.Locations.Add(name); store.SaveLocations(store.Locations); newLoc.Text = ""; FillLb();
                };
                delB.Click += (_, _) =>
                {
                    if (lb.SelectedItem is not string s || s == "Main") return;
                    store.Locations.Remove(s); store.SaveLocations(store.Locations); FillLb();
                };
                f.Controls.AddRange([lb, newLoc, addB, delB]);
                f.ShowDialog();
            };
            newTrBtn.Click += (_, _) =>
            {
                if (store.Ingredients.Count == 0) { MessageBox.Show("No ingredients yet.", "Transfer", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                using var f = new Form { Text = "New stock transfer", Size = new Size(400, 330), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
                var fromC = new ComboBox { Location = new Point(20, 34), Size = new Size(150, 28), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
                var toC = new ComboBox { Location = new Point(200, 34), Size = new Size(150, 28), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
                foreach (var l in Locations()) { fromC.Items.Add(l); toC.Items.Add(l); }
                if (fromC.Items.Count > 0) { fromC.SelectedIndex = 0; toC.SelectedIndex = Math.Min(1, toC.Items.Count - 1); }
                var ingrC = new ComboBox { Location = new Point(20, 90), Size = new Size(240, 28), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
                foreach (var i in store.Ingredients) ingrC.Items.Add($"{i.Name} (main {i.Stock:0.##} {i.Unit})");
                if (ingrC.Items.Count > 0) ingrC.SelectedIndex = 0;
                var qty = new NumericUpDown { Location = new Point(20, 146), Size = new Size(110, 28), Minimum = 0.01m, Maximum = 100000, DecimalPlaces = 2, Increment = 0.1m, BackColor = panelLight, ForeColor = Color.White };
                var okBtn = new Button { Text = "Transfer", Location = new Point(20, 196), Size = new Size(120, 34), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F) };
                var cancelBtn4 = new Button { Text = "Cancel", Location = new Point(150, 196), Size = new Size(100, 34), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                okBtn.FlatAppearance.BorderSize = 0; cancelBtn4.FlatAppearance.BorderSize = 0;
                okBtn.Click += (_, _) =>
                {
                    var from = fromC.SelectedItem?.ToString() ?? "Main";
                    var to = toC.SelectedItem?.ToString() ?? "Main";
                    if (from == to) { MessageBox.Show("From and To must differ.", "Transfer", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    var idx = ingrC.SelectedIndex;
                    if (idx < 0) return;
                    var ingr = store.Ingredients[idx];
                    void AddLoc(string loc, string ing, decimal amount)
                    {
                        if (!store.LocationStock.ContainsKey(loc)) store.LocationStock[loc] = [];
                        store.LocationStock[loc].TryGetValue(ing, out var cur);
                        store.LocationStock[loc][ing] = cur + amount;
                    }
                    decimal GetLoc(string loc, string ing) => store.LocationStock.TryGetValue(loc, out var d) && d.TryGetValue(ing, out var v) ? v : 0;
                    if (from == "Main")
                    {
                        if (qty.Value > ingr.Stock) { MessageBox.Show($"Only {ingr.Stock:0.##} {ingr.Unit} in main stock.", "Transfer", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                        store.Ingredients[idx] = ingr with { Stock = ingr.Stock - qty.Value };
                        store.SaveIngredients(store.Ingredients);
                        AddLoc(to, ingr.Name, qty.Value);
                    }
                    else if (to == "Main")
                    {
                        if (qty.Value > GetLoc(from, ingr.Name)) { MessageBox.Show($"Only {GetLoc(from, ingr.Name):0.##} {ingr.Unit} at {from}.", "Transfer", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                        AddLoc(from, ingr.Name, -qty.Value);
                        store.Ingredients[idx] = ingr with { Stock = ingr.Stock + qty.Value };
                        store.SaveIngredients(store.Ingredients);
                    }
                    else
                    {
                        if (qty.Value > GetLoc(from, ingr.Name)) { MessageBox.Show($"Only {GetLoc(from, ingr.Name):0.##} {ingr.Unit} at {from}.", "Transfer", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                        AddLoc(from, ingr.Name, -qty.Value);
                        AddLoc(to, ingr.Name, qty.Value);
                    }
                    store.SaveLocationStock();
                    var id = store.StockTransfers.Count > 0 ? store.StockTransfers.Max(t => t.Id) + 1 : 1;
                    store.StockTransfers.Add(new StockTransfer(id, from, to, ingr.Name, qty.Value, DateTime.Now, currentOperator.Name));
                    store.SaveStockTransfers(store.StockTransfers);
                    Auto86Sync();
                    if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Transfer", $"#{id} {qty.Value:0.##} {ingr.Name}: {from} → {to}");
                    f.DialogResult = DialogResult.OK;
                };
                cancelBtn4.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
                f.Controls.AddRange([new Label { Text = "From", AutoSize = true, Location = new Point(20, 12) }, fromC, new Label { Text = "To", AutoSize = true, Location = new Point(200, 12) }, toC, new Label { Text = "Ingredient", AutoSize = true, Location = new Point(20, 70) }, ingrC, new Label { Text = "Quantity", AutoSize = true, Location = new Point(20, 126) }, qty, okBtn, cancelBtn4]);
                f.AcceptButton = okBtn;
                if (f.ShowDialog() == DialogResult.OK) RebuildTr();
            };
            trButtons.Controls.AddRange([newTrBtn, locBtn]);
            trTab.Controls.AddRange([trList, trButtons]);
            tabs.TabPages.Add(trTab);
        }

        {
            var fcTab = new TabPage { Text = "Forecast", BackColor = background, ForeColor = Color.White };
            var fcList = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, MultiSelect = false };
            fcList.Columns.Add("Product", 200); fcList.Columns.Add("Sold (30d)", 90); fcList.Columns.Add("Revenue (30d)", 120); fcList.Columns.Add("Last sale", 120); fcList.Columns.Add("Status", 90); fcList.Columns.Add("Suggestion", 300);
            var cutoff = DateTime.Now.AddDays(-30);
            var stats = new List<(string Name, int Qty, decimal Rev, DateTime? Last)>();
            foreach (var p in store.Products)
            {
                var linesForProduct = store.Sales.Where(s => !s.IsRefund && s.Time >= cutoff && s.Items != null).SelectMany(s => s.Items!).Where(l => l.Product == p.Name).ToList();
                var lastSale = store.Sales.Where(s => !s.IsRefund && s.Items != null && s.Items!.Any(l => l.Product == p.Name)).Select(s => (DateTime?)s.Time).DefaultIfEmpty(null).Max();
                stats.Add((p.Name, linesForProduct.Sum(l => l.Quantity), linesForProduct.Sum(l => l.Quantity * l.UnitPrice), lastSale));
            }
            foreach (var (name, qty, rev, last) in stats.OrderByDescending(s => s.Qty))
            {
                var status = qty == 0 ? "DEAD" : qty <= 5 ? "Slow" : qty >= 21 ? "Hot" : "OK";
                var suggestion = status switch
                {
                    "DEAD" => "No sales in 30 days — consider delisting or promotion",
                    "Slow" => "Low turnover — keep stock minimal",
                    "Hot" => $"Top mover ({qty / 30m:0.0}/day) — keep 7 days of stock ready",
                    _ => ""
                };
                var item = new ListViewItem([name, qty.ToString(), rev.ToString("C"), last?.ToString("dd/MM/yyyy") ?? "—", status, suggestion]);
                if (status == "DEAD") item.ForeColor = Color.FromArgb(235, 90, 90);
                else if (status == "Hot") item.ForeColor = Color.FromArgb(110, 210, 145);
                else if (status == "Slow") item.ForeColor = Color.FromArgb(245, 180, 80);
                fcList.Items.Add(item);
            }
            fcTab.Controls.Add(fcList);
            tabs.TabPages.Add(fcTab);
        }
        panel.Controls.Add(tabs);
        ShowView(panel, "Purchases & food cost", ShowSettings);
    }

    private void ShowEmailSettings()
    {
        if (!HasPermission("Settings")) return;
        var es = store.EmailSettings;
        using var f = new Form { Text = "Email / SMTP settings", Size = new Size(440, 400), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var host = new TextBox { Location = new Point(150, 20), Size = new Size(250, 28), BackColor = panelLight, ForeColor = Color.White, Text = es.Host };
        var port = new NumericUpDown { Location = new Point(150, 58), Size = new Size(100, 28), BackColor = panelLight, ForeColor = Color.White, Minimum = 1, Maximum = 65535, Value = Math.Clamp(es.Port, 1, 65535) };
        var user = new TextBox { Location = new Point(150, 96), Size = new Size(250, 28), BackColor = panelLight, ForeColor = Color.White, Text = es.User };
        var pass = new TextBox { Location = new Point(150, 134), Size = new Size(250, 28), BackColor = panelLight, ForeColor = Color.White, Text = es.Password, UseSystemPasswordChar = true };
        var from = new TextBox { Location = new Point(150, 172), Size = new Size(250, 28), BackColor = panelLight, ForeColor = Color.White, Text = es.From };
        var tls = new CheckBox { Text = "Use TLS (recommended)", AutoSize = true, ForeColor = Color.White, Location = new Point(150, 210), Checked = es.Tls };
        void Row(string t, Control c, int y) { f.Controls.Add(new Label { Text = t, AutoSize = true, Location = new Point(20, y + 4) }); c.Location = new Point(150, y); f.Controls.Add(c); }
        Row("SMTP host:", host, 20); Row("Port:", port, 58); Row("Username:", user, 96); Row("Password:", pass, 134); Row("From address:", from, 172);
        f.Controls.Add(tls);
        var hint = new Label { Text = "Receipts are emailed automatically to customers with an email address\nwhen Email receipts is enabled and a payment is accepted.", AutoSize = true, ForeColor = Color.FromArgb(143, 154, 171), Location = new Point(20, 245) };
        var saveBtn = new Button { Text = "Save", Location = new Point(20, 300), Size = new Size(120, 38), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        var cancelBtn = new Button { Text = "Cancel", Location = new Point(150, 300), Size = new Size(120, 38), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        saveBtn.FlatAppearance.BorderSize = 0; cancelBtn.FlatAppearance.BorderSize = 0;
        saveBtn.Click += (_, _) =>
        {
            store.SaveEmailSettings(new EmailSettings(host.Text.Trim(), (int)port.Value, user.Text.Trim(), pass.Text, from.Text.Trim(), tls.Checked));
            if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Email settings", $"{host.Text.Trim()}:{port.Value} from {from.Text.Trim()}");
            f.DialogResult = DialogResult.OK;
        };
        cancelBtn.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
        f.Controls.AddRange([hint, saveBtn, cancelBtn]);
        f.AcceptButton = saveBtn;
        f.ShowDialog();
    }

    private bool SendMailWithAttachment(string to, string subject, string body, string attachmentPath)
    {
        var es = store.EmailSettings;
        if (es.Host.Length == 0) return false;
        try
        {
            using var msg = new System.Net.Mail.MailMessage(es.From, to, subject, body);
            if (File.Exists(attachmentPath)) msg.Attachments.Add(new System.Net.Mail.Attachment(attachmentPath));
            using var client = new System.Net.Mail.SmtpClient(es.Host, es.Port) { EnableSsl = es.Tls };
            if (es.User.Length > 0) client.Credentials = new System.Net.NetworkCredential(es.User, es.Password);
            client.Send(msg);
            return true;
        }
        catch (Exception ex)
        {
            if (store.Features.AuditLog) store.LogAudit("System", "Email failed", ex.Message);
            return false;
        }
    }

    private void TrySendEmailReceipt(string customerName, string paymentType, List<SoldLine> lines, decimal finalTotal, decimal discount, decimal tip, int paymentNum, string orderType, decimal serviceFee = 0, decimal cashDisc = 0, decimal surcharge = 0)
    {
        if (!store.Features.EmailReceipts) return;
        var es = store.EmailSettings;
        if (string.IsNullOrWhiteSpace(es.Host) || string.IsNullOrWhiteSpace(es.From)) return;
        if (string.IsNullOrWhiteSpace(customerName)) return;
        var cust = store.Customers.FirstOrDefault(c => c.Name == customerName);
        if (cust == null || string.IsNullOrWhiteSpace(cust.Email)) return;
        var feeLine = (serviceFee > 0 || cashDisc > 0 || surcharge > 0)
            ? "Fees: " + string.Join("  ", new[] { serviceFee > 0 ? $"service +{serviceFee:C}" : "", cashDisc > 0 ? $"cash discount -{cashDisc:C}" : "", surcharge > 0 ? $"card surcharge +{surcharge:C}" : "" }.Where(s => s.Length > 0)) + Environment.NewLine
            : "";
        var body = new System.Text.StringBuilder()
            .AppendLine("Thank you for your visit!")
            .AppendLine($"Date: {DateTime.Now:dd/MM/yyyy HH:mm}")
            .AppendLine($"Operator: {currentOperator.Name}   Table: {selectedTable}   {orderType}")
            .AppendLine($"Payment #{paymentNum}   {paymentType}")
            .AppendLine("---")
            .ToString()
            + string.Join(Environment.NewLine, lines.Select(l => $"{l.Quantity} x {l.Product}  {(l.UnitPrice * l.Quantity):C}"))
            + Environment.NewLine
            + $"Discount: -{discount:C}   Tip: {tip:C}"
            + Environment.NewLine
            + feeLine
            + $"TOTAL: {finalTotal:C}";
        var to = cust.Email;
        var fromAddr = es.From;
        _ = Task.Run(() =>
        {
            try
            {
                using var client = new System.Net.Mail.SmtpClient(es.Host, es.Port) { EnableSsl = es.Tls };
                if (!string.IsNullOrWhiteSpace(es.User)) client.Credentials = new System.Net.NetworkCredential(es.User, es.Password);
                using var msg = new System.Net.Mail.MailMessage(fromAddr, to, $"Receipt — {DateTime.Now:dd/MM/yyyy HH:mm}", body);
                client.Send(msg);
                if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Email receipt", $"sent to {to}");
            }
            catch (Exception ex)
            {
                try { if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Email receipt failed", ex.Message); } catch { }
            }
        });
    }

    private void StartQrServer()
    {
        if (isOrderPc) return;
        if (qrServer != null) { UpdateQrBanner(); return; }
        qrServer = new QrOrderServer(8080, HandleQrRequest);
        try
        {
            qrServer.Start();
        }
        catch (Exception ex)
        {
            qrServer = null;
            MessageBox.Show($"QR ordering server could not start on port 8080:\n{ex.Message}", "QR ordering", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        UpdateQrBanner();
    }

    private void StopQrServer()
    {
        qrServer?.Stop();
        qrServer = null;
        UpdateQrBanner();
    }

    private string? HandleQrRequest(string method, string path, string body)
    {
        if (IsDisposed || !IsHandleCreated) return null;
        string? result = null;
        using var done = new ManualResetEventSlim(false);
        try
        {
            BeginInvoke((Action)(() =>
            {
                try { result = RouteQr(method, path, body); }
                finally { done.Set(); }
            }));
        }
        catch { return null; }
        return done.Wait(8000) ? result : null;
    }

    private string RouteQr(string method, string path, string body)
    {
        var query = "";
        var qIdx = path.IndexOf('?');
        if (qIdx >= 0)
        {
            query = path[(qIdx + 1)..];
            path = path[..qIdx];
        }
        if (method == "GET" && (path == "/" || path == "/index.html"))
            return HtmlResponse(BuildOrderPage(query));
        if (method == "GET" && path == "/api/menu")
            return JsonResponse(store.Products
                .Where(p => !Is86(store, p.Name))
                .GroupBy(p => p.Category)
                .Select(g => new { category = g.Key, products = g.Select(p => new { name = p.Name, price = p.Price }).ToList() })
                .ToList());
        if (method == "GET" && (path == "/api/sales" || path == "/api/products" || path == "/api/orders"))
        {
            static string QV(string q, string k)
            {
                foreach (var pair in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = pair.Split('=', 2);
                    if (kv.Length == 2 && kv[0] == k) return Uri.UnescapeDataString(kv[1].Replace('+', ' '));
                }
                return "";
            }
            if (!store.Features.ApiEnabled) return JsonResponse(new { ok = false, error = "API is disabled (Settings > Network)." });
            if (!string.IsNullOrEmpty(store.Features.ApiKey) && QV(query, "key") != store.Features.ApiKey)
                return JsonResponse(new { ok = false, error = "Missing or invalid ?key=." });
            if (path == "/api/sales")
            {
                var limit = int.TryParse(QV(query, "limit"), out var l) ? Math.Clamp(l, 1, 500) : 50;
                return JsonResponse(new
                {
                    ok = true,
                    sales = store.Sales.OrderByDescending(s => s.Time).Take(limit).Select(s => new
                    {
                        payment = s.PaymentNumber,
                        time = s.Time.ToString("yyyy-MM-ddTHH:mm:ss"),
                        total = s.Total,
                        payment_type = s.PaymentType,
                        @operator = s.Operator,
                        branch = s.Branch,
                        table = s.Table,
                        order_type = s.OrderType,
                        discount = s.Discount,
                        tip = s.Tip,
                        refund = s.IsRefund
                    }).ToList()
                });
            }
            if (path == "/api/products")
                return JsonResponse(new
                {
                    ok = true,
                    products = store.Products.Where(p => !Is86(store, p.Name)).Select(p => new
                    {
                        name = p.Name,
                        category = p.Category,
                        price = p.Price,
                        family = p.Family,
                        barcode = p.Barcode,
                        weighted = p.Weighted
                    }).ToList()
                });
            return JsonResponse(new
            {
                ok = true,
                orders = store.OpenOrders.Select(o => new
                {
                    room = o.Room,
                    table = o.Table,
                    order = o.OrderNumber,
                    created = o.Created.ToString("yyyy-MM-ddTHH:mm:ss"),
                    ready = o.Ready,
                    source = o.Source,
                    items = o.Items.Select(i => new { product = i.Product, qty = i.Quantity, price = i.UnitPrice, note = i.Note }).ToList()
                }).ToList()
            });
        }
        if (path == "/sync/sales")
        {
            static string QVs(string q, string k)
            {
                foreach (var pair in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = pair.Split('=', 2);
                    if (kv.Length == 2 && kv[0] == k) return Uri.UnescapeDataString(kv[1].Replace('+', ' '));
                }
                return "";
            }
            if (!store.Features.CloudSync && !store.Features.ApiEnabled)
                return JsonResponse(new { ok = false, error = "Cloud sync is disabled (Settings > Network)." });
            var expectKey = !string.IsNullOrEmpty(store.Features.CloudSyncKey) ? store.Features.CloudSyncKey : store.Features.ApiKey;
            if (method == "GET")
            {
                if (!string.IsNullOrEmpty(expectKey) && QVs(query, "key") != expectKey)
                    return JsonResponse(new { ok = false, error = "Missing or invalid ?key=." });
                var requester = QVs(query, "device");
                DateTime.TryParse(QVs(query, "since"), null, System.Globalization.DateTimeStyles.RoundtripKind, out var since);
                var scope = QVs(query, "scope");
                var q = store.Sales.Where(s => s.Time > since).AsEnumerable();
                if (scope == "mine")
                {
                    if (string.IsNullOrEmpty(requester))
                        return JsonResponse(new { ok = false, error = "Missing ?device= when using scope=mine." });
                    q = q.Where(s => s.Device == requester);
                    return JsonResponse(new { ok = true, server_time = DateTime.Now.ToString("o"), sales = q.OrderBy(s => s.Time).Take(10000).ToList() });
                }
                if (!string.IsNullOrEmpty(requester)) q = q.Where(s => s.Device != requester);
                var picked = q.OrderBy(s => s.Time).Take(500).ToList();
                return JsonResponse(new { ok = true, server_time = DateTime.Now.ToString("o"), sales = picked });
            }
            if (method == "POST")
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    var pkey = root.TryGetProperty("key", out var kp) ? kp.GetString() ?? "" : "";
                    if (!string.IsNullOrEmpty(expectKey) && pkey != expectKey)
                        return JsonResponse(new { ok = false, error = "Missing or invalid key." });
                    var pdevice = root.TryGetProperty("device", out var dp) ? dp.GetString() ?? "" : "";
                    var added = 0;
                    if (root.TryGetProperty("sales", out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in arr.EnumerateArray())
                        {
                            try
                            {
                                var incoming = JsonSerializer.Deserialize<Sale>(el.GetRawText());
                                if (incoming == null) continue;
                                if (store.HasSale(incoming)) continue;
                                if (string.IsNullOrEmpty(incoming.Device) && !string.IsNullOrEmpty(pdevice))
                                    incoming = incoming with { Device = pdevice };
                                store.Sales.Add(incoming);
                                added++;
                            }
                            catch { }
                        }
                        if (added > 0)
                        {
                            store.Sales.Sort((a, b) => a.Time.CompareTo(b.Time));
                            store.SaveSales();
                        }
                    }
                    return JsonResponse(new { ok = true, added });
                }
                catch (Exception ex)
                {
                    return JsonResponse(new { ok = false, error = ex.Message });
                }
            }
            return JsonResponse(new { ok = false, error = "Use GET or POST /sync/sales." });
        }
        if (path == "/sync/backup")
        {
            static string QVb(string q, string k)
            {
                foreach (var pair in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = pair.Split('=', 2);
                    if (kv.Length == 2 && kv[0] == k) return Uri.UnescapeDataString(kv[1].Replace('+', ' '));
                }
                return "";
            }
            static bool ValidStore(string s)
            {
                if (s.Length < 1 || s.Length > 64) return false;
                foreach (var c in s)
                    if (!char.IsLetterOrDigit(c) && c != '-' && c != '_') return false;
                return true;
            }
            if (!store.Features.CloudSync && !store.Features.ApiEnabled)
                return JsonResponse(new { ok = false, error = "Cloud sync is disabled (Settings > Network)." });
            var expectKey = !string.IsNullOrEmpty(store.Features.CloudSyncKey) ? store.Features.CloudSyncKey : store.Features.ApiKey;
            if (method == "GET")
            {
                if (!string.IsNullOrEmpty(expectKey) && QVb(query, "key") != expectKey)
                    return JsonResponse(new { ok = false, error = "Missing or invalid ?key=." });
                var storeId = QVb(query, "store");
                if (!ValidStore(storeId))
                    return JsonResponse(new { ok = false, error = "Invalid store id." });
                var dir = Path.Combine(store.DataFolder, "stores", storeId);
                var latest = Directory.Exists(dir) ? Directory.GetFiles(dir, "backup-*.zip").OrderByDescending(f => f).FirstOrDefault() : null;
                if (latest == null)
                    return JsonResponse(new { ok = false, error = $"No backup found for store {storeId}." });
                var fileBytes = File.ReadAllBytes(latest);
                return JsonResponse(new { ok = true, store = storeId, file = Path.GetFileName(latest), data = Convert.ToBase64String(fileBytes) });
            }
            if (method == "POST")
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    var pkey = root.TryGetProperty("key", out var kp) ? kp.GetString() ?? "" : "";
                    if (!string.IsNullOrEmpty(expectKey) && pkey != expectKey)
                        return JsonResponse(new { ok = false, error = "Missing or invalid key." });
                    var storeId = root.TryGetProperty("store", out var sp) ? sp.GetString() ?? "" : "";
                    if (!ValidStore(storeId))
                        return JsonResponse(new { ok = false, error = "Invalid store id (letters, digits, - and _ only)." });
                    var dataB64 = root.TryGetProperty("data", out var datp) ? datp.GetString() ?? "" : "";
                    if (dataB64.Length == 0)
                        return JsonResponse(new { ok = false, error = "No backup data." });
                    var upBytes = Convert.FromBase64String(dataB64);
                    var dir = Path.Combine(store.DataFolder, "stores", storeId);
                    Directory.CreateDirectory(dir);
                    var fileName = $"backup-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
                    File.WriteAllBytes(Path.Combine(dir, fileName), upBytes);
                    var all = Directory.GetFiles(dir, "backup-*.zip").OrderBy(f => f).ToList();
                    for (var i = 0; i < all.Count - 14; i++)
                        try { File.Delete(all[i]); } catch { }
                    return JsonResponse(new { ok = true, file = fileName, size = upBytes.Length });
                }
                catch (Exception ex)
                {
                    return JsonResponse(new { ok = false, error = ex.Message });
                }
            }
            return JsonResponse(new { ok = false, error = "Use GET or POST /sync/backup." });
        }
        if (method == "POST" && path == "/api/order")
            return HandleQrOrder(body);
        if (method == "GET" && path == "/api/call")
        {
            string QV(string k)
            {
                foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = pair.Split('=', 2);
                    if (kv.Length == 2 && kv[0] == k) return Uri.UnescapeDataString(kv[1].Replace('+', ' '));
                }
                return "";
            }
            var r = QV("room"); var t = QV("table");
            if (r.Length == 0 || t.Length == 0) return JsonResponse(new { ok = false, error = "Missing table." });
            store.RaiseGuestAlert(r, t, "waiter");
            if (store.Features.AuditLog) store.LogAudit("Guest", "Call waiter", $"{r}/{t}");
            return JsonResponse(new { ok = true });
        }
        if (method == "GET" && path == "/api/status")
        {
            string QV2(string k)
            {
                foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = pair.Split('=', 2);
                    if (kv.Length == 2 && kv[0] == k) return Uri.UnescapeDataString(kv[1].Replace('+', ' '));
                }
                return "";
            }
            var r2 = QV2("room"); var t2 = QV2("table");
            var order = store.GetOpenOrder(r2, t2);
            var state = order == null ? "none" : order.Ready ? "ready" : "preparing";
            return JsonResponse(new { ok = true, state, order = order?.OrderNumber ?? 0, total = order?.Items.Sum(i => i.UnitPrice * i.Quantity) ?? 0, items = order?.Items.Sum(i => i.Quantity) ?? 0 });
        }
        if (method == "GET" && path == "/status")
            return HtmlResponse(BuildStatusPage(query));
        if (method == "GET" && path.StartsWith("/vt/") && !path.EndsWith("/pay"))
            return HtmlResponse(BuildVtPage(path[4..]));
        if (method == "POST" && path.StartsWith("/vt/") && path.EndsWith("/pay"))
        {
            var idStr = path[4..^4].TrimEnd('/');
            if (!int.TryParse(idStr, out var vtId)) return JsonResponse(new { ok = false, error = "Bad invoice id." });
            var vtIdx = store.VtInvoices.FindIndex(v => v.Id == vtId && v.Status == "Pending");
            if (vtIdx < 0) return JsonResponse(new { ok = false, error = "Invoice not found or already paid." });
            var vtInv = store.VtInvoices[vtIdx];
            store.VtInvoices[vtIdx] = vtInv with { Status = "Paid" };
            store.SaveVtInvoices(store.VtInvoices);
            store.AddSale(new Sale(DateTime.Now, vtInv.Amount, "VT", "Web", [], store.NextPaymentNumber(), 0, 0, $"VT #{vtInv.Id} {vtInv.Name}", vtInv.Name, "Phone order", 0));
            if (store.Features.AuditLog) store.LogAudit("Web", "VT paid", $"#{vtInv.Id} {vtInv.Amount:C} {vtInv.Name}");
            return JsonResponse(new { ok = true, amount = vtInv.Amount });
        }
        return "HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
    }

    private string BuildVtPage(string idStr)
    {
        var inv = int.TryParse(idStr, out var vid) ? store.VtInvoices.FirstOrDefault(v => v.Id == vid) : null;
        if (inv == null)
            return "<!doctype html><html><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Invoice</title></head><body style=\"font-family:sans-serif;background:#141a24;color:#fff;display:flex;align-items:center;justify-content:center;height:100vh;margin:0\"><div style=\"text-align:center\"><h2>Invoice not found</h2><p>Ask the merchant for a new link.</p></div></body></html>";
        var nameJson = JsonSerializer.Serialize(inv.Name);
        var noteJson = JsonSerializer.Serialize(inv.Note);
        return $$"""
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Payment {{inv.Id}}</title></head>
            <body style="font-family:sans-serif;background:#141a24;color:#fff;margin:0;display:flex;align-items:center;justify-content:center;height:100vh">
              <div style="background:#1d2634;padding:32px;border-radius:14px;max-width:420px;width:92%;text-align:center;box-shadow:0 8px 30px rgba(0,0,0,.45)">
                <div style="font-size:14px;color:#8fa0b8;letter-spacing:.12em">VECTRON POS — PHONE ORDER</div>
                <h2 style="margin:12px 0 4px">Invoice #{{inv.Id}}</h2>
                <div id="who" style="color:#c8d4e6"></div>
                <div style="font-size:44px;font-weight:700;color:#7ef0c6;margin:18px 0">{{inv.Amount.ToString("0.00")}} DA</div>
                <button id="pay" style="background:#2fbf71;color:#fff;border:0;border-radius:10px;padding:16px 30px;font-size:18px;font-weight:600;cursor:pointer;width:100%">Pay now</button>
                <div id="msg" style="margin-top:16px;min-height:24px;color:#7ef0c6;font-weight:600"></div>
                <script>
                  const WHO = {{nameJson}}, NOTE = {{noteJson}};
                  document.getElementById('who').textContent = WHO + (NOTE ? ' — ' + NOTE : '');
                  const btn = document.getElementById('pay'), msg = document.getElementById('msg');
                  btn.onclick = async () => {
                    btn.disabled = true; btn.textContent = 'Processing…';
                    try {
                      const r = await fetch('/vt/{{inv.Id}}/pay', { method: 'POST' });
                      const j = await r.json();
                      if (j.ok) { msg.textContent = 'Payment confirmed. Thank you!'; btn.textContent = 'Paid ✓'; btn.style.background = '#20875a'; }
                      else { msg.style.color = '#ff8a7a'; msg.textContent = j.error || 'Payment failed.'; btn.disabled = false; btn.textContent = 'Pay now'; }
                    } catch (e) { msg.style.color = '#ff8a7a'; msg.textContent = 'Network error — try again.'; btn.disabled = false; btn.textContent = 'Pay now'; }
                  };
                </script>
              </div>
            </body>
            </html>
            """;
    }

    private static string BuildStatusPage(string query)
    {
        string Q(string k)
        {
            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split('=', 2);
                if (kv.Length == 2 && kv[0] == k) return Uri.UnescapeDataString(kv[1].Replace('+', ' '));
            }
            return "";
        }
        var roomJson = JsonSerializer.Serialize(Q("room"));
        var tableJson = JsonSerializer.Serialize(Q("table"));
        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Order status</title>
            <style>
              body{margin:0;font-family:system-ui,sans-serif;background:#141a24;color:#eef2f8;min-height:100vh;display:flex;align-items:center;justify-content:center;text-align:center}
              #box{padding:28px;border-radius:16px;background:#1c2536;max-width:420px;width:92%}
              h1{color:#47d6ae;font-size:20px;margin:0 0 6px}
              #table{color:#8fa3c0;font-size:14px;margin-bottom:18px}
              #state{font-size:22px;font-weight:700;padding:18px;border-radius:12px;background:#222c40}
              .preparing{color:#f5b445;background:#3a2e18}
              .ready{color:#7ef0c7;background:#17352b}
              .none{color:#8fa3c0}
              #meta{color:#8fa3c0;font-size:14px;margin-top:14px}
              #call{margin-top:20px;padding:12px 18px;border:none;border-radius:10px;background:#f5b445;color:#2a1e08;font-size:15px;font-weight:700}
              #called{display:none;color:#47d6ae;margin-top:12px}
            </style>
            </head>
            <body>
            <div id="box">
              <h1>Your order</h1>
              <div id="table"></div>
              <div id="state" class="none">Loading…</div>
              <div id="meta"></div>
              <button id="call">🛎 Call waiter</button>
              <div id="called">The waiter has been notified.</div>
            </div>
            <script>
            const ROOM = {{roomJson}};
            const TABLE = {{tableJson}};
            document.getElementById('table').textContent = (ROOM && TABLE) ? `${ROOM} — ${TABLE}` : 'Order tracking';
            let wasReady = false;
            async function poll(){
              try{
                const res = await fetch(`/api/status?room=${encodeURIComponent(ROOM)}&table=${encodeURIComponent(TABLE)}`);
                const d = await res.json();
                const box = document.getElementById('state');
                const meta = document.getElementById('meta');
                if (d.state === 'ready'){
                  box.textContent = '🎉 Ready! Enjoy your meal';
                  box.className = 'ready';
                  meta.textContent = d.order ? `Order #${d.order} — ${d.items} item(s)` : '';
                  if (!wasReady){ wasReady = true; if (navigator.vibrate) navigator.vibrate([300,150,300]); }
                } else if (d.state === 'preparing'){
                  box.textContent = '⏳ Being prepared…';
                  box.className = 'preparing';
                  meta.textContent = d.order ? `Order #${d.order} — ${d.items} item(s)` : '';
                } else {
                  box.textContent = 'No active order for this table';
                  box.className = 'none';
                  meta.textContent = '';
                }
              }catch(e){}
            }
            document.getElementById('call').onclick = async () => {
              await fetch(`/api/call?room=${encodeURIComponent(ROOM)}&table=${encodeURIComponent(TABLE)}`);
              document.getElementById('called').style.display = 'block';
              document.getElementById('call').disabled = true;
            };
            poll(); setInterval(poll, 3000);
            </script>
            </body>
            </html>
            """;
    }

    private string HandleQrOrder(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var room = root.TryGetProperty("room", out var r) ? (r.GetString() ?? "") : "";
            var table = root.TryGetProperty("table", out var t) ? (t.GetString() ?? "") : "";
            if (string.IsNullOrWhiteSpace(room) || string.IsNullOrWhiteSpace(table))
                return JsonResponse(new { ok = false, error = "Choose a room and table." });
            var lines = new List<SoldLine>();
            if (root.TryGetProperty("items", out var items))
            {
                foreach (var it in items.EnumerateArray())
                {
                    var name = it.GetProperty("name").GetString() ?? "";
                    var qty = it.TryGetProperty("qty", out var q) && q.TryGetInt32(out var qi) ? qi : 0;
                    if (name.Length == 0 || qty <= 0) continue;
                    var product = store.Products.FirstOrDefault(p => p.Name == name);
                    if (product == null) continue;
                    lines.Add(new SoldLine(product.Name, product.Family, qty, product.Price, product.Category, ResolvePrinter(product), "", Seat: 0, Course: 0));
                }
            }
            if (lines.Count == 0)
                return JsonResponse(new { ok = false, error = "No valid items in the order." });
            var existing = store.GetOpenOrder(room, table);
            int orderNum;
            if (existing != null)
            {
                orderNum = existing.OrderNumber;
                var merged = existing.Items.Concat(lines).ToList();
                store.RemoveOpenOrder(room, table);
                store.AddOpenOrder(existing with { Items = merged, Source = existing.Source == "" ? "QR" : existing.Source });
            }
            else
            {
                orderNum = store.NextOrderNumber();
                store.AddOpenOrder(new OpenOrder(room, table, "QR", DateTime.Now, lines, "", orderNum, orderNum, OrderType: "Dine-in", SeatCount: 0, FiredCourse: 0, Source: "QR", DeliveryAddress: ""));
            }
            PrintTickets(lines, orderNumber: orderNum, orderType: "Dine-in", room: room, table: table);
            if (store.Features.AuditLog) store.LogAudit("QR", "Order received", $"{room}/{table} — {lines.Sum(l => l.Quantity)} items, order #{orderNum}");
            UpdateQrBanner();
            return JsonResponse(new { ok = true, order = orderNum, table, room, total = lines.Sum(l => l.UnitPrice * l.Quantity) });
        }
        catch (Exception ex)
        {
            return JsonResponse(new { ok = false, error = ex.Message });
        }
    }

    private static string HtmlResponse(string html)
    {
        var length = Encoding.UTF8.GetByteCount(html);
        return $"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {length}\r\nConnection: close\r\n\r\n{html}";
    }

    private static string JsonResponse(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var length = Encoding.UTF8.GetByteCount(json);
        return $"HTTP/1.1 200 OK\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {length}\r\nConnection: close\r\n\r\n{json}";
    }

    private string BuildOrderPage(string query)
    {
        string QueryVal(string key)
        {
            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split('=', 2);
                if (kv.Length == 2 && kv[0] == key) return Uri.UnescapeDataString(kv[1].Replace('+', ' '));
            }
            return "";
        }
        var menuJson = JsonSerializer.Serialize(store.Products
            .Where(p => !Is86(store, p.Name))
            .GroupBy(p => p.Category)
            .Select(g => new { category = g.Key, products = g.Select(p => new { name = p.Name, price = p.Price }).ToList() })
            .ToList());
        var roomsJson = JsonSerializer.Serialize(store.Rooms
            .Select(r => new { name = r.Name, tables = Enumerable.Range(1, r.TableCount).Select(i => TableName(r.Name, i)).ToList() })
            .ToList());
        var storeName = "Vectron POS";
        var currencyJson = JsonSerializer.Serialize(store.RegisterSettings.Currency);
        var defaultRoomJson = JsonSerializer.Serialize(QueryVal("room"));
        var defaultTableJson = JsonSerializer.Serialize(QueryVal("table"));
        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{{storeName}} — Order</title>
            <style>
              body{margin:0;font-family:system-ui,sans-serif;background:#141a24;color:#eef2f8}
              header{padding:16px;background:#1c2536;position:sticky;top:0;z-index:5}
              h1{margin:0 0 10px;font-size:20px;color:#47d6ae}
              select{padding:9px;border-radius:8px;border:1px solid #33405a;background:#222c40;color:#eef2f8;font-size:15px;width:48%;box-sizing:border-box}
              .hbtns{display:flex;gap:8px;margin-top:10px}
              .hbtns button,.hbtns a{padding:9px 12px;border:none;border-radius:8px;background:#2f3d57;color:#eef2f8;font-size:14px;text-decoration:none;text-align:center;cursor:pointer}
              .hbtns a{background:#3a4a68}
              #callok{display:none;color:#47d6ae;font-size:13px;margin-top:8px}
              section{padding:10px 14px}
              h2{font-size:14px;color:#8fa3c0;text-transform:uppercase;letter-spacing:.5px;margin:14px 0 6px}
              .row{display:flex;align-items:center;justify-content:space-between;background:#1c2536;border-radius:10px;padding:10px 12px;margin:7px 0}
              .row .nm{font-size:15px}
              .row .pr{color:#47d6ae;font-weight:600}
              .qty{display:flex;align-items:center;gap:8px}
              .qty button{width:32px;height:32px;border:none;border-radius:8px;background:#2f3d57;color:#fff;font-size:17px}
              .qty span{min-width:20px;text-align:center;font-weight:600}
              footer{position:fixed;bottom:0;left:0;right:0;background:#1c2536;padding:12px 14px;display:flex;gap:10px;align-items:center}
              #send{flex:1;padding:13px;border:none;border-radius:10px;background:#47d6ae;color:#10231d;font-size:16px;font-weight:700}
              #total{color:#8fa3c0;font-size:15px;min-width:90px}
              #done{display:none;margin:16px;padding:16px;border-radius:12px;background:#17352b;color:#7ef0c7;font-size:16px;text-align:center}
            </style>
            </head>
            <body>
            <header>
              <h1>{{storeName}}</h1>
              <select id="room"></select>
              <select id="table"></select>
              <div class="hbtns">
                <button id="callWaiter">🛎 Call waiter</button>
                <a id="trackLink" href="#" target="_blank">Track my order</a>
              </div>
              <div id="callok">The waiter is on the way.</div>
            </header>
            <div id="done"></div>
            <div id="menu"></div>
            <footer><div id="total">0.00</div><button id="send">Send order to kitchen</button></footer>
            <script>
            const MENU = {{menuJson}};
            const ROOMS = {{roomsJson}};
            const CUR = {{currencyJson}};
            const DEF_ROOM = {{defaultRoomJson}};
            const DEF_TABLE = {{defaultTableJson}};
            const cart = {};
            const roomSel = document.getElementById('room');
            const tableSel = document.getElementById('table');
            function fillRooms(){
              roomSel.innerHTML = ROOMS.map(r => `<option value="${r.name}">${r.name}</option>`).join('');
              if (DEF_ROOM && ROOMS.some(r => r.name === DEF_ROOM)) roomSel.value = DEF_ROOM;
              fillTables();
            }
            function fillTables(){
              const room = ROOMS.find(r => r.name === roomSel.value) || ROOMS[0];
              const tables = room ? room.tables : [];
              tableSel.innerHTML = tables.map(t => `<option value="${t}">${t}</option>`).join('');
              if (DEF_TABLE && tables.includes(DEF_TABLE)) tableSel.value = DEF_TABLE;
              updateTrack();
            }
            roomSel.onchange = () => { fillTables(); updateTrack(); };
            tableSel.onchange = updateTrack;
            function updateTrack(){
              const link = document.getElementById('trackLink');
              link.href = `/status?room=${encodeURIComponent(roomSel.value)}&table=${encodeURIComponent(tableSel.value)}`;
            }
            document.getElementById('callWaiter').onclick = async () => {
              await fetch(`/api/call?room=${encodeURIComponent(roomSel.value)}&table=${encodeURIComponent(tableSel.value)}`);
              document.getElementById('callok').style.display = 'block';
              document.getElementById('callWaiter').disabled = true;
            };
            function render(){
              const host = document.getElementById('menu');
              host.innerHTML = MENU.map(cat => `<section><h2>${cat.category}</h2>` +
                cat.products.map(p => {
                  const c = cart[p.name] || 0;
                  const enc = encodeURIComponent(p.name);
                  return `<div class="row"><div><div class="nm">${p.name}</div><div class="pr">${p.price.toFixed(2)} ${CUR}</div></div>
                  <div class="qty"><button onclick="step('${enc}',-1)">−</button><span id="q_${enc}">${c}</span><button onclick="step('${enc}',1)">+</button></div></div>`;
                }).join('') + `</section>`).join('');
              updateTotal();
            }
            function step(enc, d){
              const name = decodeURIComponent(enc);
              cart[name] = Math.max(0, (cart[name] || 0) + d);
              if (cart[name] === 0) delete cart[name];
              document.getElementById('q_' + enc).textContent = cart[name] || 0;
              updateTotal();
            }
            function updateTotal(){
              let sum = 0;
              for (const cat of MENU) for (const p of cat.products) sum += (cart[p.name] || 0) * p.price;
              document.getElementById('total').textContent = sum.toFixed(2);
            }
            document.getElementById('send').onclick = async () => {
              const items = Object.entries(cart).map(([name, qty]) => ({ name, qty }));
              if (!items.length) { alert('Add items first.'); return; }
              const res = await fetch('/api/order', { method: 'POST', headers: {'Content-Type':'application/json'}, body: JSON.stringify({ room: roomSel.value, table: tableSel.value, items }) });
              const data = await res.json();
              if (data.ok) {
                for (const k in cart) delete cart[k];
                render();
                const done = document.getElementById('done');
                done.style.display = 'block';
                done.textContent = `Order #${data.order} sent to the kitchen for ${data.table} — ${Number(data.total).toFixed(2)}`;
                window.scrollTo(0, 0);
              } else alert(data.error || 'Order failed.');
            };
            fillRooms(); render(); updateTrack();
            </script>
            </body>
            </html>
            """;
    }

    private void ShowQROrders()
    {
        if ((!store.Features.QROrdering && !store.Features.KioskMode) || isOrderPc) return;
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        var info = new Label { Dock = DockStyle.Top, Height = 56, ForeColor = Color.FromArgb(170, 180, 195), Text = $"Customers scan the QR code or open  http://{TcpPrintServer.LocalIp}:8080  on their phone (or use the kiosk) to order.\nOrders arrive here and print straight to the kitchen. Click a row, then Open table." };
        var lv = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, MultiSelect = false };
        lv.Columns.Add("Order #", 70); lv.Columns.Add("Room", 130); lv.Columns.Add("Table", 120); lv.Columns.Add("Source", 70); lv.Columns.Add("Received", 90); lv.Columns.Add("Items", 60); lv.Columns.Add("Total", 90);
        void Rebuild()
        {
            lv.Items.Clear();
            foreach (var o in store.OpenOrders.Where(o => o.Source is "QR" or "Kiosk").OrderByDescending(o => o.Created))
            {
                var item = new ListViewItem([o.OrderNumber > 0 ? o.OrderNumber.ToString() : "-", o.Room, o.Table, o.Source, o.Created.ToString("HH:mm"), o.Items.Sum(i => i.Quantity).ToString(), o.Items.Sum(i => i.UnitPrice * i.Quantity).ToString("C")]);
                lv.Items.Add(item);
            }
            UpdateQrBanner();
        }
        Rebuild();
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 6, 0, 0) };
        Button B(string text, Color back)
        {
            var b = new Button { Text = text, Size = new Size(150, 36), BackColor = back, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 9.5F) };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
        var openBtn = B("Open table", Color.FromArgb(60, 130, 90));
        var refreshBtn = B("Refresh", panelLight);
        openBtn.Click += (_, _) =>
        {
            if (lv.SelectedItems.Count == 0) { MessageBox.Show("Select an order first.", "QR orders", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var orderNum = int.Parse(lv.SelectedItems[0].Text == "-" ? "0" : lv.SelectedItems[0].Text);
            var roomName = lv.SelectedItems[0].SubItems[1].Text;
            var tableName = lv.SelectedItems[0].SubItems[2].Text;
            selectedRoom = roomName;
            selectedTable = tableName;
            orderLabel.Text = OrderContextText();
            hasOpenOrder = false;
            RefreshOpenOrders();
            UpdateOrderButtons();
            RenderBasket();
            var room = store.Rooms.FirstOrDefault(r => r.Name == roomName);
            if (room != null) RenderTableMap(room);
            ShowOrder();
        };
        refreshBtn.Click += (_, _) => Rebuild();
        var labelsBtn = B("Table QR labels", Color.FromArgb(70, 90, 130));
        labelsBtn.Click += (_, _) => ShowQrLabels();
        var copyBtn = B("Copy link", panelLight);
        copyBtn.Click += (_, _) =>
        {
            try { Clipboard.SetText($"http://{TcpPrintServer.LocalIp}:8080/"); MessageBox.Show("Link copied to clipboard.", "QR orders", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch { }
        };
        buttons.Controls.AddRange([openBtn, refreshBtn, labelsBtn, copyBtn]);
        panel.Controls.Add(lv);
        panel.Controls.Add(info);
        panel.Controls.Add(buttons);
        ShowView(panel, "QR orders", ShowOrder);
    }

    private void ShowQrLabels()
    {
        var tables = new List<(string Room, string Table, string Url)>();
        foreach (var room in store.Rooms)
            for (var i = 1; i <= room.TableCount; i++)
            {
                var t = TableName(room.Name, i);
                tables.Add((room.Name, t, $"http://{TcpPrintServer.LocalIp}:8080/?room={Uri.EscapeDataString(room.Name)}&table={Uri.EscapeDataString(t)}"));
            }
        if (tables.Count == 0)
        {
            MessageBox.Show("No rooms/tables defined yet.", "QR labels", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dlg = new Form { Text = "Table QR labels", Size = new Size(960, 620), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.Sizable, MaximizeBox = true, MinimizeBox = false };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(12), BackColor = background };
        var cardW = 300;
        var cardH = 300;
        var x = 12;
        var y = 12;
        foreach (var (r, t, url) in tables)
        {
            var card = new Panel { Location = new Point(x, y), Size = new Size(cardW, cardH), BackColor = Color.White, Margin = new Padding(8) };
            var qrFailed = !QrCode.TryEncode(url, out _, out _);
            var pic = new PictureBox { Location = new Point(55, 10), Size = new Size(190, 190), SizeMode = PictureBoxSizeMode.Zoom };
            try { pic.Image = QrCode.ToBitmap(url, 5); } catch { qrFailed = true; }
            var lblT = new Label { Text = t, Font = new Font("Segoe UI Semibold", 14F), ForeColor = Color.Black, AutoSize = true, Location = new Point(10, 205) };
            var lblR = new Label { Text = r, Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(90, 100, 120), AutoSize = true, Location = new Point(10, 232) };
            var lblU = new Label { Text = qrFailed ? "URL too long for QR — print this URL as text instead:\n" + url : url, Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(120, 130, 150), Location = new Point(10, 255), Size = new Size(280, 42), AutoEllipsis = true };
            card.Controls.AddRange([pic, lblT, lblR, lblU]);
            scroll.Controls.Add(card);
            x += cardW + 12;
            if (x + cardW > 930) { x = 12; y += cardH + 12; }
        }
        var printBtn = new Button { Text = "Print labels…", Dock = DockStyle.Bottom, Height = 40, BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        printBtn.FlatAppearance.BorderSize = 0;
        printBtn.Click += (_, _) => PrintQrLabels(tables);
        dlg.Controls.Add(scroll);
        dlg.Controls.Add(printBtn);
        dlg.ShowDialog();
        foreach (Control c in scroll.Controls)
            foreach (Control inner in c.Controls)
                if (inner is PictureBox pb) pb.Image?.Dispose();
    }

    private void PrintQrLabels(List<(string Room, string Table, string Url)> tables)
    {
        var perPage = 4;
        var pages = (int)Math.Ceiling(tables.Count / (double)perPage);
        using var doc = new PrintDocument();
        var page = 0;
        doc.DocumentName = "Table QR labels";
        doc.PrintPage += (_, e) =>
        {
            if (e.Graphics == null) { e.HasMorePages = false; return; }
            var area = e.MarginBounds;
            var cw = area.Width / 2;
            var ch = area.Height / 2;
            for (var slot = 0; slot < perPage; slot++)
            {
                var i = page * perPage + slot;
                if (i >= tables.Count) break;
                var (r, t, url) = tables[i];
                var col = slot % 2;
                var row = slot / 2;
                var rect = new Rectangle(area.X + col * cw, area.Y + row * ch, cw, ch);
                e.Graphics.DrawRectangle(Pens.Gray, rect);
                try
                {
                    using var qr = QrCode.ToBitmap(url, 6);
                    var side = Math.Min(cw - 24, ch - 90);
                    e.Graphics.DrawImage(qr, rect.X + (cw - side) / 2, rect.Y + 10, side, side);
                }
                catch { }
                using (var titleFont = new Font("Segoe UI Semibold", 16F))
                using (var subFont = new Font("Segoe UI", 10F))
                using (var urlFont = new Font("Segoe UI", 7.5F))
                {
                    e.Graphics.DrawString(t, titleFont, Brushes.Black, rect.X, rect.Y + ch - 72);
                    e.Graphics.DrawString(r, subFont, Brushes.Gray, rect.X, rect.Y + ch - 46);
                    e.Graphics.DrawString(url, urlFont, Brushes.DarkGray, new RectangleF(rect.X + 2, rect.Y + ch - 28, cw - 8, 26));
                }
            }
            page++;
            e.HasMorePages = page < pages;
        };
        using var preview = new PrintPreviewDialog { Document = doc, Width = 900, Height = 700 };
        preview.ShowDialog();
    }

    private void ShowStockManagement()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        var lv = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true };
        lv.Columns.Add("Ingredient", 160); lv.Columns.Add("Stock", 80); lv.Columns.Add("Unit", 60); lv.Columns.Add("Min Stock", 80); lv.Columns.Add("Cost", 80); lv.Columns.Add("Expiry", 90);
        foreach (var i in store.Ingredients)
        {
            var item = new ListViewItem([i.Name, i.Stock.ToString("F1"), i.Unit, i.MinStock.ToString("F1"), i.Cost.ToString("C"), i.Expiry?.ToString("dd/MM/yyyy") ?? "—"]);
            if (i.Expiry != null && i.Expiry.Value.Date < DateTime.Today) item.ForeColor = Color.FromArgb(235, 90, 90);
            else if (i.Expiry != null && i.Expiry.Value.Date <= DateTime.Today.AddDays(7)) item.ForeColor = Color.FromArgb(245, 180, 80);
            lv.Items.Add(item);
        }
        var addBtn = new Button { Text = "Add", Dock = DockStyle.Bottom, Height = 36, BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        addBtn.FlatAppearance.BorderSize = 0;
        addBtn.Click += (_, _) =>
        {
            using var f = new Form { Text = "Add ingredient", Size = new Size(340, 240), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var n = new TextBox { Location = new Point(20, 30), Size = new Size(280, 28), BackColor = panelLight, ForeColor = Color.White };
            var u = new TextBox { Location = new Point(20, 65), Size = new Size(140, 28), BackColor = panelLight, ForeColor = Color.White, Text = "kg" };
            var s = new NumericUpDown { Location = new Point(20, 100), Size = new Size(100, 28), BackColor = panelLight, ForeColor = Color.White, DecimalPlaces = 1 };
            var ok = new Button { Text = "OK", Location = new Point(20, 140), Size = new Size(80, 30), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
            ok.FlatAppearance.BorderSize = 0;
            f.Controls.AddRange([new Label { Text = "Name", AutoSize = true, Location = new Point(20, 12) }, n, new Label { Text = "Unit", AutoSize = true, Location = new Point(20, 50) }, u, new Label { Text = "Stock", AutoSize = true, Location = new Point(20, 85) }, s, ok]);
            ok.Click += (_, _) => { store.Ingredients.Add(new Ingredient(n.Text, u.Text, s.Value, s.Value)); store.SaveIngredients(store.Ingredients); f.DialogResult = DialogResult.OK; };
            if (f.ShowDialog() == DialogResult.OK) ShowStockManagement();
        };
        panel.Controls.Add(lv); panel.Controls.Add(addBtn);
        ShowView(panel, "Stock Management", ShowSettings);
    }

    private void ShowKitchenDisplay()
    {
        if (!store.Features.KitchenDisplay) return;
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        var kds = new KitchenDisplayForm(background, panelLight, mint, store) { TopLevel = false, FormBorderStyle = FormBorderStyle.None, Dock = DockStyle.Fill };
        panel.Controls.Add(kds);
        kds.Show();
        ShowView(panel, "Kitchen display");
    }

    private void ShowEightySixBoard()
    {
        if (!store.Features.EightySixBoard) return;
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        var scroll = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = background };
        void Rebuild()
        {
            scroll.Controls.Clear();
            foreach (var group in store.Products.GroupBy(p => p.Category))
            {
                scroll.Controls.Add(new Label { Text = group.Key.ToUpperInvariant(), Font = new Font("Segoe UI Semibold", 12F), ForeColor = mint, AutoSize = true, Margin = new Padding(0, 14, 0, 4) });
                foreach (var product in group)
                {
                    var out86 = Is86(store, product.Name);
                    var row = new Panel { Width = 560, Height = 46, BackColor = panelLight, Margin = new Padding(0, 2, 0, 2) };
                    var name = new Label { Text = product.Name, AutoSize = true, Font = out86 ? new Font("Segoe UI", 11F, FontStyle.Strikeout) : new Font("Segoe UI", 11F), ForeColor = out86 ? Color.FromArgb(150, 160, 175) : Color.White, Location = new Point(12, 13) };
                    var price = new Label { Text = product.Price.ToString("C"), AutoSize = true, ForeColor = mint, Location = new Point(260, 14) };
                    var toggle = new Button { Text = out86 ? "Restore" : "86 it", Size = new Size(110, 32), Location = new Point(440, 7), FlatStyle = FlatStyle.Flat, BackColor = out86 ? Color.FromArgb(60, 130, 90) : Color.FromArgb(180, 70, 60), ForeColor = Color.White, Cursor = Cursors.Hand };
                    toggle.FlatAppearance.BorderSize = 0;
                    toggle.Click += (_, _) =>
                    {
                        var list = store.EightySix.ToList();
                        if (!list.Remove(product.Name)) list.Add(product.Name);
                        store.SaveEightySix(list);
                        if (store.Auto86.Contains(product.Name))
                        {
                            var auto = store.Auto86.ToList();
                            auto.Remove(product.Name);
                            store.SaveAuto86(auto);
                        }
                        RenderProducts();
                        Rebuild();
                    };
                    row.Controls.AddRange([name, price, toggle]);
                    scroll.Controls.Add(row);
                }
            }
        }
        Rebuild();
        var hint = new Label { Text = "86 = out of stock. Struck-through items can't be ordered.", Dock = DockStyle.Top, Height = 32, ForeColor = Color.FromArgb(143, 154, 171), Padding = new Padding(2, 8, 0, 0) };
        panel.Controls.Add(scroll);
        panel.Controls.Add(hint);
        ShowView(panel, "86 board");
    }

    private void ShowModifierManager()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        var lv = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true };
        lv.Columns.Add("Group", 150); lv.Columns.Add("Options", 200);
        foreach (var g in store.ModifierGroups) lv.Items.Add(new ListViewItem([g.Name, string.Join(", ", g.Options.Select(o => $"{o.Name} +{o.Price:C}"))]));
        var addGroup = new Button { Text = "Add group", Dock = DockStyle.Bottom, Height = 36, BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        addGroup.FlatAppearance.BorderSize = 0;
        addGroup.Click += (_, _) =>
        {
            using var f = new Form { Text = "Add modifier group", Size = new Size(380, 300), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var gn = new TextBox { Location = new Point(20, 30), Size = new Size(320, 28), BackColor = panelLight, ForeColor = Color.White };
            var optList = new ListBox { Location = new Point(20, 70), Size = new Size(320, 120), BackColor = panelLight, ForeColor = Color.White };
            var optName = new TextBox { Location = new Point(20, 200), Size = new Size(180, 28), BackColor = panelLight, ForeColor = Color.White, PlaceholderText = "Option name" };
            var optPrice = new NumericUpDown { Location = new Point(210, 200), Size = new Size(80, 28), BackColor = panelLight, ForeColor = Color.White, DecimalPlaces = 2, Minimum = 0 };
            var addOpt = new Button { Text = "+", Location = new Point(300, 200), Size = new Size(40, 28), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            addOpt.FlatAppearance.BorderSize = 0;
            addOpt.Click += (_, _) => { if (!string.IsNullOrWhiteSpace(optName.Text)) { optList.Items.Add($"{optName.Text}|{optPrice.Value}"); optName.Text = ""; } };
            var ok = new Button { Text = "Save", Location = new Point(20, 240), Size = new Size(100, 34), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
            ok.FlatAppearance.BorderSize = 0;
            ok.Click += (_, _) =>
            {
                var opts = optList.Items.Cast<string>().Select(s => { var parts = s.Split('|'); return new ModifierOption(parts[0], decimal.Parse(parts[1])); }).ToList();
                store.ModifierGroups.Add(new ModifierGroup(gn.Text, opts));
                store.SaveModifiers(store.ModifierGroups);
                f.DialogResult = DialogResult.OK;
            };
            f.Controls.AddRange([new Label { Text = "Group name", AutoSize = true, Location = new Point(20, 12) }, gn, optList, optName, optPrice, addOpt, ok]);
            if (f.ShowDialog() == DialogResult.OK) ShowModifierManager();
        };
        panel.Controls.Add(lv); panel.Controls.Add(addGroup);
        ShowView(panel, "Modifier Groups", ShowSettings);
    }

    private void ShowCustomerManager()
    {
        if (!store.Features.CustomerDatabase) return;
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        var lv = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true, MultiSelect = false };
        lv.Columns.Add("Name", 120); lv.Columns.Add("Phone", 100); lv.Columns.Add("Email", 130); lv.Columns.Add("Points", 55);
        if (store.Features.ChargeTabs) { lv.Columns.Add("Credit", 75); lv.Columns.Add("Tab due", 75); lv.Columns.Add("Limit", 65); }
        void Rebuild()
        {
            lv.Items.Clear();
            foreach (var c in store.Customers)
            {
                var cols = new List<string> { c.Name, c.Phone, c.Email, c.Points.ToString() };
                if (store.Features.ChargeTabs) cols.AddRange([c.Balance.ToString("C"), c.TabDue.ToString("C"), c.CreditLimit > 0 ? c.CreditLimit.ToString("C") : "—"]);
                var item = new ListViewItem([.. cols]);
                if (store.Features.ChargeTabs && c.TabDue > 0) item.ForeColor = Color.Orange;
                lv.Items.Add(item);
            }
        }
        Rebuild();
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 6, 0, 0) };
        Button B(string text, Color back)
        {
            var b = new Button { Text = text, Size = new Size(170, 36), BackColor = back, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 9.5F) };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
        var addBtn = B("Add customer", mint);
        addBtn.ForeColor = background;
        var limitBtn = B("Set credit limit", Color.FromArgb(55, 120, 130));
        limitBtn.Visible = store.Features.ChargeTabs;
        var payTabBtn = B("Pay tab", Color.FromArgb(60, 130, 90));
        payTabBtn.Visible = store.Features.ChargeTabs;
        addBtn.Click += (_, _) =>
        {
            using var f = new Form { Text = "Add customer", Size = new Size(360, 220), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var n = new TextBox { Location = new Point(20, 30), Size = new Size(300, 28), BackColor = panelLight, ForeColor = Color.White };
            var p = new TextBox { Location = new Point(20, 70), Size = new Size(300, 28), BackColor = panelLight, ForeColor = Color.White };
            var e = new TextBox { Location = new Point(20, 110), Size = new Size(300, 28), BackColor = panelLight, ForeColor = Color.White };
            var ok = new Button { Text = "OK", Location = new Point(20, 150), Size = new Size(80, 30), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
            ok.FlatAppearance.BorderSize = 0;
            f.Controls.AddRange([new Label { Text = "Name", AutoSize = true, Location = new Point(20, 12) }, n, new Label { Text = "Phone", AutoSize = true, Location = new Point(20, 55) }, p, new Label { Text = "Email", AutoSize = true, Location = new Point(20, 95) }, e, ok]);
            ok.Click += (_, _) => { store.Customers.Add(new Customer(n.Text, p.Text, e.Text)); store.SaveCustomers(store.Customers); f.DialogResult = DialogResult.OK; };
            if (f.ShowDialog() == DialogResult.OK) Rebuild();
        };
        limitBtn.Click += (_, _) =>
        {
            if (lv.SelectedItems.Count == 0) { MessageBox.Show("Select a customer first.", "Credit limit", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var name = lv.SelectedItems[0].Text;
            var ci = store.Customers.FindIndex(c => c.Name == name);
            if (ci < 0) return;
            using var f = new Form { Text = $"Credit limit — {name}", Size = new Size(380, 220), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var cap = new Label { Text = $"Current: {(store.Customers[ci].CreditLimit > 0 ? store.Customers[ci].CreditLimit.ToString("C") : "no limit")}  •  tab due {store.Customers[ci].TabDue:C}", AutoSize = true, Location = new Point(20, 16) };
            var input = new NumericUpDown { Minimum = 0, Maximum = 1000000, DecimalPlaces = 2, Value = Math.Min(1000000, store.Customers[ci].CreditLimit), Location = new Point(20, 56), Size = new Size(170, 30), BackColor = panelLight, ForeColor = Color.White };
            var hint = new Label { Text = "0 = no limit.", AutoSize = true, Location = new Point(20, 94), ForeColor = Color.FromArgb(143, 154, 171) };
            var ok = new Button { Text = "Save", Location = new Point(20, 126), Size = new Size(110, 34), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "Cancel", Location = new Point(140, 126), Size = new Size(110, 34), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
            ok.Click += (_, _) =>
            {
                store.Customers[ci] = store.Customers[ci] with { CreditLimit = input.Value };
                store.SaveCustomers(store.Customers);
                if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Credit limit", $"{name} → {input.Value:C}");
                f.DialogResult = DialogResult.OK;
            };
            cancel.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
            f.Controls.AddRange([cap, input, hint, ok, cancel]);
            if (f.ShowDialog() == DialogResult.OK) Rebuild();
        };
        payTabBtn.Click += (_, _) =>
        {
            if (lv.SelectedItems.Count == 0) { MessageBox.Show("Select a customer first.", "Pay tab", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var name = lv.SelectedItems[0].Text;
            var ci = store.Customers.FindIndex(c => c.Name == name);
            if (ci < 0) return;
            var due = store.Customers[ci].TabDue;
            if (due <= 0) { MessageBox.Show($"{name} has no tab balance.", "Pay tab", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using var f = new Form { Text = $"Pay tab — {name}", Size = new Size(400, 250), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var cap = new Label { Text = $"Tab due: {due:C}", AutoSize = true, Location = new Point(20, 16) };
            var amt = new NumericUpDown { Minimum = 0.01m, Maximum = 1000000, DecimalPlaces = 2, Value = Math.Min(1000000, due), Location = new Point(20, 50), Size = new Size(170, 30), BackColor = panelLight, ForeColor = Color.White };
            var methodLabel = new Label { Text = "Paid by:", AutoSize = true, Location = new Point(20, 94) };
            var method = new ComboBox { Location = new Point(20, 118), Size = new Size(170, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
            method.Items.AddRange(["Cash", "Card", "Transfer", "Mobile"]);
            method.SelectedIndex = 0;
            var ok = new Button { Text = "Pay", Location = new Point(20, 162), Size = new Size(120, 38), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "Cancel", Location = new Point(150, 162), Size = new Size(110, 38), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
            ok.Click += (_, _) =>
            {
                var paid = amt.Value;
                store.Customers[ci] = store.Customers[ci] with { TabDue = Math.Max(0, store.Customers[ci].TabDue - paid) };
                store.SaveCustomers(store.Customers);
                store.AddSale(new Sale(DateTime.Now, paid, "Tab", currentOperator.Name, [], store.NextPaymentNumber(), 0, 0, $"Tab payment ({method.Text})", name, "Tab payment", 0, CashAmount: method.Text == "Cash" ? paid : 0));
                if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Tab payment", $"{name} {paid:C} via {method.Text} — remaining {store.Customers[ci].TabDue:C}");
                f.DialogResult = DialogResult.OK;
            };
            cancel.Click += (_, _) => f.DialogResult = DialogResult.Cancel;
            f.Controls.AddRange([cap, amt, methodLabel, method, ok, cancel]);
            if (f.ShowDialog() == DialogResult.OK) Rebuild();
        };
        buttons.Controls.AddRange(store.Features.ChargeTabs ? [addBtn, limitBtn, payTabBtn] : [addBtn]);
        panel.Controls.Add(lv); panel.Controls.Add(buttons);
        ShowView(panel, "Customer Database", ShowSettings);
    }

    private void ShowHappyHourManager()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        var lv = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true };
        lv.Columns.Add("Rule", 100); lv.Columns.Add("Time", 100); lv.Columns.Add("Discount", 80); lv.Columns.Add("Days", 140);
        foreach (var r in store.HappyHourRules) lv.Items.Add(new ListViewItem([r.Name, $"{r.Start:hh\\:mm}-{r.End:hh\\:mm}", $"{r.DiscountPercent}%", string.Join(", ", r.Days)]));
        var addBtn = new Button { Text = "Add rule", Dock = DockStyle.Bottom, Height = 36, BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        addBtn.FlatAppearance.BorderSize = 0;
        addBtn.Click += (_, _) =>
        {
            using var f = new Form { Text = "Happy hour rule", Size = new Size(380, 300), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var rn = new TextBox { Location = new Point(20, 30), Size = new Size(300, 28), BackColor = panelLight, ForeColor = Color.White };
            var sd = new DateTimePicker { Location = new Point(20, 70), Size = new Size(140, 28), BackColor = panelLight, ForeColor = Color.White, Format = DateTimePickerFormat.Time, ShowUpDown = true };
            var ed = new DateTimePicker { Location = new Point(170, 70), Size = new Size(140, 28), BackColor = panelLight, ForeColor = Color.White, Format = DateTimePickerFormat.Time, ShowUpDown = true };
            var disc = new NumericUpDown { Location = new Point(20, 110), Size = new Size(100, 28), BackColor = panelLight, ForeColor = Color.White, Minimum = 1, Maximum = 100, Value = 10 };
            var dayChecks = new List<CheckBox>();
            int dx = 20;
            foreach (var day in new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" })
            {
                var cb = new CheckBox { Text = day[..2], AutoSize = true, Location = new Point(dx, 150), ForeColor = Color.White };
                dayChecks.Add(cb); dx += 45;
            }
            var ok = new Button { Text = "Save", Location = new Point(20, 190), Size = new Size(100, 34), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
            ok.FlatAppearance.BorderSize = 0;
            ok.Click += (_, _) =>
            {
                var days = dayChecks.Where(cb => cb.Checked).Select(cb => cb.Text switch { "Mo" => "Monday", "Tu" => "Tuesday", "We" => "Wednesday", "Th" => "Thursday", "Fr" => "Friday", "Sa" => "Saturday", "Su" => "Sunday", _ => cb.Text }).ToList();
                store.HappyHourRules.Add(new HappyHourRule(rn.Text, sd.Value.TimeOfDay, ed.Value.TimeOfDay, disc.Value, days));
                store.SaveHappyHours(store.HappyHourRules);
                f.DialogResult = DialogResult.OK;
            };
            f.Controls.AddRange([new Label { Text = "Name", AutoSize = true, Location = new Point(20, 12) }, rn, new Label { Text = "Start", AutoSize = true, Location = new Point(20, 55) }, sd, new Label { Text = "End", AutoSize = true, Location = new Point(170, 55) }, ed, new Label { Text = "Discount %", AutoSize = true, Location = new Point(20, 95) }, disc, ok]);
            foreach (var cb in dayChecks) f.Controls.Add(cb);
            if (f.ShowDialog() == DialogResult.OK) ShowHappyHourManager();
        };
        panel.Controls.Add(lv); panel.Controls.Add(addBtn);
        ShowView(panel, "Happy Hour Rules", ShowSettings);
    }

    private void ShowAuditLogViewer()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        var lv = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true };
        lv.Columns.Add("Time", 140); lv.Columns.Add("Operator", 100); lv.Columns.Add("Action", 100); lv.Columns.Add("Detail", 200);
        foreach (var e in store.AuditLog.TakeLast(200).Reverse()) lv.Items.Add(new ListViewItem([e.Time.ToString("dd/MM HH:mm:ss"), e.Operator, e.Action, e.Detail]));
        panel.Controls.Add(lv);
        ShowView(panel, "Audit Log", ShowSettings);
    }
}

internal static class OnScreenKeyboard
{
    public static Func<bool> IsEnabled = static () => false;

    public static void Register() => Application.AddMessageFilter(new KeyboardMessageFilter());
}

internal sealed class KeyboardMessageFilter : IMessageFilter
{
    public bool PreFilterMessage(ref Message m)
    {
        if (m.Msg != 0x0201 && m.Msg != 0x0203) return false;
        if (KeyboardForm.IsOpen || !OnScreenKeyboard.IsEnabled()) return false;
        if (Control.FromHandle(m.HWnd) is not TextBox tb) return false;
        if (tb.IsDisposed || !tb.Enabled || !tb.Visible || tb.ReadOnly) return false;
        if (tb.FindForm() is KeyboardForm) return false;
        if (!tb.IsHandleCreated) return false;
        var lParam = (int)m.LParam;
        var clickPoint = new Point((short)(lParam & 0xFFFF), (short)((lParam >> 16) & 0xFFFF));
        if (!tb.ClientRectangle.Contains(clickPoint)) return false;
        KeyboardForm.ShowFor(tb);
        return false;
    }
}

internal sealed class KeyboardForm : Form
{
    private static readonly Color background = Color.FromArgb(19, 24, 33);
    private static readonly Color mint = Color.FromArgb(71, 214, 174);
    private static readonly Color dimText = Color.FromArgb(143, 154, 171);
    private static readonly Color darkKey = Color.FromArgb(45, 56, 74);
    private static readonly Color darkKey2 = Color.FromArgb(60, 72, 92);

    private readonly TextBox target;
    private readonly bool numericOnly;
    private readonly Label preview;
    private readonly Panel abcPanel = new();
    private readonly Panel symPanel = new();
    private readonly Panel numPanel = new();
    private readonly List<(Button Button, char Character)> letterKeys = [];
    private Button shiftButton = new();
    private bool shift;

    public static bool IsOpen { get; private set; }

    public static void ShowFor(TextBox textBox)
    {
        if (IsOpen || textBox.IsDisposed) return;
        using var form = new KeyboardForm(textBox);
        IsOpen = true;
        try
        {
            var owner = textBox.TopLevelControl as Form;
            if (owner != null && !owner.IsDisposed) form.ShowDialog(owner);
            else form.ShowDialog();
        }
        finally
        {
            IsOpen = false;
            ReleaseStaleCapture(textBox);
        }
    }

    private static void ReleaseStaleCapture(TextBox textBox)
    {
        if (textBox.IsDisposed || !textBox.IsHandleCreated) return;
        try
        {
            textBox.BeginInvoke(() =>
            {
                if (!textBox.IsDisposed && textBox.IsHandleCreated) textBox.Capture = false;
            });
        }
        catch (InvalidOperationException) { }
    }

    private KeyboardForm(TextBox textBox)
    {
        target = textBox;
        numericOnly = IsNumericTarget(textBox);
        Text = "Keyboard";
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        KeyPreview = true;
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        Size = new Size(780, 344);

        var title = string.IsNullOrWhiteSpace(target.Name) ? "Text" : target.Name;
        title = char.ToUpperInvariant(title[0]) + title[1..];
        var fieldCaption = new Label { Text = title, AutoSize = true, Location = new Point(12, 16), ForeColor = mint, Font = new Font("Segoe UI Semibold", 11F) };
        preview = new Label { Text = target.Text, Location = new Point(120, 10), Size = new Size(520, 32), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(12, 16, 24), ForeColor = Color.White, Font = new Font("Segoe UI", 12F), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        var headerDone = new Button { Text = "✓ Done", Location = new Point(654, 10), Size = new Size(114, 32), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 11F) };
        headerDone.FlatAppearance.BorderSize = 0;
        headerDone.Click += (_, _) => DialogResult = DialogResult.OK;
        AcceptButton = headerDone;

        foreach (var panel in new[] { abcPanel, symPanel, numPanel })
        {
            panel.SetBounds(0, 46, 780, 298);
            panel.BackColor = background;
        }

        BuildAbcPanel();
        BuildSymbolsPanel();
        BuildNumericPanel();

        Controls.AddRange([fieldCaption, preview, headerDone, abcPanel, symPanel, numPanel]);
        abcPanel.Visible = !numericOnly;
        symPanel.Visible = false;
        numPanel.Visible = numericOnly;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        var area = Screen.FromControl(target).WorkingArea;
        Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + area.Height - Height);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var key = keyData & Keys.KeyCode;
        if (key == Keys.Escape) { Close(); return true; }
        if (key == Keys.Enter) { DialogResult = DialogResult.OK; return true; }
        if (key == Keys.Back) { Backspace(); return true; }
        char ch;
        if (key >= Keys.A && key <= Keys.Z) ch = (char)('a' + (key - Keys.A));
        else if (key >= Keys.D0 && key <= Keys.D9) ch = (char)('0' + (key - Keys.D0));
        else if (key >= Keys.NumPad0 && key <= Keys.NumPad9) ch = (char)('0' + (key - Keys.NumPad0));
        else if (key == Keys.Space) ch = ' ';
        else if (key == Keys.OemPeriod || key == Keys.Decimal) ch = '.';
        else if (key == Keys.Oemcomma) ch = ',';
        else return base.ProcessCmdKey(ref msg, keyData);
        if (char.IsLetter(ch) && (Control.ModifierKeys.HasFlag(Keys.Shift) || IsKeyLocked(Keys.CapsLock))) ch = char.ToUpperInvariant(ch);
        if (!numericOnly || char.IsDigit(ch) || ch == '.') Insert(ch);
        return true;
    }

    private static bool IsNumericTarget(TextBox textBox)
    {
        for (Control? parent = textBox.Parent; parent != null; parent = parent.Parent)
            if (parent is NumericUpDown) return true;
        return false;
    }

    private Button MakeKey(Panel panel, string text, int col, int row, int span, Color back, int fontSize = 14)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(8 + col * 76, 8 + row * 56),
            Size = new Size(span * 76 - 4, 52),
            BackColor = back,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", fontSize)
        };
        button.FlatAppearance.BorderSize = 0;
        panel.Controls.Add(button);
        return button;
    }

    private void BuildAbcPanel()
    {
        AddCharRow(abcPanel, "1234567890", 0);
        AddLetterRow(abcPanel, "qwertyuiop", 1);
        AddLetterRow(abcPanel, "asdfghjkl'", 2);

        shiftButton = MakeKey(abcPanel, "⇧", 0, 3, 2, darkKey2, 16);
        shiftButton.Click += (_, _) => ToggleShift();
        var row = "zxcvbnm".ToCharArray();
        for (int i = 0; i < row.Length; i++)
        {
            var character = row[i];
            var button = MakeKey(abcPanel, character.ToString(), 2 + i, 3, 1, darkKey);
            letterKeys.Add((button, character));
            button.Click += (_, _) => InsertLetter(character);
        }
        var back = MakeKey(abcPanel, "⌫", 9, 3, 1, darkKey2, 16);
        back.Click += (_, _) => Backspace();

        BuildFooter(abcPanel, "123", 4, () =>
        {
            abcPanel.Visible = false;
            symPanel.Visible = true;
        });
    }

    private void BuildSymbolsPanel()
    {
        AddCharRow(symPanel, "1234567890", 0);
        AddCharRow(symPanel, "@#$%&*()-_", 1);
        AddCharRow(symPanel, "=+;:\"'~`\\/", 2);
        AddCharRow(symPanel, "!?{}[]<>|", 3);
        var back = MakeKey(symPanel, "⌫", 9, 3, 1, darkKey2, 16);
        back.Click += (_, _) => Backspace();
        BuildFooter(symPanel, "ABC", 4, () =>
        {
            symPanel.Visible = false;
            abcPanel.Visible = true;
        });
    }

    private void BuildNumericPanel()
    {
        var keys = new (string Text, string Value)[] { ("7", "7"), ("8", "8"), ("9", "9"), ("4", "4"), ("5", "5"), ("6", "6"), ("1", "1"), ("2", "2"), ("3", "3"), ("0", "0"), (".", "."), ("⌫", "<") };
        for (int i = 0; i < keys.Length; i++)
        {
            var (text, value) = keys[i];
            var col = i % 3;
            var row = i / 3;
            var button = new Button
            {
                Text = text,
                Location = new Point(8 + col * 256, 8 + row * 56),
                Size = new Size(252, 52),
                BackColor = value == "<" ? darkKey2 : darkKey,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", value == "<" ? 16F : 18F)
            };
            button.FlatAppearance.BorderSize = 0;
            if (value == "<") button.Click += (_, _) => Backspace();
            else button.Click += (_, _) => Insert(value[0]);
            numPanel.Controls.Add(button);
        }
        var clear = new Button { Text = "Clear", Location = new Point(8, 232), Size = new Size(376, 52), BackColor = darkKey2, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 12F) };
        clear.FlatAppearance.BorderSize = 0;
        clear.Click += (_, _) => { target.Clear(); UpdatePreview(); };
        var done = new Button { Text = "✓ Done", Location = new Point(388, 232), Size = new Size(376, 52), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 14F) };
        done.FlatAppearance.BorderSize = 0;
        done.Click += (_, _) => DialogResult = DialogResult.OK;
        numPanel.Controls.AddRange([clear, done]);
    }

    private void AddCharRow(Panel panel, string keys, int row)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            var character = keys[i];
            var button = MakeKey(panel, character.ToString(), i, row, 1, darkKey);
            button.Click += (_, _) => Insert(character);
        }
    }

    private void AddLetterRow(Panel panel, string keys, int row)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            var character = keys[i];
            if (char.IsLetter(character))
            {
                var button = MakeKey(panel, character.ToString(), i, row, 1, darkKey);
                letterKeys.Add((button, character));
                button.Click += (_, _) => InsertLetter(character);
            }
            else
            {
                var button = MakeKey(panel, character.ToString(), i, row, 1, darkKey);
                button.Click += (_, _) => Insert(character);
            }
        }
    }

    private void ToggleShift()
    {
        shift = !shift;
        RefreshLetters();
    }

    private void RefreshLetters()
    {
        shiftButton.BackColor = shift ? mint : darkKey2;
        foreach (var (button, character) in letterKeys)
            button.Text = shift ? char.ToUpperInvariant(character).ToString() : character.ToString();
    }

    private void InsertLetter(char character)
    {
        Insert(shift ? char.ToUpperInvariant(character) : character);
        if (shift)
        {
            shift = false;
            RefreshLetters();
        }
    }

    private void BuildFooter(Panel panel, string switchText, int row, Action onSwitch)
    {
        var switchButton = MakeKey(panel, switchText, 0, row, 1, darkKey2, 11);
        switchButton.Click += (_, _) => onSwitch();
        var comma = MakeKey(panel, ",", 1, row, 1, darkKey);
        comma.Click += (_, _) => Insert(',');
        var dot = MakeKey(panel, ".", 2, row, 1, darkKey);
        dot.Click += (_, _) => Insert('.');
        var space = MakeKey(panel, "space", 3, row, 3, darkKey, 11);
        space.Click += (_, _) => Insert(' ');
        var clear = MakeKey(panel, "Clear", 6, row, 2, darkKey2, 11);
        clear.Click += (_, _) => { target.Clear(); UpdatePreview(); };
        var done = MakeKey(panel, "✓ Done", 8, row, 2, mint, 11);
        done.ForeColor = background;
        done.Click += (_, _) => DialogResult = DialogResult.OK;
    }

    private void Insert(char character)
    {
        if (target.IsDisposed || !target.IsHandleCreated) { Close(); return; }
        target.SelectedText = character.ToString();
        UpdatePreview();
    }

    private void Backspace()
    {
        if (target.IsDisposed || !target.IsHandleCreated) { Close(); return; }
        if (target.SelectionLength > 0) target.SelectedText = "";
        else if (target.SelectionStart > 0)
        {
            var position = target.SelectionStart;
            target.Text = target.Text.Remove(position - 1, 1);
            target.SelectionStart = position - 1;
        }
        UpdatePreview();
    }

    private void UpdatePreview() => preview.Text = target.Text;
}

internal sealed class OperatorPickerForm : Form
{
    public event Action<OperatorAccount>? OperatorSelected;
    private readonly List<OperatorAccount> operators;
    private readonly TextBox passwordBox = new();
    private OperatorAccount? pendingOperator;

    public OperatorPickerForm(Color background, Color panelLight, Color mint, List<OperatorAccount> operators)
    {
        this.operators = operators;
        Text = "Choose operator";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(430, 420);
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        AutoScroll = true;
        AutoScrollMinSize = new Size(430, 420);
        var title = new Label { Text = "Who is using the register?", AutoSize = true, Font = new Font("Segoe UI Semibold", 17F), Location = new Point(26, 24) };
        var list = new FlowLayoutPanel { Location = new Point(26, 72), Size = new Size(378, 200), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        foreach (var op in operators)
        {
            var button = new Button { Text = op.Name, Size = new Size(350, 44), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft };
            button.FlatAppearance.BorderSize = 0;
            button.Click += (_, _) => SelectOperator(op);
            list.Controls.Add(button);
        }
        var passwordLabel = new Label { Text = "Password", AutoSize = true, Location = new Point(26, 282), Visible = false };
        passwordLabel.Name = "pwLabel";
        passwordBox.Location = new Point(26, 308);
        passwordBox.Size = new Size(378, 30);
        passwordBox.BackColor = panelLight;
        passwordBox.ForeColor = Color.White;
        passwordBox.BorderStyle = BorderStyle.FixedSingle;
        passwordBox.UseSystemPasswordChar = true;
        passwordBox.Visible = false;
        passwordBox.Name = "pwBox";
        passwordBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) ConfirmLogin(); };
        var loginButton = new Button { Text = "Login", Location = new Point(26, 350), Size = new Size(378, 40), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Visible = false };
        loginButton.FlatAppearance.BorderSize = 0;
        loginButton.Name = "loginBtn";
        loginButton.Click += (_, _) => ConfirmLogin();
        Controls.AddRange([title, list, passwordLabel, passwordBox, loginButton]);
    }

    private void SelectOperator(OperatorAccount op)
    {
        if (string.IsNullOrWhiteSpace(op.Password))
        {
            OperatorSelected?.Invoke(op);
            return;
        }
        pendingOperator = op;
        Controls["pwLabel"]!.Visible = true;
        passwordBox.Visible = true;
        passwordBox.Text = "";
        Controls["loginBtn"]!.Visible = true;
        passwordBox.Focus();
    }

    private void ConfirmLogin()
    {
        if (pendingOperator == null) return;
        if (pendingOperator.Password != passwordBox.Text)
        {
            MessageBox.Show("Incorrect password.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        OperatorSelected?.Invoke(pendingOperator);
    }
}

internal sealed class OperatorManagerForm : Form
{
    private readonly List<OperatorAccount> operators;
    private readonly Action<List<OperatorAccount>> save;
    private readonly ListBox list = new();
    private readonly TextBox name = new();
    private readonly TextBox password = new();
    private readonly FlowLayoutPanel permissions = new();
    private readonly Dictionary<string, CheckBox> permissionBoxes = [];
    private readonly NumericUpDown wage = new();
    private int selectedIndex = -1;
    private static readonly string[] PermissionNames = ["Take payments", "Discounts", "Refunds", "View sales", "Manage products", "Manage rooms", "Manage operators", "Settings", "Cash drawer", "Gift cards", "Purchasing", "Reservations"];

    public OperatorManagerForm(Color background, Color panelLight, Color mint, List<OperatorAccount> source, Action<List<OperatorAccount>> save)
    {
        this.operators = source.Select(op => new OperatorAccount(op.Name, [.. op.Permissions], op.Password, op.Wage)).ToList();
        this.save = save;
        Text = "Manage operators";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 590);
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        AutoScroll = true;
        AutoScrollMinSize = new Size(760, 590);
        var title = new Label { Text = "Operators and permissions", AutoSize = true, Font = new Font("Segoe UI Semibold", 18F), Location = new Point(26, 22) };
        list.Location = new Point(26, 70);
        list.Size = new Size(220, 410);
        list.BackColor = panelLight;
        list.ForeColor = Color.White;
        list.BorderStyle = BorderStyle.None;
        list.SelectedIndexChanged += (_, _) => LoadSelected();
        foreach (var op in operators) list.Items.Add(op.Name);
        var nameLabel = new Label { Text = "Operator name", AutoSize = true, Location = new Point(280, 72) };
        name.Location = new Point(280, 98);
        name.Size = new Size(430, 30);
        name.BackColor = panelLight;
        name.ForeColor = Color.White;
        name.BorderStyle = BorderStyle.FixedSingle;
        var passwordLabel = new Label { Text = "Password (leave empty for no password)", AutoSize = true, ForeColor = Color.FromArgb(143, 154, 171), Location = new Point(280, 140) };
        password.Location = new Point(280, 166);
        password.Size = new Size(430, 30);
        password.BackColor = panelLight;
        password.ForeColor = Color.White;
        password.BorderStyle = BorderStyle.FixedSingle;
        password.UseSystemPasswordChar = true;
        var wageLabel = new Label { Text = "Hourly wage (for schedule labor cost)", AutoSize = true, ForeColor = Color.FromArgb(143, 154, 171), Location = new Point(280, 210) };
        wage.Location = new Point(280, 236);
        wage.Size = new Size(140, 30);
        wage.BackColor = panelLight;
        wage.ForeColor = Color.White;
        wage.DecimalPlaces = 2;
        wage.Minimum = 0;
        wage.Maximum = 1000;
        var permissionsLabel = new Label { Text = "Permissions", AutoSize = true, Font = new Font("Segoe UI Semibold", 12F), Location = new Point(280, 274) };
        permissions.Location = new Point(280, 304);
        permissions.Size = new Size(430, 186);
        permissions.FlowDirection = FlowDirection.TopDown;
        permissions.WrapContents = false;
        permissions.AutoScroll = true;
        permissions.HorizontalScroll.Enabled = false;
        foreach (var permission in PermissionNames)
        {
            var check = new CheckBox { Text = permission, AutoSize = true, ForeColor = Color.White, Margin = new Padding(0, 4, 0, 4) };
            permissionBoxes[permission] = check;
            permissions.Controls.Add(check);
        }
        var add = ManagerButton("New operator", 26, 500, 130, panelLight);
        add.Click += (_, _) => NewOperator();
        var remove = ManagerButton("Delete", 166, 500, 80, panelLight);
        remove.Click += (_, _) => DeleteOperator();
        var saveButton = ManagerButton("Save changes", 580, 500, 130, mint);
        saveButton.ForeColor = background;
        saveButton.Click += (_, _) => SaveSelected();
        Controls.AddRange([title, list, nameLabel, name, passwordLabel, password, wageLabel, wage, permissionsLabel, permissions, add, remove, saveButton]);
        if (operators.Count > 0) list.SelectedIndex = 0;
    }

    private Button ManagerButton(string text, int x, int y, int width, Color color) => new() { Text = text, Location = new Point(x, y), Size = new Size(width, 38), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

    private void LoadSelected()
    {
        if (selectedIndex >= 0 && selectedIndex < operators.Count && selectedIndex != list.SelectedIndex)
        {
            var selectedPermissions = permissionBoxes.Where(pair => pair.Value.Checked).Select(pair => pair.Key).ToList();
            operators[selectedIndex] = operators[selectedIndex] with { Name = name.Text.Trim(), Permissions = selectedPermissions, Password = password.Text, Wage = wage.Value };
            list.Items[selectedIndex] = operators[selectedIndex].Name;
        }
        if (list.SelectedIndex < 0 || list.SelectedIndex >= operators.Count) return;
        selectedIndex = list.SelectedIndex;
        var op = operators[selectedIndex];
        name.Text = op.Name;
        password.Text = op.Password;
        wage.Value = Math.Clamp(op.Wage, wage.Minimum, wage.Maximum);
        foreach (var permission in PermissionNames)
            permissionBoxes[permission].Checked = op.Permissions.Contains("All permissions") || op.Permissions.Contains(permission);
    }

    private void NewOperator()
    {
        operators.Add(new OperatorAccount("New operator", []));
        list.Items.Add("New operator");
        list.SelectedIndex = operators.Count - 1;
    }

    private void DeleteOperator()
    {
        if (selectedIndex < 0 || operators.Count <= 1) return;
        operators.RemoveAt(selectedIndex);
        list.Items.RemoveAt(selectedIndex);
        list.SelectedIndex = Math.Min(selectedIndex, operators.Count - 1);
    }

    private void SaveSelected()
    {
        if (selectedIndex < 0) return;
        var selectedPermissions = permissionBoxes.Where(pair => pair.Value.Checked).Select(pair => pair.Key).ToList();
        operators[selectedIndex] = operators[selectedIndex] with { Name = name.Text.Trim(), Permissions = selectedPermissions, Password = password.Text, Wage = wage.Value };
        list.Items[selectedIndex] = operators[selectedIndex].Name;
        save(operators);
    }
}

internal sealed class SettingsForm : Form
{
    private FeatureSettings features;
    private readonly Action<FeatureSettings> saveFeatures;
    private readonly Dictionary<string, Button> toggleButtons = new();
    private readonly Panel contentPanel;
    private readonly Panel catPanel;
    private Color _bg, _pl, _mint;
    private Action? _mOps, _mProd, _mFam, _mRooms, _showSales, _mCur, _mPrint, _endDay, _showNet, _showMod, _showCust, _showHappy, _showAudit, _showShift, _showCash, _showEmail, _showCourseNames, _showComboDeals, _showKiosk, _showCustDisp, _showFees, _showFind, _showSpecial, _showOfflineQ, _showVt;
    private Action<string>? _report;
    private Action? _onDone;
    private bool _canOps, _canProd, _canRooms, _canSales;

    public SettingsForm(Color background, Color panelLight, Color mint, Action manageOperators, Action manageProducts, Action manageFamilies, Action manageRooms, Action showSales, Action manageCurrency, Action managePrinters, Action endDay, Action showNetwork, bool canManageOperators, bool canManageProducts, bool canManageRooms, bool canViewSales, FeatureSettings features, Action<FeatureSettings> saveFeatures, Action? showModifierManager = null, Action? showCustomerManager = null, Action? showHappyHourManager = null, Action? showAuditLog = null, Action? showShiftReport = null, Action? showCashDrawer = null, Action? editFloorPlan = null, Action? showEmailSettings = null, Action? showCourseNames = null, Action? showComboDeals = null, Action? showKiosk = null, Action? showCustDisplay = null, Action? showFeeSettings = null, Action? showFindReceipt = null, Action? showSpecialOrders = null, Action? showOfflineQueue = null, Action? showVirtualTerminal = null, Action<string>? reportAction = null, Action? onDone = null)
    {
        this.features = features;
        this.saveFeatures = saveFeatures;
        _bg = background; _pl = panelLight; _mint = mint;
        _mOps = manageOperators; _mProd = manageProducts; _mFam = manageFamilies; _mRooms = manageRooms;
        _showSales = showSales; _mCur = manageCurrency; _mPrint = managePrinters; _endDay = endDay; _showNet = showNetwork;
        _canOps = canManageOperators; _canProd = canManageProducts; _canRooms = canManageRooms; _canSales = canViewSales;
        _showMod = showModifierManager; _showCust = showCustomerManager; _showHappy = showHappyHourManager;
        _showAudit = showAuditLog; _showShift = showShiftReport; _showCash = showCashDrawer;
        _showEmail = showEmailSettings;
        _showCourseNames = showCourseNames;
        _showComboDeals = showComboDeals;
        _showKiosk = showKiosk;
        _showCustDisp = showCustDisplay;
        _showFees = showFeeSettings;
        _showFind = showFindReceipt;
        _showSpecial = showSpecialOrders;
        _showOfflineQ = showOfflineQueue;
        _showVt = showVirtualTerminal;
        _report = reportAction;
        _onDone = onDone;
        Text = "Settings";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(700, 600);
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);

        catPanel = new Panel { Dock = DockStyle.Left, Width = 200, BackColor = Color.FromArgb(18, 24, 36), Padding = new Padding(12, 16, 12, 16) };
        var catTitle = new Label { Text = "Settings", AutoSize = true, Font = new Font("Segoe UI Semibold", 14F), ForeColor = mint, Location = new Point(12, 8) };
        catPanel.Controls.Add(catTitle);
        contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = background, Padding = new Padding(20, 20, 20, 10), AutoScroll = true };

        var categories = new Dictionary<string, (string label, Color color)>();
        void AddCat(string key, string label) => categories[key] = (label, panelLight);

        AddCat("management", "Management");
        AddCat("reports", "Reports");
        AddCat("network", "Network");
        AddCat("ordering", "Ordering & Table");
        AddCat("payment", "Payment & Pricing");
        AddCat("staff", "Staff");
        AddCat("inventory", "Inventory & Stock");
        AddCat("customers", "Customers");
        AddCat("operations", "Operations");
        AddCat("receipts", "Receipts & Display");

        int catY = 44;
        foreach (var kv in categories)
        {
            var btn = new Button { Text = kv.Value.label, Size = new Size(176, 36), Location = new Point(12, catY), BackColor = Color.FromArgb(30, 38, 52), ForeColor = Color.FromArgb(160, 170, 185), FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
            btn.FlatAppearance.BorderSize = 0;
            btn.Tag = kv.Key;
            btn.Click += (_, _) => ShowCategory(kv.Key, background, panelLight, mint, manageOperators, manageProducts, manageFamilies, manageRooms, showSales, manageCurrency, managePrinters, endDay, showNetwork, canManageOperators, canManageProducts, canManageRooms, canViewSales, showModifierManager, showCustomerManager, showHappyHourManager, showAuditLog, showShiftReport, showCashDrawer, editFloorPlan);
            catPanel.Controls.Add(btn);
            catY += 42;
        }

        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 90, BackColor = Color.FromArgb(18, 24, 36) };
        var closeBtn = new Button { Text = "Done", Size = new Size(176, 38), Location = new Point(12, 8), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        closeBtn.FlatAppearance.BorderSize = 0;
        closeBtn.Click += (_, _) =>
        {
            if (_onDone != null) _onDone();
            else DialogResult = DialogResult.OK;
        };
        var exitBtn = new Button { Text = "Exit app", Size = new Size(176, 36), Location = new Point(12, 50), BackColor = Color.FromArgb(160, 40, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        exitBtn.FlatAppearance.BorderSize = 0;
        exitBtn.Click += (_, _) => Application.Exit();
        bottomPanel.Controls.AddRange([closeBtn, exitBtn]);
        catPanel.Controls.Add(bottomPanel);

        Controls.Add(contentPanel);
        Controls.Add(catPanel);
        ShowMainOverview();
    }

    private void ShowMainOverview()
    {
        foreach (Control c in catPanel.Controls) { if (c is Button b && b.Tag is string) b.BackColor = Color.FromArgb(30, 38, 52); }
        contentPanel.Controls.Clear();
        var title = new Label { Text = "Settings", AutoSize = true, Font = new Font("Segoe UI Semibold", 16F), ForeColor = _mint, Location = new Point(0, 4) };
        contentPanel.Controls.Add(title);
        int y = 48;
        void AddBtn(string lbl, Action onClick, bool visible = true)
        {
            var btn = new Button { Text = lbl, Size = new Size(450, 36), BackColor = _pl, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Visible = visible };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (_, _) => onClick();
            btn.Location = new Point(0, y); contentPanel.Controls.Add(btn); y += 44;
        }
        AddBtn("Manage operators", _mOps!, _canOps);
        AddBtn("Manage products", _mProd!, _canProd);
        AddBtn("Manage families", _mFam!, _canProd);
        AddBtn("Room management", _mRooms!, _canRooms);
        AddBtn("Printer routing", _mPrint!);
        AddBtn("Currency and TVA", _mCur!);
        AddBtn("Sales details", _showSales!, _canSales);
        AddBtn("End day (X / Z)", _endDay!);
        AddBtn("Network settings", _showNet!);
    }

    private Button MakeToggle(string label, string key, bool current, Color onColor, Color offColor, Action onToggle)
    {
        var btn = new Button { Text = label, Size = new Size(450, 38), BackColor = current ? onColor : offColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) };
        btn.FlatAppearance.BorderSize = 0;
        btn.Tag = current;
        toggleButtons[key] = btn;
        btn.Click += (_, _) =>
        {
            var isOn = (bool)btn.Tag;
            var newState = !isOn;
            btn.Tag = newState;
            btn.BackColor = newState ? onColor : offColor;
            var f = new FeatureSettings();
            foreach (var pi in typeof(FeatureSettings).GetProperties())
                if (pi.CanWrite && pi.GetValue(features) is not null) pi.SetValue(f, pi.GetValue(features));
            foreach (var kv in toggleButtons)
            {
                var pi = typeof(FeatureSettings).GetProperty(kv.Key);
                if (pi != null && kv.Value.Tag is bool bv) pi.SetValue(f, bv);
            }
            saveFeatures(f);
            features = f;
        };
        return btn;
    }

    private void ShowCategory(string key, Color bg, Color pl, Color mint, Action mOps, Action mProd, Action mFam, Action mRooms, Action showSales, Action mCur, Action mPrint, Action endDay, Action showNet, bool canOps, bool canProd, bool canRooms, bool canSales, Action? showMod, Action? showCust, Action? showHappy, Action? showAudit, Action? showShift, Action? showCash, Action? editFloorPlan = null)
    {
        contentPanel.Controls.Clear();
        foreach (Control c in catPanel.Controls) { if (c is Button b && b.Tag is string s) b.BackColor = s == key ? Color.FromArgb(50, 60, 80) : Color.FromArgb(30, 38, 52); }
        var backBtn = new Button { Text = "\u2190 Back", Size = new Size(100, 32), Location = new Point(0, 0), BackColor = Color.FromArgb(30, 38, 52), ForeColor = Color.FromArgb(160, 170, 185), FlatStyle = FlatStyle.Flat };
        backBtn.FlatAppearance.BorderSize = 0;
        backBtn.Click += (_, _) => ShowMainOverview();
        contentPanel.Controls.Add(backBtn);
        var title = new Label { Text = key switch { "management" => "Management", "reports" => "Reports", "network" => "Network", "ordering" => "Ordering & Table Service", "payment" => "Payment & Pricing", "staff" => "Staff", "inventory" => "Inventory & Stock", "customers" => "Customers", "operations" => "Operations", "receipts" => "Receipts & Display", _ => "Settings" }, AutoSize = true, Font = new Font("Segoe UI Semibold", 16F), Location = new Point(0, 40) };
        contentPanel.Controls.Add(title);
        int y = 80;
        void AddControl(Control c) { c.Location = new Point(0, y); contentPanel.Controls.Add(c); y += c.Height + 8; }
        void AddToggle(string k, string lbl) { AddControl(MakeToggle(lbl, k, (bool)(typeof(FeatureSettings).GetProperty(k)?.GetValue(features) ?? false), Color.FromArgb(40, 170, 90), Color.FromArgb(170, 50, 50), () => { })); }
        void AddBtn(string lbl, Action onClick, bool visible = true)
        {
            var btn = new Button { Text = lbl, Size = new Size(450, 36), BackColor = pl, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Visible = visible };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (_, _) => onClick();
            btn.Location = new Point(0, y); contentPanel.Controls.Add(btn); y += 44;
        }
        switch (key)
        {
            case "management":
                AddToggle("OnScreenKeyboard", "On-screen touch keyboard (text fields)");
                AddBtn("Manage operators", mOps, canOps);
                AddBtn("Manage products", mProd, canProd);
                AddBtn("Manage families", mFam, canProd);
                AddBtn("Room management", mRooms, canRooms);
                AddBtn("Printer routing", mPrint);
                AddBtn("Currency and TVA", mCur);
                break;
            case "reports":
                AddBtn("Sales details", showSales, canSales);
                AddBtn("End day (X / Z)", endDay);
                AddToggle("LiveDashboard", "Live dashboard + hourly graph");
                AddBtn("Live dashboard", () => _report?.Invoke("LiveDash"), features.LiveDashboard);
                AddToggle("CloseOfDay", "Close-of-day screen (totals by method)");
                AddBtn("Close-of-day screen", () => _report?.Invoke("CloseDay"), features.CloseOfDay);
                AddToggle("DiscountVoidReport", "Discount & void frequency");
                AddBtn("Discount & void frequency", () => _report?.Invoke("DiscVoid"), features.DiscountVoidReport);
                AddToggle("OccupancyHeatmap", "Occupancy heatmap + dayparts");
                AddBtn("Occupancy heatmap", () => _report?.Invoke("Heatmap"), features.OccupancyHeatmap);
                AddToggle("MenuEngineering", "Menu engineering matrix");
                AddBtn("Menu engineering matrix", () => _report?.Invoke("MenuEng"), features.MenuEngineering);
                AddToggle("AccountingExport", "Accounting export (QuickBooks / Xero)");
                AddBtn("Accounting export CSV", () => _report?.Invoke("AcctExport"), features.AccountingExport);
                AddToggle("AiInsights", "AI insights (auto analysis)");
                AddBtn("AI insights", () => _report?.Invoke("Insights"), features.AiInsights);
                AddBtn("KDS prep-time analytics", () => _report?.Invoke("PrepAnalytics"), features.KdsLateAlarm);
                break;
            case "network":
                AddBtn("Network settings", showNet);
                AddToggle("CloudBackup", "Cloud backup (daily zip to a folder you trust)");
                AddBtn("Backup & restore", () => _report?.Invoke("Backup"), features.CloudBackup);
                AddToggle("ApiEnabled", "REST API for external systems (POS → ERP / BI)");
                AddBtn("API key & webhooks", () => _report?.Invoke("ApiCfg"), features.ApiEnabled);
                AddToggle("CloudSync", "Cloud sync (share sales with a head-office server)");
                AddBtn("Cloud sync settings", () => _report?.Invoke("CloudSync"), features.CloudSync);
                break;
            case "ordering":
                AddToggle("SplitBill", "Split bill (by seat or item)");
                AddToggle("TableTransfer", "Table transfer / merge");
                AddToggle("Modifiers", "Product modifiers (extras, size, options)");
                AddBtn("Manage modifier groups", showMod!, features.Modifiers);
                AddToggle("ItemNotes", "Special instructions per item");
                AddToggle("CourseManagement", "Course-by-course sending to kitchen");
                AddBtn("Edit course names (Course 1 = ...)", _showCourseNames!, features.CourseManagement);
                AddToggle("HoldFire", "Hold fire — kitchen only gets courses when you press Fire");
                AddToggle("Reservations", "Reservations / bookings (floor plan badges)");
                AddToggle("KitchenDisplay", "Kitchen display screen (KDS)");
                AddToggle("KdsLateAlarm", "KDS late alarm (beep & flash past target prep time)");
                AddToggle("EightySixBoard", "86 board (mark products out of stock)");
                AddToggle("SeatOrdering", "Seat-level ordering (items assigned to seats)");
                AddToggle("FloorPlanEditor", "Floor plan editor (drag & drop, rename tables)");
                AddBtn("Edit floor plan (opens Rooms view)", () => editFloorPlan?.Invoke(), features.FloorPlanEditor && canRooms);
                AddToggle("ParkOrders", "Park / hold orders & recall later");
                AddToggle("RepeatOrders", "Repeat last order & customer usual (2 taps)");
                AddToggle("ComboDeals", "Combo meals / deal bundles");
                AddBtn("Manage combo deals", _showComboDeals!, features.ComboDeals);
                AddToggle("QuickSaleMode", "Quick-sale / retail mode (counter button)");
                break;
            case "payment":
                AddToggle("Discounts", "Discounts (% or fixed amount)");
                AddToggle("VoidsComps", "Voids & comp meals");
                AddToggle("HappyHour", "Happy hour / timed pricing");
                AddBtn("Manage happy hour rules", showHappy!, features.HappyHour);
                AddToggle("Tips", "Tip management");
                AddToggle("MultiRatePricing", "Multi-rate (dine-in / takeaway / delivery)");
                AddToggle("CashDrawer", "Cash drawer management");
                AddBtn("Open cash drawer", showCash!, features.CashDrawer);
                AddToggle("CashRounding", "Round cash payments to nearest 5");
                AddToggle("GiftCards", "Gift cards & store credit");
                AddToggle("EGiftEmail", "E-gift card email delivery");
                AddToggle("ServiceCharge", "Service charge %");
                AddToggle("CashDiscount", "Cash discount % (cash pays less)");
                AddToggle("CardSurcharge", "Card surcharge %");
                AddBtn("Fees & terminal settings", _showFees!, features.ServiceCharge || features.CashDiscount || features.CardSurcharge || features.OfflinePayments);
                AddToggle("ChargeTabs", "Tabs / charge accounts with credit limit");
                AddToggle("Layaway", "Deposits & special orders (layaway)");
                AddBtn("Special orders / deposits", _showSpecial!, features.Layaway);
                AddToggle("OfflinePayments", "Offline card payments (queue while terminal down)");
                AddBtn("Offline payment queue", _showOfflineQ!, features.OfflinePayments);
                AddToggle("VirtualTerminal", "Virtual terminal (phone orders, invoice links)");
                AddBtn("Virtual terminal", _showVt!, features.VirtualTerminal);
                break;
            case "staff":
                AddToggle("ClockInOut", "Clock in / out");
                AddToggle("ShiftReports", "Shift reports (Rapport Z)");
                AddBtn("View shift reports", showShift!, features.ShiftReports);
                AddToggle("AuditLog", "Audit log (track all actions)");
                AddBtn("View audit log", showAudit!, features.AuditLog);
                AddToggle("Scheduling", "Staff scheduling + tip pooling");
                AddToggle("TrainingMode", "Training mode (sales not recorded)");
                AddToggle("EmployeeDiscount", "Employee discount %");
                AddBtn("Set employee discount %", () => _report?.Invoke("EmpPct"), features.EmployeeDiscount);
                AddToggle("PinLogin", "PIN login (numeric login at startup)");
                AddToggle("PayrollExport", "Payroll export (hours x wage CSV)");
                AddBtn("Export payroll CSV", () => _report?.Invoke("Payroll"), features.PayrollExport);
                AddToggle("ServerPerformance", "Server performance report");
                AddBtn("Server performance", () => _report?.Invoke("ServerPerf"), features.ServerPerformance);
                break;
            case "inventory":
                AddToggle("StockManagement", "Ingredient stock / recipe deduction");
                AddToggle("InventoryAlerts", "Low stock alerts");
                AddToggle("Auto86", "Auto-86 when ingredients hit reorder point");
                AddToggle("Stocktakes", "Stock counts (stocktake) with audit trail");
                AddToggle("WasteTracking", "Waste & expiry tracking");
                AddToggle("StockTransfers", "Stock transfers between locations");
                AddToggle("WeightedItems", "Weight-priced items (price per kg, scale entry)");
                AddToggle("ProductVariants", "Product variants (size / color) + CSV import");
                AddToggle("BarcodeLabels", "Barcode label printing (Code 39)");
                AddToggle("BarcodeScanning", "Barcode scanning");
                AddToggle("Purchasing", "Purchase orders, suppliers & food cost");
                break;
            case "customers":
                AddToggle("CustomerDatabase", "Customer database");
                AddBtn("Manage customers", showCust!, features.CustomerDatabase);
                AddToggle("Loyalty", "Loyalty program (points & rewards)");
                break;
            case "operations":
                AddToggle("DeliveryOrders", "Delivery orders");
                AddToggle("MultiBranch", "Multi-branch support");
                AddBtn("Manage branches & current branch", () => _report?.Invoke("Branches"), features.MultiBranch);
                AddToggle("Refunds", "Refunds (refund ticket, restock, negative sale)");
                AddToggle("QROrdering", "QR code self-ordering (customer phone scan)");
                AddBtn("Guest ordering URL & QR code", () => _report?.Invoke("GuestUrl"), features.QROrdering || features.KioskMode);
                AddToggle("KioskMode", "Kiosk self-order mode (fullscreen, pay at counter)");
                AddBtn("Launch kiosk", _showKiosk!, features.KioskMode);
                AddToggle("CustomerDisplay", "Customer-facing display (2nd screen)");
                AddBtn("Customer display", _showCustDisp!, features.CustomerDisplay);
                break;
            case "receipts":
                AddToggle("EmailReceipts", "Email / SMS receipts");
                AddBtn("Email / SMTP settings", _showEmail!, features.EmailReceipts);
                AddToggle("ReceiptSearch", "Find, reprint & reopen any past receipt");
                AddBtn("Find past receipts", _showFind!, features.ReceiptSearch);
                AddToggle("MultiLanguage", "Multi-language (FR / AR / EN)");
                break;
        }
    }
}

internal sealed class KitchenDisplayForm : Form
{
    private readonly PosStore store;
    private readonly Color panelLight;
    private readonly Color mint;
    private readonly FlowLayoutPanel tickets = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(6) };
    private readonly System.Windows.Forms.Timer tick = new() { Interval = 1000 };
    private readonly List<(OpenOrder Order, Label Timer)> cards = [];
    private bool stopped;
    private bool wasAttached;
    private readonly HashSet<(int Order, long Created)> alarmed = [];
    private bool flashPhase;

    public KitchenDisplayForm(Color background, Color panelLight, Color mint, PosStore store)
    {
        this.store = store;
        this.panelLight = panelLight;
        this.mint = mint;
        BackColor = background;
        ForeColor = Color.White;
        Controls.Add(tickets);
        Render();
        store.DataChanged += OnStoreChanged;
        tick.Tick += (_, _) => OnTick();
        tick.Start();
        HandleDestroyed += (_, _) => Stop();
    }

    private void Stop()
    {
        if (stopped) return;
        stopped = true;
        tick.Stop();
        tick.Dispose();
        store.DataChanged -= OnStoreChanged;
    }

    private bool CheckAttached()
    {
        if (stopped || IsDisposed) return false;
        if (Parent != null) { wasAttached = true; return true; }
        if (wasAttached) { Stop(); return false; }
        return false;
    }

    private void OnStoreChanged()
    {
        if (!CheckAttached()) return;
        if (!IsHandleCreated) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(Render); }
            catch (InvalidOperationException) { Stop(); }
            return;
        }
        Render();
    }

    private void OnTick()
    {
        if (!CheckAttached()) return;
        flashPhase = !flashPhase;
        foreach (var (order, timer) in cards) UpdateTimer(order, timer);
        if (store.Features.KdsLateAlarm)
        {
            foreach (var (order, _) in cards)
            {
                if (order.Ready) continue;
                if ((DateTime.Now - order.Created).TotalMinutes >= store.Features.KdsPrepMinutes)
                {
                    if (alarmed.Add((order.OrderNumber, order.Created.Ticks)))
                    {
                        try { Console.Beep(950, 350); } catch { }
                    }
                }
            }
            var live = store.OpenOrders.Select(o => (o.OrderNumber, o.Created.Ticks)).ToHashSet();
            foreach (var key in alarmed.Where(k => !live.Contains(k)).ToList()) alarmed.Remove(key);
        }
    }

    private void UpdateTimer(OpenOrder order, Label timer)
    {
        var elapsed = DateTime.Now - order.Created;
        timer.Text = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        var late = store.Features.KdsLateAlarm && !order.Ready && elapsed.TotalMinutes >= store.Features.KdsPrepMinutes;
        timer.ForeColor = order.Ready ? Color.FromArgb(130, 140, 155)
            : late ? (flashPhase ? Color.FromArgb(255, 70, 70) : Color.FromArgb(255, 200, 90))
            : elapsed.TotalMinutes < 10 ? Color.FromArgb(110, 210, 145)
            : elapsed.TotalMinutes < 20 ? Color.FromArgb(245, 180, 80)
            : Color.FromArgb(235, 90, 90);
        var header = timer.Parent as Panel;
        if (header != null)
            header.BackColor = late && flashPhase ? Color.FromArgb(155, 45, 45)
                : order.Ready ? Color.FromArgb(46, 100, 74) : panelLight;
    }

    private void Render()
    {
        cards.Clear();
        tickets.SuspendLayout();
        tickets.Controls.Clear();
        var orders = store.OpenOrders.OrderBy(o => o.Ready ? 1 : 0).ThenBy(o => o.Created).ToList();
        if (orders.Count == 0)
            tickets.Controls.Add(new Label { Text = "No active orders", Font = new Font("Segoe UI Semibold", 16F), ForeColor = Color.FromArgb(143, 154, 171), AutoSize = true, Margin = new Padding(14, 24, 0, 0) });
        foreach (var order in orders) tickets.Controls.Add(BuildCard(order));
        tickets.ResumeLayout();
    }

    private IEnumerable<(string Text, bool IsSub)> ItemLines(OpenOrder order)
    {
        var courseOn = store.Features.CourseManagement;
        var visibleItems = courseOn ? order.Items.Where(i => i.Course == 0 || i.Course <= order.FiredCourse) : order.Items;
        foreach (var g in visibleItems.GroupBy(i => (i.Product, i.Note, i.Modifiers, i.Seat, i.Course)))
        {
            var seat = g.Key.Seat > 0 ? $"[S{g.Key.Seat}] " : "";
            var course = courseOn && g.Key.Course > 0 ? TicketLayout.CourseTag(g.Key.Course) : "";
            yield return ($"{seat}{course}{g.Sum(x => x.Quantity)}×  {g.Key.Product}", false);
            if (!string.IsNullOrWhiteSpace(g.Key.Modifiers)) yield return ($"+ {g.Key.Modifiers}", true);
            if (!string.IsNullOrWhiteSpace(g.Key.Note)) yield return ($"• {g.Key.Note}", true);
        }
    }

    private int UnfiredCount(OpenOrder order) =>
        store.Features.CourseManagement ? order.Items.Count(i => i.Course > order.FiredCourse) : 0;

    private Panel BuildCard(OpenOrder order)
    {
        var back = order.Ready ? Color.FromArgb(36, 72, 55) : Color.FromArgb(37, 47, 62);
        var head = order.Ready ? Color.FromArgb(46, 100, 74) : panelLight;
        var card = new Panel { Size = new Size(280, 340), Margin = new Padding(8), BackColor = back };

        var title = new Label { Text = order.OrderNumber > 0 ? $"#{order.OrderNumber}  {order.Table}" : order.Table, Font = new Font("Segoe UI Semibold", 13F), ForeColor = Color.White, AutoSize = true, Location = new Point(10, 8) };
        var unfired = UnfiredCount(order);
        var sub = new Label { Text = $"{order.Room}  ·  {order.OrderType}{(order.Source.Length > 0 ? $"  ·  {order.Source}" : "")}  ·  {order.Items.Sum(i => i.Quantity)} items{(unfired > 0 ? $"  ·  {unfired} waiting fire" : "")}", Font = new Font("Segoe UI", 9F), ForeColor = unfired > 0 ? Color.FromArgb(245, 180, 80) : Color.FromArgb(170, 180, 195), AutoSize = true, Location = new Point(11, 34) };
        var timer = new Label { Font = new Font("Segoe UI Semibold", 15F), TextAlign = ContentAlignment.MiddleRight, Location = new Point(174, 10), Size = new Size(96, 44) };
        var header = new Panel { Location = new Point(0, 0), Size = new Size(280, 64), BackColor = head };
        header.Controls.AddRange([title, sub, timer]);

        var body = new FlowLayoutPanel { Location = new Point(0, 64), Size = new Size(280, 234), AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = back, Padding = new Padding(10, 6, 10, 6) };
        foreach (var (text, isSub) in ItemLines(order))
        {
            body.Controls.Add(new Label { Text = text, AutoSize = true, Font = isSub ? new Font("Segoe UI", 9F) : new Font("Segoe UI", 10.5F), ForeColor = isSub ? Color.FromArgb(165, 175, 190) : Color.White, Margin = new Padding(isSub ? 16 : 0, 1, 0, isSub ? 5 : 2) });
        }

        var action = new Button { Text = order.Ready ? "↩  Put back" : "✓  READY", Location = new Point(0, 298), Size = new Size(280, 42), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 11F), BackColor = order.Ready ? Color.FromArgb(80, 90, 110) : mint, Cursor = Cursors.Hand };
        action.FlatAppearance.BorderSize = 0;
        action.Click += (_, _) => ToggleReady(order);

        card.Controls.AddRange([body, header, action]);
        UpdateTimer(order, timer);
        cards.Add((order, timer));
        return card;
    }

    private void ToggleReady(OpenOrder order)
    {
        var current = store.GetOpenOrder(order.Room, order.Table);
        if (current == null) { Render(); return; }
        store.RemoveOpenOrder(order.Room, order.Table);
        store.AddOpenOrder(current with { Ready = !current.Ready });
        if (!current.Ready)
        {
            var mins = (int)Math.Round((DateTime.Now - current.Created).TotalMinutes);
            if (mins >= 0 && mins < 600) store.AddPrepTime(mins);
            alarmed.Remove((current.OrderNumber, current.Created.Ticks));
            store.RaiseGuestAlert(order.Room, order.Table, "ready");
        }
        else store.DismissGuestAlerts(order.Room, order.Table, "ready");
        Render();
    }
}

internal sealed class KioskForm : Form
{
    private readonly List<(Product P, int Qty)> cart = [];
    private readonly List<Product> products;
    private readonly Color background;
    private readonly Color panelLight;
    private readonly Color mint;
    private readonly Func<Product, string> resolvePrinter;
    private readonly Action<List<SoldLine>> onSend;
    private readonly FlowLayoutPanel tiles = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(14) };
    private readonly FlowLayoutPanel tabsFlow = new() { Dock = DockStyle.Top, Height = 54, AutoScroll = false, WrapContents = false, Padding = new Padding(10, 8, 10, 8) };
    private readonly FlowLayoutPanel cartList = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false, FlowDirection = FlowDirection.TopDown, Padding = new Padding(8) };
    private readonly Label totalLabel = new() { Dock = DockStyle.Bottom, Height = 64, Font = new Font("Segoe UI Semibold", 22F), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 16, 0) };
    private readonly Label overlay = new() { Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 24F), ForeColor = Color.FromArgb(126, 240, 199), TextAlign = ContentAlignment.MiddleCenter, Visible = false, BackColor = Color.FromArgb(20, 26, 36) };
    private string currentCategory = "";
    private readonly System.Windows.Forms.Timer hideOverlay = new() { Interval = 5000 };

    public KioskForm(Color background, Color panelLight, Color mint, List<Product> products, Func<Product, string> resolvePrinter, Action<List<SoldLine>> onSend)
    {
        this.background = background;
        this.panelLight = panelLight;
        this.mint = mint;
        this.products = products;
        this.resolvePrinter = resolvePrinter;
        this.onSend = onSend;
        totalLabel.ForeColor = mint;
        Text = "Self ordering";
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 11F);

        hideOverlay.Tick += (_, _) => { overlay.Visible = false; hideOverlay.Stop(); };

        var header = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = Color.FromArgb(28, 36, 52) };
        var title = new Label { Text = "TAP TO ORDER  •  Pay at the counter", AutoSize = true, Font = new Font("Segoe UI Semibold", 18F), ForeColor = mint, Location = new Point(24, 22) };
        var exitBtn = new Button { Text = "Exit kiosk", Dock = DockStyle.Right, Width = 130, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(160, 50, 50), Font = new Font("Segoe UI", 10F) };
        exitBtn.FlatAppearance.BorderSize = 0;
        exitBtn.Click += (_, _) => Close();
        header.Controls.AddRange([title, exitBtn]);

        var cartPanel = new Panel { Dock = DockStyle.Right, Width = 360, BackColor = Color.FromArgb(24, 31, 45), Padding = new Padding(10) };
        var cartTitle = new Label { Text = "Your order", Dock = DockStyle.Top, Height = 40, Font = new Font("Segoe UI Semibold", 14F), ForeColor = Color.FromArgb(143, 154, 171) };
        var sendBtn = new Button { Text = "SEND ORDER TO KITCHEN", Dock = DockStyle.Bottom, Height = 74, FlatStyle = FlatStyle.Flat, BackColor = mint, ForeColor = Color.FromArgb(16, 35, 29), Font = new Font("Segoe UI Semibold", 14F) };
        sendBtn.FlatAppearance.BorderSize = 0;
        sendBtn.Click += (_, _) => SendOrder();
        var clearBtn = new Button { Text = "Clear", Dock = DockStyle.Bottom, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(80, 90, 110), ForeColor = Color.White };
        clearBtn.FlatAppearance.BorderSize = 0;
        clearBtn.Click += (_, _) => { cart.Clear(); RenderCart(); };
        cartPanel.Controls.Add(cartList);
        cartPanel.Controls.Add(totalLabel);
        cartPanel.Controls.Add(clearBtn);
        cartPanel.Controls.Add(sendBtn);
        cartPanel.Controls.Add(cartTitle);

        var center = new Panel { Dock = DockStyle.Fill, BackColor = background };
        center.Controls.Add(tiles);
        center.Controls.Add(tabsFlow);
        center.Controls.Add(overlay);

        Controls.Add(center);
        Controls.Add(cartPanel);
        Controls.Add(header);

        currentCategory = products.Select(p => p.Category).FirstOrDefault() ?? "";
        RenderTabs();
        RenderTiles();
        RenderCart();
    }

    private void RenderTabs()
    {
        tabsFlow.Controls.Clear();
        foreach (var cat in products.Select(p => p.Category).Distinct())
        {
            var c = cat;
            var tab = new Button { Text = c, AutoSize = true, MinimumSize = new Size(110, 38), FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 0), BackColor = c == currentCategory ? mint : panelLight, ForeColor = c == currentCategory ? background : Color.White, Font = new Font("Segoe UI Semibold", 10F) };
            tab.FlatAppearance.BorderSize = 0;
            tab.Click += (_, _) => { currentCategory = c; RenderTabs(); RenderTiles(); };
            tabsFlow.Controls.Add(tab);
        }
    }

    private void RenderTiles()
    {
        tiles.SuspendLayout();
        tiles.Controls.Clear();
        foreach (var p in products.Where(p => p.Category == currentCategory))
        {
            var product = p;
            var tile = new ProductTile(product, 230, 160) { Margin = new Padding(0, 0, 14, 14) };
            tile.OutOfStock = false;
            tile.Click += (_, _) => AddToCart(product);
            tiles.Controls.Add(tile);
        }
        tiles.ResumeLayout();
    }

    private void AddToCart(Product p)
    {
        var idx = cart.FindIndex(c => c.P.Name == p.Name);
        if (idx >= 0) cart[idx] = (cart[idx].P, cart[idx].Qty + 1);
        else cart.Add((p, 1));
        RenderCart();
    }

    private void RenderCart()
    {
        cartList.SuspendLayout();
        cartList.Controls.Clear();
        foreach (var (p, qty) in cart)
        {
            var row = new Panel { Width = 330, Height = 56, BackColor = panelLight, Margin = new Padding(0, 0, 0, 8) };
            var nm = new Label { Text = p.Name, AutoSize = true, Location = new Point(10, 6), Font = new Font("Segoe UI", 9.5F) };
            var pr = new Label { Text = (p.Price * qty).ToString("C"), AutoSize = true, Location = new Point(10, 28), ForeColor = mint, Font = new Font("Segoe UI", 9F) };
            var minus = new Button { Text = "−", Size = new Size(30, 34), Location = new Point(224, 11), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(70, 90, 130), ForeColor = Color.White };
            minus.FlatAppearance.BorderSize = 0;
            minus.Click += (_, _) =>
            {
                var i = cart.FindIndex(c => c.P.Name == p.Name);
                if (i < 0) return;
                if (cart[i].Qty <= 1) cart.RemoveAt(i); else cart[i] = (cart[i].P, cart[i].Qty - 1);
                RenderCart();
            };
            var qLbl = new Label { Text = qty.ToString(), AutoSize = true, Location = new Point(262, 18), Font = new Font("Segoe UI Semibold", 11F) };
            var plus = new Button { Text = "+", Size = new Size(30, 34), Location = new Point(290, 11), FlatStyle = FlatStyle.Flat, BackColor = mint, ForeColor = background };
            plus.FlatAppearance.BorderSize = 0;
            plus.Click += (_, _) => AddToCart(p);
            row.Controls.AddRange([nm, pr, minus, qLbl, plus]);
            cartList.Controls.Add(row);
        }
        totalLabel.Text = cart.Sum(c => c.P.Price * c.Qty).ToString("C");
        cartList.ResumeLayout();
    }

    private void SendOrder()
    {
        if (cart.Count == 0)
        {
            MessageBox.Show("Add something first.", "Kiosk", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var lines = cart
            .SelectMany(c => Enumerable.Repeat(c.P, c.Qty))
            .GroupBy(p => p.Name)
            .Select(g => new SoldLine(g.Key, g.First().Family, g.Count(), g.First().Price, g.First().Category, resolvePrinter(g.First()), "", Seat: 0, Course: 0))
            .ToList();
        onSend(lines);
        cart.Clear();
        RenderCart();
        overlay.Text = $"\u2713  Order sent!\n\nPay at the counter.\n\nOrder # confirmed";
        overlay.Visible = true;
        overlay.BringToFront();
        hideOverlay.Stop();
        hideOverlay.Start();
    }
}

internal sealed class CustomerDisplayForm : Form
{
    private readonly Panel listPanel = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(20, 26, 36), Padding = new Padding(18, 10, 18, 10) };
    private readonly Label contextLabel = new() { Dock = DockStyle.Top, Height = 54, Font = new Font("Segoe UI Semibold", 13F), ForeColor = Color.FromArgb(143, 154, 171), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0), BackColor = Color.FromArgb(24, 31, 45) };
    private readonly Label totalValue = new() { Dock = DockStyle.Bottom, Height = 96, Font = new Font("Segoe UI Semibold", 34F), ForeColor = Color.FromArgb(71, 214, 174), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 22, 0), BackColor = Color.FromArgb(24, 31, 45) };
    private readonly Label totalCaption = new() { Dock = DockStyle.Bottom, Height = 34, Text = "TOTAL", Font = new Font("Segoe UI Semibold", 11F), ForeColor = Color.FromArgb(143, 154, 171), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 24, 0), BackColor = Color.FromArgb(24, 31, 45) };
    private readonly Label thanks = new() { Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 26F), ForeColor = Color.FromArgb(126, 240, 199), TextAlign = ContentAlignment.MiddleCenter, Visible = false, BackColor = Color.FromArgb(20, 26, 36) };
    private readonly System.Windows.Forms.Timer thanksTimer = new() { Interval = 5000 };

    public CustomerDisplayForm(Color background, Color mint)
    {
        Text = "Customer display";
        Size = new Size(520, 780);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(Screen.PrimaryScreen?.Bounds.Width ?? 1280, 80);
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 11F);
        thanksTimer.Tick += (_, _) => { thanks.Visible = false; thanksTimer.Stop(); };
        Controls.Add(listPanel);
        Controls.Add(contextLabel);
        Controls.Add(thanks);
        Controls.Add(totalValue);
        Controls.Add(totalCaption);
        SetOrder("", [], 0m);
    }

    public void SetOrder(string context, List<(string Text, decimal Amount)> rows, decimal total)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { try { BeginInvoke(() => SetOrder(context, rows, total)); } catch { } return; }
        contextLabel.Text = "  " + context;
        listPanel.SuspendLayout();
        listPanel.Controls.Clear();
        foreach (var (text, amount) in rows)
        {
            var row = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(4, 6, 4, 6) };
            var l = new Label { Text = text, AutoSize = false, Size = new Size(330, 32), ForeColor = Color.White, Font = new Font("Segoe UI", 11F) };
            var r = new Label { Text = amount.ToString("C"), AutoSize = false, Size = new Size(130, 32), ForeColor = Color.FromArgb(71, 214, 174), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI Semibold", 11F), Dock = DockStyle.Right };
            row.Controls.Add(l);
            row.Controls.Add(r);
            listPanel.Controls.Add(row);
        }
        listPanel.ResumeLayout();
        totalValue.Text = total.ToString("C");
    }

    public void ShowThankYou(decimal paid)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { try { BeginInvoke(() => ShowThankYou(paid)); } catch { } return; }
        thanks.Text = $"\u2713  PAID\n\n{paid:C}\n\nThank you!";
        thanks.Visible = true;
        thanks.BringToFront();
        thanksTimer.Stop();
        thanksTimer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        thanksTimer.Stop();
        thanksTimer.Dispose();
        base.OnFormClosed(e);
    }
}

internal sealed class CurrencySettingsForm : Form
{
    public CurrencySettingsForm(Color background, Color panelLight, Color mint, RegisterSettings current, Action<RegisterSettings> save)
    {
        Text = "Currency and TVA";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 390);
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        var title = new Label { Text = "Currency and TVA settings", AutoSize = true, Font = new Font("Segoe UI Semibold", 18F), Location = new Point(28, 24) };
        var hint = new Label { Text = "These settings are saved for this register.", AutoSize = true, ForeColor = Color.FromArgb(143, 154, 171), Location = new Point(30, 60) };
        var currencyLabel = new Label { Text = "Currency", AutoSize = true, Location = new Point(30, 105) };
        var currency = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Items = { "EUR  €", "USD  $", "GBP  £", "DZD  د.ج" }, Location = new Point(30, 130), Size = new Size(190, 30), BackColor = panelLight, ForeColor = Color.White };
        currency.SelectedIndex = current.Currency switch { "USD" => 1, "GBP" => 2, "DZD" => 3, _ => 0 };
        var tvaLabel = new Label { Text = "TVA rate (%)", AutoSize = true, Location = new Point(30, 180) };
        var tva = new NumericUpDown { Minimum = 0, Maximum = 100, DecimalPlaces = 2, Increment = 0.5m, Value = current.TvaRate, Location = new Point(30, 205), Size = new Size(120, 30), BackColor = panelLight, ForeColor = Color.White };
        var included = new CheckBox { Text = "Prices already include TVA", Checked = current.PricesIncludeTva, AutoSize = true, Location = new Point(30, 258), ForeColor = Color.White };
        var saveButton = new Button { Text = "Save", Location = new Point(350, 310), Size = new Size(120, 40), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
        saveButton.FlatAppearance.BorderSize = 0;
        saveButton.Click += (_, _) =>
        {
            var code = currency.SelectedIndex switch { 1 => "USD", 2 => "GBP", 3 => "DZD", _ => "EUR" };
            save(current with { Currency = code, TvaRate = tva.Value, PricesIncludeTva = included.Checked });
        };
        Controls.AddRange([title, hint, currencyLabel, currency, tvaLabel, tva, included, saveButton]);
        AcceptButton = saveButton;
    }
}

internal sealed class SalesReportForm : Form
{
    private readonly Color background;
    private readonly Color panelLight;
    private readonly Color mint;
    private readonly List<Sale> allSales;
    private List<Sale> sales;
    private readonly string currency;
    private readonly FlowLayoutPanel dateButtons = new();
    private readonly Button backButton = new();
    private readonly Button printButton = new();
    private readonly Button refundButton = new();
    private readonly Label selectionLabel = new();
    private readonly Label summaryLabel = new();
    private readonly TabControl tabs = new();
    private readonly bool refundsEnabled;
    private readonly Action<Sale>? onRefund;
    private List<Sale> currentFilteredSales = [];
    private Sale? selectedTransaction;
    private int selectedYear;
    private int selectedMonth;

    public SalesReportForm(Color background, Color panelLight, Color mint, List<Sale> sales, string currency, bool refundsEnabled = false, Action<Sale>? onRefund = null, List<string>? branches = null)
    {
        this.background = background;
        this.panelLight = panelLight;
        this.mint = mint;
        allSales = sales;
        this.sales = sales;
        this.currency = currency;
        this.refundsEnabled = refundsEnabled;
        this.onRefund = onRefund;
        Text = "Sales details";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(780, 560);
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(650, 440);
        AutoScroll = true;

        var title = new Label { Text = "Sales details", AutoSize = true, Font = new Font("Segoe UI Semibold", 20F), Location = new Point(24, 20) };
        selectionLabel.AutoSize = true;
        selectionLabel.ForeColor = Color.FromArgb(190, 200, 215);
        selectionLabel.Location = new Point(26, 58);
        summaryLabel.AutoSize = true;
        summaryLabel.ForeColor = mint;
        summaryLabel.Location = new Point(26, 82);
        backButton.Text = "←";
        backButton.Size = new Size(42, 36);
        backButton.Location = new Point(24, 112);
        backButton.BackColor = panelLight;
        backButton.ForeColor = Color.White;
        backButton.FlatStyle = FlatStyle.Flat;
        backButton.FlatAppearance.BorderSize = 0;
        backButton.Font = new Font("Segoe UI Semibold", 16F);
        backButton.Enabled = false;
        backButton.Click += (_, _) => GoBack();
        printButton.Text = "Print";
        printButton.Size = new Size(80, 36);
        printButton.Location = new Point(76, 112);
        printButton.BackColor = panelLight;
        printButton.ForeColor = Color.White;
        printButton.FlatStyle = FlatStyle.Flat;
        printButton.FlatAppearance.BorderSize = 0;
        printButton.Font = new Font("Segoe UI Semibold", 10F);
        printButton.Click += (_, _) => PrintReport();
        refundButton.Text = "Refund";
        refundButton.Size = new Size(100, 36);
        refundButton.Location = new Point(655, 18);
        refundButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        refundButton.BackColor = Color.FromArgb(180, 90, 60);
        refundButton.ForeColor = Color.White;
        refundButton.FlatStyle = FlatStyle.Flat;
        refundButton.FlatAppearance.BorderSize = 0;
        refundButton.Font = new Font("Segoe UI Semibold", 10F);
        refundButton.Visible = refundsEnabled && onRefund != null;
        refundButton.Enabled = false;
        refundButton.Click += (_, _) =>
        {
            if (selectedTransaction != null && onRefund != null) onRefund(selectedTransaction);
        };
        dateButtons.Location = new Point(24, 112);
        dateButtons.Padding = new Padding(130, 0, 0, 0);
        dateButtons.Size = new Size(720, 55);
        dateButtons.WrapContents = false;
        dateButtons.AutoScroll = true;
        tabs.Location = new Point(24, 180);
        tabs.Size = new Size(720, 305);
        tabs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        Controls.AddRange([title, selectionLabel, summaryLabel, backButton, printButton, refundButton, dateButtons, tabs]);
        if (branches is { Count: > 0 })
        {
            var branchLbl = new Label { Text = "Branch:", AutoSize = true, Location = new Point(470, 26), ForeColor = Color.FromArgb(190, 200, 215) };
            var branchCombo = new ComboBox { Location = new Point(525, 22), Size = new Size(120, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
            branchCombo.Items.Add("All");
            foreach (var b in branches) branchCombo.Items.Add(b);
            branchCombo.SelectedIndex = 0;
            branchCombo.SelectedIndexChanged += (_, _) =>
            {
                var sel = branchCombo.SelectedItem?.ToString() ?? "All";
                sales = sel == "All" ? allSales : allSales.Where(s => s.Branch == sel).ToList();
                ShowYears();
            };
            Controls.AddRange([branchLbl, branchCombo]);
        }
        ShowYears();
    }

    private void UpdateRefundButton() => refundButton.Enabled = selectedTransaction is { IsRefund: false, Items.Count: > 0 } && onRefund != null;

    private void ShowYears()
    {
        selectedYear = 0;
        selectedMonth = 0;
        dateButtons.Controls.Clear();
        selectionLabel.Text = "Choose a year";
        backButton.Enabled = false;
        var years = sales.Select(sale => sale.Time.Year).Distinct().OrderByDescending(year => year).ToList();
        if (years.Count == 0) years.Add(DateTime.Today.Year);
        foreach (var year in years)
        {
            var button = DateButton(year.ToString());
            button.Click += (_, _) => ShowMonths(year);
            dateButtons.Controls.Add(button);
        }
        ShowDetails("All years", sales);
    }

    private void ShowMonths(int year)
    {
        selectedYear = year;
        selectedMonth = 0;
        dateButtons.Controls.Clear();
        selectionLabel.Text = $"Year {year}  /  choose a month";
        backButton.Enabled = true;
        foreach (var month in sales.Where(sale => sale.Time.Year == year).Select(sale => sale.Time.Month).Distinct().OrderBy(month => month))
        {
            var monthNumber = month;
            var button = DateButton(new DateTime(year, month, 1).ToString("MMMM"));
            button.Click += (_, _) => ShowDays(year, monthNumber);
            dateButtons.Controls.Add(button);
        }
        ShowDetails(year.ToString(), sales.Where(sale => sale.Time.Year == year).ToList());
    }

    private void ShowDays(int year, int month)
    {
        selectedYear = year;
        selectedMonth = month;
        dateButtons.Controls.Clear();
        selectionLabel.Text = $"{new DateTime(year, month, 1):MMMM yyyy}  /  choose a day";
        backButton.Enabled = true;
        foreach (var day in sales.Where(sale => sale.Time.Year == year && sale.Time.Month == month).Select(sale => sale.Time.Day).Distinct().OrderBy(day => day))
        {
            var dayNumber = day;
            var button = DateButton(day.ToString());
            button.Click += (_, _) => ShowDetails(new DateTime(year, month, dayNumber).ToString("dd MMMM yyyy"), sales.Where(sale => sale.Time.Date == new DateTime(year, month, dayNumber)).ToList());
            dateButtons.Controls.Add(button);
        }
        ShowDetails(new DateTime(year, month, 1).ToString("MMMM yyyy"), sales.Where(sale => sale.Time.Year == year && sale.Time.Month == month).ToList());
    }

    private void GoBack()
    {
        if (selectedYear == 0) return;
        if (selectedMonth == 0)
            ShowYears();
        else
            ShowMonths(selectedYear);
    }

    private Button DateButton(string text) => new() { Text = text, Size = new Size(120, 36), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

    private void PrintReport()
    {
        if (currentFilteredSales.Count == 0) { MessageBox.Show("No sales to print.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        using var document = new PrintDocument();
        document.PrintPage += (_, args) =>
        {
            if (args.Graphics == null) return;
            var g = args.Graphics;
            var y = 30;
            var margin = 40;
            var width = args.PageBounds.Width - (margin * 2);
            using var titleFont = new Font("Consolas", 14F, FontStyle.Bold);
            using var headerFont = new Font("Consolas", 10F, FontStyle.Bold);
            using var font = new Font("Consolas", 9F);
            using var smallFont = new Font("Consolas", 8F);
            string P(decimal amount) => amount.ToString("N2") + " " + currency;

            g.DrawString("VECTRON POS - Sales Report", titleFont, Brushes.Black, margin, y);
            y += 30;
            g.DrawString(summaryLabel.Text, headerFont, Brushes.Black, margin, y);
            y += 25;
            g.DrawLine(Pens.Black, margin, y, margin + width, y);
            y += 10;

            var activeTab = tabs.SelectedTab?.Text ?? "By product";
            g.DrawString($"Report: {activeTab}", headerFont, Brushes.Black, margin, y);
            y += 22;

            void DrawRow(string[] cells, int[] colX, Font f, Brush b)
            {
                for (var i = 0; i < cells.Length; i++)
                    g.DrawString(cells[i], f, b, colX[i], y);
                y += 16;
            }

            void DrawHeader(string[] headers, int[] colX)
            {
                for (var i = 0; i < headers.Length; i++)
                    g.DrawString(headers[i], headerFont, Brushes.Black, colX[i], y);
                y += 18;
                g.DrawLine(Pens.Black, margin, y, margin + width, y);
                y += 4;
            }

            void DrawFooter()
            {
                g.DrawLine(Pens.Black, margin, y, margin + width, y);
                y += 4;
            }

            int[] TwoCol() => [margin, margin + 300];
            int[] ThreeCol() => [margin, margin + 220, margin + 340];
            int[] FiveCol() => [margin, margin + 95, margin + 180, margin + 280, margin + 360];

            if (activeTab == "Transactions")
            {
                DrawHeader(["Time", "Table", "Operator", "Items", "Total"], FiveCol());
                foreach (var sale in currentFilteredSales.OrderByDescending(sale => sale.Time))
                {
                    if (y > args.PageBounds.Height - 60) { args.HasMorePages = true; return; }
                    var itemCount = sale.Items?.Sum(item => item.Quantity) ?? 0;
                    DrawRow([sale.Time.ToString("dd/MM HH:mm"), sale.Table, sale.Operator, itemCount.ToString(), P(sale.Total)], FiveCol(), font, Brushes.Black);
                }
                DrawFooter();
            }
            else if (activeTab == "Financial")
            {
                var totalGross = currentFilteredSales.Sum(sale => sale.Total);
                var totalItems = currentFilteredSales.SelectMany(sale => sale.Items ?? []).Sum(item => item.Quantity);
                var avgSale = currentFilteredSales.Count > 0 ? totalGross / currentFilteredSales.Count : 0;
                var topProduct = currentFilteredSales.SelectMany(sale => sale.Items ?? []).GroupBy(item => item.Product).OrderByDescending(g2 => g2.Sum(i => i.UnitPrice * i.Quantity)).FirstOrDefault();
                var topTable = currentFilteredSales.GroupBy(sale => sale.Table).OrderByDescending(g2 => g2.Sum(s => s.Total)).FirstOrDefault();
                var topOperator = currentFilteredSales.GroupBy(sale => sale.Operator).OrderByDescending(g2 => g2.Sum(s => s.Total)).FirstOrDefault();
                var topHour = currentFilteredSales.GroupBy(sale => sale.Time.Hour).OrderByDescending(g2 => g2.Sum(s => s.Total)).FirstOrDefault();

                DrawHeader(["Metric", "Value"], TwoCol());
                DrawRow(["Total Revenue", P(totalGross)], TwoCol(), font, Brushes.Black);
                DrawRow(["Transactions", currentFilteredSales.Count.ToString()], TwoCol(), font, Brushes.Black);
                DrawRow(["Items Sold", totalItems.ToString()], TwoCol(), font, Brushes.Black);
                DrawRow(["Average Sale", P(avgSale)], TwoCol(), font, Brushes.Black);
                DrawRow(["", ""], TwoCol(), font, Brushes.Black);
                DrawRow(["Top Product", topProduct != null ? $"{topProduct.Key} ({topProduct.Sum(i => i.Quantity)}x)" : "N/A"], TwoCol(), font, Brushes.Black);
                DrawRow(["Top Table", topTable != null ? $"{topTable.Key} ({topTable.Count()} sales)" : "N/A"], TwoCol(), font, Brushes.Black);
                DrawRow(["Top Operator", topOperator != null ? $"{topOperator.Key} ({topOperator.Count()} sales)" : "N/A"], TwoCol(), font, Brushes.Black);
                DrawRow(["Peak Hour", topHour != null ? $"{topHour.Key:D2}:00 ({topHour.Count()} sales)" : "N/A"], TwoCol(), font, Brushes.Black);
                DrawFooter();
            }
            else
            {
                DrawHeader(["Name", "Qty", "Total"], ThreeCol());

                IEnumerable<(string Name, int Quantity, decimal Total)> selector = activeTab switch
                {
                    "By product" => currentFilteredSales.SelectMany(sale => sale.Items?.Select(item => (item.Product, item.Quantity, item.UnitPrice * item.Quantity)) ?? []),
                    "By family" => currentFilteredSales.SelectMany(sale => sale.Items?.Select(item => (item.Family == "General" ? item.Category : item.Family, item.Quantity, item.UnitPrice * item.Quantity)) ?? []),
                    "By operator" => currentFilteredSales.SelectMany(sale => new[] { (sale.Operator, sale.Items?.Sum(i => i.Quantity) ?? 0, sale.Total) }),
                    "By table" => currentFilteredSales.SelectMany(sale => new[] { (sale.Table, sale.Items?.Sum(i => i.Quantity) ?? 0, sale.Total) }),
                    "By hour" => currentFilteredSales.GroupBy(sale => sale.Time.Hour).Select(g2 => ($"{g2.Key:D2}:00", g2.Count(), g2.Sum(s => s.Total))),
                    _ => []
                };

                var groups = activeTab == "By hour"
                    ? selector.Select(x => (x.Name, x.Quantity, x.Total))
                    : selector.GroupBy(x => x.Name).Select(g2 => (Name: g2.Key, Quantity: g2.Sum(x => x.Quantity), Total: g2.Sum(x => x.Total)));

                foreach (var item in groups.OrderByDescending(x => x.Total))
                {
                    if (y > args.PageBounds.Height - 60) { args.HasMorePages = true; return; }
                    DrawRow([item.Name, item.Quantity.ToString(), item.Total.ToString("N2") + " " + currency], ThreeCol(), font, Brushes.Black);
                }
                DrawFooter();
            }

            y += 10;
            g.DrawString($"Printed: {DateTime.Now:dd/MM/yyyy HH:mm}", smallFont, Brushes.Gray, margin, y);
        };
        using var dialog = new PrintDialog { Document = document, UseEXDialog = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            document.Print();
    }

    private void ShowDetails(string period, List<Sale> selectedSales)
    {
        currentFilteredSales = selectedSales;
        selectedTransaction = null;
        UpdateRefundButton();
        var totalRevenue = selectedSales.Sum(sale => sale.Total);
        var totalItems = selectedSales.SelectMany(sale => sale.Items ?? []).Sum(item => item.Quantity);
        var avgSale = selectedSales.Count > 0 ? totalRevenue / selectedSales.Count : 0;
        summaryLabel.Text = $"{period}  •  {totalRevenue:C}  •  {selectedSales.Count} transactions  •  {totalItems} items  •  avg {avgSale:C}";
        tabs.TabPages.Clear();
        tabs.TabPages.Add(BuildPage("By product", selectedSales, sale => sale.Items?.Select(item => (item.Product, item.Quantity, item.UnitPrice * item.Quantity)) ?? [], panelLight, mint));
        tabs.TabPages.Add(BuildPage("By family", selectedSales, sale => sale.Items?.Select(item => (item.Family == "General" ? item.Category : item.Family, item.Quantity, item.UnitPrice * item.Quantity)) ?? [], panelLight, mint));
        tabs.TabPages.Add(BuildPage("By operator", selectedSales.Select(sale => sale with { Items = [new SoldLine(sale.Operator, "Operator", 1, sale.Total)] }).ToList(), sale => sale.Items?.Select(item => (item.Product, item.Quantity, item.UnitPrice * item.Quantity)) ?? [], panelLight, mint));
        tabs.TabPages.Add(BuildPage("By table", selectedSales.Select(sale => sale with { Items = [new SoldLine(sale.Table, "Table", 1, sale.Total)] }).ToList(), sale => sale.Items?.Select(item => (item.Product, item.Quantity, item.UnitPrice * item.Quantity)) ?? [], panelLight, mint));
        tabs.TabPages.Add(BuildHourlyPage(selectedSales));
        tabs.TabPages.Add(BuildTransactionPage(selectedSales));
        tabs.TabPages.Add(BuildFinancialPage(selectedSales));
    }

    private static TabPage BuildPage(string text, List<Sale> sales, Func<Sale, IEnumerable<(string Name, int Quantity, decimal Total)>> selector, Color panelLight, Color mint)
    {
        var page = new TabPage(text) { BackColor = Color.FromArgb(19, 24, 33), ForeColor = Color.White };
        var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true, BackColor = panelLight, ForeColor = Color.White };
        list.Columns.Add("Name", 300);
        list.Columns.Add("Quantity", 120);
        list.Columns.Add("Total", 160);
        foreach (var group in sales.SelectMany(selector).GroupBy(item => item.Name).OrderByDescending(group => group.Sum(item => item.Total)))
            list.Items.Add(new ListViewItem([group.Key, group.Sum(item => item.Quantity).ToString(), group.Sum(item => item.Total).ToString("C")]));
        page.Controls.Add(list);
        return page;
    }

    private TabPage BuildHourlyPage(List<Sale> selectedSales)
    {
        var page = new TabPage("By hour") { BackColor = Color.FromArgb(19, 24, 33), ForeColor = Color.White };
        var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true, BackColor = panelLight, ForeColor = Color.White };
        list.Columns.Add("Hour", 120);
        list.Columns.Add("Transactions", 120);
        list.Columns.Add("Total", 160);
        list.Columns.Add("Avg Sale", 160);
        for (var h = 0; h < 24; h++)
        {
            var hourSales = selectedSales.Where(sale => sale.Time.Hour == h).ToList();
            if (hourSales.Count == 0) continue;
            var total = hourSales.Sum(sale => sale.Total);
            list.Items.Add(new ListViewItem([$"{h:D2}:00", hourSales.Count.ToString(), total.ToString("C"), (total / hourSales.Count).ToString("C")]));
        }
        page.Controls.Add(list);
        return page;
    }

    private TabPage BuildTransactionPage(List<Sale> selectedSales)
    {
        var page = new TabPage("Transactions") { BackColor = Color.FromArgb(19, 24, 33), ForeColor = Color.White };
        var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true, BackColor = panelLight, ForeColor = Color.White };
        list.Columns.Add("Time", 140);
        list.Columns.Add("Table", 100);
        list.Columns.Add("Operator", 100);
        list.Columns.Add("Items", 60);
        list.Columns.Add("Total", 100);
        list.SelectedIndexChanged += (_, _) =>
        {
            selectedTransaction = list.SelectedIndices.Count > 0 && list.Items[list.SelectedIndices[0]].Tag is Sale sale ? sale : null;
            UpdateRefundButton();
        };
        foreach (var sale in selectedSales.OrderByDescending(sale => sale.Time))
        {
            var itemCount = sale.Items?.Sum(item => item.Quantity) ?? 0;
            var row = new ListViewItem([sale.Time.ToString("dd/MM HH:mm"), sale.IsRefund ? $"REFUND {sale.Table}" : sale.Table, sale.Operator, itemCount.ToString(), sale.Total.ToString("C")]);
            row.Tag = sale;
            if (sale.IsRefund) row.ForeColor = Color.OrangeRed;
            list.Items.Add(row);
        }
        page.Controls.Add(list);
        return page;
    }

    private TabPage BuildFinancialPage(List<Sale> selectedSales)
    {
        var page = new TabPage("Financial") { BackColor = Color.FromArgb(19, 24, 33), ForeColor = Color.White };
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20), AutoScroll = true };
        var totalGross = selectedSales.Sum(sale => sale.Total);
        var totalItems = selectedSales.SelectMany(sale => sale.Items ?? []).Sum(item => item.Quantity);
        var avgSale = selectedSales.Count > 0 ? totalGross / selectedSales.Count : 0;
        var avgItems = selectedSales.Count > 0 ? (decimal)totalItems / selectedSales.Count : 0;
        var topProduct = selectedSales.SelectMany(sale => sale.Items ?? []).GroupBy(item => item.Product).OrderByDescending(g => g.Sum(i => i.UnitPrice * i.Quantity)).FirstOrDefault();
        var topTable = selectedSales.GroupBy(sale => sale.Table).OrderByDescending(g => g.Sum(s => s.Total)).FirstOrDefault();
        var topOperator = selectedSales.GroupBy(sale => sale.Operator).OrderByDescending(g => g.Sum(s => s.Total)).FirstOrDefault();
        var topHour = selectedSales.GroupBy(sale => sale.Time.Hour).OrderByDescending(g => g.Sum(s => s.Total)).FirstOrDefault();

        void AddLine(string label, string value)
        {
            var row = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, Margin = new Padding(0, 4, 0, 4) };
            row.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = Color.FromArgb(143, 154, 171), MinimumSize = new Size(200, 0) });
            row.Controls.Add(new Label { Text = value, AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 10F) });
            panel.Controls.Add(row);
        }

        AddLine("Total Revenue", totalGross.ToString("C"));
        AddLine("Total Transactions", selectedSales.Count.ToString());
        AddLine("Total Items Sold", totalItems.ToString());
        AddLine("Average Sale", avgSale.ToString("C"));
        AddLine("Average Items/Sale", avgItems.ToString("F1"));
        panel.Controls.Add(new Label { Text = "", AutoSize = true, Margin = new Padding(0, 8, 0, 8) });
        AddLine("Top Product", topProduct != null ? $"{topProduct.Key} ({topProduct.Sum(i => i.Quantity)}x)" : "N/A");
        AddLine("Top Table", topTable != null ? $"{topTable.Key} ({topTable.Count()} sales)" : "N/A");
        AddLine("Top Operator", topOperator != null ? $"{topOperator.Key} ({topOperator.Count()} sales)" : "N/A");
        AddLine("Peak Hour", topHour != null ? $"{topHour.Key:D2}:00 ({topHour.Count()} sales)" : "N/A");
        page.Controls.Add(panel);
        return page;
    }
}

internal sealed class RoomManagerForm : Form
{
    private readonly List<Room> rooms;
    private readonly Action<List<Room>> save;
    private readonly FlowLayoutPanel editor = new();
    private readonly List<(TextBox Name, NumericUpDown Tables)> fields = [];

    public RoomManagerForm(Color background, Color panelLight, Color mint, List<Room> source, Action<List<Room>> save)
    {
        rooms = source.Select(room => new Room(room.Name, room.TableCount)).ToList();
        this.save = save;
        Text = "Room management";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 520);
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(500, 420);
        AutoScroll = true;
        AutoScrollMinSize = new Size(520, 500);
        var title = new Label { Text = "Rooms and tables", AutoSize = true, Font = new Font("Segoe UI Semibold", 18F), Location = new Point(28, 22) };
        var hint = new Label { Text = "Name each room and set its table count.", AutoSize = true, ForeColor = Color.FromArgb(143, 154, 171), Location = new Point(30, 56) };
        editor.Location = new Point(30, 94);
        editor.Size = new Size(490, 300);
        editor.FlowDirection = FlowDirection.TopDown;
        editor.WrapContents = false;
        editor.AutoScroll = true;
        foreach (var room in rooms) AddRoomRow(room.Name, room.TableCount, panelLight);
        var add = RoomButton("+ Add room", 30, 420, 120, panelLight);
        add.Click += (_, _) => AddRoomRow("New room", 4, panelLight);
        var saveButton = RoomButton("Save changes", 370, 420, 150, mint);
        saveButton.ForeColor = background;
        saveButton.Click += (_, _) => SaveRooms();
        Controls.AddRange([title, hint, editor, add, saveButton]);
    }

    private void AddRoomRow(string roomName, int tableCount, Color panelLight)
    {
        var row = new Panel { Size = new Size(450, 40) };
        var name = new TextBox { Text = roomName, Location = new Point(0, 5), Size = new Size(310, 28), BackColor = panelLight, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        var tables = new NumericUpDown { Minimum = 1, Maximum = 99, Value = tableCount, Location = new Point(330, 5), Size = new Size(75, 28), BackColor = panelLight, ForeColor = Color.White };
        row.Controls.AddRange([name, tables]);
        editor.Controls.Add(row);
        fields.Add((name, tables));
    }

    private Button RoomButton(string text, int x, int y, int width, Color color) => new() { Text = text, Location = new Point(x, y), Size = new Size(width, 38), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

    private void SaveRooms()
    {
        save(fields.Select(field => new Room(field.Name.Text.Trim(), (int)field.Tables.Value)).ToList());
    }
}

internal static class CatalogImageStore
{
    public static string? Import()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Import catalog image",
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != DialogResult.OK) return null;
        var folder = Path.Combine(AppContext.BaseDirectory, "images");
        Directory.CreateDirectory(folder);
        var destination = Path.Combine(folder, Guid.NewGuid().ToString("N") + Path.GetExtension(dialog.FileName));
        File.Copy(dialog.FileName, destination, true);
        return destination;
    }
}

internal static class RawPrinter
{
    internal static byte[] ImageToEscPosBytes(Bitmap bmp)
    {
        var width = bmp.Width;
        var height = bmp.Height;
        var bytesPerLine = (width + 7) / 8;
        var data = new List<byte>();
        data.AddRange([0x1D, 0x76, 0x30, 0x00]);
        data.Add((byte)(bytesPerLine & 0xFF));
        data.Add((byte)((bytesPerLine >> 8) & 0xFF));
        data.Add((byte)(height & 0xFF));
        data.Add((byte)((height >> 8) & 0xFF));
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < bytesPerLine; x++)
            {
                byte b = 0;
                for (var bit = 0; bit < 8; bit++)
                {
                    var px = x * 8 + bit;
                    if (px < width && bmp.GetPixel(px, y).GetBrightness() < 0.5f)
                        b |= (byte)(0x80 >> bit);
                }
                data.Add(b);
            }
        }
        return data.ToArray();
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    internal sealed class DocumentInfo
    {
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
        public string DocumentName = string.Empty;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
        public string? OutputFile;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
        public string? DataType;
    }

    [System.Runtime.InteropServices.DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    internal static extern bool OpenPrinterLocal(string printerName, out nint printer, nint defaults);
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    internal static extern bool ClosePrinter(nint printer);
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    internal static extern bool StartDocPrinter(nint printer, int level, [System.Runtime.InteropServices.In] DocumentInfo documentInfo);
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    internal static extern bool EndDocPrinter(nint printer);
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    internal static extern bool StartPagePrinter(nint printer);
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    internal static extern bool EndPagePrinter(nint printer);
    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    internal static extern bool WritePrinter(nint printer, byte[] data, int count, out int written);

    public static void PrintTicket(PrinterDestination destination, string room, string table, string operatorName, List<SoldLine> lines, string? printerName = null, int orderNumber = 0, int billNumber = 0, int paymentNumber = 0, string orderType = "", decimal discountPercent = 0, decimal discountAmount = 0)
    {
        var name = printerName ?? destination.WindowsPrinter;
        if (!OpenPrinterLocal(name, out var printer, 0))
            throw new InvalidOperationException($"Windows printer '{name}' could not be opened.");
        var documentInfo = new DocumentInfo { DocumentName = "Vectron POS ticket", OutputFile = null, DataType = null };
        try
        {
            if (!StartDocPrinter(printer, 1, documentInfo))
                throw new InvalidOperationException($"The printer rejected the raw ticket job (Windows error {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}).");
            if (!StartPagePrinter(printer))
                throw new InvalidOperationException("The printer page could not be started.");

            using var stream = new MemoryStream();
            void Write(string s) => stream.Write(Encoding.ASCII.GetBytes(s), 0, Encoding.ASCII.GetByteCount(s));

            Write("\x1B@");
            foreach (var style in TicketLayout.Components(destination))
            {
                if (style.Component == "Cutter") { Write("\x1B\x64\x05\x1D\x56\x00"); continue; }
                if (style.Component == "Blank line") { stream.WriteByte(0x0A); continue; }
                if (style.Component == "Logo" && (string.IsNullOrWhiteSpace(destination.LogoPath) || !File.Exists(destination.LogoPath))) continue;

                string? value = null;
                if (style.Component != "Logo")
                {
                    value = TicketLayout.Text(style.Component, destination, room, table, operatorName, lines, orderNumber, billNumber, paymentNumber, orderType, discountPercent, discountAmount);
                    if (string.IsNullOrWhiteSpace(value)) continue;
                }

                Write(style.Alignment switch { "Center" => "\x1B\x61\x01", "Right" => "\x1B\x61\x02", _ => "\x1B\x61\x00" });
                var (w, h) = style.FontSize switch
                {
                    >= 32 => ((byte)4, (byte)4),
                    >= 26 => ((byte)3, (byte)3),
                    >= 22 => ((byte)3, (byte)2),
                    >= 18 => ((byte)2, (byte)2),
                    >= 15 => ((byte)2, (byte)1),
                    >= 12 => ((byte)1, (byte)2),
                    _ => ((byte)1, (byte)1)
                };
                Write($"\x1D!{(char)h}{(char)w}");
                Write("\x1B\x2D" + (char)(style.Underline ? 1 : 0));
                if (style.Bold) Write("\x1B\x45\x01");
                stream.WriteByte(0x0A);

                if (style.Component == "Logo")
                {
                    using var img = Image.FromFile(destination.LogoPath);
                    var maxW = style.FontSize >= 32 ? 576 : 384;
                    var scale = (double)maxW / img.Width;
                    var drawW = Math.Min((int)(img.Width * scale), maxW);
                    var drawH = (int)(img.Height * scale);
                    using var bmp = new Bitmap(img, new Size(drawW, drawH));
                var imgBytes = RawPrinter.ImageToEscPosBytes(bmp);
                    stream.Write(imgBytes, 0, imgBytes.Length);
                }
                else
                {
                    Write(value!);
                }

                stream.WriteByte(0x0A);
                if (style.Bold) Write("\x1B\x45\x00");
                Write("\x1B\x2D\x00");
                Write("\x1D!\x01\x01");
                Write("\x1B\x61\x00");
            }

            if (!TicketLayout.Components(destination).Any(s => s.Component == "Cutter") && destination.CutAfterPrint)
                Write("\x1B\x64\x05\x1D\x56\x00");

            var bytes = stream.ToArray();
            if (!WritePrinter(printer, bytes, bytes.Length, out var written) || written != bytes.Length)
                throw new InvalidOperationException("The printer did not accept the complete ticket.");
            EndPagePrinter(printer);
            EndDocPrinter(printer);
        }
        finally
        {
            ClosePrinter(printer);
        }
    }
}

internal static class TcpPrintServer
{
    private static System.Net.Sockets.TcpListener? listener;
    private static CancellationTokenSource? cts;
    public static bool IsRunning { get; private set; }
    public static string LocalIp { get; private set; } = "";

    public static void Start(int port = 9100)
    {
        Stop();
        try { LocalIp = GetLocalIpAddress(); } catch { }
        try
        {
            cts = new CancellationTokenSource();
            listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
            listener.Start();
            IsRunning = true;
            _ = Task.Run(() => AcceptLoop(cts.Token));
        }
        catch { IsRunning = false; }
    }

    public static void Stop()
    {
        cts?.Cancel();
        listener?.Stop();
        listener = null;
        IsRunning = false;
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            using var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            socket.Connect("8.8.8.8", 80);
            return ((System.Net.IPEndPoint)socket.LocalEndPoint!).Address.ToString();
        }
        catch { return "127.0.0.1"; }
    }

    private static async Task AcceptLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var client = await listener!.AcceptTcpClientAsync(token);
                _ = Task.Run(() => HandleClient(client), token);
            }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    private static async Task HandleClient(System.Net.Sockets.TcpClient client)
    {
        try
        {
            var stream = client.GetStream();
            var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            while (client.Connected)
            {
                var printerName = reader.ReadString();
                var dataLength = reader.ReadInt32();
                var data = reader.ReadBytes(dataLength);
                if (!RawPrinter.OpenPrinterLocal(printerName, out var printer, 0))
                    continue;
                var docInfo = new RawPrinter.DocumentInfo { DocumentName = "Remote print", OutputFile = null, DataType = null };
                try
                {
                    RawPrinter.StartDocPrinter(printer, 1, docInfo);
                    RawPrinter.StartPagePrinter(printer);
                    RawPrinter.WritePrinter(printer, data, data.Length, out _);
                    RawPrinter.EndPagePrinter(printer);
                    RawPrinter.EndDocPrinter(printer);
                }
                finally { RawPrinter.ClosePrinter(printer); }
            }
        }
        catch { }
        finally { client.Close(); }
    }
}

internal static class TcpPrintClient
{
    private static System.Net.Sockets.TcpClient? persistentClient;
    private static System.Net.Sockets.NetworkStream? persistentStream;
    private static BinaryWriter? persistentWriter;
    private static readonly object lockObj = new();
    private static string lastHost = "";

    private static void EnsureConnected(string serverHost)
    {
        if (persistentClient != null && persistentClient.Connected && lastHost == serverHost) return;
        lock (lockObj)
        {
            persistentClient?.Close();
            persistentClient = new System.Net.Sockets.TcpClient();
            persistentClient.NoDelay = true;
            persistentClient.Connect(serverHost, 9100);
            persistentStream = persistentClient.GetStream();
            persistentWriter = new BinaryWriter(persistentStream, System.Text.Encoding.UTF8, leaveOpen: true);
            lastHost = serverHost;
        }
    }

    public static void SendPrintJob(string serverHost, string printerName, byte[] data)
    {
        lock (lockObj)
        {
            try
            {
                EnsureConnected(serverHost);
                persistentWriter!.Write(printerName);
                persistentWriter.Write(data.Length);
                persistentWriter.Write(data);
                persistentWriter.Flush();
            }
            catch
            {
                persistentClient?.Close();
                persistentClient = null;
                throw;
            }
        }
    }
}

internal sealed class PrinterManagerForm : Form
{
    private readonly List<PrinterDestination> destinations;
    private readonly Action<List<PrinterDestination>> save;
    private readonly Action<PrinterDestination, List<TicketComponentStyle>> saveLayout;
    private readonly ListBox list = new();
    private readonly TextBox name = new();
    private readonly ComboBox windowsPrinter = new();
    private readonly TextBox ticketHeader = new();
    private readonly TextBox ticketFooter = new();
    private readonly CheckBox cutAfterPrint = new();
    private readonly Button editLayout = new();
    private readonly Label printerLabel = new();
    private readonly Label headerLabel = new();
    private readonly Label footerLabel = new();
    private int selectedIndex = -1;

    public PrinterManagerForm(Color background, Color panelLight, Color mint, List<PrinterDestination> source, Action<List<PrinterDestination>> save, Action<PrinterDestination, List<TicketComponentStyle>>? saveLayout = null)
    {
        destinations = [.. source];
        this.save = save;
        this.saveLayout = saveLayout ?? ((d, c) => { });
        Text = "Printer routing";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(700, 500);
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(600, 420);
        var title = new Label { Text = "Printer routing", AutoSize = true, Font = new Font("Segoe UI Semibold", 18F), Location = new Point(24, 20) };
        var hint = new Label { Text = "Create destinations such as Kitchen, Bar, or POS and map them to Windows printers.", AutoSize = true, ForeColor = Color.FromArgb(143, 154, 171), Location = new Point(26, 55) };
        list.Location = new Point(24, 92);
        list.Size = new Size(230, 300);
        list.BackColor = panelLight;
        list.ForeColor = Color.White;
        list.BorderStyle = BorderStyle.None;
        list.SelectedIndexChanged += (_, _) => LoadDestination();
        foreach (var destination in destinations) list.Items.Add(destination.Name);
        if (!destinations.Any(d => d.Name == "Cancel"))
        {
            destinations.Add(new PrinterDestination("Cancel", "", "CANCEL", "", true));
            list.Items.Add("Cancel");
        }
        if (!destinations.Any(d => d.Name == "Bill"))
        {
            destinations.Add(new PrinterDestination("Bill", "", "BILL", "Thank you", true));
            list.Items.Add("Bill");
        }
        if (!destinations.Any(d => d.Name == "Payment"))
        {
            destinations.Add(new PrinterDestination("Payment", "", "PAYMENT", "Thank you", true));
            list.Items.Add("Payment");
        }
        if (!destinations.Any(d => d.Name == "Refund"))
        {
            var payment = destinations.FirstOrDefault(d => d.Name == "Payment");
            destinations.Add(new PrinterDestination("Refund", payment?.WindowsPrinter ?? "", payment?.Header ?? "REFUND", payment?.Footer ?? "Thank you", payment?.CutAfterPrint ?? true, payment?.Layout == null ? null : [.. payment.Layout], payment?.LogoPath ?? ""));
            list.Items.Add("Refund");
        }
        var nameLabel = new Label { Text = "Destination name", AutoSize = true, Location = new Point(290, 95) };
        name.Location = new Point(290, 120);
        name.Size = new Size(350, 30);
        name.BackColor = panelLight;
        name.ForeColor = Color.White;
        name.BorderStyle = BorderStyle.FixedSingle;
        printerLabel.Text = "Windows printer";
        printerLabel.AutoSize = true;
        printerLabel.Location = new Point(290, 175);
        windowsPrinter.Location = new Point(290, 200);
        windowsPrinter.Size = new Size(350, 30);
        windowsPrinter.DropDownStyle = ComboBoxStyle.DropDownList;
        windowsPrinter.BackColor = panelLight;
        windowsPrinter.ForeColor = Color.White;
        foreach (string installed in PrinterSettings.InstalledPrinters) windowsPrinter.Items.Add(installed);
        if (windowsPrinter.Items.Count == 0) windowsPrinter.Items.Add("No Windows printers detected");
        var ticketLabel = new Label { Text = "Ticket template", AutoSize = true, Font = new Font("Segoe UI Semibold", 12F), Location = new Point(290, 250) };
        headerLabel.Text = "Header";
        headerLabel.AutoSize = true;
        headerLabel.Location = new Point(290, 280);
        ticketHeader.Location = new Point(290, 302);
        ticketHeader.Size = new Size(350, 28);
        ticketHeader.BackColor = panelLight;
        ticketHeader.ForeColor = Color.White;
        ticketHeader.BorderStyle = BorderStyle.FixedSingle;
        footerLabel.Text = "Footer";
        footerLabel.AutoSize = true;
        footerLabel.Location = new Point(290, 338);
        ticketFooter.Location = new Point(290, 360);
        ticketFooter.Size = new Size(350, 28);
        ticketFooter.BackColor = panelLight;
        ticketFooter.ForeColor = Color.White;
        ticketFooter.BorderStyle = BorderStyle.FixedSingle;
        cutAfterPrint.Text = "Activate cutter after last line";
        cutAfterPrint.AutoSize = true;
        cutAfterPrint.Location = new Point(290, 402);
        cutAfterPrint.ForeColor = Color.White;
        editLayout.Text = "Edit ticket layout";
        editLayout.Location = new Point(290, 435);
        editLayout.Size = new Size(170, 34);
        editLayout.BackColor = panelLight;
        editLayout.ForeColor = Color.White;
        editLayout.FlatStyle = FlatStyle.Flat;
        editLayout.FlatAppearance.BorderSize = 0;
        editLayout.Click += (_, _) => EditLayout();
        var add = PrinterButton("New destination", 24, 420, 135, panelLight);
        add.Click += (_, _) => AddDestination();
        var remove = PrinterButton("Delete", 169, 420, 85, panelLight);
        remove.Click += (_, _) => DeleteDestination();
        var saveButton = PrinterButton("Save changes", 510, 420, 130, mint);
        saveButton.ForeColor = background;
        saveButton.Click += (_, _) => SaveDestination();
        Controls.AddRange([title, hint, list, nameLabel, name, printerLabel, windowsPrinter, ticketLabel, headerLabel, ticketHeader, footerLabel, ticketFooter, cutAfterPrint, editLayout, add, remove, saveButton]);
        if (destinations.Count > 0) list.SelectedIndex = 0;
    }

    private Button PrinterButton(string text, int x, int y, int width, Color color) => new() { Text = text, Location = new Point(x, y), Size = new Size(width, 38), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

    private void LoadDestination()
    {
        if (selectedIndex >= 0 && selectedIndex < destinations.Count && selectedIndex != list.SelectedIndex)
        {
            var existing = destinations[selectedIndex];
            var printer = windowsPrinter.SelectedItem?.ToString() ?? existing.WindowsPrinter;
            if (existing.Name == "Cancel")
                destinations[selectedIndex] = new PrinterDestination("Cancel", "", ticketHeader.Text.Trim(), ticketFooter.Text.Trim(), cutAfterPrint.Checked, existing.Layout, existing.LogoPath);
            else if (existing.Name == "Payment")
                destinations[selectedIndex] = new PrinterDestination("Payment", printer, ticketHeader.Text.Trim(), ticketFooter.Text.Trim(), cutAfterPrint.Checked, existing.Layout, existing.LogoPath);
            else if (existing.Name == "Refund")
                destinations[selectedIndex] = existing with { Header = ticketHeader.Text.Trim(), Footer = ticketFooter.Text.Trim(), CutAfterPrint = cutAfterPrint.Checked };
            else
            {
                var newName = name.Enabled && !string.IsNullOrWhiteSpace(name.Text) ? name.Text.Trim() : existing.Name;
                destinations[selectedIndex] = new PrinterDestination(newName, printer, ticketHeader.Text.Trim(), ticketFooter.Text.Trim(), cutAfterPrint.Checked, existing.Layout, existing.LogoPath);
            }
            list.Items[selectedIndex] = destinations[selectedIndex].Name;
            if (existing.Name == "Payment") MirrorRefundFromPayment();
        }
        if (list.SelectedIndex < 0 || list.SelectedIndex >= destinations.Count) return;
        selectedIndex = list.SelectedIndex;
        if (destinations[selectedIndex].Name == "Refund") MirrorRefundFromPayment();
        var destination = destinations[selectedIndex];
        var isSpecial = destination.Name == "Cancel";
        name.Text = destination.Name;
        name.Enabled = destination.Name is not ("Cancel" or "Bill" or "Payment" or "Refund");
        windowsPrinter.Enabled = destination.Name != "Refund";
        printerLabel.Visible = !isSpecial;
        windowsPrinter.Visible = !isSpecial;
        headerLabel.Visible = true;
        ticketHeader.Visible = true;
        footerLabel.Visible = true;
        ticketFooter.Visible = true;
        cutAfterPrint.Visible = true;
        if (!isSpecial)
        {
            var idx = string.IsNullOrWhiteSpace(destination.WindowsPrinter) ? -1 : windowsPrinter.Items.IndexOf(destination.WindowsPrinter);
            windowsPrinter.SelectedIndex = idx >= 0 ? idx : -1;
        }
        ticketHeader.Text = destination.Header;
        ticketFooter.Text = destination.Footer;
        cutAfterPrint.Checked = destination.CutAfterPrint;
        editLayout.Enabled = list.SelectedIndex >= 0;
    }

    private void AddDestination()
    {
        var printerName = windowsPrinter.Items.Count > 0 ? Convert.ToString(windowsPrinter.Items[0]) ?? "" : "";
        destinations.Add(new PrinterDestination("New destination", printerName));
        list.Items.Add("New destination");
        list.SelectedIndex = destinations.Count - 1;
    }

    private void DeleteDestination()
    {
        if (selectedIndex < 0) return;
        if (destinations[selectedIndex].Name is "Cancel" or "Bill" or "Payment" or "Refund") return;
        destinations.RemoveAt(selectedIndex);
        list.Items.RemoveAt(selectedIndex);
        selectedIndex = -1;
        if (destinations.Count > 0) list.SelectedIndex = 0;
    }

    private void SaveDestination()
    {
        if (selectedIndex >= 0 && selectedIndex < destinations.Count)
        {
            var existing = destinations[selectedIndex];
            if (existing.Name == "Cancel")
                destinations[selectedIndex] = new PrinterDestination("Cancel", "", ticketHeader.Text.Trim(), ticketFooter.Text.Trim(), cutAfterPrint.Checked, existing.Layout, existing.LogoPath);
            else if (existing.Name == "Payment")
                destinations[selectedIndex] = new PrinterDestination("Payment", windowsPrinter.SelectedItem?.ToString() ?? existing.WindowsPrinter, ticketHeader.Text.Trim(), ticketFooter.Text.Trim(), cutAfterPrint.Checked, existing.Layout, existing.LogoPath);
            else if (existing.Name == "Refund")
                destinations[selectedIndex] = existing with { Header = ticketHeader.Text.Trim(), Footer = ticketFooter.Text.Trim(), CutAfterPrint = cutAfterPrint.Checked };
            else
            {
                var newName = name.Enabled && !string.IsNullOrWhiteSpace(name.Text) ? name.Text.Trim() : existing.Name;
                destinations[selectedIndex] = new PrinterDestination(newName, windowsPrinter.SelectedItem?.ToString() ?? existing.WindowsPrinter, ticketHeader.Text.Trim(), ticketFooter.Text.Trim(), cutAfterPrint.Checked, existing.Layout, existing.LogoPath);
            }
            list.Items[selectedIndex] = destinations[selectedIndex].Name;
        }
        MirrorRefundFromPayment();
        save(destinations);
    }

    private void MirrorRefundFromPayment()
    {
        var payment = destinations.FirstOrDefault(d => d.Name == "Payment");
        if (payment == null) return;
        var idx = destinations.FindIndex(d => d.Name == "Refund");
        if (idx < 0) return;
        destinations[idx] = destinations[idx] with { WindowsPrinter = payment.WindowsPrinter };
    }

        private void EditLayout()
    {
        if (selectedIndex < 0) return;
        var current = destinations[selectedIndex];
        var parent = Parent;
        while (parent != null && parent is not Panel { Dock: DockStyle.Fill, Visible: true }) parent = parent.Parent;
        var viewPanel = parent as Panel;
        if (viewPanel == null)
        {
            using var designer = new TicketDesignerForm(BackColor, Color.FromArgb(37, 47, 62), Color.FromArgb(71, 214, 174), current, (components, logo) =>
            {
                destinations[selectedIndex] = current with { Layout = components, LogoPath = logo };
                save(destinations);
                saveLayout(current, components);
            });
            designer.ShowDialog(this);
            return;
        }
        var designerPanel = new Panel { Dock = DockStyle.Fill };
        var ticketDesigner = new TicketDesignerForm(BackColor, Color.FromArgb(37, 47, 62), Color.FromArgb(71, 214, 174), current, (components, logo) =>
        {
            destinations[selectedIndex] = current with { Layout = components, LogoPath = logo };
            save(destinations);
            saveLayout(current, components);
            viewPanel.Controls.Clear();
            viewPanel.Controls.Add(this);
            this.Dock = DockStyle.Fill;
        });
        ticketDesigner.TopLevel = false;
        ticketDesigner.FormBorderStyle = FormBorderStyle.None;
        ticketDesigner.Dock = DockStyle.Fill;
        designerPanel.Controls.Add(ticketDesigner);
        ticketDesigner.Show();
        viewPanel.Controls.Clear();
        var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.FromArgb(27, 34, 46), Padding = new Padding(16, 0, 16, 0) };
        var titleLabel = new Label { Text = "Edit ticket layout", AutoSize = true, Font = new Font("Segoe UI Semibold", 16F), ForeColor = Color.White, Location = new Point(16, 14) };
        var backBtn = new Button { Text = "\u2190 Back", Size = new Size(100, 34), BackColor = Color.FromArgb(37, 47, 62), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        backBtn.FlatAppearance.BorderSize = 0;
        backBtn.Click += (_, _) =>
        {
            viewPanel.Controls.Clear();
            viewPanel.Controls.Add(this);
            this.Dock = DockStyle.Fill;
        };
        header.Controls.Add(titleLabel);
        header.Controls.Add(backBtn);
        viewPanel.Controls.Add(designerPanel);
        viewPanel.Controls.Add(header);
    }

}

internal sealed class TicketStyleEditorForm : Form
{
    private readonly List<PrinterDestination> destinations;
    private readonly Action<List<PrinterDestination>> save;
    private readonly ComboBox destinationPicker = new();
    private readonly ListBox componentList = new();
    private readonly ComboBox fontSize = new();
    private readonly CheckBox boldBox = new();
    private readonly CheckBox underlineBox = new();
    private readonly ComboBox alignmentBox = new();
    private readonly Panel previewPanel = new();
    private int selectedComponentIndex = -1;
    private static readonly (string Name, int Size)[] FontSizes = [("8", 8), ("10", 10), ("12", 12), ("15", 15), ("18", 18), ("22", 22), ("26", 26), ("32", 32)];

    public TicketStyleEditorForm(Color background, Color panelLight, Color mint, List<PrinterDestination> source, Action<List<PrinterDestination>> save)
    {
        destinations = [.. source];
        this.save = save;
        Text = "Edit ticket styles";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 620);
        MinimumSize = new Size(900, 620);
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);

        var title = new Label { Text = "Ticket style editor", AutoSize = true, Font = new Font("Segoe UI Semibold", 18F), Location = new Point(24, 20) };
        var destLabel = new Label { Text = "Printer destination", AutoSize = true, Location = new Point(24, 60) };
        destinationPicker.Location = new Point(24, 84);
        destinationPicker.Size = new Size(260, 30);
        destinationPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        destinationPicker.BackColor = panelLight;
        destinationPicker.ForeColor = Color.White;
        foreach (var d in destinations) destinationPicker.Items.Add(d.Name);
        destinationPicker.SelectedIndexChanged += (_, _) => LoadComponents();
        if (destinations.Count > 0) destinationPicker.SelectedIndex = 0;

        var componentLabel = new Label { Text = "Ticket components", AutoSize = true, Location = new Point(24, 126) };
        componentList.Location = new Point(24, 150);
        componentList.Size = new Size(240, 400);
        componentList.BackColor = panelLight;
        componentList.ForeColor = Color.White;
        componentList.BorderStyle = BorderStyle.None;
        componentList.SelectedIndexChanged += (_, _) => LoadComponentStyle();

        var styleLabel = new Label { Text = "Style settings", AutoSize = true, Font = new Font("Segoe UI Semibold", 13F), Location = new Point(300, 126) };
        var fontSizeLabel = new Label { Text = "Text size", AutoSize = true, Location = new Point(300, 158) };
        fontSize.Location = new Point(300, 182);
        fontSize.Size = new Size(170, 30);
        fontSize.DropDownStyle = ComboBoxStyle.DropDownList;
        fontSize.BackColor = panelLight;
        fontSize.ForeColor = Color.White;
        foreach (var s in FontSizes) fontSize.Items.Add(s.Name);
        fontSize.SelectedIndexChanged += (_, _) => ApplyStyle();

        boldBox.Text = "Bold";
        boldBox.AutoSize = true;
        boldBox.Location = new Point(300, 228);
        boldBox.ForeColor = Color.White;
        boldBox.CheckedChanged += (_, _) => ApplyStyle();

        underlineBox.Text = "Underline";
        underlineBox.AutoSize = true;
        underlineBox.Location = new Point(300, 262);
        underlineBox.ForeColor = Color.White;
        underlineBox.CheckedChanged += (_, _) => ApplyStyle();

        var alignLabel = new Label { Text = "Alignment", AutoSize = true, Location = new Point(300, 302) };
        alignmentBox.Location = new Point(300, 326);
        alignmentBox.Size = new Size(180, 30);
        alignmentBox.DropDownStyle = ComboBoxStyle.DropDownList;
        alignmentBox.BackColor = panelLight;
        alignmentBox.ForeColor = Color.White;
        alignmentBox.Items.AddRange(["Left", "Center", "Right"]);
        alignmentBox.SelectedIndexChanged += (_, _) => ApplyStyle();

        var previewLabel = new Label { Text = "Preview", AutoSize = true, Font = new Font("Segoe UI Semibold", 13F), Location = new Point(560, 126) };
        previewPanel.Location = new Point(560, 150);
        previewPanel.Size = new Size(310, 400);
        previewPanel.BackColor = Color.White;
        previewPanel.ForeColor = Color.Black;
        previewPanel.Paint += DrawPreview;

        var saveButton = new Button { Text = "Save changes", Location = new Point(700, 570), Size = new Size(160, 38), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        saveButton.FlatAppearance.BorderSize = 0;
        saveButton.Click += (_, _) => { save(destinations); DialogResult = DialogResult.OK; Close(); };

        Controls.AddRange([title, destLabel, destinationPicker, componentLabel, componentList, styleLabel, fontSizeLabel, fontSize, boldBox, underlineBox, alignLabel, alignmentBox, previewLabel, previewPanel, saveButton]);
        LoadComponents();
    }

    private void LoadComponents()
    {
        componentList.Items.Clear();
        selectedComponentIndex = -1;
        if (destinationPicker.SelectedIndex < 0 || destinationPicker.SelectedIndex >= destinations.Count) return;
        var dest = destinations[destinationPicker.SelectedIndex];
        foreach (var style in TicketLayout.Components(dest))
        {
            var sizeName = Array.Find(FontSizes, s => s.Size == style.FontSize);
            componentList.Items.Add($"[{style.Alignment[0]}] {(style.Bold ? "B " : "")}{(style.Underline ? "U " : "")}{sizeName.Name ?? "Small"}  {style.Component}");
        }
        if (componentList.Items.Count > 0) componentList.SelectedIndex = 0;
    }

    private void LoadComponentStyle()
    {
        if (destinationPicker.SelectedIndex < 0 || destinationPicker.SelectedIndex >= destinations.Count) return;
        if (componentList.SelectedIndex < 0) { selectedComponentIndex = -1; return; }
        selectedComponentIndex = componentList.SelectedIndex;
        var dest = destinations[destinationPicker.SelectedIndex];
        var components = TicketLayout.Components(dest);
        if (selectedComponentIndex >= components.Count) return;
        var style = components[selectedComponentIndex];
        var sizeIndex = Array.FindIndex(FontSizes, s => s.Size == style.FontSize);
        fontSize.SelectedIndex = sizeIndex >= 0 ? sizeIndex : 0;
        boldBox.Checked = style.Bold;
        underlineBox.Checked = style.Underline;
        alignmentBox.SelectedItem = style.Alignment;
        previewPanel.Invalidate();
    }

    private void ApplyStyle()
    {
        if (selectedComponentIndex < 0) return;
        if (destinationPicker.SelectedIndex < 0 || destinationPicker.SelectedIndex >= destinations.Count) return;
        var dest = destinations[destinationPicker.SelectedIndex];
        var components = TicketLayout.Components(dest).ToList();
        if (selectedComponentIndex >= components.Count) return;
        var old = components[selectedComponentIndex];
        var sizeValue = fontSize.SelectedIndex >= 0 ? FontSizes[fontSize.SelectedIndex].Size : 10;
        var sizeName = fontSize.SelectedIndex >= 0 ? FontSizes[fontSize.SelectedIndex].Name : "Small";
        components[selectedComponentIndex] = new TicketComponentStyle(
            old.Component,
            sizeValue,
            boldBox.Checked,
            underlineBox.Checked,
            alignmentBox.SelectedItem?.ToString() ?? "Left");
        destinations[destinationPicker.SelectedIndex] = dest with { Layout = components };

        componentList.Items[selectedComponentIndex] =
            $"[{(alignmentBox.SelectedItem?.ToString() ?? "Left")[0]}] {(boldBox.Checked ? "B " : "")}{(underlineBox.Checked ? "U " : "")}{sizeName}  {old.Component}";
        previewPanel.Invalidate();
    }

    private void DrawPreview(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Color.White);
        if (selectedComponentIndex < 0) return;
        if (destinationPicker.SelectedIndex < 0 || destinationPicker.SelectedIndex >= destinations.Count) return;
        var dest = destinations[destinationPicker.SelectedIndex];
        var components = TicketLayout.Components(dest);
        if (selectedComponentIndex >= components.Count) return;
        var style = components[selectedComponentIndex];
        var sample = new List<SoldLine> { new("Margherita", "Food", 2, 12.50m, "Mains") };
        var text = TicketLayout.Text(style.Component, dest, "Room 1", "Table 5", "Manager", sample, discountPercent: 10, discountAmount: 2.50m);
        if (string.IsNullOrEmpty(text)) { g.DrawString("(empty)", new Font("Consolas", 9F), Brushes.Gray, 10, 10); return; }

        var fontStyle = FontStyle.Regular;
        if (style.Bold) fontStyle |= FontStyle.Bold;
        if (style.Underline) fontStyle |= FontStyle.Underline;
        using var font = new Font("Consolas", Math.Min(style.FontSize, 20), fontStyle);
        var size = g.MeasureString(text, font, previewPanel.ClientSize.Width - 20);
        var x = style.Alignment switch
        {
            "Center" => (previewPanel.ClientSize.Width - size.Width) / 2,
            "Right" => previewPanel.ClientSize.Width - size.Width - 10,
            _ => 10
        };
        g.DrawString(text, font, Brushes.Black, new RectangleF(x, 10, previewPanel.ClientSize.Width - 20, size.Height));
    }
}

internal sealed class ProductManagerForm : Form
{
    private readonly List<Product> products;
    private readonly Action<List<Product>> save;
    private readonly ListBox list = new();
    private readonly TextBox name = new();
    private readonly ComboBox category = new();
    private readonly NumericUpDown price = new();
    private readonly TextBox color = new();
    private readonly TextBox imagePath = new();
    private readonly TextBox barcode = new();
    private readonly CheckBox weighted = new();
    private readonly FeatureSettings features;
    private readonly List<ProductVariant> variants;
    private readonly Action<List<ProductVariant>> saveVariants;
    private readonly Color bg, pl, mintC;
    private int selectedIndex = -1;

    public ProductManagerForm(Color background, Color panelLight, Color mint, List<Product> source, Action<List<Product>> save, FeatureSettings features, List<ProductVariant> sourceVariants, Action<List<ProductVariant>> saveVariants)
    {
        products = [.. source];
        this.save = save;
        this.features = features;
        variants = [.. sourceVariants];
        this.saveVariants = saveVariants;
        bg = background; pl = panelLight; mintC = mint;
        Text = "Product catalog";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 560);
        MinimumSize = new Size(620, 500);
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.Sizable;

        var title = new Label { Text = "Product catalog", AutoSize = true, Font = new Font("Segoe UI Semibold", 18F), Location = new Point(24, 20) };
        list.Location = new Point(24, 66);
        list.Size = new Size(245, 350);
        list.BackColor = panelLight;
        list.ForeColor = Color.White;
        list.BorderStyle = BorderStyle.None;
        list.SelectedIndexChanged += (_, _) => LoadProduct();
        foreach (var product in products) list.Items.Add(product.Name);

        var nameLabel = FieldLabel("Name", 310, 70);
        name.Location = new Point(310, 96);
        name.Size = new Size(350, 30);
        StyleField(name, panelLight);
        var categoryLabel = FieldLabel("Category", 310, 140);
        category.Location = new Point(310, 166);
        category.Size = new Size(220, 30);
        category.DropDownStyle = ComboBoxStyle.DropDownList;
        category.Items.AddRange(["Popular", "Mains", "Drinks", "Desserts"]);
        category.BackColor = panelLight;
        category.ForeColor = Color.White;
        var priceLabel = FieldLabel("Price", 310, 210);
        price.Location = new Point(310, 236);
        price.Size = new Size(140, 30);
        price.Minimum = 0;
        price.Maximum = 10000;
        price.DecimalPlaces = 2;
        price.Increment = 0.5m;
        price.BackColor = panelLight;
        price.ForeColor = Color.White;
        var colorLabel = FieldLabel("Color", 480, 210);
        color.Location = new Point(480, 236);
        color.Size = new Size(180, 30);
        StyleField(color, panelLight);
        var imageLabel = FieldLabel("Image", 310, 280);
        imagePath.Location = new Point(310, 306);
        imagePath.Size = new Size(250, 30);
        imagePath.ReadOnly = true;
        StyleField(imagePath, panelLight);
        var importImage = CatalogButton("Import image", 570, 306, 90, panelLight);
        importImage.Click += (_, _) => imagePath.Text = CatalogImageStore.Import() ?? imagePath.Text;
        var barcodeLabel = FieldLabel("Barcode", 310, 350);
        barcode.Location = new Point(310, 376);
        barcode.Size = new Size(250, 30);
        StyleField(barcode, panelLight);
        weighted.Text = "Priced per kg (weight item)";
        weighted.Location = new Point(570, 380);
        weighted.AutoSize = true;
        weighted.ForeColor = Color.White;

        var add = CatalogButton("New product", 24, 435, 120, panelLight);
        add.Click += (_, _) => AddProduct();
        var remove = CatalogButton("Delete", 154, 435, 90, panelLight);
        remove.Click += (_, _) => DeleteProduct();
        var saveButton = CatalogButton("Save changes", 530, 435, 130, mint);
        saveButton.ForeColor = background;
        saveButton.Click += (_, _) => SaveProduct();
        Controls.AddRange([title, list, nameLabel, name, categoryLabel, category, priceLabel, price, colorLabel, color, imageLabel, imagePath, importImage, barcodeLabel, barcode, weighted, add, remove, saveButton]);

        var row2 = new FlowLayoutPanel { Location = new Point(20, 480), Size = new Size(680, 44), FlowDirection = FlowDirection.LeftToRight, BackColor = background };
        if (features.ProductVariants)
        {
            var variantsBtn = CatalogButton("Variants…", 0, 0, 100, panelLight);
            variantsBtn.Margin = new Padding(0, 0, 8, 0);
            variantsBtn.Click += (_, _) => ShowVariants();
            var importBtn = CatalogButton("Import CSV", 0, 0, 110, panelLight);
            importBtn.Margin = new Padding(0, 0, 8, 0);
            importBtn.Click += (_, _) => ImportCsv();
            var exportBtn = CatalogButton("Export CSV", 0, 0, 110, panelLight);
            exportBtn.Margin = new Padding(0, 0, 8, 0);
            exportBtn.Click += (_, _) => ExportCsv();
            row2.Controls.AddRange([variantsBtn, importBtn, exportBtn]);
        }
        if (features.BarcodeLabels)
        {
            var labelsBtn = CatalogButton("Print barcode labels", 0, 0, 160, Color.FromArgb(70, 90, 130));
            labelsBtn.Margin = new Padding(0, 0, 8, 0);
            labelsBtn.Click += (_, _) => PrintLabels();
            row2.Controls.Add(labelsBtn);
        }
        Controls.Add(row2);
        if (products.Count > 0) list.SelectedIndex = 0;
    }

    private Label FieldLabel(string text, int x, int y) => new() { Text = text, AutoSize = true, Location = new Point(x, y) };

    private static void StyleField(Control control, Color background)
    {
        control.BackColor = background;
        control.ForeColor = Color.White;
        if (control is TextBox textBox) textBox.BorderStyle = BorderStyle.FixedSingle;
    }

    private Button CatalogButton(string text, int x, int y, int width, Color background) => new() { Text = text, Location = new Point(x, y), Size = new Size(width, 38), BackColor = background, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

    private void LoadProduct()
    {
        if (selectedIndex >= 0 && selectedIndex < products.Count && selectedIndex != list.SelectedIndex)
        {
            products[selectedIndex] = products[selectedIndex] with { Name = name.Text.Trim(), Category = category.SelectedItem?.ToString() ?? "Popular", Price = price.Value, Color = color.Text.Trim(), ImagePath = imagePath.Text.Trim(), Barcode = barcode.Text.Trim(), Weighted = weighted.Checked };
            list.Items[selectedIndex] = products[selectedIndex].Name;
        }
        if (list.SelectedIndex < 0 || list.SelectedIndex >= products.Count) return;
        selectedIndex = list.SelectedIndex;
        var product = products[selectedIndex];
        name.Text = product.Name;
        category.SelectedItem = product.Category;
        price.Value = product.Price;
        color.Text = product.Color;
        imagePath.Text = product.ImagePath;
        barcode.Text = product.Barcode;
        weighted.Checked = product.Weighted;
    }

    private void AddProduct()
    {
        products.Add(new Product("New product", "Popular", 0, "#70C1B3"));
        list.Items.Add("New product");
        list.SelectedIndex = products.Count - 1;
    }

    private void DeleteProduct()
    {
        if (selectedIndex < 0 || products.Count <= 1) return;
        products.RemoveAt(selectedIndex);
        list.Items.RemoveAt(selectedIndex);
        list.SelectedIndex = Math.Min(selectedIndex, products.Count - 1);
    }

    private void SaveProduct()
    {
        if (selectedIndex >= 0 && selectedIndex < products.Count)
        {
            products[selectedIndex] = products[selectedIndex] with { Name = name.Text.Trim(), Category = category.SelectedItem?.ToString() ?? "Popular", Price = price.Value, Color = color.Text.Trim(), ImagePath = imagePath.Text.Trim(), Barcode = barcode.Text.Trim(), Weighted = weighted.Checked };
            list.Items[selectedIndex] = products[selectedIndex].Name;
        }
        save(products);
    }

    private Product? CurrentProduct()
    {
        if (selectedIndex < 0 || selectedIndex >= products.Count) return null;
        products[selectedIndex] = products[selectedIndex] with { Name = name.Text.Trim(), Category = category.SelectedItem?.ToString() ?? "Popular", Price = price.Value, Color = color.Text.Trim(), ImagePath = imagePath.Text.Trim(), Barcode = barcode.Text.Trim(), Weighted = weighted.Checked };
        list.Items[selectedIndex] = products[selectedIndex].Name;
        return products[selectedIndex];
    }

    private void ShowVariants()
    {
        var product = CurrentProduct();
        if (product == null) { MessageBox.Show("Select a product first.", "Variants", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        using var f = new Form { Text = $"Variants — {product.Name}", Size = new Size(520, 420), StartPosition = FormStartPosition.CenterParent, BackColor = bg, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var lv = new ListView { Location = new Point(16, 16), Size = new Size(360, 250), BackColor = Color.FromArgb(27, 34, 46), ForeColor = Color.White, View = View.Details, FullRowSelect = true, MultiSelect = false };
        lv.Columns.Add("Variant", 150); lv.Columns.Add("Price", 90); lv.Columns.Add("Barcode", 110);
        void Fill()
        {
            lv.Items.Clear();
            foreach (var v in variants.Where(v => v.Product == product.Name))
                lv.Items.Add(new ListViewItem([v.Name, v.Price.ToString("C"), v.Barcode]));
        }
        Fill();
        var vName = new TextBox { Location = new Point(16, 276), Size = new Size(130, 28), BackColor = pl, ForeColor = Color.White, PlaceholderText = "Size / name" };
        var vPrice = new NumericUpDown { Location = new Point(156, 276), Size = new Size(90, 28), Minimum = 0, Maximum = 100000, DecimalPlaces = 2, Increment = 0.5m, BackColor = pl, ForeColor = Color.White };
        var vBarcode = new TextBox { Location = new Point(256, 276), Size = new Size(120, 28), BackColor = pl, ForeColor = Color.White, PlaceholderText = "Barcode" };
        var addBtn = new Button { Text = "+ Add", Location = new Point(16, 312), Size = new Size(90, 30), BackColor = mintC, ForeColor = bg, FlatStyle = FlatStyle.Flat };
        var delBtn = new Button { Text = "Remove", Location = new Point(116, 312), Size = new Size(90, 30), BackColor = Color.FromArgb(180, 70, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        var closeBtn = new Button { Text = "Close", Location = new Point(390, 312), Size = new Size(90, 30), BackColor = pl, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        addBtn.FlatAppearance.BorderSize = 0; delBtn.FlatAppearance.BorderSize = 0; closeBtn.FlatAppearance.BorderSize = 0;
        addBtn.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(vName.Text)) { MessageBox.Show("Enter a variant name (e.g. Large, 1kg).", "Variants", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            variants.Add(new ProductVariant(product.Name, vName.Text.Trim(), vPrice.Value, vBarcode.Text.Trim()));
            saveVariants(variants);
            vName.Text = ""; vBarcode.Text = ""; vPrice.Value = 0;
            Fill();
        };
        delBtn.Click += (_, _) =>
        {
            if (lv.SelectedItems.Count == 0) return;
            var vn = lv.SelectedItems[0].Text;
            variants.RemoveAll(v => v.Product == product.Name && v.Name == vn);
            saveVariants(variants);
            Fill();
        };
        closeBtn.Click += (_, _) => f.DialogResult = DialogResult.OK;
        f.Controls.AddRange([lv, vName, vPrice, vBarcode, addBtn, delBtn, closeBtn]);
        f.ShowDialog();
    }

    private Color panelLightOf() => pl;

    private void ImportCsv()
    {
        using var open = new OpenFileDialog { Filter = "CSV files|*.csv|All files|*.*" };
        if (open.ShowDialog() != DialogResult.OK) return;
        try
        {
            var lines = File.ReadAllLines(open.FileName);
            var sep = lines.Length > 0 && lines[0].Count(c => c == ';') > lines[0].Count(c => c == ',') ? ';' : ',';
            var imported = 0; var updated = 0; var skipped = 0;
            foreach (var raw in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var parts = SplitCsv(raw, sep);
                if (parts.Length < 3) { skipped++; continue; }
                if (!decimal.TryParse(parts[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var p) &&
                    !decimal.TryParse(parts[2], out p)) { skipped++; continue; }
                var pname = parts[0].Trim();
                if (pname.Length == 0) continue;
                var pcat = parts.Length > 1 && parts[1].Trim().Length > 0 ? parts[1].Trim() : "Popular";
                var pcolor = parts.Length > 3 && parts[3].Trim().Length > 0 ? parts[3].Trim() : "#70C1B3";
                var pfamily = parts.Length > 4 && parts[4].Trim().Length > 0 ? parts[4].Trim() : "General";
                var pbarcode = parts.Length > 5 ? parts[5].Trim() : "";
                var pweighted = parts.Length > 6 && parts[6].Trim().Equals("weighted", StringComparison.OrdinalIgnoreCase);
                var idx = products.FindIndex(x => string.Equals(x.Name, pname, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    products[idx] = products[idx] with { Category = pcat, Price = p, Color = pcolor, Family = pfamily, Barcode = pbarcode, Weighted = pweighted };
                    updated++;
                }
                else
                {
                    products.Add(new Product(pname, pcat, p, pcolor, pfamily, "", "", pbarcode, pweighted));
                    imported++;
                }
            }
            save(products);
            list.Items.Clear();
            foreach (var p in products) list.Items.Add(p.Name);
            if (products.Count > 0) list.SelectedIndex = 0;
            MessageBox.Show($"CSV imported:\n{imported} new product(s), {updated} updated, {skipped} skipped.", "Import CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show($"Import failed: {ex.Message}", "Import CSV", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private static string[] SplitCsv(string line, char sep)
    {
        var parts = new List<string>();
        var cur = new StringBuilder();
        var inQuotes = false;
        foreach (var c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (c == sep && !inQuotes) { parts.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(c);
        }
        parts.Add(cur.ToString());
        return [.. parts];
    }

    private void ExportCsv()
    {
        using var save = new SaveFileDialog { Filter = "CSV files|*.csv", FileName = $"products-{DateTime.Now:yyyyMMdd}.csv" };
        if (save.ShowDialog() != DialogResult.OK) return;
        var sb = new StringBuilder();
        sb.AppendLine("Name,Category,Price,Color,Family,Barcode,Weighted");
        foreach (var p in products)
            sb.AppendLine(string.Join(",", $"\"{p.Name.Replace("\"", "\"\"")}\"", p.Category, p.Price.ToString(System.Globalization.CultureInfo.InvariantCulture), p.Color, p.Family, p.Barcode, p.Weighted ? "weighted" : ""));
        File.WriteAllText(save.FileName, sb.ToString());
        MessageBox.Show($"Exported {products.Count} product(s) to:\n{save.FileName}", "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void PrintLabels()
    {
        var product = CurrentProduct();
        if (product == null) { MessageBox.Show("Select a product first.", "Labels", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (string.IsNullOrWhiteSpace(product.Barcode) || !Code39.CanEncode(product.Barcode))
        {
            MessageBox.Show("This product has no valid barcode for printing.\nAllowed: 0-9, A-Z, '-', '.' and spaces.\nSet it in the Barcode field first.", "Labels", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        using var qtyForm = new Form { Text = "How many labels?", Size = new Size(300, 170), StartPosition = FormStartPosition.CenterParent, BackColor = bg, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var qty = new NumericUpDown { Location = new Point(20, 40), Size = new Size(100, 30), Minimum = 1, Maximum = 500, Value = 1, BackColor = panelLightOf(), ForeColor = Color.White };
        var okQty = new Button { Text = "Print", Location = new Point(20, 84), Size = new Size(100, 32), BackColor = mintC, ForeColor = bg, FlatStyle = FlatStyle.Flat };
        okQty.FlatAppearance.BorderSize = 0;
        qtyForm.Controls.AddRange([new Label { Text = "Label count:", AutoSize = true, Location = new Point(20, 14) }, qty, okQty]);
        qtyForm.AcceptButton = okQty;
        okQty.Click += (_, _) => qtyForm.DialogResult = DialogResult.OK;
        if (qtyForm.ShowDialog() != DialogResult.OK) return;

        using var dialog = new PrintDialog { AllowSomePages = false };
        using var doc = new PrintDocument { DocumentName = $"Labels {product.Name}" };
        var count = (int)qty.Value;
        var barcodeBmp = Code39.Render(product.Barcode, 3);
        var done = 0;
        doc.PrintPage += (_, e) =>
        {
            if (e.Graphics == null) { e.HasMorePages = false; return; }
            var g = e.Graphics;
            const int labelW = 300, labelH = 160, gapX = 10, gapY = 10;
            var x0 = 20; var y0 = 20;
            for (var row = 0; row < 5 && done < count; row++)
            {
                for (var col = 0; col < 3 && done < count; col++)
                {
                    var x = x0 + col * (labelW + gapX);
                    var y = y0 + row * (labelH + gapY);
                    if (y + labelH > g.VisibleClipBounds.Height) break;
                    using (var border = new Pen(Color.FromArgb(120, 130, 145))) g.DrawRectangle(border, x, y, labelW, labelH);
                    using (var nameFont = new Font("Segoe UI Semibold", 11F))
                    using (var priceFont = new Font("Segoe UI", 10F))
                    using (var codeFont = new Font("Consolas", 8F))
                    using (var black = new SolidBrush(Color.Black))
                    {
                        g.DrawString(product.Name, nameFont, black, x + 8, y + 6);
                        g.DrawString(product.Price.ToString("C"), priceFont, black, x + 8, y + 28);
                        var bw = Math.Min(labelW - 16, 200);
                        var scale = (float)bw / barcodeBmp.Width;
                        var bh = Math.Min(60, (int)(barcodeBmp.Height * scale));
                        g.DrawImage(barcodeBmp, new RectangleF(x + 8, y + 54, bw, bh));
                        g.DrawString(product.Barcode, codeFont, black, x + 8, y + 54 + bh + 2);
                    }
                    done++;
                }
                if (y0 + (row + 1) * (labelH + gapY) > g.VisibleClipBounds.Height) break;
            }
            e.HasMorePages = done < count;
        };
        dialog.Document = doc;
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try { doc.Print(); }
            catch (Exception ex) { MessageBox.Show($"Print failed: {ex.Message}", "Labels", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        barcodeBmp.Dispose();
    }
}

internal sealed class FamilyManagerForm : Form
{
    private readonly List<ProductFamily> families;
    private readonly List<Product> products;
    private readonly Action<List<ProductFamily>, List<Product>> save;
    private readonly ListBox familyList = new();
    private readonly ListBox productList = new();
    private readonly TextBox familyName = new();
    private readonly TextBox familyImagePath = new();
    private readonly ComboBox familyPrinter = new();
    private readonly ComboBox productFamily = new();
    private int selectedFamily = -1;
    private int selectedProduct = -1;

    public FamilyManagerForm(Color background, Color panelLight, Color mint, List<ProductFamily> sourceFamilies, List<Product> sourceProducts, List<PrinterDestination> destinations, Action<List<ProductFamily>, List<Product>> save)
    {
        families = sourceFamilies.Select(family => new ProductFamily(family.Name, family.ImagePath, family.PrinterDestination)).ToList();
        products = [.. sourceProducts];
        this.save = save;
        Text = "Product families";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 520);
        BackColor = background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var title = new Label { Text = "Product families", AutoSize = true, Font = new Font("Segoe UI Semibold", 18F), Location = new Point(24, 20) };
        familyList.Location = new Point(24, 68);
        familyList.Size = new Size(220, 300);
        StyleList(familyList, panelLight);
        familyList.SelectedIndexChanged += (_, _) => LoadFamily();
        foreach (var family in families) familyList.Items.Add(family.Name);
        var familyLabel = new Label { Text = "Family name", AutoSize = true, Location = new Point(280, 70) };
        familyName.Location = new Point(280, 96);
        familyName.Size = new Size(400, 30);
        StyleField(familyName, panelLight);
        var imageLabel = new Label { Text = "Family image", AutoSize = true, Location = new Point(280, 130) };
        familyImagePath.Location = new Point(280, 154);
        familyImagePath.Size = new Size(300, 30);
        familyImagePath.ReadOnly = true;
        StyleField(familyImagePath, panelLight);
        var importImage = FamilyButton("Import image", 590, 154, 90, panelLight);
        importImage.Click += (_, _) => familyImagePath.Text = CatalogImageStore.Import() ?? familyImagePath.Text;
        var printerLabel = new Label { Text = "Print family to", AutoSize = true, Location = new Point(280, 190) };
        familyPrinter.Location = new Point(280, 215);
        familyPrinter.Size = new Size(220, 30);
        familyPrinter.DropDownStyle = ComboBoxStyle.DropDownList;
        familyPrinter.Items.Add("No printer");
        foreach (var destination in destinations.Where(d => d.Name != "Cancel")) familyPrinter.Items.Add(destination.Name);
        familyPrinter.SelectedIndex = 0;
        familyPrinter.BackColor = panelLight;
        familyPrinter.ForeColor = Color.White;
        var productsLabel = new Label { Text = "Assign a product to a family", AutoSize = true, Font = new Font("Segoe UI Semibold", 12F), Location = new Point(280, 260) };
        productList.Location = new Point(280, 290);
        productList.Size = new Size(220, 180);
        StyleList(productList, panelLight);
        productList.SelectedIndexChanged += (_, _) => LoadProductFamily();
        foreach (var product in products) productList.Items.Add(product.Name);
        productFamily.Location = new Point(520, 290);
        productFamily.Size = new Size(160, 30);
        productFamily.DropDownStyle = ComboBoxStyle.DropDownList;
        productFamily.BackColor = panelLight;
        productFamily.ForeColor = Color.White;
        RefreshFamilyChoices();
        productFamily.SelectedIndexChanged += (_, _) => AssignProductFamily();
        var add = FamilyButton("New family", 24, 390, 120, panelLight);
        add.Click += (_, _) => AddFamily();
        var remove = FamilyButton("Delete", 154, 390, 90, panelLight);
        remove.Click += (_, _) => DeleteFamily();
        var saveButton = FamilyButton("Save changes", 550, 390, 130, mint);
        saveButton.ForeColor = background;
        saveButton.Click += (_, _) => SaveChanges();
        Controls.AddRange([title, familyList, familyLabel, familyName, imageLabel, familyImagePath, importImage, printerLabel, familyPrinter, productsLabel, productList, productFamily, add, remove, saveButton]);
        if (families.Count > 0) familyList.SelectedIndex = 0;
    }

    private static void StyleList(ListBox list, Color background) { list.BackColor = background; list.ForeColor = Color.White; list.BorderStyle = BorderStyle.None; }
    private static void StyleField(TextBox field, Color background) { field.BackColor = background; field.ForeColor = Color.White; field.BorderStyle = BorderStyle.FixedSingle; }
    private Button FamilyButton(string text, int x, int y, int width, Color background) => new() { Text = text, Location = new Point(x, y), Size = new Size(width, 38), BackColor = background, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

    private void LoadFamily()
    {
        if (selectedFamily >= 0 && selectedFamily < families.Count && selectedFamily != familyList.SelectedIndex)
        {
            var chosen = familyPrinter.SelectedItem?.ToString();
            var printerName = chosen == "No printer" ? "" : !string.IsNullOrWhiteSpace(chosen) ? chosen : families[selectedFamily].PrinterDestination;
            families[selectedFamily] = new ProductFamily(familyName.Text.Trim(), familyImagePath.Text.Trim(), printerName);
            familyList.Items[selectedFamily] = families[selectedFamily].Name;
        }
        if (familyList.SelectedIndex < 0 || familyList.SelectedIndex >= families.Count) return;
        selectedFamily = familyList.SelectedIndex;
        familyName.Text = families[selectedFamily].Name;
        familyImagePath.Text = families[selectedFamily].ImagePath;
        familyPrinter.SelectedItem = string.IsNullOrWhiteSpace(families[selectedFamily].PrinterDestination) ? "No printer" : families[selectedFamily].PrinterDestination;
    }

    private void RefreshFamilyChoices()
    {
        productFamily.Items.Clear();
        foreach (var family in families) productFamily.Items.Add(family.Name);
    }

    private void LoadProductFamily()
    {
        selectedProduct = productList.SelectedIndex;
        if (selectedProduct < 0 || selectedProduct >= products.Count) return;
        productFamily.SelectedItem = products[selectedProduct].Family;
    }

    private void AssignProductFamily()
    {
        if (selectedProduct < 0 || productFamily.SelectedItem == null) return;
        var product = products[selectedProduct];
        products[selectedProduct] = product with { Family = productFamily.SelectedItem.ToString()! };
    }

    private void AddFamily()
    {
        families.Add(new ProductFamily("New family"));
        familyList.Items.Add("New family");
        familyList.SelectedIndex = families.Count - 1;
        RefreshFamilyChoices();
    }

    private void DeleteFamily()
    {
        if (selectedFamily < 0 || families.Count <= 1) return;
        families.RemoveAt(selectedFamily);
        familyList.Items.RemoveAt(selectedFamily);
        familyList.SelectedIndex = Math.Min(selectedFamily, families.Count - 1);
        RefreshFamilyChoices();
    }

    private void SaveChanges()
    {
        if (selectedFamily >= 0)
        {
            var chosen = familyPrinter.SelectedItem?.ToString();
            var printerName = chosen == "No printer" ? "" : !string.IsNullOrWhiteSpace(chosen) ? chosen : families[selectedFamily].PrinterDestination;
            families[selectedFamily] = new ProductFamily(familyName.Text.Trim(), familyImagePath.Text.Trim(), printerName);
            familyList.Items[selectedFamily] = families[selectedFamily].Name;
            RefreshFamilyChoices();
        }
        save(families, products);
    }
}

internal sealed class ProductTile : Panel
{
    private readonly Product product;
    private Image? image;
    public bool OutOfStock;
    public ProductTile(Product product, int width = 220, int height = 175)
    {
        this.product = product;
        if (!string.IsNullOrWhiteSpace(product.ImagePath) && File.Exists(product.ImagePath))
        {
            using var source = Image.FromFile(product.ImagePath);
            image = new Bitmap(source);
        }
        Size = new Size(width, height);
        BackColor = Color.FromArgb(27, 34, 46);
        Cursor = Cursors.Hand;
        Padding = new Padding(Math.Max(8, width / 16));
        Paint += Draw;
        MouseEnter += (_, _) => { BackColor = Color.FromArgb(42, 55, 69); Invalidate(); };
        MouseLeave += (_, _) => { BackColor = Color.FromArgb(27, 34, 46); Invalidate(); };
    }

    private void Draw(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var imgSize = Math.Max(30, (int)(Width * 0.37));
        var imageBounds = new Rectangle(Padding.Left, Padding.Top, imgSize, imgSize);
        if (image != null)
            e.Graphics.DrawImage(image, imageBounds);
        else
        {
            using var brush = new SolidBrush(ColorTranslator.FromHtml(product.Color));
            e.Graphics.FillEllipse(brush, imageBounds);
        }
        using var nameBrush = new SolidBrush(OutOfStock ? Color.FromArgb(150, 160, 175) : Color.White);
        using var priceBrush = new SolidBrush(OutOfStock ? Color.FromArgb(120, 130, 145) : Color.FromArgb(71, 214, 174));
        var fontSize = Math.Max(8F, Width / 20F);
        using var nameFont = new Font("Segoe UI Semibold", fontSize);
        using var priceFont = new Font("Segoe UI", Math.Max(7F, fontSize - 1F));
        var textY = Padding.Top + imgSize + 6;
        e.Graphics.DrawString(product.Name, nameFont, nameBrush, Padding.Left, textY);
        e.Graphics.DrawString(product.Price.ToString("C"), priceFont, priceBrush, Padding.Left, textY + fontSize + 4);
        if (OutOfStock)
        {
            var nameSize = e.Graphics.MeasureString(product.Name, nameFont);
            using var strikePen = new Pen(Color.FromArgb(230, 90, 80), 2f);
            e.Graphics.DrawLine(strikePen, Padding.Left, textY + nameSize.Height / 2f, Padding.Left + nameSize.Width, textY + nameSize.Height / 2f);
            using var badgeFont = new Font("Segoe UI Semibold", Math.Max(9F, fontSize));
            var badge = "86";
            var badgeSize = e.Graphics.MeasureString(badge, badgeFont);
            using var badgeBrush = new SolidBrush(Color.FromArgb(180, 60, 50));
            var badgeRect = new RectangleF(Width - badgeSize.Width - 10, Padding.Top - 2, badgeSize.Width + 8, badgeSize.Height + 2);
            e.Graphics.FillRectangle(badgeBrush, badgeRect);
            e.Graphics.DrawString(badge, badgeFont, Brushes.White, badgeRect.X + 4, badgeRect.Y + 1);
            return;
        }
        using var plusFont = new Font("Segoe UI", Math.Max(12F, fontSize + 5F));
        e.Graphics.DrawString("+", plusFont, nameBrush, Width - Padding.Right - plusFont.Size - 2, textY);
    }
}

internal sealed class QrOrderServer : IDisposable
{
    private readonly Func<string, string, string, string?> handle;
    private TcpListener? listener;
    private Thread? thread;
    private volatile bool running;
    public int Port { get; }

    public QrOrderServer(int port, Func<string, string, string, string?> handle)
    {
        Port = port;
        this.handle = handle;
    }

    public void Start()
    {
        if (running) return;
        listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        running = true;
        thread = new Thread(ListenLoop) { IsBackground = true };
        thread.Start();
    }

    private void ListenLoop()
    {
        while (running)
        {
            TcpClient client;
            try { client = listener!.AcceptTcpClient(); }
            catch { break; }
            var captured = client;
            _ = Task.Run(() => HandleClient(captured));
        }
    }

    private void HandleClient(TcpClient client)
    {
        using (client)
        {
            try
            {
                client.ReceiveTimeout = 60000;
                client.SendTimeout = 60000;
                var stream = client.GetStream();
                stream.ReadTimeout = 60000;
                var request = ReadRequest(stream);
                if (request == null) return;
                var response = handle(request.Value.Method, request.Value.Path, request.Value.Body)
                    ?? "HTTP/1.1 503 Service Unavailable\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
                var bytes = Encoding.UTF8.GetBytes(response);
                stream.Write(bytes, 0, bytes.Length);
            }
            catch { }
        }
    }

    private static (string Method, string Path, string Body)? ReadRequest(NetworkStream stream)
    {
        var buffer = new byte[8192];
        var data = new MemoryStream();
        var headerEnd = -1;
        while (true)
        {
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read <= 0) return null;
            data.Write(buffer, 0, read);
            var arr = data.ToArray();
            headerEnd = IndexOfHeaderEnd(arr);
            if (headerEnd >= 0) break;
            if (data.Length > 128 * 1024) return null;
        }
        var raw = data.ToArray();
        var headerText = Encoding.UTF8.GetString(raw, 0, headerEnd);
        var lines = headerText.Split("\r\n");
        var parts = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;
        var method = parts[0];
        var path = parts[1];
        var contentLength = 0;
        for (var i = 1; i < lines.Length; i++)
        {
            var idx = lines[i].IndexOf(':');
            if (idx <= 0) continue;
            if (lines[i][..idx].Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                int.TryParse(lines[i][(idx + 1)..].Trim(), out contentLength);
        }
        var body = new List<byte>();
        for (var i = headerEnd + 4; i < raw.Length && body.Count < contentLength; i++) body.Add(raw[i]);
        while (body.Count < contentLength)
        {
            var read = stream.Read(buffer, 0, Math.Min(buffer.Length, contentLength - body.Count));
            if (read <= 0) break;
            for (var i = 0; i < read; i++) body.Add(buffer[i]);
        }
        return (method, path, Encoding.UTF8.GetString(body.ToArray()));
    }

    private static int IndexOfHeaderEnd(byte[] arr)
    {
        for (var i = 0; i + 3 < arr.Length; i++)
            if (arr[i] == 13 && arr[i + 1] == 10 && arr[i + 2] == 13 && arr[i + 3] == 10) return i;
        return -1;
    }

    public void Stop()
    {
        running = false;
        try { listener?.Stop(); } catch { }
        listener = null;
        thread = null;
    }

    public void Dispose() => Stop();
}
