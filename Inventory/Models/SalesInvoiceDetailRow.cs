using System.ComponentModel;

namespace Inventory.Models
{
    // Row of fn_report_sales_invoice_lines: one item line of a sale / return invoice (a return is negative)
    public class SalesInvoiceLine
    {
        public int SaleId { get; set; }
        public int LineNo { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal LineTotal { get; set; }
        public decimal LineProfit { get; set; }
    }

    // Report 3b - Sales by invoice with items: one grid row. Each invoice row (its number, date, cashier, client,
    // how it was paid and its totals) is followed by its item rows (item, qty, price, discount, total, profit);
    // the TOTAL row at the end. Empty cells stay empty, so the invoice and its items read as one block.
    public class SalesInvoiceDetailRow
    {
        public enum RowKind { Invoice, Line, Total }

        [Browsable(false)] public RowKind Kind { get; init; }
        [Browsable(false)] public int SaleId { get; init; }
        [Browsable(false)] public bool IsReturn { get; init; }

        [DisplayName("Invoice")] public string? InvoiceNo { get; init; }
        [DisplayName("Date")] public DateTime? InvoiceDate { get; init; }
        [DisplayName("Cashier")] public string? Cashier { get; init; }
        [DisplayName("Client")] public string? Customer { get; init; }
        [DisplayName("Item")] public string? ItemName { get; init; }
        [DisplayName("Qty")] public decimal? Quantity { get; init; }
        [DisplayName("Price")] public decimal? UnitPrice { get; init; }
        [DisplayName("Discount")] public decimal? Discount { get; init; }
        [DisplayName("Total")] public decimal? Total { get; init; }
        [DisplayName("Profit")] public decimal? Profit { get; init; }
        [DisplayName("Paid with")] public string? Payments { get; init; }

        public static SalesInvoiceDetailRow ForInvoice(SalesByInvoiceRow i) => new()
        {
            Kind = RowKind.Invoice,
            SaleId = i.SaleId,
            IsReturn = i.IsReturn,
            InvoiceNo = i.IsReturn ? $"{i.InvoiceNo}  (return)" : i.InvoiceNo,
            InvoiceDate = i.InvoiceDate,
            Cashier = i.Cashier,
            Customer = i.Customer,
            ItemName = $"{i.ItemCount} item line(s)",
            Quantity = i.Quantity,
            Discount = i.Discounts,
            Total = i.Total,
            Profit = i.Profit,
            Payments = i.Payments,
        };

        public static SalesInvoiceDetailRow ForLine(SalesInvoiceLine l, bool isReturn) => new()
        {
            Kind = RowKind.Line,
            SaleId = l.SaleId,
            IsReturn = isReturn,
            ItemName = "    " + l.ItemName,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            Discount = l.Discount != 0 ? l.Discount : null,
            Total = l.LineTotal,
            Profit = l.LineProfit,
        };

        public static SalesInvoiceDetailRow ForTotal(SalesByInvoiceRow t) => new()
        {
            Kind = RowKind.Total,
            InvoiceNo = t.InvoiceNo,
            ItemName = $"{t.ItemCount} item line(s)",
            Quantity = t.Quantity,
            Discount = t.Discounts,
            Total = t.Total,
            Profit = t.Profit,
        };
    }
}
