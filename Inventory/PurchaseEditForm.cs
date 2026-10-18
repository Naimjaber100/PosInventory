using System.ComponentModel;
using Inventory.Models;
using Inventory.Repositories;
using Inventory.Validation;

namespace Inventory
{
    // Add a purchase invoice (no id) or open an existing one (id).
    // Drafts can be changed; posted / cancelled invoices open read-only.
    // Type: Purchase, or Return to supplier (the items that go back, added like a purchase): posting a return takes
    // them out of stock and lowers what we owe the supplier. A draft can still change its type.
    // Print: what is saved of it, on A4 (PurchasePrint).
    public partial class PurchaseEditForm : Form
    {
        private readonly PurchaseRepository _purchaseRepository = new();
        private readonly BindingList<PurchaseEditLine> _lines = new();
        private readonly int? _purchaseId;
        private PurchaseHeaderModel _header = new();
        private bool _loading = true;   // true while filling the form, so it does not count as a change
        private bool _dirty;
        private readonly ErrorProvider _errors = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };
        private readonly ComboBox _typeCombo;
        private readonly Label _typeLabel;
        private bool _showingType;

        private void TypeLayout()
        {
            _typeCombo.Width = notesText.Width / 2;
            _typeCombo.Location = new Point(notesText.Right - _typeCombo.Width, supplierInvoiceText.Top);
            _typeLabel.Location = new Point(_typeCombo.Left - _typeLabel.Width - 8, statusLabel.Top);
        }

        public PurchaseEditForm(int? purchaseId = null)
        {
            InitializeComponent();

            // Type, beside the status, ending where the notes box ends (placed when the window has its size: TypeLayout)
            _typeLabel = new Label { Text = "Type", AutoSize = true };
            headerGroup.Controls.Add(_typeLabel);
            _typeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            _typeCombo.Items.AddRange(new object[] { "Purchase", "Return to supplier" });
            _typeCombo.SelectedIndex = 0;
            _typeCombo.SelectedIndexChanged += (_, _) =>
            {
                if (_showingType) return;
                _header.DocType = _typeCombo.SelectedIndex == 1 ? PurchaseDocType.Return : PurchaseDocType.Purchase;
                ShowDocType();
                MarkDirty();
            };
            headerGroup.Controls.Add(_typeCombo);

            // Print (left of the other buttons): what is saved of this purchase, on A4
            var printButton = new Button { Text = "Print", Size = new Size(110, cancelButton.Height), Margin = cancelButton.Margin };
            printButton.Click += async (_, _) => await PrintAsync();
            buttonsPanel.Controls.Add(printButton);

            AppTheme.Apply(this);
            _purchaseId = purchaseId;
            _errors.ContainerControl = this;

            _lines.ListChanged += (_, _) => { UpdateTotals(); MarkDirty(); };
            linesGrid.DataSource = _lines;
            linesGrid.CellValueChanged += (_, e) =>
            {
                // A corrected cell loses its error mark
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0) linesGrid[e.ColumnIndex, e.RowIndex].ErrorText = string.Empty;
            };
            dateInput.Value = DateTime.Today;
            UpdateTotals();
        }

        private void MarkDirty()
        {
            if (!_loading) _dirty = true;
        }

        private void headerField_Changed(object? sender, EventArgs e) => MarkDirty();

        private void PurchaseEditForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK || IsReadOnly || !_dirty) return;
            if (MessageBox.Show(this, "Close without saving your changes?", Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                e.Cancel = true;
        }

        /// <summary>The id of the saved purchase (after DialogResult.OK).</summary>
        public int SavedPurchaseId { get; private set; }

        private bool IsReadOnly => !_header.IsDraft;

        private async void PurchaseEditForm_Load(object? sender, EventArgs e)
        {
            TypeLayout();
            if (_purchaseId == null)
            {
                statusValue.Text = "New";
                await LoadCurrenciesAsync(null);
                await LoadSuppliersAsync(null);
                ShowDocType();
                ActiveControl = supplierCombo;
                _loading = false;
                return;
            }

            try
            {
                UseWaitCursor = true;
                var header = await _purchaseRepository.GetByIdAsync(_purchaseId.Value);
                if (header == null)
                {
                    MessageBox.Show(this, "This purchase no longer exists.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                    return;
                }
                _header = header;

                foreach (var l in await _purchaseRepository.GetLinesAsync(header.PurchaseId))
                {
                    _lines.Add(new PurchaseEditLine
                    {
                        PackagingId = l.PackagingId,
                        ProductId = l.ProductId,
                        ItemName = l.ItemName,
                        PackagingName = l.PackagingName,
                        Barcode = l.Barcode,
                        Factor = l.Factor,
                        BaseUnit = l.BaseUnit,
                        IsWeighted = l.IsWeighted,
                        Quantity = l.Quantity,
                        UnitCost = l.UnitCost,
                    });
                }
                await LoadCurrenciesAsync(header.Currency);
                await LoadSuppliersAsync(header.SupplierId);
                FillHeader();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the purchase:\n" + ex.Message, Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
            finally
            {
                UseWaitCursor = false;
                _loading = false;
            }
        }

        // The type box and the captions of a purchase or a return (what it is, what posting does)
        private void ShowDocType()
        {
            var isReturn = _header.IsReturn;
            _showingType = true;
            _typeCombo.SelectedIndex = isReturn ? 1 : 0;
            _showingType = false;
            _typeCombo.ForeColor = isReturn ? Color.Firebrick : SystemColors.ControlText;
            _typeCombo.Enabled = _header.IsDraft && _header.ReturnOfId == null;

            headerGroup.Text = !isReturn ? "Invoice"
                : _header.ReturnOfNo != null ? $"Return to supplier  ·  of purchase {_header.ReturnOfNo}" : "Return to supplier";
            postButton.Text = isReturn ? "Save && post return" : "Save && post";
            aiButton.Visible = voiceButton.Visible = !isReturn;   // reading a supplier's invoice is for purchases
            if (_purchaseId == null) Text = isReturn ? "New return to supplier" : "New purchase";
            UpdateTotals();
        }

        private void FillHeader()
        {
            numberValue.Text = _header.PurchaseNo ?? "(given when posted)";
            SelectSupplier(_header.SupplierId);
            if (_header.Currency != null) SetCurrency(_header.Currency, _header.CurrencyRate);
            supplierInvoiceText.Text = _header.SupplierInvoiceNo;
            dateInput.Value = _header.InvoiceDate;
            notesText.Text = _header.Notes;
            discountInput.Value = Math.Min(_header.Discount, discountInput.Maximum);
            statusValue.Text = _header.Status switch { 0 => "Draft", 1 => "Posted", _ => "Cancelled" };

            var what = _header.IsReturn ? "return to supplier" : "purchase";
            Text = _header.IsDraft
                ? $"Edit {what} (draft #{_header.PurchaseId})"
                : $"{(_header.IsReturn ? "Return" : "Purchase")} {_header.PurchaseNo} - {statusValue.Text} (read-only)";
            ShowDocType();
            if (_header.ReturnOfId != null) supplierCombo.Enabled = addSupplierButton.Visible = false;

            if (IsReadOnly)
            {
                supplierInvoiceText.ReadOnly = true;
                supplierCombo.Enabled = false;
                currencyCombo.Enabled = false;
                rateInput.Enabled = false;
                addSupplierButton.Visible = false;
                notesText.ReadOnly = true;
                dateInput.Enabled = false;
                discountInput.Enabled = false;
                scanPanel.Enabled = false;
                linesGrid.ReadOnly = true;
                linesGrid.AllowUserToDeleteRows = false;
                saveDraftButton.Visible = false;
                postButton.Visible = false;
                cancelButton.Text = "Close";
            }
            UpdateTotals();
        }

        #region Currency

        private CurrencySet _currencies = CurrencySet.Default;
        private bool _settingCurrency;              // true while the screen sets the currency itself (no conversion prompt)
        private string _shownCode = string.Empty;   // the currency the costs on screen are in
        private decimal _shownRate = 1;

        // Active currencies with a rate, plus this purchase's own currency when it is no longer active
        private async Task LoadCurrenciesAsync(string? keepCode)
        {
            try { _currencies = await new CurrencyRepository().GetSetAsync(); }
            catch { /* CurrencySet.Default: LBP primary */ }

            var codes = _currencies.Payable.Select(c => c.Code).ToList();
            if (keepCode != null && !codes.Contains(keepCode, StringComparer.OrdinalIgnoreCase)) codes.Add(keepCode.Trim());
            currencyCombo.Items.Clear();
            currencyCombo.Items.AddRange(codes.Cast<object>().ToArray());
            SetCurrency(keepCode ?? _currencies.Primary.Code, null);
        }

        private CurrencyModel? SelectedCurrency => currencyCombo.SelectedItem is string code ? _currencies.Find(code) : null;

        private bool IsPrimarySelected => SelectedCurrency?.IsPrimary ?? true;

        /// <summary>1 unit of the purchase currency in the primary currency (1 for the primary).</summary>
        private decimal CurrentRate => IsPrimarySelected ? 1 : rateInput.Value;

        // Selects a currency without asking to convert; rate null = its current rate
        private void SetCurrency(string code, decimal? rate)
        {
            _settingCurrency = true;
            try
            {
                currencyCombo.SelectedItem = currencyCombo.Items.Cast<string>().FirstOrDefault(c => string.Equals(c, code.Trim(), StringComparison.OrdinalIgnoreCase))
                                             ?? _currencies.Primary.Code;
                ShowRate(rate);
            }
            finally { _settingCurrency = false; }
        }

        // Rate box: hidden for the primary currency; otherwise the given rate or today's
        private void ShowRate(decimal? rate)
        {
            var currency = SelectedCurrency;
            var primary = IsPrimarySelected;
            rateLabel.Visible = rateInput.Visible = rateUnitLabel.Visible = !primary;
            if (!primary && currency != null)
            {
                rateLabel.Text = $"1 {currency.Code} =";
                rateUnitLabel.Text = $"{_currencies.Primary.Code}   (supplier's rate, today {_currencies.Primary.Format(currency.Rate ?? 0)})";
                var value = rate ?? currency.Rate ?? 0;
                rateInput.Value = Math.Min(Math.Max(value, rateInput.Minimum), rateInput.Maximum);
            }
            _shownCode = currency?.Code ?? _currencies.Primary.Code;
            _shownRate = CurrentRate;
            ShowCurrencyCaptions();
            UpdateTotals();
        }

        private void ShowCurrencyCaptions()
        {
            var code = SelectedCurrency?.Code ?? _currencies.Primary.Code;
            if (linesGrid.Columns[nameof(PurchaseEditLine.UnitCost)] is { } cost) cost.HeaderText = $"Unit cost ({code})";
            if (linesGrid.Columns[nameof(PurchaseEditLine.LineTotal)] is { } total) total.HeaderText = $"Line total ({code})";
        }

        // Another currency chosen: offer to convert the costs already typed (else they are kept as numbers)
        private void currencyCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_settingCurrency) return;
            var oldCode = _shownCode;
            var oldRate = _shownRate;
            ShowRate(null);
            var newRate = CurrentRate;

            if ((_lines.Count > 0 || discountInput.Value > 0) && oldCode != _shownCode && oldRate > 0 && newRate > 0 &&
                MessageBox.Show(this, $"Convert the unit costs and the discount from {oldCode} to {_shownCode}?\n\n" +
                                      $"Yes = convert them at the rates.\nNo = keep the numbers as they are (they were already in {_shownCode}).",
                    "Currency", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ConvertAmounts(oldRate / newRate);
            }
            MarkDirty();
        }

        private void ConvertAmounts(decimal factor)
        {
            var decimals = Math.Max(SelectedCurrency?.Decimals ?? 2, (short)2);
            foreach (var line in _lines) line.UnitCost = Math.Round(line.UnitCost * factor, 4);
            discountInput.Value = Math.Min(Math.Round(discountInput.Value * factor, decimals), discountInput.Maximum);
            _lines.ResetBindings();
        }

        private void rateInput_ValueChanged(object? sender, EventArgs e)
        {
            if (_settingCurrency) return;
            _shownRate = CurrentRate;
            UpdateTotals();
            MarkDirty();
        }

        /// <summary>A cost in the primary currency (e.g. the item's last cost) in the purchase currency.</summary>
        private decimal FromPrimary(decimal primaryAmount)
        {
            var rate = CurrentRate;
            return rate <= 0 || rate == 1 ? primaryAmount : Math.Round(primaryAmount / rate, Math.Max(SelectedCurrency?.Decimals ?? 2, (short)2));
        }

        #endregion

        #region Supplier

        // Active suppliers, plus this purchase's own supplier when it has been deactivated since
        private async Task LoadSuppliersAsync(int? keepId)
        {
            try
            {
                var all = await new SupplierRepository().GetAllAsync(null, includeInactive: true);
                var list = all.Where(s => s.IsActive || s.SupplierId == keepId).ToList();
                var loading = _loading;
                _loading = true;   // filling the list is not a change
                supplierCombo.DataSource = null;
                supplierCombo.DisplayMember = nameof(SupplierModel.Name);
                supplierCombo.ValueMember = nameof(SupplierModel.SupplierId);
                supplierCombo.DataSource = list;
                SelectSupplier(keepId);
                _loading = loading;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the suppliers:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private int? SelectedSupplierId => (supplierCombo.SelectedItem as SupplierModel)?.SupplierId;

        private void SelectSupplier(int? supplierId)
        {
            var index = supplierCombo.DataSource is List<SupplierModel> list ? list.FindIndex(s => s.SupplierId == supplierId) : -1;
            supplierCombo.SelectedIndex = index;
            if (index < 0) supplierCombo.Text = string.Empty;
        }

        private void supplierCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            _errors.SetError(supplierCombo, string.Empty);
            MarkDirty();
        }

        // Leaving the box: a typed name is matched silently; an unknown one is only marked (Save / Post ask about it)
        private void supplierCombo_Validating(object? sender, CancelEventArgs e)
        {
            var text = supplierCombo.Text.Trim();
            if (IsReadOnly || text.Length == 0 || IsChosen(text)) return;
            if (FindSupplier(text) is { } match) SelectSupplier(match.SupplierId);
            else _errors.SetError(supplierCombo, $"\"{text}\" is not in the list. Click + to add it.");
        }

        private bool IsChosen(string text) =>
            supplierCombo.SelectedItem is SupplierModel chosen && string.Equals(chosen.Name, text, StringComparison.CurrentCultureIgnoreCase);

        // Before saving: empty = no supplier; a known name = that supplier; anything else = the closest
        // match, or an offer to add it. Returns false when the user cancelled.
        private async Task<bool> ResolveSupplierTextAsync()
        {
            if (IsReadOnly) return true;
            var text = supplierCombo.Text.Trim();
            if (text.Length == 0)
            {
                if (supplierCombo.SelectedIndex >= 0) supplierCombo.SelectedIndex = -1;
                return true;
            }
            return IsChosen(text) || await PickSupplierByNameAsync(text);
        }

        // The supplier with this name: exact (ignoring case and spaces), else the only one that contains it or is contained in it
        private SupplierModel? FindSupplier(string name)
        {
            var list = supplierCombo.DataSource as List<SupplierModel> ?? new();
            var key = Normalize(name);
            if (key.Length == 0) return null;
            var match = list.FirstOrDefault(s => Normalize(s.Name) == key);
            if (match != null) return match;
            var close = list.Where(s => Normalize(s.Name).Contains(key) || key.Contains(Normalize(s.Name))).ToList();
            return close.Count == 1 ? close[0] : null;
        }

        // Selects the supplier with this name; when none matches, offers to add it
        // (used for a typed name and for the supplier name the AI read on the invoice)
        private async Task<bool> PickSupplierByNameAsync(string name)
        {
            if (FindSupplier(name) is { } match)
            {
                SelectSupplier(match.SupplierId);
                return true;
            }

            var answer = MessageBox.Show(this, $"The supplier \"{name}\" is not in the list.\n\nAdd it as a new supplier?",
                "Supplier", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (answer == DialogResult.Cancel) { supplierCombo.Focus(); return false; }
            if (answer == DialogResult.Yes) return await AddSupplierAsync(name);
            SelectSupplier(null);   // No: leave it empty and pick one later
            return true;
        }

        private static string Normalize(string? name) =>
            string.Join(' ', (name ?? string.Empty).ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

        private async void addSupplierButton_Click(object? sender, EventArgs e)
        {
            var typed = IsChosen(supplierCombo.Text.Trim()) ? null : supplierCombo.Text.Trim();
            await AddSupplierAsync(typed);
        }

        // Opens Add supplier (with the name filled in), then reloads the list and selects the new one
        private async Task<bool> AddSupplierAsync(string? name)
        {
            using var form = new SupplierEditForm(null, CurrencySet.Default, name);
            if (form.ShowDialog(this) != DialogResult.OK) { supplierCombo.Focus(); return false; }
            await LoadSuppliersAsync(form.SavedId);
            _errors.SetError(supplierCombo, string.Empty);
            MarkDirty();
            return SelectedSupplierId == form.SavedId;
        }

        #endregion

        #region Adding lines

        private async void scanBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await AddFromSearchAsync();
        }

        private async void addLineButton_Click(object? sender, EventArgs e)
        {
            await AddFromSearchAsync();
        }

        private async Task AddFromSearchAsync()
        {
            var search = scanBox.Text.Trim();
            if (search.Length == 0)
            {
                scanBox.Focus();
                return;
            }

            try
            {
                UseWaitCursor = true;
                var matches = (await _purchaseRepository.FindPackagingAsync(search)).ToList();
                UseWaitCursor = false;

                PackagingLookupModel? chosen;
                if (matches.Count == 0)
                {
                    MessageBox.Show(this, $"No item found for \"{search}\".\nOnly active standard items that can be purchased are listed.",
                        "Add item", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    scanBox.SelectAll();
                    scanBox.Focus();
                    return;
                }
                else if (matches.Count == 1)
                {
                    chosen = matches[0];
                }
                else
                {
                    using var picker = new PackagingPickerForm(matches, search);
                    if (picker.ShowDialog(this) != DialogResult.OK) return;
                    chosen = picker.Selected;
                }

                if (chosen != null) AddLine(chosen);
                scanBox.Clear();
                scanBox.Focus();   // ready for the next scan
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not search items:\n" + ex.Message, "Add item",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        // Same packaging again adds 1 to its quantity; otherwise a new line with the last cost
        private void AddLine(PackagingLookupModel item)
        {
            var existing = _lines.FirstOrDefault(l => l.PackagingId == item.PackagingId);
            if (existing != null)
            {
                existing.Quantity += 1;
                SelectLine(existing);
                return;
            }

            var line = new PurchaseEditLine
            {
                PackagingId = item.PackagingId,
                ProductId = item.ProductId,
                ItemName = item.ItemName,
                PackagingName = item.PackagingName,
                Barcode = item.Barcode,
                Factor = item.Factor,
                BaseUnit = item.BaseUnit,
                IsWeighted = item.IsWeighted,
                Quantity = 1,
                UnitCost = FromPrimary(item.LastCost),
            };
            _lines.Add(line);
            SelectLine(line);
        }

        private void SelectLine(PurchaseEditLine line)
        {
            var index = _lines.IndexOf(line);
            if (index < 0 || index >= linesGrid.Rows.Count) return;
            linesGrid.ClearSelection();
            linesGrid.Rows[index].Selected = true;
            linesGrid.FirstDisplayedScrollingRowIndex = index;
        }

        private async void aiButton_Click(object? sender, EventArgs e) => await AddFromAiAsync(voice: false);

        private async void voiceButton_Click(object? sender, EventArgs e) => await AddFromAiAsync(voice: true);

        // Read invoice (AI) / Voice note (AI): the checked lines are added; the same packaging adds to its line.
        // The supplier (matched by name, or offered to add), its invoice number, date and discount are filled when still empty.
        private async Task AddFromAiAsync(bool voice)
        {
            if (IsReadOnly) return;
            using var form = new AiPurchaseImportForm(voice);
            if (form.ShowDialog(this) != DialogResult.OK) return;

            // Prices come in the invoice currency. An empty purchase takes that currency; otherwise they are converted.
            var factor = 1m;
            if (!string.Equals(form.CurrencyCode, _shownCode, StringComparison.OrdinalIgnoreCase))
            {
                var available = currencyCombo.Items.Cast<string>().Any(c => string.Equals(c, form.CurrencyCode, StringComparison.OrdinalIgnoreCase));
                if (_lines.Count == 0 && discountInput.Value == 0 && available)
                    SetCurrency(form.CurrencyCode, null);
                else if (CurrentRate > 0)
                    factor = form.CurrencyRate / CurrentRate;
            }
            var decimals = Math.Max(SelectedCurrency?.Decimals ?? 2, (short)2);

            foreach (var (item, quantity, aiCost) in form.Lines)
            {
                var unitCost = factor == 1 ? aiCost : Math.Round(aiCost * factor, decimals);
                var existing = _lines.FirstOrDefault(l => l.PackagingId == item.PackagingId);
                if (existing != null)
                {
                    existing.Quantity += quantity;
                    if (existing.UnitCost == 0) existing.UnitCost = unitCost;
                    continue;
                }
                _lines.Add(new PurchaseEditLine
                {
                    PackagingId = item.PackagingId,
                    ProductId = item.ProductId,
                    ItemName = item.ItemName,
                    PackagingName = item.PackagingName,
                    Barcode = item.Barcode,
                    Factor = item.Factor,
                    BaseUnit = item.BaseUnit,
                    IsWeighted = item.IsWeighted,
                    Quantity = quantity,
                    UnitCost = unitCost,
                });
            }

            if (SelectedSupplierId == null && form.SupplierName != null) await PickSupplierByNameAsync(form.SupplierName);
            if (string.IsNullOrWhiteSpace(supplierInvoiceText.Text) && form.InvoiceNumber != null) supplierInvoiceText.Text = form.InvoiceNumber;
            if (form.InvoiceDate is { } date && date >= dateInput.MinDate && date <= dateInput.MaxDate) dateInput.Value = date;
            if (discountInput.Value == 0 && form.Discount is { } discount) discountInput.Value = Math.Min(Math.Round(discount * factor, decimals), discountInput.Maximum);
            if (_lines.Count > 0) SelectLine(_lines[^1]);
            MarkDirty();
        }

        private void removeLineButton_Click(object? sender, EventArgs e)
        {
            if (IsReadOnly) return;
            if (linesGrid.CurrentRow?.DataBoundItem is PurchaseEditLine line) _lines.Remove(line);
        }

        #endregion

        #region Totals and grid

        private void discountInput_ValueChanged(object? sender, EventArgs e)
        {
            UpdateTotals();
            MarkDirty();
        }

        private void UpdateTotals()
        {
            var sub = _lines.Sum(l => l.LineTotal);
            var total = sub - discountInput.Value;
            var currency = SelectedCurrency ?? _currencies.Primary;
            subTotalValue.Text = currency.Format(sub);
            totalValue.Text = currency.Format(total);
            totalValue.ForeColor = total < 0 ? Color.Firebrick : SystemColors.ControlText;
            // In another currency: what it makes in the primary one (the supplier balance and the costs use this)
            totalLbpValue.Text = currency.IsPrimary || CurrentRate <= 0 || total == 0 ? string.Empty : $"= {_currencies.Primary.Format(total * CurrentRate)}";
            itemsGroup.Text = $"{(_header.IsReturn ? "Items going back" : "Items")} ({_lines.Count} line(s), {_lines.Sum(l => l.Quantity):#,0.###} qty)";
        }

        private void linesGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewColumn column in linesGrid.Columns)
            {
                if (column.ValueType != typeof(decimal)) continue;
                column.DefaultCellStyle.Format = "#,0.###";
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                if (!column.ReadOnly) column.DefaultCellStyle.BackColor = Color.FromArgb(255, 252, 230);   // editable cells
            }
        }

        private void linesGrid_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            MessageBox.Show(this, "Enter a valid number.", "Items", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }

        #endregion

        #region Save

        private async void saveDraftButton_Click(object? sender, EventArgs e) => await SaveAsync(post: false);

        private async void postButton_Click(object? sender, EventArgs e) => await SaveAsync(post: true);

        private async Task SaveAsync(bool post)
        {
            if (!linesGrid.EndEdit()) return;   // apply a cell that is still being edited

            if (!await ResolveSupplierTextAsync()) return;   // a typed name that is not in the list: picked or added first
            _header.SupplierId = SelectedSupplierId;
            _header.SupplierName = (supplierCombo.SelectedItem as SupplierModel)?.Name;
            if (!IsPrimarySelected && CurrentRate <= 0)
            {
                MessageBox.Show(this, $"Enter the exchange rate of {SelectedCurrency?.Code} for this purchase.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                rateInput.Focus();
                return;
            }
            _header.Currency = SelectedCurrency?.Code ?? _currencies.Primary.Code;
            _header.CurrencyRate = CurrentRate;
            _header.SupplierInvoiceNo = supplierInvoiceText.Text;
            _header.InvoiceDate = dateInput.Value.Date;
            _header.Discount = discountInput.Value;
            _header.Notes = notesText.Text;

            // Check everything here first; the database only gets valid data (and checks again)
            var sub = _lines.Sum(l => l.LineTotal);
            var validation = PurchaseHeaderValidator.Validate(_header, sub, _lines.Count, post)
                .Merge(PurchaseLineValidator.ValidateAll(_lines));
            ShowValidation(validation);
            if (!validation.IsValid)
            {
                MessageBox.Show(this, "Please correct these first:" + Environment.NewLine + Environment.NewLine + validation.ToMessage(),
                    post ? "Post purchase" : "Save purchase", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var question = _header.IsReturn
                ? $"Post this return to {_header.SupplierName} ({sub - _header.Discount:#,0.##})?\n\n" +
                  "The items are taken out of stock and what we owe the supplier goes down.\nA posted return cannot be changed."
                : $"Post this purchase ({sub - _header.Discount:#,0.##})?\n\n" +
                  "The items are added to stock and the average cost is updated.\nA posted purchase cannot be changed.";
            if (post && MessageBox.Show(this, question, _header.IsReturn ? "Post return" : "Post purchase",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                SetBusy(true);
                SavedPurchaseId = await _purchaseRepository.SaveAsync(_header, _lines, post, AppConfig.CurrentUserId);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                // Nothing is saved when anything fails (the database function is all or nothing)
                MessageBox.Show(this, "Could not save the purchase:\n" + ex.Message, Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // Marks the wrong header fields (error icon) and grid cells (error icon + tooltip); clears old marks
        private void ShowValidation(ValidationResult validation)
        {
            var headerControls = new Dictionary<string, Control>
            {
                [nameof(PurchaseHeaderModel.SupplierId)] = supplierCombo,
                [nameof(PurchaseHeaderModel.SupplierInvoiceNo)] = supplierInvoiceText,
                [nameof(PurchaseHeaderModel.InvoiceDate)] = dateInput,
                [nameof(PurchaseHeaderModel.Discount)] = discountInput,
                [nameof(PurchaseHeaderModel.Notes)] = notesText,
            };
            foreach (var control in headerControls.Values) _errors.SetError(control, string.Empty);
            foreach (DataGridViewRow row in linesGrid.Rows)
                foreach (DataGridViewCell cell in row.Cells) cell.ErrorText = string.Empty;

            foreach (var error in validation.Errors)
            {
                if (error.LineIndex is int index && index < linesGrid.Rows.Count)
                {
                    var column = error.Field != null ? linesGrid.Columns[error.Field] : linesGrid.Columns[nameof(PurchaseEditLine.ItemName)];
                    if (column != null) linesGrid[column.Index, index].ErrorText = error.Message;
                }
                else if (error.Field != null && headerControls.TryGetValue(error.Field, out var control))
                {
                    _errors.SetError(control, error.Message);
                }
            }

            var firstBadLine = validation.Errors.FirstOrDefault(e => e.LineIndex != null)?.LineIndex;
            if (firstBadLine is int line && line < _lines.Count) SelectLine(_lines[line]);
        }

        private void SetBusy(bool busy)
        {
            UseWaitCursor = busy;
            saveDraftButton.Enabled = !busy;
            postButton.Enabled = !busy;
        }

        #endregion

        // The printout is of what is saved: a new purchase or unsaved changes are saved first (as a draft)
        private async Task PrintAsync()
        {
            if (!IsReadOnly && (_purchaseId == null && SavedPurchaseId == 0 || _dirty))
            {
                MessageBox.Show(this, "Save the purchase first (Save draft or Save & post): the printout shows what is saved.",
                    "Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                UseWaitCursor = true;
                await Printing.PurchasePrint.PrintAsync(this, _purchaseId ?? SavedPurchaseId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not print:\n" + ex.Message, "Print", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {

        }
    }
}
