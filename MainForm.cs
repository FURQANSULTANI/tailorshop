namespace TailorShop;

public partial class MainForm : Form {
    private List<Customer> _customers = new();

    public MainForm() {
        InitializeComponent();
        ApplyGridStyles();
        WireEvents();
        Database.Initialize();
        LoadCustomers();
    }

    private void ApplyGridStyles() {
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 25, 10);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(184, 134, 11);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 230, 100);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 25, 10);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(255, 252, 230);
    }

    private void WireEvents() {
        btnSearch.Click += (_, _) => LoadCustomers(txtSearch.Text);
        btnClear.Click += (_, _) => { txtSearch.Clear(); LoadCustomers(); };
        btnAdd.Click += BtnAdd_Click;
        btnEdit.Click += BtnEdit_Click;
        btnDelete.Click += BtnDelete_Click;
        grid.CellDoubleClick += (s, e) => BtnEdit_Click(s, e);
        txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadCustomers(txtSearch.Text); };
    }

    private void LoadCustomers(string search = "") {
        _customers = Database.GetAllCustomers(search);
        grid.Rows.Clear();
        foreach (var c in _customers)
            grid.Rows.Add(c.Id, c.Name, c.Phone ?? "-", c.Address ?? "-",
                          c.CreatedAt?.Split(' ')[0] ?? "");
        lblStatus.Text = $"{_customers.Count} customer(s)";
    }

    private Customer? SelectedCustomer() {
        if (grid.CurrentRow == null) return null;
        var id = Convert.ToInt64(grid.CurrentRow.Cells["colId"].Value);
        return _customers.FirstOrDefault(x => x.Id == id);
    }

    private void BtnAdd_Click(object? s, EventArgs e) {
        using var frm = new CustomerForm(null);
        if (frm.ShowDialog() == DialogResult.OK) LoadCustomers(txtSearch.Text);
    }

    private void BtnEdit_Click(object? s, EventArgs e) {
        var c = SelectedCustomer();
        if (c == null) { Info("Pehle ek customer select karein."); return; }
        var full = Database.GetById(c.Id)!;
        using var frm = new CustomerForm(full);
        if (frm.ShowDialog() == DialogResult.OK) LoadCustomers(txtSearch.Text);
    }

    private void BtnDelete_Click(object? s, EventArgs e) {
        var c = SelectedCustomer();
        if (c == null) { Info("Pehle ek customer select karein."); return; }
        if (MessageBox.Show($"'{c.Name}' ko delete karna chahte hain?",
            "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
            Database.DeleteCustomer(c.Id);
            LoadCustomers(txtSearch.Text);
        }
    }

    private static void Info(string msg) =>
        MessageBox.Show(msg, "TopStitch Tailor", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void lblTitle_Click(object sender, EventArgs e) {

    }

    private void txtSearch_TextChanged(object sender, EventArgs e) {

    }
}
