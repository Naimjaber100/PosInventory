using Inventory.Models;

namespace Inventory.Validation
{
    /// <summary>
    /// Checks a sale on the cashier screen before it is sent to the database, so mistakes are shown
    /// at once. fn_save_sale checks the same rules again as the final safety net, so keep both in step.
    /// </summary>
    public static class SaleValidator
    {
        public const int QuantityDecimals = 3;   // sales_lines.quantity numeric(18,3)
        public const int MoneyDecimals = 2;      // prices, discounts, totals numeric(18,2)

        /// <summary>Checks one line; lineIndex is its position on the screen (0-based).</summary>
        public static ValidationResult ValidateLine(PosLine line, int lineIndex)
        {
            var result = new ValidationResult();
            var where = $"Line {lineIndex + 1} ({line.ItemName})";

            if (line.Quantity <= 0)
                result.Add($"{where}: quantity must be more than 0.", nameof(PosLine.Quantity), lineIndex);
            else if (decimal.Round(line.Quantity, QuantityDecimals) != line.Quantity)
                result.Add($"{where}: quantity can have at most {QuantityDecimals} decimals.", nameof(PosLine.Quantity), lineIndex);
            else if (!line.IsWeighted && line.Quantity % 1 != 0)
                result.Add($"{where}: is sold in whole units only.", nameof(PosLine.Quantity), lineIndex);

            if (line.UnitPrice < 0)
                result.Add($"{where}: price cannot be negative.", nameof(PosLine.UnitPrice), lineIndex);
            else if (decimal.Round(line.UnitPrice, MoneyDecimals) != line.UnitPrice)
                result.Add($"{where}: price can have at most {MoneyDecimals} decimals.", nameof(PosLine.UnitPrice), lineIndex);

            if (line.Discount < 0)
                result.Add($"{where}: discount cannot be negative.", nameof(PosLine.Discount), lineIndex);
            else if (line.TotalDiscount > line.Quantity * line.UnitPrice)
                result.Add($"{where}: discount is bigger than the line.", nameof(PosLine.Discount), lineIndex);

            return result;
        }

        /// <summary>Lines and invoice discount (used before holding and before paying).</summary>
        public static ValidationResult ValidateSale(IReadOnlyList<PosLine> lines, decimal invoiceDiscount)
        {
            var result = new ValidationResult();
            if (lines.Count == 0) result.Add("The invoice has no items.");
            for (var i = 0; i < lines.Count; i++) result.Merge(ValidateLine(lines[i], i));

            var subTotal = lines.Sum(l => l.LineTotal);
            if (invoiceDiscount < 0)
                result.Add("Invoice discount cannot be negative.", "InvoiceDiscount");
            else if (invoiceDiscount > 0 && invoiceDiscount > subTotal)
                result.Add($"Invoice discount ({invoiceDiscount:#,0.##}) is bigger than the invoice ({subTotal:#,0.##}).", "InvoiceDiscount");
            else if (decimal.Round(invoiceDiscount, MoneyDecimals) != invoiceDiscount)
                result.Add("Invoice discount can have at most 2 decimals.", "InvoiceDiscount");
            return result;
        }

        /// <summary>
        /// Payments against the total (in the primary currency). Same rules as fn_save_sale: every currency is
        /// active with a rate, enough is paid, and the change can only come out of cash (card cannot be more than the total).
        /// </summary>
        public static ValidationResult ValidatePayment(decimal total, CurrencySet currencies, IReadOnlyList<SalePaymentModel> payments)
        {
            var result = new ValidationResult();
            if (currencies.SecondRateMissing) result.Add($"Set the exchange rate of {currencies.Second!.Code} first (Settings > Currencies).");

            foreach (var p in payments)
            {
                if (p.Amount < 0) result.Add("A payment amount cannot be negative.");
                if (currencies.Find(p.Currency) is not { IsActive: true, HasRate: true }) result.Add($"{p.Currency} cannot be used: it is off or has no exchange rate.");
                if (p.Method == PaymentMethod.OnAccount) result.Add("On-account sales need a customer (not available here yet).");
            }

            var paid = payments.Where(p => p.Amount > 0).Sum(p => currencies.ToPrimary(p.Amount, p.Currency));
            var cash = payments.Where(p => p.Amount > 0 && p.Method == PaymentMethod.Cash).Sum(p => currencies.ToPrimary(p.Amount, p.Currency));

            if (paid < total)
                result.Add($"Paid {currencies.FormatPrimary(paid)} is less than the total {currencies.FormatPrimary(total)}.");
            else if (paid - total > cash)
                result.Add("Card is more than the total. Only cash can give change.");
            return result;
        }
    }
}
