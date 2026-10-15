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

namespace Inventory.Models
{
    // Report 2 - Sales by item: one row of fn_report_sales_by_item (amounts in the primary currency,
    // quantities in the item's base unit). The report adds a TOTAL row at the end (IsTotal).
    public class SalesByItemRow
    {
        [Browsable(false)] public int ProductId { get; set; }
        [Browsable(false)] public bool IsTotal { get; set; }

        [DisplayName("Item")] public string ItemName { get; set; } = string.Empty;
        [DisplayName("Category")] public string? CategoryName { get; set; }
        [DisplayName("Sold")] public decimal? QtySold { get; set; }
        [DisplayName("Returned")] public decimal? QtyReturned { get; set; }
        [DisplayName("Net qty")] public decimal? NetQty { get; set; }
        [DisplayName("Unit")] public string? BaseUnit { get; set; }
        [DisplayName("Gross")] public decimal Gross { get; set; }
        [DisplayName("Discounts")] public decimal Discounts { get; set; }
        [DisplayName("Net sales")] public decimal NetSales { get; set; }
        [DisplayName("Cost")] public decimal Cost { get; set; }
        [DisplayName("Profit")] public decimal Profit { get; set; }
        [DisplayName("Margin %")] public decimal? Margin => NetSales > 0 ? Math.Round(Profit / NetSales * 100, 1) : null;
        [DisplayName("% of sales")] public decimal? ShareOfSales { get; set; }   // filled by the report
        [DisplayName("Invoices")] public long? InvoiceCount { get; set; }

        /// <summary>The TOTAL row of a list of items (quantities are not added: units differ).</summary>
        public static SalesByItemRow Total(IReadOnlyCollection<SalesByItemRow> items) => new()
        {
            IsTotal = true,
            ItemName = $"TOTAL  ({items.Count} items)",
            Gross = items.Sum(i => i.Gross),
            Discounts = items.Sum(i => i.Discounts),
            NetSales = items.Sum(i => i.NetSales),
            Cost = items.Sum(i => i.Cost),
            Profit = items.Sum(i => i.Profit),
            ShareOfSales = items.Count > 0 ? 100 : null,
        };
    }
}

namespace Inventory.Models
{
    // Report 3 - Sales by invoice: one row of fn_report_sales_by_invoice (amounts in the primary currency;
    // a return has negative amounts). The report adds a TOTAL row at the end (IsTotal).
    public class SalesByInvoiceRow
    {
        public const short ReturnType = 2;

        [Browsable(false)] public int SaleId { get; set; }
        [Browsable(false)] public bool IsTotal { get; set; }
        [Browsable(false)] public short InvoiceType { get; set; }

        [DisplayName("Invoice")] public string InvoiceNo { get; set; } = string.Empty;
        [DisplayName("Type")] public string TypeText => IsTotal ? string.Empty : InvoiceType == ReturnType ? "Return" : "Sale";

        [DisplayName("Date")]
        public DateTime? InvoiceDate
        {
            get => _invoiceDate;
            set => _invoiceDate = value is { Kind: DateTimeKind.Utc } utc ? utc.ToLocalTime() : value;
        }
        private DateTime? _invoiceDate;

        [DisplayName("Cashier")] public string? Cashier { get; set; }
        [DisplayName("Client")] public string? Customer { get; set; }
        [DisplayName("Lines")] public long ItemCount { get; set; }
        [DisplayName("Qty")] public decimal Quantity { get; set; }
        [DisplayName("Gross")] public decimal Gross { get; set; }
        [DisplayName("Discounts")] public decimal Discounts { get; set; }
        [DisplayName("Total")] public decimal Total { get; set; }
        [DisplayName("Cost")] public decimal Cost { get; set; }
        [DisplayName("Profit")] public decimal Profit { get; set; }
        [DisplayName("Margin %")] public decimal? Margin => Total > 0 ? Math.Round(Profit / Total * 100, 1) : null;
        [DisplayName("Paid with")] public string? Payments { get; set; }

        [Browsable(false)] public bool IsReturn => InvoiceType == ReturnType;

        /// <summary>The TOTAL row of a list of invoices (returns are already negative, so this is the net).</summary>
        public static SalesByInvoiceRow TotalOf(IReadOnlyCollection<SalesByInvoiceRow> invoices) => new()
        {
            IsTotal = true,
            InvoiceNo = $"TOTAL  ({invoices.Count} invoices)",
            ItemCount = invoices.Sum(i => i.ItemCount),
            Quantity = invoices.Sum(i => i.Quantity),
            Gross = invoices.Sum(i => i.Gross),
            Discounts = invoices.Sum(i => i.Discounts),
            Total = invoices.Sum(i => i.Total),
            Cost = invoices.Sum(i => i.Cost),
            Profit = invoices.Sum(i => i.Profit),
        };
    }
}

namespace Inventory.Models
{
    // Report 4 - Sales and unpaid: one row of fn_report_sales_unpaid (amounts in the primary currency).
    // Unpaid is what is still unpaid now of the invoices of that day. The report adds a TOTAL row at the end (IsTotal).
    public class SalesUnpaidRow
    {
        [Browsable(false)] public DateTime Day { get; set; }
        [Browsable(false)] public bool IsTotal { get; set; }

        [DisplayName("Day")] public string DayText => IsTotal ? "TOTAL" : Day.ToString("ddd dd/MM/yyyy");
        [DisplayName("Sales #")] public long SaleCount { get; set; }
        [DisplayName("Sales")] public decimal Sales { get; set; }
        [DisplayName("Returns")] public decimal Returns { get; set; }
        [DisplayName("Net sales")] public decimal NetSales { get; set; }
        [DisplayName("Sold unpaid")] public decimal OnAccount { get; set; }
        [DisplayName("Paid later")] public decimal PaidLater => OnAccount - Unpaid;
        [DisplayName("Unpaid #")] public long UnpaidCount { get; set; }
        [DisplayName("Still unpaid")] public decimal Unpaid { get; set; }
        [DisplayName("Difference (collected)")] public decimal Collected => NetSales - Unpaid;
        [DisplayName("Unpaid %")] public decimal? UnpaidShare => NetSales > 0 ? Math.Round(Unpaid / NetSales * 100, 1) : null;

        [Browsable(false)] public bool IsEmpty => !IsTotal && SaleCount == 0 && Returns == 0;

        /// <summary>The TOTAL row of a list of days.</summary>
        public static SalesUnpaidRow Total(IReadOnlyCollection<SalesUnpaidRow> days) => new()
        {
            IsTotal = true,
            SaleCount = days.Sum(d => d.SaleCount),
            Sales = days.Sum(d => d.Sales),
            Returns = days.Sum(d => d.Returns),
            NetSales = days.Sum(d => d.NetSales),
            OnAccount = days.Sum(d => d.OnAccount),
            UnpaidCount = days.Sum(d => d.UnpaidCount),
            Unpaid = days.Sum(d => d.Unpaid),
        };
    }
}

namespace Inventory.Models
{
    // Report 5 - Unpaid by client: one row of fn_report_unpaid_by_customer (amounts in the primary currency),
    // as it is now. The report adds a TOTAL row at the end (IsTotal).
    public class UnpaidByCustomerRow
    {
        [Browsable(false)] public int CustomerId { get; set; }
        [Browsable(false)] public bool IsTotal { get; set; }

        [DisplayName("Client")] public string Name { get; set; } = string.Empty;
        [DisplayName("Phone")] public string? Phone { get; set; }
        [DisplayName("Unpaid invoices")] public long InvoiceCount { get; set; }

        [DisplayName("Oldest unpaid")]
        public DateTime? OldestUnpaid
        {
            get => _oldestUnpaid;
            set => _oldestUnpaid = value is { Kind: DateTimeKind.Utc } utc ? utc.ToLocalTime() : value;
        }
        private DateTime? _oldestUnpaid;

        [DisplayName("Days")] public long? Days => OldestUnpaid is { } d ? (long)(DateTime.Today - d.Date).TotalDays : null;
        [DisplayName("Unpaid")] public decimal Unpaid { get; set; }
        [DisplayName("% of total")] public decimal? Share { get; set; }   // filled by the report
        [DisplayName("Credit limit")] public decimal? CreditLimitLbp { get; set; }

        [DisplayName("Last payment")]
        public DateTime? LastPayment
        {
            get => _lastPayment;
            set => _lastPayment = value is { Kind: DateTimeKind.Utc } utc ? utc.ToLocalTime() : value;
        }
        private DateTime? _lastPayment;

        [Browsable(false)] public bool OverLimit => CreditLimitLbp is { } limit && Unpaid > limit;

        /// <summary>The TOTAL row of a list of clients.</summary>
        public static UnpaidByCustomerRow Total(IReadOnlyCollection<UnpaidByCustomerRow> clients) => new()
        {
            IsTotal = true,
            Name = $"TOTAL  ({clients.Count} clients)",
            InvoiceCount = clients.Sum(c => c.InvoiceCount),
            OldestUnpaid = clients.Min(c => c.OldestUnpaid),
            Unpaid = clients.Sum(c => c.Unpaid),
            Share = clients.Count > 0 ? 100 : null,
        };
    }
}

namespace Inventory.Models
{
    // Report 6 - Client statement: one row of fn_report_customer_statement (amounts in the primary currency):
    // what was owed before the period, each sale left unpaid and each payment. The report fills the running
    // balance and adds a TOTAL row at the end (IsTotal).
    public class CustomerStatementRow
    {
        public const short OpeningType = 0;
        public const short SaleType = 1;
        public const short PaymentType = 2;

        [Browsable(false)] public short EntryType { get; set; }
        [Browsable(false)] public bool IsTotal { get; set; }
        [Browsable(false)] public int? CustomerId { get; set; }

        [DisplayName("Date")]
        public DateTime? EntryDate
        {
            get => _entryDate;
            set => _entryDate = value is { Kind: DateTimeKind.Utc } utc ? utc.ToLocalTime() : value;
        }
        private DateTime? _entryDate;

        [DisplayName("Client")] public string? Customer { get; set; }
        [DisplayName("Type")] public string TypeText => IsTotal ? string.Empty : EntryType switch { SaleType => "Unpaid sale", PaymentType => "Payment", _ => string.Empty };
        [DisplayName("Invoice / payment")] public string Reference { get; set; } = string.Empty;
        [DisplayName("Sold unpaid")] public decimal Debit { get; set; }
        [DisplayName("Paid")] public decimal Credit { get; set; }
        [DisplayName("Owes after")] public decimal Balance { get; set; }   // filled by the report

        [Browsable(false)] public decimal? DueLbp { get; set; }   // a sale: what is still unpaid of it now

        /// <summary>A sale: Unpaid / Partly paid / Paid as it is now.</summary>
        [DisplayName("Invoice now")]
        public string Status =>
            EntryType != SaleType || IsTotal || DueLbp is not { } due ? string.Empty
            : due <= 0 ? "Paid"
            : due >= Debit ? "Unpaid"
            : $"Partly paid ({due:#,0} left)";

        [DisplayName("Details")] public string? Details { get; set; }
        [DisplayName("Cashier")] public string? Cashier { get; set; }

        [Browsable(false)] public bool IsSale => EntryType == SaleType && !IsTotal;
        [Browsable(false)] public bool IsPayment => EntryType == PaymentType && !IsTotal;
        [Browsable(false)] public bool IsOpening => EntryType == OpeningType && !IsTotal;
    }
}

namespace Inventory.Models
{
    // Report 7 - Purchases by invoice: one row of fn_report_purchases_by_invoice (amounts in the primary currency,
    // except Total, which is in the invoice's own currency). The report adds a TOTAL row at the end (IsTotal).
    public class PurchasesByInvoiceRow
    {
        [Browsable(false)] public int PurchaseId { get; set; }
        [Browsable(false)] public bool IsTotal { get; set; }

        [DisplayName("Purchase")] public string PurchaseNo { get; set; } = string.Empty;
        [DisplayName("Date")] public DateTime? InvoiceDate { get; set; }
        [DisplayName("Supplier")] public string? Supplier { get; set; }
        [DisplayName("Supplier invoice")] public string? SupplierInvoiceNo { get; set; }
        [DisplayName("Lines")] public long ItemCount { get; set; }
        [DisplayName("Total (own currency)")] public string TotalText => IsTotal ? string.Empty : $"{Currency} {Total:#,0.##}";
        [Browsable(false)] public string Currency { get; set; } = string.Empty;
        [Browsable(false)] public decimal Total { get; set; }
        [DisplayName("Discount")] public decimal DiscountLbp { get; set; }
        [DisplayName("Total")] public decimal TotalLbp { get; set; }
        [DisplayName("Paid")] public decimal PaidLbp { get; set; }
        [DisplayName("Still owed")] public decimal OwedLbp { get; set; }
        [DisplayName("Status")] public string Status => IsTotal ? string.Empty : OwedLbp <= 0 ? "Paid" : PaidLbp > 0 ? "Part paid" : "Unpaid";
        [DisplayName("Entered by")] public string? EnteredBy { get; set; }

        /// <summary>The TOTAL row of a list of purchases.</summary>
        public static PurchasesByInvoiceRow TotalOf(IReadOnlyCollection<PurchasesByInvoiceRow> invoices) => new()
        {
            IsTotal = true,
            PurchaseNo = $"TOTAL  ({invoices.Count} purchases)",
            ItemCount = invoices.Sum(i => i.ItemCount),
            DiscountLbp = invoices.Sum(i => i.DiscountLbp),
            TotalLbp = invoices.Sum(i => i.TotalLbp),
            PaidLbp = invoices.Sum(i => i.PaidLbp),
            OwedLbp = invoices.Sum(i => i.OwedLbp),
        };
    }
}

namespace Inventory.Models
{
    // Report 8 - Purchases by item: one row of fn_report_purchases_by_item (primary currency; quantities and unit
    // costs per base unit, after the invoice discount). The report adds a TOTAL row at the end (IsTotal).
    public class PurchasesByItemRow
    {
        [Browsable(false)] public int ProductId { get; set; }
        [Browsable(false)] public bool IsTotal { get; set; }

        [DisplayName("Item")] public string ItemName { get; set; } = string.Empty;
        [DisplayName("Category")] public string? CategoryName { get; set; }
        [DisplayName("Qty bought")] public decimal? Qty { get; set; }
        [DisplayName("Unit")] public string? BaseUnit { get; set; }
        [DisplayName("Cost")] public decimal Cost { get; set; }
        [DisplayName("% of purchases")] public decimal? Share { get; set; }   // filled by the report
        [DisplayName("Avg unit cost")] public decimal? AvgUnitCost { get; set; }
        [DisplayName("Last unit cost")] public decimal? LastUnitCost { get; set; }
        [DisplayName("Lowest")] public decimal? MinUnitCost { get; set; }
        [DisplayName("Highest")] public decimal? MaxUnitCost { get; set; }
        [DisplayName("Avg cost now")] public decimal? AvgCostNow { get; set; }
        [DisplayName("Invoices")] public long? InvoiceCount { get; set; }
        [DisplayName("Last bought")] public DateTime? LastBought { get; set; }
        [DisplayName("Suppliers")] public string? Suppliers { get; set; }

        /// <summary>The price went up: the last unit cost is more than the lowest of the period.</summary>
        [Browsable(false)] public bool CostWentUp => !IsTotal && LastUnitCost > MinUnitCost;

        /// <summary>The TOTAL row of a list of items (quantities are not added: units differ).</summary>
        public static PurchasesByItemRow TotalOf(IReadOnlyCollection<PurchasesByItemRow> items) => new()
        {
            IsTotal = true,
            ItemName = $"TOTAL  ({items.Count} items)",
            Cost = items.Sum(i => i.Cost),
            Share = items.Count > 0 ? 100 : null,
        };
    }
}

namespace Inventory.Models
{
    // Report 9 - Purchases by supplier: one row of fn_report_purchases_by_supplier (amounts in the primary currency).
    // Owed now is for all time, not only the period. The report adds a TOTAL row at the end (IsTotal).
    public class PurchasesBySupplierRow
    {
        [Browsable(false)] public int? SupplierId { get; set; }
        [Browsable(false)] public bool IsTotal { get; set; }

        [DisplayName("Supplier")] public string Supplier { get; set; } = string.Empty;
        [DisplayName("Phone")] public string? Phone { get; set; }
        [DisplayName("Purchases #")] public long InvoiceCount { get; set; }
        [DisplayName("Purchases")] public decimal Purchases { get; set; }
        [DisplayName("% of purchases")] public decimal? Share { get; set; }   // filled by the report
        [DisplayName("Discounts")] public decimal Discounts { get; set; }
        [DisplayName("Paid in period")] public decimal Paid { get; set; }
        [DisplayName("Last purchase")] public DateTime? LastPurchase { get; set; }
        [DisplayName("We owe now")] public decimal? OwedNow { get; set; }

        /// <summary>The TOTAL row of a list of suppliers.</summary>
        public static PurchasesBySupplierRow TotalOf(IReadOnlyCollection<PurchasesBySupplierRow> suppliers) => new()
        {
            IsTotal = true,
            Supplier = $"TOTAL  ({suppliers.Count} suppliers)",
            InvoiceCount = suppliers.Sum(s => s.InvoiceCount),
            Purchases = suppliers.Sum(s => s.Purchases),
            Share = suppliers.Count > 0 ? 100 : null,
            Discounts = suppliers.Sum(s => s.Discounts),
            Paid = suppliers.Sum(s => s.Paid),
            OwedNow = suppliers.Sum(s => s.OwedNow ?? 0),
        };
    }
}
