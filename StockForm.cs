namespace TailorShop;

public class StockForm : Form
{
    private readonly DataGridView _grid;
    private readonly TextBox      _txtSearch;
    private readonly Label        _lblTypesCard;
    private readonly Label        _lblItemsCard;
    private readonly Label        _lblValueCard;

    public StockForm()
    {
        Text          = "Stock";
        StartPosition = FormStartPosition.CenterParent;
        // Opening size scales with DPI too, but never past the screen's work area.
        var work      = Screen.PrimaryScreen?.WorkingArea.Size ?? new Size(1366, 728);
        ClientSize    = new Size(Math.Min(Px(1120), work.Width - 40), Math.Min(Px(600), work.Height - 60));
        MinimumSize   = new Size(1000, 460);
        BackColor     = Theme.NormalGrey;
        Font          = new Font("Segoe UI", 10f);

        // Header row: [Stock | ...... Types · Items · Stock Value | search]. The number
        // boxes look like the dashboard's; on a narrow window they wrap onto a second line
        // inside the header (which grows taller) instead of being cut off or overlapping.
        var title = new Label
        {
            Text      = "Stock",
            ForeColor = Theme.TextOnDark,
            Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
            AutoSize  = true,
            Anchor    = AnchorStyles.Left,
            Margin    = new Padding(0, 0, Px(20), 0)
        };

        _txtSearch = new TextBox
        {
            Font        = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.None,
            BackColor   = Color.White,
            Location    = new Point(Px(4), Px(5)),
            Width       = Px(212)
        };
        _txtSearch.TextChanged += (_, _) => Reload();

        var searchHolder = new Panel
        {
            BackColor = Theme.DarkGold,
            Size      = new Size(Px(220), _txtSearch.PreferredHeight + Px(10)),
            Anchor    = AnchorStyles.Right,
            Margin    = new Padding(Px(10), 0, 0, 0)
        };
        searchHolder.Controls.Add(_txtSearch);

        // Narrower floor than the dashboard's boxes — the header shares its width with
        // the title and search box, so they stay on one line on more window sizes.
        _lblTypesCard = Theme.StatCard(this);
        _lblItemsCard = Theme.StatCard(this);
        _lblValueCard = Theme.StatCard(this);
        foreach (var card in new[] { _lblTypesCard, _lblItemsCard, _lblValueCard })
            card.MinimumSize = new Size(Px(120), card.MinimumSize.Height);

        // Anchored Right (not docked) so the block of boxes hugs the search box, while
        // the boxes keep their Types → Items → Value order when they wrap.
        var cardsFlow = new FlowLayoutPanel
        {
            Anchor        = AnchorStyles.Right,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            WrapContents  = true,
            BackColor     = Color.Transparent,
            Margin        = new Padding(0)
        };
        cardsFlow.Controls.AddRange(new Control[] { _lblTypesCard, _lblItemsCard, _lblValueCard });

        var headerRow = new TableLayoutPanel
        {
            Dock         = DockStyle.Top,
            AutoSize     = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount  = 3,
            RowCount     = 1,
            BackColor    = Color.Transparent,
            MinimumSize  = new Size(0, Px(56)),
            Padding      = new Padding(Px(20), Px(6), Px(20), Px(6))
        };
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerRow.Controls.Add(title, 0, 0);
        headerRow.Controls.Add(cardsFlow, 1, 0);
        headerRow.Controls.Add(searchHolder, 2, 0);

        // Divider docked Top and added first so it always sits right under the row,
        // however tall the row grows; the panel is sized to exactly the two of them.
        var header = new Panel { Dock = DockStyle.Top, BackColor = Theme.DarkGrey };
        var headerDivider = Theme.AccentDivider(DockStyle.Top);
        header.Controls.Add(headerDivider);
        header.Controls.Add(headerRow);
        void FitHeader() => header.Height = headerRow.Height + headerDivider.Height;
        headerRow.SizeChanged += (_, _) => FitHeader();
        FitHeader();
        Controls.Add(header);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = Px(56), BackColor = Theme.DarkGrey };
        footer.Controls.Add(Theme.AccentDivider(DockStyle.Top));

        var btnAdd     = MakeButton("New Item",     Theme.NormalGrey,   Theme.TextOnNormal,       130);
        var btnEdit    = MakeButton("Edit",         Theme.NormalGrey,   Theme.TextOnNormal,       92);
        var btnDelete  = MakeButton("Delete",       Theme.DeleteAccent, Theme.TextOnDeleteAccent, 92);
        var btnHistory = MakeButton("Sale History", Theme.DarkGrey,     Theme.TextOnDark,         150);
        var btnClose   = MakeButton("Close",        Theme.NormalGrey,   Theme.TextOnNormal,       100);

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

        // [New | Edit | Delete | Sale History] .................... [Close]
        var actions = new FlowLayoutPanel
        {
            AutoSize     = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            BackColor    = Color.Transparent,
            Anchor       = AnchorStyles.Left,
            Margin       = new Padding(0)
        };
        actions.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnHistory });
        btnClose.Anchor = AnchorStyles.Right;

        var bar = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 3,
            RowCount    = 1,
            BackColor   = Color.Transparent,
            Padding     = new Padding(Px(10), Px(3), Px(10), 0)
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        bar.Controls.Add(actions, 0, 0);
        bar.Controls.Add(btnClose, 2, 0);
        footer.Controls.Add(bar);
        bar.BringToFront();
        Controls.Add(footer);

        _grid = new DataGridView
        {
            Dock                      = DockStyle.Fill,
            AllowUserToAddRows        = false,
            AllowUserToDeleteRows     = false,
            AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor           = Theme.NormalGrey,
            BorderStyle               = BorderStyle.None,
            ColumnHeadersHeight       = Px(42),
            MultiSelect               = false,
            ReadOnly                  = true,
            RowHeadersVisible         = false,
            SelectionMode             = DataGridViewSelectionMode.FullRowSelect,
            GridColor                 = Theme.DarkGold,
            CellBorderStyle           = DataGridViewCellBorderStyle.Single,
            EnableHeadersVisualStyles = false,
            Font                      = new Font("Segoe UI", 9.5f)
        };
        _grid.RowTemplate.Height = Px(36);
        _grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
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
        _lblTypesCard.Text = $"Types: {items.Count}";
        _lblItemsCard.Text = $"Items: {totalItems}";
        _lblValueCard.Text = $"Stock Value: Rs {totalValue:N0}";
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

    private int Px(int logical) => LogicalToDeviceUnits(logical);

    // minWidth is the 100%-scaling width; the button still grows if its text + icon need more.
    private Button MakeButton(string text, Color back, Color fore, int minWidth)
    {
        var btn = new Button
        {
            Text         = text,
            BackColor    = back,
            ForeColor    = fore,
            FlatStyle    = FlatStyle.Flat,
            Font         = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            MinimumSize  = new Size(Px(minWidth), Px(33)),
            AutoSize     = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding      = new Padding(Px(6), 0, Px(6), 0),
            Margin       = new Padding(Px(4), 0, Px(4), 0),
            Anchor       = AnchorStyles.Left,
            Cursor       = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btn.FlatAppearance.BorderSize  = 2;
        btn.FlatAppearance.BorderColor = Theme.DarkGold;
        btn.FlatAppearance.MouseOverBackColor = Theme.Hover(back);
        Theme.RoundCorners(btn, 6);
        return btn;
    }
}
