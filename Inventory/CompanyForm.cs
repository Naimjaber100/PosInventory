using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Settings > Company: the one company record (name, address, email, mobile number, VAT number, logo).
    // The name is shown on the login screen, the cashier screen and the back office.
    // The logo prints at the top of the sale receipt (made small and grey for the receipt printer when chosen).
    public partial class CompanyForm : Form
    {
        private readonly CompanyRepository _companyRepository = new();
        private readonly PictureBox _logoPicture;
        private readonly Button _removeLogoButton;
        private byte[]? _logo;          // what is shown (PNG, CompanyLogo.Prepare)
        private bool _logoChanged;

        public CompanyForm()
        {
            InitializeComponent();

            Validation.InputRules.UsePhoneInput(mobileText);

            // Logo: in the free last row of the fields, with Choose / Remove
            _logoPicture = new PictureBox
            {
                Size = new Size(170, 90),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Margin = new Padding(3, 6, 10, 3),
            };
            var chooseButton = new Button { Text = "Choose picture...", Size = new Size(140, 34), Margin = new Padding(0, 6, 0, 4) };
            chooseButton.Click += (_, _) => ChooseLogo();
            _removeLogoButton = new Button { Text = "Remove", Size = new Size(140, 34), ForeColor = Color.Firebrick, Margin = new Padding(0, 0, 0, 4) };
            _removeLogoButton.Click += (_, _) => SetLogo(null);
            var hint = new Label { Text = "On the receipt", AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(0, 2, 0, 0) };
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = Padding.Empty };
            buttons.Controls.AddRange(new Control[] { chooseButton, _removeLogoButton, hint });
            var logoRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
            logoRow.Controls.AddRange(new Control[] { _logoPicture, buttons });
            fieldsTable.Controls.Add(new Label { Text = "Logo", Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) }, 0, 5);
            fieldsTable.Controls.Add(logoRow, 1, 5);
            ClientSize = new Size(ClientSize.Width, ClientSize.Height + 112);
            SetLogo(null, changed: false);

            AppTheme.Apply(this);
            FormClosed += (_, _) => _logoPicture.Image?.Dispose();
        }

        // A picture file: made ready for the receipt printer at once, so what shows is what prints
        private void ChooseLogo()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Company logo",
                Filter = "Pictures (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                SetLogo(Printing.CompanyLogo.Prepare(dialog.FileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "This file could not be used as a logo:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SetLogo(byte[]? logo, bool changed = true)
        {
            _logo = logo;
            _logoChanged |= changed;
            var old = _logoPicture.Image;
            _logoPicture.Image = Printing.CompanyLogo.ToImage(logo);
            old?.Dispose();
            _removeLogoButton.Enabled = logo != null;
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
                try { SetLogo(await _companyRepository.GetLogoAsync(), changed: false); }
                catch { /* company_functions.sql not applied yet: no logo */ }
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
            if (Validation.InputRules.Check(nameText, addressText, mobileText) is { } rule)
            {
                MessageBox.Show(this, rule.Problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                rule.Field.Focus();
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
                if (_logoChanged)
                {
                    await _companyRepository.SetLogoAsync(_logo);
                    // This computer's receipts get it at once; a cashier on another computer when its screen opens again
                    if (Printing.ReceiptSettings.Current is { } receipt) receipt.Logo = _logo;
                }
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
