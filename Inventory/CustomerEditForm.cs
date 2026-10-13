using System.Globalization;
using System.Net.Mail;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Adds a client (none passed) or edits one: name, phone, address, email, credit limit, notes, active.
    // The balance is shown only: sales on account and payments change it.
    public partial class CustomerEditForm : Form
    {
        private readonly CustomerRepository _customerRepository = new();
        private readonly CustomerModel? _customer;
        private readonly CurrencySet _currencies;

        public CustomerEditForm(CustomerModel? customer, CurrencySet currencies)
        {
            InitializeComponent();
            AppTheme.Apply(this);
            _customer = customer;
            _currencies = currencies;
            limitCaption.Text = $"Credit limit ({currencies.Primary.Code})";

            if (customer != null)
            {
                Text = $"Edit client - {customer.Name}";
                nameText.Text = customer.Name;
                phoneText.Text = customer.Phone;
                addressText.Text = customer.Address;
                emailText.Text = customer.Email;
                notesText.Text = customer.Notes;
                activeCheck.Checked = customer.IsActive;
                noLimitCheck.Checked = customer.CreditLimitLbp == null;
                if (customer.CreditLimitLbp is decimal limit) limitText.Text = _currencies.FormatPrimary(limit);
                balanceLabel.Text = customer.BalanceLbp == 0 ? "Nothing" : _currencies.FormatPrimary(customer.BalanceLbp);
                if (customer.BalanceLbp > 0) balanceLabel.ForeColor = Color.DarkOrange;
            }
            else
            {
                Text = "Add client";
                noLimitCheck.Checked = true;
                balanceLabel.Text = "Nothing (new client)";
            }
            limitText.Enabled = !noLimitCheck.Checked;
        }

        /// <summary>The id of the saved client.</summary>
        public int SavedId { get; private set; }

        private void noLimitCheck_CheckedChanged(object? sender, EventArgs e)
        {
            limitText.Enabled = !noLimitCheck.Checked;
            if (limitText.Enabled) limitText.Focus();
        }

        private async void saveButton_Click(object? sender, EventArgs e)
        {
            var name = nameText.Text.Trim();
            var email = emailText.Text.Trim();
            decimal? limit = null;

            string? problem = null;
            Control? field = null;
            if (name.Length == 0) (problem, field) = ("The name is required.", nameText);
            else if (email.Length > 0 && !IsEmail(email)) (problem, field) = ("The email address is not valid.", emailText);
            else if (!noLimitCheck.Checked)
            {
                if (TryParseAmount(limitText.Text, out var value) && value >= 0) limit = value;
                else (problem, field) = ("Type the credit limit (0 or more), or tick No limit.", limitText);
            }
            if (problem != null)
            {
                MessageBox.Show(this, problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                field?.Focus();
                return;
            }

            var customer = new CustomerModel
            {
                CustomerId = _customer?.CustomerId ?? 0,
                Name = name,
                Phone = phoneText.Text.Trim(),
                Address = addressText.Text.Trim(),
                Email = email,
                CreditLimitLbp = limit,
                Notes = notesText.Text.Trim(),
                IsActive = activeCheck.Checked,
            };

            try
            {
                saveButton.Enabled = false;
                SavedId = await _customerRepository.SaveAsync(customer);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                var message = ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message;
                MessageBox.Show(this, "Could not save the client:\n" + message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                saveButton.Enabled = true;
            }
        }

        // Accepts "1,500,000", "1500000", "1 500 000 LBP"
        internal static bool TryParseAmount(string text, out decimal value)
        {
            var digits = new string(text.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
            return decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        internal static bool IsEmail(string text) =>
            MailAddress.TryCreate(text, out var address) && address.Address == text && text.Contains('.', StringComparison.Ordinal);

        private void addressText_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
