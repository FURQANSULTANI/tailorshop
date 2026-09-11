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
    private static readonly string DbPath = AppPaths.DbFile;

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

        var ordersCmd = conn.CreateCommand();
        ordersCmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Orders (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                CustomerId  INTEGER NOT NULL,
                Status      TEXT NOT NULL DEFAULT 'Pending',
                CreatedAt   TEXT DEFAULT (datetime('now','localtime')),
                UpdatedAt   TEXT DEFAULT (datetime('now','localtime')),
                ReadyAt     TEXT,
                DeliveredAt TEXT,
                FOREIGN KEY(CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS WhatsAppQueue (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderId      INTEGER NOT NULL,
                CustomerId   INTEGER NOT NULL,
                Phone        TEXT NOT NULL,
                Message      TEXT NOT NULL,
                Status       TEXT NOT NULL DEFAULT 'Pending',
                AttemptCount INTEGER NOT NULL DEFAULT 0,
                LastError    TEXT,
                CreatedAt    TEXT DEFAULT (datetime('now','localtime')),
                UpdatedAt    TEXT DEFAULT (datetime('now','localtime')),
                SentAt       TEXT,
                FOREIGN KEY(OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
            );";
        ordersCmd.ExecuteNonQuery();

        try
        {
            var alter = conn.CreateCommand();
            alter.CommandText = "ALTER TABLE Measurements ADD COLUMN OrderId INTEGER";
            alter.ExecuteNonQuery();
        }
        catch (SqliteException) { /* column already exists */ }

        try
        {
            var alter = conn.CreateCommand();
            alter.CommandText = "ALTER TABLE Orders ADD COLUMN DeliveryDate TEXT";
            alter.ExecuteNonQuery();
        }
        catch (SqliteException) { /* column already exists */ }

        var backfillOrders = conn.CreateCommand();
        backfillOrders.CommandText = @"
            INSERT INTO Orders(CustomerId, Status)
            SELECT c.Id, 'Delivered' FROM Customers c
            WHERE NOT EXISTS (SELECT 1 FROM Orders o WHERE o.CustomerId = c.Id)";
        backfillOrders.ExecuteNonQuery();

        var backfillMeasurements = conn.CreateCommand();
        backfillMeasurements.CommandText = @"
            UPDATE Measurements
            SET OrderId = (SELECT o.Id FROM Orders o WHERE o.CustomerId = Measurements.CustomerId ORDER BY o.Id ASC LIMIT 1)
            WHERE OrderId IS NULL";
        backfillMeasurements.ExecuteNonQuery();

        var renameFields = conn.CreateCommand();
        renameFields.CommandText = @"
            UPDATE Measurements SET FieldName = 'Paid Amount'      WHERE FieldName IN ('کسٹمر پے', 'ادا شدہ رقم');
            UPDATE Measurements SET FieldName = 'Advance'          WHERE FieldName = 'ایڈوانس';
            UPDATE Measurements SET FieldName = 'Previous Balance' WHERE FieldName = 'سابقہ رقم';
            UPDATE Measurements SET FieldName = 'Suit Stitching'   WHERE FieldName = 'سوٹ سلائی'   AND Section = 'Point Of Sale';
            UPDATE Measurements SET FieldName = 'Suit Purchase'    WHERE FieldName = 'سوٹ خریداری' AND Section = 'Point Of Sale';";
        renameFields.ExecuteNonQuery();
    }

    private const string LatestOrderColumns = @"
        (SELECT o.Id FROM Orders o WHERE o.CustomerId = Customers.Id ORDER BY o.Id DESC LIMIT 1) AS LatestOrderId,
        (SELECT o.Status FROM Orders o WHERE o.CustomerId = Customers.Id ORDER BY o.Id DESC LIMIT 1) AS LatestOrderStatus,
        (SELECT o.CreatedAt FROM Orders o WHERE o.CustomerId = Customers.Id ORDER BY o.Id DESC LIMIT 1) AS LatestOrderCreatedAt,
        (SELECT o.DeliveryDate FROM Orders o WHERE o.CustomerId = Customers.Id ORDER BY o.Id DESC LIMIT 1) AS LatestOrderDeliveryDate";

    public static Dictionary<long, List<Measurement>> GetSectionMeasurementsByOrder(List<long> orderIds, string section)
    {
        var result = new Dictionary<long, List<Measurement>>();
        if (orderIds.Count == 0) return result;

        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT OrderId,Section,FieldName,Value,SelectedOptions,Quantity
            FROM Measurements WHERE Section=$sec AND OrderId IN ({string.Join(",", orderIds)})
            ORDER BY SortOrder";
        cmd.Parameters.AddWithValue("$sec", section);

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var oid = Convert.ToInt64(r["OrderId"]);
            if (!result.TryGetValue(oid, out var list)) result[oid] = list = new List<Measurement>();
            list.Add(new Measurement
            {
                Section         = r["Section"].ToString()!,
                FieldName       = r["FieldName"].ToString()!,
                Value           = r["Value"] == DBNull.Value ? null : r["Value"].ToString(),
                SelectedOptions = r["SelectedOptions"] == DBNull.Value ? null : r["SelectedOptions"].ToString(),
                Quantity        = r["Quantity"] == DBNull.Value ? null : r["Quantity"].ToString()
            });
        }
        return result;
    }

    public static List<Customer> GetAllCustomers(string search = "")
    {
        var list = new List<Customer>();
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        if (string.IsNullOrWhiteSpace(search))
            cmd.CommandText = $"SELECT Customers.*, {LatestOrderColumns} FROM Customers ORDER BY Name";
        else
        {
            cmd.CommandText = $@"SELECT Customers.*, {LatestOrderColumns} FROM Customers
                WHERE Name LIKE $s OR Phone LIKE $s ORDER BY Name";
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
        cmd.CommandText = $"SELECT Customers.*, {LatestOrderColumns} FROM Customers WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return MapCustomer(r);
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

    public static List<Measurement> GetMeasurements(long orderId, SqliteConnection? conn = null)
    {
        bool owns = conn == null;
        conn ??= new SqliteConnection(ConnectionString);
        if (owns) conn.Open();
        try
        {
            var list = new List<Measurement>();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Section,FieldName,Value,SelectedOptions,Quantity FROM Measurements
                WHERE OrderId=$id ORDER BY Section,SortOrder";
            cmd.Parameters.AddWithValue("$id", orderId);
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

    public static Order? GetLatestOrder(long customerId)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Orders WHERE CustomerId=$cid ORDER BY Id DESC LIMIT 1";
        cmd.Parameters.AddWithValue("$cid", customerId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        var o = MapOrder(r);
        r.Close();
        o.Measurements = GetMeasurements(o.Id, conn);
        return o;
    }

    public static List<Order> GetOrdersForCustomer(long customerId)
    {
        var ids = new List<long>();
        using (var conn = new SqliteConnection(ConnectionString))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id FROM Orders WHERE CustomerId=$cid ORDER BY Id DESC";
            cmd.Parameters.AddWithValue("$cid", customerId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) ids.Add(Convert.ToInt64(r["Id"]));
        }
        return ids.Select(GetOrderById).Where(o => o != null).Select(o => o!).ToList();
    }

    public static Order? GetOrderById(long orderId)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Orders WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", orderId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        var o = MapOrder(r);
        r.Close();
        o.Measurements = GetMeasurements(o.Id, conn);
        return o;
    }

    public static long SaveOrder(Order o)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var cmd = conn.CreateCommand();
            if (o.Id == 0)
            {
                cmd.CommandText = @"INSERT INTO Orders(CustomerId,Status,DeliveryDate)
                    VALUES($cid,$status,$delivery); SELECT last_insert_rowid();";
            }
            else
            {
                cmd.CommandText = @"UPDATE Orders SET Status=$status, DeliveryDate=$delivery,
                    UpdatedAt=datetime('now','localtime') WHERE Id=$id; SELECT $id;";
                cmd.Parameters.AddWithValue("$id", o.Id);
            }
            cmd.Parameters.AddWithValue("$cid", o.CustomerId);
            cmd.Parameters.AddWithValue("$status", o.Status);
            cmd.Parameters.AddWithValue("$delivery", (object?)o.DeliveryDate ?? DBNull.Value);
            o.Id = Convert.ToInt64(cmd.ExecuteScalar());

            var del = conn.CreateCommand();
            del.CommandText = "DELETE FROM Measurements WHERE OrderId=$id";
            del.Parameters.AddWithValue("$id", o.Id);
            del.ExecuteNonQuery();

            int sortOrder = 0;
            foreach (var m in o.Measurements)
            {
                var ins = conn.CreateCommand();
                ins.CommandText = @"INSERT INTO Measurements(CustomerId,OrderId,Section,FieldName,Value,SortOrder,SelectedOptions,Quantity)
                    VALUES($cid,$oid,$sec,$fn,$val,$ord,$sel,$qty)";
                ins.Parameters.AddWithValue("$cid", o.CustomerId);
                ins.Parameters.AddWithValue("$oid", o.Id);
                ins.Parameters.AddWithValue("$sec", m.Section);
                ins.Parameters.AddWithValue("$fn", m.FieldName);
                ins.Parameters.AddWithValue("$val", (object?)m.Value ?? DBNull.Value);
                ins.Parameters.AddWithValue("$ord", sortOrder++);
                ins.Parameters.AddWithValue("$sel", (object?)m.SelectedOptions ?? DBNull.Value);
                ins.Parameters.AddWithValue("$qty", (object?)m.Quantity ?? DBNull.Value);
                ins.ExecuteNonQuery();
            }
            tx.Commit();
            return o.Id;
        }
        catch { tx.Rollback(); throw; }
    }

    public static void UpdateOrderStatus(long orderId, string status)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = status switch
        {
            OrderStatus.Ready => @"UPDATE Orders SET Status=$status, ReadyAt=datetime('now','localtime'),
                UpdatedAt=datetime('now','localtime') WHERE Id=$id",
            OrderStatus.Delivered => @"UPDATE Orders SET Status=$status, DeliveredAt=datetime('now','localtime'),
                UpdatedAt=datetime('now','localtime') WHERE Id=$id",
            _ => "UPDATE Orders SET Status=$status, UpdatedAt=datetime('now','localtime') WHERE Id=$id"
        };
        cmd.Parameters.AddWithValue("$id", orderId);
        cmd.Parameters.AddWithValue("$status", status);
        cmd.ExecuteNonQuery();
    }

    public const int MaxRetryAttempts = 5;

    public static long EnqueueWhatsAppMessage(long orderId, long customerId, string phone, string message)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();

        var existing = conn.CreateCommand();
        existing.CommandText = @"SELECT Id FROM WhatsAppQueue
            WHERE Status=$status AND Phone=$phone AND Message=$msg ORDER BY Id ASC LIMIT 1";
        existing.Parameters.AddWithValue("$status", WhatsAppStatus.Pending);
        existing.Parameters.AddWithValue("$phone", phone);
        existing.Parameters.AddWithValue("$msg", message);
        var found = existing.ExecuteScalar();
        if (found != null && found != DBNull.Value) return Convert.ToInt64(found);

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO WhatsAppQueue(OrderId,CustomerId,Phone,Message)
            VALUES($oid,$cid,$phone,$msg); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$oid", orderId);
        cmd.Parameters.AddWithValue("$cid", customerId);
        cmd.Parameters.AddWithValue("$phone", phone);
        cmd.Parameters.AddWithValue("$msg", message);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    public static void MarkWhatsAppSent(long id)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE WhatsAppQueue SET Status=$status, AttemptCount=AttemptCount+1,
            SentAt=datetime('now','localtime'), UpdatedAt=datetime('now','localtime'), LastError=NULL
            WHERE Id=$id
               OR (Status=$pending AND Phone=(SELECT Phone FROM WhatsAppQueue WHERE Id=$id)
                                   AND Message=(SELECT Message FROM WhatsAppQueue WHERE Id=$id))";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$status", WhatsAppStatus.Sent);
        cmd.Parameters.AddWithValue("$pending", WhatsAppStatus.Pending);
        cmd.ExecuteNonQuery();
    }

    public static void MarkWhatsAppAttemptFailed(long id, string error)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE WhatsAppQueue SET
            AttemptCount = AttemptCount + 1,
            Status = CASE WHEN AttemptCount + 1 >= $max THEN $failed ELSE $pending END,
            LastError = $error,
            UpdatedAt = datetime('now','localtime')
            WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$max", MaxRetryAttempts);
        cmd.Parameters.AddWithValue("$failed", WhatsAppStatus.Failed);
        cmd.Parameters.AddWithValue("$pending", WhatsAppStatus.Pending);
        cmd.Parameters.AddWithValue("$error", error);
        cmd.ExecuteNonQuery();
    }

    public static List<WhatsAppQueueEntry> GetPendingWhatsAppMessages()
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT * FROM WhatsAppQueue
            WHERE Status=$status AND Id IN (
                SELECT MIN(Id) FROM WhatsAppQueue WHERE Status=$status GROUP BY Phone, Message)";
        cmd.Parameters.AddWithValue("$status", WhatsAppStatus.Pending);
        using var r = cmd.ExecuteReader();
        var list = new List<WhatsAppQueueEntry>();
        while (r.Read())
            list.Add(new WhatsAppQueueEntry
            {
                Id           = Convert.ToInt64(r["Id"]),
                OrderId      = Convert.ToInt64(r["OrderId"]),
                CustomerId   = Convert.ToInt64(r["CustomerId"]),
                Phone        = r["Phone"].ToString()!,
                Message      = r["Message"].ToString()!,
                Status       = r["Status"].ToString()!,
                AttemptCount = Convert.ToInt32(r["AttemptCount"]),
                LastError    = r["LastError"] == DBNull.Value ? null : r["LastError"].ToString(),
                CreatedAt    = r["CreatedAt"]?.ToString(),
                UpdatedAt    = r["UpdatedAt"]?.ToString(),
                SentAt       = r["SentAt"] == DBNull.Value ? null : r["SentAt"].ToString()
            });
        return list;
    }

    private static Order MapOrder(SqliteDataReader r) => new()
    {
        Id          = Convert.ToInt64(r["Id"]),
        CustomerId  = Convert.ToInt64(r["CustomerId"]),
        Status      = r["Status"].ToString()!,
        CreatedAt   = r["CreatedAt"]?.ToString(),
        UpdatedAt   = r["UpdatedAt"]?.ToString(),
        ReadyAt     = r["ReadyAt"] == DBNull.Value ? null : r["ReadyAt"].ToString(),
        DeliveredAt = r["DeliveredAt"] == DBNull.Value ? null : r["DeliveredAt"].ToString(),
        DeliveryDate = r["DeliveryDate"] == DBNull.Value ? null : r["DeliveryDate"].ToString(),
    };

    // Default fields shown when creating a new customer
    public static readonly Dictionary<string, List<FieldDef>> DefaultFields = new()
    {
        ["Shirt"]          = new() { "Chest", "Shoulder", "Length", "Arm", "Neck" },
        ["Shalwar Kameez"] = new()
        {
            "Kameez Length", "Arm", "Shoulder", "Neck", "Chest", "Waist", "Belly",
            "Width (Tayari)", "Chest (Tayari)", "Waist (Tayari)", "Arm Hole",
            "Shalwar / Trouser Length", "Bottom", "Width (Ghera)", "Crotch Seam (Aasan)", "Hip",

            "Collar", "Bain", "Front Pocket", "Cuff", "Front Patti",
            "Side Pocket", "Shalwar / Trouser Pocket",
        },
        ["Pant"]           = new() { "Length", "Waist", "Ghera", "Thigh (Raan)", "Knee (Ghutna)", "Bottom (Paincha)" },
        ["Coat / Sherwani"]= new() { "Length", "Chest", "Shoulder", "Arm" },
        ["Point Of Sale"]  = new()
        {
            new FieldDef { Name = "Suit Stitching", Kind = FieldKind.Text, ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true, HasQuantity = true },
            new FieldDef { Name = "Suit Purchase",  Kind = FieldKind.Text, ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true, HasQuantity = true },
            new FieldDef { Name = "Advance",          Kind = FieldKind.Text, ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true },
            new FieldDef { Name = "Previous Balance", Kind = FieldKind.Text, ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true },
        },
    };

    private static Customer MapCustomer(SqliteDataReader r) => new()
    {
        Id                = Convert.ToInt64(r["Id"]),
        Name              = r["Name"].ToString()!,
        Phone             = r["Phone"]    == DBNull.Value ? null : r["Phone"].ToString(),
        Address           = r["Address"]  == DBNull.Value ? null : r["Address"].ToString(),
        Notes             = r["Notes"]    == DBNull.Value ? null : r["Notes"].ToString(),
        CreatedAt         = r["CreatedAt"]?.ToString(),
        UpdatedAt         = r["UpdatedAt"]?.ToString(),
        LatestOrderId     = r["LatestOrderId"] == DBNull.Value ? null : Convert.ToInt64(r["LatestOrderId"]),
        LatestOrderStatus = r["LatestOrderStatus"] == DBNull.Value ? null : r["LatestOrderStatus"].ToString(),
        LatestOrderCreatedAt = r["LatestOrderCreatedAt"] == DBNull.Value ? null : r["LatestOrderCreatedAt"].ToString(),
        LatestOrderDeliveryDate = r["LatestOrderDeliveryDate"] == DBNull.Value ? null : r["LatestOrderDeliveryDate"].ToString(),
    };
}
