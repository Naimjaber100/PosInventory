using System.Globalization;
using ClosedXML.Excel;
using Inventory.Models;

namespace Inventory
{
    // Reads products from the first worksheet of an .xlsx file.
    // Row 1 holds the headers (any order; case and spaces ignored). Only "Name" is required.
    public static class ProductExcelReader
    {
        public static readonly string[] Headers =
        {
            "Name", "Category", "Type", "Base unit", "Size", "Color", "Cost", "Sale price",
            "Barcode", "Opening stock", "Reorder level", "Weighted", "Active"
        };

        // Accepted header spellings (normalized) -> field
        private static readonly Dictionary<string, string> Aliases = new()
        {
            ["name"] = "name", ["itemname"] = "name", ["productname"] = "name",
            ["category"] = "category",
            ["type"] = "type", ["producttype"] = "type",
            ["baseunit"] = "unit", ["unit"] = "unit",
            ["size"] = "size",
            ["color"] = "color", ["colour"] = "color",
            ["cost"] = "cost", ["avgcost"] = "cost", ["averagecost"] = "cost",
            ["saleprice"] = "price", ["price"] = "price",
            ["barcode"] = "barcode",
            ["openingstock"] = "stock", ["stock"] = "stock", ["stockqty"] = "stock", ["quantity"] = "stock", ["qty"] = "stock",
            ["reorderlevel"] = "reorder", ["reorder"] = "reorder",
            ["weighted"] = "weighted", ["isweighted"] = "weighted", ["soldbyweight"] = "weighted",
            ["active"] = "active", ["isactive"] = "active",
        };

        public static (List<ProductImportRow> Rows, List<string> Errors) Read(string path)
        {
            var rows = new List<ProductImportRow>();
            var errors = new List<string>();

            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheets.First();
            var used = sheet.RangeUsed();
            if (used == null)
            {
                errors.Add("The worksheet is empty.");
                return (rows, errors);
            }

            // Map header columns
            var columns = new Dictionary<string, int>();
            foreach (var cell in sheet.Row(1).CellsUsed())
            {
                var key = Normalize(cell.GetString());
                if (Aliases.TryGetValue(key, out var field) && !columns.ContainsKey(field))
                    columns[field] = cell.Address.ColumnNumber;
            }
            if (!columns.ContainsKey("name"))
            {
                errors.Add("Row 1 must contain a \"Name\" header.");
                return (rows, errors);
            }

            var lastRow = used.LastRow().RowNumber();
            for (var r = 2; r <= lastRow; r++)
            {
                var row = sheet.Row(r);
                if (row.IsEmpty()) continue;

                string Text(string field) =>
                    columns.TryGetValue(field, out var c) ? row.Cell(c).GetFormattedString().Trim() : string.Empty;

                var item = new ProductImportRow { RowNumber = r, Name = Text("name") };
                if (item.Name.Length == 0)
                {
                    errors.Add($"Row {r}: Name is required.");
                    continue;
                }
                if (item.Name.Length > 150) errors.Add($"Row {r}: Name is longer than 150 characters.");

                item.Category = NullIfEmpty(Text("category"));
                item.Size = NullIfEmpty(Text("size"));
                item.Color = NullIfEmpty(Text("color"));
                item.Barcode = NullIfEmpty(columns.TryGetValue("barcode", out var bc) ? row.Cell(bc).GetString().Trim() : string.Empty);
                var unit = Text("unit");
                if (unit.Length > 0) item.BaseUnit = unit;

                if (!TryType(Text("type"), out var type))
                    errors.Add($"Row {r}: Type must be Standard, Bundle or Variants (or 1, 2, 3).");
                item.ProductType = type;

                item.AvgCost = Number(row, columns, "cost", "Cost", errors);
                item.SalePrice = Number(row, columns, "price", "Sale price", errors);
                item.OpeningQty = Number(row, columns, "stock", "Opening stock", errors);
                item.ReorderLevel = Number(row, columns, "reorder", "Reorder level", errors);
                item.IsWeighted = Bool(Text("weighted"), false, "Weighted", r, errors);
                item.IsActive = Bool(Text("active"), true, "Active", r, errors);

                if (item.OpeningQty > 0 && item.ProductType != 1)
                    errors.Add($"Row {r}: Only standard items can have opening stock.");

                rows.Add(item);
            }

            var duplicates = rows.Where(x => x.Barcode != null)
                .GroupBy(x => x.Barcode!, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1);
            foreach (var g in duplicates)
                errors.Add($"Barcode \"{g.Key}\" is repeated on rows {string.Join(", ", g.Select(x => x.RowNumber))}.");

            if (rows.Count == 0 && errors.Count == 0) errors.Add("No items found below the header row.");
            return (rows, errors);
        }

        /// <summary>Writes an import file with the expected headers and one example row.</summary>
        public static void WriteTemplate(string path)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Items");
            for (var i = 0; i < Headers.Length; i++)
                sheet.Cell(1, i + 1).Value = Headers[i];
            sheet.Row(1).Style.Font.Bold = true;

            sheet.Cell(2, 1).Value = "Pepsi 330ml";
            sheet.Cell(2, 2).Value = "Beverages";
            sheet.Cell(2, 3).Value = "Standard";
            sheet.Cell(2, 4).Value = "can";
            sheet.Cell(2, 7).Value = 30.5;
            sheet.Cell(2, 8).Value = 45;
            sheet.Cell(2, 9).SetValue("5449000000996");   // stored as text so long barcodes keep every digit
            sheet.Cell(2, 10).Value = 100;
            sheet.Cell(2, 11).Value = 10;
            sheet.Cell(2, 12).Value = "No";
            sheet.Cell(2, 13).Value = "Yes";
            sheet.Column(9).Style.NumberFormat.Format = "@";
            sheet.Columns().AdjustToContents();
            workbook.SaveAs(path);
        }

        private static string Normalize(string header) =>
            new string(header.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

        private static bool TryType(string value, out short type)
        {
            switch (Normalize(value))
            {
                case "": case "1": case "standard": type = 1; return true;
                case "2": case "bundle": type = 2; return true;
                case "3": case "variants": case "variant": case "model": case "modelwithvariants": type = 3; return true;
                default: type = 1; return false;
            }
        }

        private static decimal Number(IXLRow row, Dictionary<string, int> columns, string field, string column, List<string> errors)
        {
            if (!columns.TryGetValue(field, out var c)) return 0;
            var cell = row.Cell(c);
            if (cell.IsEmpty()) return 0;

            decimal n;
            if (cell.DataType == XLDataType.Number)
            {
                n = (decimal)cell.GetDouble();
            }
            else
            {
                var text = cell.GetString().Trim();
                if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out n) &&
                    !decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out n))
                {
                    errors.Add($"Row {row.RowNumber()}: {column} \"{text}\" is not a number.");
                    return 0;
                }
            }

            if (n >= 0) return n;
            errors.Add($"Row {row.RowNumber()}: {column} cannot be negative.");
            return 0;
        }

        private static bool Bool(string value, bool defaultValue, string column, int row, List<string> errors)
        {
            switch (Normalize(value))
            {
                case "": return defaultValue;
                case "yes": case "y": case "true": case "1": return true;
                case "no": case "n": case "false": case "0": return false;
                default:
                    errors.Add($"Row {row}: {column} must be Yes or No.");
                    return defaultValue;
            }
        }
    }
}
