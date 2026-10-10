namespace Inventory
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            Text = "My Store";
            WindowState = FormWindowState.Maximized;
            KeyPreview = true;

            InitializeComponent();
            // Example: read connection string during form initialization
            var conn = AppConfig.ConnectionString; // populated from appsettings.json
            // You can use 'conn' to initialize your DB connection components

            // Who is logged in, and Log out (right side of the menu) back to the login screen
            Text = $"My Store  -  {AppSession.UserName}";
            var logOutItem = new ToolStripMenuItem($"Log out  ({AppSession.UserName})") { Alignment = ToolStripItemAlignment.Right };
            logOutItem.Click += (_, _) => AppSession.LogOut();
            menuStrip1.Items.Add(logOutItem);
            FormClosing += Form1_FormClosing;
        }

        // The cashier closes first, so it can ask about a sale that is still open
        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            foreach (var cashier in Application.OpenForms.OfType<CashierForm>().ToList())
            {
                cashier.Close();
                if (Application.OpenForms.Cast<Form>().Contains(cashier))
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private async void Form1_Load(object sender, EventArgs e) => await ShowCompanyNameAsync();

        // Title: company name - logged-in user
        private async Task ShowCompanyNameAsync()
        {
            Text = $"{await new Repositories.CompanyRepository().GetNameAsync()}  -  {AppSession.UserName}";
        }

        // Settings > Company: the one company record (name, address, email, mobile, VAT number)
        private async void companyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var form = new CompanyForm();
            if (form.ShowDialog(this) == DialogResult.OK) await ShowCompanyNameAsync();
        }

        private void sfToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void itemListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Item > Item list opens the items form (the "Item" menu itself only opens its drop-down)
            var itemForm = new Item();
            itemForm.Show(this);
        }

        private void purchaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Open the purchase invoices form when the menu item is clicked
            var purchaseForm = new PurchaseForm();
            purchaseForm.Show(this);
        }

        // Reports > Daily sales summary
        private void dailySalesReportToolStripMenuItem_Click(object sender, EventArgs e) => OpenReport(new Reports.DailySalesReport());

        // Each report opens in the shared report screen (several reports can be open at once)
        private void OpenReport(Reports.ReportDefinition report)
        {
            var form = new Reports.ReportForm(report);
            form.Show(this);
        }

        // Settings > Currencies & exchange rates: the primary currency, the others and their rates
        private void currenciesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var form = new CurrenciesForm();
            form.ShowDialog(this);
        }

        // Settings > Users: the people who use the system (Manager / User)
        private void usersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var form = new UsersForm();
            form.ShowDialog(this);
        }

        // Item > Quick buttons: the buttons for items without a barcode on the cashier screen
        private void quickButtonsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var form = new QuickButtonsForm();
            form.ShowDialog(this);
        }

        private void cashierToolStripMenuItem_Click(object sender, EventArgs e) => OpenCashier();

        // One cashier screen at a time: bring it to the front if it is already open
        private void OpenCashier()
        {
            var open = Application.OpenForms.OfType<CashierForm>().FirstOrDefault();
            if (open != null)
            {
                if (open.WindowState == FormWindowState.Minimized) open.WindowState = FormWindowState.Maximized;
                open.Activate();
                return;
            }
            new CashierForm().Show();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12)
            {
                OpenCashier();
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

    }
}
