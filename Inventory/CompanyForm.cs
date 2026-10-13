using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Settings > Company: the one company record (name, address, email, mobile number, VAT number).
    // The name is shown on the login screen, the cashier screen and the back office.
    public partial class CompanyForm : Form
    {
        private readonly CompanyRepository _companyRepository = new();

        public CompanyForm()
        {
            InitializeComponent();
            AppTheme.Apply(this);
        }

        private async void CompanyForm_Load(object? sender, EventArgs e)
        {
            try
            {
                saveButton.Enabled = false;
                var company = await _companyRepository.GetAsync();
                nameText.Text = company.Name;
                addressText.Text = company.Address;
                emailText.Text = company.Email;
                mobileText.Text = company.Mobile;
                vatText.Text = company.VatNumber;
                saveButton.Enabled = true;
                nameText.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the company:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void saveButton_Click(object? sender, EventArgs e)
        {
            var company = new CompanyModel
            {
                Name = nameText.Text.Trim(),
                Address = addressText.Text.Trim(),
                Email = emailText.Text.Trim(),
                Mobile = mobileText.Text.Trim(),
                VatNumber = vatText.Text.Trim(),
            };

            if (company.Name.Length == 0)
            {
                MessageBox.Show(this, "The company name is required.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nameText.Focus();
                return;
            }
            if (company.Email.Length > 0 && !System.Net.Mail.MailAddress.TryCreate(company.Email, out _))
            {
                MessageBox.Show(this, "The email address is not valid.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                emailText.Focus();
                return;
            }

            try
            {
                saveButton.Enabled = false;
                await _companyRepository.SaveAsync(company);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save the company:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                saveButton.Enabled = true;
            }
        }
    }
}
