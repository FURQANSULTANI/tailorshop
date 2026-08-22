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
        Text           = _isNew ? "New Customer — TopStitch Tailor" : $"Edit: {_customer.Name}";

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
        btnAddField.Click += (_, _) => AddFieldRow(section, "", "");
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
        var existing = _customer.ForSection(section);
        if (existing.Count > 0)
            foreach (var m in existing) AddFieldRow(section, m.FieldName, m.Value ?? "", scrollIntoView: false);
        else
            foreach (var f in Database.DefaultFields[section]) AddFieldRow(section, f, "", scrollIntoView: false);

        return tab;
    }

    // ── Field Row ─────────────────────────────────────────────────────────────

    private void AddFieldRow(string section, string fieldName, string value, bool scrollIntoView = true)
    {
        var panel = _sectionPanels[section];

        var row = new Panel { Height = 48, Margin = new Padding(0, 0, 0, 6), BackColor = Theme.NormalGrey };

        var txtField = new TextBox
        {
            Text            = fieldName,
            Location        = new Point(12, 10),
            Width           = 215,
            Font            = Theme.UrduFont,
            ForeColor       = Theme.TextOnNormal,
            RightToLeft     = RightToLeft.Yes,
            TextAlign       = HorizontalAlignment.Right,
            BorderStyle     = BorderStyle.FixedSingle,
            BackColor       = Theme.NormalGrey,
            PlaceholderText = "Field ka naam..."
        };
        var txtVal = new TextBox
        {
            Text            = value,
            Location        = new Point(240, 12),
            Width           = 160,
            Font            = new Font("Segoe UI", 9.5f),
            BorderStyle     = BorderStyle.FixedSingle,
            BackColor       = Theme.NormalGrey,
            ForeColor       = Theme.TextOnNormal,
            PlaceholderText = "Inches..."
        };
        var lblIn = new Label
        {
            Text      = "in",
            Location  = new Point(408, 16),
            AutoSize  = true,
            ForeColor = Theme.TextOnNormal,
            Font      = new Font("Segoe UI", 8.5f)
        };
        var btnRemove = new Button
        {
            Text      = "✖",
            Location  = new Point(432, 10),
            Size      = new Size(28, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.DarkGrey,
            ForeColor = Theme.TextOnDark,
            Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
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
        };
        Theme.RoundCorners(btnRemove, 5);

        row.Controls.AddRange(new Control[] { txtField, txtVal, lblIn, btnRemove });
        void FitWidth() => row.Width = Math.Max(panel.ClientSize.Width, 480);
        FitWidth();
        panel.Resize += (_, _) => FitWidth();
        panel.Controls.Add(row);
        if (scrollIntoView) panel.ScrollControlIntoView(row);
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
                "TopStitch Tailor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var printDoc = new PrintDocument();
        printDoc.DocumentName = $"TopStitch Tailor - {_customer.Name} - {section}";
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
            TextBox? tField = null, tVal = null;
            foreach (Control c in rowPanel.Controls)
                if (c is TextBox tb) { if (tField == null) tField = tb; else tVal = tb; }

            var fn = tField?.Text.Trim() ?? "";
            list.Add((fn, tVal?.Text.Trim() ?? "—"));
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

        g.DrawString("✂  TopStitch Tailor", shopFont, new SolidBrush(Theme.DarkGold), x, y);
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
        using var rowFont     = new Font("Segoe UI", 10f);
        using var rowAltBg    = new SolidBrush(Theme.NormalGrey);
        using var gridBorder  = new Pen(Theme.DarkGrey);
        using var labelFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        using var valueFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        float rowH   = 32f;
        float gap    = 16f;
        float halfW  = (pageW - gap) / 2f;
        float valueW = 60f;

        void DrawCell(float cellX, string field, string val, bool shade)
        {
            if (shade) g.FillRectangle(rowAltBg, cellX, y, halfW, rowH);
            g.DrawRectangle(gridBorder, cellX, y, halfW, rowH);
            g.DrawLine(gridBorder, cellX + valueW, y, cellX + valueW, y + rowH);
            g.DrawString(val, rowFont, darkBrush, new RectangleF(cellX, y, valueW, rowH), valueFormat);
            g.DrawString(field + " :", Theme.UrduFont, darkBrush,
                new RectangleF(cellX + valueW + 4, y, halfW - valueW - 8, rowH), labelFormat);
        }

        bool alt = false;
        for (int i = 0; i < _printRows.Count; i += 2)
        {
            var (f1, v1) = _printRows[i];
            DrawCell(x + halfW + gap, f1, v1, alt);
            if (i + 1 < _printRows.Count)
            {
                var (f2, v2) = _printRows[i + 1];
                DrawCell(x, f2, v2, alt);
            }
            y  += rowH;
            alt = !alt;
        }

        y += 20;
        // ── Footer ───────────────────────────────────────────────────
        g.DrawLine(goldPen, x - 20, y, x + pageW + 20, y);
        y += 8;
        using var footerFont = new Font("Segoe UI", 8.5f, FontStyle.Italic);
        g.DrawString("TopStitch Tailor  •  Thank you for your trust!",
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
            MessageBox.Show("Customer ka naam zaroor likhein!", "TopStitch Tailor",
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
                TextBox? tField = null, tVal = null;
                foreach (Control c in rowPanel.Controls)
                    if (c is TextBox tb) { if (tField == null) tField = tb; else tVal = tb; }

                var fn = tField?.Text.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(fn)) continue;
                _customer.Measurements.Add(new Measurement
                {
                    Section   = section,
                    FieldName = fn,
                    Value     = tVal?.Text.Trim().NullIfEmpty()
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
