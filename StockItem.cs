namespace TailorShop;

public class StockItem
{
    public long    Id            { get; set; }
    public string  SuitType      { get; set; } = "";
    public string? Color         { get; set; }
    public int     Quantity      { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal RetailPrice   { get; set; }
    public string? CreatedAt     { get; set; }
    public string? UpdatedAt     { get; set; }

    public string Display => string.IsNullOrWhiteSpace(Color)
        ? $"{SuitType}  ({Quantity} left)"
        : $"{SuitType} - {Color}  ({Quantity} left)";
}

public class StockSale
{
    public long     Id           { get; set; }
    public long?    StockItemId  { get; set; }
    public long     OrderId      { get; set; }
    public long     CustomerId   { get; set; }
    public string   CustomerName { get; set; } = "";
    public string   SuitType     { get; set; } = "";
    public string?  Color        { get; set; }
    public int      Qty          { get; set; }
    public decimal  UnitPrice    { get; set; }
    public string?  SoldAt       { get; set; }
    public string?  ItemAddedAt   { get; set; }
    public string?  ItemUpdatedAt { get; set; }

    public decimal Total => Qty * UnitPrice;
}

public static class StockDate
{
    public const string Format = "dd-MM-yyyy";

    public static string Short(string? raw) =>
        DateTime.TryParse(raw, out var dt) ? dt.ToString(Format) : "-";

    public static string Long(string? raw) =>
        DateTime.TryParse(raw, out var dt) ? dt.ToString(Format + "  hh:mm tt") : "-";
}

public class StockSelection
{
    public long    StockItemId { get; set; }
    public int     Qty         { get; set; }
    public decimal UnitPrice   { get; set; }
}
