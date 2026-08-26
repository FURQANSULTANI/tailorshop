using System.Text;
using System.Text.Json;

namespace TailorShop;

public class WhatsAppHealth
{
    public string Status { get; set; } = "unreachable";
    public string? Qr    { get; set; }
}

public class WhatsAppClient
{
    private readonly HttpClient _http;

    public WhatsAppClient(int port)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}/"),
            Timeout     = TimeSpan.FromSeconds(5)
        };
    }

    public WhatsAppHealth GetHealth()
    {
        try
        {
            var json = _http.GetStringAsync("health").GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new WhatsAppHealth
            {
                Status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "unreachable" : "unreachable",
                Qr     = root.TryGetProperty("qr", out var q) && q.ValueKind == JsonValueKind.String ? q.GetString() : null
            };
        }
        catch
        {
            return new WhatsAppHealth { Status = "unreachable" };
        }
    }

    public (bool Success, string? Error) Send(string phone, string message)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new { phone, message });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var resp = _http.PostAsync("send", content).GetAwaiter().GetResult();
            var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var success = root.TryGetProperty("success", out var s) && s.GetBoolean();
            var error   = root.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() : null;
            return (success, error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
