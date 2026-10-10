using Inventory.Models;
using Inventory.Repositories;

namespace Inventory.Reports
{
    // Report 1 - Daily sales summary: per day sales, returns, discounts, net sales, cost, profit and margin.
    // Double-click a day: its invoices (the Sales screen for that day and cashier).
    public sealed class DailySalesReport : ReportDefinition
    {
        private static readonly Color Green = Color.FromArgb(34, 139, 117);
        private static readonly Color Blue = Color.FromArgb(29, 78, 160);
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);
        private static readonly Color Red = Color.FromArgb(192, 57, 43);
        private static readonly Color Orange = Color.FromArgb(217, 119, 6);

        public override string Title => "Daily sales summary";

        public override string Description => "Sales, returns, discounts, cost and profit for each day. Returns are subtracted.";

        public override string? DoubleClickHint => "Double-click a day to see its invoices.";

        public override async Task<ReportResult> LoadAsync(ReportFilter filter)
        {
            var days = await new ReportRepository().GetDailySalesAsync(filter.From, filter.To, filter.UserId);
            var total = DailySalesRow.Total(days);
            var rows = new List<DailySalesRow>(days) { total };

            var money = filter.Currencies;
            return new ReportResult
            {
                Rows = rows,
                Cards = new()
                {
                    new("NET SALES", money.FormatPrimary(total.NetSales), Green,
                        money.FormatSecond(total.NetSales) is { Length: > 0 } s ? s : null),
                    new("PROFIT", money.FormatPrimary(total.Profit), total.Profit < 0 ? Red : Blue,
                        total.Margin is { } m ? $"margin {m:0.#}%" : null),
                    new("SALES", $"{total.SaleCount:#,0}", Dark,
                        total.AverageSale is { } avg ? $"average sale {money.FormatPrimary(avg)}" : null),
                    new("RETURNS", money.FormatPrimary(total.Returns), Red, $"{total.ReturnCount:#,0} return invoice(s)"),
                    new("DISCOUNTS", money.FormatPrimary(total.Discounts), Orange,
                        total.Gross > 0 ? $"{total.Discounts / total.Gross * 100:0.#}% of gross" : null),
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
                    column.DefaultCellStyle.Format = column.DataPropertyName == nameof(DailySalesRow.Margin) ? "0.0" : "#,0";
                }
            }
            if (grid.Columns[nameof(DailySalesRow.DayText)] is { } day) day.FillWeight = 130;
            foreach (var narrow in new[] { nameof(DailySalesRow.SaleCount), nameof(DailySalesRow.ReturnCount), nameof(DailySalesRow.Margin) })
                if (grid.Columns[narrow] is { } c) c.FillWeight = 60;
            foreach (var bold in new[] { nameof(DailySalesRow.NetSales), nameof(DailySalesRow.Profit) })
                if (grid.Columns[bold] is { } c) c.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }

        // Days without sales in grey, a loss in red, the TOTAL row bold on a light background
        public override void FormatCell(object row, DataGridViewColumn column, DataGridViewCellStyle style)
        {
            if (row is not DailySalesRow d) return;
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
            if (column.DataPropertyName is nameof(DailySalesRow.Profit) or nameof(DailySalesRow.Margin) && d.Profit < 0)
                style.ForeColor = style.SelectionForeColor = Red;
            if (column.DataPropertyName is nameof(DailySalesRow.Returns) or nameof(DailySalesRow.ReturnCount) && d.Returns > 0)
                style.ForeColor = style.SelectionForeColor = Red;
        }

        public override bool IsTotalRow(object row) => row is DailySalesRow { IsTotal: true };

        // The invoices of that day (all days of the period for the TOTAL row)
        public override void OnRowDoubleClick(IWin32Window owner, object row, ReportFilter filter)
        {
            if (row is not DailySalesRow d) return;
            var (from, to) = d.IsTotal ? (filter.From, filter.To) : (d.Day, d.Day);
            using var form = new SalesHistoryForm(from, to, filter.UserId);
            form.ShowDialog(owner);
        }
    }
}
