namespace Inventory.Models
{
    // One row read from the import file; sent to fn_product_import as JSON (snake_case keys)
    public class ProductImportRow
    {
        public int RowNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public short ProductType { get; set; } = 1;
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string BaseUnit { get; set; } = "pcs";
        public bool IsWeighted { get; set; }
        public decimal AvgCost { get; set; }
        public decimal SalePrice { get; set; }
        public string? Barcode { get; set; }
        public decimal OpeningQty { get; set; }
        public decimal ReorderLevel { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
