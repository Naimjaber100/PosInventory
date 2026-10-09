namespace Inventory.Models
{
    // Row returned by fn_purchase_get_by_id
    public class PurchaseHeaderModel
    {
        public int PurchaseId { get; set; }
        public string? PurchaseNo { get; set; }
        public string? SupplierInvoiceNo { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.Today;
        public decimal ExchangeRate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public decimal PaidLbp { get; set; }
        public short Status { get; set; }          // 0 draft, 1 posted, 2 cancelled
        public string? Notes { get; set; }

        public bool IsDraft => Status == 0;
    }
}
