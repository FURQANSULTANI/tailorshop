namespace TailorShop;

public class BackupForm : Form
{
    private readonly Label _lblLast;

    public bool DataRestored { get; private set; }

    public BackupForm()
    {
        Text            = "Backup & Restore";
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ClientSize      = new Size(540, 340);
        BackColor       = Theme.NormalGrey;
        Font            = new Font("Segoe UI", 10f);

        var header = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Theme.DarkGrey };
        header.Controls.Add(new Label
        {
            Text      = "Backup & Restore",
            ForeColor = Theme.TextOnDark,
            Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
            AutoSize  = true,
            Location  = new Point(22, 16)
        });
        Controls.Add(header);

        _lblLast = new Label
        {
            Font      = new Font("Segoe UI", 9.5f),
            ForeColor = Theme.TextInk,
            Location  = new Point(24, 78),
            Size      = new Size(490, 24)
        };
        Controls.Add(_lblLast);
        UpdateLastLabel();

        Controls.Add(new Label
        {
            Text      = "A backup is taken automatically every week. Use the buttons below to save a copy now, or to restore an earlier backup.",
            Font      = new Font("Segoe UI", 9.5f),
            ForeColor = Theme.TextInk,
            Location  = new Point(24, 104),
            Size      = new Size(490, 44)
        });

        var btnNow = MakeButton("Backup Now", Theme.DarkGrey, Theme.TextOnDark, new Point(24, 158), new Size(230, 40));
        btnNow.Click += BtnNow_Click;
        Controls.Add(btnNow);

        var btnCopy = MakeButton("Save Copy To USB / Folder...", Theme.DarkGold, Theme.TextOnGold, new Point(266, 158), new Size(248, 40));
        btnCopy.Click += BtnCopy_Click;
        Controls.Add(btnCopy);

        var btnFolder = MakeButton("Open Backup Folder", Theme.NormalGrey, Theme.TextOnNormal, new Point(24, 210), new Size(230, 40));
        btnFolder.Click += (_, _) =>
        {
            Directory.CreateDirectory(AppPaths.BackupFolder);
            System.Diagnostics.Process.Start("explorer.exe", AppPaths.BackupFolder);
        };
        Controls.Add(btnFolder);

        var btnRestore = MakeButton("Restore From Backup...", Theme.DeleteAccent, Theme.TextOnDeleteAccent, new Point(266, 210), new Size(248, 40));
        btnRestore.Click += BtnRestore_Click;
        Controls.Add(btnRestore);

        var btnClose = MakeButton("Close", Theme.NormalGrey, Theme.TextOnNormal, new Point(266, 272), new Size(248, 36));
        btnClose.Click += (_, _) => Close();
        Controls.Add(btnClose);
    }

    private void BtnNow_Click(object? sender, EventArgs e)
    {
        var result = Backup.Create();
        if (result.Success)
        {
            UpdateLastLabel();
            MessageBox.Show($"Backup saved.{Environment.NewLine}{Environment.NewLine}{result.Path}",
                "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show($"Backup failed.{Environment.NewLine}{Environment.NewLine}{result.Error}",
                "Backup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnCopy_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description  = "Choose where to save the backup copy",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var result = Backup.Create(dialog.SelectedPath);
        MessageBox.Show(
            result.Success
                ? $"Backup saved.{Environment.NewLine}{Environment.NewLine}{result.Path}"
                : $"Backup failed.{Environment.NewLine}{Environment.NewLine}{result.Error}",
            "Backup", MessageBoxButtons.OK,
            result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
    }

    private void BtnRestore_Click(object? sender, EventArgs e)
    {
        Directory.CreateDirectory(AppPaths.BackupFolder);

        using var dialog = new OpenFileDialog
        {
            Title            = "Select a backup file",
            InitialDirectory = AppPaths.BackupFolder,
            Filter           = "Database files (*.db)|*.db|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var confirm = MessageBox.Show(
            "This will replace all current data with the selected backup." + Environment.NewLine + Environment.NewLine +
            "A copy of the current data is kept in the backup folder first." + Environment.NewLine + Environment.NewLine +
            "Continue?",
            "Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

        if (confirm != DialogResult.Yes) return;

        if (Backup.Restore(dialog.FileName, out var error))
        {
            DataRestored = true;
            MessageBox.Show("Data restored successfully.", "Restore",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        else
        {
            MessageBox.Show($"Restore failed.{Environment.NewLine}{Environment.NewLine}{error}",
                "Restore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateLastLabel()
    {
        var last = Backup.LastBackupTime();
        _lblLast.Text = last.HasValue
            ? $"Last automatic backup: {last.Value:dd MMM yyyy, hh:mm tt}"
            : "No automatic backup has run yet.";
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
        Theme.RoundCorners(btn, 8);
        return btn;
    }
}
