namespace TailorShop;

public partial class MainForm : Form {
    private List<Customer> _customers = new();
    private WhatsAppClient _waClient = null!;
    private System.Windows.Forms.Timer? _healthTimer;
    private System.Windows.Forms.Timer? _retryTimer;
    private WhatsAppQrForm? _qrForm;
    private bool _qrDismissed;
    private bool _healthBusy;
    private bool _retryBusy;
    private int _markReadyHoverRow = -1;
    private Panel _toolBar = null!;
    private TableLayoutPanel _toolbarRow = null!;
    private FlowLayoutPanel _cardsFlow = null!;
    private FlowLayoutPanel _actionsFlow = null!;
    private Label _lblStockCard = null!;
    private Label _lblRemainingCard = null!;
    private Label _lblUnstitchedCard = null!;

    public MainForm() {
        InitializeComponent();
        Theme.FitToScreen(this);
        Database.Initialize();
        ApplyGridStyles();
        ApplyPolish();
        BuildToolbarLayout();
        BuildSummaryCards();
        BuildActionButtons();
        WireEvents();
        LoadCustomers();
        Task.Run(Backup.RunIfDue);

        WhatsAppConfig.Load();
        _waClient = new WhatsAppClient(WhatsAppConfig.Port);
        Task.Run(WhatsAppProcess.Start);
        StartWhatsAppTimers();
        FormClosing += (_, _) => WhatsAppProcess.Stop();
        Shown += (_, _) => Theme.CenterButtons(this);
    }

    // Every size below is in 96-DPI logical pixels; Px() scales it to the monitor's
    // DPI so the layout looks the same at 100%, 125%, 150%... Windows scaling.
    private int Px(int logical) => LogicalToDeviceUnits(logical);

    private void ApplyPolish() {
        foreach (var btn in new[] { btnSearch, btnClear, btnAdd, btnEdit, btnDelete, btnRelinkWhatsApp })
            Theme.RoundCorners(btn, 6);

        Theme.SetIcon(btnSearch, Theme.Glyph.Search);
        Theme.SetIcon(btnClear, Theme.Glyph.Clear);
        Theme.SetIcon(btnAdd, Theme.Glyph.Add);
        Theme.SetIcon(btnEdit, Theme.Glyph.Edit);
        Theme.SetIcon(btnDelete, Theme.Glyph.Delete);

        foreach (var btn in new[] { btnSearch, btnClear, btnAdd, btnEdit })
            Theme.ApplyLightHover(btn);

        BuildHeaderLayout();
        BuildBottomLayout();

        panelHeader.Controls.Add(Theme.AccentDivider(DockStyle.Bottom));
        panelBottom.Controls.Add(Theme.AccentDivider(DockStyle.Top));
    }

    // Buttons grow to fit their text + icon instead of clipping it; the designer
    // size is kept as a floor so wide screens look exactly as before.
    private static void MakeAutoSize(Button btn) {
        btn.MinimumSize = btn.Size;
        btn.AutoSize = true;
        btn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
    }

    // ── Header: [Golden Tailor logo ............ Koder Bench brand] ─────────────
    private void BuildHeaderLayout() {
        panelHeader.MinimumSize = new Size(0, panelHeader.Height);
        panelHeader.AutoSize = true;
        panelHeader.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var header = new TableLayoutPanel {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(Px(14), Px(4), Px(14), Px(4))
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.Controls.Add(BuildAppLogo(), 0, 0);
        header.Controls.Add(BuildHeaderBrand(), 2, 0);

        panelHeader.Controls.Remove(lblTitle);
        panelHeader.Controls.Remove(lblSubtitle);
        panelHeader.Controls.Add(header);
    }

    private Control BuildAppLogo() {
        var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "GoldenTailor_Logo.png");
        if (File.Exists(logoPath)) {
            try {
                using var fs = new FileStream(logoPath, FileMode.Open, FileAccess.Read);
                return new PictureBox {
                    Image = new Bitmap(Image.FromStream(fs)),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent,
                    Size = new Size(Px(200), Px(72)),
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(0)
                };
            }
            catch { }
        }

        // No logo file: fall back to the designer's text title + subtitle.
        var fallback = new FlowLayoutPanel {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0)
        };
        lblTitle.Margin = new Padding(0);
        lblSubtitle.Margin = new Padding(Px(2), 0, 0, 0);
        fallback.Controls.Add(lblTitle);
        fallback.Controls.Add(lblSubtitle);
        return fallback;
    }

    private Control BuildHeaderBrand() {
        var brand = new TableLayoutPanel {
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0)
        };
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        brand.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        brand.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "TheKoderBench_Logo_White.png");
        if (File.Exists(logoPath)) {
            try {
                using var fs = new FileStream(logoPath, FileMode.Open, FileAccess.Read);
                using var raw = Image.FromStream(fs);
                using var trimmed = Theme.TrimUniformMargins(raw);
                var pic = new PictureBox {
                    Image = Theme.ToSilhouette(trimmed, Theme.DarkGold),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent,
                    Size = new Size(Px(62), Px(56)),
                    Anchor = AnchorStyles.None,
                    Margin = new Padding(0, 0, Px(2), 0)
                };
                brand.Controls.Add(pic, 0, 0);
                brand.SetRowSpan(pic, 2);
            }
            catch { }
        }

        // AutoSize labels are measured at the current DPI, so the text can never be cut off.
        brand.Controls.Add(new Label {
            Text = "The Koder Bench",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Theme.DarkGold,
            BackColor = Color.Transparent,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            Margin = new Padding(0)
        }, 1, 0);

        brand.Controls.Add(new Label {
            Text = "BUILD  ·  TRUST  ·  SOLVE",
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Theme.DarkGold,
            BackColor = Color.Transparent,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            Margin = new Padding(Px(1), Px(2), 0, 0)
        }, 1, 1);

        return brand;
    }

    // ── Toolbar bar: [summary cards ......... action buttons], then the search row ──
    private void BuildToolbarLayout() {
        _cardsFlow = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        _actionsFlow = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        _toolbarRow = new TableLayoutPanel {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(Px(14), Px(6), Px(14), Px(6))
        };
        _toolbarRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _toolbarRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _toolbarRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _toolbarRow.Controls.Add(_cardsFlow, 0, 0);
        _toolbarRow.Controls.Add(_actionsFlow, 1, 0);

        // Divider docked Top and added first so it always sits right under the row,
        // however tall the row grows when it wraps; the bar is sized to exactly both.
        _toolBar = new Panel { Dock = DockStyle.Top, BackColor = Theme.DarkGrey };
        var toolDivider = Theme.AccentDivider(DockStyle.Top);
        _toolBar.Controls.Add(toolDivider);
        _toolBar.Controls.Add(_toolbarRow);
        void FitToolBar() => _toolBar.Height = _toolbarRow.Height + toolDivider.Height;
        _toolbarRow.SizeChanged += (_, _) => FitToolBar();
        FitToolBar();

        // Among docked siblings a higher child index sits higher on screen, so the Fill grid
        // stays lowest and the Top panels ascend: search row, then this bar, then the header.
        Controls.Add(_toolBar);
        Controls.SetChildIndex(grid, 0);
        Controls.SetChildIndex(panelBottom, 1);
        Controls.SetChildIndex(panelSearch, 2);
        Controls.SetChildIndex(_toolBar, 3);
        Controls.SetChildIndex(panelHeader, 4);

        // Search row: label, bordered box, Search, Clear — wraps on a narrow window.
        var search = new FlowLayoutPanel {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            BackColor = Color.Transparent,
            Padding = new Padding(Px(14), Px(6), Px(14), Px(6))
        };
        var searchBox = WrapWithBorder(txtSearch, Theme.DarkGold);
        MakeAutoSize(btnSearch);
        MakeAutoSize(btnClear);
        foreach (Control c in new Control[] { lblSearch, searchBox, btnSearch, btnClear }) {
            panelSearch.Controls.Remove(c);
            c.Anchor = AnchorStyles.Left; // no Top/Bottom → vertically centered in the flow row
            c.Margin = new Padding(Px(4), Px(3), Px(4), Px(3));
            search.Controls.Add(c);
        }
        panelSearch.Controls.Remove(btnRelinkWhatsApp); // lives on the toolbar row instead
        panelSearch.Controls.Add(search);

        // Panel.AutoSize doesn't track wrapping children reliably, so size it to the row.
        void FitSearch() => panelSearch.Height = search.Height;
        search.SizeChanged += (_, _) => FitSearch();
        FitSearch();
    }

    // Split the toolbar width between cards and buttons in proportion to how wide
    // each wants to be: on a wide window both fit on one line, on a narrow one
    // each side wraps onto extra lines instead of overlapping the other.
    private void BalanceToolbarColumns() {
        var cards = _cardsFlow.GetPreferredSize(Size.Empty).Width;
        var actions = _actionsFlow.GetPreferredSize(Size.Empty).Width;
        if (cards + actions == 0) return;
        _toolbarRow.ColumnStyles[0].Width = cards;
        _toolbarRow.ColumnStyles[1].Width = actions;
    }

    private Button MakeToolbarButton(string text, Theme.Glyph glyph, EventHandler onClick) {
        var btn = new Button {
            Text = "  " + text,
            BackColor = Theme.DarkGrey,
            ForeColor = Theme.TextOnDark,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            TextImageRelation = TextImageRelation.ImageBeforeText
        };
        btn.FlatAppearance.BorderSize = 2;
        btn.FlatAppearance.BorderColor = Theme.DarkGold;
        btn.FlatAppearance.MouseOverBackColor = Theme.Hover(Theme.DarkGrey);
        StyleToolbarButton(btn);
        Theme.RoundCorners(btn, 6);
        Theme.SetIcon(btn, glyph);
        btn.Click += onClick;
        return btn;
    }

    private void StyleToolbarButton(Button btn) {
        btn.Anchor = AnchorStyles.None;
        btn.Padding = new Padding(Px(8), 0, Px(8), 0);
        btn.Margin = new Padding(Px(5), Px(3), Px(5), Px(3));
        btn.MinimumSize = new Size(0, Px(32));
        btn.AutoSize = true;
        btn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
    }

    private void BuildActionButtons() {
        StyleToolbarButton(btnRelinkWhatsApp);
        // The designer draws the chat icon at a fixed 18px; draw it at this DPI like the others.
        btnRelinkWhatsApp.Image = Theme.CreateChatBubbleIcon(Theme.TextOnDark, Px(18));

        // Right-to-left flow: the first one added ends up right-most.
        _actionsFlow.Controls.Add(btnRelinkWhatsApp);
        _actionsFlow.Controls.Add(MakeToolbarButton("Backup && Restore", Theme.Glyph.Backup, BtnBackup_Click));
        _actionsFlow.Controls.Add(MakeToolbarButton("Stock", Theme.Glyph.Stock,
            (_, _) => { using var f = new StockForm(); f.ShowDialog(this); LoadCustomers(txtSearch.Text); }));
        _actionsFlow.Controls.Add(MakeToolbarButton("Sale History", Theme.Glyph.History,
            (_, _) => { using var f = new StockHistoryForm(null); f.ShowDialog(this); }));

        BalanceToolbarColumns();
    }

    private void BuildSummaryCards() {
        _lblStockCard      = MakeSummaryCard();
        _lblRemainingCard  = MakeSummaryCard();
        _lblUnstitchedCard = MakeSummaryCard();

        _cardsFlow.Controls.Add(_lblStockCard);
        _cardsFlow.Controls.Add(_lblRemainingCard);
        _cardsFlow.Controls.Add(_lblUnstitchedCard);

        RefreshSummaryCards(0m);
    }

    private Label MakeSummaryCard() => Theme.StatCard(this);

    private void RefreshSummaryCards(decimal totalRemaining, int unstitchedQty = 0) {
        var stockItems    = Database.GetStockItems();
        var totalStockQty = stockItems.Sum(s => s.Quantity);
        _lblStockCard.Text      = $"Stock Items: {stockItems.Count}   |   Qty: {totalStockQty}";
        _lblRemainingCard.Text  = $"Remaining Total: Rs {totalRemaining:N0}";
        _lblUnstitchedCard.Text = $"Unstitched Suits: {unstitchedQty}";
        BalanceToolbarColumns();
    }

    private void BtnBackup_Click(object? sender, EventArgs e) {
        using var form = new BackupForm();
        form.ShowDialog(this);
        if (form.DataRestored) {
            Database.Initialize();
            LoadCustomers();
        }
    }

    // ── Bottom bar: New / Edit / Delete / status ────────────────────────────────
    private void BuildBottomLayout() {
        var bar = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(Px(10), Px(6), Px(10), Px(4))
        };
        foreach (var btn in new[] { btnAdd, btnEdit, btnDelete }) MakeAutoSize(btn);
        foreach (Control c in new Control[] { btnAdd, btnEdit, btnDelete, lblStatus }) {
            panelBottom.Controls.Remove(c);
            c.Anchor = AnchorStyles.Left;
            c.Margin = new Padding(Px(4), Px(3), Px(4), Px(3));
            bar.Controls.Add(c);
        }
        lblStatus.Margin = new Padding(Px(12), Px(3), Px(4), Px(3));
        panelBottom.Controls.Add(bar);
    }

    private static Panel WrapWithBorder(TextBox box, Color color, int thickness = 1) {
        var width = box.Width;

        box.BorderStyle = BorderStyle.None;
        var inner = box.PreferredHeight;

        var holder = new Panel {
            BackColor = color,
            Size = new Size(width + thickness * 2, inner + thickness * 2 + 6)
        };

        box.Parent?.Controls.Remove(box);
        box.Location = new Point(thickness, thickness + 3);
        box.Width = width;
        holder.Controls.Add(box);
        return holder;
    }

    private void ApplyGridStyles() {
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.DarkGrey;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextOnDark;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.DarkGrey;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.TextOnDark;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        // DataGridView doesn't DPI-scale its row heights, so scale the designer values here.
        grid.ColumnHeadersHeight = Px(42);
        grid.RowTemplate.Height = Px(36);
        grid.DefaultCellStyle.BackColor = Theme.NormalGrey;
        grid.DefaultCellStyle.ForeColor = Theme.TextOnNormal;
        grid.DefaultCellStyle.SelectionBackColor = Theme.DeleteAccent;
        grid.DefaultCellStyle.SelectionForeColor = Theme.TextOnDeleteAccent;
        grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.RowAlt;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = Theme.TextOnRowAlt;
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Theme.DeleteAccent;
        grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Theme.TextOnDeleteAccent;
        grid.GridColor = Theme.DarkGold;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
    }

    private void WireEvents() {
        btnSearch.Click += (_, _) => LoadCustomers(txtSearch.Text);
        btnClear.Click += (_, _) => { txtSearch.Clear(); LoadCustomers(); };
        btnAdd.Click += BtnAdd_Click;
        btnEdit.Click += BtnEdit_Click;
        btnDelete.Click += BtnDelete_Click;
        btnRelinkWhatsApp.Click += BtnRelinkWhatsApp_Click;
        grid.CellDoubleClick += (s, e) => { if (e.ColumnIndex != colMarkReady.Index) BtnEdit_Click(s, e); };
        grid.CellContentClick += Grid_CellContentClick;
        grid.CellPainting += Grid_CellPainting;
        grid.CellMouseEnter += (_, e) => {
            if (e.ColumnIndex != colMarkReady.Index || e.RowIndex < 0) return;
            _markReadyHoverRow = e.RowIndex;
            grid.InvalidateCell(e.ColumnIndex, e.RowIndex);
        };
        grid.CellMouseLeave += (_, e) => {
            if (e.ColumnIndex != colMarkReady.Index || e.RowIndex < 0) return;
            _markReadyHoverRow = -1;
            grid.InvalidateCell(e.ColumnIndex, e.RowIndex);
        };
        txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadCustomers(txtSearch.Text); };
    }

    private void Grid_CellPainting(object? s, DataGridViewCellPaintingEventArgs e) {
        if (e.RowIndex < 0 || e.ColumnIndex != colMarkReady.Index) return;

        var selected = e.State.HasFlag(DataGridViewElementStates.Selected);
        var style    = e.CellStyle!;

        Color back, fore;
        if (e.RowIndex == _markReadyHoverRow) { back = Theme.DeleteAccent; fore = Theme.TextOnWhite; }
        else if (selected) { back = style.SelectionBackColor; fore = style.SelectionForeColor; }
        else { back = style.BackColor; fore = style.ForeColor; }

        using var brush = new SolidBrush(back);
        e.Graphics!.FillRectangle(brush, e.CellBounds);

        TextRenderer.DrawText(e.Graphics, e.FormattedValue?.ToString() ?? "",
            style.Font, e.CellBounds, fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        e.Handled = true;
    }

    private static string MarkReadyButtonText(string? status) => status switch {
        OrderStatus.Ready     => "Ready ✓",
        OrderStatus.Delivered => "Delivered",
        _                     => "Mark Ready"
    };

    private void LoadCustomers(string search = "") {
        _customers = Database.GetAllCustomers(search);

        var orderIds = _customers.Where(c => c.LatestOrderId != null)
                                 .Select(c => c.LatestOrderId!.Value).ToList();
        var posByOrder = Database.GetSectionMeasurementsByOrder(orderIds, "Point Of Sale");

        var rows = _customers.Select(c => {
            var baaqaya = c.LatestOrderId != null && posByOrder.TryGetValue(c.LatestOrderId.Value, out var pos)
                ? CustomerForm.ComputePosBaaqaya(pos)
                : 0m;
            DateTime.TryParse(c.LatestOrderCreatedAt, out var orderDate);
            return (Customer: c, Baaqaya: baaqaya, OrderDate: orderDate);
        })
        .OrderByDescending(r => r.Baaqaya > 0)
        .ThenBy(r => r.Baaqaya > 0 ? r.OrderDate : DateTime.MaxValue)
        .ThenBy(r => r.Customer.Name)
        .ToList();

        grid.Rows.Clear();
        foreach (var r in rows) {
            DateTime dlv;
            bool dlvParsed;
            var idx = grid.Rows.Add(r.Customer.Id,
                          string.IsNullOrWhiteSpace(r.Customer.SerialNumber) ? "-" : r.Customer.SerialNumber,
                          r.Customer.Name, r.Customer.Phone ?? "-",
                          r.Customer.CreatedAt?.Split(' ')[0] ?? "",
                          (dlvParsed = DateTime.TryParse(r.Customer.LatestOrderDeliveryDate, out dlv)) ? dlv.ToString("dd MMM yyyy") : "-",
                          r.Customer.LatestOrderStatus ?? "-",
                          r.Baaqaya > 0 ? r.Baaqaya.ToString("N0") : "-",
                          MarkReadyButtonText(r.Customer.LatestOrderStatus));

            if (r.Baaqaya > 0) {
                var daysOld = (DateTime.Now.Date - r.OrderDate.Date).Days;
                if (daysOld >= 30) 
                    SetRowAlertColor(grid.Rows[idx], Theme.AlertRed);
                else if (daysOld >= 15) 
                    SetRowAlertColor(grid.Rows[idx], Theme.AlertOrange);
            }

            ApplyDeliveryCellAlert(grid.Rows[idx], r.Customer.LatestOrderStatus, dlvParsed ? dlv : null);
        }

        lblStatus.Text = $"{_customers.Count} customer(s)";

        var unstitchedQty = _customers
            .Where(c => c.LatestOrderStatus == OrderStatus.Pending && c.LatestOrderId != null)
            .Where(c => posByOrder.ContainsKey(c.LatestOrderId!.Value))
            .Sum(c => {
                var suitField = posByOrder[c.LatestOrderId!.Value]
                    .FirstOrDefault(m => m.FieldName == CustomerForm.SuitStitchingFieldName);
                if (suitField == null) return 0;
                if (int.TryParse(suitField.Quantity, out var qty)) return qty;
                // A blank quantity on a row that has an amount still means one suit.
                return string.IsNullOrWhiteSpace(suitField.Value) ? 0 : 1;
            });

        RefreshSummaryCards(rows.Sum(r => r.Baaqaya), unstitchedQty);
    }

    private static void ApplyDeliveryCellAlert(DataGridViewRow row, string? status, DateTime? deliveryDate) {
        if (deliveryDate == null) return;
        if (status is OrderStatus.Ready or OrderStatus.Delivered) return;

        var daysLeft = (deliveryDate.Value.Date - DateTime.Now.Date).Days;
        if (daysLeft > 0) return;

        var color = daysLeft < 0 ? Theme.AlertRed : Theme.AlertOrange;
        var cell  = row.Cells["colDelivery"];
        cell.Style.BackColor = color;
        cell.Style.ForeColor = Theme.TextOnAlert;
        cell.Style.Font = new Font(row.DataGridView!.Font, FontStyle.Bold);
        cell.Style.SelectionBackColor = Theme.Hover(color);
        cell.Style.SelectionForeColor = Theme.TextOnAlert;
        cell.ToolTipText = daysLeft < 0
            ? $"Delivery {Math.Abs(daysLeft)} day(s) overdue"
            : "Delivery due today";
    }

    private static void SetRowAlertColor(DataGridViewRow row, Color color) {
        row.DefaultCellStyle.BackColor = color;
        row.DefaultCellStyle.ForeColor = Theme.TextOnAlert;
        row.DefaultCellStyle.SelectionBackColor = Theme.Hover(color);
        row.DefaultCellStyle.SelectionForeColor = Theme.TextOnAlert;
    }

    private Customer? SelectedCustomer() {
        if (grid.CurrentRow == null) return null;
        var id = Convert.ToInt64(grid.CurrentRow.Cells["colRealId"].Value);
        return _customers.FirstOrDefault(x => x.Id == id);
    }

    private void BtnAdd_Click(object? s, EventArgs e) {
        using var frm = new CustomerForm(null);
        if (frm.ShowDialog() == DialogResult.OK) LoadCustomers(txtSearch.Text);
    }

    private void BtnEdit_Click(object? s, EventArgs e) {
        var c = SelectedCustomer();
        if (c == null) { Info("Pehle ek customer select karein."); return; }
        var full  = Database.GetById(c.Id)!;
        var order = Database.GetLatestOrder(c.Id);
        using var frm = new CustomerForm(full, order);
        if (frm.ShowDialog() == DialogResult.OK) LoadCustomers(txtSearch.Text);
    }

    private void Grid_CellContentClick(object? s, DataGridViewCellEventArgs e) {
        if (e.RowIndex < 0 || e.ColumnIndex != colMarkReady.Index) return;
        var id = Convert.ToInt64(grid.Rows[e.RowIndex].Cells["colRealId"].Value);
        var c  = _customers.FirstOrDefault(x => x.Id == id);
        if (c != null) HandleMarkReady(c);
    }

    private void HandleMarkReady(Customer c) {
        if (c.LatestOrderId == null) return;
        if (c.LatestOrderStatus != OrderStatus.Pending && c.LatestOrderStatus != OrderStatus.Ready) return;

        if (c.LatestOrderStatus == OrderStatus.Pending)
            Database.UpdateOrderStatus(c.LatestOrderId.Value, OrderStatus.Ready);

        if (string.IsNullOrWhiteSpace(c.Phone)) {
            Info($"'{c.Name}' Ready hai. Phone number save nahi hai isliye WhatsApp message nahi bheja ja sakta.");
            LoadCustomers(txtSearch.Text);
            return;
        }

        var health = _waClient.GetHealth();
        if (health.Status != "ready") {
            Info(!string.IsNullOrWhiteSpace(health.Error)
                ? health.Error!
                : "WhatsApp connected nahi hai. Pehle 'Re-link WhatsApp' pe click karke QR code scan karein, phir dobara koshish karein.");
            LoadCustomers(txtSearch.Text);
            return;
        }

        var order          = Database.GetOrderById(c.LatestOrderId.Value);
        var defaultMessage = BuildReadyMessage(order);

        using var dlg = new WhatsAppSendForm(c.Name, c.Phone!, defaultMessage);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Message.Length > 0) {
            var phone   = c.Phone!;
            var text    = dlg.Message;
            var orderId = c.LatestOrderId.Value;
            var custId  = c.Id;

            UseWaitCursor = true;
            Task.Run(() => {
                bool success; string? error = null;
                try {
                    var queueId = Database.EnqueueWhatsAppMessage(orderId, custId, phone, text);
                    (success, error) = _waClient.Send(phone, text);
                    if (success) Database.MarkWhatsAppSent(queueId);
                    else Database.MarkWhatsAppAttemptFailed(queueId, error ?? "Unknown error");
                } catch (Exception ex) { success = false; error = ex.Message; }

                try {
                    BeginInvoke(() => {
                        UseWaitCursor = false;
                        Info(success
                            ? "WhatsApp message bhej diya gaya."
                            : $"Message send nahi ho saka.\n\nWajah: {error}\n\nMessage queue mein mehfooz hai — WhatsApp connect hone par khud bhej diya jayega.");
                        LoadCustomers(txtSearch.Text);
                    });
                } catch { }
            });
            return;
        }

        LoadCustomers(txtSearch.Text);
    }

    private static string BuildReadyMessage(Order? order) {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", "whatsapp-message.txt");
        var message = File.Exists(path)
            ? File.ReadAllText(path).TrimEnd()
            : "Your suits are ready please collect krlo.";

        if (order != null) {
            var baaqaya = CustomerForm.ComputePosBaaqaya(order.ForSection("Point Of Sale"));
            if (baaqaya > 0) message += $"\nAapke bill mein Rs {baaqaya:N0} baaqaya hai.";
        }

        return message;
    }

    private void BtnRelinkWhatsApp_Click(object? s, EventArgs e) {
        if (MessageBox.Show("WhatsApp session reset ho jayegi aur dobara QR code scan karna hoga. Continue?",
            "Golden Tailor", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        _qrDismissed = false;
        Task.Run(WhatsAppProcess.ResetSession);
        Info("WhatsApp session reset ho gayi. Naya QR code kuch second mein show hoga.");
    }

    private void StartWhatsAppTimers() {
        _healthTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _healthTimer.Tick += (_, _) => PollWhatsAppHealth();
        _healthTimer.Start();

        _retryTimer = new System.Windows.Forms.Timer { Interval = 60000 };
        _retryTimer.Tick += (_, _) => RetryPendingWhatsAppMessages();
        _retryTimer.Start();
    }

    private void PollWhatsAppHealth() {
        if (_healthBusy) return;
        _healthBusy = true;
        Task.Run(() => {
            var health = _waClient.GetHealth();
            try {
                BeginInvoke(() => { ApplyWhatsAppHealth(health); _healthBusy = false; });
            } catch { _healthBusy = false; }
        });
    }

    private void ApplyWhatsAppHealth(WhatsAppHealth health) {
        if (health.Status == "qr" && !string.IsNullOrEmpty(health.Qr)) {
            if (_qrDismissed) return;
            if (_qrForm is not { IsDisposed: false }) {
                _qrForm = new WhatsAppQrForm();
                _qrForm.FormClosing += (_, e) => {
                    if (e.CloseReason == CloseReason.UserClosing) _qrDismissed = true;
                };
                _qrForm.Show(this);
            }
            try {
                var raw   = health.Qr!.Contains(',') ? health.Qr.Split(',')[1] : health.Qr;
                var bytes = Convert.FromBase64String(raw);
                using var ms = new MemoryStream(bytes);
                _qrForm.SetQr(Image.FromStream(ms));
            } catch { }
        } else {
            _qrDismissed = false;
            if (_qrForm is { IsDisposed: false }) {
                _qrForm.Close();
                _qrForm = null;
            }
        }
    }

    private void RetryPendingWhatsAppMessages() {
        if (_retryBusy) return;
        _retryBusy = true;
        Task.Run(() => {
            try { RetryPendingWhatsAppMessagesCore(); }
            catch { }
            finally { _retryBusy = false; }
        });
    }

    private void RetryPendingWhatsAppMessagesCore() {
        var health = _waClient.GetHealth();
        if (health.Status != "ready") return;

        foreach (var msg in Database.GetPendingWhatsAppMessages()) {
            var (success, error) = _waClient.Send(msg.Phone, msg.Message);
            if (success) Database.MarkWhatsAppSent(msg.Id);
            else Database.MarkWhatsAppAttemptFailed(msg.Id, error ?? "Unknown error");
        }
    }

    private void BtnDelete_Click(object? s, EventArgs e) {
        var c = SelectedCustomer();
        if (c == null) { Info("Pehle ek customer select karein."); return; }
        if (MessageBox.Show($"'{c.Name}' ko delete karna chahte hain?",
            "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
            Database.DeleteCustomer(c.Id);
            LoadCustomers(txtSearch.Text);
        }
    }

    private static void Info(string msg) =>
        MessageBox.Show(msg, "Golden Tailor", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void lblTitle_Click(object sender, EventArgs e) {

    }

    private void txtSearch_TextChanged(object sender, EventArgs e) {

    }
}
