using Microsoft.Data.Sqlite;

namespace TailorShop;

public class Database
{
    private static readonly string DbPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "golden_tailor.db");

    private static string ConnectionString => $"Data Source={DbPath}";

    public static void Initialize()
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Customers (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Name      TEXT NOT NULL,
                Phone     TEXT,
                Address   TEXT,
                Notes     TEXT,
                CreatedAt TEXT DEFAULT (datetime('now','localtime')),
                UpdatedAt TEXT DEFAULT (datetime('now','localtime'))
            );
            CREATE TABLE IF NOT EXISTS Measurements (
                Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                CustomerId INTEGER NOT NULL,
                Section    TEXT NOT NULL,
                FieldName  TEXT NOT NULL,
                Value      TEXT,
                SortOrder  INTEGER DEFAULT 0,
                FOREIGN KEY(CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE
            );";
        cmd.ExecuteNonQuery();
    }

    public static List<Customer> GetAllCustomers(string search = "")
    {
        var list = new List<Customer>();
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        if (string.IsNullOrWhiteSpace(search))
            cmd.CommandText = "SELECT * FROM Customers ORDER BY Name";
        else
        {
            cmd.CommandText = "SELECT * FROM Customers WHERE Name LIKE $s OR Phone LIKE $s ORDER BY Name";
            cmd.Parameters.AddWithValue("$s", $"%{search}%");
        }
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(MapCustomer(r));
        return list;
    }

    public static Customer? GetById(long id)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Customers WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        var c = MapCustomer(r);
        r.Close();
        c.Measurements = GetMeasurements(id, conn);
        return c;
    }

    public static long SaveCustomer(Customer c)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var cmd = conn.CreateCommand();
            if (c.Id == 0)
            {
                cmd.CommandText = @"INSERT INTO Customers(Name,Phone,Address,Notes)
                    VALUES($n,$p,$a,$no); SELECT last_insert_rowid();";
            }
            else
            {
                cmd.CommandText = @"UPDATE Customers SET Name=$n,Phone=$p,Address=$a,Notes=$no,
                    UpdatedAt=datetime('now','localtime') WHERE Id=$id; SELECT $id;";
                cmd.Parameters.AddWithValue("$id", c.Id);
            }
            cmd.Parameters.AddWithValue("$n", c.Name);
            cmd.Parameters.AddWithValue("$p", (object?)c.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$a", (object?)c.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$no", (object?)c.Notes ?? DBNull.Value);
            c.Id = Convert.ToInt64(cmd.ExecuteScalar());

            var del = conn.CreateCommand();
            del.CommandText = "DELETE FROM Measurements WHERE CustomerId=$id";
            del.Parameters.AddWithValue("$id", c.Id);
            del.ExecuteNonQuery();

            int order = 0;
            foreach (var m in c.Measurements)
            {
                var ins = conn.CreateCommand();
                ins.CommandText = @"INSERT INTO Measurements(CustomerId,Section,FieldName,Value,SortOrder)
                    VALUES($cid,$sec,$fn,$val,$ord)";
                ins.Parameters.AddWithValue("$cid", c.Id);
                ins.Parameters.AddWithValue("$sec", m.Section);
                ins.Parameters.AddWithValue("$fn", m.FieldName);
                ins.Parameters.AddWithValue("$val", (object?)m.Value ?? DBNull.Value);
                ins.Parameters.AddWithValue("$ord", order++);
                ins.ExecuteNonQuery();
            }
            tx.Commit();
            return c.Id;
        }
        catch { tx.Rollback(); throw; }
    }

    public static void DeleteCustomer(long id)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Customers WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public static List<Measurement> GetMeasurements(long customerId, SqliteConnection? conn = null)
    {
        bool owns = conn == null;
        conn ??= new SqliteConnection(ConnectionString);
        if (owns) conn.Open();
        try
        {
            var list = new List<Measurement>();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Section,FieldName,Value FROM Measurements
                WHERE CustomerId=$id ORDER BY Section,SortOrder";
            cmd.Parameters.AddWithValue("$id", customerId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Measurement
                {
                    Section = r["Section"].ToString()!,
                    FieldName = r["FieldName"].ToString()!,
                    Value = r["Value"] == DBNull.Value ? null : r["Value"].ToString()
                });
            return list;
        }
        finally { if (owns) conn.Dispose(); }
    }

    // Default fields shown when creating a new customer
    public static readonly Dictionary<string, List<string>> DefaultFields = new()
    {
        ["Shirt"]          = new() { "Chest (Seena)", "Shoulder (Kandha)", "Length (Lambai)", "Sleeve (Bazu)", "Neck (Gala)" },
        ["Shalwar Kameez"] = new() { "Kameez Length", "Chest (Seena)", "Waist (Kamar)", "Hip (Ghera)", "Shalwar Length", "Paincha" },
        ["Pant"]           = new() { "Length (Lambai)", "Waist (Kamar)", "Hip (Ghera)", "Thigh (Ran)", "Knee (Ghutna)", "Bottom (Paincha)" },
        ["Coat / Sherwani"]= new() { "Length", "Chest (Seena)", "Shoulder (Kandha)", "Sleeve (Bazu)" },
    };

    private static Customer MapCustomer(SqliteDataReader r) => new()
    {
        Id        = Convert.ToInt64(r["Id"]),
        Name      = r["Name"].ToString()!,
        Phone     = r["Phone"]    == DBNull.Value ? null : r["Phone"].ToString(),
        Address   = r["Address"]  == DBNull.Value ? null : r["Address"].ToString(),
        Notes     = r["Notes"]    == DBNull.Value ? null : r["Notes"].ToString(),
        CreatedAt = r["CreatedAt"]?.ToString(),
        UpdatedAt = r["UpdatedAt"]?.ToString(),
    };
}
