namespace TailorShop;

partial class CustomerForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.panelHeader = new Panel();
        this.lblHeader   = new Label();
        this.tabControl  = new TabControl();
        this.tabInfo     = new TabPage();
        this.lblName     = new Label();
        this.txtName     = new TextBox();
        this.lblPhone    = new Label();
        this.txtPhone    = new TextBox();
        this.lblAddress  = new Label();
        this.txtAddress  = new TextBox();
        this.lblNotes    = new Label();
        this.txtNotes    = new TextBox();
        this.panelBottom = new Panel();
        this.btnSave     = new Button();
        this.btnCancel   = new Button();

        // ── panelHeader ──────────────────────────────────────────────
        this.panelHeader.BackColor = Color.FromArgb(30, 25, 10);
        this.panelHeader.Controls.Add(this.lblHeader);
        this.panelHeader.Dock     = DockStyle.Top;
        this.panelHeader.Name     = "panelHeader";
        this.panelHeader.Size     = new Size(720, 52);

        this.lblHeader.AutoSize  = true;
        this.lblHeader.Font      = new Font("Segoe UI", 14f, FontStyle.Bold);
        this.lblHeader.ForeColor = Color.FromArgb(184, 134, 11);
        this.lblHeader.Location  = new Point(16, 12);
        this.lblHeader.Name      = "lblHeader";
        this.lblHeader.Text      = "✂  Customer";

        // ── tabControl ───────────────────────────────────────────────
        this.tabControl.Dock     = DockStyle.Fill;
        this.tabControl.Font     = new Font("Segoe UI", 10f);
        this.tabControl.Name     = "tabControl";
        this.tabControl.TabIndex = 0;
        this.tabControl.Controls.Add(this.tabInfo);
        // Measurement tabs are added dynamically in code

        // ── tabInfo ──────────────────────────────────────────────────
        this.tabInfo.BackColor = Color.White;
        this.tabInfo.Name      = "tabInfo";
        this.tabInfo.Padding   = new Padding(15);
        this.tabInfo.Text      = "👤  Customer Info";
        this.tabInfo.Controls.Add(this.lblName);
        this.tabInfo.Controls.Add(this.txtName);
        this.tabInfo.Controls.Add(this.lblPhone);
        this.tabInfo.Controls.Add(this.txtPhone);
        this.tabInfo.Controls.Add(this.lblAddress);
        this.tabInfo.Controls.Add(this.txtAddress);
        this.tabInfo.Controls.Add(this.lblNotes);
        this.tabInfo.Controls.Add(this.txtNotes);

        // lblName
        this.lblName.AutoSize  = true;
        this.lblName.Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        this.lblName.Location  = new Point(20, 20);
        this.lblName.Name      = "lblName";
        this.lblName.Text      = "Customer Name *";

        // txtName
        this.txtName.BorderStyle = BorderStyle.FixedSingle;
        this.txtName.Font        = new Font("Segoe UI", 10f);
        this.txtName.Location    = new Point(20, 42);
        this.txtName.Name        = "txtName";
        this.txtName.Size        = new Size(635, 26);
        this.txtName.TabIndex    = 0;

        // lblPhone
        this.lblPhone.AutoSize = true;
        this.lblPhone.Font     = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        this.lblPhone.Location = new Point(20, 80);
        this.lblPhone.Name     = "lblPhone";
        this.lblPhone.Text     = "Phone Number";

        // txtPhone
        this.txtPhone.BorderStyle = BorderStyle.FixedSingle;
        this.txtPhone.Font        = new Font("Segoe UI", 10f);
        this.txtPhone.Location    = new Point(20, 102);
        this.txtPhone.Name        = "txtPhone";
        this.txtPhone.Size        = new Size(635, 26);
        this.txtPhone.TabIndex    = 1;

        // lblAddress
        this.lblAddress.AutoSize = true;
        this.lblAddress.Font     = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        this.lblAddress.Location = new Point(20, 140);
        this.lblAddress.Name     = "lblAddress";
        this.lblAddress.Text     = "Address";

        // txtAddress
        this.txtAddress.BorderStyle = BorderStyle.FixedSingle;
        this.txtAddress.Font        = new Font("Segoe UI", 10f);
        this.txtAddress.Location    = new Point(20, 162);
        this.txtAddress.Name        = "txtAddress";
        this.txtAddress.Size        = new Size(635, 26);
        this.txtAddress.TabIndex    = 2;

        // lblNotes
        this.lblNotes.AutoSize = true;
        this.lblNotes.Font     = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        this.lblNotes.Location = new Point(20, 205);
        this.lblNotes.Name     = "lblNotes";
        this.lblNotes.Text     = "Notes";

        // txtNotes
        this.txtNotes.BorderStyle  = BorderStyle.FixedSingle;
        this.txtNotes.Font         = new Font("Segoe UI", 9.5f);
        this.txtNotes.Location     = new Point(20, 227);
        this.txtNotes.Multiline    = true;
        this.txtNotes.Name         = "txtNotes";
        this.txtNotes.ScrollBars   = ScrollBars.Vertical;
        this.txtNotes.Size         = new Size(635, 85);
        this.txtNotes.TabIndex     = 3;

        // ── panelBottom ──────────────────────────────────────────────
        this.panelBottom.BackColor = Color.FromArgb(255, 248, 215);
        this.panelBottom.Controls.Add(this.btnSave);
        this.panelBottom.Controls.Add(this.btnCancel);
        this.panelBottom.Dock     = DockStyle.Bottom;
        this.panelBottom.Name     = "panelBottom";
        this.panelBottom.Size     = new Size(720, 56);

        // btnSave
        this.btnSave.BackColor = Color.FromArgb(34, 120, 34);
        this.btnSave.FlatStyle = FlatStyle.Flat;
        this.btnSave.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
        this.btnSave.ForeColor = Color.White;
        this.btnSave.Location  = new Point(16, 11);
        this.btnSave.Name      = "btnSave";
        this.btnSave.Size      = new Size(155, 34);
        this.btnSave.TabIndex  = 0;
        this.btnSave.Text      = "💾  Save Customer";
        this.btnSave.UseVisualStyleBackColor = false;
        this.btnSave.Cursor    = Cursors.Hand;

        // btnCancel
        this.btnCancel.BackColor    = Color.FromArgb(150, 140, 100);
        this.btnCancel.DialogResult = DialogResult.Cancel;
        this.btnCancel.FlatStyle    = FlatStyle.Flat;
        this.btnCancel.Font         = new Font("Segoe UI", 10f);
        this.btnCancel.ForeColor    = Color.White;
        this.btnCancel.Location     = new Point(180, 11);
        this.btnCancel.Name         = "btnCancel";
        this.btnCancel.Size         = new Size(90, 34);
        this.btnCancel.TabIndex     = 1;
        this.btnCancel.Text         = "Cancel";
        this.btnCancel.UseVisualStyleBackColor = false;
        this.btnCancel.Cursor       = Cursors.Hand;

        // ── Form ─────────────────────────────────────────────────────
        this.AutoScaleDimensions = new SizeF(7f, 15f);
        this.AutoScaleMode       = AutoScaleMode.Font;
        this.BackColor           = Color.FromArgb(255, 252, 235);
        this.CancelButton        = this.btnCancel;
        this.ClientSize          = new Size(720, 660);
        this.Controls.Add(this.tabControl);
        this.Controls.Add(this.panelBottom);
        this.Controls.Add(this.panelHeader);
        this.Font                = new Font("Segoe UI", 9.5f);
        this.FormBorderStyle     = FormBorderStyle.FixedDialog;
        this.MaximizeBox         = false;
        this.Name                = "CustomerForm";
        this.StartPosition       = FormStartPosition.CenterParent;
    }

    // ── Designer Fields ──────────────────────────────────────────────
    private Panel    panelHeader = null!;
    private Label    lblHeader   = null!;
    private TabControl tabControl= null!;
    private TabPage  tabInfo     = null!;
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
