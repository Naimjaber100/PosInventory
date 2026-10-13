using System.Globalization;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Settings > Currencies & exchange rates.
    // One primary currency: every price and total is stored in it. Other active currencies can be paid and
    // refunded at the cashier at their latest rate (1 USD = 89,500 LBP); each new rate is kept with who set it.
    // The primary currency can only change while there are no sales or purchases yet.
    public partial class CurrenciesForm : Form
    {
        private readonly CurrencyRepository _currencyRepository = new();
        private CurrencySet _set = CurrencySet.Default;
        private bool _changed;

        public CurrenciesForm()
        {
            InitializeComponent();
            AppTheme.Apply(this);
        }

        private async void CurrenciesForm_Load(object? sender, EventArgs e) => await LoadAsync();

        private async Task LoadAsync(string? selectCode = null)
        {
            selectCode ??= Selected?.Code;
            try
            {
                _set = await _currencyRepository.GetSetAsync();
                currenciesGrid.DataSource = _set.All;
                infoLabel.Text = $"Primary currency: {_set.Primary.Code} ({_set.Primary.Name}). All prices and totals are in it."
                                 + (_set.Second != null ? $"   ·   Shown next to totals: {_set.Second.Code}" : string.Empty);
                var index = _set.All.FindIndex(c => c.Code == selectCode);
                if (index >= 0) currenciesGrid.CurrentCell = currenciesGrid.Rows[index].Cells.Cast<DataGridViewCell>().First(c => c.Visible);
                await ShowRatesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the currencies:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private CurrencyModel? Selected => currenciesGrid.CurrentRow?.DataBoundItem as CurrencyModel;

        private async void currenciesGrid_SelectionChanged(object? sender, EventArgs e) => await ShowRatesAsync();

        private string? _ratesShownFor;

        // The rate history of the selected currency
        private async Task ShowRatesAsync()
        {
            var currency = Selected;
            if (currency?.Code == _ratesShownFor && currency != null) return;
            _ratesShownFor = currency?.Code;
            setRateButton.Enabled = currency is { IsPrimary: false };
            primaryButton.Enabled = currency is { IsPrimary: false };
            if (currency == null) return;

            if (currency.IsPrimary)
            {
                ratesCaption.Text = $"{currency.Code} is the primary currency: its rate is always 1.";
                ratesGrid.DataSource = null;
                return;
            }
            try
            {
                var rates = (await _currencyRepository.GetRatesAsync(currency.Code)).ToList();
                ratesCaption.Text = rates.Count == 0
                    ? $"{currency.Code} has no rate yet: press \"Set rate\" before it can be used at the cashier."
                    : $"Rate history of {currency.Code}  (1 {currency.Code} = ... {_set.Primary.Code})";
                ratesGrid.DataSource = rates;
            }
            catch (Exception ex)
            {
                ratesCaption.Text = "Could not load the rates: " + ex.Message;
            }
        }

        #region Buttons

        private async void addButton_Click(object? sender, EventArgs e)
        {
            var currency = new CurrencyModel { SortOrder = _set.All.Count == 0 ? 1 : _set.All.Max(c => c.SortOrder) + 1 };
            if (!EditDialog(currency, isNew: true)) return;
            if (await SaveAsync(currency)) await LoadAsync(currency.Code);
        }

        private async void editButton_Click(object? sender, EventArgs e) => await EditSelectedAsync();

        private async void currenciesGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) await EditSelectedAsync();
        }

        private async Task EditSelectedAsync()
        {
            if (Selected is not { } selected) return;
            var copy = new CurrencyModel
            {
                Code = selected.Code, Name = selected.Name, Symbol = selected.Symbol, Decimals = selected.Decimals,
                IsActive = selected.IsActive, IsPrimary = selected.IsPrimary, SortOrder = selected.SortOrder,
            };
            if (!EditDialog(copy, isNew: false)) return;
            if (await SaveAsync(copy)) await LoadAsync(copy.Code);
        }

        private async Task<bool> SaveAsync(CurrencyModel currency)
        {
            try
            {
                await _currencyRepository.SaveAsync(currency);
                _changed = true;
                return true;
            }
            catch (Exception ex)
            {
                ShowError("Could not save the currency", ex);
                return false;
            }
        }

        private async void setRateButton_Click(object? sender, EventArgs e)
        {
            if (Selected is not { IsPrimary: false } currency) return;
            if (RateDialog(currency) is not { } rate) return;
            try
            {
                await _currencyRepository.SetRateAsync(currency.Code, rate, AppConfig.CurrentUserId);
                _changed = true;
                _ratesShownFor = null;
                await LoadAsync(currency.Code);
            }
            catch (Exception ex)
            {
                ShowError("Could not save the rate", ex);
            }
        }

        private async void primaryButton_Click(object? sender, EventArgs e)
        {
            if (Selected is not { IsPrimary: false } currency) return;
            if (MessageBox.Show(this,
                    $"Make {currency.Code} the primary currency?\n\nAll prices and totals will then be in {currency.Code}. " +
                    "This is only possible while there are no sales or purchases yet, and the exchange rates must be set again after.",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            try
            {
                await _currencyRepository.SetPrimaryAsync(currency.Code);
                _changed = true;
                _ratesShownFor = null;
                await LoadAsync(currency.Code);
            }
            catch (Exception ex)
            {
                ShowError("The primary currency was not changed", ex);
            }
        }

        private void ShowError(string what, Exception ex) =>
            MessageBox.Show(this, $"{what}:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);

        #endregion

        #region Dialogs

        // Code (new only), name, symbol, decimals, order, active
        private bool EditDialog(CurrencyModel currency, bool isNew)
        {
            using var dialog = NewDialog(isNew ? "Add currency" : $"Edit {currency.Code}", new Size(420, 330));
            var code = new TextBox { Text = currency.Code, MaxLength = 3, CharacterCasing = CharacterCasing.Upper, ReadOnly = !isNew, PlaceholderText = "EUR" };
            var name = new TextBox { Text = currency.Name, MaxLength = 50, PlaceholderText = "Euro" };
            var symbol = new TextBox { Text = currency.Symbol, MaxLength = 8, PlaceholderText = "€" };
            var decimals = new NumericUpDown { Minimum = 0, Maximum = 4, Value = currency.Decimals, Width = 70 };
            var order = new NumericUpDown { Minimum = 0, Maximum = 999, Value = currency.SortOrder, Width = 70 };
            var active = new CheckBox { Text = "Can be used at the cashier", Checked = currency.IsActive, AutoSize = true, Enabled = !currency.IsPrimary };

            AddRow(dialog, 0, "Code", code, "3 letters (ISO), e.g. USD, EUR");
            AddRow(dialog, 1, "Name", name);
            AddRow(dialog, 2, "Symbol", symbol, "Shown with amounts: $ 5.00 or 5,000 LBP");
            AddRow(dialog, 3, "Decimals", decimals);
            AddRow(dialog, 4, "Order", order, "The first one after the primary is shown next to totals");
            active.Location = new Point(130, 14 + 5 * 46);
            dialog.Controls.Add(active);
            var ok = AddButtons(dialog);

            while (dialog.ShowDialog(this) == DialogResult.OK)
            {
                var problem = !System.Text.RegularExpressions.Regex.IsMatch(code.Text.Trim(), "^[A-Z]{3}$") ? "The code must be 3 letters, e.g. EUR."
                    : name.Text.Trim().Length == 0 ? "The name is required."
                    : symbol.Text.Trim().Length == 0 ? "The symbol is required."
                    : isNew && _set.Find(code.Text.Trim()) != null ? $"{code.Text.Trim()} is already in the list."
                    : null;
                if (problem != null)
                {
                    MessageBox.Show(dialog, problem, dialog.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }
                currency.Code = code.Text.Trim();
                currency.Name = name.Text.Trim();
                currency.Symbol = symbol.Text.Trim();
                currency.Decimals = (short)decimals.Value;
                currency.SortOrder = (int)order.Value;
                currency.IsActive = active.Checked;
                return true;
            }
            return false;
        }

        // "1 USD = [ 89,500 ] LBP"
        private decimal? RateDialog(CurrencyModel currency)
        {
            using var dialog = NewDialog($"Exchange rate of {currency.Code}", new Size(460, 200));
            var current = currency.Rate > 0 ? $"Now: {currency.RateText}" : "No rate yet.";
            dialog.Controls.Add(new Label { Text = current, Location = new Point(16, 14), Size = new Size(420, 24), ForeColor = Color.DimGray });
            dialog.Controls.Add(new Label { Text = $"1 {currency.Code} =", Location = new Point(16, 58), Size = new Size(90, 34), Font = new Font("Segoe UI", 14F), TextAlign = ContentAlignment.MiddleRight });
            var rate = new TextBox
            {
                Location = new Point(112, 54),
                Width = 220,
                Font = new Font("Segoe UI", 16F),
                TextAlign = HorizontalAlignment.Right,
                Text = currency.Rate > 0 ? currency.Rate.Value.ToString("0.##", CultureInfo.CurrentCulture) : string.Empty,
            };
            dialog.Controls.Add(rate);
            dialog.Controls.Add(new Label { Text = _set.Primary.Code, Location = new Point(338, 58), Size = new Size(90, 34), Font = new Font("Segoe UI", 14F), TextAlign = ContentAlignment.MiddleLeft });
            AddButtons(dialog);
            dialog.Shown += (_, _) => { rate.Focus(); rate.SelectAll(); };

            while (dialog.ShowDialog(this) == DialogResult.OK)
            {
                if ((decimal.TryParse(rate.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value)
                     || decimal.TryParse(rate.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)) && value > 0)
                    return value;
                MessageBox.Show(dialog, "Type a rate more than 0.", dialog.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return null;
        }

        private static Form NewDialog(string title, Size size) => new()
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = size,
        };

        private static void AddRow(Form dialog, int row, string caption, Control field, string? hint = null)
        {
            var top = 14 + row * 46;
            dialog.Controls.Add(new Label { Text = caption, Location = new Point(16, top + 3), Size = new Size(110, 24) });
            field.Location = new Point(130, top);
            if (field is TextBox) field.Width = 120;
            dialog.Controls.Add(field);
            if (hint != null)
                dialog.Controls.Add(new Label { Text = hint, Location = new Point(field.Right + 10, top + 3), Size = new Size(dialog.ClientSize.Width - field.Right - 16, 40), ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8F) });
        }

        private static Button AddButtons(Form dialog)
        {
            var size = dialog.ClientSize;
            var ok = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                Location = new Point(size.Width - 222, size.Height - 50),
                Size = new Size(100, 36),
                BackColor = Color.FromArgb(34, 139, 117),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
            };
            ok.FlatAppearance.BorderSize = 0;
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(size.Width - 116, size.Height - 50), Size = new Size(100, 36) };
            dialog.Controls.AddRange(new Control[] { ok, cancel });
            dialog.AcceptButton = ok;
            dialog.CancelButton = cancel;
            return ok;
        }

        #endregion

        #region Grid look

        private void currenciesGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (currenciesGrid.Columns[nameof(CurrencyModel.RateText)] is { } rate)
            {
                rate.FillWeight = 180;
                rate.DefaultCellStyle.Font = new Font(currenciesGrid.Font, FontStyle.Bold);
            }
            if (currenciesGrid.Columns[nameof(CurrencyModel.Name)] is { } name) name.FillWeight = 140;
            if (currenciesGrid.Columns[nameof(CurrencyModel.RateAt)] is { } at) at.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            foreach (var narrow in new[] { nameof(CurrencyModel.Code), nameof(CurrencyModel.Symbol), nameof(CurrencyModel.Decimals), nameof(CurrencyModel.IsPrimary), nameof(CurrencyModel.IsActive) })
                if (currenciesGrid.Columns[narrow] is { } column) column.FillWeight = 55;
        }

        private void currenciesGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.CellStyle == null || currenciesGrid.Rows[e.RowIndex].DataBoundItem is not CurrencyModel c) return;
            if (c.IsPrimary) e.CellStyle.Font = new Font(currenciesGrid.Font, FontStyle.Bold);
            else if (!c.IsActive) e.CellStyle.ForeColor = Color.Gray;
            else if (!c.HasRate && currenciesGrid.Columns[e.ColumnIndex].DataPropertyName == nameof(CurrencyModel.RateText))
                e.CellStyle.ForeColor = e.CellStyle.SelectionForeColor = Color.Firebrick;
        }

        private void ratesGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (ratesGrid.Columns[nameof(CurrencyRateModel.EffectiveAt)] is { } at) at.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            if (ratesGrid.Columns[nameof(CurrencyRateModel.Rate)] is { } rate)
            {
                rate.DefaultCellStyle.Format = "#,0.##";
                rate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        #endregion

        // An open cashier screen picks up the new rates
        private void CurrenciesForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            if (!_changed) return;
            foreach (var cashier in Application.OpenForms.OfType<CashierForm>().ToList())
                _ = cashier.LoadCurrenciesAsync();
        }
    }
}
