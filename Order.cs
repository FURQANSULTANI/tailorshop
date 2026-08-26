namespace TailorShop;

public static class OrderStatus
{
    public const string Pending   = "Pending";
    public const string Ready     = "Ready";
    public const string Delivered = "Delivered";
}

public class Order
{
    public long    Id          { get; set; }
    public long    CustomerId  { get; set; }
    public string  Status      { get; set; } = OrderStatus.Pending;
    public string? CreatedAt   { get; set; }
    public string? UpdatedAt   { get; set; }
    public string? ReadyAt     { get; set; }
    public string? DeliveredAt { get; set; }

    public List<Measurement> Measurements { get; set; } = new();

    public List<Measurement> ForSection(string section) =>
        Measurements.Where(m => m.Section == section).ToList();
}
