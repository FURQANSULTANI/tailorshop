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

    public MainForm() {
        InitializeComponent();
        ApplyGridStyles();
        ApplyPolish();
        BuildBackupButton();
        WireEvents();
        Database.Initialize();
        LoadCustomers();
        Task.Run(Backup.RunIfDue);

        WhatsAppConfig.Load();
        _waClient = new WhatsAppClient(WhatsAppConfig.Port);
        Task.Run(WhatsAppProcess.Start);
        StartWhatsAppTimers();
        FormClosing += (_, _) => WhatsAppProcess.Stop();
    }

    private void ApplyPolish() {
        foreach (var btn in new[] { btnSearch, btnClear, btnAdd, btnEdit, btnDelete, btnRelinkWhatsApp })
            Theme.RoundCorners(btn, 6);

        foreach (var btn in new[] { btnSearch, btnClear, btnAdd, btnEdit })
            Theme.ApplyLightHover(btn);

        WrapWithBorder(txtSearch, Theme.DarkGold);
        BuildHeaderBrand();

        panelHeader.Controls.Add(Theme.AccentDivider(DockStyle.Bottom));
        panelBottom.Controls.Add(Theme.AccentDivider(DockStyle.Top));
    }

    private void BuildBackupButton() {
        var btnBackup = new Button {
            Text      = "Backup & Restore",
            BackColor = Theme.NormalGrey,
            ForeColor = Theme.TextOnNormal,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Size      = new Size(175, 33),
            Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            Location  = new Point(panelBottom.Width - 189, 12),
            Cursor    = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btnBackup.FlatAppearance.BorderSize  = 2;
        btnBackup.FlatAppearance.BorderColor = Theme.DarkGold;
        btnBackup.FlatAppearance.MouseOverBackColor = Theme.Hover(Theme.NormalGrey);
        Theme.RoundCorners(btnBackup, 6);
        btnBackup.Click += BtnBackup_Click;

        panelBottom.Controls.Add(btnBackup);
        btnBackup.BringToFront();
    }

    private void BtnBackup_Click(object? sender, EventArgs e) {
        using var form = new BackupForm();
        form.ShowDialog(this);
        if (form.DataRestored) {
            Database.Initialize();
            LoadCustomers();
        }
    }

    private void BuildHeaderBrand() {
        var brand = new Panel {
            Size      = new Size(190, 72),
            Location  = new Point(0, 4),
            BackColor = Color.Transparent
        };

        var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "TheKoderBench_Logo_White.png");
        if (File.Exists(logoPath)) {
            try {
                using var fs      = new FileStream(logoPath, FileMode.Open, FileAccess.Read);
                using var raw     = Image.FromStream(fs);
                using var trimmed = Theme.TrimUniformMargins(raw);
                brand.Controls.Add(new PictureBox {
                    Image     = Theme.ToWhiteSilhouette(trimmed),
                    SizeMode  = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent,
                    Location  = new Point(0, 0),
                    Size      = new Size(190, 34)
                });
            } catch { }
        }

        brand.Controls.Add(new Label {
            Text      = "The Koder Bench",
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Location  = new Point(0, 36),
            Size      = new Size(190, 20)
        });

        brand.Controls.Add(new Label {
            Text      = "BUILD  ·  TRUST  ·  SOLVE",
            Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Location  = new Point(0, 56),
            Size      = new Size(190, 14)
        });

        panelHeader.Controls.Add(brand);
        brand.BringToFront();

        void PositionBrand() => brand.Left = panelHeader.ClientSize.Width - brand.Width - 20;
        PositionBrand();
        panelHeader.Resize += (_, _) => PositionBrand();
    }

    private static void WrapWithBorder(TextBox box, Color color, int thickness = 1) {
        var parent = box.Parent;
        if (parent == null) return;

        var origin = box.Location;
        var width  = box.Width;

        box.BorderStyle = BorderStyle.None;
        var inner = box.PreferredHeight;

        var holder = new Panel {
            BackColor = color,
            Location  = origin,
            Size      = new Size(width + thickness * 2, inner + thickness * 2 + 6)
        };

        parent.Controls.Remove(box);
        box.Location = new Point(thickness, thickness + 3);
        box.Width    = width;
        holder.Controls.Add(box);
        parent.Controls.Add(holder);
    }

    private void ApplyGridStyles() {
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.DarkGrey;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextOnDark;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.DarkGrey;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.TextOnDark;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
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
            var idx = grid.Rows.Add(r.Customer.Id, r.Customer.Name, r.Customer.Phone ?? "-",
                          r.Customer.CreatedAt?.Split(' ')[0] ?? "", r.Customer.LatestOrderStatus ?? "-",
                          r.Baaqaya > 0 ? r.Baaqaya.ToString("N0") : "-",
                          MarkReadyButtonText(r.Customer.LatestOrderStatus));

            if (r.Baaqaya > 0) {
                var daysOld = (DateTime.Now.Date - r.OrderDate.Date).Days;
                if (daysOld >= 2) 
                    SetRowAlertColor(grid.Rows[idx], Theme.AlertRed);
                else if (daysOld >= 1) 
                    SetRowAlertColor(grid.Rows[idx], Theme.AlertOrange);
            }
        }

        lblStatus.Text = $"{_customers.Count} customer(s)";
    }

    private static void SetRowAlertColor(DataGridViewRow row, Color color) {
        row.DefaultCellStyle.BackColor = color;
        row.DefaultCellStyle.ForeColor = Theme.TextOnAlert;
        row.DefaultCellStyle.SelectionBackColor = Theme.Hover(color);
        row.DefaultCellStyle.SelectionForeColor = Theme.TextOnAlert;
    }

    private Customer? SelectedCustomer() {
        if (grid.CurrentRow == null) return null;
        var id = Convert.ToInt64(grid.CurrentRow.Cells["colId"].Value);
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
        var id = Convert.ToInt64(grid.Rows[e.RowIndex].Cells["colId"].Value);
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
            "TailorShop", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

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
        MessageBox.Show(msg, "TailorShop", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void lblTitle_Click(object sender, EventArgs e) {

    }

    private void txtSearch_TextChanged(object sender, EventArgs e) {

    }
}
