using Microsoft.Data.Sqlite;

namespace TailorShop;

public enum FieldKind { Text, Checkbox, Both }

public class FieldDef
{
    public string     Name             { get; init; } = "";
    public FieldKind  Kind             { get; init; } = FieldKind.Text;
    public string[]   Options          { get; init; } = Array.Empty<string>();
    public string     ValuePlaceholder { get; init; } = "Inches...";
    public string     UnitLabel        { get; init; } = "in";
    public bool       Numeric          { get; init; } = false;
    public bool       HasQuantity      { get; init; } = false;

    public static implicit operator FieldDef(string name) => new() { Name = name };
}

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

        // Databases created before checkbox-option fields existed won't have this column yet.
        try
        {
            var alter = conn.CreateCommand();
            alter.CommandText = "ALTER TABLE Measurements ADD COLUMN SelectedOptions TEXT";
            alter.ExecuteNonQuery();
        }
        catch (SqliteException) { /* column already exists */ }

        // Databases created before Point Of Sale quantity fields existed won't have this column yet.
        try
        {
            var alter = conn.CreateCommand();
            alter.CommandText = "ALTER TABLE Measurements ADD COLUMN Quantity TEXT";
            alter.ExecuteNonQuery();
        }
        catch (SqliteException) { /* column already exists */ }
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
                ins.CommandText = @"INSERT INTO Measurements(CustomerId,Section,FieldName,Value,SortOrder,SelectedOptions,Quantity)
                    VALUES($cid,$sec,$fn,$val,$ord,$sel,$qty)";
                ins.Parameters.AddWithValue("$cid", c.Id);
                ins.Parameters.AddWithValue("$sec", m.Section);
                ins.Parameters.AddWithValue("$fn", m.FieldName);
                ins.Parameters.AddWithValue("$val", (object?)m.Value ?? DBNull.Value);
                ins.Parameters.AddWithValue("$ord", order++);
                ins.Parameters.AddWithValue("$sel", (object?)m.SelectedOptions ?? DBNull.Value);
                ins.Parameters.AddWithValue("$qty", (object?)m.Quantity ?? DBNull.Value);
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
            cmd.CommandText = @"SELECT Section,FieldName,Value,SelectedOptions,Quantity FROM Measurements
                WHERE CustomerId=$id ORDER BY Section,SortOrder";
            cmd.Parameters.AddWithValue("$id", customerId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Measurement
                {
                    Section         = r["Section"].ToString()!,
                    FieldName       = r["FieldName"].ToString()!,
                    Value           = r["Value"] == DBNull.Value ? null : r["Value"].ToString(),
                    SelectedOptions = r["SelectedOptions"] == DBNull.Value ? null : r["SelectedOptions"].ToString(),
                    Quantity        = r["Quantity"] == DBNull.Value ? null : r["Quantity"].ToString()
                });
            return list;
        }
        finally { if (owns) conn.Dispose(); }
    }

    // Default fields shown when creating a new customer
    public static readonly Dictionary<string, List<FieldDef>> DefaultFields = new()
    {
        ["Shirt"]          = new() { "چھاتی", "کندھا", "لمبائی", "بازو", "گلا" },
        ["Shalwar Kameez"] = new()
        {
            // Plain length fields (Input Field column was empty = shown; no checkboxes)
            "قمیض لمبائی", "بازو", "تیرہ", "چھاتی", "نوک", "کمر", "گھیرہ", "گلا", "کف", "بازو موڈا", "پٹی",

            // Reference/design number fields
            new FieldDef { Name = "سوٹ ڈیزائن", Kind = FieldKind.Text, ValuePlaceholder = "Ref..." },
            new FieldDef { Name = "کڑھائی",     Kind = FieldKind.Text, ValuePlaceholder = "Ref..." },

            // Length + style options together (Input Field + CheckBoxes both shown)
            new FieldDef { Name = "کالر",        Kind = FieldKind.Both, Options = new[] { "سادہ", "گول نوک" } },
            new FieldDef { Name = "بین",         Kind = FieldKind.Both, Options = new[] { "سادہ", "گول نوک" } },
            new FieldDef { Name = "فرنٹ پاکٹ",   Kind = FieldKind.Both, Options = new[] { "سادہ", "ڈیزائن" } },
            new FieldDef { Name = "سائیڈ پاکٹ",  Kind = FieldKind.Both, Options = new[] { "ڈبل", "سنگل" } },
            new FieldDef { Name = "سلائی",       Kind = FieldKind.Both, Options = new[] { "سنگل", "ڈبل", "ٹربل" } },
            new FieldDef { Name = "سوٹ سلائی",   Kind = FieldKind.Both, Options = new[] { "سادہ", "ڈیزائن" } },
            new FieldDef { Name = "کف ڈیزائن",   Kind = FieldKind.Both, Options = new[] { "سنگل", "ڈبل", "گول", "چورس کٹ" } },
            new FieldDef { Name = "بازو پلیٹ",   Kind = FieldKind.Both, Options = new[] { "سنگل", "ڈبل", "بغیر پلیٹ" } },
            new FieldDef { Name = "تیرہ ڈیزائن", Kind = FieldKind.Both, Options = new[] { "نوک" } },
            new FieldDef { Name = "بٹن",         Kind = FieldKind.Both, Options = new[] { "سادہ", "فینسی", "میٹل" } },
            new FieldDef { Name = "شلوار ڈیزائن",  Kind = FieldKind.Both, Options = new[] { "سادہ", "گڈی کاٹ", "پاجامہ" } },
            new FieldDef { Name = "پائنچہ ڈیزائن", Kind = FieldKind.Both, Options = new[] { "جالی", "ہاتھ کانٹا", "کمپیوٹر کانٹا" } },
            new FieldDef { Name = "شلوار پاکٹ",    Kind = FieldKind.Both, Options = new[] { "سائیڈ", "زپ" } },

            // بازو شپ, بازو جوک, شلوار لمبائی, پائنچہ, شلوار گھیرہ, آسن, بپ — every column NA in the table, so skipped
        },
        ["Pant"]           = new() { "لمبائی", "کمر", "گھیرا", "ران", "گھٹنا", "پائنچہ" },
        ["Coat / Sherwani"]= new() { "لمبائی", "چھاتی", "کندھا", "بازو" },
        ["Point Of Sale"]  = new()
        {
            new FieldDef { Name = "سوٹ سلائی",   Kind = FieldKind.Text, ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true, HasQuantity = true },
            new FieldDef { Name = "سوٹ خریداری", Kind = FieldKind.Text, ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true, HasQuantity = true },
            new FieldDef { Name = "ایڈوانس",     Kind = FieldKind.Text, ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true },
            new FieldDef { Name = "سابقہ رقم",   Kind = FieldKind.Text, ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true },
        },
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
