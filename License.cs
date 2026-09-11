using System.Security.Cryptography;
using System.Text;

namespace TailorShop;

public enum LicenseState
{
    Valid,
    Missing,
    Invalid,
    WrongMachine,
    Expired
}

public class LicenseInfo
{
    public LicenseState State      { get; set; } = LicenseState.Missing;
    public string       MachineId  { get; set; } = "";
    public string?      CustomerName { get; set; }
    public DateTime?    ExpiresOn  { get; set; }

    public bool IsValid => State == LicenseState.Valid;
}

public static class License
{
    private const string Secret = "TKB-TailorShop-2026-#Q7pR2vLx9mZs4KdE1wYn6HbT";

    private static string FilePath =>
        Path.Combine(AppPaths.DataFolder, "license.key");

    private static string LegacyFilePath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.key");

    public static LicenseInfo Check()
    {
        var info = new LicenseInfo { MachineId = MachineId.Current };

        string key;
        try
        {
            var source = File.Exists(FilePath) ? FilePath
                       : File.Exists(LegacyFilePath) ? LegacyFilePath
                       : null;

            if (source == null)
            {
                info.State = LicenseState.Missing;
                return info;
            }

            key = File.ReadAllText(source).Trim();
        }
        catch
        {
            info.State = LicenseState.Missing;
            return info;
        }

        return Validate(key, info);
    }

    public static LicenseInfo Validate(string key, LicenseInfo? target = null)
    {
        var info = target ?? new LicenseInfo { MachineId = MachineId.Current };

        var payload = Decode(key);
        if (payload == null)
        {
            info.State = LicenseState.Invalid;
            return info;
        }

        var parts = payload.Split('|');
        if (parts.Length != 4)
        {
            info.State = LicenseState.Invalid;
            return info;
        }

        var machine   = parts[0];
        var customer  = parts[1];
        var expiry    = parts[2];
        var signature = parts[3];

        if (!FixedTimeEquals(signature, Sign(machine, customer, expiry)))
        {
            info.State = LicenseState.Invalid;
            return info;
        }

        info.CustomerName = customer;

        if (!machine.Equals(info.MachineId, StringComparison.OrdinalIgnoreCase))
        {
            info.State = LicenseState.WrongMachine;
            return info;
        }

        if (expiry != "NEVER")
        {
            if (!DateTime.TryParseExact(expiry, "yyyy-MM-dd", null,
                    System.Globalization.DateTimeStyles.None, out var expiresOn))
            {
                info.State = LicenseState.Invalid;
                return info;
            }

            info.ExpiresOn = expiresOn;
            if (DateTime.Today > expiresOn)
            {
                info.State = LicenseState.Expired;
                return info;
            }
        }

        info.State = LicenseState.Valid;
        return info;
    }

    public static string Generate(string machineId, string customerName, DateTime? expiresOn)
    {
        var expiry = expiresOn?.ToString("yyyy-MM-dd") ?? "NEVER";
        var clean  = customerName.Replace("|", " ").Trim();
        var payload = $"{machineId}|{clean}|{expiry}|{Sign(machineId, clean, expiry)}";
        return Chunk(Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)));
    }

    public static bool Install(string key)
    {
        try
        {
            File.WriteAllText(FilePath, key.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string Sign(string machineId, string customerName, string expiry)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{machineId}|{customerName}|{expiry}"));
        return Convert.ToHexString(hash)[..32];
    }

    private static string? Decode(string key)
    {
        try
        {
            var compact = new string(key.Where(ch => !char.IsWhiteSpace(ch) && ch != '-').ToArray());
            return Encoding.UTF8.GetString(Convert.FromBase64String(compact));
        }
        catch
        {
            return null;
        }
    }

    private static string Chunk(string value)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            if (i > 0 && i % 25 == 0) sb.Append(Environment.NewLine);
            sb.Append(value[i]);
        }

        return sb.ToString();
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }
}
