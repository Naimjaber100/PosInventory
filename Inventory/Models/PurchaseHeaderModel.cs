namespace Inventory.Models
{
    // purchase_invoices.doc_type (Database/purchase_functions.sql)
    public static class PurchaseDocType
    {
        public const short Purchase = 1;
        public const short Return = 2;   // goods sent back to the supplier (PR-...): stock and the supplier balance go down
    }

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
        public string? Currency { get; set; }          // the invoice currency (null = primary); costs and the discount are in it
        public decimal CurrencyRate { get; set; } = 1; // 1 unit of it in the primary currency
        public decimal TotalLbp { get; set; }          // total in the primary currency
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public decimal PaidLbp { get; set; }
        public short Status { get; set; }          // 0 draft, 1 posted, 2 cancelled
        public string? Notes { get; set; }
        public short DocType { get; set; } = PurchaseDocType.Purchase;
        public int? ReturnOfId { get; set; }       // a return: the posted purchase the goods came on (null = without it)
        public string? ReturnOfNo { get; set; }

        public bool IsDraft => Status == 0;
        public bool IsReturn => DocType == PurchaseDocType.Return;
    }
}
