using System.ComponentModel;

namespace Inventory.Models
{
    // One packaging of an item (row of fn_product_get_packagings, and an editable row in the item form).
    // Factor = how many base units it contains (can = 1, box of 24 cans = 24).
    public class PackagingModel
    {
        [Browsable(false)] public int? PackagingId { get; set; }
        [DisplayName("Packaging")] public string Name { get; set; } = string.Empty;
        [DisplayName("Contains")] public decimal Factor { get; set; } = 1;
        [DisplayName("Barcode")] public string? Barcode { get; set; }
        [DisplayName("Sale price")] public decimal SalePrice { get; set; }
        [DisplayName("Default")] public bool IsDefault { get; set; }
        [DisplayName("Can sell")] public bool CanSell { get; set; } = true;
        [DisplayName("Can buy")] public bool CanPurchase { get; set; } = true;
    }
}
