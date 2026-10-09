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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
