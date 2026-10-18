using System.Drawing.Printing;
using System.Text.RegularExpressions;
using Inventory.Models;

namespace Inventory.Printing
{
    // What one A4 document (a purchase, a sale invoice...) shows; A4Document draws it.
    public sealed class A4Content
    {
        public string Title { get; init; } = string.Empty;          // "PURCHASE INVOICE"
        public bool TitleInRed { get; init; }                       // a return
        public string Number { get; init; } = string.Empty;         // in the footer of every page too
        public List<A4Info> Info { get; init; } = new();            // under the title: number, date, status...
        public string PartyCaption { get; init; } = string.Empty;   // "Supplier", "Client"
        public string PartyName { get; init; } = string.Empty;
        public List<string> PartyDetails { get; init; } = new();    // phone, address, VAT no.
        public string? CurrencyText { get; init; }                  // "Amounts in USD · 1 USD = 89,000 LBP"
        public List<A4Column> Columns { get; init; } = new();
        public List<string[]> Rows { get; init; } = new();
        public List<A4Total> Totals { get; init; } = new();         // under the rows, on the right
        public string? Notes { get; init; }
        public string? LeftSignature { get; init; }                 // "Received by"; null = no lines to sign
        public string? RightSignature { get; init; }
    }

    /// <summary>A caption and value under the title; Warning = in red (e.g. DRAFT).</summary>
    public sealed record A4Info(string Caption, string? Value, bool Warning = false);

    /// <summary>A column of the table: its share of the page width; Right = numbers (right aligned).</summary>
    public sealed record A4Column(string Header, float Share, bool Right = false);

    /// <summary>A line under the table: Big = the main total, LineAbove = a line drawn above it.</summary>
    public sealed record A4Total(string Caption, string Value, bool Big = false, bool LineAbove = false);

    // Draws an A4Content on A4 portrait:
    //   - the company on the left (logo, name, address, phone, email, VAT no.), the title and its details on the right;
    //   - the other party (supplier / client) and the currency;
    //   - the table, over as many pages as needed, its column names on every page; Arabic text right-to-left;
    //   - after the last row: the totals, the notes and the lines to sign (on a new page when they do not fit);
    //   - the number, when it was printed and the page in the footer of every page.
    // Show(): choose the printer, look at the preview, then print from there.
    public static class A4Document
    {
        private static readonly Color HeaderBack = Color.FromArgb(232, 240, 254);
        private static readonly Color Line = Color.FromArgb(200, 205, 212);
        private static readonly Color Red = Color.FromArgb(192, 57, 43);
        private static readonly Regex Arabic = new("[؀-ۿݐ-ݿࢠ-ࣿﭐ-﷿ﹰ-﻿]", RegexOptions.Compiled);

        /// <summary>Choose the printer, then the preview (print from there).</summary>
        public static void Show(IWin32Window owner, A4Content content, CompanyModel company, byte[]? logo)
        {
            using var document = Create(content, company, logo);
            using (var choose = new PrintDialog { Document = document, UseEXDialog = true, AllowSomePages = false })
            {
                if (choose.ShowDialog(owner) != DialogResult.OK) return;
            }
            document.DefaultPageSettings.Landscape = false;   // the chosen printer starts from its own settings

            using var preview = new PrintPreviewDialog
            {
                Document = document,
                Text = $"Print - {document.DocumentName}",
                StartPosition = FormStartPosition.CenterParent,
                ShowIcon = false,
                ClientSize = new Size(900, 900),
            };
            preview.PrintPreviewControl.Zoom = 0.9;
            preview.ShowDialog(owner);
        }

        /// <summary>The print job (A4 portrait); the caller disposes it.</summary>
        public static PrintDocument Create(A4Content content, CompanyModel company, byte[]? logoBytes)
        {
            var document = new PrintDocument { DocumentName = $"{content.Title} {content.Number}" };
            document.DefaultPageSettings.Landscape = false;
            document.DefaultPageSettings.Margins = new Margins(50, 50, 45, 45);

            var logo = CompanyLogo.ToImage(logoBytes);
            document.Disposed += (_, _) => logo?.Dispose();
            var next = 0;            // the next row to print
            var page = 0;
            var totalsDone = false;
            var printedAt = DateTime.Now;
            document.BeginPrint += (_, _) => { next = 0; page = 0; totalsDone = false; };
            document.PrintPage += (_, e) =>
            {
                var g = e.Graphics!;
                var area = e.MarginBounds;
                page++;

                using var companyFont = new Font("Segoe UI", 15F, FontStyle.Bold);
                using var titleFont = new Font("Segoe UI", 16F, FontStyle.Bold);
                using var font = new Font("Segoe UI", 9F);
                using var boldFont = new Font("Segoe UI", 9F, FontStyle.Bold);
                using var smallFont = new Font("Segoe UI", 8F);
                using var bigFont = new Font("Segoe UI", 12F, FontStyle.Bold);
                using var linePen = new Pen(Line);
                using var darkPen = new Pen(Color.Black);
                using var headerBrush = new SolidBrush(HeaderBack);
                using var redBrush = new SolidBrush(Red);
                using var near = new StringFormat { Alignment = StringAlignment.Near };
                using var far = new StringFormat { Alignment = StringAlignment.Far };
                using var rtl = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Near };
                float y = area.Top;

                float Text(string text, Font f, Brush brush, float x, float top, float width, StringFormat format)
                {
                    var size = g.MeasureString(text, f, (int)width, format);
                    g.DrawString(text, f, brush, new RectangleF(x, top, width, size.Height), format);
                    return size.Height;
                }

                if (page == 1)
                {
                    // Company on the left (logo, then its details), the document on the right
                    var left = (float)area.Left;
                    var leftWidth = area.Width * 0.55f;
                    var top = y;
                    if (logo != null)
                    {
                        var scale = Math.Min(160f / logo.Width, 70f / logo.Height);
                        g.DrawImage(logo, left, top, logo.Width * scale, logo.Height * scale);
                        top += logo.Height * scale + 6;
                    }
                    top += Text(company.Name, companyFont, Brushes.Black, left, top, leftWidth, near);
                    foreach (var detail in new[]
                             {
                                 company.Address,
                                 string.Join("   ", new[] { company.Mobile is { Length: > 0 } m ? $"Tel {m}" : null, company.Email }.Where(s => !string.IsNullOrWhiteSpace(s))),
                                 company.VatNumber is { Length: > 0 } vat ? $"VAT no. {vat}" : null,
                             }.Where(s => !string.IsNullOrWhiteSpace(s)))
                        top += Text(detail!, font, Brushes.DimGray, left, top, leftWidth, near);

                    var right = area.Left + area.Width * 0.55f;
                    var rightWidth = area.Width * 0.45f;
                    var rtop = y;
                    rtop += Text(content.Title, titleFont, content.TitleInRed ? redBrush : Brushes.Black, right, rtop, rightWidth, far) + 4;
                    foreach (var info in content.Info.Where(i => !string.IsNullOrWhiteSpace(i.Value)))
                    {
                        Text(info.Caption, font, Brushes.DimGray, right, rtop, rightWidth * 0.45f, near);
                        rtop += Text(info.Value!, boldFont, info.Warning ? redBrush : Brushes.Black, right + rightWidth * 0.45f, rtop, rightWidth * 0.55f, far);
                    }
                    y = Math.Max(top, rtop) + 12;
                    g.DrawLine(darkPen, area.Left, y, area.Right, y);
                    y += 8;

                    // The other party and the currency
                    Text(content.PartyCaption, font, Brushes.DimGray, area.Left, y, 90, near);
                    y += Text(content.PartyName, bigFont, Brushes.Black, area.Left + 90, y, area.Width - 90, near);
                    foreach (var detail in content.PartyDetails.Where(s => !string.IsNullOrWhiteSpace(s)))
                        y += Text(detail, font, Brushes.Black, area.Left + 90, y, area.Width - 90, near);
                    if (content.CurrencyText != null)
                        y += 4 + Text(content.CurrencyText, font, Brushes.DimGray, area.Left + 90, y + 4, area.Width - 90, near);
                    y += 10;
                }

                // The table
                var widths = content.Columns.Select(c => c.Share * area.Width).ToArray();
                const float pad = 4f;

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
                        var format = content.Columns[c].Right ? far : Arabic.IsMatch(cells[c]) ? rtl : near;
                        g.DrawString(cells[c], f, Brushes.Black, new RectangleF(x + pad, y + pad, widths[c] - pad * 2, height - pad), format);
                        x += widths[c];
                    }
                }

                var footerTop = area.Bottom - smallFont.GetHeight(g) - 4;
                if (next < content.Rows.Count)
                {
                    var headers = content.Columns.Select(c => c.Header).ToArray();
                    var headerHeight = RowHeight(headers, boldFont);
                    g.FillRectangle(headerBrush, area.Left, y, area.Width, headerHeight);
                    DrawRow(headers, boldFont, headerHeight);
                    y += headerHeight;
                    g.DrawLine(darkPen, area.Left, y, area.Right, y);
                }

                var first = true;
                while (next < content.Rows.Count)
                {
                    var cells = content.Rows[next];
                    var height = RowHeight(cells, font);
                    if (!first && y + height > footerTop) break;
                    DrawRow(cells, font, height);
                    y += height;
                    g.DrawLine(linePen, area.Left, y, area.Right, y);
                    next++;
                    first = false;
                }

                // After the last row: totals, notes, signatures (on the next page when they do not fit)
                if (next >= content.Rows.Count && !totalsDone)
                {
                    var rowHeight = boldFont.GetHeight(g) + 4;
                    var notesHeight = string.IsNullOrWhiteSpace(content.Notes) ? 0 : g.MeasureString(content.Notes, font, area.Width).Height + rowHeight;
                    var needed = rowHeight * (content.Totals.Count + 1) + notesHeight + 70;
                    if (y + needed > footerTop && !first)
                    {
                        Footer();
                        e.HasMorePages = true;
                        return;
                    }

                    y += 8;
                    var labelX = area.Left + area.Width * 0.55f;
                    var labelWidth = area.Width * 0.25f;
                    var valueX = labelX + labelWidth;
                    var valueWidth = area.Width * 0.20f;
                    foreach (var total in content.Totals)
                    {
                        if (total.LineAbove)
                        {
                            g.DrawLine(darkPen, labelX, y, area.Right, y);
                            y += 2;
                        }
                        var f = total.Big ? bigFont : font;
                        var height = f.GetHeight(g) + 4;
                        g.DrawString(total.Caption, f, Brushes.Black, new RectangleF(labelX, y, labelWidth, height), near);
                        g.DrawString(total.Value, f, Brushes.Black, new RectangleF(valueX, y, valueWidth, height), far);
                        y += height;
                    }

                    if (!string.IsNullOrWhiteSpace(content.Notes))
                    {
                        y += 6;
                        g.DrawString("Notes", boldFont, Brushes.Black, area.Left, y);
                        y += rowHeight;
                        y += Text(content.Notes, font, Brushes.Black, area.Left, y, area.Width, Arabic.IsMatch(content.Notes) ? rtl : near);
                    }

                    if (content.LeftSignature != null || content.RightSignature != null)
                    {
                        y = Math.Max(y + 40, footerTop - 50);
                        var signWidth = area.Width * 0.35f;
                        if (content.LeftSignature != null)
                        {
                            g.DrawLine(darkPen, area.Left, y, area.Left + signWidth, y);
                            g.DrawString(content.LeftSignature, smallFont, Brushes.DimGray, area.Left, y + 2);
                        }
                        if (content.RightSignature != null)
                        {
                            g.DrawLine(darkPen, area.Right - signWidth, y, area.Right, y);
                            g.DrawString(content.RightSignature, smallFont, Brushes.DimGray, area.Right - signWidth, y + 2);
                        }
                    }
                    totalsDone = true;
                }

                Footer();
                e.HasMorePages = next < content.Rows.Count || !totalsDone;

                void Footer()
                {
                    g.DrawString($"{content.Number}   ·   printed {printedAt:dd/MM/yyyy HH:mm}", smallFont, Brushes.DimGray, area.Left, footerTop + 4);
                    g.DrawString($"Page {page}", smallFont, Brushes.DimGray,
                        new RectangleF(area.Left, footerTop + 4, area.Width, smallFont.GetHeight(g) + 2), far);
                }
            };
            return document;
        }
    }
}
