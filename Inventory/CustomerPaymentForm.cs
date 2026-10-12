using System.Globalization;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Receive payment: a client pays (part of) what they owe for unpaid sales (cashier: Unpaid F3).
    // The money settles their invoices oldest first: the "This payment" column shows it while the amount is typed.
    // Cash or card, in any currency that has a rate. More than the client owes is refused.
    // Opened for one client (Clients page), or without one (cashier): then the client is chosen from those who owe.
    // Built in code, like the cashier's small windows.
    public class CustomerPaymentForm : Form
    {
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);
        private static readonly Color Accent = Color.FromArgb(34, 139, 117);
        private const int WalkInId = 1;   // the walk-in customer: never on account

        private readonly CustomerRepository _customerRepository = new();
        private readonly CurrencySet _currencies;
        private CustomerModel? _customer;
        private List<CustomerOpenInvoiceModel> _invoices = new();
        private bool _saving;

        private readonly Label _titleLabel, _owesLabel, _afterLabel, _historyCaption;
        private readonly ComboBox _customerCombo, _currencyCombo, _methodCombo;
        private readonly Panel _customerPanel;
        private readonly TextBox _amountBox;
        private readonly DataGridView _invoicesGrid, _historyGrid;
        private readonly Button _receiveButton;

        /// <param name="customer">The client who pays; null = choose one of the clients who owe.</param>
        public CustomerPaymentForm(CurrencySet currencies, CustomerModel? customer = null)
        {
            _currencies = currencies;
            _customer = customer;

            Text = "Receive payment";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = new Size(820, 690);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10F);

            var header = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = Dark, Padding = new Padding(20, 12, 20, 8) };
            _owesLabel = new Label { Dock = DockStyle.Fill, ForeColor = Color.Gainsboro, Font = new Font("Segoe UI", 12F) };
            _titleLabel = new Label { Dock = DockStyle.Top, Height = 32, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold) };
            header.Controls.Add(_owesLabel);
            header.Controls.Add(_titleLabel);

            _customerPanel = new Panel { Dock = DockStyle.Top, Height = 52, Visible = customer == null };
            _customerPanel.Controls.Add(new Label { Text = "Customer", Location = new Point(20, 15), AutoSize = true, ForeColor = Color.DimGray });
            _customerCombo = new ComboBox { Location = new Point(110, 10), Width = 690, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12F), FormattingEnabled = true };
            _customerCombo.Format += (_, e) =>
            {
                if (e.ListItem is CustomerModel c) e.Value = $"{c.Name}   ·   {c.Phone}   ·   owes {_currencies.FormatPrimary(c.BalanceLbp)}";
            };
            _customerCombo.SelectedIndexChanged += async (_, _) =>
            {
                if (_customerCombo.SelectedItem is CustomerModel c && !ReferenceEquals(c, _customer)) await ShowCustomerAsync(c);
            };
            _customerPanel.Controls.Add(_customerCombo);

            var invoicesCaption = Caption("Unpaid invoices  ·  the oldest is paid first");
            _invoicesGrid = NewGrid();
            _invoicesGrid.Dock = DockStyle.Top;
            _invoicesGrid.Height = 200;
            _invoicesGrid.DataBindingComplete += (_, _) => FormatColumns(_invoicesGrid);
            _invoicesGrid.CellFormatting += invoicesGrid_CellFormatting;

            var payPanel = new Panel { Dock = DockStyle.Top, Height = 104, BackColor = Color.FromArgb(238, 241, 244) };
            payPanel.Controls.Add(new Label { Text = "Amount paid now", Location = new Point(20, 10), AutoSize = true, ForeColor = Color.DimGray });
            _amountBox = new TextBox { Location = new Point(20, 34), Width = 220, Font = new Font("Segoe UI", 18F), TextAlign = HorizontalAlignment.Right };
            _amountBox.TextChanged += (_, _) => UpdatePreview();
            _currencyCombo = new ComboBox { Location = new Point(250, 36), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 14F), DisplayMember = nameof(CurrencyModel.Code) };
            _currencyCombo.Items.AddRange(_currencies.Payable.Cast<object>().ToArray());
            if (_currencyCombo.Items.Count > 0) _currencyCombo.SelectedIndex = 0;
            _currencyCombo.SelectedIndexChanged += (_, _) => UpdatePreview();
            _methodCombo = new ComboBox { Location = new Point(350, 36), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 14F) };
            _methodCombo.Items.AddRange(new object[] { "Cash", "Card" });
            _methodCombo.SelectedIndex = 0;
            var allButton = new Button { Text = "Pays everything", Location = new Point(462, 34), Size = new Size(150, 40), FlatStyle = FlatStyle.Flat, BackColor = Color.White, UseVisualStyleBackColor = false, TabStop = false };
            allButton.FlatAppearance.BorderColor = Color.Silver;
            allButton.Click += (_, _) => FillEverything();
            _afterLabel = new Label { Location = new Point(20, 78), Size = new Size(780, 22), ForeColor = Color.DimGray };
            payPanel.Controls.AddRange(new Control[] { _amountBox, _currencyCombo, _methodCombo, allButton, _afterLabel });

            _historyCaption = Caption("Payments received");
            _historyGrid = NewGrid();
            _historyGrid.Dock = DockStyle.Fill;
            _historyGrid.DataBindingComplete += (_, _) => FormatColumns(_historyGrid);

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 72, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12, 8, 12, 8) };
            var closeButton = new Button
            {
                Text = "Close (Esc)",
                DialogResult = DialogResult.Cancel,
                Size = new Size(130, 52),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(244, 246, 248),
                UseVisualStyleBackColor = false,
            };
            closeButton.FlatAppearance.BorderColor = Color.Silver;
            _receiveButton = new Button
            {
                Text = "Receive payment",
                Size = new Size(260, 52),
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Accent,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false,
                Enabled = false,
            };
            _receiveButton.FlatAppearance.BorderSize = 0;
            _receiveButton.Click += async (_, _) => await ReceiveAsync();
            bottom.Controls.Add(closeButton);
            bottom.Controls.Add(_receiveButton);

            // Docking goes from the last control added to the first: the header ends up on top, the history fills the rest
            Controls.AddRange(new Control[] { _historyGrid, _historyCaption, payPanel, _invoicesGrid, invoicesCaption, _customerPanel, header, bottom });
            AcceptButton = _receiveButton;   // Enter
            CancelButton = closeButton;      // Esc
            Load += CustomerPaymentForm_Load;
        }

        /// <summary>True when a payment was saved (the caller reloads its balances).</summary>
        public bool Received { get; private set; }

        private static Label Caption(string text) => new()
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 32,
            Padding = new Padding(16, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            ForeColor = Dark,
        };

        private static DataGridView NewGrid() => new()
        {
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            TabStop = false,
        };

        private static void FormatColumns(DataGridView grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.ValueType == typeof(decimal))
                {
                    column.DefaultCellStyle.Format = "#,0.##";
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (column.ValueType == typeof(DateTime))
                {
                    column.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                    column.FillWeight = 150;
                }
            }
            if (grid.Columns[nameof(CustomerOpenInvoiceModel.InvoiceNo)] is { } invoiceNo) invoiceNo.FillWeight = 140;
            if (grid.Columns[nameof(CustomerPaymentModel.Invoices)] is { } invoices) invoices.FillWeight = 220;
            if (grid.Columns[nameof(CustomerOpenInvoiceModel.PayNow)] is { } payNow)
                payNow.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }

        private async void CustomerPaymentForm_Load(object? sender, EventArgs e)
        {
            if (_customer != null)
            {
                await ShowCustomerAsync(_customer);
                return;
            }

            // Cashier: the clients who owe something, by name
            try
            {
                var debtors = (await _customerRepository.GetAllAsync())
                    .Where(c => c.CustomerId != WalkInId && c.BalanceLbp > 0).ToList();
                if (debtors.Count == 0)
                {
                    MessageBox.Show(this, "No customer owes anything.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                    return;
                }
                _customerCombo.Items.AddRange(debtors.Cast<object>().ToArray());
                _customerCombo.SelectedIndex = 0;   // shows that client
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the customers:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private async Task ShowCustomerAsync(CustomerModel customer)
        {
            _customer = customer;
            _titleLabel.Text = customer.Name + (string.IsNullOrWhiteSpace(customer.Phone) ? string.Empty : $"   ·   {customer.Phone}");
            _amountBox.Clear();
            try
            {
                UseWaitCursor = true;
                _invoices = (await _customerRepository.GetOpenInvoicesAsync(customer.CustomerId)).ToList();
                var payments = (await _customerRepository.GetPaymentsAsync(customer.CustomerId)).ToList();
                _invoicesGrid.DataSource = _invoices;
                _historyGrid.DataSource = payments;
                _historyCaption.Text = payments.Count == 0 ? "Payments received  ·  none yet" : $"Payments received  ·  {payments.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the invoices:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
            UpdatePreview();
            _amountBox.Focus();
        }

        private decimal Owed => Math.Max(_customer?.BalanceLbp ?? 0, 0);

        private CurrencyModel? Currency => _currencyCombo.SelectedItem as CurrencyModel;

        /// <summary>
        /// The amount typed, in the primary currency (0 when it is not a number). A little over what is owed because the
        /// other currency was rounded up ("Pays everything" in USD) counts as exactly what is owed, as the database does.
        /// </summary>
        private decimal TypedInPrimary(out decimal amount)
        {
            amount = 0;
            var text = _amountBox.Text.Trim();
            if (Currency is not { } currency || text.Length == 0) return 0;
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out amount)
                && !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
                return 0;
            if (amount <= 0) return 0;

            var primary = _currencies.ToPrimary(amount, currency.Code);
            var smallestUnit = (currency.IsPrimary ? 1 : currency.Rate ?? 0) / (decimal)Math.Pow(10, currency.Decimals);
            return primary > Owed && primary - Owed < smallestUnit ? Owed : primary;
        }

        // Shows what the typed amount pays, oldest invoice first, and what the client still owes after it
        private void UpdatePreview()
        {
            var paying = TypedInPrimary(out _);
            var tooMuch = paying > Owed;
            var left = tooMuch ? 0 : paying;
            foreach (var invoice in _invoices)
            {
                invoice.PayNow = Math.Min(invoice.DueLbp, left);
                left -= invoice.PayNow;
            }
            _invoicesGrid.Refresh();

            var second = _currencies.FormatSecond(Owed);
            _owesLabel.Text = Owed > 0
                ? $"Owes {_currencies.FormatPrimary(Owed)}" + (second.Length > 0 ? $"   ({second})" : string.Empty) + $"   ·   {_invoices.Count} unpaid invoice(s)"
                : "Owes nothing";

            _amountBox.BackColor = tooMuch || (_amountBox.TextLength > 0 && paying == 0) ? Color.MistyRose : SystemColors.Window;
            _afterLabel.ForeColor = tooMuch ? Color.Firebrick : Color.DimGray;
            _afterLabel.Text = tooMuch ? $"That is more than the customer owes ({_currencies.FormatPrimary(Owed)})."
                : paying > 0 ? $"= {_currencies.FormatPrimary(paying)}   ·   still owes after this payment: {_currencies.FormatPrimary(Owed - paying)}"
                : "Type what the customer pays now. It can be less than what they owe.";
            _receiveButton.Enabled = !_saving && paying > 0 && !tooMuch && _customer != null;
        }

        private void FillEverything()
        {
            if (Currency is not { } currency) return;
            var amount = _currencies.FromPrimary(Owed, currency);
            _amountBox.Text = amount.ToString(currency.Decimals == 0 ? "0" : "0." + new string('0', currency.Decimals), CultureInfo.CurrentCulture);
            _amountBox.Focus();
            _amountBox.SelectAll();
        }

        private void invoicesGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _invoices.Count || e.CellStyle == null) return;
            var invoice = _invoices[e.RowIndex];
            switch (_invoicesGrid.Columns[e.ColumnIndex].DataPropertyName)
            {
                case nameof(CustomerOpenInvoiceModel.PayNow):
                    if (invoice.PayNow == 0) { e.Value = "-"; e.FormattingApplied = true; }
                    e.CellStyle.ForeColor = Accent;
                    break;
                case nameof(CustomerOpenInvoiceModel.Left):
                    if (invoice.PayNow > 0 && invoice.Left == 0) { e.Value = "paid"; e.FormattingApplied = true; e.CellStyle.ForeColor = Accent; }
                    else e.CellStyle.ForeColor = Color.DarkOrange;
                    break;
            }
        }

        private async Task ReceiveAsync()
        {
            if (_saving || _customer is not { } customer || Currency is not { } currency) return;
            var paying = TypedInPrimary(out var amount);
            if (paying <= 0 || paying > Owed) return;

            try
            {
                _saving = true;
                _receiveButton.Enabled = false;
                UseWaitCursor = true;
                var method = _methodCombo.SelectedIndex == 1 ? PaymentMethod.Card : PaymentMethod.Cash;
                var result = await _customerRepository.ReceivePaymentAsync(customer.CustomerId, amount, currency.Code, method, AppConfig.CurrentUserId);
                customer.BalanceLbp = result.BalanceLbp;
                Received = true;

                var settled = result.InvoicesPaid > 0 ? $"{result.InvoicesPaid} invoice(s) paid" : string.Empty;
                if (result.InvoicesPartly > 0) settled += (settled.Length > 0 ? ", " : string.Empty) + "1 invoice partly paid";
                MessageBox.Show(this,
                    $"Received {_currencies.FormatPrimary(result.PaidLbp)} from {customer.Name}.\n"
                    + (settled.Length > 0 ? settled + ".\n" : string.Empty)
                    + (result.BalanceLbp > 0 ? $"Still owes {_currencies.FormatPrimary(result.BalanceLbp)}." : "Nothing left to pay."),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                // Nothing was saved (fn_receive_customer_payment is all or nothing)
                MessageBox.Show(this, "The payment was not saved:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _saving = false;
                UseWaitCursor = false;
                if (!IsDisposed) UpdatePreview();
            }
        }
    }
}
