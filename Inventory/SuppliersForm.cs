using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Contacts > Suppliers: the suppliers table. "We owe" comes from posted purchases and payments, never typed in.
    // Delete removes a supplier without history; one with purchases or payments is deactivated instead.
    public partial class SuppliersForm : Form
    {
        private readonly SupplierRepository _supplierRepository = new();
        private List<SupplierModel> _all = new();
        private List<SupplierModel> _shown = new();
        private CurrencySet _currencies = CurrencySet.Default;

        public SuppliersForm()
        {
            InitializeComponent();
            AppTheme.Apply(this);
        }

        private async void SuppliersForm_Load(object? sender, EventArgs e)
        {
            try { _currencies = await new CurrencyRepository().GetSetAsync(); }
            catch { /* CurrencySet.Default formats the balances */ }
            await LoadAsync();
            searchText.Focus();
        }

        private async Task LoadAsync(int? selectId = null)
        {
            selectId ??= Selected?.SupplierId;
            try
            {
                _all = (await _supplierRepository.GetAllAsync(null, includeInactive: true)).ToList();
                ShowList(selectId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the suppliers:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Filters the loaded list by the search box and Show inactive (fast: no trip to the database)
        private void ShowList(int? selectId = null)
        {
            selectId ??= Selected?.SupplierId;
            var search = searchText.Text.Trim();
            _shown = _all.Where(s => (inactiveCheck.Checked || s.IsActive) &&
                                     (search.Length == 0 || Contains(s.Name, search) || Contains(s.Phone, search) ||
                                      Contains(s.Email, search) || Contains(s.VatNumber, search)))
                         .ToList();
            listGrid.DataSource = _shown;

            var active = _all.Where(s => s.IsActive).ToList();
            var owed = active.Sum(s => Math.Max(s.BalanceLbp, 0));
            countLabel.Text = $"{active.Count} active supplier(s)  ·  we owe {_currencies.FormatPrimary(owed)}";

            if (_shown.FindIndex(s => s.SupplierId == selectId) is var index and >= 0)
                listGrid.CurrentCell = listGrid.Rows[index].Cells.Cast<DataGridViewCell>().First(c => c.Visible);
        }

        private static bool Contains(string? value, string search) =>
            value != null && value.Contains(search, StringComparison.CurrentCultureIgnoreCase);

        private SupplierModel? Selected => listGrid.CurrentRow?.DataBoundItem as SupplierModel;

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
            using var form = new SupplierEditForm(null, _currencies);
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
            if (Selected is not { } supplier) return;
            using var form = new SupplierEditForm(supplier, _currencies);
            if (form.ShowDialog(this) != DialogResult.OK) return;
            await LoadAsync(supplier.SupplierId);
        }

        private async void deleteButton_Click(object? sender, EventArgs e)
        {
            if (Selected is not { } supplier) return;
            var balance = supplier.BalanceLbp != 0 ? $"\n\nWe still owe this supplier {_currencies.FormatPrimary(supplier.BalanceLbp)}." : "";
            if (MessageBox.Show(this, $"Delete the supplier \"{supplier.Name}\"?{balance}\n\n" +
                                      "A supplier who already has purchases or payments is deactivated instead, to keep the history.",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                var deleted = await _supplierRepository.DeleteAsync(supplier.SupplierId);
                if (!deleted)
                    MessageBox.Show(this, $"\"{supplier.Name}\" has purchases or payments, so the supplier was deactivated instead of deleted.\n" +
                                          "Tick Show inactive to see it again.",
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadAsync(deleted ? null : supplier.SupplierId);
            }
            catch (Exception ex)
            {
                var message = ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message;
                MessageBox.Show(this, "Could not delete the supplier:\n" + message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void closeButton_Click(object? sender, EventArgs e) => Close();

        private void SuppliersForm_KeyDown(object? sender, KeyEventArgs e)
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
            SetWeight(nameof(SupplierModel.Name), 150);
            SetWeight(nameof(SupplierModel.Phone), 80);
            SetWeight(nameof(SupplierModel.Address), 120);
            SetWeight(nameof(SupplierModel.Email), 110);
            SetWeight(nameof(SupplierModel.VatNumber), 80);
            SetWeight(nameof(SupplierModel.Notes), 110);
            SetWeight(nameof(SupplierModel.IsActive), 45);
            if (listGrid.Columns[nameof(SupplierModel.BalanceLbp)] is { } money)
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

        // Amounts in the primary currency; inactive suppliers in grey; what we owe in orange
        private void listGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _shown.Count || e.CellStyle == null) return;
            var supplier = _shown[e.RowIndex];
            if (!supplier.IsActive) e.CellStyle.ForeColor = Color.Gray;

            if (listGrid.Columns[e.ColumnIndex].DataPropertyName == nameof(SupplierModel.BalanceLbp))
            {
                e.Value = supplier.BalanceLbp == 0 ? "-" : _currencies.FormatPrimary(supplier.BalanceLbp);
                e.FormattingApplied = true;
                if (supplier.IsActive && supplier.BalanceLbp > 0) e.CellStyle.ForeColor = Color.DarkOrange;
            }
        }
    }
}
