namespace Inventory.Models
{
    // Row returned by fn_product_get_all / fn_product_get_by_id
    // (snake_case columns are matched via Dapper's MatchNamesWithUnderscores)
    public class ProductModel
    {
        public int ProductId { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public short ProductType { get; set; } = 1;
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string BaseUnit { get; set; } = "pcs";
        public bool IsWeighted { get; set; }
        public decimal AvgCost { get; set; }
        public decimal StockQty { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal? SalePrice { get; set; }
        public string? Barcode { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal? VatRate { get; set; }   // TVA %; null when adding = the company's rate
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // The biggest packaging (fn_product_get_all only): "box" of 6 -> the stock is also shown as boxes
        public string? BoxUnit { get; set; }
        public decimal? BoxFactor { get; set; }

        /// <summary>The stock in boxes and the pieces left over: 44 pcs, box of 6 -> "7 box + 2 pcs"; null without a box.</summary>
        public string? StockInBoxes()
        {
            if (ProductType != 1 || BoxFactor is not > 1 || string.IsNullOrWhiteSpace(BoxUnit)) return null;
            var factor = BoxFactor.Value;
            var boxes = decimal.Truncate(StockQty / factor);
            var rest = StockQty - boxes * factor;
            var text = $"{boxes:#,0} {BoxUnit}";
            return rest == 0 ? text : $"{text} + {rest:#,0.###} {BaseUnit}";
        }
    }
}
