using System.ComponentModel;

namespace Inventory.Models
{
    // Row returned by fn_customer_get_all: a client (customers table).
    // Balance = what the client owes us (sales on account minus payments); it is never typed in.
    public class CustomerModel
    {
        [Browsable(false)] public int CustomerId { get; set; }
        [DisplayName("Name")] public string Name { get; set; } = string.Empty;
        [DisplayName("Phone")] public string? Phone { get; set; }
        [DisplayName("Address")] public string? Address { get; set; }
        [DisplayName("Email")] public string? Email { get; set; }
        [DisplayName("Owes us")] public decimal BalanceLbp { get; set; }
        [DisplayName("Credit limit")] public decimal? CreditLimitLbp { get; set; }   // null = no limit
        [DisplayName("Notes")] public string? Notes { get; set; }
        [DisplayName("Active")] public bool IsActive { get; set; } = true;
    }

    // Row returned by fn_customer_open_invoices: an invoice the client has not (fully) paid yet
    public class CustomerOpenInvoiceModel
    {
        [Browsable(false)] public int SaleId { get; set; }
        [DisplayName("Invoice")] public string InvoiceNo { get; set; } = string.Empty;

        [DisplayName("Date")]
        public DateTime InvoiceDate
        {
            get => _invoiceDate;
            set => _invoiceDate = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
        }
        private DateTime _invoiceDate;

        [DisplayName("Total")] public decimal Total { get; set; }
        [Browsable(false)] public decimal OnAccountLbp { get; set; }
        [DisplayName("Already paid")] public decimal PaidLbp => Total - DueLbp;
        [DisplayName("Unpaid")] public decimal DueLbp { get; set; }

        /// <summary>What the amount being typed pays of this invoice (oldest first); filled by the payment window.</summary>
        [DisplayName("This payment")] public decimal PayNow { get; set; }
        [DisplayName("Left")] public decimal Left => DueLbp - PayNow;
    }

    // Row returned by fn_receive_customer_payment
    public class CustomerPaymentResultModel
    {
        public int PaymentId { get; set; }
        public decimal PaidLbp { get; set; }
        public decimal BalanceLbp { get; set; }     // what the client still owes
        public int InvoicesPaid { get; set; }
        public int InvoicesPartly { get; set; }
    }

    // Row returned by fn_customer_get_payments: a payment the client made
    public class CustomerPaymentModel
    {
        [Browsable(false)] public int PaymentId { get; set; }

        [DisplayName("Date")]
        public DateTime PaymentDate
        {
            get => _paymentDate;
            set => _paymentDate = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
        }
        private DateTime _paymentDate;

        [Browsable(false)] public short Method { get; set; }
        [Browsable(false)] public string Currency { get; set; } = string.Empty;
        [Browsable(false)] public decimal Amount { get; set; }
        [DisplayName("Paid")] public string PaidText => $"{Currency} {Amount:#,0.##}" + (Method == PaymentMethod.Card ? " card" : string.Empty);
        [DisplayName("Amount")] public decimal AmountLbp { get; set; }
        [DisplayName("Received by")] public string? Cashier { get; set; }
        [DisplayName("Invoices")] public string? Invoices { get; set; }
    }
}
