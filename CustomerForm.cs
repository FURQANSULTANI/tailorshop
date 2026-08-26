using System.Drawing.Printing;

namespace TailorShop;

public partial class CustomerForm : Form
{
    private readonly Customer _customer;
    private readonly bool _isNew;
    private readonly Dictionary<string, FlowLayoutPanel> _sectionPanels = new();

    // Print state
    private string _printSection = "";
    private List<(string Field, string Value)> _printRows = new();

    private static readonly Color Gold   = Theme.DarkGold;
    private static readonly Color DarkBg = Theme.DarkGrey;

    public CustomerForm(Customer? customer)
    {
        _isNew    = customer == null;
        _customer = customer ?? new Customer();
        InitializeComponent();

        lblHeader.Text = _isNew ? "✂  New Customer" : $"✂  Edit: {_customer.Name}";
        Text           = _isNew ? "New Customer — TailorShop" : $"Edit: {_customer.Name}";

        foreach (var section in Database.DefaultFields.Keys)
            tabControl.TabPages.Add(BuildMeasurementTab(section));

        txtName.Text    = _customer.Name;
        txtPhone.Text   = _customer.Phone   ?? "";
        txtAddress.Text = _customer.Address ?? "";
        txtNotes.Text   = _customer.Notes   ?? "";

        btnSave.Click += BtnSave_Click;
        tabControl.DrawItem += TabControl_DrawItem;
        AcceptButton   = btnSave;

        Theme.RoundCorners(btnSave, 6);
        Theme.RoundCorners(btnCancel, 6);
        panelHeader.Controls.Add(Theme.AccentDivider(DockStyle.Bottom));
        panelBottom.Controls.Add(Theme.AccentDivider(DockStyle.Top));
    }

    // ── Tab Strip (owner-drawn so it follows the theme, not the OS default) ────

    private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
    {
        var tab      = tabControl.TabPages[e.Index];
        var selected = e.Index == tabControl.SelectedIndex;
        var back     = selected ? Theme.DarkGold : Theme.DarkGrey;
        var fore     = selected ? Theme.TextOnGold : Theme.TextOnDark;

        using var backBrush = new SolidBrush(back);
        e.Graphics.FillRectangle(backBrush, e.Bounds);
        TextRenderer.DrawText(e.Graphics, tab.Text, tabControl.Font, e.Bounds, fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    // ── Measurement Tab ───────────────────────────────────────────────────────

    private TabPage BuildMeasurementTab(string section)
    {
        bool isPos = section == "Point Of Sale";
        var tab = new TabPage(section) { BackColor = Theme.NormalGrey };

        // Scrollable field area
        var scroll = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            AutoScroll    = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            BackColor     = Theme.NormalGrey,
            Padding       = new Padding(0, 10, 0, 10)
        };
        _sectionPanels[section] = scroll;
        tab.Controls.Add(scroll);

        // Bottom toolbar panel
        var toolbar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 44,
            BackColor = Theme.DarkGrey
        };

        var btnAddField = new Button
        {
            Text      = "+  Add Field",
            BackColor = Gold,
            ForeColor = DarkBg,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Size      = new Size(130, 32),
            Location  = new Point(12, 6),
            Cursor    = Cursors.Hand
        };
        btnAddField.FlatAppearance.BorderSize = 0;
        btnAddField.FlatAppearance.MouseOverBackColor = Theme.Hover(Gold);
        btnAddField.Click += (_, _) =>
        {
            if (isPos)
            {
                var newDef = new FieldDef { Name = "", ValuePlaceholder = "Amount...", UnitLabel = "Rs", Numeric = true };
                AddFieldRow(section, newDef, "", Array.Empty<string>(), onChanged: () => RecalculatePos(section));
                MoveTotalRowLast(section);
                RecalculatePos(section);
            }
            else
            {
                AddFieldRow(section, new FieldDef { Name = "" }, "", Array.Empty<string>());
            }
        };
        Theme.RoundCorners(btnAddField, 6);

        var btnPrint = new Button
        {
            Text      = "Print",
            BackColor = Theme.NormalGrey,
            ForeColor = Theme.TextOnNormal,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Size      = new Size(90, 32),
            Location  = new Point(150, 6),
            Cursor    = Cursors.Hand
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.FlatAppearance.MouseOverBackColor = Theme.Hover(Theme.NormalGrey);
        btnPrint.Click += (_, _) => PrintSection(section);
        Theme.RoundCorners(btnPrint, 6);

        toolbar.Controls.Add(btnAddField);
        toolbar.Controls.Add(btnPrint);
        tab.Controls.Add(toolbar);

        // Load fields
        Action? onChanged = isPos ? () => RecalculatePos(section) : null;
        var existing = _customer.ForSection(section);
        if (existing.Count > 0)
        {
            var defsByName = Database.DefaultFields.TryGetValue(section, out var defs)
                ? defs.ToDictionary(d => d.Name)
                : new Dictionary<string, FieldDef>();
            foreach (var m in existing)
            {
                var def      = defsByName.TryGetValue(m.FieldName, out var d) ? d : new FieldDef { Name = m.FieldName };
                var selected = m.SelectedOptions?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                AddFieldRow(section, def, m.Value ?? "", selected, scrollIntoView: false, onChanged: onChanged, quantity: m.Quantity);
            }
        }
        else
        {
            foreach (var f in Database.DefaultFields[section])
                AddFieldRow(section, f, "", Array.Empty<string>(), scrollIntoView: false, onChanged: onChanged);
        }

        if (isPos)
        {
            scroll.Controls.Add(BuildTotalRow(section));
            RecalculatePos(section);
        }

        return tab;
    }

    // ── Field Row ─────────────────────────────────────────────────────────────

    private Panel AddFieldRow(string section, FieldDef def, string value, string[] selectedOptions,
        bool scrollIntoView = true, Action? onChanged = null, string? quantity = null)
    {
        var panel = _sectionPanels[section];

        bool showValue      = def.Kind is FieldKind.Text or FieldKind.Both;
        bool showCheckboxes = def.Kind is FieldKind.Checkbox or FieldKind.Both;

        var row = new Panel { Height = 48, Margin = new Padding(0, 0, 0, 6), BackColor = Theme.NormalGrey };

        int cursorX = 12;

        var txtField = new TextBox
        {
            Text            = def.Name,
            Tag             = "field",
            Location        = new Point(cursorX, 10),
            Width           = 190,
            Font            = Theme.UrduFont,
            ForeColor       = Theme.TextOnNormal,
            RightToLeft     = RightToLeft.Yes,
            TextAlign       = HorizontalAlignment.Right,
            BorderStyle     = BorderStyle.FixedSingle,
            BackColor       = Theme.NormalGrey,
            PlaceholderText = "Field ka naam..."
        };
        row.Controls.Add(txtField);
        cursorX += txtField.Width + 12;

        if (def.HasQuantity)
        {
            var txtQty = new TextBox
            {
                Text            = quantity ?? "",
                Tag             = "qty",
                Location        = new Point(cursorX, 12),
                Width           = 50,
                Font            = new Font("Segoe UI", 9.5f),
                BorderStyle     = BorderStyle.FixedSingle,
                BackColor       = Theme.NormalGrey,
                ForeColor       = Theme.TextOnNormal,
                TextAlign       = HorizontalAlignment.Center,
                PlaceholderText = "Qty..."
            };
            txtQty.KeyPress += (_, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            if (onChanged != null) txtQty.TextChanged += (_, _) => onChanged();
            row.Controls.Add(txtQty);
            cursorX += txtQty.Width + 4;

            var lblX = new Label
            {
                Text      = "×",
                Location  = new Point(cursorX, 16),
                AutoSize  = true,
                ForeColor = Theme.TextOnNormal,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            row.Controls.Add(lblX);
            cursorX += lblX.Width + 4;
        }

        if (showValue)
        {
            var txtVal = new TextBox
            {
                Text            = value,
                Tag             = "value",
                Location        = new Point(cursorX, 12),
                Width           = 90,
                Font            = new Font("Segoe UI", 9.5f),
                BorderStyle     = BorderStyle.FixedSingle,
                BackColor       = Theme.NormalGrey,
                ForeColor       = Theme.TextOnNormal,
                PlaceholderText = def.ValuePlaceholder
            };
            if (def.Numeric)
            {
                txtVal.KeyPress += (_, e) =>
                {
                    if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar)) return;
                    if (e.KeyChar == '.' && !txtVal.Text.Contains('.')) return;
                    e.Handled = true;
                };
            }
            if (onChanged != null) txtVal.TextChanged += (_, _) => onChanged();
            row.Controls.Add(txtVal);
            cursorX += txtVal.Width + 4;

            var lblIn = new Label
            {
                Text      = def.UnitLabel,
                Location  = new Point(cursorX, 16),
                AutoSize  = true,
                ForeColor = Theme.TextOnNormal,
                Font      = new Font("Segoe UI", 8.5f)
            };
            row.Controls.Add(lblIn);
            cursorX += lblIn.Width + 16;
        }

        const int fieldHeight = 28; // matches the value input box — checkbox chips and ✖ line up with it

        if (showCheckboxes)
        {
            foreach (var opt in def.Options)
            {
                var cb = new CheckBox
                {
                    Text        = opt,
                    Checked     = selectedOptions.Contains(opt),
                    Appearance  = Appearance.Button,
                    FlatStyle   = FlatStyle.Flat,
                    AutoSize    = false,
                    Height      = fieldHeight,
                    TextAlign   = ContentAlignment.MiddleCenter,
                    Font        = Theme.UrduFontSmall,
                    Location    = new Point(cursorX, 11)
                };
                cb.Width = Math.Max(95, TextRenderer.MeasureText(opt, Theme.UrduFontSmall).Width + 52);
                cb.FlatAppearance.BorderSize = 1;
                cb.FlatAppearance.BorderColor = Theme.DarkGrey;
                void UpdateCbColors() {
                    cb.BackColor = cb.Checked ? Theme.DarkGold : Theme.NormalGrey;
                    cb.ForeColor = cb.Checked ? Theme.TextOnGold : Theme.TextOnNormal;
                }
                UpdateCbColors();
                cb.CheckedChanged += (_, _) => UpdateCbColors();
                row.Controls.Add(cb);
                cursorX += cb.Width + 10;
            }
        }

        var btnRemove = new Button
        {
            Text      = "✖",
            Location  = new Point(cursorX, 11),
            Size      = new Size(fieldHeight, fieldHeight),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.DarkGrey,
            ForeColor = Theme.TextOnDark,
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            TabStop   = false
        };
        btnRemove.FlatAppearance.BorderSize = 1;
        btnRemove.FlatAppearance.BorderColor = Theme.DarkGold;
        btnRemove.FlatAppearance.MouseOverBackColor = Theme.DarkGold;
        btnRemove.MouseEnter += (_, _) => btnRemove.ForeColor = Theme.TextOnGold;
        btnRemove.MouseLeave += (_, _) => btnRemove.ForeColor = Theme.TextOnDark;
        btnRemove.Click += (_, _) =>
        {
            panel.Controls.Remove(row);
            row.Dispose();
            onChanged?.Invoke();
        };
        Theme.RoundCorners(btnRemove, 4);
        row.Controls.Add(btnRemove);
        cursorX += btnRemove.Width + 12;

        void FitWidth() => row.Width = Math.Max(Math.Max(panel.ClientSize.Width, 480), cursorX);
        FitWidth();
        panel.Resize += (_, _) => FitWidth();
        panel.Controls.Add(row);
        if (scrollIntoView) panel.ScrollControlIntoView(row);
        return row;
    }

    // ── Point Of Sale ────────────────────────────────────────────────────────

    private const string TotalRowTag = "TotalRow";
    private const string AdvanceFieldName = "ایڈوانس";

    private Panel BuildTotalRow(string section)
    {
        var panel = _sectionPanels[section];
        var row = new Panel { Height = 48, Margin = new Padding(0, 6, 0, 0), BackColor = Theme.DarkGrey, Tag = TotalRowTag };

        int cursorX = 12;

        var lblName = new TextBox
        {
            Text        = "ٹوٹل بل",
            Tag         = "field",
            ReadOnly    = true,
            TabStop     = false,
            Location    = new Point(cursorX, 10),
            Width       = 190,
            Font        = Theme.UrduFont,
            ForeColor   = Theme.DarkGold,
            RightToLeft = RightToLeft.Yes,
            TextAlign   = HorizontalAlignment.Right,
            BorderStyle = BorderStyle.None,
            BackColor   = Theme.DarkGrey
        };
        row.Controls.Add(lblName);
        cursorX += lblName.Width + 12;

        var txtTotal = new TextBox
        {
            Text        = "0",
            Tag         = "value",
            ReadOnly    = true,
            TabStop     = false,
            Location    = new Point(cursorX, 10),
            Width       = 140,
            Font        = new Font("Segoe UI", 12f, FontStyle.Bold),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor   = Theme.DarkGold,
            ForeColor   = Theme.TextOnGold,
            TextAlign   = HorizontalAlignment.Center
        };
        row.Controls.Add(txtTotal);
        cursorX += txtTotal.Width + 8;

        var lblRs = new Label
        {
            Text      = "Rs",
            Location  = new Point(cursorX, 16),
            AutoSize  = true,
            ForeColor = Theme.TextOnDark,
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        row.Controls.Add(lblRs);

        void FitWidth() => row.Width = Math.Max(panel.ClientSize.Width, 480);
        FitWidth();
        panel.Resize += (_, _) => FitWidth();

        return row;
    }

    private void MoveTotalRowLast(string section)
    {
        var panel = _sectionPanels[section];
        foreach (Control c in panel.Controls)
            if (Equals(c.Tag, TotalRowTag)) { panel.Controls.SetChildIndex(c, panel.Controls.Count - 1); return; }
    }

    private void RecalculatePos(string section)
    {
        var panel = _sectionPanels[section];
        TextBox? totalBox = null;
        decimal total = 0;

        foreach (Control c in panel.Controls)
        {
            if (c is not Panel rowPanel) continue;
            TextBox? tField = null, tVal = null, tQty = null;
            foreach (Control cc in rowPanel.Controls)
            {
                if (cc is not TextBox tb) continue;
                switch (tb.Tag as string)
                {
                    case "field": tField = tb; break;
                    case "value": tVal = tb; break;
                    case "qty":   tQty = tb; break;
                }
            }

            if (Equals(rowPanel.Tag, TotalRowTag)) { totalBox = tVal; continue; }

            decimal.TryParse(tVal?.Text.Trim(), out var amount);
            if (tQty != null)
            {
                decimal.TryParse(tQty.Text.Trim(), out var qty);
                amount *= qty == 0 && string.IsNullOrEmpty(tQty.Text.Trim()) ? 1 : qty;
            }
            total += tField?.Text.Trim() == AdvanceFieldName ? -amount : amount;
        }

        if (totalBox != null) totalBox.Text = total.ToString("N0");
    }

    // ── Print ─────────────────────────────────────────────────────────────────

    private void PrintSection(string section)
    {
        // Collect current field values from the UI (not just saved DB values)
        _printSection = section;
        _printRows    = CollectRows(section);

        if (_printRows.Count == 0)
        {
            MessageBox.Show("Print karne ke liye koi measurement nahi hai.",
                "TailorShop", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var printDoc = new PrintDocument();
        printDoc.DocumentName = $"TailorShop - {_customer.Name} - {section}";
        printDoc.PrintPage   += PrintPage;

        using var preview = new PrintPreviewDialog
        {
            Document    = printDoc,
            WindowState = FormWindowState.Maximized,
            Text        = $"Print Preview — {_customer.Name} — {section}"
        };
        preview.ShowDialog(this);
    }

    private List<(string, string)> CollectRows(string section)
    {
        var list  = new List<(string, string)>();
        if (!_sectionPanels.TryGetValue(section, out var panel)) return list;

        foreach (Control row in panel.Controls)
        {
            if (row is not Panel rowPanel) continue;
            TextBox? tField = null, tVal = null, tQty = null;
            var checkedOptions = new List<string>();
            foreach (Control c in rowPanel.Controls)
            {
                if (c is TextBox tb)
                {
                    switch (tb.Tag as string)
                    {
                        case "field": tField = tb; break;
                        case "value": tVal = tb; break;
                        case "qty":   tQty = tb; break;
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

    private void PrintPage(object sender, PrintPageEventArgs e)
    {
        var g      = e.Graphics!;
        float x    = 60f;
        float y    = 50f;
        float pageW = e.PageBounds.Width - 120f;

        // ── Shop Header ───────────────────────────────────────────────
        using var shopFont   = new Font("Segoe UI", 22f, FontStyle.Bold);
        using var subFont    = new Font("Segoe UI", 10f, FontStyle.Italic);
        using var goldBrush  = new SolidBrush(Theme.DarkGold);
        using var darkBrush  = new SolidBrush(Theme.DarkGrey);
        using var grayBrush  = new SolidBrush(Color.FromArgb(180, Theme.DarkGrey));
        using var whiteBrush = new SolidBrush(Color.White);

        float titleH    = shopFont.GetHeight(g);
        float subtitleY = y + titleH + 2;
        float subtitleH = subFont.GetHeight(g);

        // Header background bar
        using var headerBg = new SolidBrush(Theme.DarkGrey);
        g.FillRectangle(headerBg, x - 20, y - 10, pageW + 40, subtitleY + subtitleH - y + 16);

        g.DrawString("✂  TailorShop", shopFont, new SolidBrush(Theme.DarkGold), x, y);
        g.DrawString("Professional Tailoring Services", subFont,
            new SolidBrush(Theme.TextOnDark), x + 2, subtitleY);
        y = subtitleY + subtitleH + 26;

        // ── Divider ───────────────────────────────────────────────────
        using var goldPen = new Pen(Theme.DarkGold, 2);
        g.DrawLine(goldPen, x - 20, y, x + pageW + 20, y);
        y += 14;

        // ── Customer Info ─────────────────────────────────────────────
        using var infoTitleFont = new Font("Segoe UI", 10f, FontStyle.Bold);
        using var infoFont      = new Font("Segoe UI", 10f);

        g.DrawString("Customer Information", infoTitleFont, goldBrush, x, y);
        y += 20;

        DrawInfoRow(g, infoFont, darkBrush, grayBrush, x, ref y, "Name",    _customer.Name);
        DrawInfoRow(g, infoFont, darkBrush, grayBrush, x, ref y, "Phone",   _customer.Phone    ?? "—");
        DrawInfoRow(g, infoFont, darkBrush, grayBrush, x, ref y, "Address", _customer.Address  ?? "—");
        DrawInfoRow(g, infoFont, darkBrush, grayBrush, x, ref y, "Date",
            DateTime.Now.ToString("dd MMM yyyy"));
        y += 8;

        // ── Divider ───────────────────────────────────────────────────
        g.DrawLine(goldPen, x - 20, y, x + pageW + 20, y);
        y += 14;

        // ── Section Title ─────────────────────────────────────────────
        using var secFont = new Font("Segoe UI", 13f, FontStyle.Bold);
        g.DrawString($"{_printSection} Measurements", secFont, goldBrush, x, y);
        y += 28;

        // ── Measurement Grid — two label:value pairs per line, right-to-left ───
        using var rowAltBg    = new SolidBrush(Theme.NormalGrey);
        using var gridBorder  = new Pen(Theme.DarkGrey);
        using var smallUrdu   = new Font("Urdu Typesetting", 12f);
        using var labelFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        using var valueFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        bool isPosSlip = _printSection == "Point Of Sale";
        float rowH   = 36f;
        float gap    = 16f;
        float halfW  = (pageW - gap) / 2f;
        float valueW = 110f;

        void DrawCell(float cellX, float cellW, string field, string val, bool shade)
        {
            if (shade) g.FillRectangle(rowAltBg, cellX, y, cellW, rowH);
            g.DrawRectangle(gridBorder, cellX, y, cellW, rowH);
            g.DrawLine(gridBorder, cellX + valueW, y, cellX + valueW, y + rowH);
            g.DrawString(val, smallUrdu, darkBrush, new RectangleF(cellX, y, valueW, rowH), valueFormat);
            g.DrawString(field + " :", Theme.UrduFont, darkBrush,
                new RectangleF(cellX + valueW + 4, y, cellW - valueW - 8, rowH), labelFormat);
        }

        if (isPosSlip)
        {
            // Billing slip — one line item per row, full width, top to bottom.
            bool alt = false;
            foreach (var (field, val) in _printRows)
            {
                DrawCell(x, pageW, field, val, alt);
                y  += rowH;
                alt = !alt;
            }
        }
        else
        {
            // Measurement chit — two label:value pairs per line, right-to-left.
            bool alt = false;
            for (int i = 0; i < _printRows.Count; i += 2)
            {
                var (f1, v1) = _printRows[i];
                DrawCell(x + halfW + gap, halfW, f1, v1, alt);
                if (i + 1 < _printRows.Count)
                {
                    var (f2, v2) = _printRows[i + 1];
                    DrawCell(x, halfW, f2, v2, alt);
                }
                y  += rowH;
                alt = !alt;
            }
        }

        y += 20;
        // ── Footer ───────────────────────────────────────────────────
        g.DrawLine(goldPen, x - 20, y, x + pageW + 20, y);
        y += 8;
        using var footerFont = new Font("Segoe UI", 8.5f, FontStyle.Italic);
        g.DrawString("TailorShop  •  Thank you for your trust!",
            footerFont, grayBrush, x, y);
        g.DrawString($"Printed: {DateTime.Now:dd MMM yyyy  hh:mm tt}",
            footerFont, grayBrush, x + pageW - 180, y);

        e.HasMorePages = false;
    }

    private static void DrawInfoRow(Graphics g, Font font,
        Brush dark, Brush gray, float x, ref float y,
        string label, string value)
    {
        g.DrawString($"{label}:", font, gray, x, y);
        g.DrawString(value,       font, dark, x + 80, y);
        y += 18;
    }

    // ── Save ─────────────────────────────────────────────────────────────────

    private void BtnSave_Click(object? s, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("Customer ka naam zaroor likhein!", "TailorShop",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _customer.Name    = txtName.Text.Trim();
        _customer.Phone   = txtPhone.Text.Trim().NullIfEmpty();
        _customer.Address = txtAddress.Text.Trim().NullIfEmpty();
        _customer.Notes   = txtNotes.Text.Trim().NullIfEmpty();
        _customer.Measurements.Clear();

        foreach (var (section, panel) in _sectionPanels)
        {
            foreach (Control row in panel.Controls)
            {
                if (row is not Panel rowPanel) continue;
                if (Equals(rowPanel.Tag, TotalRowTag)) continue; // computed fresh on load, never persisted
                TextBox? tField = null, tVal = null, tQty = null;
                var checkedOptions = new List<string>();
                foreach (Control c in rowPanel.Controls)
                {
                    if (c is TextBox tb)
                    {
                        switch (tb.Tag as string)
                        {
                            case "field": tField = tb; break;
                            case "value": tVal = tb; break;
                            case "qty":   tQty = tb; break;
                        }
                    }
                    else if (c is CheckBox { Checked: true } cb) checkedOptions.Add(cb.Text);
                }

                var fn = tField?.Text.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(fn)) continue;
                _customer.Measurements.Add(new Measurement
                {
                    Section         = section,
                    FieldName       = fn,
                    Value           = tVal?.Text.Trim().NullIfEmpty(),
                    SelectedOptions = checkedOptions.Count > 0 ? string.Join(",", checkedOptions) : null,
                    Quantity        = tQty?.Text.Trim().NullIfEmpty()
                });
            }
        }

        Database.SaveCustomer(_customer);
        DialogResult = DialogResult.OK;
        Close();
    }
}

public static class StringExtensions
{
    public static string? NullIfEmpty(this string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
