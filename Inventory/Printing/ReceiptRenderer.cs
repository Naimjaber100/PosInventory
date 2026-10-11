using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using Inventory.Models;

namespace Inventory.Printing
{
    // Draws a receipt for an 80 mm (or 58 mm) roll. Units are 1/100 inch (the printer's default), so the same
    // drawing works on the printer, in the preview and on a picture. Windows fonts, so Arabic prints correctly.
    public sealed class ReceiptRenderer : IDisposable
    {
        private readonly Receipt _r;
        private readonly CurrencySet _currencies;
        private readonly string _footer;
        private readonly float _width;   // printable width, 1/100 inch

        private readonly Font _small = new("Arial", 7.5F);
        private readonly Font _normal = new("Arial", 8.5F);
        private readonly Font _bold = new("Arial", 8.5F, FontStyle.Bold);
        private readonly Font _big = new("Arial", 12F, FontStyle.Bold);
        private readonly Font _title = new("Arial", 13F, FontStyle.Bold);

        public ReceiptRenderer(Receipt receipt, CurrencySet currencies, ReceiptSettings settings)
        {
            _r = receipt;
            _currencies = currencies;
            _footer = settings.Footer;
            // The printer cannot print to the paper edge: about 4 mm each side
            _width = (settings.PaperWidthMm - 8) / 25.4F * 100F;
        }

        public float Width => _width;

        /// <summary>Draws the receipt (g null = only measure). Returns the height used, in 1/100 inch.</summary>
        public float Render(Graphics? g)
        {
            var y = 0F;
            var c = _r.Company;

            // Shop
            y = Text(g, c.Name ?? string.Empty, _title, y, Align.Center);
            if (!string.IsNullOrWhiteSpace(c.Address)) y = Text(g, c.Address, _small, y, Align.Center);
            var contact = string.Join("   ", new[] { c.Mobile is { Length: > 0 } m ? $"Tel {m}" : null, c.Email }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (contact.Length > 0) y = Text(g, contact, _small, y, Align.Center);
            if (!string.IsNullOrWhiteSpace(c.VatNumber)) y = Text(g, $"VAT no. {c.VatNumber}", _small, y, Align.Center);
            y = Separator(g, y + 4);

            // Invoice
            y = Text(g, _r.IsReturn ? "RETURN / REFUND" : "INVOICE", _bold, y, Align.Center);
            y = TwoColumns(g, _r.InvoiceNo, _r.InvoiceDate.ToLocalTime().ToString("dd/MM/yyyy  HH:mm"), _normal, y);
            if (!string.IsNullOrWhiteSpace(_r.Cashier)) y = Text(g, $"Cashier: {_r.Cashier}", _small, y, Align.Left);
            if (!string.IsNullOrWhiteSpace(_r.Customer)) y = Text(g, $"Client: {_r.Customer}", _small, y, Align.Left);
            y = Separator(g, y + 2);

            // Items: name on its own line(s), then "qty x price" and the line total
            foreach (var line in _r.Lines)
            {
                y = Text(g, line.ItemName, _normal, y, Align.Auto);
                var qty = line.Quantity == Math.Round(line.Quantity) ? line.Quantity.ToString("#,0") : line.Quantity.ToString("#,0.###");
                var left = $"   {qty} x {Number(line.UnitPrice)}" + (line.Discount > 0 ? $"   (-{Number(line.Discount)})" : string.Empty);
                y = TwoColumns(g, left, Number(line.LineTotal), _normal, y) + 2;
            }
            y = Separator(g, y);

            // Totals
            var discounts = _r.LineDiscounts + _r.Discount;
            if (discounts > 0)
            {
                y = TwoColumns(g, "Sub total", Number(_r.SubTotal + _r.LineDiscounts), _normal, y);
                y = TwoColumns(g, "Discount", "-" + Number(discounts), _normal, y);
            }
            y = TwoColumns(g, _r.IsReturn ? "REFUND" : "TOTAL", _currencies.Primary.Format(_r.Total), _big, y + 2);
            if (_r.UsdRate > 1 && !string.Equals(_r.PrimaryCode, "USD", StringComparison.OrdinalIgnoreCase))
                y = TwoColumns(g, $"In USD (1 $ = {Number(_r.UsdRate)})", $"$ {_r.Total / _r.UsdRate:#,0.00}", _small, y);

            // Payments and change
            if (_r.Payments.Count > 0)
            {
                y += 4;
                foreach (var p in _r.Payments)
                {
                    var how = p.Method switch { 2 => "Card", 3 => "On account", _ => _r.IsReturn ? "Cash back" : "Cash" };
                    var amount = _currencies.Find(p.Currency) is { } cur ? cur.Format(p.Amount) : $"{p.Currency} {p.Amount:#,0.##}";
                    y = TwoColumns(g, $"{how} {p.Currency}", amount, _normal, y);
                }
                if (_r.ChangeLbp > 0) y = TwoColumns(g, "Change", _currencies.Primary.Format(_r.ChangeLbp), _bold, y);
            }

            // Footer
            y = Separator(g, y + 4);
            if (!string.IsNullOrWhiteSpace(_footer)) y = Text(g, _footer, _normal, y, Align.Center);
            y = Text(g, $"{_r.Lines.Count} item line(s)", _small, y + 2, Align.Center);
            return y + 20;   // a little paper after the text, before the cut
        }

        private string Number(decimal amount) =>
            Math.Round(amount, _currencies.Primary.Decimals).ToString(_currencies.Primary.Decimals == 0 ? "#,0" : "#,0." + new string('0', _currencies.Primary.Decimals));

        private enum Align { Left, Center, Right, Auto }

        private static readonly Regex RightToLeft = new("[֐-ࣿיִ-﷿ﹰ-﻿]", RegexOptions.Compiled);

        // Wrapped text over the full width; Auto = right-aligned and right-to-left for Arabic, else left
        private float Text(Graphics? g, string text, Font font, float y, Align align)
        {
            using var format = new StringFormat();
            var rtl = align == Align.Auto && RightToLeft.IsMatch(text);
            if (rtl) format.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
            format.Alignment = align switch
            {
                Align.Center => StringAlignment.Center,
                Align.Right => StringAlignment.Far,
                _ => StringAlignment.Near,   // with right-to-left, Near is the right side
            };
            var size = Measure(g, text, font, _width, format);
            g?.DrawString(text, font, Brushes.Black, new RectangleF(0, y, _width, size.Height), format);
            return y + size.Height;
        }

        // Left text and right text on one line (the left one is cut with "..." when both do not fit)
        private float TwoColumns(Graphics? g, string left, string right, Font font, float y)
        {
            var rightSize = Measure(g, right, font, _width, StringFormat.GenericTypographic);
            var height = Math.Max(rightSize.Height, font.GetHeight() * 1.0F) + 1;
            if (g != null)
            {
                using var leftFormat = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                using var rightFormat = new StringFormat { Alignment = StringAlignment.Far, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(left, font, Brushes.Black, new RectangleF(0, y, _width - rightSize.Width - 6, height), leftFormat);
                g.DrawString(right, font, Brushes.Black, new RectangleF(0, y, _width, height), rightFormat);
            }
            return y + height;
        }

        private float Separator(Graphics? g, float y)
        {
            if (g != null)
            {
                using var pen = new Pen(Color.Black, 1F) { DashStyle = DashStyle.Dash };
                g.DrawLine(pen, 0, y + 3, _width, y + 3);
            }
            return y + 8;
        }

        private static readonly Bitmap MeasureBitmap = new(1, 1);

        private static SizeF Measure(Graphics? g, string text, Font font, float width, StringFormat format)
        {
            if (g != null) return g.MeasureString(text, font, (int)width, format);
            lock (MeasureBitmap)
            {
                using var mg = Graphics.FromImage(MeasureBitmap);
                UseHundredthsOfInch(mg);
                return mg.MeasureString(text, font, (int)width, format);
            }
        }

        /// <summary>Screen / picture graphics in the printer's units (1/100 inch) instead of pixels.</summary>
        public static void UseHundredthsOfInch(Graphics g)
        {
            g.PageUnit = GraphicsUnit.Inch;
            g.PageScale = 0.01F;
        }

        /// <summary>The receipt as a picture at the printer's resolution (203 dpi): preview and checks without a printer.</summary>
        public Bitmap ToBitmap(int dpi = 203)
        {
            var height = Render(null);
            var bitmap = new Bitmap((int)(_width / 100F * dpi) + 1, (int)(height / 100F * dpi) + 1);
            bitmap.SetResolution(dpi, dpi);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            UseHundredthsOfInch(g);
            Render(g);
            return bitmap;
        }

        public void Dispose()
        {
            _small.Dispose();
            _normal.Dispose();
            _bold.Dispose();
            _big.Dispose();
            _title.Dispose();
        }
    }
}
