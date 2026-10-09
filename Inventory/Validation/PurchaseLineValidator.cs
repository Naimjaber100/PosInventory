using Inventory.Models;

namespace Inventory.Validation
{
    /// <summary>
    /// Checks purchase lines before they are sent to the database, so mistakes are shown at once
    /// without a database call. The database (fn_purchase_save / fn_post_purchase) checks the same
    /// rules again as the final safety net, so keep both in step when a rule changes.
    /// </summary>
    public static class PurchaseLineValidator
    {
        // Column sizes in purchase_lines
        public const int QuantityDecimals = 3;    // quantity numeric(18,3)
        public const int UnitCostDecimals = 4;    // unit_cost numeric(18,4)
        public const decimal MaxQuantity = 999_999_999_999_999.999m;
        public const decimal MaxUnitCost = 99_999_999_999_999.9999m;

        /// <summary>Checks one line. lineIndex is its position in the list (0-based) for marking the row.</summary>
        public static ValidationResult Validate(PurchaseEditLine line, int lineIndex)
        {
            var result = new ValidationResult();
            var name = string.IsNullOrEmpty(line.PackagingName) ? line.ItemName : $"{line.ItemName} · {line.PackagingName}";
            var where = $"Line {lineIndex + 1} ({name})";

            if (line.PackagingId <= 0)
                result.Add($"{where}: the item is missing. Remove the line and add it again.", null, lineIndex);

            if (line.Quantity <= 0)
                result.Add($"{where}: quantity must be more than 0.", nameof(PurchaseEditLine.Quantity), lineIndex);
            else if (line.Quantity > MaxQuantity)
                result.Add($"{where}: quantity is too large.", nameof(PurchaseEditLine.Quantity), lineIndex);
            else if (decimal.Round(line.Quantity, QuantityDecimals) != line.Quantity)
                result.Add($"{where}: quantity can have at most {QuantityDecimals} decimals.", nameof(PurchaseEditLine.Quantity), lineIndex);
            else if (!line.IsWeighted && line.Factor > 0 && (line.Quantity * line.Factor) % 1 != 0)
                // fn_post_purchase: items not sold by weight are bought in whole base units
                result.Add($"{where}: must be a whole number of {(string.IsNullOrEmpty(line.BaseUnit) ? "units" : line.BaseUnit)} " +
                           $"({line.Quantity:0.###} x {line.Factor:0.###} = {line.Quantity * line.Factor:0.###}).",
                           nameof(PurchaseEditLine.Quantity), lineIndex);

            if (line.UnitCost < 0)
                result.Add($"{where}: unit cost cannot be negative.", nameof(PurchaseEditLine.UnitCost), lineIndex);
            else if (line.UnitCost > MaxUnitCost)
                result.Add($"{where}: unit cost is too large.", nameof(PurchaseEditLine.UnitCost), lineIndex);
            else if (decimal.Round(line.UnitCost, UnitCostDecimals) != line.UnitCost)
                result.Add($"{where}: unit cost can have at most {UnitCostDecimals} decimals.", nameof(PurchaseEditLine.UnitCost), lineIndex);

            return result;
        }

        /// <summary>Checks every line.</summary>
        public static ValidationResult ValidateAll(IReadOnlyList<PurchaseEditLine> lines)
        {
            var result = new ValidationResult();
            for (var i = 0; i < lines.Count; i++)
                result.Merge(Validate(lines[i], i));
            return result;
        }
    }
}
