using System.ComponentModel;
using System.Globalization;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Invoice > "+ New invoice" (back office): a sale typed like a purchase, without the cashier screen.
    // Client (optional; needed for what stays unpaid), items by barcode or name, quantity / price / discount per line,
    // an invoice discount, and how it is paid: cash in the primary and the second currency, card, and the rest unpaid
    // on the client's account (their credit limit is checked). Saved like a cashier sale (fn_save_sale: stock out,
    // in the user's open shift), then it can be printed on A4. Built in code, like the cashier's small windows.
    public class SaleEditForm : Form
    {
        // One line of the invoice; Qty, Price and Discount can be changed in the grid
        public sealed class Line : INotifyPropertyChanged
        {
            private decimal _quantity = 1, _unitPrice, _discount;

            [Browsable(false)] public int PackagingId { get; init; }
            [Browsable(false)] public int ProductId { get; init; }
            [Browsable(false)] public bool IsWeighted { get; init; }
            [DisplayName("Item"), ReadOnly(true)] public string ItemName { get; init; } = string.Empty;
            [DisplayName("Barcode"), ReadOnly(true)] public string? Barcode { get; init; }
            [DisplayName("In stock"), ReadOnly(true)] public decimal StockQty { get; init; }
            [DisplayName("Qty")] public decimal Quantity { get => _quantity; set => Set(ref _quantity, value); }
            [DisplayName("Price")] public decimal UnitPrice { get => _unitPrice; set => Set(ref _unitPrice, value); }
            [DisplayName("Discount")] public decimal Discount { get => _discount; set => Set(ref _discount, value); }
            [DisplayName("Total"), ReadOnly(true)] public decimal LineTotal => Math.Round(Quantity * UnitPrice - Discount, 2);

            public event PropertyChangedEventHandler? PropertyChanged;

            private void Set(ref decimal field, decimal value)
            {
                if (field == value) return;
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));   // the total changes too
            }
        }

        private readonly SaleRepository _saleRepository = new();
        private readonly BindingList<Line> _lines = new();
        private CurrencySet _currencies = CurrencySet.Default;
        private List<CustomerModel> _customers = new();
        private bool _paymentTouched;   // the user typed a payment: it no longer follows the total
        private bool _settingPayment;
        private bool _dirty;

        private readonly ComboBox _customerCombo;
        private readonly Label _customerInfo, _rateLabel, _subTotalValue, _totalValue, _totalSecondValue, _paymentInfo;
        private readonly TextBox _scanBox;
        private readonly DataGridView _grid;
        private readonly NumericUpDown _discountInput, _cashInput, _cashSecondInput, _cardInput;
        private readonly Label _cashCaption, _cashSecondCaption;
        private readonly Button _saveButton;

        /// <summary>The saved invoice (after DialogResult.OK).</summary>
        public int SavedSaleId { get; private set; }

        public SaleEditForm()
        {
            Text = "New sale invoice";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1180, 760);
            MinimumSize = new Size(980, 640);
            Font = new Font("Segoe UI", 10F);
            KeyPreview = true;

            // Client
            var header = new TableLayoutPanel { Dock = DockStyle.Top, Height = 92, ColumnCount = 4, Padding = new Padding(14, 10, 14, 4) };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _customerCombo = new ComboBox { Width = 370, DropDownStyle = ComboBoxStyle.DropDown, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
            _customerCombo.SelectedIndexChanged += (_, _) => { ShowCustomer(); UpdateTotals(); _dirty = true; };
            var addCustomerButton = new Button { Text = "+", Size = new Size(38, 30), Margin = new Padding(3, 2, 3, 3) };
            addCustomerButton.Click += async (_, _) => await AddCustomerAsync();
            _customerInfo = new Label { AutoSize = true, ForeColor = Color.FromArgb(90, 98, 108), Margin = new Padding(3, 7, 3, 3) };
            _rateLabel = new Label { AutoSize = true, ForeColor = Color.FromArgb(90, 98, 108), Margin = new Padding(3, 6, 3, 3) };
            header.Controls.Add(new Label { Text = "Client", AutoSize = true, Margin = new Padding(3, 7, 3, 3) }, 0, 0);
            header.Controls.Add(_customerCombo, 1, 0);
            header.Controls.Add(addCustomerButton, 2, 0);
            header.Controls.Add(_customerInfo, 3, 0);
            header.Controls.Add(new Label { Text = "Date", AutoSize = true, Margin = new Padding(3, 6, 3, 3) }, 0, 1);
            header.Controls.Add(new Label { Text = DateTime.Today.ToString("dddd dd/MM/yyyy") + "  (saved with today's date and time)", AutoSize = true, Margin = new Padding(3, 6, 3, 3) }, 1, 1);
            header.Controls.Add(_rateLabel, 3, 1);

            // Scan / search
            var scanPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(14, 6, 14, 4), WrapContents = false };
            _scanBox = new TextBox { Width = 420, PlaceholderText = "Scan a barcode or type a name, then Enter", Margin = new Padding(3, 5, 3, 3) };
            _scanBox.KeyDown += async (_, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                await AddFromSearchAsync();
            };
            var addButton = new Button { Text = "Add item", Size = new Size(110, 34) };
            addButton.Click += async (_, _) => await AddFromSearchAsync();
            var removeButton = new Button { Text = "Remove line", Size = new Size(120, 34), ForeColor = Color.Firebrick };
            removeButton.Click += (_, _) => { if (_grid!.CurrentRow?.DataBoundItem is Line line) _lines.Remove(line); };
            scanPanel.Controls.AddRange(new Control[] { _scanBox, addButton, removeButton });

            // Lines
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
            };
            _grid.RowTemplate.Height = 32;
            _grid.DataBindingComplete += (_, _) => FormatGrid();
            _grid.DataError += (_, e) =>
            {
                MessageBox.Show(this, "Enter a valid number.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            };
            _grid.CellValueChanged += (_, e) => { if (e.RowIndex >= 0) _grid[e.ColumnIndex, e.RowIndex].ErrorText = string.Empty; };
            _grid.UserDeletingRow += (_, e) => { if (e.Row?.DataBoundItem is Line line) { e.Cancel = true; _lines.Remove(line); } };
            _lines.ListChanged += (_, _) => { UpdateTotals(); _dirty = true; };
            _grid.DataSource = _lines;

            // Totals (right) and payment (left)
            var bottom = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 196, ColumnCount = 2, Padding = new Padding(14, 6, 14, 0) };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));

            var payment = new GroupBox { Text = "Payment", Dock = DockStyle.Fill, Padding = new Padding(10, 6, 10, 6) };
            var payTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            payTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            payTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
            payTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _cashInput = MoneyInput();
            _cashSecondInput = MoneyInput();
            _cardInput = MoneyInput();
            _cashCaption = new Label { Text = "Cash", AutoSize = true, Margin = new Padding(3, 7, 3, 3) };
            _cashSecondCaption = new Label { Text = "Cash", AutoSize = true, Margin = new Padding(3, 7, 3, 3) };
            foreach (var input in new[] { _cashInput, _cashSecondInput, _cardInput })
                input.ValueChanged += (_, _) => { if (!_settingPayment) _paymentTouched = true; UpdateTotals(); _dirty = true; };
            var quick = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            quick.Controls.Add(QuickButton("All cash", () => SetPayment(cash: Total)));
            quick.Controls.Add(QuickButton("All card", () => SetPayment(card: Total)));
            quick.Controls.Add(QuickButton("All unpaid", () => SetPayment()));
            _paymentInfo = new Label { AutoSize = true, Margin = new Padding(3, 8, 3, 3), Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold) };
            payTable.Controls.Add(_cashCaption, 0, 0);
            payTable.Controls.Add(_cashInput, 1, 0);
            payTable.Controls.Add(quick, 2, 0);
            payTable.Controls.Add(_cashSecondCaption, 0, 1);
            payTable.Controls.Add(_cashSecondInput, 1, 1);
            payTable.Controls.Add(new Label { Text = "Card", AutoSize = true, Margin = new Padding(3, 7, 3, 3) }, 0, 2);
            payTable.Controls.Add(_cardInput, 1, 2);
            payTable.Controls.Add(_paymentInfo, 0, 3);
            payTable.SetColumnSpan(_paymentInfo, 3);
            payment.Controls.Add(payTable);

            var totals = new GroupBox { Text = "Totals", Dock = DockStyle.Fill, Padding = new Padding(10, 6, 10, 6) };
            var totalTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            totalTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            totalTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            _subTotalValue = ValueLabel(10F);
            _discountInput = MoneyInput();
            _discountInput.ValueChanged += (_, _) => { UpdateTotals(); _dirty = true; };
            _totalValue = ValueLabel(16F, bold: true);
            _totalSecondValue = ValueLabel(10F);
            _totalSecondValue.ForeColor = Color.FromArgb(90, 98, 108);
            totalTable.Controls.Add(new Label { Text = "Sub total", AutoSize = true, Margin = new Padding(3, 7, 3, 3) }, 0, 0);
            totalTable.Controls.Add(_subTotalValue, 1, 0);
            totalTable.Controls.Add(new Label { Text = "Invoice discount", AutoSize = true, Margin = new Padding(3, 7, 3, 3) }, 0, 1);
            totalTable.Controls.Add(_discountInput, 1, 1);
            totalTable.Controls.Add(new Label { Text = "TOTAL", AutoSize = true, Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold), Margin = new Padding(3, 10, 3, 3) }, 0, 2);
            totalTable.Controls.Add(_totalValue, 1, 2);
            totalTable.Controls.Add(_totalSecondValue, 1, 3);
            totals.Controls.Add(totalTable);

            bottom.Controls.Add(payment, 0, 0);
            bottom.Controls.Add(totals, 1, 0);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 62, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(14, 10, 14, 8), BackColor = Color.White };
            var cancelButton = new Button { Text = "Cancel", Size = new Size(110, 40), DialogResult = DialogResult.Cancel };
            _saveButton = new Button
            {
                Text = "Save invoice", Size = new Size(170, 40), BackColor = Color.FromArgb(28, 110, 92), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
            };
            _saveButton.FlatAppearance.BorderSize = 0;
            _saveButton.Click += async (_, _) => await SaveAsync();
            buttons.Controls.AddRange(new Control[] { cancelButton, _saveButton });

            Controls.Add(_grid);
            Controls.Add(scanPanel);
            Controls.Add(header);
            Controls.Add(bottom);
            Controls.Add(buttons);
            CancelButton = cancelButton;
            AppTheme.Apply(this);

            Load += async (_, _) => await LoadAsync();
            FormClosing += (_, e) =>
            {
                if (DialogResult == DialogResult.OK || !_dirty || _lines.Count == 0) return;
                if (MessageBox.Show(this, "Close without saving this invoice?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    e.Cancel = true;
            };
            KeyDown += async (_, e) =>
            {
                if (e.KeyCode == Keys.F9 || e.Control && e.KeyCode == Keys.S) { e.SuppressKeyPress = true; await SaveAsync(); }
            };
        }

        private async Task LoadAsync()
        {
            try { _currencies = await new CurrencyRepository().GetSetAsync(); } catch { /* LBP / USD defaults */ }
            var primary = _currencies.Primary;
            var second = _currencies.Second is { HasRate: true } s ? s : null;
            _cashCaption.Text = $"Cash {primary.Code}";
            _cashInput.DecimalPlaces = _cardInput.DecimalPlaces = _discountInput.DecimalPlaces = primary.Decimals;
            _cashSecondCaption.Text = second != null ? $"Cash {second.Code}" : "Cash";
            _cashSecondCaption.Visible = _cashSecondInput.Visible = second != null;
            if (second != null) _cashSecondInput.DecimalPlaces = second.Decimals;
            _rateLabel.Text = second != null ? second.RateText : string.Empty;
            await LoadCustomersAsync(null);
            UpdateTotals();
            _dirty = false;
            _scanBox.Focus();
        }

        #region Client

        private async Task LoadCustomersAsync(int? selectId)
        {
            try
            {
                _customers = (await new CustomerRepository().GetAllAsync()).Where(c => c.IsActive).OrderBy(c => c.Name).ToList();
                _customerCombo.DataSource = null;
                _customerCombo.DisplayMember = nameof(CustomerModel.Name);
                _customerCombo.ValueMember = nameof(CustomerModel.CustomerId);
                _customerCombo.DataSource = _customers;
                _customerCombo.SelectedIndex = selectId is int id ? _customers.FindIndex(c => c.CustomerId == id) : -1;
                if (_customerCombo.SelectedIndex < 0) _customerCombo.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the clients:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            ShowCustomer();
        }

        private CustomerModel? SelectedCustomer => _customerCombo.SelectedItem as CustomerModel;

        private void ShowCustomer()
        {
            var customer = SelectedCustomer;
            _customerInfo.Text = customer == null ? "No client: everything must be paid now."
                : $"Owes us {_currencies.FormatPrimary(customer.BalanceLbp)}" +
                  (customer.CreditLimitLbp is { } limit ? $"   ·   credit limit {_currencies.FormatPrimary(limit)}" : "   ·   no credit limit");
        }

        private async Task AddCustomerAsync()
        {
            using var form = new CustomerEditForm(null, _currencies);
            if (form.ShowDialog(this) != DialogResult.OK) return;
            await LoadCustomersAsync(form.SavedId);
        }

        // A typed name that is not chosen from the list: the client with that name, else ask
        private bool ResolveCustomerText()
        {
            var text = _customerCombo.Text.Trim();
            if (text.Length == 0) { _customerCombo.SelectedIndex = -1; return true; }
            if (SelectedCustomer is { } chosen && string.Equals(chosen.Name, text, StringComparison.CurrentCultureIgnoreCase)) return true;
            var match = _customers.FindIndex(c => string.Equals(c.Name.Trim(), text, StringComparison.CurrentCultureIgnoreCase));
            if (match >= 0) { _customerCombo.SelectedIndex = match; return true; }
            MessageBox.Show(this, $"The client \"{text}\" is not in the list. Choose one, or click + to add it.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _customerCombo.Focus();
            return false;
        }

        #endregion

        #region Lines

        private async Task AddFromSearchAsync()
        {
            var search = _scanBox.Text.Trim();
            if (search.Length == 0) { _scanBox.Focus(); return; }
            try
            {
                UseWaitCursor = true;
                var matches = (await _saleRepository.FindItemAsync(search)).ToList();
                UseWaitCursor = false;
                PosItemModel? chosen;
                if (matches.Count == 0)
                {
                    MessageBox.Show(this, $"No item found for \"{search}\".\nOnly active items that can be sold are listed.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _scanBox.SelectAll();
                    _scanBox.Focus();
                    return;
                }
                if (matches.Count == 1) chosen = matches[0];
                else
                {
                    using var picker = new PackagingPickerForm(matches, search);
                    if (picker.ShowDialog(this) != DialogResult.OK) return;
                    chosen = picker.SelectedItem as PosItemModel;
                }
                if (chosen != null) AddLine(chosen);
                _scanBox.Clear();
                _scanBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not search items:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        // The same packaging again adds 1 to its line; otherwise a new line at its sale price
        private void AddLine(PosItemModel item)
        {
            var line = _lines.FirstOrDefault(l => l.PackagingId == item.PackagingId);
            if (line != null) line.Quantity += 1;
            else
            {
                line = new Line
                {
                    PackagingId = item.PackagingId,
                    ProductId = item.ProductId,
                    IsWeighted = item.IsWeighted,
                    ItemName = item.ItemName,
                    Barcode = item.Barcode,
                    StockQty = item.StockQty,
                    UnitPrice = item.SalePrice,
                };
                _lines.Add(line);
            }
            var index = _lines.IndexOf(line);
            if (index >= 0 && index < _grid.Rows.Count && _grid.Columns[nameof(Line.Quantity)] is { } qty)
                _grid.CurrentCell = _grid[qty.Index, index];
        }

        private void FormatGrid()
        {
            foreach (DataGridViewColumn column in _grid.Columns)
            {
                if (column.ValueType != typeof(decimal)) continue;
                column.DefaultCellStyle.Format = "#,0.###";
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                if (!column.ReadOnly) column.DefaultCellStyle.BackColor = Color.FromArgb(255, 252, 230);   // editable cells
            }
            if (_grid.Columns[nameof(Line.ItemName)] is { } name) name.FillWeight = 260;
            if (_grid.Columns[nameof(Line.Barcode)] is { } barcode) barcode.FillWeight = 120;
        }

        #endregion

        #region Totals and payment

        private decimal SubTotal => _lines.Sum(l => l.LineTotal);

        private decimal Total => SubTotal - _discountInput.Value;

        private decimal SecondRate => _currencies.Second is { HasRate: true, IsPrimary: false, Rate: > 0 } s ? s.Rate!.Value : 0;

        // What is paid now, in the primary currency (as fn_save_sale converts it)
        private decimal Paid => _cashInput.Value + Math.Round(_cashSecondInput.Value * SecondRate, 2) + _cardInput.Value;

        private void SetPayment(decimal cash = 0, decimal card = 0)
        {
            _settingPayment = true;
            _cashInput.Value = Math.Min(Math.Max(0, cash), _cashInput.Maximum);
            _cashSecondInput.Value = 0;
            _cardInput.Value = Math.Min(Math.Max(0, card), _cardInput.Maximum);
            _settingPayment = false;
            _paymentTouched = true;
            UpdateTotals();
        }

        private void UpdateTotals()
        {
            var total = Total;
            // Until a payment is typed it is all cash, following the total
            if (!_paymentTouched)
            {
                _settingPayment = true;
                _cashInput.Value = Math.Min(Math.Max(0, total), _cashInput.Maximum);
                _settingPayment = false;
            }

            _subTotalValue.Text = _currencies.FormatPrimary(SubTotal);
            _totalValue.Text = _currencies.FormatPrimary(total);
            _totalValue.ForeColor = total < 0 ? Color.Firebrick : Color.FromArgb(22, 32, 43);
            _totalSecondValue.Text = SecondRate > 0 && total > 0 ? "= " + _currencies.FormatSecond(total) : string.Empty;

            var paid = Paid;
            var unpaid = total - paid;
            var customer = SelectedCustomer;
            (_paymentInfo.Text, _paymentInfo.ForeColor) =
                total <= 0 ? (string.Empty, Color.DimGray)
                : unpaid == 0 ? ("Paid in full.", Color.FromArgb(34, 139, 117))
                : unpaid < 0 ? ($"Change to give: {_currencies.FormatPrimary(-unpaid)}", Color.FromArgb(29, 78, 160))
                : customer == null ? ($"Unpaid {_currencies.FormatPrimary(unpaid)}: choose a client to put it on their account.", Color.Firebrick)
                : customer.CreditLimitLbp is { } limit && customer.BalanceLbp + unpaid > limit
                    ? ($"Unpaid {_currencies.FormatPrimary(unpaid)} on {customer.Name}'s account: over the credit limit.", Color.Firebrick)
                : ($"Unpaid {_currencies.FormatPrimary(unpaid)} goes on {customer.Name}'s account.", Color.FromArgb(217, 119, 6));
            Text = _lines.Count == 0 ? "New sale invoice" : $"New sale invoice  ·  {_lines.Count} line(s)  ·  {_currencies.FormatPrimary(total)}";
        }

        #endregion

        #region Save

        // Checks here first (the database checks again): marks the wrong cells, returns the first problem
        private string? CheckInvoice()
        {
            foreach (DataGridViewRow row in _grid.Rows)
                foreach (DataGridViewCell cell in row.Cells) cell.ErrorText = string.Empty;
            if (_lines.Count == 0) return "Add at least one item.";

            string? first = null;
            for (var i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                (string Field, string Message)? problem =
                    line.Quantity <= 0 ? (nameof(Line.Quantity), "The quantity must be more than 0.")
                    : !line.IsWeighted && line.Quantity != decimal.Truncate(line.Quantity) ? (nameof(Line.Quantity), "This item is sold in whole units only.")
                    : line.UnitPrice < 0 ? (nameof(Line.UnitPrice), "The price cannot be negative.")
                    : line.Discount < 0 ? (nameof(Line.Discount), "The discount cannot be negative.")
                    : line.Discount > line.Quantity * line.UnitPrice ? (nameof(Line.Discount), "The discount is more than the line.")
                    : null;
                if (problem is not { } p) continue;
                if (_grid.Columns[p.Field] is { } column && i < _grid.Rows.Count) _grid[column.Index, i].ErrorText = p.Message;
                first ??= $"Line {i + 1} ({line.ItemName}): {p.Message}";
            }
            if (first != null) return first;

            if (_discountInput.Value > SubTotal) return "The invoice discount is more than the sub total.";
            var total = Total;
            var unpaid = total - Paid;
            var cash = _cashInput.Value + Math.Round(_cashSecondInput.Value * SecondRate, 2);
            if (unpaid < 0 && -unpaid > cash) return "Card is more than the total: only cash can give change.";
            if (unpaid > 0 && SelectedCustomer == null) return $"{_currencies.FormatPrimary(unpaid)} is not paid: choose the client whose account it goes on, or type the payment.";
            return null;
        }

        private async Task SaveAsync()
        {
            if (!_saveButton.Enabled) return;
            if (!_grid.EndEdit()) return;
            if (!ResolveCustomerText()) return;
            if (CheckInvoice() is { } problem)
            {
                MessageBox.Show(this, problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var total = Total;
            var unpaid = Math.Max(0, total - Paid);
            var customer = SelectedCustomer;
            var payments = new List<SalePaymentModel>();
            if (_cashInput.Value > 0) payments.Add(new() { Method = PaymentMethod.Cash, Currency = _currencies.Primary.Code, Amount = _cashInput.Value });
            if (_cashSecondInput.Value > 0 && _currencies.Second is { } second) payments.Add(new() { Method = PaymentMethod.Cash, Currency = second.Code, Amount = _cashSecondInput.Value });
            if (_cardInput.Value > 0) payments.Add(new() { Method = PaymentMethod.Card, Currency = _currencies.Primary.Code, Amount = _cardInput.Value });
            if (unpaid > 0) payments.Add(new() { Method = PaymentMethod.OnAccount, Currency = _currencies.Primary.Code, Amount = unpaid });

            var question = $"Save this sale of {_currencies.FormatPrimary(total)}" + (customer != null ? $" to {customer.Name}" : string.Empty) + "?\n\n" +
                           "The items are taken out of stock." + (unpaid > 0 ? $"\n{_currencies.FormatPrimary(unpaid)} goes on the client's account." : string.Empty) +
                           "\nA saved sale cannot be changed (make a return to correct it).";
            if (MessageBox.Show(this, question, "Save invoice", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                _saveButton.Enabled = false;
                UseWaitCursor = true;
                var lines = _lines.Select(l => new PosLine
                {
                    PackagingId = l.PackagingId,
                    ProductId = l.ProductId,
                    ItemName = l.ItemName,
                    IsWeighted = l.IsWeighted,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    Discount = l.Discount,
                }).ToList();
                var result = await _saleRepository.SaveSaleAsync(lines, payments, _discountInput.Value, null, AppConfig.CurrentUserId, customer?.CustomerId);
                SavedSaleId = result.SaleId;

                if (unpaid > 0 && customer != null && AppSession.CurrentUser is { } approver)
                {
                    try
                    {
                        await _saleRepository.AddAuditAsync("unpaid_sale_approved", "sales_invoices", result.SaleId,
                            new { ApprovedBy = approver.UserId, ApprovedByName = approver.FullName, result.InvoiceNo, result.TotalLbp,
                                  customer.CustomerId, CustomerName = customer.Name, From = "back office" });
                    }
                    catch { /* the sale is saved; the approval note is extra */ }
                }
                UseWaitCursor = false;

                var change = result.ChangeLbp > 0 ? $"\nChange to give: {_currencies.FormatPrimary(result.ChangeLbp)}" : string.Empty;
                if (MessageBox.Show(this, $"Invoice {result.InvoiceNo} saved ({_currencies.FormatPrimary(result.TotalLbp)}).{change}\n\nPrint it on A4?",
                        Text, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    try { await Printing.SaleInvoicePrint.PrintAsync(this, result.SaleId, _currencies); }
                    catch (Exception ex) { MessageBox.Show(this, "The invoice is saved, but could not be printed:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                // Nothing is saved when anything fails (fn_save_sale is all or nothing)
                MessageBox.Show(this, "Could not save the invoice:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _saveButton.Enabled = true;
                UseWaitCursor = false;
            }
        }

        #endregion

        private static NumericUpDown MoneyInput() => new()
        {
            Maximum = 100_000_000_000m,
            ThousandsSeparator = true,
            TextAlign = HorizontalAlignment.Right,
            Width = 180,
            Margin = new Padding(3, 3, 3, 3),
        };

        private static Label ValueLabel(float size, bool bold = false) => new()
        {
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            Margin = new Padding(3, 6, 3, 3),
        };

        private Button QuickButton(string text, Action click)
        {
            var button = new Button { Text = text, AutoSize = true, MinimumSize = new Size(86, 30), Margin = new Padding(3, 2, 3, 2) };
            button.Click += (_, _) => { if (Total > 0) click(); };
            return button;
        }
    }
}
