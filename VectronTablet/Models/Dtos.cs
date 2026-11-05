namespace VectronTablet.Models;

public class BootstrapInfo
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public string? App { get; set; }
    public string? ServerTime { get; set; }
    public string? Currency { get; set; }
    public decimal TvaRate { get; set; }
    public bool PricesIncludeTva { get; set; }
    public bool CourseManagement { get; set; }
    public bool SeatOrdering { get; set; }
    public bool ItemNotes { get; set; }
    public bool ModifiersEnabled { get; set; }
    public bool PinLogin { get; set; }
    public bool ProductVariants { get; set; }
    public bool WeightedItems { get; set; }
    public bool HappyHour { get; set; }
    public List<string> CourseNames { get; set; } = [];
    public List<OperatorInfo> Operators { get; set; } = [];
    public List<RoomInfo> Rooms { get; set; } = [];
}

public class OperatorInfo
{
    public string Name { get; set; } = "";
    public List<string> Permissions { get; set; } = [];
    public bool HasPin { get; set; }
}

public class RoomInfo
{
    public string Name { get; set; } = "";
    public int TableCount { get; set; }
}

public class MenuInfo
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public List<ProductInfo> Products { get; set; } = [];
    public List<ModGroupInfo> ModifierGroups { get; set; } = [];
    public List<string> Categories { get; set; } = [];
    public List<string> CourseNames { get; set; } = [];
}

public class ProductInfo
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public string Color { get; set; } = "#2E3A47";
    public string Family { get; set; } = "";
    public string Barcode { get; set; } = "";
    public bool Weighted { get; set; }
    public bool Eightysix { get; set; }
    public string Image { get; set; } = "";
    public List<VariantInfo> Variants { get; set; } = [];
}

public class VariantInfo
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class ModGroupInfo
{
    public string Name { get; set; } = "";
    public List<ModOptionInfo> Options { get; set; } = [];
}

public class ModOptionInfo
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class MapInfo
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public string? ServerTime { get; set; }
    public List<RoomMapInfo> Rooms { get; set; } = [];
}

public class RoomMapInfo
{
    public string Name { get; set; } = "";
    public List<TableInfo> Tables { get; set; } = [];
}

public class TableInfo
{
    public string Name { get; set; } = "";
    public bool Taken { get; set; }
    public OrderSummaryInfo? Order { get; set; }
}

public class OrderSummaryInfo
{
    public int Number { get; set; }
    public string Operator { get; set; } = "";
    public string Created { get; set; } = "";
    public int Items { get; set; }
    public decimal Total { get; set; }
    public bool Ready { get; set; }
    public int FiredCourse { get; set; }
    public bool PendingCourse { get; set; }
    public string Source { get; set; } = "";
    public string OrderType { get; set; } = "";
}

public class OrderDetailInfo
{
    public string Room { get; set; } = "";
    public string Table { get; set; } = "";
    public string Operator { get; set; } = "";
    public string Created { get; set; } = "";
    public int Number { get; set; }
    public int Bill { get; set; }
    public string Note { get; set; } = "";
    public string OrderType { get; set; } = "";
    public string Customer { get; set; } = "";
    public bool Ready { get; set; }
    public int FiredCourse { get; set; }
    public string Source { get; set; } = "";
    public int SeatCount { get; set; }
    public string DeliveryAddress { get; set; } = "";
    public decimal Total { get; set; }
    public List<LineInfo> Items { get; set; } = [];
}

public class LineInfo
{
    public string Product { get; set; } = "";
    public string Family { get; set; } = "";
    public int Qty { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; } = "";
    public string Note { get; set; } = "";
    public string Modifiers { get; set; } = "";
    public int Course { get; set; }
    public int Seat { get; set; }
    public string Printer { get; set; } = "";
}

public class OrderPostResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public bool Network { get; set; }
    public int Order { get; set; }
    public string Room { get; set; } = "";
    public string Table { get; set; } = "";
    public bool Merged { get; set; }
    public int Lines { get; set; }
    public int FiredCourse { get; set; }
    public decimal Total { get; set; }
    public decimal OrderTotal { get; set; }
    public List<string> Issues { get; set; } = [];
    public List<string> PrintWarnings { get; set; } = [];
}

public class LineRequest
{
    public string Name { get; set; } = "";
    public int Qty { get; set; } = 1;
    public string? Variant { get; set; }
    public List<string>? Modifiers { get; set; }
    public string? Note { get; set; }
    public int? Course { get; set; }
    public int? Seat { get; set; }
    public decimal? Weight { get; set; }
}

public class OrderRequest
{
    public string Room { get; set; } = "";
    public string Table { get; set; } = "";
    public string Operator { get; set; } = "";
    public string? OrderType { get; set; }
    public string? Note { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? PendingId { get; set; }
    public List<LineRequest> Items { get; set; } = [];
}

public class LoginRequest
{
    public string Name { get; set; } = "";
    public string Pin { get; set; } = "";
}

public class LoginResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public string Operator { get; set; } = "";
    public List<string> Permissions { get; set; } = [];
}

public class SimpleResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public int Course { get; set; }
    public int Lines { get; set; }
    public int Bill { get; set; }
    public List<string> PrintWarnings { get; set; } = [];
}

public class TableActionRequest
{
    public string Room { get; set; } = "";
    public string Table { get; set; } = "";
    public string? Operator { get; set; }
    public decimal? Discount { get; set; }
}

public class AlertsResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public string? ServerTime { get; set; }
    public List<AlertInfo> Alerts { get; set; } = [];
    public List<ReadyInfo> Ready { get; set; } = [];
}

public class AlertInfo
{
    public string Id { get; set; } = "";
    public string Room { get; set; } = "";
    public string Table { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Time { get; set; } = "";
}

public class ReadyInfo
{
    public string Room { get; set; } = "";
    public string Table { get; set; } = "";
    public int Order { get; set; }
    public decimal Total { get; set; }
}

public class DismissRequest
{
    public string? Id { get; set; }
    public string? Room { get; set; }
    public string? Table { get; set; }
    public string? Kind { get; set; }
}

public class LineEditRequest
{
    public string Room { get; set; } = "";
    public string Table { get; set; } = "";
    public int Index { get; set; }
    public string Operator { get; set; } = "";
    public string? Product { get; set; }
    public bool Remove { get; set; }
    public int? Qty { get; set; }
}

public class LineEditResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public int Order { get; set; }
    public bool Removed { get; set; }
    public int Qty { get; set; }
    public int Remaining { get; set; }
    public bool OrderGone { get; set; }
    public bool Unchanged { get; set; }
    public decimal OrderTotal { get; set; }
    public List<string> PrintWarnings { get; set; } = [];
}
