namespace TailorShop;

public class StockHistoryForm : Form
{
    private readonly long? _stockItemId;
    private readonly DataGridView _grid;
    private readonly Label _lblSummary;
    private readonly DateTimePicker _dtFrom;
    private readonly DateTimePicker _dtTo;

    public StockHistoryForm(long? stockItemId)
    {
        _stockItemId = stockItemId;

        Text          = "Stock Sale History";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize    = new Size(1180, 636);
        MinimumSize   = new Size(1000, 480);
        BackColor     = Theme.NormalGrey;
        Font          = new Font("Segoe UI", 10f);

        var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.DarkGrey };
        header.Controls.Add(new Label
        {
            Text      = "Sale History",
            ForeColor = Theme.TextOnDark,
            Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
            AutoSize  = true,
            Location  = new Point(20, 14)
        });
        header.Controls.Add(Theme.AccentDivider(DockStyle.Bottom));
        Controls.Add(header);

        var filterBar = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Theme.NormalGrey };
        filterBar.Controls.Add(Theme.AccentDivider(DockStyle.Bottom));

        var lblFrom = new Label
        {
            Text      = "From:",
            ForeColor = Theme.TextOnNormal,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            AutoSize  = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Location  = new Point(20, 22)
        };
        filterBar.Controls.Add(lblFrom);

        _dtFrom = new DateTimePicker
        {
            Format    = DateTimePickerFormat.Short,
            Location  = new Point(74, 15),
            Size      = new Size(130, 28),
            Font      = new Font("Segoe UI", 9.5f),
            CalendarMonthBackground = Theme.NormalGrey,
            CalendarForeColor = Theme.TextOnNormal,
            CalendarTitleBackColor = Theme.DarkGrey,
            CalendarTitleForeColor = Theme.TextOnDark,
            CalendarTrailingForeColor = Theme.RowAlt
        };
        filterBar.Controls.Add(_dtFrom);

        var lblTo = new Label
        {
            Text      = "To:",
            ForeColor = Theme.TextOnNormal,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            AutoSize  = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Location  = new Point(226, 22)
        };
        filterBar.Controls.Add(lblTo);

        _dtTo = new DateTimePicker
        {
            Format    = DateTimePickerFormat.Short,
            Location  = new Point(262, 15),
            Size      = new Size(130, 28),
            Font      = new Font("Segoe UI", 9.5f),
            CalendarMonthBackground = Theme.NormalGrey,
            CalendarForeColor = Theme.TextOnNormal,
            CalendarTitleBackColor = Theme.DarkGrey,
            CalendarTitleForeColor = Theme.TextOnDark,
            CalendarTrailingForeColor = Theme.RowAlt
        };
        filterBar.Controls.Add(_dtTo);

        var btnApply = MakeButton("Apply", Theme.DarkGrey, Theme.TextOnDark, new Point(414, 13), new Size(96, 32));
        Theme.SetIcon(btnApply, Theme.Glyph.Search, 14);
        btnApply.Click += (_, _) => LoadData();
        filterBar.Controls.Add(btnApply);

        var btnClear = MakeButton("Clear", Theme.NormalGrey, Theme.TextOnNormal, new Point(518, 13), new Size(96, 32));
        Theme.SetIcon(btnClear, Theme.Glyph.Clear, 14);
        btnClear.Click += (_, _) =>
        {
            _dtFrom.Value = DefaultFrom();
            _dtTo.Value   = DateTime.Today;
            LoadData();
        };
        filterBar.Controls.Add(btnClear);

        var btnExport = MakeButton("Export to Excel", Theme.DarkGold, Theme.TextOnGold,
            new Point(filterBar.Width - 174, 13), new Size(154, 32));
        btnExport.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Theme.SetIcon(btnExport, Theme.Glyph.Save, 14);
        btnExport.Click += BtnExport_Click;
        filterBar.Controls.Add(btnExport);

        Controls.Add(filterBar);

        _dtFrom.Value = DefaultFrom();
        _dtTo.Value   = DateTime.Today;

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Theme.DarkGrey };
        footer.Controls.Add(Theme.AccentDivider(DockStyle.Top));

        _lblSummary = new Label
        {
            Font      = new Font("Segoe UI", 9.5f),
            ForeColor = Theme.TextOnDark,
            AutoSize  = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location  = new Point(16, 11),
            Size      = new Size(footer.Width - 16 - 128, 33),
            Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        footer.Controls.Add(_lblSummary);

        var btnClose = MakeButton("Close", Theme.NormalGrey, Theme.TextOnNormal,
            new Point(footer.Width - 114, 11), new Size(100, 33));
        btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Theme.SetIcon(btnClose, Theme.Glyph.Close);
        btnClose.Click += (_, _) => Close();
        footer.Controls.Add(btnClose);
        Controls.Add(footer);

        _grid = new DataGridView
        {
            Dock                      = DockStyle.Fill,
            AllowUserToAddRows        = false,
            AllowUserToDeleteRows     = false,
            AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor           = Theme.NormalGrey,
            BorderStyle               = BorderStyle.None,
            ColumnHeadersHeight       = 42,
            MultiSelect               = false,
            ReadOnly                  = true,
            RowHeadersVisible         = false,
            SelectionMode             = DataGridViewSelectionMode.FullRowSelect,
            GridColor                 = Theme.DarkGold,
            CellBorderStyle           = DataGridViewCellBorderStyle.Single,
            EnableHeadersVisualStyles = false,
            Font                      = new Font("Segoe UI", 9.5f)
        };
        _grid.RowTemplate.Height = 36;
        _grid.Columns.Add("colDate",     "Sold On");
        _grid.Columns.Add("colCustomer", "Customer");
        _grid.Columns.Add("colType",     "Suit Type");
        _grid.Columns.Add("colColor",    "Color");
        _grid.Columns.Add("colQty",      "Qty");
        _grid.Columns.Add("colPrice",    "Unit Price (Rs)");
        _grid.Columns.Add("colTotal",    "Total (Rs)");
        _grid.Columns.Add("colAdded",    "Item Added");
        _grid.Columns.Add("colUpdated",  "Item Updated");

        _grid.Columns["colDate"]!.FillWeight     = 110;
        _grid.Columns["colCustomer"]!.FillWeight = 110;
        _grid.Columns["colType"]!.FillWeight     = 90;
        _grid.Columns["colColor"]!.FillWeight    = 70;
        _grid.Columns["colQty"]!.FillWeight      = 40;
        _grid.Columns["colPrice"]!.FillWeight    = 105;
        _grid.Columns["colTotal"]!.FillWeight    = 85;
        _grid.Columns["colAdded"]!.FillWeight    = 95;
        _grid.Columns["colUpdated"]!.FillWeight  = 100;

        _grid.Columns["colQty"]!.DefaultCellStyle.Alignment   = DataGridViewContentAlignment.MiddleCenter;
        _grid.Columns["colPrice"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _grid.Columns["colTotal"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        _grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.DarkGrey;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextOnDark;
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.DarkGrey;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        _grid.DefaultCellStyle.BackColor = Theme.NormalGrey;
        _grid.DefaultCellStyle.ForeColor = Theme.TextOnNormal;
        _grid.DefaultCellStyle.SelectionBackColor = Theme.DeleteAccent;
        _grid.DefaultCellStyle.SelectionForeColor = Theme.TextOnDeleteAccent;
        _grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.RowAlt;
        _grid.AlternatingRowsDefaultCellStyle.ForeColor = Theme.TextOnRowAlt;
        _grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Theme.DeleteAccent;
        _grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Theme.TextOnDeleteAccent;

        Controls.Add(_grid);
        _grid.BringToFront();
        Shown += (_, _) => Theme.CenterButtons(this);

        LoadData();
    }

    private static DateTime DefaultFrom() => DateTime.Today.AddDays(-6);

    private void LoadData()
    {
        var from = _dtFrom.Value.Date;
        var to   = _dtTo.Value.Date.AddDays(1).AddTicks(-1);

        var sales = Database.GetStockSales(_stockItemId, from, to);

        _grid.Rows.Clear();
        foreach (var s in sales)
        {
            _grid.Rows.Add(StockDate.Long(s.SoldAt), s.CustomerName, s.SuitType, s.Color ?? "-",
                s.Qty, s.UnitPrice.ToString("N0"), s.Total.ToString("N0"),
                StockDate.Short(s.ItemAddedAt), StockDate.Short(s.ItemUpdatedAt));
        }

        _lblSummary.Text = $"{sales.Count} sale(s)   |   {sales.Sum(s => s.Qty)} item(s)   |   Rs {sales.Sum(s => s.Total):N0}";
    }

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        if (_grid.Rows.Count == 0)
        {
            MessageBox.Show("There is no data to export.", "Export to Excel",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title    = "Export Stock Sale History",
            Filter   = "CSV (Excel) file (*.csv)|*.csv",
            FileName = $"StockHistory_{DateTime.Now:yyyy-MM-dd_HHmm}.csv"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            using var writer = new StreamWriter(dialog.FileName, false, new System.Text.UTF8Encoding(true));

            var headers = _grid.Columns.Cast<DataGridViewColumn>().Select(c => c.HeaderText);
            writer.WriteLine(string.Join(",", headers.Select(CsvCell)));

            foreach (DataGridViewRow row in _grid.Rows)
            {
                var cells = row.Cells.Cast<DataGridViewCell>().Select(c => c.Value?.ToString() ?? "");
                writer.WriteLine(string.Join(",", cells.Select(CsvCell)));
            }

            MessageBox.Show($"Exported {_grid.Rows.Count} row(s).{Environment.NewLine}{Environment.NewLine}{dialog.FileName}",
                "Export to Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Export to Excel", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string CsvCell(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private static Button MakeButton(string text, Color back, Color fore, Point location, Size size)
    {
        var btn = new Button
        {
            Text      = text,
            BackColor = back,
            ForeColor = fore,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location  = location,
            Size      = size,
            Cursor    = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btn.FlatAppearance.BorderSize  = 2;
        btn.FlatAppearance.BorderColor = Theme.DarkGold;
        btn.FlatAppearance.MouseOverBackColor = Theme.Hover(back);
        Theme.RoundCorners(btn, 6);
        return btn;
    }
}
