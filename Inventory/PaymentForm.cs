using System.Globalization;
using Inventory.Models;
using Inventory.Validation;

namespace Inventory
{
    // Takes payment for a sale: cash in every active currency that has an exchange rate (Settings > Currencies),
    // and card in the primary currency. "Rest in ..." fills what is left in that currency. Shows the change live
    // (always given in the primary currency, in cash). Enter = pay, Esc = back to the sale.
    public partial class PaymentForm : Form
    {
        private const int RowHeight = 60;

        private readonly decimal _total;          // in the primary currency
        private readonly CurrencySet _currencies;
        private readonly List<PaymentRow> _rows = new();

        // One payment line on the screen
        private sealed record PaymentRow(short Method, CurrencyModel Currency, TextBox Amount);

        public PaymentForm(decimal total, CurrencySet currencies)
        {
            InitializeComponent();
            AppTheme.Apply(this);
            _total = total;
            _currencies = currencies;

            totalValue.Text = currencies.FormatPrimary(total);
            totalSecondValue.Text = currencies.FormatSecond(total);
            rateValue.Text = string.Join("\n", currencies.Payable.Where(c => !c.IsPrimary).Select(c => c.RateText));

            foreach (var currency in currencies.Payable)
                AddRow($"Cash {currency.Code}", PaymentMethod.Cash, currency, $"Rest in {currency.Code}");
            AddRow($"Card ({currencies.Primary.Code})", PaymentMethod.Card, currencies.Primary, "Rest by card");

            ClientSize = new Size(ClientSize.Width, headerPanel.Height + _rows.Count * RowHeight + 12 + footerPanel.Height);
            UpdatePaid();
        }

        /// <summary>The payments entered (amounts more than 0), after DialogResult.OK.</summary>
        public List<SalePaymentModel> Payments { get; } = new();

        private void AddRow(string caption, short method, CurrencyModel currency, string restText)
        {
            var index = _rows.Count;
            rowsTable.RowCount = index + 1;
            rowsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeight));

            var label = new Label { Text = caption, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 13F), TextAlign = ContentAlignment.MiddleLeft };
            var amount = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 18F),
                TextAlign = HorizontalAlignment.Right,
                Margin = new Padding(3, 6, 3, 6),
                TabIndex = index,
            };
            amount.TextChanged += (_, _) => UpdatePaid();
            var row = new PaymentRow(method, currency, amount);
            var rest = new Button { Text = restText, Dock = DockStyle.Fill, Margin = new Padding(6, 6, 0, 6), TabStop = false };
            rest.Click += (_, _) => FillRemaining(row);

            rowsTable.Controls.Add(label, 0, index);
            rowsTable.Controls.Add(amount, 1, index);
            rowsTable.Controls.Add(rest, 2, index);
            _rows.Add(row);
        }

        private void PaymentForm_Shown(object? sender, EventArgs e)
        {
            if (_rows.Count == 0) return;
            _rows[0].Amount.Focus();
            _rows[0].Amount.SelectAll();
        }

        private List<SalePaymentModel> ReadPayments() =>
            _rows.Select(r => new SalePaymentModel { Method = r.Method, Currency = r.Currency.Code, Amount = Parse(r.Amount) }).ToList();

        private static decimal Parse(TextBox box)
        {
            var text = box.Text.Trim();
            var ok = text.Length == 0
                     || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out _)
                     || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
            box.BackColor = ok ? SystemColors.Window : Color.MistyRose;
            if (text.Length == 0 || !ok) return 0;
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var v)
                ? v : decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);
        }

        private decimal PaidInPrimary(IEnumerable<SalePaymentModel> payments) =>
            payments.Where(p => p.Amount > 0).Sum(p => _currencies.ToPrimary(p.Amount, p.Currency));

        private void UpdatePaid()
        {
            var paid = PaidInPrimary(ReadPayments());
            var change = paid - _total;

            paidValue.Text = _currencies.FormatPrimary(paid);
            if (change >= 0)
            {
                changeCaption.Text = "Change";
                changeValue.Text = _currencies.FormatPrimary(change);
                changeSecondValue.Text = change > 0 && _currencies.FormatSecond(change) is { Length: > 0 } s ? $"({s})" : string.Empty;
                changeValue.ForeColor = Color.FromArgb(34, 139, 117);
            }
            else
            {
                changeCaption.Text = "Remaining";
                changeValue.Text = _currencies.FormatPrimary(-change);
                changeSecondValue.Text = _currencies.FormatSecond(-change) is { Length: > 0 } s ? $"({s})" : string.Empty;
                changeValue.ForeColor = Color.Firebrick;
            }
            payButton.Enabled = change >= 0;
        }

        // Fills what is left after the other boxes in this row's currency (rounded up so it is enough)
        private void FillRemaining(PaymentRow row)
        {
            row.Amount.Text = string.Empty;
            var remaining = Math.Max(0, _total - PaidInPrimary(ReadPayments()));
            var amount = _currencies.FromPrimary(remaining, row.Currency);
            var format = row.Currency.Decimals == 0 ? "0" : "0." + new string('0', row.Currency.Decimals);
            row.Amount.Text = amount.ToString(format, CultureInfo.CurrentCulture);
            row.Amount.Focus();
            row.Amount.SelectAll();
        }

        private void clearButton_Click(object? sender, EventArgs e)
        {
            foreach (var row in _rows) row.Amount.Clear();
            if (_rows.Count > 0) _rows[0].Amount.Focus();
        }

        private void payButton_Click(object? sender, EventArgs e)
        {
            var payments = ReadPayments();
            var validation = SaleValidator.ValidatePayment(_total, _currencies, payments);
            if (!validation.IsValid)
            {
                MessageBox.Show(this, validation.ToMessage(), "Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Payments.Clear();
            Payments.AddRange(payments.Where(p => p.Amount > 0));
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
