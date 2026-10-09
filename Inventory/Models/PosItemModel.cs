using System.ComponentModel;

namespace Inventory.Models
{
    // Row returned by fn_pos_find_item: one packaging that can be sold.
    // DisplayName / Browsable(false) set the columns when it is shown in the item picker.
    public class PosItemModel
    {
        [Browsable(false)] public int PackagingId { get; set; }
        [Browsable(false)] public int ProductId { get; set; }
        [DisplayName("Item")] public string ItemName { get; set; } = string.Empty;
        [Browsable(false)] public string PackagingName { get; set; } = string.Empty;
        [DisplayName("Barcode")] public string? Barcode { get; set; }
        [DisplayName("Price")] public decimal SalePrice { get; set; }
        [Browsable(false)] public decimal Factor { get; set; } = 1;
        [Browsable(false)] public bool IsWeighted { get; set; }
        [DisplayName("Unit")] public string BaseUnit { get; set; } = string.Empty;
        [DisplayName("In stock")] public decimal StockQty { get; set; }   // in base units

        /// <summary>A copy sold at another price (open-price quick buttons).</summary>
        public PosItemModel WithPrice(decimal price)
        {
            var copy = (PosItemModel)MemberwiseClone();
            copy.SalePrice = price;
            return copy;
        }
    }
}
