namespace TailorShop;

public class WhatsAppSendForm : Form
{
    private readonly TextBox _txtMessage;

    public string Message => _txtMessage.Text.Trim();

    public WhatsAppSendForm(string customerName, string phone, string defaultMessage)
    {
        Text            = "Send WhatsApp Message";
        Size            = new Size(420, 320);
        StartPosition   = FormStartPosition.CenterParent;
        BackColor       = Theme.NormalGrey;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;

        var lblTo = new Label
        {
            Text      = $"{customerName}  ({phone})",
            Dock      = DockStyle.Top,
            Height    = 36,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(12, 0, 0, 0),
            ForeColor = Theme.TextOnDark,
            BackColor = Theme.DarkGrey,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold)
        };

        var panelButtons = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Theme.DarkGrey };

        var btnSend = new Button
        {
            Text      = "Send",
            BackColor = Theme.DarkGold,
            ForeColor = Theme.TextOnGold,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Size      = new Size(100, 32),
            Location  = new Point(16, 9),
            Cursor    = Cursors.Hand
        };
        btnSend.FlatAppearance.BorderSize = 0;
        btnSend.FlatAppearance.MouseOverBackColor = Theme.Hover(Theme.DarkGold);
        btnSend.Image             = Theme.CreateChatBubbleIcon(btnSend.ForeColor, 16);
        btnSend.ImageAlign        = ContentAlignment.MiddleLeft;
        btnSend.TextImageRelation = TextImageRelation.ImageBeforeText;
        btnSend.Padding           = new Padding(6, 0, 6, 0);
        btnSend.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };

        var btnCancel = new Button
        {
            Text      = "Cancel",
            BackColor = Theme.NormalGrey,
            ForeColor = Theme.TextOnNormal,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f),
            Size      = new Size(90, 32),
            Location  = new Point(126, 9),
            Cursor    = Cursors.Hand
        };
        Theme.ApplyLightHover(btnCancel);
        Theme.SetIcon(btnCancel, Theme.Glyph.Cancel);
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        panelButtons.Controls.Add(btnSend);
        panelButtons.Controls.Add(btnCancel);

        _txtMessage = new TextBox
        {
            Multiline   = true,
            Dock        = DockStyle.Fill,
            Text        = defaultMessage,
            Font        = new Font("Segoe UI", 10f),
            BackColor   = Theme.NormalGrey,
            ForeColor   = Theme.TextOnNormal,
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars  = ScrollBars.Vertical
        };

        Controls.Add(_txtMessage);
        Controls.Add(panelButtons);
        Controls.Add(lblTo);

        AcceptButton = btnSend;
        CancelButton = btnCancel;

        Theme.RoundCorners(btnSend, 6);
        Theme.RoundCorners(btnCancel, 6);
        Shown += (_, _) => Theme.CenterButtons(this);
    }
}
