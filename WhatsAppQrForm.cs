namespace TailorShop;

public class WhatsAppQrForm : Form
{
    private readonly PictureBox _qr;

    public WhatsAppQrForm()
    {
        Text            = "WhatsApp Setup";
        Size            = new Size(340, 420);
        StartPosition   = FormStartPosition.CenterScreen;
        BackColor       = Theme.NormalGrey;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;

        var lbl = new Label
        {
            Text      = "WhatsApp se link karne ke liye QR code scan karein\n(WhatsApp > Linked Devices > Link a Device)",
            Dock      = DockStyle.Top,
            Height    = 60,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Theme.TextOnNormal,
            BackColor = Theme.NormalGrey,
            Font      = new Font("Segoe UI", 9.5f)
        };
        _qr = new PictureBox
        {
            Dock      = DockStyle.Fill,
            SizeMode  = PictureBoxSizeMode.Zoom,
            BackColor = Theme.NormalGrey
        };
        Controls.Add(_qr);
        Controls.Add(lbl);
    }

    public void SetQr(Image img)
    {
        _qr.Image?.Dispose();
        _qr.Image = img;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _qr.Image?.Dispose();
        base.Dispose(disposing);
    }
}
