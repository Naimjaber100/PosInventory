using Inventory.Models;
using Inventory.Repositories;

namespace Inventory.Reports
{
    // Report 4 - Sales and unpaid: per day the sales, what is still unpaid of them (Unpaid F3 in the cashier)
    // and the difference (what was really collected). A client who pays later lowers the unpaid of the day of the sale.
    // Double-click a day: its invoices (the Sales screen shows Unpaid / Partly paid / Paid).
    public sealed class SalesUnpaidReport : ReportDefinition
    {
        private static readonly Color Green = Color.FromArgb(34, 139, 117);
        private static readonly Color Blue = Color.FromArgb(29, 78, 160);
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);
        private static readonly Color Red = Color.FromArgb(192, 57, 43);
        private static readonly Color Orange = Color.FromArgb(217, 119, 6);

        public override string Title => "Sales and unpaid";

        public override string Description => "Total sales, what is still unpaid of them, and the difference (collected). Returns are subtracted from sales.";

        public override string? DoubleClickHint => "Double-click a day to see its invoices and which ones are unpaid.";

        public override async Task<ReportResult> LoadAsync(ReportFilter filter)
        {
            var days = await new ReportRepository().GetSalesUnpaidAsync(filter.From, filter.To, filter.UserId);
            var total = SalesUnpaidRow.Total(days);
            var rows = new List<SalesUnpaidRow>(days) { total };

            var money = filter.Currencies;
            string? Second(decimal amount) => money.FormatSecond(amount) is { Length: > 0 } s ? s : null;
            return new ReportResult
            {
                Rows = rows,
                Cards = new()
                {
                    new("TOTAL SALES", money.FormatPrimary(total.NetSales), Blue, Second(total.NetSales)),
                    new("TOTAL UNPAID", money.FormatPrimary(total.Unpaid), total.Unpaid > 0 ? Orange : Dark,
                        $"{total.UnpaidCount:#,0} invoice(s)" + (total.UnpaidShare is { } share ? $"  ·  {share:0.#}% of sales" : string.Empty)),
                    new("DIFFERENCE (COLLECTED)", money.FormatPrimary(total.Collected), Green, Second(total.Collected)),
                    new("SOLD UNPAID", money.FormatPrimary(total.OnAccount), Dark,
                        total.OnAccount > 0 ? $"{money.FormatPrimary(total.PaidLater)} of it paid later" : null),
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
                    column.DefaultCellStyle.Format = column.DataPropertyName == nameof(SalesUnpaidRow.UnpaidShare) ? "0.0" : "#,0";
                }
            }
            if (grid.Columns[nameof(SalesUnpaidRow.DayText)] is { } day) day.FillWeight = 130;
            if (grid.Columns[nameof(SalesUnpaidRow.Collected)] is { } collected) collected.FillWeight = 130;
            foreach (var narrow in new[] { nameof(SalesUnpaidRow.SaleCount), nameof(SalesUnpaidRow.UnpaidCount), nameof(SalesUnpaidRow.UnpaidShare) })
                if (grid.Columns[narrow] is { } c) c.FillWeight = 60;
            foreach (var bold in new[] { nameof(SalesUnpaidRow.NetSales), nameof(SalesUnpaidRow.Unpaid), nameof(SalesUnpaidRow.Collected) })
                if (grid.Columns[bold] is { } c) c.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }

        // Days without sales in grey, what is still unpaid in orange, the TOTAL row bold on a light background
        public override void FormatCell(object row, DataGridViewColumn column, DataGridViewCellStyle style)
        {
            if (row is not SalesUnpaidRow d) return;
            if (d.IsTotal)
            {
                style.BackColor = style.SelectionBackColor = Color.FromArgb(232, 240, 254);
                style.ForeColor = style.SelectionForeColor = Dark;
                style.Font = new Font(style.Font ?? SystemFonts.DefaultFont, FontStyle.Bold);
            }
            else if (d.IsEmpty)
            {
                style.ForeColor = Color.Silver;
            }
            if (column.DataPropertyName is nameof(SalesUnpaidRow.Unpaid) or nameof(SalesUnpaidRow.UnpaidCount) or nameof(SalesUnpaidRow.UnpaidShare) && d.Unpaid > 0)
                style.ForeColor = style.SelectionForeColor = Orange;
            if (column.DataPropertyName == nameof(SalesUnpaidRow.Returns) && d.Returns > 0)
                style.ForeColor = style.SelectionForeColor = Red;
        }

        public override bool IsTotalRow(object row) => row is SalesUnpaidRow { IsTotal: true };

        // The invoices of that day (all days of the period for the TOTAL row)
        public override void OnRowDoubleClick(IWin32Window owner, object row, ReportFilter filter)
        {
            if (row is not SalesUnpaidRow d) return;
            var (from, to) = d.IsTotal ? (filter.From, filter.To) : (d.Day, d.Day);
            using var form = new SalesHistoryForm(from, to, filter.UserId);
            form.ShowDialog(owner);
        }
    }
}
