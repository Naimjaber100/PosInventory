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

        public override bool HasSlip => true;

        // For the 80 mm printer, per item: "qty unit x average price   net sales", then "Cost ...   Profit ... (margin)".
        // The average price is what one unit sold for after discounts (net sales / net qty).
        public override Printing.ReportSlip ToSlip(ReportResult result, ReportFilter filter, string company)
        {
            var primary = filter.Currencies.Primary;
            string N(decimal amount) => Math.Round(amount, primary.Decimals).ToString(primary.Decimals == 0 ? "#,0" : "#,0." + new string('0', primary.Decimals));
            static string Qty(decimal? qty) => (qty ?? 0).ToString(qty % 1 == 0 ? "#,0" : "#,0.###");

            var items = result.Rows.OfType<SalesByItemRow>().Where(i => !i.IsTotal).ToList();
            var total = result.Rows.OfType<SalesByItemRow>().FirstOrDefault(i => i.IsTotal) ?? SalesByItemRow.Total(items);
            var rows = items.Select(i =>
            {
                var price = i.NetQty is { } q and not 0 ? $" x {N(i.NetSales / q)}" : string.Empty;
                var returned = i.QtyReturned > 0 ? $"  (ret {Qty(i.QtyReturned)})" : string.Empty;
                return new Printing.SlipRow(
                    i.ItemName,
                    $"   {Qty(i.NetQty)} {i.BaseUnit}{price}{returned}",
                    N(i.NetSales),
                    $"   Cost {N(i.Cost)}",
                    $"Profit {N(i.Profit)}" + (i.Margin is { } m ? $" ({m:0.#}%)" : string.Empty));
            }).ToList();

            var totals = new List<Printing.SlipTotal> { new("Items", $"{items.Count:#,0}") };
            if (total.Discounts != 0)
            {
                totals.Add(new("Gross", N(total.Gross)));
                totals.Add(new("Discounts", "-" + N(total.Discounts)));
            }
            totals.Add(new("NET SALES", primary.Format(total.NetSales), Strong: true));
            totals.Add(new("Cost", N(total.Cost)));
            totals.Add(new("PROFIT", primary.Format(total.Profit), Strong: true));
            if (total.Margin is { } margin) totals.Add(new("Margin", $"{margin:0.#}%"));

            return new Printing.ReportSlip
            {
                Company = company,
                Title = Title,
                Header = { filter.PeriodText, filter.UserId == null ? "All cashiers" : $"Cashier: {filter.UserName}", $"Amounts in {primary.Code}" },
                ColumnsLeft = "Qty x avg price",
                ColumnsRight = "Net sales",
                Rows = rows,
                Totals = totals,
            };
        }
    }
}
