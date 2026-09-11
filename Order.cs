namespace TailorShop;

public static class OrderStatus
{
    public const string Pending   = "Pending";
    public const string Ready     = "Ready";
    public const string Delivered = "Delivered";
}

public static class WhatsAppStatus
{
    public const string Pending = "Pending";
    public const string Sent    = "Sent";
    public const string Failed  = "Failed";
}

public class Order
{
    public long    Id          { get; set; }
    public long    CustomerId  { get; set; }
    public string  Status      { get; set; } = OrderStatus.Pending;
    public string? CreatedAt   { get; set; }
    public string? UpdatedAt   { get; set; }
    public string? ReadyAt      { get; set; }
    public string? DeliveredAt  { get; set; }
    public string? DeliveryDate { get; set; }

    public List<Measurement> Measurements { get; set; } = new();

    public List<Measurement> ForSection(string section) =>
        Measurements.Where(m => m.Section == section).ToList();
}

public class WhatsAppQueueEntry
{
    public long    Id           { get; set; }
    public long    OrderId      { get; set; }
    public long    CustomerId   { get; set; }
    public string  Phone        { get; set; } = "";
    public string  Message      { get; set; } = "";
    public string  Status       { get; set; } = WhatsAppStatus.Pending;
    public int     AttemptCount { get; set; }
    public string? LastError    { get; set; }
    public string? CreatedAt    { get; set; }
    public string? UpdatedAt    { get; set; }
    public string? SentAt       { get; set; }
}
