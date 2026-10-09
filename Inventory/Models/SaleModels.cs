using System.ComponentModel;

namespace Inventory.Models
{
    public static class PaymentMethod
    {
        public const short Cash = 1;
        public const short Card = 2;
        public const short OnAccount = 3;   // needs a customer (not on the cashier screen yet)
    }

    // One payment line sent to fn_save_sale (amount in its own currency: "LBP" or "USD")
    public class SalePaymentModel
    {
        public short Method { get; set; } = PaymentMethod.Cash;
        public string Currency { get; set; } = "LBP";
        public decimal Amount { get; set; }
    }

    // Row returned by fn_save_sale
    public class SaleResultModel
    {
        public int SaleId { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public decimal TotalLbp { get; set; }
        public decimal ChangeLbp { get; set; }
    }

    // Row returned by fn_pos_get_held_sales
    public class HeldSaleModel
    {
        [DisplayName("#")] public int SaleId { get; set; }
        [DisplayName("Held at")] public DateTime HeldAt { get; set; }
        [DisplayName("Cashier")] public string? UserName { get; set; }
        [DisplayName("Lines")] public long ItemCount { get; set; }
        [DisplayName("Total")] public decimal Total { get; set; }
    }

    // Row returned by fn_sales_by_user: a finished sale or return (amounts positive; a return is subtracted in totals)
    public class SaleHistoryModel
    {
        public const short SaleType = 1;
        public const short ReturnType = 2;

        [Browsable(false)] public int SaleId { get; set; }
        [DisplayName("Invoice")] public string InvoiceNo { get; set; } = string.Empty;
        [Browsable(false)] public short InvoiceType { get; set; }
        [DisplayName("Type")] public string TypeText => InvoiceType == ReturnType ? "Return" : "Sale";

        [DisplayName("Date")]
        public DateTime InvoiceDate
        {
            get => _invoiceDate;
            set => _invoiceDate = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
        }
        private DateTime _invoiceDate;

        [DisplayName("Cashier")] public string? Cashier { get; set; }
        [DisplayName("Items")] public long ItemCount { get; set; }
        [DisplayName("Total")] public decimal Total { get; set; }
        [DisplayName("Profit")] public decimal Profit { get; set; }
        [DisplayName("Paid / refunded")] public string? Payments { get; set; }

        [Browsable(false)] public bool IsReturn => InvoiceType == ReturnType;

        /// <summary>Total with its sign for the totals: a return counts negative.</summary>
        [Browsable(false)] public decimal SignedTotal => IsReturn ? -Total : Total;
        [Browsable(false)] public decimal SignedProfit => IsReturn ? -Profit : Profit;
    }

    // Row returned by fn_sale_get_lines: an item of a sale or return
    public class SaleHistoryLineModel
    {
        [DisplayName("#")] public int LineNo { get; set; }
        [DisplayName("Item")] public string ItemName { get; set; } = string.Empty;
        [DisplayName("Qty")] public decimal Quantity { get; set; }
        [DisplayName("Price")] public decimal UnitPrice { get; set; }
        [DisplayName("Discount")] public decimal Discount { get; set; }   // line discount + share of the invoice discount
        [DisplayName("Total")] public decimal LineTotal { get; set; }
        [DisplayName("Profit")] public decimal LineProfit { get; set; }
    }

    // Row returned by fn_save_return: the saved return invoice
    public class ReturnResultModel
    {
        public int ReturnId { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public decimal RefundLbp { get; set; }
        public decimal RefundAmount { get; set; }   // in the refund currency
    }

    // Row returned by fn_pos_get_held_lines
    public class HeldLineModel
    {
        public int PackagingId { get; set; }
        public int ProductId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string PackagingName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Factor { get; set; } = 1;
        public bool IsWeighted { get; set; }
        public string BaseUnit { get; set; } = string.Empty;
        public decimal StockQty { get; set; }
    }
}
