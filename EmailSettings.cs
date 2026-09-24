using System.Security.Cryptography;
using System.Text;

namespace TailorShop;

public class EmailSettings
{
    public string Host       { get; set; } = "smtp.gmail.com";
    public int    Port       { get; set; } = 587;
    public bool   UseSsl     { get; set; } = true;
    public string Username   { get; set; } = "";
    public string Password   { get; set; } = "";
    public string FromName   { get; set; } = "Golden Tailor Backup";
    public string SendTo     { get; set; } = "";

    public bool IsConfigured =>
        Host.Length > 0 && Username.Length > 0 && Password.Length > 0 && SendTo.Length > 0;

    private static string FilePath => Path.Combine(AppPaths.DataFolder, "email-settings.txt");

    public static EmailSettings Load()
    {
        var s = new EmailSettings();
        try
        {
            if (!File.Exists(FilePath)) return s;

            foreach (var line in File.ReadAllLines(FilePath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
                var idx = trimmed.IndexOf('=');
                if (idx < 0) continue;

                var key = trimmed[..idx].Trim();
                var val = trimmed[(idx + 1)..].Trim();

                switch (key)
                {
                    case "HOST":      s.Host     = val; break;
                    case "PORT":      if (int.TryParse(val, out var p)) s.Port = p; break;
                    case "USE_SSL":   s.UseSsl   = val.Equals("true", StringComparison.OrdinalIgnoreCase); break;
                    case "USERNAME":  s.Username = val; break;
                    case "PASSWORD":  s.Password = Unprotect(val); break;
                    case "FROM_NAME": s.FromName = val; break;
                    case "SEND_TO":   s.SendTo   = val; break;
                }
            }
        }
        catch
        {
        }

        return s;
    }

    public bool Save()
    {
        try
        {
            var lines = new[]
            {
                $"HOST={Host}",
                $"PORT={Port}",
                $"USE_SSL={(UseSsl ? "true" : "false")}",
                $"USERNAME={Username}",
                $"PASSWORD={Protect(Password)}",
                $"FROM_NAME={FromName}",
                $"SEND_TO={SendTo}"
            };
            File.WriteAllLines(FilePath, lines);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string Protect(string plain)
    {
        if (plain.Length == 0) return "";
        try
        {
            var bytes = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(plain), null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(bytes);
        }
        catch
        {
            return "";
        }
    }

    private static string Unprotect(string stored)
    {
        if (stored.Length == 0) return "";
        try
        {
            var bytes = ProtectedData.Unprotect(
                Convert.FromBase64String(stored), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return "";
        }
    }
}
