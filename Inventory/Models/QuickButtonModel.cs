using System.ComponentModel;

namespace Inventory.Models
{
    // Row returned by fn_quick_button_get_all: a cashier quick button and the packaging it sells.
    // The item columns are the same as PosItemModel, so the cashier adds it like a scanned item.
    public class QuickButtonModel : PosItemModel
    {
        [Browsable(false)] public int ButtonId { get; set; }
        [Browsable(false)] public int? GroupId { get; set; }      // the tab; null = no tab
        [Browsable(false)] public string? Caption { get; set; }   // null = the item name
        [Browsable(false)] public string? Color { get; set; }     // '#RRGGBB', null = the tab's color
        [Browsable(false)] public int SortOrder { get; set; }
        [Browsable(false)] public bool AskPrice { get; set; }     // the cashier types the price
        [Browsable(false)] public bool IsActive { get; set; } = true;

        /// <summary>What the button shows.</summary>
        [Browsable(false)] public string DisplayText => string.IsNullOrWhiteSpace(Caption) ? ItemName : Caption;
    }
}
