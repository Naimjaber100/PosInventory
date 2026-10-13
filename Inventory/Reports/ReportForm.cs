using Inventory.Models;
using Inventory.Repositories;

namespace Inventory.Reports
{
    // The screen every report uses: period (quick buttons or From / To), cashier, total cards, the rows,
    // export to Excel. What is shown comes from the report (ReportDefinition).
    public partial class ReportForm : Form
    {
        private const int AllUsers = 0;
        private const int AllCustomers = 0;
        private const int WalkInCustomerId = 1;

        private readonly ReportDefinition _report;
        private CurrencySet _currencies = CurrencySet.Default;
        private ReportFilter _filter = new();
        private bool _loading = true;
        private List<string> _totals = new();   // the total cards as text, for Print

        public ReportForm(ReportDefinition report)
        {
            InitializeComponent();
            _report = report;
            Text = report.Title;
            titleLabel.Text = report.Title;
            descriptionLabel.Text = report.Description;
            hintLabel.Text = report.DoubleClickHint ?? string.Empty;
            userCaption.Visible = userCombo.Visible = report.UsesCashierFilter;
            customerCaption.Visible = customerCombo.Visible = report.UsesCustomerFilter;
            if (!report.UsesPeriodFilter)
            {
                foreach (Control control in filterPanel.Controls)
                    if (control != userCaption && control != userCombo && control != customerCaption && control != customerCombo) control.Visible = false;
                filterPanel.Visible = report.UsesCashierFilter || report.UsesCustomerFilter;
            }
            (fromPicker.Value, toPicker.Value) = report.DefaultPeriod;
        }

        private async void ReportForm_Load(object? sender, EventArgs e)
        {
            try { _currencies = await new CurrencyRepository().GetSetAsync(); } catch { /* LBP / USD defaults */ }

            if (_report.UsesCashierFilter)
            {
                try
                {
                    var users = (await new UserRepository().GetAllAsync()).ToList();
                    userCombo.DisplayMember = "Name";
                    userCombo.ValueMember = "Id";
                    userCombo.DataSource = new[] { new { Id = AllUsers, Name = "All users" } }
                        .Concat(users.Select(u => new { Id = u.UserId, Name = u.FullName + (u.IsActive ? string.Empty : " (inactive)") }))
                        .ToList();
                    userCombo.SelectedValue = AllUsers;
                }
                catch
                {
                    userCaption.Visible = userCombo.Visible = false;
                }
            }
            if (_report.UsesCustomerFilter)
            {
                try
                {
                    // Every client, also the inactive ones (their history stays); not the walk-in customer
                    var customers = (await new CustomerRepository().GetAllAsync(null, includeInactive: true))
                        .Where(c => c.CustomerId != WalkInCustomerId).ToList();
                    customerCombo.DisplayMember = "Name";
                    customerCombo.ValueMember = "Id";
                    customerCombo.DataSource = new[] { new { Id = AllCustomers, Name = "All clients" } }
                        .Concat(customers.Select(c => new { Id = c.CustomerId, Name = c.Name + (string.IsNullOrWhiteSpace(c.Phone) ? string.Empty : $"  ·  {c.Phone}") }))
                        .ToList();
                    customerCombo.SelectedValue = AllCustomers;
                }
                catch
                {
                    customerCaption.Visible = customerCombo.Visible = false;
                }
            }
            _loading = false;
            await LoadReportAsync();
        }

        private async Task LoadReportAsync()
        {
            if (_loading) return;
            if (toPicker.Value.Date < fromPicker.Value.Date) toPicker.Value = fromPicker.Value;
            var userId = userCombo.Visible && userCombo.SelectedValue is int id && id != AllUsers ? id : (int?)null;
            var customerId = customerCombo.Visible && customerCombo.SelectedValue is int cid && cid != AllCustomers ? cid : (int?)null;
            _filter = new ReportFilter
            {
                From = fromPicker.Value.Date,
                To = toPicker.Value.Date,
                UserId = userId,
                UserName = userId == null ? "All users" : userCombo.Text,
                CustomerId = customerId,
                CustomerName = customerId == null ? "All clients" : customerCombo.Text,
                Currencies = _currencies,
            };

            try
            {
                UseWaitCursor = true;
                var started = DateTime.Now;
                var result = await _report.LoadAsync(_filter);
                grid.DataSource = result.Rows;
                ShowCards(result.Cards);
                _totals = result.Cards.Select(c => $"{c.Caption}: {c.Value}" + (c.Note != null ? $" ({c.Note})" : string.Empty)).ToList();
                filterLabel.Text = (_report.UsesPeriodFilter ? _filter.PeriodText : $"As of {DateTime.Now:ddd dd/MM/yyyy HH:mm}")
                                   + (_report.UsesCashierFilter ? $"   ·   {_filter.UserName}" : string.Empty)
                                   + (_report.UsesCustomerFilter ? $"   ·   {_filter.CustomerName}" : string.Empty)
                                   + $"   ·   amounts in {_currencies.Primary.Code}";
                timeLabel.Text = $"{result.Rows.Count} row(s) in {(DateTime.Now - started).TotalSeconds:0.00} s";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the report:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        // Total cards: caption, value, a small note, a colored bar
        private void ShowCards(IEnumerable<ReportCard> cards)
        {
            cardsPanel.SuspendLayout();
            foreach (var old in cardsPanel.Controls.Cast<Control>().ToList())
            {
                cardsPanel.Controls.Remove(old);
                old.Dispose();
            }
            foreach (var card in cards)
            {
                var panel = new Panel { Width = 238, Height = 92, Margin = new Padding(0, 0, 10, 0), BackColor = Color.White, Tag = "card" };
                var color = card.Color;
                panel.Paint += (_, e) => { using var bar = new SolidBrush(color); e.Graphics.FillRectangle(bar, 0, 0, 5, panel.Height); };
                panel.Controls.Add(new Label { Text = card.Caption, Location = new Point(16, 8), AutoSize = true, ForeColor = Color.DimGray, Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold) });
                panel.Controls.Add(new Label { Text = card.Value, Location = new Point(13, 28), Size = new Size(222, 36), ForeColor = card.Color, Font = new Font("Segoe UI", 14.5F, FontStyle.Bold), AutoEllipsis = true, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right });
                if (card.Note != null)
                    panel.Controls.Add(new Label { Text = card.Note, Location = new Point(16, 66), Size = new Size(218, 20), ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.5F), AutoEllipsis = true, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right });
                cardsPanel.Controls.Add(panel);
            }
            FitCards();
            cardsPanel.ResumeLayout();
        }

        // Cards share the width of the window (238 px at most), so 5, 6 or more cards always fit
        private void FitCards()
        {
            var cards = cardsPanel.Controls.Cast<Control>().Where(c => Equals(c.Tag, "card")).ToList();
            if (cards.Count == 0) return;
            var room = cardsPanel.ClientSize.Width - cardsPanel.Padding.Horizontal - cards.Count * 10;
            var width = Math.Clamp(room / cards.Count, 150, 238);
            foreach (var card in cards) card.Width = width;
        }

        private void cardsPanel_Resize(object? sender, EventArgs e) => FitCards();

        #region Period and filters

        private async Task SetPeriodAsync(DateTime from, DateTime to)
        {
            _loading = true;
            fromPicker.Value = from;
            toPicker.Value = to;
            _loading = false;
            await LoadReportAsync();
        }

        private async void todayButton_Click(object? sender, EventArgs e) => await SetPeriodAsync(DateTime.Today, DateTime.Today);

        private async void yesterdayButton_Click(object? sender, EventArgs e) => await SetPeriodAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(-1));

        // Monday to today
        private async void weekButton_Click(object? sender, EventArgs e)
        {
            var monday = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
            await SetPeriodAsync(monday, DateTime.Today);
        }

        private async void monthButton_Click(object? sender, EventArgs e) =>
            await SetPeriodAsync(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today);

        private async void lastMonthButton_Click(object? sender, EventArgs e)
        {
            var first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
            await SetPeriodAsync(first, first.AddMonths(1).AddDays(-1));
        }

        private async void yearButton_Click(object? sender, EventArgs e) =>
            await SetPeriodAsync(new DateTime(DateTime.Today.Year, 1, 1), DateTime.Today);

        private async void period_ValueChanged(object? sender, EventArgs e) => await LoadReportAsync();

        private async void userCombo_SelectedIndexChanged(object? sender, EventArgs e) => await LoadReportAsync();

        private async void customerCombo_SelectedIndexChanged(object? sender, EventArgs e) => await LoadReportAsync();

        private async void refreshButton_Click(object? sender, EventArgs e) => await LoadReportAsync();

        #endregion

        #region Grid

        private void grid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            _report.FormatGrid(grid);
            grid.ClearSelection();   // no row highlighted until one is clicked
        }

        private void grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.CellStyle == null || grid.Rows[e.RowIndex].DataBoundItem is not { } row) return;
            _report.FormatCell(row, grid.Columns[e.ColumnIndex], e.CellStyle);
        }

        private void grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || grid.Rows[e.RowIndex].DataBoundItem is not { } row) return;
            _report.OnRowDoubleClick(this, row, _filter);
        }

        #endregion

        // Print: the report as it shows (title, filters, totals, rows), on the printer chosen, after a preview
        private void printButton_Click(object? sender, EventArgs e)
        {
            if (grid.Rows.Count == 0) { MessageBox.Show(this, "There is nothing to print.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            try
            {
                ReportPrint.Print(this, _report.Title, filterLabel.Text, _totals, grid, _report.IsTotalRow, _report.FormatCell);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not print:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void excelButton_Click(object? sender, EventArgs e)
        {
            if (grid.Rows.Count == 0) { MessageBox.Show(this, "There is nothing to export.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using var dialog = new SaveFileDialog
            {
                Title = "Export to Excel",
                Filter = "Excel workbook (*.xlsx)|*.xlsx",
                FileName = _report.UsesPeriodFilter
                    ? $"{_report.Title} {_filter.From:yyyy-MM-dd} to {_filter.To:yyyy-MM-dd}.xlsx"
                    : $"{_report.Title} {DateTime.Today:yyyy-MM-dd}.xlsx",
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                ReportExcel.Save(dialog.FileName, _report.Title, filterLabel.Text, grid, _report.IsTotalRow);
                if (MessageBox.Show(this, "Saved. Open it now?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
            catch (IOException)
            {
                MessageBox.Show(this, "The file could not be saved. If it is open in Excel, close it and try again.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not export:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
