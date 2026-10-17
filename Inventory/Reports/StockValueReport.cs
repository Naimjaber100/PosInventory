using Inventory.Models;
using Inventory.Repositories;

namespace Inventory.Reports
{
    // Report 10 - Stock value: what the stock costs and what it would sell for, as it is now. Per item: quantity in
    // stock, average cost and sale price of one unit, total cost, total sale, the profit it would make. Biggest cost first.
    // Items below zero (sold more than was entered) are red and not in the totals: their stock needs correcting.
    // Items sold under their cost are red; no price or no cost yet in orange.
    public sealed class StockValueReport : ReportDefinition
    {
        private static readonly Color Green = Color.FromArgb(34, 139, 117);
        private static readonly Color Blue = Color.FromArgb(29, 78, 160);
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);
        private static readonly Color Red = Color.FromArgb(192, 57, 43);
        private static readonly Color Orange = Color.FromArgb(217, 119, 6);

        public override string Title => "Stock value";

        public override string Description =>
            "What the stock costs and what it would sell for, now. Per piece / kg: average cost and sale price. Items below zero are not counted.";

        public override bool UsesCashierFilter => false;

        public override bool UsesPeriodFilter => false;

        public override async Task<ReportResult> LoadAsync(ReportFilter filter)
        {
            var items = await new ReportRepository().GetStockValueAsync();
            var total = StockValueRow.TotalOf(items);
            foreach (var item in items.Where(i => i.StockQty > 0))
                item.Share = total.TotalCost > 0 ? Math.Round(item.TotalCost / total.TotalCost * 100, 1) : null;
            var rows = new List<StockValueRow>(items) { total };

            var money = filter.Currencies;
            var inStock = items.Where(i => i.StockQty > 0).ToList();
            var belowZero = items.Where(i => i.BelowZero).ToList();
            var underCost = inStock.Where(i => i.Profit < 0).ToList();
            var noPrice = inStock.Where(i => i.NoPrice).ToList();
            var noCost = inStock.Where(i => i.NoCost).ToList();
            return new ReportResult
            {
                Rows = rows,
                Cards = new()
                {
                    new("STOCK AT COST", money.FormatPrimary(total.TotalCost), Blue,
                        money.FormatSecond(total.TotalCost) is { Length: > 0 } s ? s : null),
                    new("STOCK AT SALE PRICE", money.FormatPrimary(total.TotalSale), Green,
                        money.FormatSecond(total.TotalSale) is { Length: > 0 } s2 ? s2 : null),
                    new("POSSIBLE PROFIT", money.FormatPrimary(total.Profit), total.Profit < 0 ? Red : Green,
                        total.Margin is { } m ? $"margin {m:0.#}%" : null),
                    new("ITEMS IN STOCK", $"{inStock.Count:#,0}", Dark,
                        noPrice.Count + noCost.Count > 0 ? $"{noPrice.Count} without price, {noCost.Count} without cost" : null),
                    new("PRICED UNDER COST", $"{underCost.Count:#,0} item(s)", underCost.Count > 0 ? Red : Dark,
                        underCost.Count > 0 ? $"{money.FormatPrimary(underCost.Sum(i => i.Profit))}  ·  check price / cost" : "none"),
                    new("BELOW ZERO", $"{belowZero.Count:#,0} item(s)", belowZero.Count > 0 ? Red : Dark,
                        belowZero.Count > 0 ? "sold more than entered: correct the stock" : "none"),
                },
            };
        }

        public override void FormatGrid(DataGridView grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.ValueType is not { } valueType) continue;
                var type = Nullable.GetUnderlyingType(valueType) ?? valueType;
                if (type != typeof(decimal)) continue;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.DefaultCellStyle.Format = column.DataPropertyName switch
                {
                    nameof(StockValueRow.StockQty) => "#,0.###",
                    nameof(StockValueRow.Margin) or nameof(StockValueRow.Share) => "0.0",
                    _ => "#,0",
                };
            }
            if (grid.Columns[nameof(StockValueRow.ItemName)] is { } name) name.FillWeight = 170;
            if (grid.Columns[nameof(StockValueRow.CategoryName)] is { } category) category.FillWeight = 90;
            foreach (var narrow in new[] { nameof(StockValueRow.BaseUnit), nameof(StockValueRow.Margin), nameof(StockValueRow.Share) })
                if (grid.Columns[narrow] is { } c) c.FillWeight = 55;
            foreach (var bold in new[] { nameof(StockValueRow.TotalCost), nameof(StockValueRow.TotalSale), nameof(StockValueRow.Profit) })
                if (grid.Columns[bold] is { } c) c.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }

        public override void FormatCell(object row, DataGridViewColumn column, DataGridViewCellStyle style)
        {
            if (row is not StockValueRow item) return;
            if (item.IsTotal)
            {
                style.BackColor = style.SelectionBackColor = Color.FromArgb(232, 240, 254);
                style.ForeColor = style.SelectionForeColor = Dark;
                style.Font = new Font(style.Font ?? SystemFonts.DefaultFont, FontStyle.Bold);
                return;
            }
            if (item.BelowZero)
            {
                style.ForeColor = style.SelectionForeColor = Red;   // the whole row: not real stock
                return;
            }
            if (!item.IsActive) style.ForeColor = style.SelectionForeColor = Color.Gray;
            switch (column.DataPropertyName)
            {
                case nameof(StockValueRow.Profit) or nameof(StockValueRow.Margin) when item.Profit < 0:
                    style.ForeColor = style.SelectionForeColor = Red;
                    break;
                case nameof(StockValueRow.SalePrice) or nameof(StockValueRow.TotalSale) when item.NoPrice:
                case nameof(StockValueRow.AvgCost) or nameof(StockValueRow.TotalCost) when item.NoCost:
                    style.ForeColor = style.SelectionForeColor = Orange;
                    break;
            }
        }

        public override bool IsTotalRow(object row) => row is StockValueRow { IsTotal: true };
    }
}
