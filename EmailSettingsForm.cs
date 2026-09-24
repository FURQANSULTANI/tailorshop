namespace TailorShop;

public class EmailSettingsForm : Form
{
    private readonly EmailSettings _settings;
    private readonly TextBox  _txtHost;
    private readonly TextBox  _txtPort;
    private readonly TextBox  _txtUser;
    private readonly TextBox  _txtPass;
    private readonly TextBox  _txtFrom;
    private readonly TextBox  _txtTo;
    private readonly CheckBox _chkSsl;
    private readonly Label    _lblStatus;

    public EmailSettingsForm()
    {
        _settings = EmailSettings.Load();

        Text            = "Email Settings";
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ClientSize      = new Size(580, 476);
        BackColor       = Theme.NormalGrey;
        Font            = new Font("Segoe UI", 10f);

        var header = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Theme.DarkGrey };
        header.Controls.Add(new Label
        {
            Text      = "Email Settings",
            ForeColor = Theme.TextOnDark,
            Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
            AutoSize  = true,
            Location  = new Point(22, 10)
        });
        header.Controls.Add(new Label
        {
            Text      = "Backups can be emailed as a zip file (up to 25 MB)",
            ForeColor = Theme.TextOnDark,
            Font      = new Font("Segoe UI", 8.5f),
            AutoSize  = true,
            Location  = new Point(24, 36)
        });
        header.Controls.Add(Theme.AccentDivider(DockStyle.Bottom));
        Controls.Add(header);

        int y = 80;
        _txtHost = AddField("Mail Server",     _settings.Host, ref y);
        _txtPort = AddField("Port",            _settings.Port.ToString(), ref y);
        _txtUser = AddField("Email Address",   _settings.Username, ref y);
        _txtPass = AddField("Password",        _settings.Password, ref y);
        _txtFrom = AddField("Sender Name",     _settings.FromName, ref y);
        _txtTo   = AddField("Send Backup To",  _settings.SendTo, ref y);

        _txtPass.UseSystemPasswordChar = true;
        _txtPort.KeyPress += (_, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };

        _chkSsl = new CheckBox
        {
            Text      = "Use SSL / TLS",
            Checked   = _settings.UseSsl,
            Font      = new Font("Segoe UI", 9.5f),
            ForeColor = Theme.TextInk,
            AutoSize  = true,
            Cursor    = Cursors.Hand,
            Location  = new Point(180, y)
        };
        Controls.Add(_chkSsl);
        y += 32;

        Controls.Add(new Label
        {
            Text      = "Gmail users: turn on 2-Step Verification, then create an App Password "
                      + "at myaccount.google.com and use that here — not your normal password.",
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(120, Theme.TextInk),
            Location  = new Point(24, y),
            Size      = new Size(532, 34)
        });
        y += 40;

        _lblStatus = new Label
        {
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Theme.TextInk,
            Location  = new Point(24, y),
            Size      = new Size(532, 20)
        };
        Controls.Add(_lblStatus);
        y += 26;

        var btnTest = MakeButton("Test Connection", Theme.DarkGold, Theme.TextOnGold, new Point(24, y), new Size(190, 38), Theme.Glyph.Cloud);
        btnTest.Click += BtnTest_Click;
        Controls.Add(btnTest);

        var btnSave = MakeButton("Save", Theme.DarkGrey, Theme.TextOnDark, new Point(226, y), new Size(140, 38), Theme.Glyph.Save);
        btnSave.Click += BtnSave_Click;
        Controls.Add(btnSave);

        var btnCancel = MakeButton("Cancel", Theme.NormalGrey, Theme.TextOnNormal, new Point(378, y), new Size(130, 38), Theme.Glyph.Cancel);
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(btnCancel);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
        Shown += (_, _) => Theme.CenterButtons(this);
    }

    private TextBox AddField(string label, string value, ref int y)
    {
        Controls.Add(new Label
        {
            Text      = label,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Theme.TextInk,
            AutoSize  = true,
            Location  = new Point(24, y + 5)
        });

        var holder = new Panel
        {
            BackColor = Theme.DarkGold,
            Location  = new Point(180, y),
            Size      = new Size(376, 30)
        };
        var box = new TextBox
        {
            Text        = value,
            Font        = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.None,
            BackColor   = Color.White,
            ForeColor   = Theme.TextInk,
            Location    = new Point(4, 5),
            Width       = 368
        };
        holder.Controls.Add(box);
        Controls.Add(holder);

        y += 42;
        return box;
    }

    private EmailSettings Collect()
    {
        int.TryParse(_txtPort.Text.Trim(), out var port);
        return new EmailSettings
        {
            Host     = _txtHost.Text.Trim(),
            Port     = port == 0 ? 587 : port,
            UseSsl   = _chkSsl.Checked,
            Username = _txtUser.Text.Trim(),
            Password = _txtPass.Text,
            FromName = _txtFrom.Text.Trim().Length == 0 ? "Golden Tailor Backup" : _txtFrom.Text.Trim(),
            SendTo   = _txtTo.Text.Trim()
        };
    }

    private void BtnTest_Click(object? sender, EventArgs e)
    {
        SetStatus("Testing connection...", Theme.DarkGrey);
        UseWaitCursor = true;
        Enabled       = false;

        var settings = Collect();
        Task.Run(() =>
        {
            var result = EmailBackup.TestConnection(settings);
            try
            {
                BeginInvoke(() =>
                {
                    Enabled       = true;
                    UseWaitCursor = false;
                    Cursor.Current = Cursors.Default;
                    if (result.Success)
                    {
                        settings.Save();
                        SetStatus("Settings work and have been saved. Now use \"Email Backup\".", Theme.DarkGrey);
                    }
                    else
                    {
                        SetStatus(result.Error ?? "Could not send.", Theme.AlertRed);
                    }
                });
            }
            catch
            {
            }
        });
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        var settings = Collect();
        if (!settings.Save())
        {
            SetStatus("Could not save the settings file.", Theme.AlertRed);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void SetStatus(string text, Color color)
    {
        _lblStatus.Text      = text;
        _lblStatus.ForeColor = color;
    }

    private static Button MakeButton(string text, Color back, Color fore, Point location, Size size, Theme.Glyph glyph)
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
            Cursor    = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btn.FlatAppearance.BorderSize  = 2;
        btn.FlatAppearance.BorderColor = Theme.DarkGold;
        btn.FlatAppearance.MouseOverBackColor = Theme.Hover(back);
        Theme.SetIcon(btn, glyph);
        Theme.RoundCorners(btn, 6);
        return btn;
    }
}
