namespace TailorShop;

public static class WhatsAppConfig
{
    public static int Port { get; private set; } = 4001;
    public static string SenderNumber { get; private set; } = "";

    public static void Load()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WhatsAppConfig.env");
        if (!File.Exists(path)) return;

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
            var idx = trimmed.IndexOf('=');
            if (idx < 0) continue;
            var key = trimmed[..idx].Trim();
            var val = trimmed[(idx + 1)..].Trim();
            switch (key)
            {
                case "WHATSAPP_PORT": if (int.TryParse(val, out var p)) Port = p; break;
                case "WHATSAPP_SENDER_NUMBER": SenderNumber = val; break;
            }
        }
    }
}
