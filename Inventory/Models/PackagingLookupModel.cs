using System.ComponentModel;

namespace Inventory.Models
{
    // Row returned by fn_purchase_find_packaging (a packaging that can be purchased)
    public class PackagingLookupModel
    {
        [Browsable(false)] public int PackagingId { get; set; }
        [Browsable(false)] public int ProductId { get; set; }
        [DisplayName("Item")] public string ItemName { get; set; } = string.Empty;
        [DisplayName("Packaging")] public string PackagingName { get; set; } = string.Empty;
        [DisplayName("Barcode")] public string? Barcode { get; set; }
        [DisplayName("Contains")] public decimal Factor { get; set; }
        [DisplayName("Unit")] public string BaseUnit { get; set; } = string.Empty;
        [Browsable(false)] public bool IsWeighted { get; set; }
        [DisplayName("Last cost")] public decimal LastCost { get; set; }
    }
}
