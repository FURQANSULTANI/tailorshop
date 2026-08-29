namespace TailorShop;

public class Customer
{
    public long    Id                { get; set; }
    public string  Name              { get; set; } = "";
    public string? Phone             { get; set; }
    public string? Address           { get; set; }
    public string? Notes             { get; set; }
    public string? CreatedAt         { get; set; }
    public string? UpdatedAt         { get; set; }
    public long?   LatestOrderId        { get; set; }
    public string? LatestOrderStatus    { get; set; }
    public string? LatestOrderCreatedAt { get; set; }

    public override string ToString() => Name;
}

public class Measurement
{
    public string  Section         { get; set; } = "";
    public string  FieldName       { get; set; } = "";
    public string? Value           { get; set; }
    public string? SelectedOptions { get; set; } // comma-separated checkbox options that are ticked
    public string? Quantity        { get; set; } // number of suits, for Point Of Sale line items
}
