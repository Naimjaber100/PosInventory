using System.ComponentModel;

namespace Inventory.Models
{
    public static class PromotionType
    {
        public const short PriceCut = 1;     // a new price, or % off, per unit
        public const short MultiBuy = 2;     // every full group of the same item costs a fixed price
        public const short BuyXGetY = 3;     // the cheapest unit(s) of each group are free
        public const short MixMatch = 4;     // as multi-buy, across a list of items
        public const short Category = 5;     // % off every item of a category
        public const short Invoice = 6;      // % off the invoice total from a minimum, after the item promotions

        public static readonly (short Type, string Name)[] All =
        {
            (PriceCut, "Price cut"), (MultiBuy, "Multi-buy"), (BuyXGetY, "Buy X get Y"),
            (MixMatch, "Mix & match"), (Category, "Category %"), (Invoice, "Invoice discount"),
        };

        public static string NameOf(short type) => All.FirstOrDefault(t => t.Type == type).Name ?? "?";

        /// <summary>The types that are for a list of items (the others are for a category or the whole invoice).</summary>
        public static bool HasItems(short type) => type is PriceCut or MultiBuy or BuyXGetY or MixMatch;
    }

    // Row returned by fn_promotion_get_all (back-office list) and fn_promotion_get_active (cashier: with PackagingIds).
    // Only the values its type uses are set (Database/promotion_functions.sql).
    public class PromotionModel
    {
        [Browsable(false)] public int PromotionId { get; set; }
        [DisplayName("Promotion")] public string Name { get; set; } = string.Empty;
        [Browsable(false)] public short PromoType { get; set; } = PromotionType.PriceCut;
        [DisplayName("Type")] public string TypeText => PromotionType.NameOf(PromoType);
        [DisplayName("What it does")] public string Summary => Describe();
        [DisplayName("From")] public DateTime? StartsOn { get; set; }
        [DisplayName("Until")] public DateTime? EndsOn { get; set; }
        [DisplayName("Status")] public string Status => StatusOn(DateTime.Today);
        [Browsable(false)] public bool IsActive { get; set; } = true;

        [Browsable(false)] public decimal? NewPrice { get; set; }
        [Browsable(false)] public decimal? Percent { get; set; }
        [Browsable(false)] public int? BuyQty { get; set; }        // multi-buy / mix & match: the group; buy X get Y: X
        [Browsable(false)] public int? FreeQty { get; set; }       // buy X get Y: Y
        [Browsable(false)] public decimal? GroupPrice { get; set; }
        [Browsable(false)] public decimal? MinTotal { get; set; }
        [Browsable(false)] public int? CategoryId { get; set; }
        [Browsable(false)] public string? CategoryName { get; set; }
        [Browsable(false)] public long ItemCount { get; set; }
        [Browsable(false)] public string? FirstItem { get; set; }

        /// <summary>The items it is for (cashier; a category % has every item of the category).</summary>
        [Browsable(false)] public int[] PackagingIds { get; set; } = Array.Empty<int>();

        [Browsable(false)] public bool RunsToday => Status == "Running";

        /// <summary>Running / Starts dd/MM / Ended / Off.</summary>
        public string StatusOn(DateTime day) =>
            !IsActive ? "Off"
            : StartsOn is { } from && from.Date > day ? $"Starts {from:dd/MM}"
            : EndsOn is { } until && until.Date < day ? "Ended"
            : "Running";

        // "Pepsi Can: 3 for 120,000", "Any 2 of 5 items for 600,000", "Clothes: 20% off"...
        private string Describe()
        {
            var items = ItemCount <= 1 ? FirstItem ?? "1 item" : $"{ItemCount} items";
            return PromoType switch
            {
                PromotionType.PriceCut => NewPrice is { } price ? $"{items}: {price:#,0.##} each" : $"{items}: {Percent:0.##}% off",
                PromotionType.MultiBuy => $"{items}: {BuyQty} for {GroupPrice:#,0.##}",
                PromotionType.BuyXGetY => $"{items}: buy {BuyQty}, get {FreeQty} free",
                PromotionType.MixMatch => $"Any {BuyQty} of {items} for {GroupPrice:#,0.##}",
                PromotionType.Category => $"{CategoryName}: {Percent:0.##}% off",
                PromotionType.Invoice => $"Spend {MinTotal:#,0.##} or more: {Percent:0.##}% off",
                _ => string.Empty,
            };
        }
    }

    // Row returned by fn_promotion_get_items: an item of a promotion
    public class PromotionItemModel
    {
        [Browsable(false)] public int PackagingId { get; set; }
        [DisplayName("Item")] public string ItemName { get; set; } = string.Empty;
        [DisplayName("Barcode")] public string? Barcode { get; set; }
        [DisplayName("Price")] public decimal SalePrice { get; set; }
        [Browsable(false)] public bool IsWeighted { get; set; }
    }
}
