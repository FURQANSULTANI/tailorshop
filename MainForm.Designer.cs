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
        panelHeader.BackColor = Color.FromArgb(30, 25, 10);
        panelHeader.Controls.Add(lblTitle);
        panelHeader.Controls.Add(lblSubtitle);
        panelHeader.Dock = DockStyle.Top;
        panelHeader.Location = new Point(0, 0);
        panelHeader.Name = "panelHeader";
        panelHeader.Size = new Size(960, 77);
        panelHeader.TabIndex = 0;
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        lblTitle.ForeColor = Color.FromArgb(184, 134, 11);
        lblTitle.Location = new Point(18, 12);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(288, 41);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "✂Golden Tailor";
        lblTitle.Click += lblTitle_Click;
        // 
        // lblSubtitle
        // 
        lblSubtitle.AutoSize = true;
        lblSubtitle.Font = new Font("Segoe UI", 9F);
        lblSubtitle.ForeColor = Color.FromArgb(200, 185, 130);
        lblSubtitle.Location = new Point(18, 53);
        lblSubtitle.Name = "lblSubtitle";
        lblSubtitle.Size = new Size(223, 20);
        lblSubtitle.TabIndex = 1;
        lblSubtitle.Text = "Customer Measurement Records";
        // 
        // panelSearch
        // 
        panelSearch.BackColor = Color.FromArgb(255, 248, 215);
        panelSearch.Controls.Add(lblSearch);
        panelSearch.Controls.Add(txtSearch);
        panelSearch.Controls.Add(btnSearch);
        panelSearch.Controls.Add(btnClear);
        panelSearch.Dock = DockStyle.Top;
        panelSearch.Location = new Point(0, 77);
        panelSearch.Name = "panelSearch";
        panelSearch.Size = new Size(960, 58);
        panelSearch.TabIndex = 1;
        // 
        // lblSearch
        // 
        lblSearch.AutoSize = true;
        lblSearch.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblSearch.ForeColor = Color.FromArgb(30, 25, 10);
        lblSearch.Location = new Point(14, 17);
        lblSearch.Name = "lblSearch";
        lblSearch.Size = new Size(102, 23);
        lblSearch.TabIndex = 0;
        lblSearch.Text = "🔍  Search:";
        // 
        // txtSearch
        // 
        txtSearch.Font = new Font("Segoe UI", 10F);
        txtSearch.Location = new Point(140, 14);
        txtSearch.Name = "txtSearch";
        txtSearch.PlaceholderText = " Naam ya phone number...";
        txtSearch.Size = new Size(270, 30);
        txtSearch.TabIndex = 0;
        txtSearch.TextChanged += txtSearch_TextChanged;
        // 
        // btnSearch
        // 
        btnSearch.BackColor = Color.FromArgb(184, 134, 11);
        btnSearch.Cursor = Cursors.Hand;
        btnSearch.FlatStyle = FlatStyle.Flat;
        btnSearch.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnSearch.ForeColor = Color.FromArgb(30, 25, 10);
        btnSearch.Location = new Point(420, 13);
        btnSearch.Name = "btnSearch";
        btnSearch.Size = new Size(90, 32);
        btnSearch.TabIndex = 1;
        btnSearch.Text = "Search";
        btnSearch.UseVisualStyleBackColor = false;
        // 
        // btnClear
        // 
        btnClear.BackColor = Color.FromArgb(150, 140, 100);
        btnClear.Cursor = Cursors.Hand;
        btnClear.FlatStyle = FlatStyle.Flat;
        btnClear.Font = new Font("Segoe UI", 9.5F);
        btnClear.ForeColor = Color.White;
        btnClear.Location = new Point(518, 13);
        btnClear.Name = "btnClear";
        btnClear.Size = new Size(80, 32);
        btnClear.TabIndex = 2;
        btnClear.Text = "Clear";
        btnClear.UseVisualStyleBackColor = false;
        // 
        // panelBottom
        // 
        panelBottom.BackColor = Color.FromArgb(255, 248, 215);
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
        btnAdd.BackColor = Color.FromArgb(34, 120, 34);
        btnAdd.Cursor = Cursors.Hand;
        btnAdd.FlatStyle = FlatStyle.Flat;
        btnAdd.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnAdd.ForeColor = Color.White;
        btnAdd.Location = new Point(14, 12);
        btnAdd.Name = "btnAdd";
        btnAdd.Size = new Size(155, 33);
        btnAdd.TabIndex = 0;
        btnAdd.Text = "➕  New Customer";
        btnAdd.UseVisualStyleBackColor = false;
        // 
        // btnEdit
        // 
        btnEdit.BackColor = Color.FromArgb(184, 134, 11);
        btnEdit.Cursor = Cursors.Hand;
        btnEdit.FlatStyle = FlatStyle.Flat;
        btnEdit.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnEdit.ForeColor = Color.FromArgb(30, 25, 10);
        btnEdit.Location = new Point(178, 12);
        btnEdit.Name = "btnEdit";
        btnEdit.Size = new Size(100, 33);
        btnEdit.TabIndex = 1;
        btnEdit.Text = "✏️  Edit";
        btnEdit.UseVisualStyleBackColor = false;
        // 
        // btnDelete
        // 
        btnDelete.BackColor = Color.FromArgb(180, 30, 30);
        btnDelete.Cursor = Cursors.Hand;
        btnDelete.FlatStyle = FlatStyle.Flat;
        btnDelete.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnDelete.ForeColor = Color.White;
        btnDelete.Location = new Point(287, 12);
        btnDelete.Name = "btnDelete";
        btnDelete.Size = new Size(100, 33);
        btnDelete.TabIndex = 2;
        btnDelete.Text = "🗑️  Delete";
        btnDelete.UseVisualStyleBackColor = false;
        // 
        // lblStatus
        // 
        lblStatus.AutoSize = true;
        lblStatus.Font = new Font("Segoe UI", 9.5F);
        lblStatus.ForeColor = Color.FromArgb(100, 90, 50);
        lblStatus.Location = new Point(410, 19);
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
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.ColumnHeadersHeight = 38;
        grid.Columns.AddRange(new DataGridViewColumn[] { colId, colName, colPhone, colAddress, colDate });
        grid.Dock = DockStyle.Fill;
        grid.GridColor = Color.FromArgb(230, 215, 160);
        grid.Location = new Point(0, 135);
        grid.MultiSelect = false;
        grid.Name = "grid";
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.RowHeadersWidth = 51;
        grid.RowTemplate.Height = 32;
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
        BackColor = Color.FromArgb(255, 252, 235);
        ClientSize = new Size(960, 620);
        Controls.Add(grid);
        Controls.Add(panelBottom);
        Controls.Add(panelSearch);
        Controls.Add(panelHeader);
        Font = new Font("Segoe UI", 9.5F);
        MinimumSize = new Size(800, 500);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Golden Tailor — Customer Management";
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
