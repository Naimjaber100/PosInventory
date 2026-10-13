using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Sales (cashier F7): the finished sales and returns of a cashier between two dates (Today by default).
    // Top: total sales and total profit (returns subtracted), invoice count, returns. Click an invoice: its items below.
    // A User sees only their own invoices; a Manager can choose a cashier or All users.
    public partial class SalesHistoryForm : Form
    {
        private const int AllUsers = 0;

        private readonly SaleRepository _saleRepository = new();
        private CurrencySet _currencies = CurrencySet.Default;
        private List<SaleHistoryModel> _sales = new();
        private bool _loading = true;
        private Label _salesValue = null!, _profitValue = null!, _countValue = null!, _returnsValue = null!;

        private int? _openForUserId;        // set when opened from a report: the cashier (null = all users)
        private bool _openedFromReport;

        public SalesHistoryForm()
        {
            InitializeComponent();
            AppTheme.Apply(this);
            BuildCards();
            fromPicker.Value = toPicker.Value = DateTime.Today;
        }

        /// <summary>Opens on a period and a cashier (userId null = all users), e.g. a day of the daily sales report.</summary>
        public SalesHistoryForm(DateTime from, DateTime to, int? userId) : this()
        {
            fromPicker.Value = from.Date;
            toPicker.Value = to.Date;
            _openForUserId = userId;
            _openedFromReport = true;
        }

        // The four totals at the top
        private void BuildCards()
        {
            _salesValue = AddCard("TOTAL SALES", Color.FromArgb(34, 139, 117));
            _profitValue = AddCard("TOTAL PROFIT", Color.FromArgb(29, 78, 160));
            _countValue = AddCard("INVOICES", Color.FromArgb(22, 32, 43));
            _returnsValue = AddCard("RETURNS", Color.FromArgb(192, 57, 43));

            Label AddCard(string caption, Color color)
            {
                var card = new Panel { Width = 250, Height = 82, Margin = new Padding(0, 0, 12, 0), BackColor = Color.White };
                card.Paint += (_, e) => { using var bar = new SolidBrush(color); e.Graphics.FillRectangle(bar, 0, 0, 5, card.Height); };
                card.Controls.Add(new Label { Text = caption, Location = new Point(16, 8), AutoSize = true, ForeColor = Color.DimGray, Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold) });
                var value = new Label { Location = new Point(14, 30), Size = new Size(230, 44), ForeColor = color, Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoEllipsis = true };
                card.Controls.Add(value);
                cardsPanel.Controls.Add(card);
                return value;
            }
        }

        private async void SalesHistoryForm_Load(object? sender, EventArgs e)
        {
            FitToScreen();
            try { _currencies = await new CurrencyRepository().GetSetAsync(); } catch { /* default LBP / USD */ }

            // A Manager can look at every cashier; a User only at their own sales
            if (AppSession.IsManager)
            {
                try
                {
                    var users = (await new UserRepository().GetAllAsync()).ToList();
                    var choices = new[] { new { Id = AllUsers, Name = "All users" } }
                        .Concat(users.Select(u => new { Id = u.UserId, Name = u.FullName + (u.IsActive ? string.Empty : " (inactive)") }))
                        .ToList();
                    userCombo.DisplayMember = "Name";
                    userCombo.ValueMember = "Id";
                    userCombo.DataSource = choices;
                    userCombo.SelectedValue = _openedFromReport ? _openForUserId ?? AllUsers : AppConfig.CurrentUserId;
                }
                catch
                {
                    userCombo.Visible = userCaption.Visible = false;   // only their own sales then
                }
            }
            else
            {
                userCombo.Visible = userCaption.Visible = false;
            }
            Text = AppSession.IsManager ? "Sales" : $"My sales  -  {AppSession.UserName}";
            _loading = false;
            await LoadSalesAsync();
        }

        // On a small screen the window is taller than the screen: shrink it so the buttons at the bottom (Print receipt) stay visible
        private void FitToScreen()
        {
            var area = Screen.FromControl(this).WorkingArea;
            if (Width <= area.Width && Height <= area.Height) return;
            MinimumSize = Size.Empty;
            Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
            Location = new Point(area.X + (area.Width - Width) / 2, area.Y + (area.Height - Height) / 2);
        }

        private int? SelectedUserId =>
            !AppSession.IsManager ? AppConfig.CurrentUserId
            : userCombo.SelectedValue is int id ? (id == AllUsers ? null : id)
            : AppConfig.CurrentUserId;

        private async Task LoadSalesAsync()
        {
            if (_loading) return;
            if (toPicker.Value.Date < fromPicker.Value.Date) toPicker.Value = fromPicker.Value;
            try
            {
                UseWaitCursor = true;
                _sales = (await _saleRepository.GetSalesByUserAsync(SelectedUserId, fromPicker.Value, toPicker.Value)).ToList();
                invoicesGrid.DataSource = _sales;
                ShowTotals();
                if (_sales.Count > 0) await ShowLinesAsync(_sales[0]);
                else ClearLines("No sales in this period.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the sales:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        // Returns are subtracted: what was really sold and earned
        private void ShowTotals()
        {
            var sales = _sales.Where(s => !s.IsReturn).ToList();
            var returns = _sales.Where(s => s.IsReturn).ToList();
            _salesValue.Text = _currencies.FormatPrimary(_sales.Sum(s => s.SignedTotal));
            _profitValue.Text = _currencies.FormatPrimary(_sales.Sum(s => s.SignedProfit));
            _countValue.Text = $"{sales.Count} sale(s)";
            _returnsValue.Text = returns.Count == 0 ? "none" : $"{returns.Count}  ·  {_currencies.FormatPrimary(returns.Sum(r => r.Total))}";

            var period = fromPicker.Value.Date == toPicker.Value.Date
                ? fromPicker.Value.ToString("ddd dd/MM/yyyy")
                : $"{fromPicker.Value:dd/MM/yyyy} - {toPicker.Value:dd/MM/yyyy}";
            var who = SelectedUserId == null ? "all users" : (userCombo.Visible ? userCombo.Text : AppSession.UserName);
            summaryLabel.Text = $"{period}   ·   {who}   ·   returns are subtracted from sales and profit";
        }

        private async void invoicesGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (invoicesGrid.CurrentRow?.DataBoundItem is SaleHistoryModel sale && !ReferenceEquals(linesGrid.Tag, sale))
                await ShowLinesAsync(sale);
        }

        private async Task ShowLinesAsync(SaleHistoryModel sale)
        {
            linesGrid.Tag = sale;
            try
            {
                linesGrid.DataSource = (await _saleRepository.GetSaleLinesAsync(sale.SaleId)).ToList();
                linesCaption.Text = $"{(sale.IsReturn ? "Return" : "Sale")} {sale.InvoiceNo}   ·   {sale.InvoiceDate:dd/MM/yyyy HH:mm}   ·   {sale.Cashier}"
                                    + $"   ·   total {_currencies.FormatPrimary(sale.Total)}   ·   profit {_currencies.FormatPrimary(sale.Profit)}"
                                    + (string.IsNullOrEmpty(sale.Payments) ? string.Empty : $"   ·   {(sale.IsReturn ? "refunded" : "paid")} {sale.Payments}");
                linesCaption.ForeColor = sale.IsReturn ? Color.FromArgb(192, 57, 43) : Color.FromArgb(22, 32, 43);
            }
            catch (Exception ex)
            {
                ClearLines("Could not load the items: " + ex.Message);
            }
        }

        private void ClearLines(string message)
        {
            linesGrid.Tag = null;
            linesGrid.DataSource = null;
            linesCaption.Text = message;
            linesCaption.ForeColor = Color.DimGray;
        }

        #region Period and user

        private async void todayButton_Click(object? sender, EventArgs e) => await SetPeriodAsync(DateTime.Today, DateTime.Today);

        private async void yesterdayButton_Click(object? sender, EventArgs e) => await SetPeriodAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(-1));

        private async void monthButton_Click(object? sender, EventArgs e) =>
            await SetPeriodAsync(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today);

        private async Task SetPeriodAsync(DateTime from, DateTime to)
        {
            _loading = true;
            fromPicker.Value = from;
            toPicker.Value = to;
            _loading = false;
            await LoadSalesAsync();
        }

        private async void period_ValueChanged(object? sender, EventArgs e) => await LoadSalesAsync();

        private async void userCombo_SelectedIndexChanged(object? sender, EventArgs e) => await LoadSalesAsync();

        private async void refreshButton_Click(object? sender, EventArgs e) => await LoadSalesAsync();

        #endregion

        #region Grid look

        private void invoicesGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e) => FormatColumns(invoicesGrid);

        private void linesGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            FormatColumns(linesGrid);
            if (linesGrid.Columns[nameof(SaleHistoryLineModel.LineNo)] is { } no) no.FillWeight = 25;
            if (linesGrid.Columns[nameof(SaleHistoryLineModel.ItemName)] is { } item) item.FillWeight = 200;
        }

        private static void FormatColumns(DataGridView grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.ValueType == typeof(decimal))
                {
                    column.DefaultCellStyle.Format = column.DataPropertyName == nameof(SaleHistoryLineModel.Quantity) ? "#,0.###" : "#,0.##";
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (column.ValueType == typeof(DateTime))
                {
                    column.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                }
                if (column.DataPropertyName is nameof(SaleHistoryModel.Total) or nameof(SaleHistoryModel.Profit))
                    column.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
            }
            if (grid.Columns[nameof(SaleHistoryModel.Payments)] is { } payments) payments.FillWeight = 160;
            if (grid.Columns[nameof(SaleHistoryModel.PayStatus)] is { } status) status.FillWeight = 130;
        }

        // Returns in red; a loss in red on the profit column
        private void invoicesGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _sales.Count || e.CellStyle == null) return;
            var sale = _sales[e.RowIndex];
            if (sale.IsReturn) e.CellStyle.ForeColor = Color.FromArgb(192, 57, 43);
            else if (invoicesGrid.Columns[e.ColumnIndex].DataPropertyName == nameof(SaleHistoryModel.Profit) && sale.Profit < 0)
                e.CellStyle.ForeColor = Color.Firebrick;
            else if (invoicesGrid.Columns[e.ColumnIndex].DataPropertyName == nameof(SaleHistoryModel.PayStatus))
                e.CellStyle.ForeColor = sale.IsUnpaid ? Color.DarkOrange : Color.FromArgb(34, 139, 117);
        }

        #region Receipt

        // Print receipt: the selected invoice again on this computer's receipt printer
        private async void printButton_Click(object? sender, EventArgs e)
        {
            if (invoicesGrid.CurrentRow?.DataBoundItem is not SaleHistoryModel sale) return;
            printButton.Enabled = false;
            var problem = await Printing.ReceiptPrinter.PrintSaleAsync(sale.SaleId, _currencies);
            printButton.Enabled = true;
            if (problem != null) MessageBox.Show(this, problem, "Print receipt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // Receipt preview: the selected invoice as it prints (works without a printer)
        private async void previewReceiptButton_Click(object? sender, EventArgs e)
        {
            if (invoicesGrid.CurrentRow?.DataBoundItem is not SaleHistoryModel sale) return;
            try
            {
                var settings = Printing.ReceiptSettings.Current ?? await Printing.ReceiptSettings.LoadAsync();
                if (await _saleRepository.GetReceiptAsync(sale.SaleId) is { } receipt)
                    Printing.ReceiptPrinter.ShowPreview(this, receipt, _currencies, settings);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not show the receipt:\n" + ex.Message, "Receipt", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        private void linesGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.CellStyle == null || linesGrid.Columns[e.ColumnIndex].DataPropertyName != nameof(SaleHistoryLineModel.LineProfit)) return;
            if (e.Value is decimal profit && profit < 0) e.CellStyle.ForeColor = Color.Firebrick;
        }

        #endregion
    }
}
