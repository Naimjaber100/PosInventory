using Inventory.Models;
using Inventory.Repositories;

namespace Inventory.Reports
{
    // Report 2 - Sales by item: per item the quantity sold and returned, net sales, cost, profit and margin,
    // best selling first. Items sold at a loss are red (check their cost or price).
    public sealed class SalesByItemReport : ReportDefinition
    {
        private static readonly Color Green = Color.FromArgb(34, 139, 117);
        private static readonly Color Blue = Color.FromArgb(29, 78, 160);
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);
        private static readonly Color Red = Color.FromArgb(192, 57, 43);
        private static readonly Color Orange = Color.FromArgb(217, 119, 6);

        public override string Title => "Sales by item";

        public override string Description =>
            "Quantity, net sales, cost and profit for each item, best selling first. Quantities are in the item's unit; returns are subtracted.";

        public override async Task<ReportResult> LoadAsync(ReportFilter filter)
        {
            var items = await new ReportRepository().GetSalesByItemAsync(filter.From, filter.To, filter.UserId);
            var total = SalesByItemRow.Total(items);
            foreach (var item in items)
                item.ShareOfSales = total.NetSales > 0 ? Math.Round(item.NetSales / total.NetSales * 100, 1) : null;
            var rows = new List<SalesByItemRow>(items) { total };

            var money = filter.Currencies;
            var top = items.FirstOrDefault(i => i.NetSales > 0);
            var mostProfit = items.Where(i => i.Profit > 0).MaxBy(i => i.Profit);
            var losses = items.Where(i => i.Profit < 0).ToList();
            return new ReportResult
            {
                Rows = rows,
                Cards = new()
                {
                    new("NET SALES", money.FormatPrimary(total.NetSales), Green,
                        money.FormatSecond(total.NetSales) is { Length: > 0 } s ? s : null),
                    new("PROFIT", money.FormatPrimary(total.Profit), total.Profit < 0 ? Red : Blue,
                        total.Margin is { } m ? $"margin {m:0.#}%" : null),
                    new("ITEMS SOLD", $"{items.Count(i => i.NetQty > 0):#,0}", Dark, $"{items.Count:#,0} item(s) with sales or returns"),
                    new("BEST SELLER", top?.ItemName ?? "-", Green,
                        top != null ? $"{money.FormatPrimary(top.NetSales)}  ·  {top.ShareOfSales:0.#}% of sales" : null),
                    new("MOST PROFIT", mostProfit?.ItemName ?? "-", Blue,
                        mostProfit != null ? $"{money.FormatPrimary(mostProfit.Profit)} profit" : null),
                    new("SOLD AT A LOSS", $"{losses.Count:#,0} item(s)", losses.Count > 0 ? Red : Dark,
                        losses.Count > 0 ? $"{money.FormatPrimary(losses.Sum(i => i.Profit))}  ·  check cost / price" : "none"),
                },
            };
        }

        public override void FormatGrid(DataGridView grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.ValueType is not { } valueType) continue;
                var type = Nullable.GetUnderlyingType(valueType) ?? valueType;
                if (type != typeof(decimal) && type != typeof(long)) continue;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.DefaultCellStyle.Format = column.DataPropertyName switch
                {
                    nameof(SalesByItemRow.Margin) or nameof(SalesByItemRow.ShareOfSales) => "0.0",
                    nameof(SalesByItemRow.QtySold) or nameof(SalesByItemRow.QtyReturned) or nameof(SalesByItemRow.NetQty) => "#,0.###",
                    _ => "#,0",
                };
            }
            if (grid.Columns[nameof(SalesByItemRow.ItemName)] is { } name) name.FillWeight = 170;
            if (grid.Columns[nameof(SalesByItemRow.CategoryName)] is { } category) category.FillWeight = 90;
            foreach (var narrow in new[] { nameof(SalesByItemRow.QtySold), nameof(SalesByItemRow.QtyReturned), nameof(SalesByItemRow.NetQty),
                                           nameof(SalesByItemRow.BaseUnit), nameof(SalesByItemRow.Margin), nameof(SalesByItemRow.ShareOfSales),
                                           nameof(SalesByItemRow.InvoiceCount) })
                if (grid.Columns[narrow] is { } c) c.FillWeight = 55;
            foreach (var bold in new[] { nameof(SalesByItemRow.NetSales), nameof(SalesByItemRow.Profit) })
                if (grid.Columns[bold] is { } c) c.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }

        // A loss in red (whole profit and margin), returns in red, the TOTAL row bold on a light background
        public override void FormatCell(object row, DataGridViewColumn column, DataGridViewCellStyle style)
        {
            if (row is not SalesByItemRow item) return;
            if (item.IsTotal)
            {
                style.BackColor = style.SelectionBackColor = Color.FromArgb(232, 240, 254);
                style.ForeColor = style.SelectionForeColor = Dark;
                style.Font = new Font(style.Font ?? SystemFonts.DefaultFont, FontStyle.Bold);
            }
            if (column.DataPropertyName is nameof(SalesByItemRow.Profit) or nameof(SalesByItemRow.Margin) && item.Profit < 0)
                style.ForeColor = style.SelectionForeColor = Red;
            if (column.DataPropertyName == nameof(SalesByItemRow.QtyReturned) && item.QtyReturned > 0)
                style.ForeColor = style.SelectionForeColor = Red;
            if (column.DataPropertyName == nameof(SalesByItemRow.Discounts) && item.Discounts > 0 && !item.IsTotal)
                style.ForeColor = style.SelectionForeColor = Orange;
            // Sold with no cost recorded (cost 0): the profit is not real, show it in orange
            if (column.DataPropertyName == nameof(SalesByItemRow.Cost) && item.Cost == 0 && item.NetSales != 0 && !item.IsTotal)
                style.ForeColor = style.SelectionForeColor = Orange;
        }

        public override bool IsTotalRow(object row) => row is SalesByItemRow { IsTotal: true };
    }
}
