namespace TailorShop;

public class Customer
{
    public long    Id        { get; set; }
    public string  Name      { get; set; } = "";
    public string? Phone     { get; set; }
    public string? Address   { get; set; }
    public string? Notes     { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }

    // All measurements stored dynamically
    public List<Measurement> Measurements { get; set; } = new();

    public List<Measurement> ForSection(string section) =>
        Measurements.Where(m => m.Section == section).ToList();

    public override string ToString() => Name;
}

public class Measurement
{
    public string  Section   { get; set; } = "";
    public string  FieldName { get; set; } = "";
    public string? Value     { get; set; }
}
