using System.ComponentModel;

namespace Inventory.Models
{
    // Row returned by fn_quick_group_get_all: a tab of quick buttons (Salads, Drinks, Bakery...).
    public class QuickGroupModel
    {
        /// <summary>GroupId of the "(no group)" row shown in the back office list.</summary>
        public const int NoGroupId = 0;

        [Browsable(false)] public int GroupId { get; set; }
        [DisplayName("Group")] public string Name { get; set; } = string.Empty;
        [Browsable(false)] public string? Color { get; set; }   // '#RRGGBB', null = default
        [Browsable(false)] public int SortOrder { get; set; }
        [DisplayName("Active")] public bool IsActive { get; set; } = true;
        [DisplayName("Buttons")] public long ButtonCount { get; set; }

        [Browsable(false)] public bool IsNoGroup => GroupId == NoGroupId;

        /// <summary>The id to save on a button (null for "(no group)").</summary>
        [Browsable(false)] public int? SaveId => IsNoGroup ? null : GroupId;
    }
}
