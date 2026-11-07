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
    private Label _lblStockCard = null!;
    private Label _lblRemainingCard = null!;
    private Label _lblUnstitchedCard = null!;
    private TableLayoutPanel _toolbarRow = null!;
    private FlowLayoutPanel _cardsFlow = null!;
    private FlowLayoutPanel _actionsFlow = null!;

    public MainForm() {
        InitializeComponent();
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

    // ── Header: [app logo | centered title | Koder Bench brand] ─────────────────
    private void BuildHeaderLayout() {
        panelHeader.MinimumSize = new Size(0, panelHeader.Height);
        panelHeader.AutoSize = true;
        panelHeader.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var left = BuildAppLogo();
        var title = BuildAppTitle();
        var brand = BuildHeaderBrand();

        var header = new TableLayoutPanel {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(Px(14), Px(4), Px(14), Px(4))
        };
        // Both side columns get the same width so the title stays centered on the
        // window, not just within whatever space is left between logo and brand.
        // (A Zoom PictureBox reports its image's native size as "preferred", so only
        // ask AutoSize controls for that — fixed-size ones just use their Width.)
        static int SlotWidth(Control c) => c.AutoSize ? c.GetPreferredSize(Size.Empty).Width : c.Width;
        var side = Math.Max(SlotWidth(left), SlotWidth(brand));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, side));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, side));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.Controls.Add(left, 0, 0);
        header.Controls.Add(title, 1, 0);
        header.Controls.Add(brand, 2, 0);
        header.SizeChanged += (_, _) =>
            FitTitleFont(title, header.ClientSize.Width - header.Padding.Horizontal - 2 * side);

        panelHeader.Controls.Remove(lblTitle);
        panelHeader.Controls.Remove(lblSubtitle);
        panelHeader.Controls.Add(header);
    }

    private Control BuildAppLogo() {
        var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "NationalTailor_Logo.png");
        if (File.Exists(logoPath)) {
            try {
                using var fs = new FileStream(logoPath, FileMode.Open, FileAccess.Read);
                var logo = Image.FromStream(fs);
                return new PictureBox {
                    Image = new Bitmap(logo),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent,
                    Size = new Size(Px(260), Px(72)),
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

    private const float TitleMaxFontSize = 26f, TitleMinFontSize = 12f;

    private Label BuildAppTitle() => new Label {
        Text = "Tailor Shop",
        Font = new Font("Segoe UI", TitleMaxFontSize, FontStyle.Bold),
        ForeColor = Theme.DarkGold,
        BackColor = Color.Transparent,
        TextAlign = ContentAlignment.MiddleCenter,
        AutoSize = false,
        Dock = DockStyle.Fill,
        Margin = new Padding(0),
        MinimumSize = new Size(0, Px(62))
    };

    // On a narrow window the space between logo and brand shrinks, so step the
    // font down until the whole title fits instead of cutting it to "Tail...".
    private static void FitTitleFont(Label title, int available) {
        if (available <= 0) return;
        var size = TitleMaxFontSize;
        while (size > TitleMinFontSize) {
            using var probe = new Font("Segoe UI", size, FontStyle.Bold);
            if (TextRenderer.MeasureText(title.Text, probe).Width <= available) break;
            size -= 1f;
        }
        if (Math.Abs(title.Font.Size - size) < 0.1f) return;
        var old = title.Font;
        title.Font = new Font("Segoe UI", size, FontStyle.Bold);
        old.Dispose();
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

    // ── Toolbar: [summary cards ......... action buttons] / divider / search row ──
    private void BuildToolbarLayout() {
        var search = new FlowLayoutPanel {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            BackColor = Color.Transparent,
            Padding = new Padding(Px(14), Px(8), Px(14), Px(8))
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
            Padding = new Padding(Px(14), Px(8), Px(14), Px(8))
        };
        _toolbarRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _toolbarRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _toolbarRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _toolbarRow.Controls.Add(_cardsFlow, 0, 0);
        _toolbarRow.Controls.Add(_actionsFlow, 1, 0);

        dividerSearch.Dock = DockStyle.Top;
        dividerSearch.Height = Px(3);

        // Docked controls stack in reverse add order: the last one added sits on top.
        panelSearch.Controls.Clear();
        panelSearch.Controls.Add(search);
        panelSearch.Controls.Add(dividerSearch);
        panelSearch.Controls.Add(_toolbarRow);

        // Panel.AutoSize doesn't track wrapping children reliably, so size it to
        // exactly the stacked rows whenever one of them grows or shrinks.
        void FitHeight() => panelSearch.Height = _toolbarRow.Height + dividerSearch.Height + search.Height;
        _toolbarRow.SizeChanged += (_, _) => FitHeight();
        search.SizeChanged += (_, _) => FitHeight();
        FitHeight();
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
            Text = text,
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
        // The designer's chat icon is a fixed 16px bitmap; draw it at this DPI like the others.
        btnRelinkWhatsApp.Image = Theme.CreateChatBubbleIcon(btnRelinkWhatsApp.ForeColor, Px(16));

        // Right-to-left flow: the first one added ends up right-most.
        _actionsFlow.Controls.Add(btnRelinkWhatsApp);
        _actionsFlow.Controls.Add(MakeToolbarButton("  Backup && Restore", Theme.Glyph.Backup, BtnBackup_Click));
        _actionsFlow.Controls.Add(MakeToolbarButton("  Stock", Theme.Glyph.Stock,
            (_, _) => { using var f = new StockForm(); f.ShowDialog(this); LoadCustomers(txtSearch.Text); }));
        _actionsFlow.Controls.Add(MakeToolbarButton("  Sale History", Theme.Glyph.History,
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
        var stockItems = Database.GetStockItems();
        var totalStockQty = stockItems.Sum(s => s.Quantity);
        _lblStockCard.Text = $"Stock Items: {stockItems.Count}   |   Qty: {totalStockQty}";
        _lblRemainingCard.Text = $"Remaining Total: Rs {totalRemaining:N0}";
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
        var style = e.CellStyle!;

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
        OrderStatus.Ready => "Ready ✓",
        OrderStatus.Delivered => "Delivered",
        _ => "Mark Ready"
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
            var idx = grid.Rows.Add(r.Customer.Id,
                          string.IsNullOrWhiteSpace(r.Customer.SerialNumber) ? "-" : r.Customer.SerialNumber,
                          r.Customer.Name, r.Customer.Phone ?? "-",
                          r.Customer.CreatedAt?.Split(' ')[0] ?? "", r.Customer.LatestOrderStatus ?? "-",
                          r.Baaqaya > 0 ? r.Baaqaya.ToString("N0") : "-",
                          MarkReadyButtonText(r.Customer.LatestOrderStatus));

            if (r.Baaqaya > 0) {
                var daysOld = (DateTime.Now.Date - r.OrderDate.Date).Days;
                if (daysOld >= 30)
                    SetRowAlertColor(grid.Rows[idx], Theme.AlertRed);
                else if (daysOld >= 20)
                    SetRowAlertColor(grid.Rows[idx], Theme.AlertOrange);
            }
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
        var full = Database.GetById(c.Id)!;
        var order = Database.GetLatestOrder(c.Id);
        using var frm = new CustomerForm(full, order);
        if (frm.ShowDialog() == DialogResult.OK) LoadCustomers(txtSearch.Text);
    }

    private void Grid_CellContentClick(object? s, DataGridViewCellEventArgs e) {
        if (e.RowIndex < 0 || e.ColumnIndex != colMarkReady.Index) return;
        var id = Convert.ToInt64(grid.Rows[e.RowIndex].Cells["colRealId"].Value);
        var c = _customers.FirstOrDefault(x => x.Id == id);
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

        var order = Database.GetOrderById(c.LatestOrderId.Value);
        var defaultMessage = BuildReadyMessage(order);

        using var dlg = new WhatsAppSendForm(c.Name, c.Phone!, defaultMessage);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Message.Length > 0) {
            var phone = c.Phone!;
            var text = dlg.Message;
            var orderId = c.LatestOrderId.Value;
            var custId = c.Id;

            UseWaitCursor = true;
            Task.Run(() => {
                bool success; string? error = null;
                try {
                    var queueId = Database.EnqueueWhatsAppMessage(orderId, custId, phone, text);
                    (success, error) = _waClient.Send(phone, text);
                    if (success) Database.MarkWhatsAppSent(queueId);
                    else Database.MarkWhatsAppAttemptFailed(queueId, error ?? "Unknown error");
                }
                catch (Exception ex) { success = false; error = ex.Message; }

                try {
                    BeginInvoke(() => {
                        ClearWaitCursor();
                        Info(success
                            ? "WhatsApp message bhej diya gaya."
                            : $"Message send nahi ho saka.\n\nWajah: {error}\n\nMessage queue mein mehfooz hai — WhatsApp connect hone par khud bhej diya jayega.");
                        LoadCustomers(txtSearch.Text);
                        ClearWaitCursor();
                    });
                }
                catch {
                    try { BeginInvoke(ClearWaitCursor); } catch { }
                }
            });
            return;
        }

        LoadCustomers(txtSearch.Text);
    }

    private void ClearWaitCursor() {
        UseWaitCursor = false;
        Application.UseWaitCursor = false;
        grid.UseWaitCursor = false;
        grid.Cursor = Cursors.Default;
        Cursor = Cursors.Default;
        Cursor.Current = Cursors.Default;
    }

    private static string BuildReadyMessage(Order? order) {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", "whatsapp-message.txt");
        var message = File.Exists(path)
            ? File.ReadAllText(path).TrimEnd()
            : "Apka Suit Ready ha pick krlo.";

        if (order != null) {
            var baaqaya = CustomerForm.ComputePosBaaqaya(order.ForSection("Point Of Sale"));
            if (baaqaya > 0) message += $"\nAapke bill mein Rs {baaqaya:N0} baaqaya hai.";
        }

        return message;
    }

    private void BtnRelinkWhatsApp_Click(object? s, EventArgs e) {
        if (MessageBox.Show("WhatsApp session reset ho jayegi aur dobara QR code scan karna hoga. Continue?",
            "National Tailor", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

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
            }
            catch { _healthBusy = false; }
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
                var raw = health.Qr!.Contains(',') ? health.Qr.Split(',')[1] : health.Qr;
                var bytes = Convert.FromBase64String(raw);
                using var ms = new MemoryStream(bytes);
                _qrForm.SetQr(Image.FromStream(ms));
            }
            catch { }
        }
        else {
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
        MessageBox.Show(msg, "National Tailor", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void lblTitle_Click(object sender, EventArgs e) {

    }

    private void txtSearch_TextChanged(object sender, EventArgs e) {

    }

    private void btnAdd_Click_1(object sender, EventArgs e) {

    }
}
