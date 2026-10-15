using Inventory.Models;
using Inventory.Repositories;

namespace Inventory.Reports
{
    // Report 8 - Purchases by item: per item the quantity bought and what it cost, with the average, last, lowest
    // and highest unit cost of the period and the average cost now. Filter: one supplier or all.
    // Unit costs are per base unit (a box of 12 counts as 12 pieces), after the invoice discount.
    // A last cost above the lowest of the period is orange: the price went up.
    public sealed class PurchasesByItemReport : ReportDefinition
    {
        private static readonly Color Green = Color.FromArgb(34, 139, 117);
        private static readonly Color Blue = Color.FromArgb(29, 78, 160);
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);
        private static readonly Color Orange = Color.FromArgb(217, 119, 6);

        public override string Title => "Purchases by item";

        public override string Description =>
            "Quantity bought and cost of each item, biggest cost first. Unit costs are per piece / kg, after the invoice discount.";

        public override bool UsesCashierFilter => false;

        public override bool UsesSupplierFilter => true;

        public override async Task<ReportResult> LoadAsync(ReportFilter filter)
        {
            var items = await new ReportRepository().GetPurchasesByItemAsync(filter.From, filter.To, filter.SupplierId);
            var total = PurchasesByItemRow.TotalOf(items);
            foreach (var item in items)
                item.Share = total.Cost > 0 ? Math.Round(item.Cost / total.Cost * 100, 1) : null;
            var rows = new List<PurchasesByItemRow>(items) { total };

            var money = filter.Currencies;
            var top = items.FirstOrDefault();
            var upItems = items.Where(i => i.CostWentUp).ToList();
            var mostOften = items.MaxBy(i => i.InvoiceCount);
            return new ReportResult
            {
                Rows = rows,
                Cards = new()
                {
                    new("TOTAL COST", money.FormatPrimary(total.Cost), Blue,
                        money.FormatSecond(total.Cost) is { Length: > 0 } s ? s : null),
                    new("ITEMS BOUGHT", $"{items.Count:#,0}", Dark, null),
                    new("BIGGEST COST", top?.ItemName ?? "-", Blue,
                        top != null ? $"{money.FormatPrimary(top.Cost)}  ·  {top.Share:0.#}% of purchases" : null),
                    new("BOUGHT MOST OFTEN", mostOften?.ItemName ?? "-", Green,
                        mostOften != null ? $"on {mostOften.InvoiceCount:#,0} purchase(s)" : null),
                    new("COST WENT UP", $"{upItems.Count:#,0} item(s)", upItems.Count > 0 ? Orange : Dark,
                        upItems.Count > 0 ? "last cost above the lowest of the period" : "none"),
                },
            };
        }

        public override void FormatGrid(DataGridView grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.ValueType is not { } valueType) continue;
                var type = Nullable.GetUnderlyingType(valueType) ?? valueType;
                if (type == typeof(DateTime))
                {
                    column.DefaultCellStyle.Format = "dd/MM/yyyy";
                    continue;
                }
                if (type != typeof(decimal) && type != typeof(long)) continue;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.DefaultCellStyle.Format = column.DataPropertyName switch
                {
                    nameof(PurchasesByItemRow.Share) => "0.0",
                    nameof(PurchasesByItemRow.Qty) => "#,0.###",
                    _ => "#,0",
                };
            }
            if (grid.Columns[nameof(PurchasesByItemRow.ItemName)] is { } name) name.FillWeight = 160;
            if (grid.Columns[nameof(PurchasesByItemRow.Suppliers)] is { } suppliers) suppliers.FillWeight = 110;
            foreach (var narrow in new[] { nameof(PurchasesByItemRow.BaseUnit), nameof(PurchasesByItemRow.Share) })
                if (grid.Columns[narrow] is { } c) c.FillWeight = 50;
            if (grid.Columns[nameof(PurchasesByItemRow.InvoiceCount)] is { } invoices) invoices.FillWeight = 65;
            if (grid.Columns[nameof(PurchasesByItemRow.Cost)] is { } cost) cost.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }

        // A cost that went up in orange, the TOTAL row bold on a light background
        public override void FormatCell(object row, DataGridViewColumn column, DataGridViewCellStyle style)
        {
            if (row is not PurchasesByItemRow item) return;
            if (item.IsTotal)
            {
                style.BackColor = style.SelectionBackColor = Color.FromArgb(232, 240, 254);
                style.ForeColor = style.SelectionForeColor = Dark;
                style.Font = new Font(style.Font ?? SystemFonts.DefaultFont, FontStyle.Bold);
                return;
            }
            if (column.DataPropertyName is nameof(PurchasesByItemRow.LastUnitCost) or nameof(PurchasesByItemRow.MaxUnitCost) && item.CostWentUp)
                style.ForeColor = style.SelectionForeColor = Orange;
        }

        public override bool IsTotalRow(object row) => row is PurchasesByItemRow { IsTotal: true };
    }
}
