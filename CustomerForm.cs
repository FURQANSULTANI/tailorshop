using System.Drawing.Printing;
using System.Text.RegularExpressions;

namespace TailorShop;

public partial class CustomerForm : Form {
    private readonly Customer _customer;
    private Order _order;
    private readonly bool _isNew;
    private readonly Dictionary<string, FlowLayoutPanel> _sectionPanels = new();

    // Counts stock rows as they are built so each restores its own saved sale.
    private int _stockRowsBuilt;

    // Print state
    private string _printSection = "";
    private List<(string Field, string Value)> _printRows = new();

    public CustomerForm(Customer? customer, Order? order = null) {
        _isNew = customer == null;
        _customer = customer ?? new Customer();
        _order = order ?? new Order { CustomerId = _customer.Id };
        InitializeComponent();

        lblHeader.Text = _isNew ? "✂  New Customer" : $"✂  Edit: {_customer.Name}";
        Text = _isNew ? "New Customer — National Tailor" : $"Edit: {_customer.Name}";

        foreach (var section in Database.DefaultFields.Keys)
            tabControl.TabPages.Add(BuildMeasurementTab(section));

        txtSerial.Text = _customer.SerialNumber ?? "";
        txtName.Text = _customer.Name;
        txtPhone.Text = _customer.Phone ?? "";
        txtAddress.Text = _customer.Address ?? "";
        txtNotes.Text = _customer.Notes ?? "";

        btnSave.Click += BtnSave_Click;
        tabControl.DrawItem += TabControl_DrawItem;
        AcceptButton = btnSave;

        Theme.RoundCorners(btnSave, 6);
        Theme.RoundCorners(btnCancel, 6);
        Theme.ApplyLightHover(btnCancel);
        Theme.SetIcon(btnSave, Theme.Glyph.Save);
        Theme.SetIcon(btnCancel, Theme.Glyph.Cancel);
        Theme.PaintFieldBorders(tabInfo, txtSerial, txtName, txtPhone, txtAddress, txtNotes);
        panelHeader.Controls.Add(Theme.AccentDivider(DockStyle.Bottom));
        panelBottom.Controls.Add(Theme.AccentDivider(DockStyle.Top));
        Shown += (_, _) => Theme.CenterButtons(this);
    }

    // ── Tab Strip (owner-drawn so it follows the theme, not the OS default) ────

    private void TabControl_DrawItem(object? sender, DrawItemEventArgs e) {
        var tab = tabControl.TabPages[e.Index];
        var selected = e.Index == tabControl.SelectedIndex;
        var back = selected ? Theme.DarkGrey : Theme.NormalGrey;
        var fore = selected ? Theme.TextOnDark : Theme.TextOnNormal;

        using var backBrush = new SolidBrush(back);
        e.Graphics.FillRectangle(backBrush, e.Bounds);
        TextRenderer.DrawText(e.Graphics, tab.Text, tabControl.Font, e.Bounds, fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    // ── Measurement Tab ───────────────────────────────────────────────────────

    private TabPage BuildMeasurementTab(string section) {
        bool isPos = section == "Point Of Sale";
        var tab = new TabPage(section) { BackColor = Theme.NormalGrey };

        // Scrollable field area
        var scroll = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Theme.NormalGrey,
            Padding = new Padding(0, 10, 0, 10)
        };
        _sectionPanels[section] = scroll;
        tab.Controls.Add(scroll);

        Action? posRelayout = null;

        // Bottom toolbar panel
        var toolbar = new Panel {
            Dock = DockStyle.Bottom,
            Height = 44,
            BackColor = Theme.DarkGrey
        };

        var btnAddField = new Button {
            Text = "Add Field",
            BackColor = Theme.NormalGrey,
            ForeColor = Theme.TextOnNormal,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Size = new Size(130, 32),
            Location = new Point(12, 6),
            Cursor = Cursors.Hand
        };
        btnAddField.FlatAppearance.BorderSize = 0;
        Theme.ApplyLightHover(btnAddField);
        Theme.SetIcon(btnAddField, Theme.Glyph.Add);
        btnAddField.Click += (_, _) => {
            if (isPos) {
                var newDef = new FieldDef { Name = "", ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true };
                AddFieldRow(section, newDef, "", Array.Empty<string>(), onChanged: () => RecalculatePos(section));
                RepositionPosSpecialRows(section);
                RecalculatePos(section);
                posRelayout?.Invoke();
            }
            else {
                AddFieldRow(section, new FieldDef { Name = "" }, "", Array.Empty<string>());
            }
        };
        Theme.RoundCorners(btnAddField, 6);

        var btnPrint = new Button {
            Text = "Print",
            BackColor = Theme.NormalGrey,
            ForeColor = Theme.TextOnNormal,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Size = new Size(90, 32),
            Location = new Point(150, 6),
            Cursor = Cursors.Hand
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        Theme.ApplyLightHover(btnPrint);
        Theme.SetIcon(btnPrint, Theme.Glyph.Print);
        btnPrint.Click += (_, _) => PrintSection(section);
        Theme.RoundCorners(btnPrint, 6);

        toolbar.Controls.Add(btnAddField);
        toolbar.Controls.Add(btnPrint);

        Button? btnNewSlip = null;
        if (isPos) {
            btnNewSlip = new Button {
                Text = "New Slip",
                BackColor = Theme.DarkGold,
                ForeColor = Theme.TextOnGold,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Size = new Size(110, 32),
                Location = new Point(250, 6),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnNewSlip.FlatAppearance.BorderSize = 0;
            btnNewSlip.FlatAppearance.MouseOverBackColor = Theme.Hover(Theme.DarkGold);
            Theme.SetIcon(btnNewSlip, Theme.Glyph.Add);
            btnNewSlip.Click += (_, _) => StartNewSlip();
            Theme.RoundCorners(btnNewSlip, 6);
            toolbar.Controls.Add(btnNewSlip);
        }

        tab.Controls.Add(toolbar);

        if (isPos)
            foreach (var past in Database.GetOrdersForCustomer(_customer.Id).Where(o => o.Id != _order.Id)) {
                var pastPos = past.ForSection(section);
                if (pastPos.Count == 0) continue;
                scroll.Controls.Add(BuildHistoryBar(section, past, pastPos));
            }

        // Load fields
        var existing = _order.ForSection(section);
        if (isPos) {
            var curLabel = _order.Id != 0 && DateTime.TryParse(_order.CreatedAt, out var curDt)
                ? $"{curDt:dd MMM yyyy}  (Current Slip)"
                : "Current Slip";
            var (curOuter, _, curInner, curRelayout) = BuildCollapsibleBar(scroll, curLabel, startExpanded: true);
            scroll.Controls.Add(curOuter);
            _sectionPanels[section] = curInner;
            posRelayout = curRelayout;

            BuildPosFields(curInner, section, existing, () => { RecalculatePos(section); curRelayout(); }, Theme.DarkGrey);
            curRelayout();

            var savedPay = existing.FirstOrDefault(m => m.FieldName == CustomerPayFieldName)?.Value;
            btnNewSlip!.Visible = !_isNew && _order.Id != 0 && !string.IsNullOrWhiteSpace(savedPay);
        }
        else if (existing.Count > 0) {
            var defsByName = Database.DefaultFields.TryGetValue(section, out var defs)
                ? defs.ToDictionary(d => d.Name)
                : new Dictionary<string, FieldDef>();
            foreach (var m in existing) {
                var def = defsByName.TryGetValue(m.FieldName, out var d) ? d : new FieldDef { Name = m.FieldName };
                var selected = m.SelectedOptions?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                AddFieldRow(section, def, m.Value ?? "", selected, scrollIntoView: false, quantity: m.Quantity);
            }
        }
        else {
            foreach (var f in Database.DefaultFields[section])
                AddFieldRow(section, f, "", Array.Empty<string>(), scrollIntoView: false);
        }

        return tab;
    }

    // ── Field Row ─────────────────────────────────────────────────────────────

    private Panel AddFieldRow(string section, FieldDef def, string value, string[] selectedOptions,
        bool scrollIntoView = true, Action? onChanged = null, string? quantity = null,
        bool removable = true, object? rowTag = null) =>
        AddFieldRowCore(_sectionPanels[section], def, value, selectedOptions, scrollIntoView, onChanged, quantity, removable, rowTag);

    private Panel AddFieldRowCore(FlowLayoutPanel panel, FieldDef def, string value, string[] selectedOptions,
        bool scrollIntoView = true, Action? onChanged = null, string? quantity = null,
        bool removable = true, object? rowTag = null, Color? accent = null) {
        bool showValue = def.Kind is FieldKind.Text or FieldKind.Both;
        bool showCheckboxes = def.Kind is FieldKind.Checkbox or FieldKind.Both;

        var row = new Panel { Height = 58, Margin = new Padding(0, 0, 0, 6), BackColor = Theme.NormalGrey, Tag = rowTag };
        var bordered = new List<Control>();

        int cursorX = 12;

        var txtField = new TextBox {
            Text = def.Name,
            Tag = "field",
            AutoSize = false,
            Location = new Point(cursorX, RowFieldTop + (RowFieldHeight - 34) / 2),
            Width = 190,
            Height = 34,
            Font = new Font("Urdu Typesetting", 12f),
            ForeColor = Theme.TextOnNormal,
            RightToLeft = RightToLeft.Yes,
            TextAlign = HorizontalAlignment.Center,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.NormalGrey,
            PlaceholderText = "Field ka naam..."
        };
        row.Controls.Add(txtField);
        bordered.Add(txtField);
        cursorX += txtField.Width + 12;

        ComboBox? stockCombo = null;
        TextBox? qtyBox = null;
        var customStockName = selectedOptions.Length > 0 ? selectedOptions[0] : null;
        if (def.Name == SuitPurchaseFieldName) {
            stockCombo = new ComboBox {
                Tag = StockComboTag,
                Location = new Point(cursorX, RowFieldTop + 1),
                Size = new Size(200, RowFieldHeight - 2),
                Font = new Font("Segoe UI", 9.5f),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = RowFieldHeight - 8,
                BackColor = Theme.NormalGrey,
                ForeColor = Theme.TextOnNormal,
                DisplayMember = nameof(StockItem.Display)
            };

            stockCombo.DrawItem += (_, de) => {
                de.DrawBackground();
                if (de.Index < 0) return;

                var item = stockCombo.Items[de.Index];
                var text = item is StockItem si ? si.Display : item?.ToString() ?? "";
                var fore = (de.State & DrawItemState.Selected) == DrawItemState.Selected
                    ? Theme.TextOnWhite
                    : Theme.TextOnNormal;

                TextRenderer.DrawText(de.Graphics!, text, stockCombo.Font,
                    Rectangle.Inflate(de.Bounds, -4, 0), fore,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                de.DrawFocusRectangle();
            };

            // Rows for this field can repeat, so each one restores the sale at its own position.
            var priorSale = _order.Id == 0
                ? null
                : Database.GetStockSales()
                    .Where(x => x.OrderId == _order.Id)
                    .OrderBy(x => x.Id)
                    .Skip(_stockRowsBuilt)
                    .FirstOrDefault();
            _stockRowsBuilt++;

            stockCombo.Items.Add(StockNonePlaceholder);
            stockCombo.SelectedIndex = 0;
            foreach (var stockItem in Database.GetStockItems()) {
                var at = stockCombo.Items.Add(stockItem);
                if (priorSale?.StockItemId == stockItem.Id) stockCombo.SelectedIndex = at;
            }
            stockCombo.Items.Add(StockOtherOption);

            stockCombo.SelectedIndexChanged += (_, _) => {
                if (stockCombo.SelectedItem == null) return;
                if (ReferenceEquals(stockCombo.SelectedItem, StockOtherOption)) {
                    if (stockCombo.DropDownStyle == ComboBoxStyle.DropDown) return;
                    void Switch() {
                        stockCombo.DropDownStyle = ComboBoxStyle.DropDown;
                        stockCombo.Text = "";
                        stockCombo.Focus();
                    }
                    if (stockCombo.IsHandleCreated) stockCombo.BeginInvoke(Switch);
                    else Switch();
                } else {
                    stockCombo.DropDownStyle = ComboBoxStyle.DropDownList;
                }
            };

            var wasOther = priorSale == null && !string.IsNullOrEmpty(customStockName);
            if (wasOther) {
                stockCombo.DropDownStyle = ComboBoxStyle.DropDown;
                stockCombo.SelectedItem = StockOtherOption;
                stockCombo.Text = customStockName;
                stockCombo.HandleCreated += (_, _) => stockCombo.BeginInvoke(() => stockCombo.Text = customStockName);
            }

            row.Controls.Add(stockCombo);
            bordered.Add(stockCombo);
            cursorX += stockCombo.Width + 12;
        }

        if (def.HasQuantity) {
            var txtQty = new TextBox {
                Text = quantity ?? "",
                Tag = "qty",
                Location = new Point(cursorX, BandCentre - LatinInkCentre),
                Width = 50,
                Font = ValueFont,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.NormalGrey,
                ForeColor = Theme.TextOnNormal,
                TextAlign = HorizontalAlignment.Center,
                PlaceholderText = "Qty..."
            };
            txtQty.KeyPress += (_, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            if (onChanged != null) txtQty.TextChanged += (_, _) => onChanged();
            row.Controls.Add(txtQty);
            bordered.Add(txtQty);
            qtyBox = txtQty;
            cursorX += txtQty.Width + 4;

            var lblX = new Label {
                Text = "×",
                Location = new Point(cursorX, RowFieldTop + 10),
                AutoSize = true,
                ForeColor = Theme.TextOnNormal,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            row.Controls.Add(lblX);
            cursorX += lblX.Width + 4;
        }

        if (showValue) {
            var txtVal = new TextBox {
                Text = value,
                Tag = "value",
                Location = new Point(cursorX, BandCentre - LatinInkCentre),
                Width = 90,
                Font = ValueFont,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.NormalGrey,
                ForeColor = Theme.TextOnNormal,
                TextAlign = HorizontalAlignment.Center,
                PlaceholderText = def.ValuePlaceholder
            };
            if (def.Numeric) {
                txtVal.KeyPress += (_, e) => {
                    if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar)) return;
                    if (e.KeyChar == '.' && !txtVal.Text.Contains('.')) return;
                    e.Handled = true;
                };
            }
            if (onChanged != null) txtVal.TextChanged += (_, _) => onChanged();

            Label lblIn;
            if (def.ValueOptions.Length > 0) {
                const string otherOption = "Other";
                txtVal.Visible = false;
                row.Controls.Add(txtVal);

                var cmbVal = new ComboBox {
                    Location = new Point(cursorX, RowFieldTop + 1),
                    Size = new Size(110, RowFieldHeight - 2),
                    Font = ValueFont,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.NormalGrey,
                    ForeColor = Theme.TextOnNormal
                };
                cmbVal.Items.AddRange(def.ValueOptions);

                var isOther = !string.IsNullOrEmpty(value) && !def.ValueOptions.Contains(value);

                cmbVal.SelectedIndexChanged += (_, _) => {
                    if (cmbVal.SelectedItem == null) return;
                    if (Equals(cmbVal.SelectedItem, otherOption)) {
                        if (cmbVal.DropDownStyle == ComboBoxStyle.DropDown) return;
                        void Switch() {
                            cmbVal.DropDownStyle = ComboBoxStyle.DropDown;
                            cmbVal.Text = "";
                            cmbVal.Focus();
                        }
                        if (cmbVal.IsHandleCreated) cmbVal.BeginInvoke(Switch);
                        else Switch();
                    } else {
                        cmbVal.DropDownStyle = ComboBoxStyle.DropDownList;
                        txtVal.Text = cmbVal.SelectedItem.ToString() ?? "";
                    }
                };
                cmbVal.TextChanged += (_, _) => {
                    if (cmbVal.DropDownStyle != ComboBoxStyle.DropDown) return;
                    txtVal.Text = cmbVal.Text.Trim();
                };

                if (isOther) {
                    cmbVal.DropDownStyle = ComboBoxStyle.DropDown;
                    cmbVal.SelectedItem = otherOption;
                    cmbVal.Text = value;
                    cmbVal.HandleCreated += (_, _) => cmbVal.BeginInvoke(() => cmbVal.Text = value);
                } else {
                    cmbVal.SelectedItem = value.Length > 0 ? value : null;
                }

                row.Controls.Add(cmbVal);
                bordered.Add(cmbVal);
                cursorX += cmbVal.Width + 8;

                lblIn = new Label {
                    Text = def.UnitLabel,
                    Location = new Point(cursorX, RowFieldTop + 12),
                    AutoSize = true,
                    ForeColor = Theme.TextOnNormal,
                    Font = new Font("Segoe UI", 8.5f)
                };
                row.Controls.Add(lblIn);
            } else {
                row.Controls.Add(txtVal);
                bordered.Add(txtVal);
                cursorX += txtVal.Width + 4;

                lblIn = new Label {
                    Text = def.UnitLabel,
                    Location = new Point(cursorX, RowFieldTop + 12),
                    AutoSize = true,
                    ForeColor = Theme.TextOnNormal,
                    Font = new Font("Segoe UI", 8.5f)
                };
                row.Controls.Add(lblIn);
            }
            cursorX += lblIn.Width + 16;

            if (stockCombo != null) {
                var priceBox = txtVal;
                stockCombo.SelectedIndexChanged += (_, _) => {
                    if (stockCombo.SelectedItem is not StockItem picked) return;
                    priceBox.Text = picked.RetailPrice.ToString("0.##");
                    if (qtyBox != null && string.IsNullOrWhiteSpace(qtyBox.Text)) qtyBox.Text = "1";
                };
            }
        }

        if (showCheckboxes) {
            foreach (var opt in def.Options) {
                var cb = new CheckBox {
                    Text = opt,
                    Checked = selectedOptions.Contains(opt),
                    Appearance = Appearance.Button,
                    FlatStyle = FlatStyle.Flat,
                    AutoSize = false,
                    Height = 40,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = Theme.UrduFont,
                    Location = new Point(cursorX, RowFieldTop + (RowFieldHeight - 40) / 2)
                };
                cb.Width = Math.Max(95, TextRenderer.MeasureText(opt, Theme.UrduFont).Width + 52);
                cb.FlatAppearance.BorderSize = 0;
                cb.FlatAppearance.CheckedBackColor = Theme.DeleteAccent;
                cb.FlatAppearance.MouseOverBackColor = Theme.DarkGrey;
                void UpdateCbColors() {
                    cb.BackColor = cb.Checked ? Theme.DeleteAccent : Theme.NormalGrey;
                    cb.ForeColor = cb.Checked ? Theme.TextOnDeleteAccent : Theme.TextOnNormal;
                }
                UpdateCbColors();
                cb.CheckedChanged += (_, _) => UpdateCbColors();
                cb.MouseEnter += (_, _) => cb.ForeColor = Theme.TextOnDark;
                cb.MouseLeave += (_, _) => UpdateCbColors();
                row.Controls.Add(cb);
                bordered.Add(cb);
                cursorX += cb.Width + 10;
            }
        }

        if (removable) {
            var btnRemove = new Button {
                Location = new Point(cursorX, RowFieldTop + 4),
                Size = new Size(30, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.DeleteAccent,
                ForeColor = Theme.TextOnDeleteAccent,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnRemove.FlatAppearance.BorderSize = 0;
            btnRemove.FlatAppearance.MouseOverBackColor = Theme.AlertRed;
            Theme.SetIcon(btnRemove, Theme.Glyph.Close, 13);
            new ToolTip().SetToolTip(btnRemove, "Ye field hatayein");
            Theme.RoundCorners(btnRemove, 15);
            btnRemove.Click += (_, _) => {
                panel.Controls.Remove(row);
                row.Dispose();
                onChanged?.Invoke();
            };
            row.Controls.Add(btnRemove);
            cursorX += btnRemove.Width + 12;
        }

        if (def.Name == SuitPurchaseFieldName) {
            var btnDuplicate = new Button {
                Location = new Point(cursorX, RowFieldTop + 4),
                Size = new Size(30, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.DarkGold,
                ForeColor = Theme.TextOnGold,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnDuplicate.FlatAppearance.BorderSize = 0;
            btnDuplicate.FlatAppearance.MouseOverBackColor = Theme.Hover(Theme.DarkGold);
            Theme.SetIcon(btnDuplicate, Theme.Glyph.Add, 14);
            new ToolTip().SetToolTip(btnDuplicate, "Aur suit add karein");
            Theme.RoundCorners(btnDuplicate, 15);
            btnDuplicate.Click += (_, _) => {
                var copy = AddFieldRowCore(panel, def, "", Array.Empty<string>(),
                    scrollIntoView: true, onChanged: onChanged, accent: accent);
                panel.Controls.SetChildIndex(copy, panel.Controls.GetChildIndex(row) + 1);
                RepositionPosSpecialRowsCore(panel);
                onChanged?.Invoke();
            };
            row.Controls.Add(btnDuplicate);
            cursorX += btnDuplicate.Width + 12;
        }

        Theme.PaintFieldBand(row, RowFieldTop, RowFieldHeight, bordered.ToArray());

        void FitWidth() => row.Width = Math.Max(Math.Max(panel.ClientSize.Width - 4, 480), cursorX + 8);
        FitWidth();
        panel.Resize += (_, _) => FitWidth();
        panel.Controls.Add(row);
        if (scrollIntoView) panel.ScrollControlIntoView(row);
        return row;
    }

    // ── Point Of Sale ────────────────────────────────────────────────────────

    public const string TotalRowTag = "TotalRow";
    public const string CustomerPayRowTag = "CustomerPayRow";
    public const string BaaqayaRowTag = "BaaqayaRow";
    public const string AdvanceFieldName = "ایڈوانس";
    public const string DiscountFieldName = "ڈسکاؤنٹ";
    public const string CustomerPayFieldName = "ادا شدہ رقم";
    public const string RemainingFieldName = "سابقہ رقم";
    public const string SuitStitchingFieldName = "سوٹ سلائی";
    public const string SuitPurchaseFieldName = "سوٹ خریداری";
    public const string StockComboTag = "stock";
    public const string StockNonePlaceholder = "-- No stock item --";
    public const string StockOtherOption = "Other...";
    private static readonly Font ValueFont = new("Segoe UI", 12f);
    private const int UrduInkCentre = 13;
    private const int LatinInkCentre = 11;
    private static int BandCentre => RowFieldTop + (RowFieldHeight - 1) / 2;
    private const int RowFieldHeight = 42;
    private const int RowFieldTop = 10;

    private static Panel BuildComputedRow(FlowLayoutPanel panel, string label, object rowTag, Color accent) {
        var row = new Panel { Height = 58, Margin = new Padding(0, 6, 0, 0), BackColor = accent, Tag = rowTag };

        int cursorX = 12;

        var lblName = new TextBox {
            Text = label,
            Tag = "field",
            ReadOnly = true,
            TabStop = false,
            Location = new Point(cursorX, 10),
            Width = 190,
            Font = Theme.UrduFont,
            ForeColor = Theme.TextOnDark,
            RightToLeft = RightToLeft.Yes,
            TextAlign = HorizontalAlignment.Right,
            BorderStyle = BorderStyle.None,
            BackColor = accent
        };
        row.Controls.Add(lblName);
        cursorX += lblName.Width + 12;

        var txtValue = new TextBox {
            Text = "0",
            Tag = "value",
            ReadOnly = true,
            TabStop = false,
            Location = new Point(cursorX, 10),
            Width = 140,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Theme.NormalGrey,
            ForeColor = Theme.TextOnNormal,
            TextAlign = HorizontalAlignment.Center
        };
        row.Controls.Add(txtValue);
        cursorX += txtValue.Width + 8;

        var lblRs = new Label {
            Text = "Rs",
            Location = new Point(cursorX, 16),
            AutoSize = true,
            ForeColor = Theme.TextOnDark,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        row.Controls.Add(lblRs);
        Theme.PaintFieldBorders(row, txtValue);

        void FitWidth() => row.Width = Math.Max(panel.ClientSize.Width, 480);
        FitWidth();
        panel.Resize += (_, _) => FitWidth();

        return row;
    }

    private Panel BuildTotalRow(string section) =>
        BuildComputedRow(_sectionPanels[section], "ٹوٹل بل", TotalRowTag, Theme.DarkGrey);

    private Panel BuildBaaqayaRow(string section) =>
        BuildComputedRow(_sectionPanels[section], "باقی", BaaqayaRowTag, Theme.DarkGrey);

    private void RepositionPosSpecialRows(string section) => RepositionPosSpecialRowsCore(_sectionPanels[section]);

    private void RepositionPosSpecialRowsCore(FlowLayoutPanel panel) {
        Control? total = null, customerPay = null, baaqaya = null;
        foreach (Control c in panel.Controls) {
            if (Equals(c.Tag, TotalRowTag)) total = c;
            else if (Equals(c.Tag, CustomerPayRowTag)) customerPay = c;
            else if (Equals(c.Tag, BaaqayaRowTag)) baaqaya = c;
        }
        var last = panel.Controls.Count - 1;
        if (total != null) panel.Controls.SetChildIndex(total, last);
        if (customerPay != null) panel.Controls.SetChildIndex(customerPay, last);
        if (baaqaya != null) panel.Controls.SetChildIndex(baaqaya, last);
    }

    private void RecalculatePos(string section) => RecalculatePosCore(_sectionPanels[section]);

    private void RecalculatePosCore(FlowLayoutPanel panel) {
        TextBox? totalBox = null, baaqayaBox = null, customerPayBox = null;
        decimal total = 0;

        foreach (Control c in panel.Controls) {
            if (c is not Panel rowPanel) continue;
            TextBox? tField = null, tVal = null, tQty = null;
            foreach (Control cc in rowPanel.Controls) {
                if (cc is not TextBox tb) continue;
                switch (tb.Tag as string) {
                    case "field": tField = tb; break;
                    case "value": tVal = tb; break;
                    case "qty": tQty = tb; break;
                }
            }

            if (Equals(rowPanel.Tag, TotalRowTag)) { totalBox = tVal; continue; }
            if (Equals(rowPanel.Tag, BaaqayaRowTag)) { baaqayaBox = tVal; continue; }
            if (Equals(rowPanel.Tag, CustomerPayRowTag)) { customerPayBox = tVal; continue; }

            decimal.TryParse(tVal?.Text.Trim(), out var amount);
            if (tQty != null) {
                decimal.TryParse(tQty.Text.Trim(), out var qty);
                amount *= qty == 0 && string.IsNullOrEmpty(tQty.Text.Trim()) ? 1 : qty;
            }
            var fieldName = tField?.Text.Trim();
            total += fieldName == AdvanceFieldName || fieldName == DiscountFieldName ? -amount : amount;
        }

        if (totalBox != null) totalBox.Text = total.ToString("N0");

        decimal.TryParse(customerPayBox?.Text.Trim(), out var paid);
        if (baaqayaBox != null) baaqayaBox.Text = (total - paid).ToString("N0");
    }

    private void BuildPosFields(FlowLayoutPanel panel, string section, List<Measurement> existing, Action onChanged, Color accent) {
        _stockRowsBuilt = 0;
        if (existing.Count > 0) {
            var defsByName = Database.DefaultFields.TryGetValue(section, out var defs)
                ? defs.ToDictionary(d => d.Name)
                : new Dictionary<string, FieldDef>();
            foreach (var m in existing) {
                if (m.FieldName == CustomerPayFieldName) continue;
                var def = defsByName.TryGetValue(m.FieldName, out var d) ? d : new FieldDef { Name = m.FieldName };
                var selected = m.SelectedOptions?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                AddFieldRowCore(panel, def, m.Value ?? "", selected, scrollIntoView: false, onChanged: onChanged, quantity: m.Quantity, accent: accent);
            }

            if (defsByName.TryGetValue(DiscountFieldName, out var discountDef)
                && !existing.Any(m => m.FieldName == DiscountFieldName)) {
                AddFieldRowCore(panel, discountDef, "", Array.Empty<string>(),
                    scrollIntoView: false, onChanged: onChanged, accent: accent);
            }
        }
        else {
            foreach (var f in Database.DefaultFields[section])
                AddFieldRowCore(panel, f, "", Array.Empty<string>(), scrollIntoView: false, onChanged: onChanged, accent: accent);
        }

        panel.Controls.Add(BuildComputedRow(panel, "ٹوٹل بل", TotalRowTag, accent));

        var customerPayValue = existing.FirstOrDefault(m => m.FieldName == CustomerPayFieldName)?.Value ?? "";
        var customerPayDef = new FieldDef { Name = CustomerPayFieldName, ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true };
        AddFieldRowCore(panel, customerPayDef, customerPayValue, Array.Empty<string>(),
            scrollIntoView: false, onChanged: onChanged, removable: false, rowTag: CustomerPayRowTag);

        panel.Controls.Add(BuildComputedRow(panel, "باقی", BaaqayaRowTag, accent));
        RecalculatePosCore(panel);
    }

    private void SaveHistoryBar(Order pastOrder, FlowLayoutPanel barPanel, string section) {
        var updated = new List<Measurement>();
        foreach (Control row in barPanel.Controls) {
            if (row is not Panel rowPanel) continue;
            if (Equals(rowPanel.Tag, TotalRowTag) || Equals(rowPanel.Tag, BaaqayaRowTag)) continue;
            TextBox? tField = null, tVal = null, tQty = null;
            var checkedOptions = new List<string>();
            foreach (Control c in rowPanel.Controls) {
                if (c is TextBox tb) {
                    switch (tb.Tag as string) {
                        case "field": tField = tb; break;
                        case "value": tVal = tb; break;
                        case "qty": tQty = tb; break;
                    }
                }
                else if (c is CheckBox { Checked: true } cb) checkedOptions.Add(cb.Text);
            }

            var fn = tField?.Text.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(fn)) continue;
            updated.Add(new Measurement {
                Section = section,
                FieldName = fn,
                Value = tVal?.Text.Trim().NullIfEmpty(),
                SelectedOptions = checkedOptions.Count > 0 ? string.Join(",", checkedOptions) : null,
                Quantity = tQty?.Text.Trim().NullIfEmpty()
            });
        }

        pastOrder.Measurements = pastOrder.Measurements.Where(m => m.Section != section).Concat(updated).ToList();
        Database.SaveOrder(pastOrder);
    }

    public static decimal ComputePosTotal(List<Measurement> posMeasurements) {
        decimal total = 0;
        foreach (var m in posMeasurements) {
            if (m.FieldName == CustomerPayFieldName) continue;
            decimal.TryParse(m.Value, out var amount);
            if (!string.IsNullOrEmpty(m.Quantity)) {
                decimal.TryParse(m.Quantity, out var qty);
                amount *= qty;
            }
            total += m.FieldName == AdvanceFieldName || m.FieldName == DiscountFieldName ? -amount : amount;
        }
        return total;
    }

    public static decimal ComputePosPaid(List<Measurement> posMeasurements) {
        decimal.TryParse(posMeasurements.FirstOrDefault(m => m.FieldName == CustomerPayFieldName)?.Value, out var paid);
        return paid;
    }

    public static decimal ComputePosBaaqaya(List<Measurement> posMeasurements) =>
        ComputePosTotal(posMeasurements) - ComputePosPaid(posMeasurements);

    private (Panel Outer, Panel Body, FlowLayoutPanel Inner, Action Relayout) BuildCollapsibleBar(
        FlowLayoutPanel outerScroll, string label, bool startExpanded, Color? headerColor = null) {
        var outer = new Panel { Height = 36, Margin = new Padding(0, 0, 0, 6), BackColor = Theme.NormalGrey };

        var header = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = headerColor ?? Theme.DarkGrey, Cursor = Cursors.Hand };
        var lblChevron = new Label {
            Text = startExpanded ? "▾" : "▸",
            ForeColor = Theme.TextOnDark,
            AutoSize = true,
            Location = new Point(12, 9),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        var lblLabel = new Label {
            Text = label,
            ForeColor = Theme.TextOnDark,
            AutoSize = true,
            Location = new Point(32, 8),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        header.Controls.Add(lblChevron);
        header.Controls.Add(lblLabel);

        var body = new Panel { Location = new Point(0, 36), BackColor = Theme.NormalGrey, Visible = startExpanded };

        var inner = new FlowLayoutPanel {
            Location = new Point(0, 0),
            Width = Math.Max(outerScroll.ClientSize.Width, 480),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Theme.NormalGrey,
            Padding = new Padding(0, 6, 0, 6)
        };
        body.Controls.Add(inner);

        void Relayout() {
            inner.PerformLayout();
            int innerBottom = 0;
            foreach (Control c in inner.Controls) innerBottom = Math.Max(innerBottom, c.Bottom);
            inner.Height = innerBottom + inner.Padding.Bottom;

            int bodyBottom = 0;
            foreach (Control c in body.Controls) bodyBottom = Math.Max(bodyBottom, c.Bottom);
            body.Height = bodyBottom + 8;
            body.Width = inner.Width;

            if (body.Visible) {
                outerScroll.SuspendLayout();
                outer.Height = header.Height + body.Height;
                outerScroll.ResumeLayout(true);
                outerScroll.PerformLayout();
            }
        }

        outer.Controls.Add(body);
        outer.Controls.Add(header);
        outer.Height = header.Height + (startExpanded ? body.Height : 0);

        header.Click += (_, _) => {
            outerScroll.SuspendLayout();
            body.Visible = !body.Visible;
            lblChevron.Text = body.Visible ? "▾" : "▸";
            outer.Height = header.Height + (body.Visible ? body.Height : 0);
            outerScroll.ResumeLayout(true);
            outerScroll.PerformLayout();
        };

        void FitWidth() {
            outer.Width = Math.Max(outerScroll.ClientSize.Width, 480);
            inner.Width = outer.Width;
            Relayout();
        }
        FitWidth();
        outerScroll.Resize += (_, _) => FitWidth();

        return (outer, body, inner, Relayout);
    }

    private Panel BuildHistoryBar(string section, Order pastOrder, List<Measurement> pastPos) {
        var panel = _sectionPanels[section];
        var dateText = DateTime.TryParse(pastOrder.CreatedAt, out var dt) ? dt.ToString("dd MMM yyyy") : "";
        var (outer, body, barScroll, relayout) = BuildCollapsibleBar(panel, dateText, startExpanded: false, headerColor: Theme.RowAlt);

        var barToolbar = new Panel { Height = 40, BackColor = Theme.RowAlt };
        var btnAddField = new Button {
            Text = "Add Field",
            BackColor = Theme.NormalGrey,
            ForeColor = Theme.TextOnNormal,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Size = new Size(120, 28),
            Location = new Point(10, 6),
            Cursor = Cursors.Hand
        };
        btnAddField.FlatAppearance.BorderSize = 0;
        Theme.ApplyLightHover(btnAddField);
        Theme.SetIcon(btnAddField, Theme.Glyph.Add);
        Theme.RoundCorners(btnAddField, 6);

        var btnUpdate = new Button {
            Text = "Update Slip",
            BackColor = Theme.NormalGrey,
            ForeColor = Theme.TextOnNormal,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Size = new Size(120, 28),
            Location = new Point(140, 6),
            Cursor = Cursors.Hand
        };
        btnUpdate.FlatAppearance.BorderSize = 0;
        Theme.ApplyLightHover(btnUpdate);
        Theme.SetIcon(btnUpdate, Theme.Glyph.Save);
        Theme.RoundCorners(btnUpdate, 6);

        barToolbar.Controls.Add(btnAddField);
        barToolbar.Controls.Add(btnUpdate);
        body.Controls.Add(barToolbar);

        void PositionToolbar() {
            barToolbar.Location = new Point(0, barScroll.Bottom + 4);
            barToolbar.Width = barScroll.Width;
        }

        void OnBarChanged() {
            RecalculatePosCore(barScroll);
            PositionToolbar();
            relayout();
        }

        BuildPosFields(barScroll, section, pastPos, OnBarChanged, Theme.RowAlt);
        PositionToolbar();
        relayout();
        panel.Resize += (_, _) => PositionToolbar();

        btnAddField.Click += (_, _) => {
            var newDef = new FieldDef { Name = "", ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true };
            AddFieldRowCore(barScroll, newDef, "", Array.Empty<string>(), onChanged: OnBarChanged, accent: Theme.RowAlt);
            RepositionPosSpecialRowsCore(barScroll);
            OnBarChanged();
        };
        btnUpdate.Click += (_, _) => {
            SaveHistoryBar(pastOrder, barScroll, section);
            MessageBox.Show("Purani slip update ho gayi.", "National Tailor",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        return outer;
    }

    private void StartNewSlip() {
        var previousBaaqaya = ComputePosBaaqaya(_order.ForSection("Point Of Sale"));
        var newOrder = new Order { CustomerId = _customer.Id, Status = OrderStatus.Pending };

        foreach (var m in _order.Measurements.Where(m => m.Section != "Point Of Sale"))
            newOrder.Measurements.Add(new Measurement {
                Section = m.Section,
                FieldName = m.FieldName,
                Value = m.Value,
                SelectedOptions = m.SelectedOptions,
                Quantity = m.Quantity
            });

        foreach (var f in Database.DefaultFields["Point Of Sale"])
            newOrder.Measurements.Add(new Measurement {
                Section = "Point Of Sale",
                FieldName = f.Name,
                Value = f.Name == RemainingFieldName ? previousBaaqaya.ToString() : null
            });

        _order = newOrder;
        RebuildTab("Point Of Sale");
    }

    private void RebuildTab(string section) {
        int index = -1;
        for (int i = 0; i < tabControl.TabPages.Count; i++)
            if (tabControl.TabPages[i].Text == section) { index = i; break; }
        if (index < 0) return;

        var oldTab = tabControl.TabPages[index];
        var newTab = BuildMeasurementTab(section);
        tabControl.TabPages.RemoveAt(index);
        oldTab.Dispose();
        tabControl.TabPages.Insert(index, newTab);
        tabControl.SelectedIndex = index;
    }

    // ── Print ─────────────────────────────────────────────────────────────────

    private void PrintSection(string section) {
        // Collect current field values from the UI (not just saved DB values)
        _printSection = section;
        _printRows = CollectRows(section);

        if (_printRows.Count == 0) {
            MessageBox.Show("Print karne ke liye koi measurement nahi hai.",
                "National Tailor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var printDoc = new PrintDocument();
        printDoc.DocumentName = $"National Tailor - {_customer.Name} - {section}";
        printDoc.PrintPage += PrintPage;
        ApplyReceiptPaper(printDoc);

        using var preview = new PrintPreviewDialog {
            Document = printDoc,
            WindowState = FormWindowState.Maximized,
            Text = $"Print Preview — {_customer.Name} — {section}"
        };
        preview.PrintPreviewControl.AutoZoom = false;
        preview.PrintPreviewControl.Zoom = 2.0;
        SelectPreviewZoom(preview, "200%");
        preview.ShowDialog(this);
    }

    private const int ReceiptWidthHundredths = 315;
    private const int ReceiptMaxHeightHundredths = 3900;

    private static void ApplyReceiptPaper(PrintDocument doc) {
        try {
            var existing = doc.PrinterSettings.PaperSizes
                .Cast<PaperSize>()
                .FirstOrDefault(p => Math.Abs(p.Width - ReceiptWidthHundredths) <= 12);

            doc.DefaultPageSettings.PaperSize = existing
                ?? new PaperSize("80mm Roll", ReceiptWidthHundredths, ReceiptMaxHeightHundredths);

            doc.DefaultPageSettings.Margins = new Margins(12, 12, 12, 12);
            doc.OriginAtMargins = false;
        }
        catch {
        }
    }

    private static void SelectPreviewZoom(PrintPreviewDialog preview, string label) {
        try {
            var strip = preview.Controls.OfType<ToolStrip>().FirstOrDefault();
            var split = strip?.Items.OfType<ToolStripSplitButton>().FirstOrDefault();
            if (split == null) return;

            foreach (var item in split.DropDownItems.OfType<ToolStripMenuItem>())
                item.Checked = item.Text == label;
        }
        catch {
        }
    }

    private List<(string, string)> CollectRows(string section) {
        var list = new List<(string, string)>();
        if (!_sectionPanels.TryGetValue(section, out var panel)) return list;

        foreach (Control row in panel.Controls) {
            if (row is not Panel rowPanel) continue;
            TextBox? tField = null, tVal = null, tQty = null;
            var checkedOptions = new List<string>();
            foreach (Control c in rowPanel.Controls) {
                if (c is TextBox tb) {
                    switch (tb.Tag as string) {
                        case "field": tField = tb; break;
                        case "value": tVal = tb; break;
                        case "qty": tQty = tb; break;
                    }
                }
                else if (c is CheckBox { Checked: true } cb) checkedOptions.Add(cb.Text);
            }

            var fn = tField?.Text.Trim() ?? "";
            var valParts = new List<string>();
            var v = tVal?.Text.Trim();
            if (!string.IsNullOrEmpty(v))
                valParts.Add(!string.IsNullOrEmpty(tQty?.Text.Trim()) ? $"{tQty.Text.Trim()} x {v}" : v);
            if (checkedOptions.Count > 0) valParts.Add(string.Join(" / ", checkedOptions));
            if (valParts.Count == 0) continue; // empty fields are skipped on the printed receipt
            list.Add((fn, string.Join("   ", valParts)));
        }
        return list;
    }

    private void PrintPage(object sender, PrintPageEventArgs e) {
        var g = e.Graphics!;

        float x = e.MarginBounds.Left;
        float pageW = e.MarginBounds.Width;
        float y = e.MarginBounds.Top;

        using var shopFont = new Font("Segoe UI", 13f, FontStyle.Bold);
        using var subFont = new Font("Segoe UI", 7f, FontStyle.Italic);
        using var darkBrush = new SolidBrush(Color.Black);
        using var grayBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0));

        using var centerFormat = new StringFormat { Alignment = StringAlignment.Center };
        using var labelFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        using var valueFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

        var logo = ShopLogo;
        if (logo != null) {
            const float logoHeight = 34f;
            var logoWidth = Math.Min(pageW, logo.Width * logoHeight / logo.Height);
            g.DrawImage(logo, x + (pageW - logoWidth) / 2f, y, logoWidth, logoHeight);
            y += logoHeight + 6;
        }
        else {
            g.DrawString("National Tailor", shopFont, darkBrush, new RectangleF(x, y, pageW, 22f), centerFormat);
            y += shopFont.GetHeight(g) + 1;
            g.DrawString("Professional Tailoring Services", subFont, darkBrush, new RectangleF(x, y, pageW, 14f), centerFormat);
            y += subFont.GetHeight(g) + 6;
        }

        using var rulePen = new Pen(Color.Black, 1f);
        g.DrawLine(rulePen, x, y, x + pageW, y);
        y += 6;

        using var infoFont = new Font("Segoe UI", 8f);
        if (!string.IsNullOrWhiteSpace(_customer.SerialNumber))
            DrawInfoRow(g, infoFont, darkBrush, grayBrush, x, pageW, ref y, "Serial", _customer.SerialNumber!);
        DrawInfoRow(g, infoFont, darkBrush, grayBrush, x, pageW, ref y, "Name", _customer.Name);
        DrawInfoRow(g, infoFont, darkBrush, grayBrush, x, pageW, ref y, "Phone", _customer.Phone ?? "-");
        DrawInfoRow(g, infoFont, darkBrush, grayBrush, x, pageW, ref y, "Date", DateTime.Now.ToString("dd-MM-yyyy"));

        y += 4;
        g.DrawLine(rulePen, x, y, x + pageW, y);
        y += 6;

        using var secFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        bool isPosSlip = _printSection == "Point Of Sale";
        g.DrawString(isPosSlip ? _printSection : $"{_printSection} Measurements",
            secFont, darkBrush, new RectangleF(x, y, pageW, 16f), centerFormat);
        y += secFont.GetHeight(g) + 5;

        using var dottedPen = new Pen(Color.FromArgb(150, 0, 0, 0), 1f) {
            DashStyle = System.Drawing.Drawing2D.DashStyle.Dot
        };
        using var latinFont = new Font("Segoe UI", 8.5f);

        Font FontFor(string text) =>
            text.Any(ch => ch >= 0x0600 && ch <= 0x06FF) ? Theme.UrduFontSmall : latinFont;

        const float rowH = 22f;
        const float fractionRowH = 30f;
        float valueW = pageW * 0.38f;
        float labelW = pageW - valueW;

        foreach (var (field, val) in _printRows) {
            var thisRowH = FractionPattern.IsMatch(val.Trim()) ? fractionRowH : rowH;

            g.DrawString(field, FontFor(field), darkBrush,
                new RectangleF(x, y, labelW, thisRowH), labelFormat);
            DrawValueWithFraction(g, val, FontFor(val), darkBrush,
                new RectangleF(x + labelW, y, valueW, thisRowH), valueFormat);
            y += thisRowH;
            g.DrawLine(dottedPen, x, y, x + pageW, y);
            y += 2;
        }

        y += 6;
        g.DrawLine(rulePen, x, y, x + pageW, y);
        y += 5;

        using var footerFont = new Font("Segoe UI", 7f);
        g.DrawString("Thank you for your trust!", footerFont, darkBrush,
            new RectangleF(x, y, pageW, 12f), centerFormat);
        y += footerFont.GetHeight(g) + 1;
        g.DrawString($"Printed: {DateTime.Now:dd-MM-yyyy hh:mm tt}", footerFont, grayBrush,
            new RectangleF(x, y, pageW, 12f), centerFormat);
        y += footerFont.GetHeight(g) + 6;

        DrawBrandingFooter(g, x, pageW, y, centerFormat);

        e.HasMorePages = false;
    }

    private static Image? _shopLogo;
    private static bool _shopLogoLoaded;

    private static Image? ShopLogo {
        get {
            if (_shopLogoLoaded) return _shopLogo;
            _shopLogoLoaded = true;
            try {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "NationalTailor_Logo.png");
                if (File.Exists(path)) {
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                    _shopLogo = new Bitmap(Image.FromStream(fs));
                }
            }
            catch { }
            return _shopLogo;
        }
    }

    private static readonly Regex FractionPattern =
        new(@"^(?<whole>\d+\s*[-\s])?(?<num>\d+)\s*/\s*(?<den>\d+)$", RegexOptions.Compiled);

    private static void DrawValueWithFraction(Graphics g, string val, Font font, Brush brush, RectangleF bounds, StringFormat format) {
        var match = FractionPattern.Match(val.Trim());
        if (!match.Success) {
            g.DrawString(val, font, brush, bounds, format);
            return;
        }

        var whole = match.Groups["whole"].Success ? match.Groups["whole"].Value.Trim(' ', '-') : "";
        var num = match.Groups["num"].Value;
        var den = match.Groups["den"].Value;

        using var fracFont = new Font(font.FontFamily, font.Size * 0.62f, font.Style);

        var numSize = g.MeasureString(num, fracFont);
        var denSize = g.MeasureString(den, fracFont);
        var barWidth = Math.Max(numSize.Width, denSize.Width) + 2f;
        var wholeSize = whole.Length > 0 ? g.MeasureString(whole + " ", font) : SizeF.Empty;

        var totalWidth = wholeSize.Width + barWidth;
        var startX = format.Alignment switch {
            StringAlignment.Center => bounds.X + (bounds.Width - totalWidth) / 2f,
            StringAlignment.Far => bounds.X + bounds.Width - totalWidth,
            _ => bounds.X
        };

        var midY = bounds.Y + bounds.Height / 2f;

        if (whole.Length > 0) {
            using var wholeFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            g.DrawString(whole, font, brush, new RectangleF(startX, bounds.Y, wholeSize.Width, bounds.Height), wholeFormat);
        }

        var fracX = startX + wholeSize.Width;
        using var fracCenterFormat = new StringFormat { Alignment = StringAlignment.Center };

        g.DrawString(num, fracFont, brush, new RectangleF(fracX, midY - numSize.Height - 1f, barWidth, numSize.Height), fracCenterFormat);
        using var barPen = new Pen(brush is SolidBrush sb ? sb.Color : Color.Black, 1f);
        g.DrawLine(barPen, fracX + 1f, midY, fracX + barWidth - 1f, midY);
        g.DrawString(den, fracFont, brush, new RectangleF(fracX, midY + 1f, barWidth, denSize.Height), fracCenterFormat);
    }

    private static void DrawBrandingFooter(Graphics g, float x, float width, float y, StringFormat format) {
        using var brandFont = new Font("Segoe UI", 6.5f, FontStyle.Bold);
        using var brandBrush = new SolidBrush(Color.Black);
        g.DrawString("For Business Solution Call 03043713001", brandFont, brandBrush,
            new RectangleF(x, y, width, 12f), format);
    }

    private static void DrawInfoRow(Graphics g, Font font,
        Brush dark, Brush gray, float x, float width, ref float y,
        string label, string value) {
        using var valueFormat = new StringFormat {
            Alignment = StringAlignment.Far,
            Trimming = StringTrimming.EllipsisCharacter
        };

        var isUrdu = value.Any(ch => ch >= 0x0600 && ch <= 0x06FF);
        var valueFont = isUrdu ? Theme.UrduFontSmall : font;
        var rowHeight = isUrdu ? 22f : 14f;

        g.DrawString($"{label}:", font, gray, x, y);
        g.DrawString(value, valueFont, dark, new RectangleF(x + 42f, y, width - 42f, rowHeight), valueFormat);
        y += isUrdu ? 21f : 13f;
    }

    // ── Save ─────────────────────────────────────────────────────────────────

    private void BtnSave_Click(object? s, EventArgs e) {
        if (string.IsNullOrWhiteSpace(txtName.Text)) {
            MessageBox.Show("Customer ka naam zaroor likhein!", "National Tailor",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var serial = txtSerial.Text.Trim();
        if (serial.Length > 0 && Database.SerialNumberExists(serial, _customer.Id)) {
            MessageBox.Show($"Serial number '{serial}' pehle se kisi aur customer ka hai." +
                Environment.NewLine + Environment.NewLine + "Koi aur serial number likhein.",
                "National Tailor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            tabControl.SelectedTab = tabInfo;
            txtSerial.Focus();
            txtSerial.SelectAll();
            return;
        }

        _customer.SerialNumber = serial.NullIfEmpty();
        _customer.Name = txtName.Text.Trim();
        _customer.Phone = txtPhone.Text.Trim().NullIfEmpty();
        _customer.Address = txtAddress.Text.Trim().NullIfEmpty();
        _customer.Notes = txtNotes.Text.Trim().NullIfEmpty();
        _order.Measurements.Clear();

        foreach (var (section, panel) in _sectionPanels) {
            foreach (Control row in panel.Controls) {
                if (row is not Panel rowPanel) continue;
                if (Equals(rowPanel.Tag, TotalRowTag) || Equals(rowPanel.Tag, BaaqayaRowTag)) continue;
                TextBox? tField = null, tVal = null, tQty = null;
                var checkedOptions = new List<string>();
                foreach (Control c in rowPanel.Controls) {
                    if (c is TextBox tb) {
                        switch (tb.Tag as string) {
                            case "field": tField = tb; break;
                            case "value": tVal = tb; break;
                            case "qty": tQty = tb; break;
                        }
                    }
                    else if (c is CheckBox { Checked: true } cb) checkedOptions.Add(cb.Text);
                    else if (c is ComboBox { Tag: string tag } scb && tag == StockComboTag
                             && scb.DropDownStyle == ComboBoxStyle.DropDown && scb.Text.Trim().Length > 0)
                        checkedOptions.Add(scb.Text.Trim());
                }

                var fn = tField?.Text.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(fn)) continue;
                _order.Measurements.Add(new Measurement {
                    Section = section,
                    FieldName = fn,
                    Value = tVal?.Text.Trim().NullIfEmpty(),
                    SelectedOptions = checkedOptions.Count > 0 ? string.Join(",", checkedOptions) : null,
                    Quantity = tQty?.Text.Trim().NullIfEmpty()
                });
            }
        }

        var stock = ReadStockSelections();
        if (!ConfirmStockAvailability(stock)) return;

        Database.SaveCustomer(_customer);
        _order.CustomerId = _customer.Id;
        Database.SaveOrder(_order, stock, _customer.Name);
        DialogResult = DialogResult.OK;
        Close();
    }

    private List<StockSelection> ReadStockSelections() {
        var picks = new List<StockSelection>();
        if (!_sectionPanels.TryGetValue("Point Of Sale", out var panel)) return picks;

        foreach (Control row in panel.Controls) {
            if (row is not Panel rowPanel) continue;

            ComboBox? combo = null;
            TextBox? tField = null, tVal = null, tQty = null;
            foreach (Control c in rowPanel.Controls) {
                if (c is ComboBox cb && Equals(cb.Tag, StockComboTag)) combo = cb;
                else if (c is TextBox tb) {
                    switch (tb.Tag as string) {
                        case "field": tField = tb; break;
                        case "value": tVal = tb; break;
                        case "qty": tQty = tb; break;
                    }
                }
            }

            if (combo?.SelectedItem is not StockItem picked) continue;
            if (tField?.Text.Trim() != SuitPurchaseFieldName) continue;

            var qty = 1;
            if (!string.IsNullOrWhiteSpace(tQty?.Text) && int.TryParse(tQty.Text.Trim(), out var parsedQty) && parsedQty > 0)
                qty = parsedQty;

            decimal.TryParse(tVal?.Text.Trim(), out var price);

            picks.Add(new StockSelection { StockItemId = picked.Id, Qty = qty, UnitPrice = price });
        }

        return picks;
    }

    private bool ConfirmStockAvailability(List<StockSelection> stock) {
        // Rows can repeat the same item, so availability is judged on the combined quantity.
        foreach (var group in stock.GroupBy(s => s.StockItemId)) {
            var item = Database.GetStockItem(group.Key);
            if (item == null) continue;

            var wanted = group.Sum(s => s.Qty);
            var alreadyTaken = _order.Id == 0
                ? 0
                : Database.GetStockSales(group.Key).Where(x => x.OrderId == _order.Id).Sum(x => x.Qty);

            var available = item.Quantity + alreadyTaken;
            if (wanted <= available) continue;

            var label = string.IsNullOrWhiteSpace(item.Color) ? item.SuitType : $"{item.SuitType} - {item.Color}";
            var answer = MessageBox.Show(
                $"Only {available} item(s) of '{label}' are in stock, but {wanted} are being sold." +
                Environment.NewLine + Environment.NewLine +
                "Save anyway? Stock will go negative.",
                "Stock", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (answer != DialogResult.Yes) return false;
        }

        return true;
    }

}

public static class StringExtensions
{
    public static string? NullIfEmpty(this string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
