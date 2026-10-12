using Inventory.Models;
using Inventory.Repositories;

namespace Inventory.Reports
{
    // Report 5 - Unpaid by client: every client who still owes something for unpaid sales (Unpaid F3 in the cashier),
    // as it is now, biggest amount first. No period: an old debt counts as much as a new one.
    // Double-click a client: receive a payment (it settles the oldest invoices first).
    public sealed class UnpaidByCustomerReport : ReportDefinition
    {
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);
        private static readonly Color Red = Color.FromArgb(192, 57, 43);
        private static readonly Color Orange = Color.FromArgb(217, 119, 6);

        public override string Title => "Unpaid by client";

        public override string Description => "What each client still owes now, with the unpaid invoices and the oldest one. Biggest amount first.";

        public override bool UsesCashierFilter => false;

        public override bool UsesPeriodFilter => false;

        public override string? DoubleClickHint => "Double-click a client to receive a payment.";

        public override async Task<ReportResult> LoadAsync(ReportFilter filter)
        {
            var clients = await new ReportRepository().GetUnpaidByCustomerAsync();
            var total = UnpaidByCustomerRow.Total(clients);
            foreach (var client in clients)
                client.Share = total.Unpaid > 0 ? Math.Round(client.Unpaid / total.Unpaid * 100, 1) : null;
            var rows = new List<UnpaidByCustomerRow>(clients) { total };

            var money = filter.Currencies;
            var biggest = clients.FirstOrDefault();
            var overLimit = clients.Count(c => c.OverLimit);
            return new ReportResult
            {
                Rows = rows,
                Cards = new()
                {
                    new("TOTAL UNPAID", money.FormatPrimary(total.Unpaid), total.Unpaid > 0 ? Orange : Dark,
                        money.FormatSecond(total.Unpaid) is { Length: > 0 } s ? s : null),
                    new("CLIENTS WHO OWE", $"{clients.Count:#,0}", Dark, $"{total.InvoiceCount:#,0} unpaid invoice(s)"),
                    new("BIGGEST", biggest != null ? money.FormatPrimary(biggest.Unpaid) : "-", Dark, biggest?.Name),
                    new("OLDEST UNPAID", total.Days is { } days ? $"{days:#,0} day(s)" : "-", Dark,
                        total.OldestUnpaid is { } oldest ? $"since {oldest:dd/MM/yyyy}" : null),
                    new("OVER CREDIT LIMIT", $"{overLimit:#,0}", overLimit > 0 ? Red : Dark, "client(s)"),
                },
            };
        }

        public override void FormatGrid(DataGridView grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.ValueType is not { } valueType) continue;
                var type = Nullable.GetUnderlyingType(valueType) ?? valueType;
                if (type == typeof(decimal) || type == typeof(long))
                {
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    column.DefaultCellStyle.Format = column.DataPropertyName == nameof(UnpaidByCustomerRow.Share) ? "0.0" : "#,0";
                }
                else if (type == typeof(DateTime))
                {
                    column.DefaultCellStyle.Format = "dd/MM/yyyy";
                }
            }
            if (grid.Columns[nameof(UnpaidByCustomerRow.Name)] is { } name) name.FillWeight = 170;
            if (grid.Columns[nameof(UnpaidByCustomerRow.Phone)] is { } phone) phone.FillWeight = 110;
            foreach (var narrow in new[] { nameof(UnpaidByCustomerRow.InvoiceCount), nameof(UnpaidByCustomerRow.Days), nameof(UnpaidByCustomerRow.Share) })
                if (grid.Columns[narrow] is { } c) c.FillWeight = 65;
            if (grid.Columns[nameof(UnpaidByCustomerRow.Unpaid)] is { } unpaid) unpaid.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }

        // What is owed in orange (red when it is over the client's credit limit), the TOTAL row bold on a light background
        public override void FormatCell(object row, DataGridViewColumn column, DataGridViewCellStyle style)
        {
            if (row is not UnpaidByCustomerRow c) return;
            if (c.IsTotal)
            {
                style.BackColor = style.SelectionBackColor = Color.FromArgb(232, 240, 254);
                style.ForeColor = style.SelectionForeColor = Dark;
                style.Font = new Font(style.Font ?? SystemFonts.DefaultFont, FontStyle.Bold);
                return;
            }
            if (column.DataPropertyName == nameof(UnpaidByCustomerRow.Unpaid))
                style.ForeColor = style.SelectionForeColor = c.OverLimit ? Red : Orange;
            if (column.DataPropertyName == nameof(UnpaidByCustomerRow.CreditLimitLbp) && c.OverLimit)
                style.ForeColor = style.SelectionForeColor = Red;
        }

        public override bool IsTotalRow(object row) => row is UnpaidByCustomerRow { IsTotal: true };

        // The client pays: the payment window with their unpaid invoices (Refresh shows the new amounts)
        public override void OnRowDoubleClick(IWin32Window owner, object row, ReportFilter filter)
        {
            if (row is not UnpaidByCustomerRow { IsTotal: false } c) return;
            var customer = new CustomerModel { CustomerId = c.CustomerId, Name = c.Name, Phone = c.Phone, BalanceLbp = c.Unpaid };
            using var form = new CustomerPaymentForm(filter.Currencies, customer);
            form.ShowDialog(owner);
        }
    }
}
