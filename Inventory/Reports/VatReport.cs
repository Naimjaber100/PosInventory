using Inventory.Models;
using Inventory.Repositories;

namespace Inventory.Reports
{
    // Report 11 - TVA: what was charged on sales and given back on returns, per day and TVA rate, for the declaration.
    // Only sales made while TVA was on (Settings > Company). A total row per rate, then the grand TOTAL.
    // Double-click a day: its invoices (the Sales screen for that day and cashier).
    public sealed class VatReport : ReportDefinition
    {
        private static readonly Color Green = Color.FromArgb(34, 139, 117);
        private static readonly Color Blue = Color.FromArgb(29, 78, 160);
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);
        private static readonly Color Red = Color.FromArgb(192, 57, 43);

        public override string Title => "TVA";

        public override string Description => "TVA charged on sales, less the TVA given back on returns, per day and rate.";

        public override string? DoubleClickHint => "Double-click a day to see its invoices.";

        public override (DateTime From, DateTime To) DefaultPeriod =>
            (new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today);

        public override async Task<ReportResult> LoadAsync(ReportFilter filter)
        {
            var days = await new ReportRepository().GetVatAsync(filter.From, filter.To, filter.UserId);
            var rows = new List<VatReportRow>(days);
            var rates = days.GroupBy(d => d.VatRate).OrderBy(g => g.Key).ToList();
            if (rates.Count > 1)
                rows.AddRange(rates.Select(g => VatReportRow.TotalOf(g.ToList(), g.Key)));
            var total = VatReportRow.TotalOf(days, rates.Count == 1 ? rates[0].Key : null);
            rows.Add(total);

            var money = filter.Currencies;
            return new ReportResult
            {
                Rows = rows,
                Cards = new()
                {
                    new("NET TVA", money.FormatPrimary(total.NetVat), Green,
                        money.FormatSecond(total.NetVat) is { Length: > 0 } s ? s : null),
                    new("NET WITHOUT TVA", money.FormatPrimary(total.NetBase), Blue, "sales less returns"),
                    new("TVA ON SALES", money.FormatPrimary(total.SalesVat), Dark,
                        total.SalesBase > 0 ? $"on {money.FormatPrimary(total.SalesBase)}" : null),
                    new("TVA ON RETURNS", money.FormatPrimary(total.ReturnsVat), Red,
                        total.ReturnsBase > 0 ? $"on {money.FormatPrimary(total.ReturnsBase)}" : null),
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
                    column.DefaultCellStyle.Format = column.DataPropertyName == nameof(VatReportRow.VatRate) ? "0.##" : "#,0";
                }
            }
            if (grid.Columns[nameof(VatReportRow.DayText)] is { } day) day.FillWeight = 130;
            foreach (var narrow in new[] { nameof(VatReportRow.VatRate), nameof(VatReportRow.SaleCount) })
                if (grid.Columns[narrow] is { } c) c.FillWeight = 60;
            foreach (var bold in new[] { nameof(VatReportRow.NetBase), nameof(VatReportRow.NetVat) })
                if (grid.Columns[bold] is { } c) c.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }

        // Total rows bold on a light background, returns in red
        public override void FormatCell(object row, DataGridViewColumn column, DataGridViewCellStyle style)
        {
            if (row is not VatReportRow v) return;
            if (v.IsTotal)
            {
                style.BackColor = style.SelectionBackColor = Color.FromArgb(232, 240, 254);
                style.ForeColor = style.SelectionForeColor = Dark;
                style.Font = new Font(style.Font ?? SystemFonts.DefaultFont, FontStyle.Bold);
            }
            if (column.DataPropertyName is nameof(VatReportRow.ReturnsBase) or nameof(VatReportRow.ReturnsVat) && v.ReturnsBase != 0)
                style.ForeColor = style.SelectionForeColor = Red;
        }

        public override bool IsTotalRow(object row) => row is VatReportRow { IsTotal: true };

        // The invoices of that day (all days of the period for a total row)
        public override void OnRowDoubleClick(IWin32Window owner, object row, ReportFilter filter)
        {
            if (row is not VatReportRow v) return;
            var (from, to) = v.Day is { } d ? (d, d) : (filter.From, filter.To);
            using var form = new SalesHistoryForm(from, to, filter.UserId);
            form.ShowDialog(owner);
        }
    }
}
