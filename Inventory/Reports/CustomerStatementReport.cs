using Inventory.Models;
using Inventory.Repositories;

namespace Inventory.Reports
{
    // Report 6 - Client statement: the history of a client's unpaid sales (Unpaid F3 in the cashier) and payments,
    // in time order, with what they owe after each line. Filter: one client or all clients.
    // The first row is what was owed before the period, so the last "Owes after" is what is owed at its end.
    // Double-click a sale: the invoices of that day (the Sales screen).
    public sealed class CustomerStatementReport : ReportDefinition
    {
        private static readonly Color Green = Color.FromArgb(34, 139, 117);
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);
        private static readonly Color Orange = Color.FromArgb(217, 119, 6);

        public override string Title => "Client statement";

        public override string Description => "Unpaid sales and payments of a client in time order, with what they owe after each one.";

        public override bool UsesCashierFilter => false;

        public override bool UsesCustomerFilter => true;

        public override (DateTime From, DateTime To) DefaultPeriod => (new DateTime(DateTime.Today.Year, 1, 1), DateTime.Today);

        public override string? DoubleClickHint => "Double-click an unpaid sale to see the invoices of that day.";

        public override async Task<ReportResult> LoadAsync(ReportFilter filter)
        {
            var rows = await new ReportRepository().GetCustomerStatementAsync(filter.From, filter.To, filter.CustomerId);

            // What is owed after each line (all clients: what they owe together)
            var balance = 0m;
            foreach (var row in rows)
            {
                balance += row.Debit - row.Credit;
                row.Balance = balance;
            }

            var opening = rows.Where(r => r.IsOpening).Sum(r => r.Debit - r.Credit);
            var sales = rows.Where(r => r.IsSale).ToList();
            var payments = rows.Where(r => r.IsPayment).ToList();
            var sold = sales.Sum(r => r.Debit);
            var paid = payments.Sum(r => r.Credit);
            rows.Add(new CustomerStatementRow
            {
                IsTotal = true,
                Reference = "TOTAL",
                Debit = sold,
                Credit = paid,
                Balance = balance,
                Details = opening != 0 ? $"owed before the period: {opening:#,0}" : null,
            });

            var money = filter.Currencies;
            return new ReportResult
            {
                Rows = rows,
                Cards = new()
                {
                    new("OWED BEFORE", money.FormatPrimary(opening), Dark, $"before {filter.From:dd/MM/yyyy}"),
                    new("SOLD UNPAID", money.FormatPrimary(sold), Orange, $"{sales.Count:#,0} sale(s)"),
                    new("PAID", money.FormatPrimary(paid), Green, $"{payments.Count:#,0} payment(s)"),
                    new("OWES AT THE END", money.FormatPrimary(balance), balance > 0 ? Orange : Dark,
                        money.FormatSecond(balance) is { Length: > 0 } s ? s : null),
                },
            };
        }

        public override void FormatGrid(DataGridView grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.ValueType is not { } valueType) continue;
                var type = Nullable.GetUnderlyingType(valueType) ?? valueType;
                if (type == typeof(decimal))
                {
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    column.DefaultCellStyle.Format = "#,0";
                }
                else if (type == typeof(DateTime))
                {
                    column.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                    column.FillWeight = 115;
                }
            }
            if (grid.Columns[nameof(CustomerStatementRow.Customer)] is { } customer) customer.FillWeight = 120;
            if (grid.Columns[nameof(CustomerStatementRow.Reference)] is { } reference) reference.FillWeight = 115;
            if (grid.Columns[nameof(CustomerStatementRow.TypeText)] is { } type2) type2.FillWeight = 80;
            if (grid.Columns[nameof(CustomerStatementRow.Details)] is { } details) details.FillWeight = 240;
            if (grid.Columns[nameof(CustomerStatementRow.Status)] is { } status) status.FillWeight = 120;
            if (grid.Columns[nameof(CustomerStatementRow.Balance)] is { } balance) balance.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }

        // Payments in green, what is still unpaid in orange, 0 amounts hidden, the TOTAL row bold on a light background
        public override void FormatCell(object row, DataGridViewColumn column, DataGridViewCellStyle style)
        {
            if (row is not CustomerStatementRow r) return;
            if (r.IsTotal)
            {
                style.BackColor = style.SelectionBackColor = Color.FromArgb(232, 240, 254);
                style.ForeColor = style.SelectionForeColor = Dark;
                style.Font = new Font(style.Font ?? SystemFonts.DefaultFont, FontStyle.Bold);
                return;
            }
            if (r.IsOpening) style.ForeColor = Color.DimGray;
            else if (r.IsPayment) style.ForeColor = style.SelectionForeColor = Green;

            switch (column.DataPropertyName)
            {
                case nameof(CustomerStatementRow.Debit) when r.Debit == 0:
                case nameof(CustomerStatementRow.Credit) when r.Credit == 0:
                    style.ForeColor = style.SelectionForeColor = style.BackColor;   // an empty cell reads better than 0
                    break;
                case nameof(CustomerStatementRow.Status) when r.DueLbp > 0:
                    style.ForeColor = style.SelectionForeColor = Orange;
                    break;
                case nameof(CustomerStatementRow.Status):
                    style.ForeColor = style.SelectionForeColor = Green;
                    break;
            }
        }

        public override bool IsTotalRow(object row) => row is CustomerStatementRow { IsTotal: true };

        // The invoices of the day of that sale
        public override void OnRowDoubleClick(IWin32Window owner, object row, ReportFilter filter)
        {
            if (row is not CustomerStatementRow { IsSale: true, EntryDate: { } date }) return;
            using var form = new SalesHistoryForm(date, date, null);
            form.ShowDialog(owner);
        }
    }
}
