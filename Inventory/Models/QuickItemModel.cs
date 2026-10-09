namespace Inventory.Models
{
    // Row returned by fn_quick_item_list: an item (packaging) that can be put on a quick button
    public class QuickItemModel
    {
        public int PackagingId { get; set; }
        public int ProductId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? Barcode { get; set; }
        public decimal SalePrice { get; set; }
        public bool IsWeighted { get; set; }
        public string BaseUnit { get; set; } = string.Empty;
        public string? ButtonTab { get; set; }   // the tab it is already on; null = no button yet
    }
}
