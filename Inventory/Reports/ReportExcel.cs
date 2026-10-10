using ClosedXML.Excel;

namespace Inventory.Reports
{
    // Saves what a report grid shows to an Excel file: title, filters, the visible columns, numbers as numbers
    public static class ReportExcel
    {
        public static void Save(string path, string title, string filters, DataGridView grid, Func<object, bool> isTotalRow)
        {
            using var book = new XLWorkbook();
            var sheet = book.Worksheets.Add(SheetName(title));

            sheet.Cell(1, 1).Value = title;
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;
            sheet.Cell(2, 1).Value = filters;
            sheet.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

            var columns = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();
            const int headerRow = 4;
            for (var c = 0; c < columns.Count; c++)
            {
                var cell = sheet.Cell(headerRow, c + 1);
                cell.Value = columns[c].HeaderText;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(232, 240, 254);
            }

            var r = headerRow + 1;
            foreach (DataGridViewRow row in grid.Rows)
            {
                var total = row.DataBoundItem != null && isTotalRow(row.DataBoundItem);
                for (var c = 0; c < columns.Count; c++)
                {
                    var cell = sheet.Cell(r, c + 1);
                    var value = row.Cells[columns[c].Index].Value;
                    switch (value)
                    {
                        case decimal d:
                            cell.Value = d;
                            cell.Style.NumberFormat.Format = columns[c].DefaultCellStyle.Format == "0.0" ? "0.0"
                                : d == decimal.Truncate(d) ? "#,##0" : "#,##0.00";
                            break;
                        case long or int:
                            cell.Value = Convert.ToDouble(value);
                            cell.Style.NumberFormat.Format = "#,##0";
                            break;
                        case DateTime dt:
                            cell.Value = dt;
                            cell.Style.DateFormat.Format = "dd/MM/yyyy";
                            break;
                        case null:
                            break;
                        default:
                            cell.Value = value.ToString();
                            break;
                    }
                    if (total) cell.Style.Font.Bold = true;
                }
                if (total) sheet.Range(r, 1, r, columns.Count).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                r++;
            }

            sheet.Columns(1, columns.Count).AdjustToContents();
            sheet.SheetView.FreezeRows(headerRow);
            book.SaveAs(path);
        }

        // Excel sheet names: at most 31 characters, no : \ / ? * [ ]
        private static string SheetName(string title)
        {
            var name = new string(title.Where(ch => !":\\/?*[]".Contains(ch)).ToArray());
            return name.Length > 31 ? name[..31] : name;
        }
    }
}
