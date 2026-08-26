namespace TailorShop;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent() {
        panelHeader = new Panel();
        lblTitle = new Label();
        lblSubtitle = new Label();
        panelSearch = new Panel();
        lblSearch = new Label();
        txtSearch = new TextBox();
        btnSearch = new Button();
        btnClear = new Button();
        panelBottom = new Panel();
        btnAdd = new Button();
        btnEdit = new Button();
        btnDelete = new Button();
        lblStatus = new Label();
        grid = new DataGridView();
        colId = new DataGridViewTextBoxColumn();
        colName = new DataGridViewTextBoxColumn();
        colPhone = new DataGridViewTextBoxColumn();
        colAddress = new DataGridViewTextBoxColumn();
        colDate = new DataGridViewTextBoxColumn();
        panelHeader.SuspendLayout();
        panelSearch.SuspendLayout();
        panelBottom.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        SuspendLayout();
        // 
        // panelHeader
        // 
        panelHeader.BackColor = Theme.DarkGrey;
        panelHeader.Controls.Add(lblTitle);
        panelHeader.Controls.Add(lblSubtitle);
        panelHeader.Dock = DockStyle.Top;
        panelHeader.Location = new Point(0, 0);
        panelHeader.Name = "panelHeader";
        panelHeader.Size = new Size(960, 80);
        panelHeader.TabIndex = 0;
        //
        // lblTitle
        //
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        lblTitle.ForeColor = Theme.DarkGold;
        lblTitle.Location = new Point(20, 15);
        lblTitle.Name = "lblTitle";
        lblTitle.TabIndex = 0;
        lblTitle.Text = "✂TailorShop";
        lblTitle.Click += lblTitle_Click;
        //
        // lblSubtitle
        //
        lblSubtitle.AutoSize = true;
        lblSubtitle.Font = new Font("Segoe UI", 9.5F);
        lblSubtitle.ForeColor = Theme.TextOnDark;
        lblSubtitle.Location = new Point(21, 56);
        lblSubtitle.Name = "lblSubtitle";
        lblSubtitle.TabIndex = 1;
        lblSubtitle.Text = "Customer Measurement Records";
        // 
        // panelSearch
        // 
        panelSearch.BackColor = Theme.DarkGrey;
        panelSearch.Controls.Add(lblSearch);
        panelSearch.Controls.Add(txtSearch);
        panelSearch.Controls.Add(btnSearch);
        panelSearch.Controls.Add(btnClear);
        panelSearch.Dock = DockStyle.Top;
        panelSearch.Location = new Point(0, 80);
        panelSearch.Name = "panelSearch";
        panelSearch.Size = new Size(960, 58);
        panelSearch.TabIndex = 1;
        //
        // lblSearch
        //
        lblSearch.AutoSize = true;
        lblSearch.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblSearch.ForeColor = Theme.TextOnDark;
        lblSearch.Location = new Point(20, 18);
        lblSearch.Name = "lblSearch";
        lblSearch.TabIndex = 0;
        lblSearch.Text = "Search:";
        //
        // txtSearch
        //
        txtSearch.BackColor = Theme.NormalGrey;
        txtSearch.ForeColor = Theme.TextOnNormal;
        txtSearch.Font = new Font("Segoe UI", 10F);
        txtSearch.Location = new Point(96, 14);
        txtSearch.Name = "txtSearch";
        txtSearch.PlaceholderText = " Naam ya phone number...";
        txtSearch.Size = new Size(314, 30);
        txtSearch.TabIndex = 0;
        txtSearch.TextChanged += txtSearch_TextChanged;
        //
        // btnSearch
        //
        btnSearch.BackColor = Theme.DarkGold;
        btnSearch.Cursor = Cursors.Hand;
        btnSearch.FlatStyle = FlatStyle.Flat;
        btnSearch.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnSearch.ForeColor = Theme.DarkGrey;
        btnSearch.Location = new Point(420, 13);
        btnSearch.Name = "btnSearch";
        btnSearch.Size = new Size(90, 32);
        btnSearch.TabIndex = 1;
        btnSearch.Text = "Search";
        btnSearch.UseVisualStyleBackColor = false;
        btnSearch.FlatAppearance.MouseOverBackColor = Theme.Hover(Theme.DarkGold);
        //
        // btnClear
        //
        btnClear.BackColor = Theme.NormalGrey;
        btnClear.Cursor = Cursors.Hand;
        btnClear.FlatStyle = FlatStyle.Flat;
        btnClear.Font = new Font("Segoe UI", 9.5F);
        btnClear.ForeColor = Theme.TextOnNormal;
        btnClear.Location = new Point(518, 13);
        btnClear.Name = "btnClear";
        btnClear.Size = new Size(80, 32);
        btnClear.TabIndex = 2;
        btnClear.Text = "Clear";
        btnClear.UseVisualStyleBackColor = false;
        btnClear.FlatAppearance.MouseOverBackColor = Theme.Hover(Theme.NormalGrey);
        // 
        // panelBottom
        // 
        panelBottom.BackColor = Theme.DarkGrey;
        panelBottom.Controls.Add(btnAdd);
        panelBottom.Controls.Add(btnEdit);
        panelBottom.Controls.Add(btnDelete);
        panelBottom.Controls.Add(lblStatus);
        panelBottom.Dock = DockStyle.Bottom;
        panelBottom.Location = new Point(0, 564);
        panelBottom.Name = "panelBottom";
        panelBottom.Size = new Size(960, 56);
        panelBottom.TabIndex = 2;
        // 
        // btnAdd
        // 
        btnAdd.BackColor = Theme.DarkGold;
        btnAdd.Cursor = Cursors.Hand;
        btnAdd.FlatStyle = FlatStyle.Flat;
        btnAdd.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnAdd.ForeColor = Theme.DarkGrey;
        btnAdd.Location = new Point(14, 12);
        btnAdd.Name = "btnAdd";
        btnAdd.Size = new Size(155, 33);
        btnAdd.TabIndex = 0;
        btnAdd.Text = "New Customer";
        btnAdd.UseVisualStyleBackColor = false;
        btnAdd.FlatAppearance.MouseOverBackColor = Theme.Hover(Theme.DarkGold);
        //
        // btnEdit
        //
        btnEdit.BackColor = Theme.NormalGrey;
        btnEdit.Cursor = Cursors.Hand;
        btnEdit.FlatStyle = FlatStyle.Flat;
        btnEdit.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnEdit.ForeColor = Theme.TextOnNormal;
        btnEdit.Location = new Point(178, 12);
        btnEdit.Name = "btnEdit";
        btnEdit.Size = new Size(92, 33);
        btnEdit.TabIndex = 1;
        btnEdit.Text = "Edit";
        btnEdit.UseVisualStyleBackColor = false;
        btnEdit.FlatAppearance.MouseOverBackColor = Theme.Hover(Theme.NormalGrey);
        //
        // btnDelete
        //
        btnDelete.BackColor = Theme.DarkGrey;
        btnDelete.Cursor = Cursors.Hand;
        btnDelete.FlatStyle = FlatStyle.Flat;
        btnDelete.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnDelete.ForeColor = Theme.TextOnDark;
        btnDelete.Location = new Point(278, 12);
        btnDelete.Name = "btnDelete";
        btnDelete.Size = new Size(92, 33);
        btnDelete.TabIndex = 2;
        btnDelete.Text = "Delete";
        btnDelete.UseVisualStyleBackColor = false;
        btnDelete.FlatAppearance.BorderSize = 2;
        btnDelete.FlatAppearance.BorderColor = Theme.DarkGold;
        btnDelete.FlatAppearance.MouseOverBackColor = Theme.Hover(Theme.DarkGrey);
        // 
        // lblStatus
        // 
        lblStatus.AutoSize = true;
        lblStatus.Font = new Font("Segoe UI", 9.5F);
        lblStatus.ForeColor = Theme.TextOnDark;
        lblStatus.Location = new Point(392, 19);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(53, 21);
        lblStatus.TabIndex = 3;
        lblStatus.Text = "Ready";
        // 
        // grid
        // 
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = Theme.NormalGrey;
        grid.BorderStyle = BorderStyle.None;
        grid.ColumnHeadersHeight = 42;
        grid.Columns.AddRange(new DataGridViewColumn[] { colId, colName, colPhone, colAddress, colDate });
        grid.Dock = DockStyle.Fill;
        grid.Location = new Point(0, 138);
        grid.MultiSelect = false;
        grid.Name = "grid";
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.RowHeadersWidth = 51;
        grid.RowTemplate.Height = 36;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.Size = new Size(960, 429);
        grid.TabIndex = 0;
        //
        // colId
        //
        colId.FillWeight = 5F;
        colId.HeaderText = "#";
        colId.MinimumWidth = 6;
        colId.Name = "colId";
        colId.ReadOnly = true;
        colId.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        // 
        // colName
        // 
        colName.FillWeight = 30F;
        colName.HeaderText = "Customer Name";
        colName.MinimumWidth = 6;
        colName.Name = "colName";
        colName.ReadOnly = true;
        // 
        // colPhone
        // 
        colPhone.FillWeight = 20F;
        colPhone.HeaderText = "Phone";
        colPhone.MinimumWidth = 6;
        colPhone.Name = "colPhone";
        colPhone.ReadOnly = true;
        // 
        // colAddress
        // 
        colAddress.FillWeight = 30F;
        colAddress.HeaderText = "Address";
        colAddress.MinimumWidth = 6;
        colAddress.Name = "colAddress";
        colAddress.ReadOnly = true;
        // 
        // colDate
        // 
        colDate.FillWeight = 15F;
        colDate.HeaderText = "Added On";
        colDate.MinimumWidth = 6;
        colDate.Name = "colDate";
        colDate.ReadOnly = true;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(9F, 21F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Theme.NormalGrey;
        ClientSize = new Size(960, 620);
        Controls.Add(grid);
        Controls.Add(panelBottom);
        Controls.Add(panelSearch);
        Controls.Add(panelHeader);
        Font = new Font("Segoe UI", 9.5F);
        MinimumSize = new Size(800, 500);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "TailorShop — Customer Management";
        panelHeader.ResumeLayout(false);
        panelHeader.PerformLayout();
        panelSearch.ResumeLayout(false);
        panelSearch.PerformLayout();
        panelBottom.ResumeLayout(false);
        panelBottom.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        ResumeLayout(false);
    }

    // ── Designer Fields ──────────────────────────────────────────────
    private Panel    panelHeader  = null!;
    private Label    lblTitle     = null!;
    private Label    lblSubtitle  = null!;
    private Panel    panelSearch  = null!;
    private Label    lblSearch    = null!;
    private TextBox  txtSearch    = null!;
    private Button   btnSearch    = null!;
    private Button   btnClear     = null!;
    private Panel    panelBottom  = null!;
    private Button   btnAdd       = null!;
    private Button   btnEdit      = null!;
    private Button   btnDelete    = null!;
    private Label    lblStatus    = null!;
    private DataGridView grid     = null!;
    private DataGridViewTextBoxColumn colId      = null!;
    private DataGridViewTextBoxColumn colName    = null!;
    private DataGridViewTextBoxColumn colPhone   = null!;
    private DataGridViewTextBoxColumn colAddress = null!;
    private DataGridViewTextBoxColumn colDate    = null!;
}
