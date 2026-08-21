using System.Drawing.Printing;

namespace TailorShop;

public partial class CustomerForm : Form
{
    private readonly Customer _customer;
    private readonly bool _isNew;
    private readonly Dictionary<string, Panel> _sectionPanels = new();

    // Print state
    private string _printSection = "";
    private List<(string Field, string Value)> _printRows = new();

    private static readonly Color Gold   = Color.FromArgb(184, 134, 11);
    private static readonly Color DarkBg = Color.FromArgb(30, 25, 10);

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
        AcceptButton   = btnSave;
    }

    // ── Measurement Tab ───────────────────────────────────────────────────────

    private TabPage BuildMeasurementTab(string section)
    {
        var emoji = section switch
        {
            "Shirt"           => "👔",
            "Shalwar Kameez"  => "🇵🇰",
            "Pant"            => "👖",
            "Coat / Sherwani" => "🥼",
            _                 => "📏"
        };

        var tab = new TabPage($"{emoji}  {section}") { BackColor = Color.White };

        // Scrollable field area
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
        _sectionPanels[section] = scroll;
        tab.Controls.Add(scroll);

        // Bottom toolbar panel
        var toolbar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 44,
            BackColor = Color.FromArgb(255, 248, 220)
        };

        var btnAddField = new Button
        {
            Text      = "➕  Add Field",
            BackColor = Gold,
            ForeColor = DarkBg,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Size      = new Size(140, 32),
            Location  = new Point(10, 6),
            Cursor    = Cursors.Hand
        };
        btnAddField.FlatAppearance.BorderSize = 0;
        btnAddField.Click += (_, _) => AddFieldRow(section, "", "");

        var btnPrint = new Button
        {
            Text      = "🖨️  Print",
            BackColor = Color.FromArgb(30, 90, 160),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Size      = new Size(120, 32),
            Location  = new Point(158, 6),
            Cursor    = Cursors.Hand
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += (_, _) => PrintSection(section);

        toolbar.Controls.Add(btnAddField);
        toolbar.Controls.Add(btnPrint);
        tab.Controls.Add(toolbar);

        // Load fields
        var existing = _customer.ForSection(section);
        if (existing.Count > 0)
            foreach (var m in existing) AddFieldRow(section, m.FieldName, m.Value ?? "");
        else
            foreach (var f in Database.DefaultFields[section]) AddFieldRow(section, f, "");

        return tab;
    }

    // ── Field Row ─────────────────────────────────────────────────────────────

    private void AddFieldRow(string section, string fieldName, string value)
    {
        var panel = _sectionPanels[section];

        int top = 8;
        foreach (Control c in panel.Controls) top = Math.Max(top, c.Bottom + 5);

        var row = new Panel { Location = new Point(0, top), Height = 38, BackColor = Color.White };

        var txtField = new TextBox
        {
            Text            = fieldName,
            Location        = new Point(12, 6),
            Width           = 215,
            Font            = new Font("Segoe UI", 9.5f),
            BorderStyle     = BorderStyle.FixedSingle,
            BackColor       = Color.FromArgb(255, 252, 235),
            PlaceholderText = "Field ka naam..."
        };
        var txtVal = new TextBox
        {
            Text            = value,
            Location        = new Point(236, 6),
            Width           = 160,
            Font            = new Font("Segoe UI", 9.5f),
            BorderStyle     = BorderStyle.FixedSingle,
            PlaceholderText = "Inches..."
        };
        var lblIn = new Label
        {
            Text      = "in",
            Location  = new Point(404, 10),
            AutoSize  = true,
            ForeColor = Color.FromArgb(130, 110, 50),
            Font      = new Font("Segoe UI", 8.5f)
        };
        var btnRemove = new Button
        {
            Text      = "✖",
            Location  = new Point(428, 5),
            Size      = new Size(28, 26),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(200, 60, 60),
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            TabStop   = false
        };
        btnRemove.FlatAppearance.BorderSize = 0;
        btnRemove.Click += (_, _) =>
        {
            panel.Controls.Remove(row);
            row.Dispose();
            ReflowRows(panel);
        };

        row.Controls.AddRange(new Control[] { txtField, txtVal, lblIn, btnRemove });
        row.Width = Math.Max(panel.Width, 480);
        panel.Resize += (_, _) => row.Width = Math.Max(panel.Width, 480);
        panel.Controls.Add(row);
        panel.ScrollControlIntoView(row);
    }

    private static void ReflowRows(Panel panel)
    {
        int top = 8;
        foreach (Control c in panel.Controls) { c.Top = top; top = c.Bottom + 5; }
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
            if (string.IsNullOrWhiteSpace(fn)) continue;
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
        using var goldBrush  = new SolidBrush(Color.FromArgb(160, 110, 0));
        using var darkBrush  = new SolidBrush(Color.FromArgb(30, 25, 10));
        using var grayBrush  = new SolidBrush(Color.FromArgb(100, 100, 100));
        using var whiteBrush = new SolidBrush(Color.White);

        // Header background bar
        using var headerBg = new SolidBrush(Color.FromArgb(30, 25, 10));
        g.FillRectangle(headerBg, x - 20, y - 10, pageW + 40, 60);

        g.DrawString("✂  TopStitch Tailor", shopFont, goldBrush, x, y);
        g.DrawString("Professional Tailoring Services", subFont,
            new SolidBrush(Color.FromArgb(200, 175, 110)), x + 2, y + 32);
        y += 80;

        // ── Divider ───────────────────────────────────────────────────
        using var goldPen = new Pen(Color.FromArgb(184, 134, 11), 2);
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

        // ── Table Header ──────────────────────────────────────────────
        using var tableHeaderBg = new SolidBrush(Color.FromArgb(50, 42, 20));
        using var colFont       = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        float col1 = x, col2 = x + pageW * 0.65f;
        float rowH = 26f;

        g.FillRectangle(tableHeaderBg, x - 5, y, pageW + 10, rowH);
        g.DrawString("Measurement", colFont, goldBrush, col1 + 4, y + 5);
        g.DrawString("Value (inches)", colFont, goldBrush, col2, y + 5);
        y += rowH;

        // ── Table Rows ────────────────────────────────────────────────
        using var rowFont    = new Font("Segoe UI", 10f);
        using var rowAltBg   = new SolidBrush(Color.FromArgb(255, 250, 225));
        using var rowBorder  = new Pen(Color.FromArgb(220, 200, 140));

        bool alt = false;
        foreach (var (field, val) in _printRows)
        {
            if (alt) g.FillRectangle(rowAltBg, x - 5, y, pageW + 10, rowH);
            g.DrawRectangle(rowBorder, x - 5, y, pageW + 10, rowH);
            g.DrawString(field, rowFont, darkBrush, col1 + 4, y + 5);
            g.DrawString(val,   rowFont, darkBrush, col2,     y + 5);
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
