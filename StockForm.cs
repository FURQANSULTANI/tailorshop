namespace TailorShop;

public class StockForm : Form
{
    private readonly DataGridView _grid;
    private readonly TextBox      _txtSearch;
    private readonly Label        _lblSummary;

    public StockForm()
    {
        Text          = "Stock";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize    = new Size(1120, 600);
        MinimumSize   = new Size(1000, 460);
        BackColor     = Theme.NormalGrey;
        Font          = new Font("Segoe UI", 10f);

        var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.DarkGrey };
        header.Controls.Add(new Label
        {
            Text      = "Stock",
            ForeColor = Theme.TextOnDark,
            Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
            AutoSize  = true,
            Location  = new Point(20, 14)
        });

        _txtSearch = new TextBox
        {
            Font        = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.None,
            BackColor   = Color.White,
            Location    = new Point(4, 5),
            Width       = 212
        };
        _txtSearch.TextChanged += (_, _) => Reload();

        var searchHolder = new Panel
        {
            BackColor = Theme.DarkGold,
            Size      = new Size(220, 30),
            Location  = new Point(header.Width - 240, 13),
            Anchor    = AnchorStyles.Top | AnchorStyles.Right
        };
        searchHolder.Controls.Add(_txtSearch);
        header.Controls.Add(searchHolder);
        header.Controls.Add(Theme.AccentDivider(DockStyle.Bottom));
        Controls.Add(header);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Theme.DarkGrey };
        footer.Controls.Add(Theme.AccentDivider(DockStyle.Top));

        var btnAdd     = MakeButton("New Item",     Theme.NormalGrey,   Theme.TextOnNormal,       new Point(14, 11),  new Size(130, 33));
        var btnEdit    = MakeButton("Edit",         Theme.NormalGrey,   Theme.TextOnNormal,       new Point(152, 11), new Size(92, 33));
        var btnDelete  = MakeButton("Delete",       Theme.DeleteAccent, Theme.TextOnDeleteAccent, new Point(252, 11), new Size(92, 33));
        var btnHistory = MakeButton("Sale History", Theme.DarkGrey,     Theme.TextOnDark,         new Point(352, 11), new Size(150, 33));
        var btnClose   = MakeButton("Close",        Theme.NormalGrey,   Theme.TextOnNormal,       new Point(footer.Width - 114, 11), new Size(100, 33));
        btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        Theme.SetIcon(btnAdd,     Theme.Glyph.Add);
        Theme.SetIcon(btnEdit,    Theme.Glyph.Edit);
        Theme.SetIcon(btnDelete,  Theme.Glyph.Delete);
        Theme.SetIcon(btnHistory, Theme.Glyph.History);
        Theme.SetIcon(btnClose,   Theme.Glyph.Close);

        btnAdd.Click     += (_, _) => EditItem(null);
        btnEdit.Click    += (_, _) => EditSelected();
        btnDelete.Click  += BtnDelete_Click;
        btnHistory.Click += (_, _) => { using var f = new StockHistoryForm(null); f.ShowDialog(this); };
        btnClose.Click   += (_, _) => Close();

        footer.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnHistory, btnClose });

        _lblSummary = new Label
        {
            Font      = new Font("Segoe UI", 9.5f),
            ForeColor = Theme.TextOnDark,
            AutoSize  = false,
            TextAlign = ContentAlignment.MiddleRight,
            Location  = new Point(516, 11),
            Size      = new Size(footer.Width - 516 - 128, 33),
            Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        footer.Controls.Add(_lblSummary);
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
        _grid.Columns.Add("colId",       "Id");
        _grid.Columns.Add("colType",     "Suit Type");
        _grid.Columns.Add("colColor",    "Color");
        _grid.Columns.Add("colQty",      "In Stock");
        _grid.Columns.Add("colPurchase", "Purchase (Rs)");
        _grid.Columns.Add("colRetail",   "Retail (Rs)");
        _grid.Columns.Add("colAdded",    "Added");
        _grid.Columns.Add("colUpdated",  "Updated");
        _grid.Columns["colId"]!.Visible = false;

        _grid.Columns["colQty"]!.FillWeight      = 65;
        _grid.Columns["colPurchase"]!.FillWeight = 85;
        _grid.Columns["colRetail"]!.FillWeight   = 85;
        _grid.Columns["colAdded"]!.FillWeight    = 80;
        _grid.Columns["colUpdated"]!.FillWeight  = 80;

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
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditSelected(); };

        Controls.Add(_grid);
        _grid.BringToFront();

        Reload();
        Shown += (_, _) => Theme.CenterButtons(this);
    }

    private void Reload()
    {
        var items = Database.GetStockItems(_txtSearch.Text.Trim());
        _grid.Rows.Clear();
        foreach (var i in items)
        {
            var idx = _grid.Rows.Add(i.Id, i.SuitType, i.Color ?? "-", i.Quantity,
                i.PurchasePrice.ToString("N0"), i.RetailPrice.ToString("N0"),
                StockDate.Short(i.CreatedAt), StockDate.Short(i.UpdatedAt));

            if (i.Quantity < 5)
            {
                var cell = _grid.Rows[idx].Cells["colQty"];
                cell.Style.BackColor = Theme.AlertRed;
                cell.Style.ForeColor = Theme.TextOnAlert;
                cell.Style.Font = new Font(_grid.Font, FontStyle.Bold);
                cell.Style.SelectionBackColor = Theme.Hover(Theme.AlertRed);
                cell.Style.SelectionForeColor = Theme.TextOnAlert;
            }
        }

        var totalItems = items.Sum(i => i.Quantity);
        var totalValue = items.Sum(i => i.Quantity * i.PurchasePrice);
        _lblSummary.Text = $"{items.Count} type(s)   |   {totalItems} item(s)   |   Stock value Rs {totalValue:N0}";
    }

    private StockItem? Selected()
    {
        if (_grid.CurrentRow == null) return null;
        var id = Convert.ToInt64(_grid.CurrentRow.Cells["colId"].Value);
        return Database.GetStockItem(id);
    }

    private void EditSelected()
    {
        var item = Selected();
        if (item == null)
        {
            MessageBox.Show("Select an item first.", "Stock", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        EditItem(item);
    }

    private void EditItem(StockItem? item)
    {
        using var form = new StockEditForm(item);
        if (form.ShowDialog(this) == DialogResult.OK) Reload();
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        var item = Selected();
        if (item == null)
        {
            MessageBox.Show("Select an item first.", "Stock", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var label = string.IsNullOrWhiteSpace(item.Color) ? item.SuitType : $"{item.SuitType} - {item.Color}";
        var confirm = MessageBox.Show(
            $"Delete '{label}'?" + Environment.NewLine + Environment.NewLine +
            "Past sale history for this item will still be kept.",
            "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

        if (confirm != DialogResult.Yes) return;

        Database.DeleteStockItem(item.Id);
        Reload();
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
