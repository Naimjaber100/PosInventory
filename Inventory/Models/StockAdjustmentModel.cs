using System.ComponentModel;

namespace Inventory.Models
{
    // Row of fn_stock_adjustment_get_all: one stock adjustment (Database/stock_adjustment_functions.sql)
    public class StockAdjustmentModel
    {
        public const short DraftStatus = 0;

        [Browsable(false)] public int AdjustmentId { get; set; }
        [DisplayName("No.")] public string? AdjustmentNo { get; set; }

        [DisplayName("Date")]
        public DateTime AdjustmentDate { get => _date; set => _date = Local(value); }
        private DateTime _date;

        [DisplayName("Reason")] public string Reason { get; set; } = string.Empty;
        [Browsable(false)] public short Status { get; set; }
        [DisplayName("Status")] public string StatusText => IsDraft ? "Draft" : "Posted";
        [Browsable(false)] public bool IsDraft => Status == DraftStatus;
        [DisplayName("By")] public string? UserName { get; set; }
        [DisplayName("Items")] public long LineCount { get; set; }
        [DisplayName("Value change")] public decimal? ValueChange { get; set; }

        [Browsable(false)]
        public DateTime? PostedAt { get => _postedAt; set => _postedAt = value is { } v ? Local(v) : null; }
        private DateTime? _postedAt;

        private static DateTime Local(DateTime value) => value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
    }

    // Row of fn_stock_adjustment_get_lines / an item counted in the adjustment form.
    // QtyBefore = the stock when the item was counted; Difference = what posting adds to (or takes from) the stock.
    public class StockAdjustmentLineModel
    {
        [Browsable(false)] public int ProductId { get; set; }
        [DisplayName("Item")] public string ItemName { get; set; } = string.Empty;
        [DisplayName("Unit")] public string BaseUnit { get; set; } = string.Empty;
        [DisplayName("System qty")] public decimal QtyBefore { get; set; }
        [DisplayName("Counted")] public decimal CountedQty { get; set; }

        [DisplayName("Difference")]
        public decimal Difference { get => _difference ?? CountedQty - QtyBefore; set => _difference = value; }
        private decimal? _difference;

        [DisplayName("Cost")] public decimal UnitCost { get; set; }
        [DisplayName("Value change")] public decimal ValueChange => Math.Round(Difference * UnitCost, 2);
        [Browsable(false)] public decimal StockQty { get; set; }

        /// <summary>A posted line keeps the difference that was posted; a draft's follows the counted quantity.</summary>
        public void FollowCounted() => _difference = null;
    }

    // Row of fn_stock_adjustment_find_items: a standard item that can be counted
    public class StockItemModel
    {
        public int ProductId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string BaseUnit { get; set; } = string.Empty;
        public decimal StockQty { get; set; }
        public decimal AvgCost { get; set; }
    }
}
