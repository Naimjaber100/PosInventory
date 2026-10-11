using System.ComponentModel;

namespace Inventory.Models
{
    // Row returned by fn_purchase_get_all. DisplayName = grid column header, Browsable(false) = hidden column.
    public class PurchaseInvoiceModel
    {
        [Browsable(false)] public int PurchaseId { get; set; }
        [DisplayName("Number")] public string? PurchaseNo { get; set; }
        [DisplayName("Supplier invoice")] public string? SupplierInvoiceNo { get; set; }
        [DisplayName("Date")] public DateTime InvoiceDate { get; set; }
        [Browsable(false)] public int SupplierId { get; set; }
        [DisplayName("Supplier")] public string? SupplierName { get; set; }
        [DisplayName("Items")] public long ItemCount { get; set; }
        [DisplayName("Total")] public decimal Total { get; set; }                 // in the purchase currency
        [DisplayName("Cur.")] public string Currency { get; set; } = "LBP";
        [DisplayName("Total (LBP)")] public decimal TotalLbp { get; set; }        // in the primary currency
        [DisplayName("Paid")] public decimal PaidLbp { get; set; }
        [DisplayName("Balance")] public decimal? BalanceLbp { get; set; }
        [DisplayName("Total $")] public decimal TotalUsd { get; set; }
        [Browsable(false)] public short Status { get; set; }      // 0 draft, 1 posted, 2 cancelled
        [DisplayName("Status")] public string StatusText { get; set; } = string.Empty;
        [DisplayName("Created by")] public string? CreatedByName { get; set; }
        [DisplayName("Posted by")] public string? PostedByName { get; set; }
    }
}
