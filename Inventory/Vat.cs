using Inventory.Repositories;

namespace Inventory
{
    // TVA of a sale (Settings > Company), worked out exactly as fn_save_sale / fn_save_return do
    // (Database/pos_functions.sql), so the total the cashier asks for is the one the database saves:
    //   - the invoice discount is shared over the lines in proportion (the rest of the rounding on the biggest line);
    //   - each line's TVA is on what it costs after its share, at its item's rate, rounded to 2 decimals;
    //   - prices with TVA included: it is the part of the total that is TVA, the total does not change;
    //     prices without TVA: it is added on top of the total.
    public sealed class VatSettings
    {
        public static readonly VatSettings Off = new(false, true);

        public bool Enabled { get; }
        public bool PricesInclude { get; }

        public VatSettings(bool enabled, bool pricesInclude)
        {
            Enabled = enabled;
            PricesInclude = pricesInclude;
        }

        /// <summary>True when TVA is added on top of the prices (the total grows by it).</summary>
        public bool AddsToTotal => Enabled && !PricesInclude;

        /// <summary>The company's settings; Off when they cannot be read (company_functions.sql not applied yet).</summary>
        public static async Task<VatSettings> LoadAsync()
        {
            try
            {
                var company = await new CompanyRepository().GetAsync();
                return new VatSettings(company.VatEnabled, company.PricesIncludeVat);
            }
            catch
            {
                return Off;
            }
        }
    }

    public static class Vat
    {
        /// <summary>The TVA of an amount at rate %: the part of it that is TVA (included), or what is added on top (as fn_vat_of).</summary>
        public static decimal Of(decimal amount, decimal rate, bool included) =>
            Math.Round(amount * rate / (included ? 100 + rate : 100), 2, MidpointRounding.AwayFromZero);

        /// <summary>
        /// The TVA of the whole sale. lines: each line's total after its own discount, and its item's TVA rate.
        /// invoiceDiscount: the whole invoice discount.
        /// </summary>
        public static decimal OfSale(IReadOnlyList<(decimal Total, decimal Rate)> lines, decimal invoiceDiscount, VatSettings settings)
        {
            if (!settings.Enabled || lines.Count == 0) return 0;

            var sub = lines.Sum(l => l.Total);
            var shares = new decimal[lines.Count];
            if (invoiceDiscount > 0 && sub != 0)
            {
                for (var i = 0; i < lines.Count; i++)
                    shares[i] = Math.Round(invoiceDiscount * lines[i].Total / sub, 2, MidpointRounding.AwayFromZero);
                // The rounding rest goes on the biggest line (the first of them), as in fn_save_sale
                var biggest = 0;
                for (var i = 1; i < lines.Count; i++)
                    if (lines[i].Total > lines[biggest].Total) biggest = i;
                shares[biggest] += invoiceDiscount - shares.Sum();
            }

            decimal vat = 0;
            for (var i = 0; i < lines.Count; i++)
                vat += Of(lines[i].Total - shares[i], lines[i].Rate, settings.PricesInclude);
            return vat;
        }
    }
}
