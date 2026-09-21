namespace TailorShop;

partial class CustomerForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent() {
        panelHeader = new Panel();
        lblHeader = new Label();
        tabControl = new TabControl();
        tabInfo = new TabPage();
        lblSerial = new Label();
        txtSerial = new TextBox();
        lblName = new Label();
        txtName = new TextBox();
        lblPhone = new Label();
        txtPhone = new TextBox();
        lblAddress = new Label();
        txtAddress = new TextBox();
        lblNotes = new Label();
        txtNotes = new TextBox();
        panelBottom = new Panel();
        btnSave = new Button();
        btnCancel = new Button();
        panelHeader.SuspendLayout();
        tabControl.SuspendLayout();
        tabInfo.SuspendLayout();
        panelBottom.SuspendLayout();
        SuspendLayout();
        // 
        // panelHeader
        // 
        panelHeader.BackColor = Theme.DarkGrey;
        panelHeader.Controls.Add(lblHeader);
        panelHeader.Dock = DockStyle.Top;
        panelHeader.Location = new Point(0, 0);
        panelHeader.Name = "panelHeader";
        panelHeader.Size = new Size(1020, 56);
        panelHeader.TabIndex = 2;
        //
        // lblHeader
        //
        lblHeader.AutoSize = true;
        lblHeader.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblHeader.ForeColor = Theme.TextOnDark;
        lblHeader.Location = new Point(20, 15);
        lblHeader.Name = "lblHeader";
        lblHeader.Size = new Size(172, 32);
        lblHeader.TabIndex = 0;
        lblHeader.Text = "✂  Customer";
        // 
        // tabControl
        // 
        tabControl.Controls.Add(tabInfo);
        tabControl.Dock = DockStyle.Fill;
        tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabControl.Font = new Font("Segoe UI", 10F);
        tabControl.Location = new Point(0, 56);
        tabControl.Name = "tabControl";
        tabControl.SelectedIndex = 0;
        tabControl.Size = new Size(1020, 648);
        tabControl.TabIndex = 0;
        // 
        // tabInfo
        // 
        tabInfo.BackColor = Theme.NormalGrey;
        tabInfo.Controls.Add(lblSerial);
        tabInfo.Controls.Add(txtSerial);
        tabInfo.Controls.Add(lblName);
        tabInfo.Controls.Add(txtName);
        tabInfo.Controls.Add(lblPhone);
        tabInfo.Controls.Add(txtPhone);
        tabInfo.Controls.Add(lblAddress);
        tabInfo.Controls.Add(txtAddress);
        tabInfo.Controls.Add(lblNotes);
        tabInfo.Controls.Add(txtNotes);
        tabInfo.Location = new Point(4, 32);
        tabInfo.Name = "tabInfo";
        tabInfo.Padding = new Padding(15);
        tabInfo.Size = new Size(1012, 612);
        tabInfo.TabIndex = 0;
        tabInfo.Text = "Customer Info";
        // 
        // lblSerial
        // 
        lblSerial.AutoSize = true;
        lblSerial.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblSerial.ForeColor = Theme.TextInk;
        lblSerial.Location = new Point(20, 20);
        lblSerial.Name = "lblSerial";
        lblSerial.Size = new Size(120, 21);
        lblSerial.TabIndex = 0;
        lblSerial.Text = "Serial Number";
        // 
        // txtSerial
        // 
        txtSerial.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtSerial.BackColor = Theme.NormalGrey;
        txtSerial.BorderStyle = BorderStyle.FixedSingle;
        txtSerial.Font = new Font("Segoe UI", 10F);
        txtSerial.ForeColor = Theme.TextInk;
        txtSerial.Location = new Point(20, 42);
        txtSerial.Name = "txtSerial";
        txtSerial.Size = new Size(974, 30);
        txtSerial.TabIndex = 0;
        // 
        // lblName
        // 
        lblName.AutoSize = true;
        lblName.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblName.ForeColor = Theme.TextInk;
        lblName.Location = new Point(20, 82);
        lblName.Name = "lblName";
        lblName.Size = new Size(144, 21);
        lblName.TabIndex = 1;
        lblName.Text = "Customer Name *";
        // 
        // txtName
        // 
        txtName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtName.BackColor = Theme.NormalGrey;
        txtName.BorderStyle = BorderStyle.FixedSingle;
        txtName.Font = new Font("Segoe UI", 10F);
        txtName.ForeColor = Theme.TextInk;
        txtName.Location = new Point(20, 104);
        txtName.Name = "txtName";
        txtName.Size = new Size(974, 30);
        txtName.TabIndex = 1;
        // 
        // lblPhone
        // 
        lblPhone.AutoSize = true;
        lblPhone.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblPhone.ForeColor = Theme.TextInk;
        lblPhone.Location = new Point(20, 144);
        lblPhone.Name = "lblPhone";
        lblPhone.Size = new Size(126, 21);
        lblPhone.TabIndex = 2;
        lblPhone.Text = "Phone Number";
        // 
        // txtPhone
        // 
        txtPhone.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtPhone.BackColor = Theme.NormalGrey;
        txtPhone.BorderStyle = BorderStyle.FixedSingle;
        txtPhone.Font = new Font("Segoe UI", 10F);
        txtPhone.ForeColor = Theme.TextInk;
        txtPhone.Location = new Point(20, 166);
        txtPhone.Name = "txtPhone";
        txtPhone.Size = new Size(974, 30);
        txtPhone.TabIndex = 2;
        // 
        // lblAddress
        // 
        lblAddress.AutoSize = true;
        lblAddress.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblAddress.ForeColor = Theme.TextInk;
        lblAddress.Location = new Point(20, 206);
        lblAddress.Name = "lblAddress";
        lblAddress.Size = new Size(70, 21);
        lblAddress.TabIndex = 3;
        lblAddress.Text = "Address";
        // 
        // txtAddress
        // 
        txtAddress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtAddress.BackColor = Theme.NormalGrey;
        txtAddress.BorderStyle = BorderStyle.FixedSingle;
        txtAddress.Font = new Font("Segoe UI", 10F);
        txtAddress.ForeColor = Theme.TextInk;
        txtAddress.Location = new Point(20, 228);
        txtAddress.Name = "txtAddress";
        txtAddress.Size = new Size(974, 30);
        txtAddress.TabIndex = 3;
        // 
        // lblNotes
        // 
        lblNotes.AutoSize = true;
        lblNotes.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblNotes.ForeColor = Theme.TextInk;
        lblNotes.Location = new Point(20, 268);
        lblNotes.Name = "lblNotes";
        lblNotes.Size = new Size(55, 21);
        lblNotes.TabIndex = 4;
        lblNotes.Text = "Notes";
        // 
        // txtNotes
        // 
        txtNotes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtNotes.BackColor = Theme.NormalGrey;
        txtNotes.BorderStyle = BorderStyle.FixedSingle;
        txtNotes.Font = new Font("Segoe UI", 9.5F);
        txtNotes.ForeColor = Theme.TextInk;
        txtNotes.Location = new Point(20, 292);
        txtNotes.Multiline = true;
        txtNotes.Name = "txtNotes";
        txtNotes.ScrollBars = ScrollBars.Vertical;
        txtNotes.Size = new Size(974, 302);
        txtNotes.TabIndex = 4;
        // 
        // panelBottom
        // 
        panelBottom.BackColor = Theme.DarkGrey;
        panelBottom.Controls.Add(btnSave);
        panelBottom.Controls.Add(btnCancel);
        panelBottom.Dock = DockStyle.Bottom;
        panelBottom.Location = new Point(0, 704);
        panelBottom.Name = "panelBottom";
        panelBottom.Size = new Size(1020, 56);
        panelBottom.TabIndex = 1;
        // 
        // btnSave
        // 
        btnSave.BackColor = Theme.DarkGrey;
        btnSave.Cursor = Cursors.Hand;
        btnSave.FlatAppearance.BorderColor = Theme.DarkGold;
        btnSave.FlatAppearance.BorderSize = 2;
        btnSave.FlatAppearance.MouseOverBackColor = Theme.Hover(Theme.DarkGrey);
        btnSave.FlatStyle = FlatStyle.Flat;
        btnSave.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnSave.ForeColor = Theme.TextOnDark;
        btnSave.Location = new Point(16, 11);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(155, 34);
        btnSave.TabIndex = 0;
        btnSave.Text = "Save Customer";
        btnSave.UseVisualStyleBackColor = false;
        // 
        // btnCancel
        // 
        btnCancel.BackColor = Theme.NormalGrey;
        btnCancel.Cursor = Cursors.Hand;
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.FlatAppearance.BorderColor = Theme.DarkGold;
        btnCancel.FlatAppearance.BorderSize = 2;
        btnCancel.FlatAppearance.MouseOverBackColor = Theme.Hover(Theme.NormalGrey);
        btnCancel.FlatStyle = FlatStyle.Flat;
        btnCancel.Font = new Font("Segoe UI", 10F);
        btnCancel.ForeColor = Theme.TextInk;
        btnCancel.Location = new Point(180, 11);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(90, 34);
        btnCancel.TabIndex = 1;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = false;
        // 
        // CustomerForm
        // 
        AutoScaleDimensions = new SizeF(9F, 21F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Theme.NormalGrey;
        CancelButton = btnCancel;
        ClientSize = new Size(1020, 760);
        Controls.Add(tabControl);
        Controls.Add(panelBottom);
        Controls.Add(panelHeader);
        Font = new Font("Segoe UI", 9.5F);
        MinimumSize = new Size(800, 600);
        Name = "CustomerForm";
        StartPosition = FormStartPosition.CenterParent;
        panelHeader.ResumeLayout(false);
        panelHeader.PerformLayout();
        tabControl.ResumeLayout(false);
        tabInfo.ResumeLayout(false);
        tabInfo.PerformLayout();
        panelBottom.ResumeLayout(false);
        ResumeLayout(false);
    }

    // ── Designer Fields ──────────────────────────────────────────────
    private Panel    panelHeader = null!;
    private Label    lblHeader   = null!;
    private TabControl tabControl= null!;
    private TabPage  tabInfo     = null!;
    private Label    lblSerial   = null!;
    private TextBox  txtSerial   = null!;
    private Label    lblName     = null!;
    private TextBox  txtName     = null!;
    private Label    lblPhone    = null!;
    private TextBox  txtPhone    = null!;
    private Label    lblAddress  = null!;
    private TextBox  txtAddress  = null!;
    private Label    lblNotes    = null!;
    private TextBox  txtNotes    = null!;
    private Panel    panelBottom = null!;
    private Button   btnSave     = null!;
    private Button   btnCancel   = null!;
}
