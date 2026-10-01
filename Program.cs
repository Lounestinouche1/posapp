using System.Drawing.Drawing2D;
using System.Drawing.Printing;
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

internal sealed record Product(string Name, string Category, decimal Price, string Color, string Family = "General", string ImagePath = "", string PrinterDestination = "", string Barcode = "");
internal sealed record SoldLine(string Product, string Family, int Quantity, decimal UnitPrice, string Category = "General", string PrinterDestination = "", string Note = "", string Modifiers = "", decimal Discount = 0, string DiscountType = "", int Course = 0, int Seat = 0);
internal sealed record Sale(DateTime Time, decimal Total, string Table, string Operator = "Unknown", List<SoldLine>? Items = null, int PaymentNumber = 0, int BillNumber = 0, decimal Tip = 0, string PaymentType = "Cash", string CustomerName = "", string OrderType = "Dine-in", decimal Discount = 0);
internal sealed record OpenOrder(string Room, string Table, string Operator, DateTime Created, List<SoldLine> Items, string Note = "", int OrderNumber = 0, int BillNumber = 0, string OrderType = "Dine-in", string CustomerName = "", int SeatCount = 0, bool Ready = false);
internal sealed record Room(string Name, int TableCount);
    internal sealed record ProductFamily(string Name, string ImagePath = "", string PrinterDestination = "");
internal sealed record OperatorAccount(string Name, List<string> Permissions, string Password = "");
internal sealed record ModifierGroup(string Name, List<ModifierOption> Options);
internal sealed record ModifierOption(string Name, decimal Price);
internal sealed record Customer(string Name, string Phone = "", string Email = "", int Points = 0, decimal Balance = 0);
internal sealed record Ingredient(string Name, string Unit, decimal Stock, decimal MinStock, decimal Cost = 0);
internal sealed record RecipeItem(string Ingredient, decimal Quantity);
internal sealed record ShiftRecord(string Operator, DateTime ClockIn, DateTime? ClockOut = null, decimal OpeningBalance = 0, decimal ClosingBalance = 0);
internal sealed record AuditEntry(DateTime Time, string Operator, string Action, string Detail = "");
internal sealed record HappyHourRule(string Name, TimeSpan Start, TimeSpan End, decimal DiscountPercent, List<string> Days);
internal sealed record StockAlert(string ProductName, string Ingredient, decimal Remaining, decimal MinStock);
internal sealed record RegisterSettings(string Currency, decimal TvaRate, bool PricesIncludeTva, string NetworkPath = "", bool IsMainPc = true);

internal sealed record ZReportRecord(DateTime Time, int Transactions, decimal Gross, string Operator);

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
}
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
        if (File.Exists(featureSettingsFile))
            Features = JsonSerializer.Deserialize<FeatureSettings>(File.ReadAllText(featureSettingsFile), JsonOptions) ?? Features;
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
        Sales.Add(sale);
        suppressWatcher = true;
        File.WriteAllText(file, JsonSerializer.Serialize(Sales, JsonOptions));
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
        Features = features;
        suppressWatcher = true;
        File.WriteAllText(featureSettingsFile, JsonSerializer.Serialize(features, JsonOptions));
        suppressWatcher = false;
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
    public bool ZReportPrintedToday() => ZReports.Any(z => z.Time.Date == DateTime.Today);
    public void LogAudit(string op, string action, string detail = "") { AuditLog.Add(new(DateTime.Now, op, action, detail)); SaveAuditLog(AuditLog); }

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

    public LoginForm(Color background, Color panelLight, Color mint, List<OperatorAccount> operators)
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
        var subtitle = new Label { Text = "Select operator and enter password", AutoSize = true, ForeColor = Color.FromArgb(143, 154, 171), Location = new Point(60, 60) };
        var operatorLabel = new Label { Text = "Operator", AutoSize = true, Location = new Point(30, 100) };
        var operatorList = new ComboBox { Location = new Point(30, 126), Size = new Size(300, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White };
        foreach (var op in operators) operatorList.Items.Add(op.Name);
        if (operatorList.Items.Count > 0) operatorList.SelectedIndex = 0;
        var passwordLabel = new Label { Text = "Password", AutoSize = true, Location = new Point(30, 172) };
        var passwordBox = new TextBox { Location = new Point(30, 198), Size = new Size(300, 30), BackColor = panelLight, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true };
        passwordBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) TryLogin(operators, operatorList, passwordBox); };
        var loginButton = new Button { Text = "Login", Location = new Point(30, 250), Size = new Size(300, 40), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        loginButton.FlatAppearance.BorderSize = 0;
        loginButton.Click += (_, _) => TryLogin(operators, operatorList, passwordBox);
        Controls.AddRange([title, subtitle, operatorLabel, operatorList, passwordLabel, passwordBox, loginButton]);
        AcceptButton = loginButton;
    }

    private void TryLogin(List<OperatorAccount> operators, ComboBox operatorList, TextBox passwordBox)
    {
        if (operatorList.SelectedIndex < 0) return;
        var selected = operators[operatorList.SelectedIndex];
        if (!string.IsNullOrWhiteSpace(selected.Password) && selected.Password != passwordBox.Text)
        {
            MessageBox.Show("Incorrect password.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
    private readonly List<(Product Product, int Count, int Course, string Note, int Seat)> basket = [];
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
    private readonly Panel menuPanel = new();
    private readonly Panel workspacePanel = new();
    private readonly Panel roomsPanel = new();
    private readonly Panel viewPanel = new();
    private Panel mainHeader = new();
    private Panel orderHeader = new();
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
            using var login = new LoginForm(background, panelLight, mint, store.Operators);
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

        BuildUi();
        ApplyOrderPcRestrictions();
        RenderProducts();
        RenderBasket();

        store.DataChanged += OnDataChanged;
    }

    private void OnDataChanged()
    {
        if (InvokeRequired) { BeginInvoke(OnDataChanged); return; }
        ApplyCurrencyCulture(store.RegisterSettings.Currency);
        products = store.Products;
        RenderProducts();
        RenderRooms();
        RenderBasket();
        salesLabel.Text = TodaySales();
        RefreshOpenOrders();
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
                        var idx = basket.FindIndex(b => b.Product == product && b.Note == item.Note && (!store.Features.SeatOrdering || b.Seat == item.Seat));
                        if (idx >= 0) basket[idx] = (product, basket[idx].Count + item.Quantity, basket[idx].Course, basket[idx].Note, item.Seat);
                        else basket.Add((product, item.Quantity, 0, item.Note, item.Seat));
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
        operatorButton.Text = "Operator: " + currentOperator.Name;
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
        orderHeader.Controls.AddRange([orderTitle, orderLabel, seatBar]);
        basketList.Dock = DockStyle.Fill;
        basketList.FlowDirection = FlowDirection.TopDown;
        basketList.WrapContents = false;
        basketList.AutoScroll = true;
        basketList.Padding = new Padding(0, 18, 0, 8);
        var summary = new Panel { Dock = DockStyle.Bottom, Height = 204 };
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
        splitBillButton = ActionButton("Split bill", 158, 166, 74, 34, Color.FromArgb(100, 80, 140));
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
        summary.Controls.AddRange([totalCaption, totalLabel, orderButton, noteButton, payButton, splitBillButton, transferButton, mergeButton, discountButton]);
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
        var barcodeSearch = new TextBox { Dock = DockStyle.Top, Height = 36, BackColor = panelLight, ForeColor = Color.White, PlaceholderText = "Scan barcode...", Visible = store.Features.BarcodeScanning };
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
            tile.OutOfStock = store.EightySix.Contains(product.Name);
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
        roomButtons.BringToFront();
        if (store.Rooms.Count > 0)
            RenderTableMap(store.Rooms[0]);
    }

    private void RenderTableMap(Room room)
    {
        tableMap.Controls.Clear();
        var title = new Label { Text = room.Name + "  •  " + room.TableCount + " tables", AutoSize = true, Font = new Font("Segoe UI Semibold", 15F), Location = new Point(4, 5) };
        tableMap.Controls.Add(title);
        for (var index = 1; index <= room.TableCount; index++)
        {
            var tableName = "Table " + index;
            var openOrder = store.GetOpenOrder(room.Name, tableName);
            var isTaken = openOrder != null;
            var status = isTaken ? "Taken" : "Available";
            var color = isTaken ? Color.FromArgb(180, 90, 60) : panelLight;
            var table = ActionButton($"Table {index}\n{status}", 0, 0, 128, 72, color);
            table.Location = new Point(10 + ((index - 1) % 4) * 150, 48 + ((index - 1) / 4) * 98);
            table.ForeColor = Color.White;
            table.Click += (_, _) =>
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
                            var idx = basket.FindIndex(b => b.Product == product && b.Note == item.Note && (!store.Features.SeatOrdering || b.Seat == item.Seat));
                            if (idx >= 0) basket[idx] = (product, basket[idx].Count + item.Quantity, basket[idx].Course, basket[idx].Note, item.Seat);
                            else basket.Add((product, item.Quantity, 0, item.Note, item.Seat));
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
        });
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        panel.Controls.Add(form);
        form.Show();
        ShowView(panel, "Manage products", ShowSettings);
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
            var row = new Panel { Width = rowWidth, Height = hasNote ? 78 : 58, BackColor = panelLight, Margin = new Padding(0, 0, 0, 8), Padding = new Padding(12, 8, 8, 6) };
            var seatTag = store.Features.SeatOrdering && item.Seat > 0 ? $"[S{item.Seat}] " : "";
            var name = new Label { Text = $"{seatTag}{item.Count} \u00d7 {item.Product.Name}", AutoSize = true, Location = new Point(10, 8) };
            var price = new Label { Text = $"{item.Product.Price:C} each  \u2022  {(item.Product.Price * item.Count):C}", AutoSize = true, ForeColor = mint, Location = new Point(10, 30) };
            if (hasNote)
            {
                var noteLabel = new Label { Text = "\u270e " + item.Note, AutoSize = true, ForeColor = Color.FromArgb(200, 180, 100), Location = new Point(10, 52), Font = new Font("Segoe UI", 8F) };
                row.Controls.Add(noteLabel);
            }
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
        RenderSeatBar();
        totalLabel.Text = NetTotal().ToString("C");
        discountButton.Text = basketDiscountPercent > 0 ? $"Discount {basketDiscountPercent:0.#}%" : "Discount";
        discountButton.Visible = store.Features.Discounts && (currentOperator == null || HasPermission("Discounts"));
        basketList.ResumeLayout();
    }

    private string OrderContextText() => $"Operator: {currentOperator.Name}\nRoom: {selectedRoom}  •  {selectedTable}\nDine in";

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
        if (store.EightySix.Contains(product.Name))
        {
            MessageBox.Show($"{product.Name} is out of stock (86'd).", "86 board", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
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
        var index = basket.FindIndex(item => item.Product.Name == product.Name && item.Product.PrinterDestination == product.PrinterDestination && (!store.Features.SeatOrdering || item.Seat == activeSeat));
        if (index >= 0) basket[index] = (new Product(product.Name, product.Category, finalPrice, product.Color, product.Family, product.ImagePath, product.PrinterDestination, product.Barcode), basket[index].Count + 1, basket[index].Course, string.IsNullOrEmpty(basket[index].Note) ? note : basket[index].Note + ", " + note, basket[index].Seat);
        else basket.Add((new Product(product.Name, product.Category, finalPrice, product.Color, product.Family, product.ImagePath, product.PrinterDestination, product.Barcode), 1, 0, note, store.Features.SeatOrdering ? activeSeat : 0));
        if (!string.IsNullOrWhiteSpace(modifiers) || !string.IsNullOrWhiteSpace(note))
        {
            var soldIndex = basket.Count - 1;
            if (index >= 0) soldIndex = index;
        }
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

    private void PlaceOrder()
    {
        if (basket.Count == 0) return;
        string orderType = "Dine-in";
        using var typeForm = new Form { Text = "Order type", Size = new Size(320, 200), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 12F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var typeLabel = new Label { Text = "Select order type:", AutoSize = true, Location = new Point(30, 20) };
        var dineBtn = new Button { Text = "Dine-in", Size = new Size(260, 44), Location = new Point(30, 55), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 11F) };
        var takeBtn = new Button { Text = "Takeaway", Size = new Size(260, 44), Location = new Point(30, 105), BackColor = Color.FromArgb(55, 120, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 11F) };
        dineBtn.FlatAppearance.BorderSize = 0; takeBtn.FlatAppearance.BorderSize = 0;
        dineBtn.Click += (_, _) => { orderType = "Dine-in"; typeForm.DialogResult = DialogResult.OK; };
        takeBtn.Click += (_, _) => { orderType = "Takeaway"; typeForm.DialogResult = DialogResult.OK; };
        typeForm.Controls.AddRange([typeLabel, dineBtn, takeBtn]);
        if (typeForm.ShowDialog() != DialogResult.OK) return;

        var lines = basket.Select(item => new SoldLine(item.Product.Name, item.Product.Family, item.Count, item.Product.Price, item.Product.Category, ResolvePrinter(item.Product), item.Note, Seat: item.Seat)).ToList();
        var existing = store.GetOpenOrder(selectedRoom, selectedTable);
        if (existing != null)
        {
            var merged = existing.Items.Concat(lines).ToList();
            store.RemoveOpenOrder(selectedRoom, selectedTable);
            store.AddOpenOrder(new OpenOrder(selectedRoom, selectedTable, currentOperator.Name, existing.Created, merged, existing.Note, existing.OrderNumber, existing.BillNumber, OrderType: orderType, SeatCount: Math.Max(existing.SeatCount, merged.Max(l => l.Seat))));
        }
        else
        {
            var orderNum = store.NextOrderNumber();
            store.AddOpenOrder(new OpenOrder(selectedRoom, selectedTable, currentOperator.Name, DateTime.Now, lines, "", orderNum, orderNum, OrderType: orderType, SeatCount: lines.Count > 0 ? lines.Max(l => l.Seat) : 0));
        }
        PrintTickets(lines, orderType: orderType);
        MessageBox.Show($"Order placed for {selectedTable}\n{NetTotal():C}\n{orderType}", "Order saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        basket.Clear();
        activeSeat = 1;
        hasOpenOrder = true;
        UpdateOrderButtons();
        RenderBasket();
        var currentRoom = store.Rooms.FirstOrDefault(r => r.Name == selectedRoom);
        if (currentRoom != null) RenderTableMap(currentRoom);
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
            Size = new Size(400, 565),
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
        var cancelBtn = new Button { Text = "Cancel", Location = new Point(260, 449), Size = new Size(110, 50), BackColor = panelLight, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F) };
        cancelBtn.FlatAppearance.BorderSize = 0;
        cancelBtn.Click += (_, _) => form.DialogResult = DialogResult.Cancel;

        form.Controls.AddRange([orderCaption, percentCaption, percentValue, resultCaption, resultValue, applyBtn, cancelBtn]);
        form.AcceptButton = applyBtn;
        form.CancelButton = cancelBtn;
        RefreshPreview();
        form.ShowDialog(this);
        RenderBasket();
    }

    private void CompleteSale()
    {
        if (basket.Count == 0) return;

        string orderType = "Dine-in";
        using var typeForm = new Form { Text = "Order type", Size = new Size(320, 200), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 12F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var typeLabel = new Label { Text = "Select order type:", AutoSize = true, Location = new Point(30, 20) };
        var dineBtn = new Button { Text = "Dine-in", Size = new Size(260, 44), Location = new Point(30, 55), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 11F) };
        var takeBtn = new Button { Text = "Takeaway / Delivery", Size = new Size(260, 44), Location = new Point(30, 105), BackColor = Color.FromArgb(55, 120, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 11F) };
        dineBtn.FlatAppearance.BorderSize = 0; takeBtn.FlatAppearance.BorderSize = 0;
        dineBtn.Click += (_, _) => { orderType = "Dine-in"; typeForm.DialogResult = DialogResult.OK; };
        takeBtn.Click += (_, _) => { orderType = "Takeaway"; typeForm.DialogResult = DialogResult.OK; };
        typeForm.Controls.AddRange([typeLabel, dineBtn, takeBtn]);
        if (typeForm.ShowDialog() != DialogResult.OK) return;

        decimal discount = DiscountAmount(); decimal tip = 0; string paymentType = "Cash"; string customerName = "";

        using var payForm = new Form { Text = "Payment", Size = new Size(420, 440), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var yPos = 15;
        Control MakeRow(string label, Control ctrl) { var p = new Panel { Location = new Point(20, yPos), Size = new Size(370, 38) }; var l = new Label { Text = label, AutoSize = true, Location = new Point(0, 8) }; ctrl.Location = new Point(150, 4); ctrl.Size = new Size(ctrl.Width, 30); p.Controls.AddRange([l, ctrl]); yPos += 44; return p; }

        var totalBeforeDiscount = Total();
        var totalLabel = new Label { Text = $"Total: {totalBeforeDiscount - discount:C}", AutoSize = true, Font = new Font("Segoe UI Semibold", 14F), ForeColor = mint, Location = new Point(20, yPos) }; yPos += 36;

        var discountValue = new Label { Text = basketDiscountPercent > 0 ? $"-{discount:C} ({basketDiscountPercent:0.#}%)" : "None", AutoSize = true, ForeColor = basketDiscountPercent > 0 ? Color.Orange : Color.FromArgb(143, 154, 171) };
        var tipInput = new NumericUpDown { Minimum = 0, Maximum = 10000, Value = 0, Size = new Size(120, 30), BackColor = panelLight, ForeColor = Color.White, Location = new Point(150, 4) };

        var paymentCombo = new ComboBox { Size = new Size(150, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelLight, ForeColor = Color.White, Location = new Point(150, 4) };
        paymentCombo.Items.AddRange(["Cash", "Card", "Transfer", "Mobile"]); paymentCombo.SelectedIndex = 0;

        var customerCombo = new ComboBox { Size = new Size(150, 30), DropDownStyle = ComboBoxStyle.DropDown, BackColor = panelLight, ForeColor = Color.White, Location = new Point(150, 4) };
        customerCombo.Items.Add("(none)");
        foreach (var c in store.Customers) customerCombo.Items.Add(c.Name);
        customerCombo.SelectedIndex = 0;

        payForm.Controls.AddRange([
            totalLabel,
            MakeRow("Discount:", discountValue),
            MakeRow("Tip:", tipInput),
            MakeRow("Payment:", paymentCombo),
            MakeRow("Customer:", customerCombo)
        ]);

        var payOk = new Button { Text = $"Accept payment  {totalBeforeDiscount - discount:C}", Location = new Point(20, yPos), Size = new Size(370, 48), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 12F) };
        payOk.FlatAppearance.BorderSize = 0;
        payOk.Click += (_, _) => payForm.DialogResult = DialogResult.OK;
        payForm.Controls.Add(payOk);
        payForm.AcceptButton = payOk;

        if (payForm.ShowDialog() != DialogResult.OK) return;

        tip = tipInput.Value;
        paymentType = paymentCombo.Text;
        customerName = customerCombo.Text == "(none)" ? "" : customerCombo.Text;

        if (store.Features.Loyalty && !string.IsNullOrWhiteSpace(customerName))
        {
            var cust = store.Customers.FirstOrDefault(c => c.Name == customerName);
            if (cust != null)
            {
                var idx = store.Customers.IndexOf(cust);
                store.Customers[idx] = cust with { Points = cust.Points + (int)(totalBeforeDiscount - discount) };
                store.SaveCustomers(store.Customers);
            }
        }

        var lines = basket.Select(item => new SoldLine(item.Product.Name, item.Product.Family, item.Count, item.Product.Price, item.Product.Category, ResolvePrinter(item.Product), item.Note, Seat: item.Seat)).ToList();
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
        var finalTotal = totalBeforeDiscount - discount + tip;
        store.AddSale(new Sale(DateTime.Now, finalTotal, selectedTable, currentOperator.Name, lines, paymentNum, billNum, tip, paymentType, customerName, orderType, discount));
        if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Payment", $"{finalTotal:C} ({paymentType}) {orderType} discount={discount:C} tip={tip:C}");
        if (store.Features.StockManagement && store.Recipes.Count > 0)
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
            if (alerts.Count > 0)
                MessageBox.Show("LOW STOCK ALERTS:\n\n" + string.Join("\n", alerts.Select(a => $"• {a.Ingredient}: {a.Remaining:F1} remaining (min: {a.MinStock:F1})\n  Used by: {a.ProductName}")),
                    "Stock Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        store.RemoveOpenOrder(selectedRoom, selectedTable);

        var payDest = store.PrinterDestinations.FirstOrDefault(d => d.Name == "Payment");
        if (payDest != null && !string.IsNullOrWhiteSpace(payDest.WindowsPrinter))
        {
            try
            {
                if (isOrderPc)
                {
                    var escPosBytes = BuildEscPosBytes(payDest, lines, selectedRoom, selectedTable, currentOperator.Name, billNumber: billNum, paymentNumber: paymentNum, discountPercent: basketDiscountPercent, discountAmount: discount);
                    TcpPrintClient.SendPrintJob(GetMainPcHost(), payDest.WindowsPrinter, escPosBytes);
                }
                else if (payDest.CutAfterPrint)
                {
                    try { RawPrinter.PrintTicket(payDest, selectedRoom, selectedTable, currentOperator.Name, lines, ResolvePrinterName(payDest), billNumber: billNum, paymentNumber: paymentNum, discountPercent: basketDiscountPercent, discountAmount: discount); }
                    catch (InvalidOperationException) { PrintWindowsTicket(payDest, lines, billNumber: billNum, paymentNumber: paymentNum, discountPercent: basketDiscountPercent, discountAmount: discount); }
                }
                else PrintWindowsTicket(payDest, lines, billNumber: billNum, paymentNumber: paymentNum, discountPercent: basketDiscountPercent, discountAmount: discount);
            }
            catch (Exception error)
            {
                MessageBox.Show($"Could not print payment: {error.Message}", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        MessageBox.Show($"Payment accepted\n{finalTotal:C}  •  {selectedTable}", "Sale complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

    private void PrintTickets(List<SoldLine> lines, int orderNumber = 0, int billNumber = 0, string orderType = "")
    {
        var existing = store.GetOpenOrder(selectedRoom, selectedTable);
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
                            var escPosBytes = BuildEscPosBytes(destination, ticketLines, selectedRoom, selectedTable, currentOperator.Name, orderNum, billNum, orderType: orderType);
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
                            RawPrinter.PrintTicket(destination, selectedRoom, selectedTable, currentOperator.Name, ticketLines, orderNumber: orderNum, billNumber: billNum, orderType: orderType);
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
                    var escPosBytes = BuildEscPosBytes(destination, ticketLines, selectedRoom, selectedTable, currentOperator.Name, orderNum, billNum, orderType: orderType);
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
                    PrintWindowsTicket(destination, ticketLines, orderNum, billNum, orderType: orderType);
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


        private void PrintWindowsTicket(PrinterDestination destination, List<SoldLine> ticketLines, int orderNumber = 0, int billNumber = 0, int paymentNumber = 0, string orderType = "", decimal discountPercent = 0, decimal discountAmount = 0)
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
                    var text = TicketLayout.Text(style.Component, destination, selectedRoom, selectedTable, currentOperator.Name, ticketLines, orderNumber, billNumber, paymentNumber, orderType, discountPercent, discountAmount);
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
        var form = new SalesReportForm(background, panelLight, mint, store.Sales, store.RegisterSettings.Currency);
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        panel.Controls.Add(form);
        form.Show();
        ShowView(panel, "Sales report", ShowSettings);
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
                clockInOutBtn.Visible = features.ClockInOut;
                stockBtn.Visible = features.StockManagement;
                kdsNavButton.Visible = features.KitchenDisplay;
                eightySixNavButton.Visible = features.EightySixBoard;
                seatBar.Visible = features.SeatOrdering;
                orderHeader.Height = features.SeatOrdering ? 116 : 102;
                RenderSeatBar();
            },
            ShowModifierManager, ShowCustomerManager, ShowHappyHourManager, ShowAuditLogViewer, ShowShiftReport, ShowCashDrawer);
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
            operatorButton.Text = "Operator: " + currentOperator.Name;
            greetingLabel.Text = GreetingText();
            orderLabel.Text = OrderContextText();
            ApplyPermissions();
            clockInOutBtn.Visible = store.Features.ClockInOut;
            stockBtn.Visible = store.Features.StockManagement;
            kdsNavButton.Visible = store.Features.KitchenDisplay;
            eightySixNavButton.Visible = store.Features.EightySixBoard;
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
        return $"{greeting}, {currentOperator.Name}";
    }

    private bool HasPermission(string permission) => currentOperator.Permissions.Contains("All permissions") || currentOperator.Permissions.Contains(permission);

    private void ApplyPermissions()
    {
        roomsNavButton.Visible = HasPermission("Manage rooms");
        settingsNavButton.Visible = HasPermission("Settings");
    }

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
                if (p != null) basket.Add((p, item.Quantity, 0, item.Note, item.Seat));
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
                var tn = "Table " + i;
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
                var tn = "Table " + i;
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
            var idx = basket.FindIndex(b => b.Product == product && b.Note == item.Note && (!store.Features.SeatOrdering || b.Seat == item.Seat));
            if (idx >= 0) basket[idx] = (product, basket[idx].Count + item.Quantity, basket[idx].Course, basket[idx].Note, basket[idx].Seat);
            else basket.Add((product, item.Quantity, 0, item.Note, item.Seat));
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

    private void ShowCashDrawer()
    {
        using var form = new Form { Text = "Cash Drawer", Size = new Size(400, 250), StartPosition = FormStartPosition.CenterParent, BackColor = background, ForeColor = Color.White, Font = new Font("Segoe UI", 10F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var todayCash = store.Sales.Where(s => s.Time.Date == DateTime.Today && s.PaymentType == "Cash").Sum(s => s.Total);
        var countLabel = new Label { Text = $"Today's cash sales: {todayCash:C}", AutoSize = true, Location = new Point(20, 20), Font = new Font("Segoe UI", 11F), ForeColor = mint };
        var countLabel2 = new Label { Text = "Enter cash in drawer:", AutoSize = true, Location = new Point(20, 60) };
        var cashInput = new NumericUpDown { Minimum = 0, Maximum = 100000, Value = todayCash, Location = new Point(20, 85), Size = new Size(200, 28), BackColor = panelLight, ForeColor = Color.White, DecimalPlaces = 2 };
        var diffLabel = new Label { Text = "", AutoSize = true, Location = new Point(20, 125), ForeColor = mint };
        cashInput.ValueChanged += (_, _) => { var diff = cashInput.Value - todayCash; diffLabel.Text = diff >= 0 ? $"Over: {diff:C}" : $"Short: {Math.Abs(diff):C}"; diffLabel.ForeColor = diff >= 0 ? mint : Color.FromArgb(200, 80, 80); };
        var closeBtn = new Button { Text = "Close drawer", Location = new Point(20, 155), Size = new Size(180, 34), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        closeBtn.FlatAppearance.BorderSize = 0;
        closeBtn.Click += (_, _) => { if (store.Features.AuditLog) store.LogAudit(currentOperator.Name, "Cash Drawer", $"Counted: {cashInput.Value:C}"); form.DialogResult = DialogResult.OK; };
        form.Controls.AddRange([countLabel, countLabel2, cashInput, diffLabel, closeBtn]);
        form.ShowDialog();
    }

    private void ShowStockManagement()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        var lv = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true };
        lv.Columns.Add("Ingredient", 160); lv.Columns.Add("Stock", 80); lv.Columns.Add("Unit", 60); lv.Columns.Add("Min Stock", 80); lv.Columns.Add("Cost", 80);
        foreach (var i in store.Ingredients) lv.Items.Add(new ListViewItem([i.Name, i.Stock.ToString("F1"), i.Unit, i.MinStock.ToString("F1"), i.Cost.ToString("C")]));
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
                    var out86 = store.EightySix.Contains(product.Name);
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
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        var lv = new ListView { Dock = DockStyle.Fill, BackColor = panelLight, ForeColor = Color.White, View = View.Details, FullRowSelect = true };
        lv.Columns.Add("Name", 120); lv.Columns.Add("Phone", 100); lv.Columns.Add("Email", 120); lv.Columns.Add("Points", 60);
        foreach (var c in store.Customers) lv.Items.Add(new ListViewItem([c.Name, c.Phone, c.Email, c.Points.ToString()]));
        var addBtn = new Button { Text = "Add customer", Dock = DockStyle.Bottom, Height = 36, BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        addBtn.FlatAppearance.BorderSize = 0;
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
            if (f.ShowDialog() == DialogResult.OK) ShowCustomerManager();
        };
        panel.Controls.Add(lv); panel.Controls.Add(addBtn);
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
    private int selectedIndex = -1;
    private static readonly string[] PermissionNames = ["Take payments", "Discounts", "Refunds", "View sales", "Manage products", "Manage rooms", "Manage operators", "Settings"];

    public OperatorManagerForm(Color background, Color panelLight, Color mint, List<OperatorAccount> source, Action<List<OperatorAccount>> save)
    {
        this.operators = source.Select(op => new OperatorAccount(op.Name, [.. op.Permissions], op.Password)).ToList();
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
        var permissionsLabel = new Label { Text = "Permissions", AutoSize = true, Font = new Font("Segoe UI Semibold", 12F), Location = new Point(280, 210) };
        permissions.Location = new Point(280, 240);
        permissions.Size = new Size(430, 230);
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
        Controls.AddRange([title, list, nameLabel, name, passwordLabel, password, permissionsLabel, permissions, add, remove, saveButton]);
        if (operators.Count > 0) list.SelectedIndex = 0;
    }

    private Button ManagerButton(string text, int x, int y, int width, Color color) => new() { Text = text, Location = new Point(x, y), Size = new Size(width, 38), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

    private void LoadSelected()
    {
        if (selectedIndex >= 0 && selectedIndex < operators.Count && selectedIndex != list.SelectedIndex)
        {
            var selectedPermissions = permissionBoxes.Where(pair => pair.Value.Checked).Select(pair => pair.Key).ToList();
            operators[selectedIndex] = new OperatorAccount(name.Text.Trim(), selectedPermissions, password.Text);
            list.Items[selectedIndex] = operators[selectedIndex].Name;
        }
        if (list.SelectedIndex < 0 || list.SelectedIndex >= operators.Count) return;
        selectedIndex = list.SelectedIndex;
        var op = operators[selectedIndex];
        name.Text = op.Name;
        password.Text = op.Password;
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
        operators[selectedIndex] = new OperatorAccount(name.Text.Trim(), selectedPermissions, password.Text);
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
    private Action? _mOps, _mProd, _mFam, _mRooms, _showSales, _mCur, _mPrint, _endDay, _showNet, _showMod, _showCust, _showHappy, _showAudit, _showShift, _showCash;
    private bool _canOps, _canProd, _canRooms, _canSales;

    public SettingsForm(Color background, Color panelLight, Color mint, Action manageOperators, Action manageProducts, Action manageFamilies, Action manageRooms, Action showSales, Action manageCurrency, Action managePrinters, Action endDay, Action showNetwork, bool canManageOperators, bool canManageProducts, bool canManageRooms, bool canViewSales, FeatureSettings features, Action<FeatureSettings> saveFeatures, Action? showModifierManager = null, Action? showCustomerManager = null, Action? showHappyHourManager = null, Action? showAuditLog = null, Action? showShiftReport = null, Action? showCashDrawer = null)
    {
        this.features = features;
        this.saveFeatures = saveFeatures;
        _bg = background; _pl = panelLight; _mint = mint;
        _mOps = manageOperators; _mProd = manageProducts; _mFam = manageFamilies; _mRooms = manageRooms;
        _showSales = showSales; _mCur = manageCurrency; _mPrint = managePrinters; _endDay = endDay; _showNet = showNetwork;
        _canOps = canManageOperators; _canProd = canManageProducts; _canRooms = canManageRooms; _canSales = canViewSales;
        _showMod = showModifierManager; _showCust = showCustomerManager; _showHappy = showHappyHourManager;
        _showAudit = showAuditLog; _showShift = showShiftReport; _showCash = showCashDrawer;
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
            btn.Click += (_, _) => ShowCategory(kv.Key, background, panelLight, mint, manageOperators, manageProducts, manageFamilies, manageRooms, showSales, manageCurrency, managePrinters, endDay, showNetwork, canManageOperators, canManageProducts, canManageRooms, canViewSales, showModifierManager, showCustomerManager, showHappyHourManager, showAuditLog, showShiftReport, showCashDrawer);
            catPanel.Controls.Add(btn);
            catY += 42;
        }

        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 90, BackColor = Color.FromArgb(18, 24, 36) };
        var closeBtn = new Button { Text = "Done", Size = new Size(176, 38), Location = new Point(12, 8), BackColor = mint, ForeColor = background, FlatStyle = FlatStyle.Flat };
        closeBtn.FlatAppearance.BorderSize = 0;
        closeBtn.Click += (_, _) => { DialogResult = DialogResult.OK; };
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
                if (pi.CanWrite && pi.GetValue(features) is bool bv0) pi.SetValue(f, bv0);
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

    private void ShowCategory(string key, Color bg, Color pl, Color mint, Action mOps, Action mProd, Action mFam, Action mRooms, Action showSales, Action mCur, Action mPrint, Action endDay, Action showNet, bool canOps, bool canProd, bool canRooms, bool canSales, Action? showMod, Action? showCust, Action? showHappy, Action? showAudit, Action? showShift, Action? showCash)
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
                break;
            case "network":
                AddBtn("Network settings", showNet);
                break;
            case "ordering":
                AddToggle("SplitBill", "Split bill (by seat or item)");
                AddToggle("TableTransfer", "Table transfer / merge");
                AddToggle("Modifiers", "Product modifiers (extras, size, options)");
                AddBtn("Manage modifier groups", showMod!, features.Modifiers);
                AddToggle("ItemNotes", "Special instructions per item");
                AddToggle("CourseManagement", "Course-by-course sending to kitchen");
                AddToggle("KitchenDisplay", "Kitchen display screen (KDS)");
                AddToggle("EightySixBoard", "86 board (mark products out of stock)");
                AddToggle("SeatOrdering", "Seat-level ordering (items assigned to seats)");
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
                break;
            case "staff":
                AddToggle("ClockInOut", "Clock in / out");
                AddToggle("ShiftReports", "Shift reports (Rapport Z)");
                AddBtn("View shift reports", showShift!, features.ShiftReports);
                AddToggle("AuditLog", "Audit log (track all actions)");
                AddBtn("View audit log", showAudit!, features.AuditLog);
                break;
            case "inventory":
                AddToggle("StockManagement", "Ingredient stock / recipe deduction");
                AddToggle("InventoryAlerts", "Low stock alerts");
                AddToggle("BarcodeScanning", "Barcode scanning");
                break;
            case "customers":
                AddToggle("CustomerDatabase", "Customer database");
                AddBtn("Manage customers", showCust!, features.CustomerDatabase);
                AddToggle("Loyalty", "Loyalty program (points & rewards)");
                break;
            case "operations":
                AddToggle("DeliveryOrders", "Delivery orders");
                AddToggle("MultiBranch", "Multi-branch support");
                break;
            case "receipts":
                AddToggle("EmailReceipts", "Email / SMS receipts");
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
        foreach (var (order, timer) in cards) UpdateTimer(order, timer);
    }

    private void UpdateTimer(OpenOrder order, Label timer)
    {
        var elapsed = DateTime.Now - order.Created;
        timer.Text = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        timer.ForeColor = order.Ready ? Color.FromArgb(130, 140, 155)
            : elapsed.TotalMinutes < 10 ? Color.FromArgb(110, 210, 145)
            : elapsed.TotalMinutes < 20 ? Color.FromArgb(245, 180, 80)
            : Color.FromArgb(235, 90, 90);
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

    private static IEnumerable<(string Text, bool IsSub)> ItemLines(OpenOrder order)
    {
        foreach (var g in order.Items.GroupBy(i => (i.Product, i.Note, i.Modifiers, i.Seat)))
        {
            var seat = g.Key.Seat > 0 ? $"[S{g.Key.Seat}] " : "";
            yield return ($"{seat}{g.Sum(x => x.Quantity)}×  {g.Key.Product}", false);
            if (!string.IsNullOrWhiteSpace(g.Key.Modifiers)) yield return ($"+ {g.Key.Modifiers}", true);
            if (!string.IsNullOrWhiteSpace(g.Key.Note)) yield return ($"• {g.Key.Note}", true);
        }
    }

    private Panel BuildCard(OpenOrder order)
    {
        var back = order.Ready ? Color.FromArgb(36, 72, 55) : Color.FromArgb(37, 47, 62);
        var head = order.Ready ? Color.FromArgb(46, 100, 74) : panelLight;
        var card = new Panel { Size = new Size(280, 340), Margin = new Padding(8), BackColor = back };

        var title = new Label { Text = order.OrderNumber > 0 ? $"#{order.OrderNumber}  {order.Table}" : order.Table, Font = new Font("Segoe UI Semibold", 13F), ForeColor = Color.White, AutoSize = true, Location = new Point(10, 8) };
        var sub = new Label { Text = $"{order.Room}  ·  {order.OrderType}  ·  {order.Items.Sum(i => i.Quantity)} items", Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(170, 180, 195), AutoSize = true, Location = new Point(11, 34) };
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
        Render();
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
    private readonly List<Sale> sales;
    private readonly string currency;
    private readonly FlowLayoutPanel dateButtons = new();
    private readonly Button backButton = new();
    private readonly Button printButton = new();
    private readonly Label selectionLabel = new();
    private readonly Label summaryLabel = new();
    private readonly TabControl tabs = new();
    private List<Sale> currentFilteredSales = [];
    private int selectedYear;
    private int selectedMonth;

    public SalesReportForm(Color background, Color panelLight, Color mint, List<Sale> sales, string currency)
    {
        this.background = background;
        this.panelLight = panelLight;
        this.mint = mint;
        this.sales = sales;
        this.currency = currency;
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
        dateButtons.Location = new Point(24, 112);
        dateButtons.Padding = new Padding(130, 0, 0, 0);
        dateButtons.Size = new Size(720, 55);
        dateButtons.WrapContents = false;
        dateButtons.AutoScroll = true;
        tabs.Location = new Point(24, 180);
        tabs.Size = new Size(720, 305);
        tabs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        Controls.AddRange([title, selectionLabel, summaryLabel, backButton, printButton, dateButtons, tabs]);
        ShowYears();
    }

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
        foreach (var sale in selectedSales.OrderByDescending(sale => sale.Time))
        {
            var itemCount = sale.Items?.Sum(item => item.Quantity) ?? 0;
            list.Items.Add(new ListViewItem([sale.Time.ToString("dd/MM HH:mm"), sale.Table, sale.Operator, itemCount.ToString(), sale.Total.ToString("C")]));
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
            else
            {
                var newName = name.Enabled && !string.IsNullOrWhiteSpace(name.Text) ? name.Text.Trim() : existing.Name;
                destinations[selectedIndex] = new PrinterDestination(newName, printer, ticketHeader.Text.Trim(), ticketFooter.Text.Trim(), cutAfterPrint.Checked, existing.Layout, existing.LogoPath);
            }
            list.Items[selectedIndex] = destinations[selectedIndex].Name;
        }
        if (list.SelectedIndex < 0 || list.SelectedIndex >= destinations.Count) return;
        selectedIndex = list.SelectedIndex;
        var destination = destinations[selectedIndex];
        var isSpecial = destination.Name == "Cancel";
        name.Text = destination.Name;
        name.Enabled = destination.Name is not ("Cancel" or "Bill" or "Payment");
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
        if (destinations[selectedIndex].Name is "Cancel" or "Bill" or "Payment") return;
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
            else
            {
                var newName = name.Enabled && !string.IsNullOrWhiteSpace(name.Text) ? name.Text.Trim() : existing.Name;
                destinations[selectedIndex] = new PrinterDestination(newName, windowsPrinter.SelectedItem?.ToString() ?? existing.WindowsPrinter, ticketHeader.Text.Trim(), ticketFooter.Text.Trim(), cutAfterPrint.Checked, existing.Layout, existing.LogoPath);
            }
            list.Items[selectedIndex] = destinations[selectedIndex].Name;
        }
        save(destinations);
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
    private int selectedIndex = -1;

    public ProductManagerForm(Color background, Color panelLight, Color mint, List<Product> source, Action<List<Product>> save)
    {
        products = [.. source];
        this.save = save;
        Text = "Product catalog";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 520);
        MinimumSize = new Size(620, 460);
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

        var add = CatalogButton("New product", 24, 435, 120, panelLight);
        add.Click += (_, _) => AddProduct();
        var remove = CatalogButton("Delete", 154, 435, 90, panelLight);
        remove.Click += (_, _) => DeleteProduct();
        var saveButton = CatalogButton("Save changes", 530, 435, 130, mint);
        saveButton.ForeColor = background;
        saveButton.Click += (_, _) => SaveProduct();
        Controls.AddRange([title, list, nameLabel, name, categoryLabel, category, priceLabel, price, colorLabel, color, imageLabel, imagePath, importImage, add, remove, saveButton]);
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
            products[selectedIndex] = new Product(name.Text.Trim(), category.SelectedItem?.ToString() ?? "Popular", price.Value, color.Text.Trim(), products[selectedIndex].Family, imagePath.Text.Trim());
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
            products[selectedIndex] = new Product(name.Text.Trim(), category.SelectedItem?.ToString() ?? "Popular", price.Value, color.Text.Trim(), products[selectedIndex].Family, imagePath.Text.Trim());
            list.Items[selectedIndex] = products[selectedIndex].Name;
        }
        save(products);
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