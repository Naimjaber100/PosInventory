using System.Drawing.Printing;

namespace Inventory.Reports
{
    // Prints what a report grid shows on paper (A4 landscape): title, filters and totals on the first page, then the
    // visible columns as a table. The column names repeat on every page; long text wraps; the total row is bold.
    // Print: choose the printer, look at the preview, then print from there.
    public static class ReportPrint
    {
        private static readonly Color HeaderBack = Color.FromArgb(232, 240, 254);
        private static readonly Color Line = Color.FromArgb(200, 205, 212);

        /// <param name="totals">The total cards of the report, as "CAPTION: value" texts.</param>
        /// <param name="formatCell">The report's cell colors: a cell whose text is hidden there (same color as its background) prints empty.</param>
        public static void Print(IWin32Window owner, string title, string filters, IReadOnlyList<string> totals, DataGridView grid,
            Func<object, bool> isTotalRow, Action<object, DataGridViewColumn, DataGridViewCellStyle> formatCell)
        {
            var columns = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();
            if (columns.Count == 0) return;

            // What is on the screen, as text (dates, numbers and custom texts as the grid formats them)
            var rows = new List<(string[] Cells, bool Total)>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                var item = row.DataBoundItem;
                var cells = new string[columns.Count];
                for (var c = 0; c < columns.Count; c++)
                {
                    var cell = row.Cells[columns[c].Index];
                    var text = cell.FormattedValue?.ToString() ?? string.Empty;
                    if (item != null)
                    {
                        var style = new DataGridViewCellStyle(cell.InheritedStyle);
                        formatCell(item, columns[c], style);
                        if (style.ForeColor.ToArgb() == style.BackColor.ToArgb()) text = string.Empty;
                    }
                    cells[c] = text;
                }
                rows.Add((cells, item != null && isTotalRow(item)));
            }

            using var document = new PrintDocument { DocumentName = title };
            document.DefaultPageSettings.Landscape = true;
            document.DefaultPageSettings.Margins = new Margins(40, 40, 40, 45);

            using (var choose = new PrintDialog { Document = document, UseEXDialog = true, AllowSomePages = false })
            {
                if (choose.ShowDialog(owner) != DialogResult.OK) return;
            }
            document.DefaultPageSettings.Landscape = true;   // the chosen printer starts from its own settings

            var next = 0;      // the next row to print
            var page = 0;
            var printedAt = DateTime.Now;
            document.BeginPrint += (_, _) => { next = 0; page = 0; };   // the preview and the real print both start at the top
            document.PrintPage += (_, e) =>
            {
                var g = e.Graphics!;
                var area = e.MarginBounds;
                page++;

                using var titleFont = new Font("Segoe UI", 15F, FontStyle.Bold);
                using var smallFont = new Font("Segoe UI", 8.5F);
                using var font = new Font("Segoe UI", 8.5F);
                using var boldFont = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                using var linePen = new Pen(Line);
                using var darkPen = new Pen(Color.Black);
                using var headerBrush = new SolidBrush(HeaderBack);
                float y = area.Top;

                if (page == 1)
                {
                    g.DrawString(title, titleFont, Brushes.Black, area.Left, y);
                    y += titleFont.GetHeight(g) + 2;
                    g.DrawString(filters, smallFont, Brushes.DimGray, area.Left, y);
                    y += smallFont.GetHeight(g) + 6;
                    if (totals.Count > 0)
                    {
                        var text = string.Join("      ", totals);
                        var size = g.MeasureString(text, boldFont, area.Width);
                        g.DrawString(text, boldFont, Brushes.Black, new RectangleF(area.Left, y, area.Width, size.Height));
                        y += size.Height + 8;
                    }
                }

                // Columns share the page width as they share the grid
                var gridWidth = (float)columns.Sum(c => c.Width);
                var widths = columns.Select(c => c.Width / gridWidth * area.Width).ToArray();
                const float pad = 3f;

                using var near = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
                using var far = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near };
                StringFormat FormatOf(DataGridViewColumn column) =>
                    column.DefaultCellStyle.Alignment is DataGridViewContentAlignment.MiddleRight or DataGridViewContentAlignment.TopRight
                        or DataGridViewContentAlignment.BottomRight ? far : near;

                float RowHeight(string[] cells, Font f)
                {
                    var height = f.GetHeight(g);
                    for (var c = 0; c < cells.Length; c++)
                        height = Math.Max(height, g.MeasureString(cells[c], f, (int)Math.Max(10, widths[c] - pad * 2)).Height);
                    return height + pad * 2;
                }

                void DrawRow(string[] cells, Font f, float height)
                {
                    var x = (float)area.Left;
                    for (var c = 0; c < cells.Length; c++)
                    {
                        g.DrawString(cells[c], f, Brushes.Black, new RectangleF(x + pad, y + pad, widths[c] - pad * 2, height - pad), FormatOf(columns[c]));
                        x += widths[c];
                    }
                }

                // Column names
                var headers = columns.Select(c => c.HeaderText).ToArray();
                var headerHeight = RowHeight(headers, boldFont);
                g.FillRectangle(headerBrush, area.Left, y, area.Width, headerHeight);
                DrawRow(headers, boldFont, headerHeight);
                y += headerHeight;
                g.DrawLine(darkPen, area.Left, y, area.Right, y);

                // Rows until the page is full (a page always takes at least one row)
                var footerTop = area.Bottom - smallFont.GetHeight(g) - 4;
                var first = true;
                while (next < rows.Count)
                {
                    var (cells, total) = rows[next];
                    var rowFont = total ? boldFont : font;
                    var height = RowHeight(cells, rowFont);
                    if (!first && y + height > footerTop) break;
                    if (total) g.DrawLine(darkPen, area.Left, y, area.Right, y);
                    DrawRow(cells, rowFont, height);
                    y += height;
                    g.DrawLine(linePen, area.Left, y, area.Right, y);
                    next++;
                    first = false;
                }

                g.DrawString($"Printed {printedAt:dd/MM/yyyy HH:mm}", smallFont, Brushes.DimGray, area.Left, footerTop + 4);
                g.DrawString($"Page {page}", smallFont, Brushes.DimGray, new RectangleF(area.Left, footerTop + 4, area.Width, smallFont.GetHeight(g) + 2), far);
                e.HasMorePages = next < rows.Count;
            };

            using var preview = new PrintPreviewDialog
            {
                Document = document,
                Text = $"Print - {title}",
                StartPosition = FormStartPosition.CenterParent,
                ShowIcon = false,
                ClientSize = new Size(1100, 760),
            };
            preview.PrintPreviewControl.Zoom = 1.0;
            preview.ShowDialog(owner);
        }
    }
}
