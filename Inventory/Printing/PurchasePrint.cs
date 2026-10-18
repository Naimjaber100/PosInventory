using System.Drawing.Printing;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory.Printing
{
    // One purchase invoice or return to supplier on A4 (Purchase list > Print, or Print in the purchase window),
    // drawn by A4Document: the company and the document (number, date, the supplier's invoice no., status), the
    // supplier and the currency with its rate, the lines, the totals in the invoice's currency (and in the primary
    // currency when it is another one), the notes and lines to sign. What is saved is printed (a draft says DRAFT).
    public static class PurchasePrint
    {
        /// <summary>Loads the purchase and everything around it, then shows the printer choice and the preview.</summary>
        public static async Task PrintAsync(IWin32Window owner, int purchaseId)
        {
            var repository = new PurchaseRepository();
            var header = await repository.GetByIdAsync(purchaseId);
            if (header == null)
            {
                MessageBox.Show(owner, "This purchase no longer exists.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var lines = (await repository.GetLinesAsync(purchaseId)).ToList();
            var company = await new CompanyRepository().GetAsync();
            byte[]? logo = null;
            try { logo = await new CompanyRepository().GetLogoAsync(); } catch { /* no logo column yet */ }
            SupplierModel? supplier = null;
            if (header.SupplierId is int supplierId)
                supplier = (await new SupplierRepository().GetAllAsync(null, includeInactive: true)).FirstOrDefault(s => s.SupplierId == supplierId);
            var currencies = CurrencySet.Default;
            try { currencies = await new CurrencyRepository().GetSetAsync(); } catch { /* LBP / USD defaults */ }

            A4Document.Show(owner, Content(header, lines, supplier, currencies), company, logo);
        }

        /// <summary>The print job of one purchase (A4 portrait); the caller disposes it.</summary>
        public static PrintDocument CreateDocument(PurchaseHeaderModel header, List<PurchaseLineModel> lines, CompanyModel company,
            byte[]? logo, SupplierModel? supplier, CurrencySet currencies) =>
            A4Document.Create(Content(header, lines, supplier, currencies), company, logo);

        private static A4Content Content(PurchaseHeaderModel header, List<PurchaseLineModel> lines, SupplierModel? supplier, CurrencySet currencies)
        {
            var currency = currencies.Find(header.Currency ?? currencies.Primary.Code) ?? currencies.Primary;
            var decimals = Math.Max((int)currency.Decimals, 0);
            string Money(decimal value) => value.ToString(decimals == 0 ? "#,0" : "#,0." + new string('0', decimals));
            static string Qty(decimal value) => value.ToString(value % 1 == 0 ? "#,0" : "#,0.###");

            var totals = new List<A4Total> { new($"{lines.Count} line(s)   ·   Sub total", Money(header.SubTotal)) };
            if (header.Discount != 0) totals.Add(new("Discount", "-" + Money(header.Discount)));
            totals.Add(new(header.IsReturn ? "TOTAL RETURNED" : "TOTAL", currency.Format(header.Total), Big: true, LineAbove: true));
            if (!currency.IsPrimary) totals.Add(new($"In {currencies.Primary.Code}", currencies.Primary.Format(header.TotalLbp)));

            return new A4Content
            {
                Title = header.IsReturn ? "RETURN TO SUPPLIER" : "PURCHASE INVOICE",
                TitleInRed = header.IsReturn,
                Number = header.PurchaseNo ?? $"Draft #{header.PurchaseId}",
                Info =
                {
                    new("Number", header.PurchaseNo ?? $"Draft #{header.PurchaseId}"),
                    new("Date", header.InvoiceDate.ToString("dd/MM/yyyy")),
                    new("Supplier invoice", header.SupplierInvoiceNo),
                    new("Status", header.Status switch { 0 => "DRAFT (not posted)", 1 => "Posted", _ => "Cancelled" }, Warning: header.Status == 0),
                },
                PartyCaption = header.IsReturn ? "Returned to" : "Supplier",
                PartyName = supplier?.Name ?? header.SupplierName ?? "(no supplier)",
                PartyDetails =
                {
                    supplier?.Phone is { Length: > 0 } phone ? $"Tel {phone}" : string.Empty,
                    supplier?.Address ?? string.Empty,
                    supplier?.VatNumber is { Length: > 0 } vat ? $"VAT no. {vat}" : string.Empty,
                },
                CurrencyText = currency.IsPrimary
                    ? $"Amounts in {currency.Code}"
                    : $"Amounts in {currency.Code}   ·   1 {currency.Code} = {currencies.Primary.Format(header.CurrencyRate)}",
                Columns =
                {
                    new("#", 0.05f, Right: true), new("Item", 0.33f), new("Packaging", 0.11f), new("Barcode", 0.14f),
                    new("Qty", 0.08f, Right: true), new($"Unit cost ({currency.Code})", 0.14f, Right: true), new($"Total ({currency.Code})", 0.15f, Right: true),
                },
                Rows = lines.Select((l, i) => new[]
                {
                    (i + 1).ToString(), l.ItemName, l.PackagingName, l.Barcode ?? string.Empty, Qty(l.Quantity), Money(l.UnitCost), Money(l.LineTotal),
                }).ToList(),
                Totals = totals,
                Notes = header.Notes,
                LeftSignature = header.IsReturn ? "Handed over by" : "Received by",
                RightSignature = "Supplier",
            };
        }
    }
}
