namespace TailorShop;

public class LicenseForm : Form
{
    private readonly LicenseInfo _info;
    private readonly TextBox     _txtMachineId;
    private readonly TextBox     _txtKey;
    private readonly Label       _lblStatus;

    public LicenseForm(LicenseInfo info)
    {
        _info = info;

        Text            = "Software Activation";
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ClientSize      = new Size(560, 430);
        BackColor       = Theme.NormalGrey;
        Font            = new Font("Segoe UI", 10f);

        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 76,
            BackColor = Theme.DarkGrey
        };

        header.Controls.Add(new Label
        {
            Text      = "TailorShop Activation",
            ForeColor = Theme.TextOnDark,
            Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
            AutoSize  = true,
            Location  = new Point(24, 14)
        });

        header.Controls.Add(new Label
        {
            Text      = "The Koder Bench  |  03043713001",
            ForeColor = Theme.TextOnDark,
            Font      = new Font("Segoe UI", 9f),
            AutoSize  = true,
            Location  = new Point(26, 46)
        });

        Controls.Add(header);

        _lblStatus = new Label
        {
            Text      = MessageFor(info.State),
            ForeColor = info.State == LicenseState.Missing ? Theme.TextInk : Theme.AlertRed,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            Location  = new Point(24, 92),
            Size      = new Size(510, 44)
        };
        Controls.Add(_lblStatus);

        Controls.Add(new Label
        {
            Text      = "Your Machine ID (send this to the vendor)",
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Theme.TextInk,
            AutoSize  = true,
            Location  = new Point(24, 142)
        });

        _txtMachineId = new TextBox
        {
            Text       = info.MachineId,
            ReadOnly   = true,
            Font       = new Font("Consolas", 12f, FontStyle.Bold),
            Location   = new Point(24, 166),
            Size       = new Size(390, 30),
            BackColor  = Color.White,
            ForeColor  = Theme.DarkGrey,
            TextAlign  = HorizontalAlignment.Center,
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(_txtMachineId);

        var btnCopy = MakeButton("Copy", Theme.DarkGold, Theme.TextOnGold, new Point(424, 165), new Size(110, 32));
        btnCopy.Click += (_, _) =>
        {
            Clipboard.SetText(info.MachineId);
            SetStatus("Machine ID copied to clipboard.", Theme.DarkGrey);
        };
        Controls.Add(btnCopy);

        Controls.Add(new Label
        {
            Text      = "Paste your License Key here",
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Theme.TextInk,
            AutoSize  = true,
            Location  = new Point(24, 212)
        });

        _txtKey = new TextBox
        {
            Multiline   = true,
            ScrollBars  = ScrollBars.Vertical,
            Font        = new Font("Consolas", 10f),
            Location    = new Point(24, 236),
            Size        = new Size(510, 96),
            BackColor   = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(_txtKey);

        var btnActivate = MakeButton("Activate", Theme.DarkGrey, Theme.TextOnDark, new Point(24, 348), new Size(170, 40));
        btnActivate.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        btnActivate.Click += BtnActivate_Click;
        Controls.Add(btnActivate);

        var btnExit = MakeButton("Exit", Theme.DeleteAccent, Theme.TextOnDeleteAccent, new Point(206, 348), new Size(120, 40));
        btnExit.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(btnExit);

        Theme.SetIcon(btnCopy, Theme.Glyph.Copy);
        Theme.SetIcon(btnActivate, Theme.Glyph.Key);
        Theme.SetIcon(btnExit, Theme.Glyph.Close);
        Shown += (_, _) => Theme.CenterButtons(this);

        AcceptButton = btnActivate;
    }

    private void BtnActivate_Click(object? sender, EventArgs e)
    {
        var key = _txtKey.Text.Trim();
        if (key.Length == 0)
        {
            SetStatus("Please paste the license key first.", Theme.AlertRed);
            return;
        }

        var result = License.Validate(key);
        if (!result.IsValid)
        {
            SetStatus(MessageFor(result.State), Theme.AlertRed);
            return;
        }

        if (!License.Install(key))
        {
            SetStatus("Could not save the license file. Run the application as Administrator.", Theme.AlertRed);
            return;
        }

        MessageBox.Show(
            $"Activation successful.{Environment.NewLine}{Environment.NewLine}Licensed to: {result.CustomerName}",
            "Activated", MessageBoxButtons.OK, MessageBoxIcon.Information);

        DialogResult = DialogResult.OK;
        Close();
    }

    private void SetStatus(string text, Color color)
    {
        _lblStatus.Text      = text;
        _lblStatus.ForeColor = color;
    }

    private static string MessageFor(LicenseState state) => state switch
    {
        LicenseState.Missing      => "This copy is not activated yet. Share the Machine ID below with the vendor to receive your license key.",
        LicenseState.Invalid      => "The license key is not valid. Please check that it was copied completely.",
        LicenseState.WrongMachine => "This license key belongs to a different computer. Each license works on one machine only.",
        LicenseState.Expired      => "This license has expired. Please contact the vendor to renew it.",
        _                         => ""
    };

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
        Theme.RoundCorners(btn, 8);
        return btn;
    }
}
