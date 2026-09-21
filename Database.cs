using Microsoft.Data.Sqlite;

namespace TailorShop;

public enum FieldKind { Text, Checkbox, Both }

public class FieldDef
{
    public string     Name             { get; init; } = "";
    public FieldKind  Kind             { get; init; } = FieldKind.Text;
    public string[]   Options          { get; init; } = Array.Empty<string>();
    public string[]   ValueOptions     { get; init; } = Array.Empty<string>();
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

        var stockCmd = conn.CreateCommand();
        stockCmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS StockItems (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                SuitType      TEXT NOT NULL,
                Color         TEXT,
                Quantity      INTEGER NOT NULL DEFAULT 0,
                PurchasePrice REAL NOT NULL DEFAULT 0,
                RetailPrice   REAL NOT NULL DEFAULT 0,
                IsDeleted     INTEGER NOT NULL DEFAULT 0,
                CreatedAt     TEXT DEFAULT (datetime('now','localtime')),
                UpdatedAt     TEXT DEFAULT (datetime('now','localtime'))
            );

            CREATE TABLE IF NOT EXISTS StockSales (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                StockItemId  INTEGER,
                OrderId      INTEGER NOT NULL,
                CustomerId   INTEGER NOT NULL,
                CustomerName TEXT NOT NULL DEFAULT '',
                SuitType     TEXT NOT NULL DEFAULT '',
                Color        TEXT,
                Qty          INTEGER NOT NULL DEFAULT 0,
                UnitPrice    REAL NOT NULL DEFAULT 0,
                SoldAt       TEXT DEFAULT (datetime('now','localtime')),
                ItemAddedAt   TEXT,
                ItemUpdatedAt TEXT
            );

            CREATE INDEX IF NOT EXISTS IX_StockSales_OrderId ON StockSales(OrderId);";
        stockCmd.ExecuteNonQuery();

        foreach (var stockAlter in new[]
                 {
                     "ALTER TABLE StockSales ADD COLUMN ItemAddedAt TEXT",
                     "ALTER TABLE StockSales ADD COLUMN ItemUpdatedAt TEXT"
                 })
        {
            try
            {
                var a = conn.CreateCommand();
                a.CommandText = stockAlter;
                a.ExecuteNonQuery();
            }
            catch (SqliteException) { /* column already exists */ }
        }

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
            alter.CommandText = "ALTER TABLE Customers ADD COLUMN SerialNumber TEXT";
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

        var renameCustomerPay = conn.CreateCommand();
        renameCustomerPay.CommandText =
            "UPDATE Measurements SET FieldName = 'ادا شدہ رقم' WHERE FieldName = 'کسٹمر پے'";
        renameCustomerPay.ExecuteNonQuery();
    }

    private const string LatestOrderColumns = @"
        (SELECT o.Id FROM Orders o WHERE o.CustomerId = Customers.Id ORDER BY o.Id DESC LIMIT 1) AS LatestOrderId,
        (SELECT o.Status FROM Orders o WHERE o.CustomerId = Customers.Id ORDER BY o.Id DESC LIMIT 1) AS LatestOrderStatus,
        (SELECT o.CreatedAt FROM Orders o WHERE o.CustomerId = Customers.Id ORDER BY o.Id DESC LIMIT 1) AS LatestOrderCreatedAt";

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
                WHERE Name LIKE $s OR Phone LIKE $s OR SerialNumber LIKE $s ORDER BY Name";
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
                cmd.CommandText = @"INSERT INTO Customers(Name,Phone,Address,Notes,SerialNumber)
                    VALUES($n,$p,$a,$no,$sn); SELECT last_insert_rowid();";
            }
            else
            {
                cmd.CommandText = @"UPDATE Customers SET Name=$n,Phone=$p,Address=$a,Notes=$no,
                    SerialNumber=$sn, UpdatedAt=datetime('now','localtime') WHERE Id=$id; SELECT $id;";
                cmd.Parameters.AddWithValue("$id", c.Id);
            }
            cmd.Parameters.AddWithValue("$n", c.Name);
            cmd.Parameters.AddWithValue("$p", (object?)c.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$a", (object?)c.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$no", (object?)c.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$sn", (object?)c.SerialNumber ?? DBNull.Value);
            c.Id = Convert.ToInt64(cmd.ExecuteScalar());

            tx.Commit();
            return c.Id;
        }
        catch { tx.Rollback(); throw; }
    }

    public static bool SerialNumberExists(string serial, long excludeCustomerId)
    {
        if (string.IsNullOrWhiteSpace(serial)) return false;

        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT COUNT(*) FROM Customers
            WHERE SerialNumber IS NOT NULL
              AND TRIM(LOWER(SerialNumber)) = TRIM(LOWER($sn))
              AND Id <> $id";
        cmd.Parameters.AddWithValue("$sn", serial);
        cmd.Parameters.AddWithValue("$id", excludeCustomerId);
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
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

    public static long SaveOrder(Order o) => SaveOrder(o, null, "", false);

    public static long SaveOrder(Order o, StockSelection? stock, string customerName)
        => SaveOrder(o, stock, customerName, true);

    private static long SaveOrder(Order o, StockSelection? stock, string customerName, bool adjustStock)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            if (o.Id != 0)
            {
                var priorCmd = conn.CreateCommand();
                priorCmd.CommandText = "SELECT Status FROM Orders WHERE Id=$id";
                priorCmd.Parameters.AddWithValue("$id", o.Id);
                var priorStatus = priorCmd.ExecuteScalar()?.ToString();

                var priorQtyCmd = conn.CreateCommand();
                priorQtyCmd.CommandText = "SELECT Quantity FROM Measurements WHERE OrderId=$id AND FieldName=$fn AND Section=$sec";
                priorQtyCmd.Parameters.AddWithValue("$id", o.Id);
                priorQtyCmd.Parameters.AddWithValue("$fn", CustomerForm.SuitStitchingFieldName);
                priorQtyCmd.Parameters.AddWithValue("$sec", "Point Of Sale");
                int.TryParse(priorQtyCmd.ExecuteScalar()?.ToString(), out var priorQty);

                var newQtyValue = o.Measurements
                    .FirstOrDefault(m => m.FieldName == CustomerForm.SuitStitchingFieldName && m.Section == "Point Of Sale")
                    ?.Quantity;
                int.TryParse(newQtyValue, out var newQty);

                if (priorStatus == OrderStatus.Ready && newQty > priorQty)
                {
                    o.Status = OrderStatus.Pending;
                    var clearReady = conn.CreateCommand();
                    clearReady.CommandText = "UPDATE Orders SET ReadyAt=NULL WHERE Id=$id";
                    clearReady.Parameters.AddWithValue("$id", o.Id);
                    clearReady.ExecuteNonQuery();
                }
            }

            var cmd = conn.CreateCommand();
            if (o.Id == 0)
            {
                cmd.CommandText = @"INSERT INTO Orders(CustomerId,Status)
                    VALUES($cid,$status); SELECT last_insert_rowid();";
            }
            else
            {
                cmd.CommandText = @"UPDATE Orders SET Status=$status,
                    UpdatedAt=datetime('now','localtime') WHERE Id=$id; SELECT $id;";
                cmd.Parameters.AddWithValue("$id", o.Id);
            }
            cmd.Parameters.AddWithValue("$cid", o.CustomerId);
            cmd.Parameters.AddWithValue("$status", o.Status);
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
            if (adjustStock) ApplyStockChange(conn, o, stock, customerName);

            tx.Commit();
            return o.Id;
        }
        catch { tx.Rollback(); throw; }
    }

    private static void ApplyStockChange(SqliteConnection conn, Order o, StockSelection? stock, string customerName)
    {
        var prior = conn.CreateCommand();
        prior.CommandText = "SELECT StockItemId, Qty FROM StockSales WHERE OrderId=$oid";
        prior.Parameters.AddWithValue("$oid", o.Id);

        var restore = new List<(long ItemId, int Qty)>();
        using (var r = prior.ExecuteReader())
            while (r.Read())
                if (r["StockItemId"] != DBNull.Value)
                    restore.Add((Convert.ToInt64(r["StockItemId"]), Convert.ToInt32(r["Qty"])));

        foreach (var (itemId, qty) in restore)
        {
            var back = conn.CreateCommand();
            back.CommandText = "UPDATE StockItems SET Quantity = Quantity + $q, UpdatedAt=datetime('now','localtime') WHERE Id=$id";
            back.Parameters.AddWithValue("$q", qty);
            back.Parameters.AddWithValue("$id", itemId);
            back.ExecuteNonQuery();
        }

        var clear = conn.CreateCommand();
        clear.CommandText = "DELETE FROM StockSales WHERE OrderId=$oid";
        clear.Parameters.AddWithValue("$oid", o.Id);
        clear.ExecuteNonQuery();

        if (stock == null || stock.StockItemId <= 0 || stock.Qty <= 0) return;

        var item = conn.CreateCommand();
        item.CommandText = "SELECT SuitType, Color, CreatedAt, UpdatedAt FROM StockItems WHERE Id=$id";
        item.Parameters.AddWithValue("$id", stock.StockItemId);

        string suitType = "", color = "";
        object addedAt = DBNull.Value, updatedAt = DBNull.Value;
        using (var r = item.ExecuteReader())
        {
            if (!r.Read()) return;
            suitType  = r["SuitType"].ToString()!;
            color     = r["Color"] == DBNull.Value ? "" : r["Color"].ToString()!;
            addedAt   = r["CreatedAt"] == DBNull.Value ? DBNull.Value : r["CreatedAt"];
            updatedAt = r["UpdatedAt"] == DBNull.Value ? DBNull.Value : r["UpdatedAt"];
        }

        var deduct = conn.CreateCommand();
        deduct.CommandText = "UPDATE StockItems SET Quantity = Quantity - $q, UpdatedAt=datetime('now','localtime') WHERE Id=$id";
        deduct.Parameters.AddWithValue("$q", stock.Qty);
        deduct.Parameters.AddWithValue("$id", stock.StockItemId);
        deduct.ExecuteNonQuery();

        var log = conn.CreateCommand();
        log.CommandText = @"INSERT INTO StockSales(StockItemId,OrderId,CustomerId,CustomerName,SuitType,Color,Qty,UnitPrice,ItemAddedAt,ItemUpdatedAt)
            VALUES($sid,$oid,$cid,$cname,$type,$color,$qty,$price,$added,$updated)";
        log.Parameters.AddWithValue("$sid", stock.StockItemId);
        log.Parameters.AddWithValue("$oid", o.Id);
        log.Parameters.AddWithValue("$cid", o.CustomerId);
        log.Parameters.AddWithValue("$cname", customerName);
        log.Parameters.AddWithValue("$type", suitType);
        log.Parameters.AddWithValue("$color", color.Length == 0 ? DBNull.Value : color);
        log.Parameters.AddWithValue("$qty", stock.Qty);
        log.Parameters.AddWithValue("$price", stock.UnitPrice);
        log.Parameters.AddWithValue("$added", addedAt);
        log.Parameters.AddWithValue("$updated", updatedAt);
        log.ExecuteNonQuery();
    }

    public static List<StockItem> GetStockItems(string search = "", bool includeEmpty = true)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT * FROM StockItems WHERE IsDeleted = 0"
            + (string.IsNullOrWhiteSpace(search) ? "" : " AND (SuitType LIKE $q OR Color LIKE $q)")
            + (includeEmpty ? "" : " AND Quantity > 0")
            + " ORDER BY SuitType, Color";
        if (!string.IsNullOrWhiteSpace(search))
            cmd.Parameters.AddWithValue("$q", $"%{search}%");

        var list = new List<StockItem>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(MapStockItem(r));
        return list;
    }

    public static StockItem? GetStockItem(long id)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM StockItems WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? MapStockItem(r) : null;
    }

    public static long SaveStockItem(StockItem item)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        if (item.Id == 0)
        {
            cmd.CommandText = @"INSERT INTO StockItems(SuitType,Color,Quantity,PurchasePrice,RetailPrice)
                VALUES($type,$color,$qty,$pp,$rp); SELECT last_insert_rowid();";
        }
        else
        {
            cmd.CommandText = @"UPDATE StockItems SET SuitType=$type, Color=$color, Quantity=$qty,
                PurchasePrice=$pp, RetailPrice=$rp, UpdatedAt=datetime('now','localtime')
                WHERE Id=$id; SELECT $id;";
            cmd.Parameters.AddWithValue("$id", item.Id);
        }
        cmd.Parameters.AddWithValue("$type", item.SuitType);
        cmd.Parameters.AddWithValue("$color", (object?)item.Color ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$qty", item.Quantity);
        cmd.Parameters.AddWithValue("$pp", item.PurchasePrice);
        cmd.Parameters.AddWithValue("$rp", item.RetailPrice);
        item.Id = Convert.ToInt64(cmd.ExecuteScalar());
        return item.Id;
    }

    public static void DeleteStockItem(long id)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE StockItems SET IsDeleted=1, UpdatedAt=datetime('now','localtime') WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public static List<StockSale> GetStockSales(long? stockItemId = null, DateTime? from = null, DateTime? to = null)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        var cmd = conn.CreateCommand();

        var where = new List<string>();
        if (stockItemId != null) where.Add("StockItemId=$sid");
        if (from != null) where.Add("datetime(SoldAt) >= datetime($from)");
        if (to != null) where.Add("datetime(SoldAt) <= datetime($to)");

        cmd.CommandText = "SELECT * FROM StockSales"
            + (where.Count == 0 ? "" : " WHERE " + string.Join(" AND ", where))
            + " ORDER BY datetime(SoldAt) DESC, Id DESC";

        if (stockItemId != null) cmd.Parameters.AddWithValue("$sid", stockItemId.Value);
        if (from != null) cmd.Parameters.AddWithValue("$from", from.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        if (to != null) cmd.Parameters.AddWithValue("$to", to.Value.ToString("yyyy-MM-dd HH:mm:ss"));

        var list = new List<StockSale>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(MapStockSale(r));
        return list;
    }

    private static StockItem MapStockItem(SqliteDataReader r) => new()
    {
        Id            = Convert.ToInt64(r["Id"]),
        SuitType      = r["SuitType"].ToString()!,
        Color         = r["Color"] == DBNull.Value ? null : r["Color"].ToString(),
        Quantity      = Convert.ToInt32(r["Quantity"]),
        PurchasePrice = Convert.ToDecimal(r["PurchasePrice"]),
        RetailPrice   = Convert.ToDecimal(r["RetailPrice"]),
        CreatedAt     = r["CreatedAt"] == DBNull.Value ? null : r["CreatedAt"].ToString(),
        UpdatedAt     = r["UpdatedAt"] == DBNull.Value ? null : r["UpdatedAt"].ToString(),
    };

    private static StockSale MapStockSale(SqliteDataReader r) => new()
    {
        Id           = Convert.ToInt64(r["Id"]),
        StockItemId  = r["StockItemId"] == DBNull.Value ? null : Convert.ToInt64(r["StockItemId"]),
        OrderId      = Convert.ToInt64(r["OrderId"]),
        CustomerId   = Convert.ToInt64(r["CustomerId"]),
        CustomerName = r["CustomerName"].ToString()!,
        SuitType     = r["SuitType"].ToString()!,
        Color        = r["Color"] == DBNull.Value ? null : r["Color"].ToString(),
        Qty          = Convert.ToInt32(r["Qty"]),
        UnitPrice    = Convert.ToDecimal(r["UnitPrice"]),
        SoldAt        = r["SoldAt"] == DBNull.Value ? null : r["SoldAt"].ToString(),
        ItemAddedAt   = r["ItemAddedAt"] == DBNull.Value ? null : r["ItemAddedAt"].ToString(),
        ItemUpdatedAt = r["ItemUpdatedAt"] == DBNull.Value ? null : r["ItemUpdatedAt"].ToString(),
    };

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
    };

    // Default fields shown when creating a new customer
    public static readonly Dictionary<string, List<FieldDef>> DefaultFields = new()
    {
        ["Shirt"]          = new() { "چھاتی", "کندھا", "لمبائی", "بازو", "گلا" },
        ["Shalwar Kameez"] = new()
        {
            // Plain length fields (Input Field column was empty = shown; no checkboxes)
            "قمیض لمبائی", "تیرہ", "بازو", "چھاتی", "کمر", "گھیرہ", "گلا", "شلوار لمبائی", "پائنچہ", "شلوار گھیرہ", "بازو موڈا", "کف", "پٹی", "آسن",

            // Reference/design number fields
            new FieldDef { Name = "سوٹ ڈیزائن", Kind = FieldKind.Text, ValuePlaceholder = "Ref..." },
            new FieldDef { Name = "کڑھائی",     Kind = FieldKind.Text, ValuePlaceholder = "Ref..." },

            // Length + style options together (Input Field + CheckBoxes both shown)
            new FieldDef { Name = "کالر",        Kind = FieldKind.Both, Options = new[] { "سادہ", "گول" }, ValueOptions = new[] { "انچ2 ", "انچ2.5 ", "Other" } },
            new FieldDef { Name = "بین",         Kind = FieldKind.Both, Options = new[] { "سادہ", "گول" }, ValueOptions = new[] { "انچ", "پونی انچ", "Other" } },
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
            new FieldDef { Name = "گھیرا ڈیزائن",  Kind = FieldKind.Both, Options = new[] { "گول", "چورس" } },
            new FieldDef { Name = "بازو شیپ",      Kind = FieldKind.Checkbox, Options = new[] { "ہاں" } },

            // بازو جوک, بپ — every column NA in the table, so skipped
        },
        ["Point Of Sale"]  = new()
        {
            new FieldDef { Name = "سوٹ سلائی",   Kind = FieldKind.Text, ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true, HasQuantity = true },
            new FieldDef { Name = "سوٹ خریداری", Kind = FieldKind.Text, ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true, HasQuantity = true },
            new FieldDef { Name = "ایڈوانس",     Kind = FieldKind.Text, ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true },
            new FieldDef { Name = "سابقہ رقم",   Kind = FieldKind.Text, ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true },
            new FieldDef { Name = "ڈسکاؤنٹ",     Kind = FieldKind.Text, ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true },
        },
    };

    private static Customer MapCustomer(SqliteDataReader r) => new()
    {
        Id                = Convert.ToInt64(r["Id"]),
        Name              = r["Name"].ToString()!,
        SerialNumber      = r["SerialNumber"] == DBNull.Value ? null : r["SerialNumber"].ToString(),
        Phone             = r["Phone"]    == DBNull.Value ? null : r["Phone"].ToString(),
        Address           = r["Address"]  == DBNull.Value ? null : r["Address"].ToString(),
        Notes             = r["Notes"]    == DBNull.Value ? null : r["Notes"].ToString(),
        CreatedAt         = r["CreatedAt"]?.ToString(),
        UpdatedAt         = r["UpdatedAt"]?.ToString(),
        LatestOrderId     = r["LatestOrderId"] == DBNull.Value ? null : Convert.ToInt64(r["LatestOrderId"]),
        LatestOrderStatus = r["LatestOrderStatus"] == DBNull.Value ? null : r["LatestOrderStatus"].ToString(),
        LatestOrderCreatedAt = r["LatestOrderCreatedAt"] == DBNull.Value ? null : r["LatestOrderCreatedAt"].ToString(),
    };
}
