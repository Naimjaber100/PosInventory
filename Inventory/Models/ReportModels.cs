using System.ComponentModel;

namespace Inventory.Models
{
    // Report 1 - Daily sales summary: one row of fn_report_daily_sales (amounts in the primary currency).
    // The report adds a TOTAL row at the end (IsTotal).
    public class DailySalesRow
    {
        [Browsable(false)] public DateTime Day { get; set; }
        [Browsable(false)] public bool IsTotal { get; set; }

        [DisplayName("Day")] public string DayText => IsTotal ? "TOTAL" : Day.ToString("ddd dd/MM/yyyy");
        [DisplayName("Sales #")] public long SaleCount { get; set; }
        [DisplayName("Returns #")] public long ReturnCount { get; set; }
        [DisplayName("Gross")] public decimal Gross { get; set; }
        [DisplayName("Discounts")] public decimal Discounts { get; set; }
        [DisplayName("Sales")] public decimal Sales { get; set; }
        [DisplayName("Returns")] public decimal Returns { get; set; }
        [DisplayName("Net sales")] public decimal NetSales { get; set; }
        [DisplayName("Cost")] public decimal Cost { get; set; }
        [DisplayName("Profit")] public decimal Profit { get; set; }
        [DisplayName("Margin %")] public decimal? Margin => NetSales > 0 ? Math.Round(Profit / NetSales * 100, 1) : null;
        [DisplayName("Avg sale")] public decimal? AverageSale => SaleCount > 0 ? Math.Round(Sales / SaleCount, 0) : null;

        [Browsable(false)] public bool IsEmpty => !IsTotal && SaleCount == 0 && ReturnCount == 0;

        /// <summary>The TOTAL row of a list of days.</summary>
        public static DailySalesRow Total(IReadOnlyCollection<DailySalesRow> days) => new()
        {
            IsTotal = true,
            SaleCount = days.Sum(d => d.SaleCount),
            ReturnCount = days.Sum(d => d.ReturnCount),
            Gross = days.Sum(d => d.Gross),
            Discounts = days.Sum(d => d.Discounts),
            Sales = days.Sum(d => d.Sales),
            Returns = days.Sum(d => d.Returns),
            NetSales = days.Sum(d => d.NetSales),
            Cost = days.Sum(d => d.Cost),
            Profit = days.Sum(d => d.Profit),
        };
    }
}
