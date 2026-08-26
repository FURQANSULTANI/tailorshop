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
        this.panelHeader.BackColor = Theme.DarkGrey;
        this.panelHeader.Controls.Add(this.lblHeader);
        this.panelHeader.Dock     = DockStyle.Top;
        this.panelHeader.Name     = "panelHeader";
        this.panelHeader.Size     = new Size(720, 56);

        this.lblHeader.AutoSize  = true;
        this.lblHeader.Font      = new Font("Segoe UI", 14f, FontStyle.Bold);
        this.lblHeader.ForeColor = Theme.DarkGold;
        this.lblHeader.Location  = new Point(20, 15);
        this.lblHeader.Name      = "lblHeader";
        this.lblHeader.Text      = "✂  Customer";

        // ── tabControl ───────────────────────────────────────────────
        this.tabControl.Dock     = DockStyle.Fill;
        this.tabControl.Font     = new Font("Segoe UI", 10f);
        this.tabControl.Name     = "tabControl";
        this.tabControl.TabIndex = 0;
        this.tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
        this.tabControl.Controls.Add(this.tabInfo);
        // Measurement tabs are added dynamically in code

        // ── tabInfo ──────────────────────────────────────────────────
        this.tabInfo.BackColor = Theme.NormalGrey;
        this.tabInfo.Name      = "tabInfo";
        this.tabInfo.Padding   = new Padding(15);
        this.tabInfo.Text      = "Customer Info";
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
        this.lblName.ForeColor = Theme.TextOnNormal;
        this.lblName.Location  = new Point(20, 20);
        this.lblName.Name      = "lblName";
        this.lblName.Text      = "Customer Name *";

        // txtName
        this.txtName.BackColor   = Theme.NormalGrey;
        this.txtName.ForeColor   = Theme.TextOnNormal;
        this.txtName.BorderStyle = BorderStyle.FixedSingle;
        this.txtName.Font        = new Font("Segoe UI", 10f);
        this.txtName.Location    = new Point(20, 42);
        this.txtName.Name        = "txtName";
        this.txtName.Size        = new Size(635, 26);
        this.txtName.Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        this.txtName.TabIndex    = 0;

        // lblPhone
        this.lblPhone.AutoSize = true;
        this.lblPhone.Font     = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        this.lblPhone.ForeColor = Theme.TextOnNormal;
        this.lblPhone.Location = new Point(20, 82);
        this.lblPhone.Name     = "lblPhone";
        this.lblPhone.Text     = "Phone Number";

        // txtPhone
        this.txtPhone.BackColor   = Theme.NormalGrey;
        this.txtPhone.ForeColor   = Theme.TextOnNormal;
        this.txtPhone.BorderStyle = BorderStyle.FixedSingle;
        this.txtPhone.Font        = new Font("Segoe UI", 10f);
        this.txtPhone.Location    = new Point(20, 104);
        this.txtPhone.Name        = "txtPhone";
        this.txtPhone.Size        = new Size(635, 26);
        this.txtPhone.Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        this.txtPhone.TabIndex    = 1;

        // lblAddress
        this.lblAddress.AutoSize = true;
        this.lblAddress.Font     = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        this.lblAddress.ForeColor = Theme.TextOnNormal;
        this.lblAddress.Location = new Point(20, 144);
        this.lblAddress.Name     = "lblAddress";
        this.lblAddress.Text     = "Address";

        // txtAddress
        this.txtAddress.BackColor   = Theme.NormalGrey;
        this.txtAddress.ForeColor   = Theme.TextOnNormal;
        this.txtAddress.BorderStyle = BorderStyle.FixedSingle;
        this.txtAddress.Font        = new Font("Segoe UI", 10f);
        this.txtAddress.Location    = new Point(20, 166);
        this.txtAddress.Name        = "txtAddress";
        this.txtAddress.Size        = new Size(635, 26);
        this.txtAddress.Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        this.txtAddress.TabIndex    = 2;

        // lblNotes
        this.lblNotes.AutoSize = true;
        this.lblNotes.Font     = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        this.lblNotes.ForeColor = Theme.TextOnNormal;
        this.lblNotes.Location = new Point(20, 206);
        this.lblNotes.Name     = "lblNotes";
        this.lblNotes.Text     = "Notes";

        // txtNotes
        this.txtNotes.BackColor    = Theme.NormalGrey;
        this.txtNotes.ForeColor    = Theme.TextOnNormal;
        this.txtNotes.BorderStyle  = BorderStyle.FixedSingle;
        this.txtNotes.Font         = new Font("Segoe UI", 9.5f);
        this.txtNotes.Location     = new Point(20, 228);
        this.txtNotes.Multiline    = true;
        this.txtNotes.Name         = "txtNotes";
        this.txtNotes.ScrollBars   = ScrollBars.Vertical;
        this.txtNotes.Size         = new Size(635, 85);
        this.txtNotes.Anchor       = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.txtNotes.TabIndex     = 3;

        // ── panelBottom ──────────────────────────────────────────────
        this.panelBottom.BackColor = Theme.DarkGrey;
        this.panelBottom.Controls.Add(this.btnSave);
        this.panelBottom.Controls.Add(this.btnCancel);
        this.panelBottom.Dock     = DockStyle.Bottom;
        this.panelBottom.Name     = "panelBottom";
        this.panelBottom.Size     = new Size(720, 56);

        // btnSave
        this.btnSave.BackColor = Theme.DarkGold;
        this.btnSave.FlatStyle = FlatStyle.Flat;
        this.btnSave.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
        this.btnSave.ForeColor = Theme.DarkGrey;
        this.btnSave.Location  = new Point(16, 11);
        this.btnSave.Name      = "btnSave";
        this.btnSave.Size      = new Size(155, 34);
        this.btnSave.TabIndex  = 0;
        this.btnSave.Text      = "Save Customer";
        this.btnSave.UseVisualStyleBackColor = false;
        this.btnSave.Cursor    = Cursors.Hand;
        this.btnSave.FlatAppearance.MouseOverBackColor = Theme.Hover(Theme.DarkGold);

        // btnCancel
        this.btnCancel.BackColor    = Theme.NormalGrey;
        this.btnCancel.DialogResult = DialogResult.Cancel;
        this.btnCancel.FlatStyle    = FlatStyle.Flat;
        this.btnCancel.Font         = new Font("Segoe UI", 10f);
        this.btnCancel.ForeColor    = Theme.TextOnNormal;
        this.btnCancel.Location     = new Point(180, 11);
        this.btnCancel.Name         = "btnCancel";
        this.btnCancel.Size         = new Size(90, 34);
        this.btnCancel.TabIndex     = 1;
        this.btnCancel.Text         = "Cancel";
        this.btnCancel.UseVisualStyleBackColor = false;
        this.btnCancel.Cursor       = Cursors.Hand;
        this.btnCancel.FlatAppearance.MouseOverBackColor = Theme.Hover(Theme.NormalGrey);

        // ── Form ─────────────────────────────────────────────────────
        this.AutoScaleDimensions = new SizeF(7f, 15f);
        this.AutoScaleMode       = AutoScaleMode.Font;
        this.BackColor           = Theme.NormalGrey;
        this.CancelButton        = this.btnCancel;
        this.ClientSize          = new Size(1020, 760);
        this.Controls.Add(this.tabControl);
        this.Controls.Add(this.panelBottom);
        this.Controls.Add(this.panelHeader);
        this.Font                = new Font("Segoe UI", 9.5f);
        this.FormBorderStyle     = FormBorderStyle.Sizable;
        this.MaximizeBox         = true;
        this.MinimumSize         = new Size(800, 600);
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
