using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using Inventory.Models;

namespace Inventory.Printing
{
    // What is printed on the 80 mm (or 58 mm) receipt roll: the receipt (ReceiptRenderer) and the short reports
    // (SlipRenderer). Units are 1/100 inch (the printer's default), so the same drawing works on the printer, in the
    // preview and on a picture. Windows fonts, so Arabic prints correctly. Render(null) only measures the height.
    public abstract class RollRenderer : IDisposable
    {
        protected readonly CurrencySet Currencies;
        private readonly float _width;   // printable width, 1/100 inch

        protected readonly Font Small = new("Arial", 7.5F);
        protected readonly Font Normal = new("Arial", 8.5F);
        protected readonly Font Bold = new("Arial", 8.5F, FontStyle.Bold);
        protected readonly Font Big = new("Arial", 12F, FontStyle.Bold);
        protected readonly Font Title = new("Arial", 13F, FontStyle.Bold);

        protected RollRenderer(CurrencySet currencies, int paperWidthMm)
        {
            Currencies = currencies;
            // What the head prints: 72 mm of 80 mm paper (576 dots at 203 dpi), 48 mm of 58 mm paper (384 dots)
            _width = (paperWidthMm == 58 ? 48 : 72) / 25.4F * 100F;
        }

        public float Width => _width;

        /// <summary>A name for the print job (shown in the printer queue).</summary>
        public abstract string DocumentName { get; }

        /// <summary>Draws it (g null = only measure). Returns the height used, in 1/100 inch.</summary>
        public abstract float Render(Graphics? g);

        /// <summary>An amount in the primary currency, with its decimals ("1,250,000").</summary>
        protected string Number(decimal amount) =>
            Math.Round(amount, Currencies.Primary.Decimals).ToString(Currencies.Primary.Decimals == 0 ? "#,0" : "#,0." + new string('0', Currencies.Primary.Decimals));

        protected enum Align { Left, Center, Right, Auto }

        private static readonly Regex RightToLeft = new("[֐-ࣿיִ-﷿ﹰ-﻿]", RegexOptions.Compiled);

        // Wrapped text over the full width; Auto = right-aligned and right-to-left for Arabic, else left
        protected float Text(Graphics? g, string text, Font font, float y, Align align)
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
        protected float TwoColumns(Graphics? g, string left, string right, Font font, float y)
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

        /// <summary>A column of a table row: its text, its share of the width (the shares add up to 1) and its alignment.</summary>
        protected readonly record struct Cell(string Text, float Share, Align Align);

        // One row of a table: each text wraps in its own column; the row is as tall as its tallest cell
        protected float Row(Graphics? g, Font font, float y, params Cell[] cells)
        {
            const float Gap = 4F;   // between two columns
            var height = font.GetHeight() + 1;
            var x = 0F;
            var boxes = new List<(Cell Cell, float X, float Width)>();
            foreach (var cell in cells)
            {
                var width = _width * cell.Share;
                boxes.Add((cell, x, width - Gap));
                x += width;
            }

            var formats = boxes.Select(b =>
            {
                var format = new StringFormat();
                if (b.Cell.Align == Align.Auto && RightToLeft.IsMatch(b.Cell.Text)) format.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
                format.Alignment = b.Cell.Align switch { Align.Center => StringAlignment.Center, Align.Right => StringAlignment.Far, _ => StringAlignment.Near };
                return format;
            }).ToList();
            try
            {
                for (var i = 0; i < boxes.Count; i++)
                    height = Math.Max(height, Measure(g, boxes[i].Cell.Text, font, boxes[i].Width, formats[i]).Height);
                if (g != null)
                    for (var i = 0; i < boxes.Count; i++)
                        g.DrawString(boxes[i].Cell.Text, font, Brushes.Black, new RectangleF(boxes[i].X, y, boxes[i].Width, height), formats[i]);
            }
            finally
            {
                formats.ForEach(f => f.Dispose());
            }
            return y + height;
        }

        protected float Separator(Graphics? g, float y)
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

        /// <summary>It as a picture at the printer's resolution (203 dpi): preview and checks without a printer.</summary>
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

        public virtual void Dispose()
        {
            Small.Dispose();
            Normal.Dispose();
            Bold.Dispose();
            Big.Dispose();
            Title.Dispose();
        }
    }
}
