using System.Security.Cryptography;
using System.Text;

namespace TailorShop.LicenseGenerator;

static class Program
{
    private const string Secret = "TKB-TailorShop-2026-#Q7pR2vLx9mZs4KdE1wYn6HbT";

    static void Main()
    {
        Console.WriteLine("TailorShop License Generator");
        Console.WriteLine("============================");
        Console.WriteLine();

        Console.Write("Machine ID    : ");
        var machineId = (Console.ReadLine() ?? "").Trim();
        if (machineId.Length == 0)
        {
            Console.WriteLine("Machine ID is required.");
            return;
        }

        Console.Write("Customer Name : ");
        var customer = (Console.ReadLine() ?? "").Replace("|", " ").Trim();
        if (customer.Length == 0) customer = "Customer";

        Console.Write("Valid days (blank = lifetime) : ");
        var daysInput = (Console.ReadLine() ?? "").Trim();

        string expiry = "NEVER";
        if (daysInput.Length > 0 && int.TryParse(daysInput, out var days) && days > 0)
            expiry = DateTime.Today.AddDays(days).ToString("yyyy-MM-dd");

        var payload = $"{machineId}|{customer}|{expiry}|{Sign(machineId, customer, expiry)}";
        var key     = Chunk(Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)));

        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine($"Machine  : {machineId}");
        Console.WriteLine($"Customer : {customer}");
        Console.WriteLine($"Expires  : {expiry}");
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine("LICENSE KEY:");
        Console.WriteLine();
        Console.WriteLine(key);
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");

        var file = $"license-{machineId.Replace("-", "")}.key";
        File.WriteAllText(file, key);
        Console.WriteLine($"Saved to: {Path.GetFullPath(file)}");
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static string Sign(string machineId, string customerName, string expiry)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{machineId}|{customerName}|{expiry}"));
        return Convert.ToHexString(hash)[..32];
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
}
