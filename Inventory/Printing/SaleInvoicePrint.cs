using System.Drawing.Printing;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory.Printing
{
    // One sale invoice or return on A4 (back office: Invoice > Print A4 invoice), drawn by A4Document like a purchase.
    // From the same data as the receipt (fn_sale_receipt): the company and the invoice (number, date, cashier, paid or
    // unpaid), the client, the lines with their discounts and promotions, the totals, how it was paid and the change.
    public static class SaleInvoicePrint
    {
        /// <summary>Loads the invoice, then shows the printer choice and the preview.</summary>
        public static async Task PrintAsync(IWin32Window owner, int saleId, CurrencySet currencies)
        {
            var receipt = await new SaleRepository().GetReceiptAsync(saleId);
            if (receipt == null)
            {
                MessageBox.Show(owner, "This invoice was not found.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            byte[]? logo = null;
            try { logo = await new CompanyRepository().GetLogoAsync(); } catch { /* no logo column yet */ }
            A4Document.Show(owner, Content(receipt, currencies), Company(receipt), logo);
        }

        /// <summary>The print job of one invoice (A4 portrait); the caller disposes it.</summary>
        public static PrintDocument CreateDocument(Receipt receipt, CurrencySet currencies, byte[]? logo) =>
            A4Document.Create(Content(receipt, currencies), Company(receipt), logo);

        private static CompanyModel Company(Receipt r) => new()
        {
            Name = r.Company.Name ?? CompanyRepository.DefaultName,
            Address = r.Company.Address,
            Email = r.Company.Email,
            Mobile = r.Company.Mobile,
            VatNumber = r.Company.VatNumber,
        };

        private static A4Content Content(Receipt r, CurrencySet currencies)
        {
            var primary = currencies.Primary;
            string Money(decimal value) => Math.Round(value, primary.Decimals).ToString(primary.Decimals == 0 ? "#,0" : "#,0." + new string('0', primary.Decimals));
            static string Qty(decimal value) => value.ToString(value % 1 == 0 ? "#,0" : "#,0.###");

            var onAccount = r.Payments.Where(p => p.Method == 3).Sum(p => p.AmountLbp);
            var discounts = r.LineDiscounts + r.Discount;
            var totals = new List<A4Total> { new($"{r.Lines.Count} line(s)   ·   Sub total", Money(r.SubTotal + r.LineDiscounts)) };
            if (discounts > 0)
                totals.Add(new("Discount" + (r.Discount > 0 && !string.IsNullOrWhiteSpace(r.PromoName) ? $" (incl. {r.PromoName})" : string.Empty), "-" + Money(discounts)));
            totals.Add(new(r.IsReturn ? "TOTAL REFUNDED" : "TOTAL", primary.Format(r.Total), Big: true, LineAbove: true));
            if (r.UsdRate > 1 && !string.Equals(r.PrimaryCode, "USD", StringComparison.OrdinalIgnoreCase))
                totals.Add(new($"In USD (1 $ = {Money(r.UsdRate)})", $"$ {r.Total / r.UsdRate:#,0.00}"));
            var firstPayment = true;
            foreach (var p in r.Payments)
            {
                var how = p.Method switch { 2 => "Card", 3 => "On account (unpaid)", _ => r.IsReturn ? "Cash back" : "Cash" };
                var amount = currencies.Find(p.Currency) is { } cur ? cur.Format(p.Amount) : $"{p.Currency} {p.Amount:#,0.##}";
                totals.Add(new($"{how} {p.Currency}", amount, LineAbove: firstPayment));
                firstPayment = false;
            }
            if (r.ChangeLbp > 0) totals.Add(new("Change", primary.Format(r.ChangeLbp)));

            return new A4Content
            {
                Title = r.IsReturn ? "RETURN / REFUND" : "INVOICE",
                TitleInRed = r.IsReturn,
                Number = r.InvoiceNo,
                Info =
                {
                    new("Number", r.InvoiceNo),
                    new("Date", r.InvoiceDate.ToLocalTime().ToString("dd/MM/yyyy  HH:mm")),
                    new("Cashier", r.Cashier),
                    new("Payment", r.IsReturn ? null : onAccount > 0 ? (onAccount >= r.Total ? "UNPAID (on account)" : "Partly on account") : "Paid",
                        Warning: !r.IsReturn && onAccount > 0),
                },
                PartyCaption = "Client",
                PartyName = r.Customer ?? "Walk-in customer",
                PartyDetails =
                {
                    r.CustomerPhone is { Length: > 0 } phone ? $"Tel {phone}" : string.Empty,
                    r.CustomerAddress ?? string.Empty,
                },
                CurrencyText = $"Amounts in {primary.Code}",
                Columns =
                {
                    new("#", 0.05f, Right: true), new("Item", 0.43f), new("Qty", 0.09f, Right: true),
                    new($"Price ({primary.Code})", 0.14f, Right: true), new("Discount", 0.13f, Right: true), new($"Total ({primary.Code})", 0.16f, Right: true),
                },
                Rows = r.Lines.Select((l, i) => new[]
                {
                    (i + 1).ToString(),
                    l.Discount > 0 && !string.IsNullOrWhiteSpace(l.PromoName) ? $"{l.ItemName}\nPromo: {l.PromoName}" : l.ItemName,
                    Qty(l.Quantity), Money(l.UnitPrice), l.Discount > 0 ? "-" + Money(l.Discount) : string.Empty, Money(l.LineTotal),
                }).ToList(),
                Totals = totals,
                LeftSignature = r.Customer != null ? "Seller" : null,
                RightSignature = r.Customer != null ? "Client" : null,
            };
        }
    }
}
