using System.ComponentModel;

namespace Inventory.Models
{
    // Row returned by fn_purchase_get_lines. DisplayName = grid column header, Browsable(false) = hidden column.
    public class PurchaseLineModel
    {
        [Browsable(false)] public int PurchaseLineId { get; set; }
        [Browsable(false)] public int ProductId { get; set; }
        [Browsable(false)] public int PackagingId { get; set; }
        [DisplayName("Item")] public string ItemName { get; set; } = string.Empty;
        [DisplayName("Packaging")] public string PackagingName { get; set; } = string.Empty;
        [DisplayName("Barcode")] public string? Barcode { get; set; }
        [DisplayName("Qty")] public decimal Quantity { get; set; }
        [Browsable(false)] public decimal Factor { get; set; }
        [DisplayName("Base qty")] public decimal BaseQty { get; set; }
        [DisplayName("Unit")] public string BaseUnit { get; set; } = string.Empty;
        [Browsable(false)] public bool IsWeighted { get; set; }
        [DisplayName("Unit cost")] public decimal UnitCost { get; set; }
        [DisplayName("Line total")] public decimal LineTotal { get; set; }
    }
}
