using System.ComponentModel;

namespace Inventory.Models
{
    // Row of fn_shift_list / fn_shift_summary / fn_close_shift: one user's shift and its figures (Database/shift_functions.sql).
    // Open shift: worked out now; ended shift: the cash expected and counted when it ended.
    public class ShiftModel
    {
        public const short OpenStatus = 1;

        [DisplayName("Shift")] public int ShiftId { get; set; }
        [Browsable(false)] public int UserId { get; set; }
        [DisplayName("Cashier")] public string UserName { get; set; } = string.Empty;

        [DisplayName("Opened")]
        public DateTime OpenedAt { get => _openedAt; set => _openedAt = Local(value); }
        private DateTime _openedAt;

        [DisplayName("Ended")]
        public DateTime? ClosedAt { get => _closedAt; set => _closedAt = value is { } v ? Local(v) : null; }
        private DateTime? _closedAt;

        [Browsable(false)] public short Status { get; set; }
        [DisplayName("Status")] public string StatusText => IsOpen ? "Open" : "Ended";
        [Browsable(false)] public bool IsOpen => Status == OpenStatus;

        [DisplayName("Invoices")] public int Invoices { get; set; }
        [DisplayName("Returns")] public int Returns { get; set; }
        [DisplayName("Sales")] public decimal SalesLbp { get; set; }
        [DisplayName("Refunds")] public decimal RefundsLbp { get; set; }
        [Browsable(false)] public decimal ProfitLbp { get; set; }
        [DisplayName("Card")] public decimal CardLbp { get; set; }
        [DisplayName("On account")] public decimal OnAccountLbp { get; set; }
        [Browsable(false)] public decimal ClientCashLbp { get; set; }
        [Browsable(false)] public decimal ClientCashUsd { get; set; }
        [Browsable(false)] public decimal OpeningCashLbp { get; set; }
        [Browsable(false)] public decimal OpeningCashUsd { get; set; }
        [DisplayName("Cash expected LBP")] public decimal ExpectedCashLbp { get; set; }
        [DisplayName("Cash expected USD")] public decimal ExpectedCashUsd { get; set; }
        [DisplayName("Counted LBP")] public decimal? CountedCashLbp { get; set; }
        [DisplayName("Counted USD")] public decimal? CountedCashUsd { get; set; }
        [DisplayName("Difference LBP")] public decimal? DifferenceLbp { get; set; }
        [DisplayName("Difference USD")] public decimal? DifferenceUsd { get; set; }

        private static DateTime Local(DateTime value) => value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
    }
}
