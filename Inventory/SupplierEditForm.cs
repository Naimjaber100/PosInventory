using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Adds a supplier (none passed) or edits one: name, phone, address, email, VAT number, notes, active.
    // The balance is shown only: posted purchases and payments change it.
    public partial class SupplierEditForm : Form
    {
        private readonly SupplierRepository _supplierRepository = new();
        private readonly SupplierModel? _supplier;

        /// <param name="newName">For a new supplier: the name to start with (e.g. typed on a purchase).</param>
        public SupplierEditForm(SupplierModel? supplier, CurrencySet currencies, string? newName = null)
        {
            InitializeComponent();
            AppTheme.Apply(this);
            _supplier = supplier;

            if (supplier != null)
            {
                Text = $"Edit supplier - {supplier.Name}";
                nameText.Text = supplier.Name;
                phoneText.Text = supplier.Phone;
                addressText.Text = supplier.Address;
                emailText.Text = supplier.Email;
                vatText.Text = supplier.VatNumber;
                notesText.Text = supplier.Notes;
                activeCheck.Checked = supplier.IsActive;
                balanceLabel.Text = supplier.BalanceLbp == 0 ? "Nothing" : currencies.FormatPrimary(supplier.BalanceLbp);
                if (supplier.BalanceLbp > 0) balanceLabel.ForeColor = Color.DarkOrange;
            }
            else
            {
                Text = "Add supplier";
                nameText.Text = newName?.Trim();
                balanceLabel.Text = "Nothing (new supplier)";
            }
        }

        /// <summary>The id of the saved supplier.</summary>
        public int SavedId { get; private set; }

        private async void saveButton_Click(object? sender, EventArgs e)
        {
            var name = nameText.Text.Trim();
            var email = emailText.Text.Trim();

            string? problem = null;
            Control? field = null;
            if (name.Length == 0) (problem, field) = ("The name is required.", nameText);
            else if (email.Length > 0 && !CustomerEditForm.IsEmail(email)) (problem, field) = ("The email address is not valid.", emailText);
            if (problem != null)
            {
                MessageBox.Show(this, problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                field?.Focus();
                return;
            }

            var supplier = new SupplierModel
            {
                SupplierId = _supplier?.SupplierId ?? 0,
                Name = name,
                Phone = phoneText.Text.Trim(),
                Address = addressText.Text.Trim(),
                Email = email,
                VatNumber = vatText.Text.Trim(),
                Notes = notesText.Text.Trim(),
                IsActive = activeCheck.Checked,
            };

            try
            {
                saveButton.Enabled = false;
                SavedId = await _supplierRepository.SaveAsync(supplier);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                var message = ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message;
                MessageBox.Show(this, "Could not save the supplier:\n" + message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                saveButton.Enabled = true;
            }
        }
    }
}
