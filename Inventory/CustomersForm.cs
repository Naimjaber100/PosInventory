using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Contacts > Clients: the customers table. "Owes us" comes from sales on account and payments, never typed in.
    // Delete removes a client without history; one with sales or payments is deactivated instead.
    public partial class CustomersForm : Form
    {
        private readonly CustomerRepository _customerRepository = new();
        private List<CustomerModel> _all = new();
        private List<CustomerModel> _shown = new();
        private CurrencySet _currencies = CurrencySet.Default;

        public CustomersForm()
        {
            InitializeComponent();
        }

        private async void CustomersForm_Load(object? sender, EventArgs e)
        {
            try { _currencies = await new CurrencyRepository().GetSetAsync(); }
            catch { /* CurrencySet.Default formats the balances */ }
            await LoadAsync();
            searchText.Focus();
        }

        private async Task LoadAsync(int? selectId = null)
        {
            selectId ??= Selected?.CustomerId;
            try
            {
                _all = (await _customerRepository.GetAllAsync(null, includeInactive: true)).ToList();
                ShowList(selectId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the clients:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Filters the loaded list by the search box and Show inactive (fast: no trip to the database)
        private void ShowList(int? selectId = null)
        {
            selectId ??= Selected?.CustomerId;
            var search = searchText.Text.Trim();
            _shown = _all.Where(c => (inactiveCheck.Checked || c.IsActive) &&
                                     (search.Length == 0 || Contains(c.Name, search) || Contains(c.Phone, search) || Contains(c.Email, search)))
                         .ToList();
            listGrid.DataSource = _shown;

            var active = _all.Where(c => c.IsActive).ToList();
            var owed = active.Sum(c => Math.Max(c.BalanceLbp, 0));
            countLabel.Text = $"{active.Count} active client(s)  ·  they owe us {_currencies.FormatPrimary(owed)}";

            if (_shown.FindIndex(c => c.CustomerId == selectId) is var index and >= 0)
                listGrid.CurrentCell = listGrid.Rows[index].Cells.Cast<DataGridViewCell>().First(c => c.Visible);
        }

        private static bool Contains(string? value, string search) =>
            value != null && value.Contains(search, StringComparison.CurrentCultureIgnoreCase);

        private CustomerModel? Selected => listGrid.CurrentRow?.DataBoundItem as CustomerModel;

        private void searchText_TextChanged(object? sender, EventArgs e) => ShowList();

        private void inactiveCheck_CheckedChanged(object? sender, EventArgs e) => ShowList();

        // Down arrow / Enter from the search box go to the list
        private void searchText_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Down or Keys.Enter && listGrid.Rows.Count > 0)
            {
                e.SuppressKeyPress = true;
                listGrid.Focus();
            }
        }

        private async void addButton_Click(object? sender, EventArgs e)
        {
            using var form = new CustomerEditForm(null, _currencies);
            if (form.ShowDialog(this) != DialogResult.OK) return;
            searchText.Clear();
            await LoadAsync(form.SavedId);
        }

        private async void editButton_Click(object? sender, EventArgs e) => await EditSelectedAsync();

        private async void listGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) await EditSelectedAsync();
        }

        private async Task EditSelectedAsync()
        {
            if (Selected is not { } customer) return;
            using var form = new CustomerEditForm(customer, _currencies);
            if (form.ShowDialog(this) != DialogResult.OK) return;
            await LoadAsync(customer.CustomerId);
        }

        private async void deleteButton_Click(object? sender, EventArgs e)
        {
            if (Selected is not { } customer) return;
            var balance = customer.BalanceLbp != 0 ? $"\n\nThis client still has a balance of {_currencies.FormatPrimary(customer.BalanceLbp)}." : "";
            if (MessageBox.Show(this, $"Delete the client \"{customer.Name}\"?{balance}\n\n" +
                                      "A client who already has sales or payments is deactivated instead, to keep the history.",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                var deleted = await _customerRepository.DeleteAsync(customer.CustomerId);
                if (!deleted)
                    MessageBox.Show(this, $"\"{customer.Name}\" has sales or payments, so the client was deactivated instead of deleted.\n" +
                                          "Tick Show inactive to see it again.",
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadAsync(deleted ? null : customer.CustomerId);
            }
            catch (Exception ex)
            {
                var message = ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message;
                MessageBox.Show(this, "Could not delete the client:\n" + message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void closeButton_Click(object? sender, EventArgs e) => Close();

        private void CustomersForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; Close(); }
            else if (e.KeyCode == Keys.Insert) { e.SuppressKeyPress = true; addButton.PerformClick(); }
            else if (e.KeyCode == Keys.F && e.Control) { e.SuppressKeyPress = true; searchText.Focus(); searchText.SelectAll(); }
        }

        private void listGrid_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter: e.SuppressKeyPress = true; editButton.PerformClick(); break;
                case Keys.Delete: deleteButton.PerformClick(); break;
            }
        }

        private void listGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            SetWeight(nameof(CustomerModel.Name), 150);
            SetWeight(nameof(CustomerModel.Phone), 80);
            SetWeight(nameof(CustomerModel.Address), 120);
            SetWeight(nameof(CustomerModel.Email), 110);
            SetWeight(nameof(CustomerModel.Notes), 110);
            SetWeight(nameof(CustomerModel.IsActive), 45);
            foreach (var column in new[] { nameof(CustomerModel.BalanceLbp), nameof(CustomerModel.CreditLimitLbp) })
                if (listGrid.Columns[column] is { } money)
                {
                    money.FillWeight = 90;
                    money.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    money.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
        }

        private void SetWeight(string column, float weight)
        {
            if (listGrid.Columns[column] is { } c) c.FillWeight = weight;
        }

        // Amounts in the primary currency; inactive clients in grey; a balance over the credit limit in red
        private void listGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _shown.Count || e.CellStyle == null) return;
            var customer = _shown[e.RowIndex];
            if (!customer.IsActive) e.CellStyle.ForeColor = Color.Gray;

            switch (listGrid.Columns[e.ColumnIndex].DataPropertyName)
            {
                case nameof(CustomerModel.BalanceLbp):
                    e.Value = customer.BalanceLbp == 0 ? "-" : _currencies.FormatPrimary(customer.BalanceLbp);
                    e.FormattingApplied = true;
                    if (customer.IsActive && customer.CreditLimitLbp is decimal limit && customer.BalanceLbp > limit)
                        e.CellStyle.ForeColor = Color.Firebrick;
                    else if (customer.IsActive && customer.BalanceLbp > 0)
                        e.CellStyle.ForeColor = Color.DarkOrange;
                    break;
                case nameof(CustomerModel.CreditLimitLbp):
                    e.Value = customer.CreditLimitLbp is decimal l ? _currencies.FormatPrimary(l) : "No limit";
                    e.FormattingApplied = true;
                    if (customer.CreditLimitLbp == null && customer.IsActive) e.CellStyle.ForeColor = Color.DimGray;
                    break;
            }
        }
    }
}
