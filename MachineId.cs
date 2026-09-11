using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace TailorShop;

public static class MachineId
{
    private static string? _cached;

    public static string Current
    {
        get
        {
            _cached ??= Compute();
            return _cached;
        }
    }

    private static string Compute()
    {
        var parts = new List<string>
        {
            Query("Win32_BaseBoard", "SerialNumber"),
            Query("Win32_Processor", "ProcessorId"),
            Query("Win32_BIOS", "SerialNumber"),
            Environment.MachineName
        };

        var raw = string.Join("|", parts.Where(p => p.Length > 0));
        if (raw.Length == 0) raw = Environment.MachineName + "|" + Environment.UserName;

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Format(Convert.ToHexString(hash)[..20]);
    }

    private static string Query(string wmiClass, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
            foreach (var obj in searcher.Get())
            {
                var value = obj[property]?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(value) &&
                    !value.Equals("To be filled by O.E.M.", StringComparison.OrdinalIgnoreCase) &&
                    !value.Equals("Default string", StringComparison.OrdinalIgnoreCase))
                    return value;
            }
        }
        catch
        {
        }

        return "";
    }

    private static string Format(string value)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            if (i > 0 && i % 5 == 0) sb.Append('-');
            sb.Append(value[i]);
        }

        return sb.ToString();
    }
}
