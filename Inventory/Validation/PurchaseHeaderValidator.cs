using Inventory.Models;

namespace Inventory.Validation
{
    /// <summary>
    /// Checks a purchase header before it is sent to the database, so mistakes are shown at once
    /// without a database call. The database (fn_purchase_save / fn_post_purchase) checks the same
    /// rules again as the final safety net, so keep both in step when a rule changes.
    /// </summary>
    public static class PurchaseHeaderValidator
    {
        // Column sizes in purchase_invoices
        public const int SupplierInvoiceNoMaxLength = 50;   // supplier_invoice_no varchar(50)
        public const int NotesMaxLength = 300;              // notes varchar(300)
        public const decimal MaxAmount = 9_999_999_999_999_999.99m;   // numeric(18,2)

        /// <param name="header">The header as entered.</param>
        /// <param name="subTotal">Sum of the line totals.</param>
        /// <param name="lineCount">Number of lines.</param>
        /// <param name="post">True when the purchase is being posted (stricter: needs lines).</param>
        public static ValidationResult Validate(PurchaseHeaderModel header, decimal subTotal, int lineCount, bool post)
        {
            var result = new ValidationResult();

            if (!header.IsDraft)
                result.Add("This purchase is already posted or cancelled and cannot be changed.");

            // Supplier is optional until the supplier form exists. To require it later:
            // if (header.SupplierId == null) result.Add("Choose a supplier.", nameof(PurchaseHeaderModel.SupplierId));

            if (header.InvoiceDate.Date > DateTime.Today)
                result.Add("Invoice date cannot be in the future.", nameof(PurchaseHeaderModel.InvoiceDate));
            if (header.InvoiceDate.Year < 2000)
                result.Add("Invoice date is not valid.", nameof(PurchaseHeaderModel.InvoiceDate));

            if ((header.SupplierInvoiceNo?.Trim().Length ?? 0) > SupplierInvoiceNoMaxLength)
                result.Add($"Supplier invoice no. can have at most {SupplierInvoiceNoMaxLength} characters.", nameof(PurchaseHeaderModel.SupplierInvoiceNo));
            if ((header.Notes?.Trim().Length ?? 0) > NotesMaxLength)
                result.Add($"Notes can have at most {NotesMaxLength} characters.", nameof(PurchaseHeaderModel.Notes));

            if (header.Discount < 0)
                result.Add("Discount cannot be negative.", nameof(PurchaseHeaderModel.Discount));
            else if (header.Discount > subTotal)
                result.Add($"Discount ({header.Discount:#,0.##}) is bigger than the invoice ({subTotal:#,0.##}).", nameof(PurchaseHeaderModel.Discount));
            if (decimal.Round(header.Discount, 2) != header.Discount)
                result.Add("Discount can have at most 2 decimals.", nameof(PurchaseHeaderModel.Discount));

            if (subTotal > MaxAmount)
                result.Add("The invoice total is too large.");

            if (post && lineCount == 0)
                result.Add("Add at least one item before posting.");

            return result;
        }
    }
}
