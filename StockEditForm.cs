namespace TailorShop;

public class StockEditForm : Form
{
    private readonly StockItem _item;
    private readonly TextBox   _txtType;
    private readonly TextBox   _txtColor;
    private readonly TextBox   _txtQty;
    private readonly TextBox   _txtPurchase;
    private readonly TextBox   _txtRetail;

    public StockEditForm(StockItem? item)
    {
        _item = item ?? new StockItem();
        var isNew = _item.Id == 0;

        Text            = isNew ? "New Stock Item" : "Edit Stock Item";
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ClientSize      = new Size(440, 356);
        BackColor       = Theme.NormalGrey;
        Font            = new Font("Segoe UI", 10f);

        var header = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.DarkGrey };
        header.Controls.Add(new Label
        {
            Text      = isNew ? "New Stock Item" : "Edit Stock Item",
            ForeColor = Theme.TextOnDark,
            Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
            AutoSize  = true,
            Location  = new Point(20, 13)
        });
        Controls.Add(header);

        int y = 70;
        _txtType     = AddField("Suit Type *",     _item.SuitType, ref y);
        _txtColor    = AddField("Color",           _item.Color ?? "", ref y);
        _txtQty      = AddField("No. of Items *",  isNew ? "" : _item.Quantity.ToString(), ref y);
        _txtPurchase = AddField("Purchase Price",  isNew ? "" : _item.PurchasePrice.ToString("0.##"), ref y);
        _txtRetail   = AddField("Retail Price *",  isNew ? "" : _item.RetailPrice.ToString("0.##"), ref y);

        foreach (var box in new[] { _txtQty, _txtPurchase, _txtRetail })
            box.KeyPress += NumericOnly;

        var btnSave = MakeButton("Save", Theme.DarkGrey, Theme.TextOnDark, new Point(20, y + 10), new Size(150, 38));
        Theme.SetIcon(btnSave, Theme.Glyph.Save);
        btnSave.Click += BtnSave_Click;
        Controls.Add(btnSave);

        var btnCancel = MakeButton("Cancel", Theme.NormalGrey, Theme.TextOnNormal, new Point(182, y + 10), new Size(110, 38));
        Theme.SetIcon(btnCancel, Theme.Glyph.Cancel);
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(btnCancel);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
        Shown += (_, _) => Theme.CenterButtons(this);
    }

    public StockItem Item => _item;

    private TextBox AddField(string label, string value, ref int y)
    {
        Controls.Add(new Label
        {
            Text      = label,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Theme.TextInk,
            AutoSize  = true,
            Location  = new Point(20, y)
        });

        var holder = new Panel
        {
            BackColor = Theme.DarkGold,
            Location  = new Point(180, y - 4),
            Size      = new Size(232, 30)
        };
        var box = new TextBox
        {
            Text        = value,
            Font        = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.None,
            BackColor   = Color.White,
            ForeColor   = Theme.TextInk,
            Location    = new Point(4, 5),
            Width       = 224
        };
        holder.Controls.Add(box);
        Controls.Add(holder);

        y += 44;
        return box;
    }

    private static void NumericOnly(object? sender, KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar)) return;
        if (char.IsDigit(e.KeyChar)) return;
        if (e.KeyChar == '.' && sender is TextBox t && !t.Text.Contains('.')) return;
        e.Handled = true;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtType.Text))
        {
            MessageBox.Show("Suit type is required.", "Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtType.Focus();
            return;
        }

        if (!int.TryParse(_txtQty.Text.Trim(), out var qty) || qty < 0)
        {
            MessageBox.Show("Enter a valid number of items.", "Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtQty.Focus();
            return;
        }

        if (!decimal.TryParse(_txtRetail.Text.Trim(), out var retail) || retail < 0)
        {
            MessageBox.Show("Enter a valid retail price.", "Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtRetail.Focus();
            return;
        }

        decimal.TryParse(_txtPurchase.Text.Trim(), out var purchase);

        _item.SuitType      = _txtType.Text.Trim();
        _item.Color         = _txtColor.Text.Trim().Length == 0 ? null : _txtColor.Text.Trim();
        _item.Quantity      = qty;
        _item.PurchasePrice = purchase;
        _item.RetailPrice   = retail;

        Database.SaveStockItem(_item);
        DialogResult = DialogResult.OK;
        Close();
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
            Cursor    = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize  = 2;
        btn.FlatAppearance.BorderColor = Theme.DarkGold;
        btn.FlatAppearance.MouseOverBackColor = Theme.Hover(back);
        Theme.RoundCorners(btn, 6);
        return btn;
    }
}
