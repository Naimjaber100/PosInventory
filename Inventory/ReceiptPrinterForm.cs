using System.Drawing.Printing;
using Inventory.Models;
using Inventory.Printing;
using Inventory.Repositories;

namespace Inventory
{
    // Settings > Receipt printer: the 80 mm printer of THIS POS station, paper width, copies, print after each sale,
    // or no printing at all on this station; saved in "ReceiptPrinter" of appsettings.json next to the program (ReceiptSettings).
    // The footer text is the same on every station (database). The printer can be typed before it is installed:
    // until then sales work normally and nothing prints.
    public partial class ReceiptPrinterForm : Form
    {
        private CurrencySet _currencies = CurrencySet.Default;
        private string _companyName = "My Store";

        public ReceiptPrinterForm()
        {
            InitializeComponent();
            AppTheme.Apply(this);
            computerLabel.Text = Environment.MachineName;
            paperCombo.SelectedIndex = 0;
        }

        private async void ReceiptPrinterForm_Load(object? sender, EventArgs e)
        {
            printerCombo.Items.AddRange(PrinterSettings.InstalledPrinters.Cast<string>().OrderBy(p => p).Cast<object>().ToArray());
            try
            {
                var settings = await ReceiptSettings.LoadAsync();
                enabledCheck.Checked = settings.Enabled;
                printerCombo.Text = settings.PrinterName;
                paperCombo.SelectedIndex = settings.PaperWidthMm == 58 ? 1 : 0;
                copiesInput.Value = settings.Copies;
                autoCheck.Checked = settings.PrintAfterSale;
                footerText.Text = settings.Footer;
                _currencies = await new CurrencyRepository().GetSetAsync();
                _companyName = await new CompanyRepository().GetNameAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the receipt settings:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            ShowPrinterStatus();
        }

        private ReceiptSettings FromScreen() => new()
        {
            Enabled = enabledCheck.Checked,
            PrinterName = printerCombo.Text.Trim(),
            PaperWidthMm = paperCombo.SelectedIndex == 1 ? 58 : 80,
            Copies = (int)copiesInput.Value,
            PrintAfterSale = autoCheck.Checked,
            Footer = footerText.Text.Trim(),
            Logo = ReceiptSettings.Current?.Logo,   // set in Settings > Company, shown in the preview and the test print
        };

        private void printerCombo_TextChanged(object? sender, EventArgs e) => ShowPrinterStatus();

        private void enabledCheck_CheckedChanged(object? sender, EventArgs e)
        {
            printerCombo.Enabled = paperCombo.Enabled = copiesInput.Enabled = autoCheck.Enabled = enabledCheck.Checked;
            ShowPrinterStatus();
        }

        private void ShowPrinterStatus()
        {
            var settings = FromScreen();
            var count = printerCombo.Items.Count;
            (printerStatusLabel.Text, printerStatusLabel.ForeColor) =
                !settings.Enabled ? ("Printing is off on this station: sales work, no receipt is printed here.", Color.DimGray)
                : !settings.HasPrinter ? ("No printer: sales work, no receipt is printed. Type the printer's Windows name or pick it" +
                                        (count == 0 ? " (no printer is installed on this computer yet)." : "."), Color.DimGray)
                : settings.PrinterInstalled ? ("Installed on this computer: receipts will print.", Color.FromArgb(34, 139, 117))
                : ("Not installed yet: saved anyway; receipts print as soon as a printer with exactly this name is installed.", Color.FromArgb(217, 119, 6));
            testButton.Enabled = settings.PrinterInstalled;
        }

        // The last sale on screen (or a made-up one when there are no sales yet)
        private async Task<Receipt> PreviewReceiptAsync()
        {
            try
            {
                var last = (await new SaleRepository().GetSalesByUserAsync(null, DateTime.Today.AddDays(-30), DateTime.Today)).FirstOrDefault();
                if (last != null && await new SaleRepository().GetReceiptAsync(last.SaleId) is { } receipt) return receipt;
            }
            catch { /* the sample below */ }
            return Receipt.Sample(_companyName);
        }

        private async void previewButton_Click(object? sender, EventArgs e)
        {
            var receipt = await PreviewReceiptAsync();
            ReceiptPrinter.ShowPreview(this, receipt, _currencies, FromScreen());
        }

        private async void testButton_Click(object? sender, EventArgs e)
        {
            testButton.Enabled = false;
            var problem = await ReceiptPrinter.PrintAsync(Receipt.Sample(_companyName), _currencies, FromScreen());
            testButton.Enabled = true;
            if (problem != null) MessageBox.Show(this, problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private async void saveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                saveButton.Enabled = false;
                await FromScreen().SaveAsync();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                saveButton.Enabled = true;
            }
        }
    }
}
