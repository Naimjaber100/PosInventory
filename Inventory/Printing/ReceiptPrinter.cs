using System.Drawing.Printing;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory.Printing
{
    // Prints receipts on this computer's receipt printer (Settings > Receipt printer).
    // Fast: one database call for the receipt, then the print job is sent in the background with no
    // "Printing..." window, so the cashier goes on with the next customer at once.
    // No printer set or not installed: nothing is printed and the sale is not affected.
    public static class ReceiptPrinter
    {
        /// <summary>
        /// Prints the receipt of a saved sale or return. Returns null when it was sent to the printer,
        /// otherwise why it was not (for the status line).
        /// </summary>
        public static async Task<string?> PrintSaleAsync(int saleId, CurrencySet currencies, ReceiptSettings? settings = null)
        {
            try
            {
                settings ??= ReceiptSettings.Current ?? await ReceiptSettings.LoadAsync();
                if (!settings.HasPrinter) return "No receipt printer is set (Settings > Receipt printer).";
                if (!settings.PrinterInstalled) return $"The receipt printer \"{settings.PrinterName}\" is not installed on this computer.";

                var receipt = await new SaleRepository().GetReceiptAsync(saleId);
                if (receipt == null) return "This invoice was not found.";
                return await PrintAsync(receipt, currencies, settings);
            }
            catch (Exception ex)
            {
                return "The receipt was not printed: " + ex.Message;
            }
        }

        /// <summary>Sends a receipt to the printer in the background. Returns null when done, otherwise the error.</summary>
        public static Task<string?> PrintAsync(Receipt receipt, CurrencySet currencies, ReceiptSettings settings) =>
            Task.Run(() =>
            {
                try
                {
                    using var document = CreateDocument(receipt, currencies, settings);
                    document.PrintController = new StandardPrintController();   // no "Printing page 1..." window
                    document.Print();
                    return (string?)null;
                }
                catch (Exception ex)
                {
                    return "The receipt was not printed: " + ex.Message;
                }
            });

        /// <summary>The print job for one receipt (also used by the preview). The page is as long as the receipt.</summary>
        public static PrintDocument CreateDocument(Receipt receipt, CurrencySet currencies, ReceiptSettings settings)
        {
            var renderer = new ReceiptRenderer(receipt, currencies, settings);
            var height = renderer.Render(null);

            var document = new PrintDocument { DocumentName = $"Receipt {receipt.InvoiceNo}" };
            if (settings.PrinterInstalled) document.PrinterSettings.PrinterName = settings.PrinterName;
            document.PrinterSettings.Copies = (short)Math.Clamp(settings.Copies, 1, 5);
            // A roll: paper as wide as the roll and as long as this receipt (1/100 inch)
            var paperWidth = (int)Math.Ceiling(settings.PaperWidthMm / 25.4 * 100);
            document.DefaultPageSettings.PaperSize = new PaperSize("Receipt", paperWidth, (int)Math.Ceiling(height) + 40);
            document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
            document.OriginAtMargins = false;

            document.PrintPage += (_, e) =>
            {
                if (e.Graphics == null) return;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                // Centre the printable width on the roll; the printer's own margin is already outside the drawing area
                var offset = Math.Max(0, (e.PageBounds.Width - renderer.Width) / 2F - e.PageSettings.HardMarginX);
                e.Graphics.TranslateTransform(offset, 4);
                renderer.Render(e.Graphics);
                e.HasMorePages = false;
            };
            document.Disposed += (_, _) => renderer.Dispose();
            return document;
        }

        /// <summary>Shows the receipt on screen as it will print (works without a printer).</summary>
        public static void ShowPreview(IWin32Window owner, Receipt receipt, CurrencySet currencies, ReceiptSettings settings)
        {
            using var renderer = new ReceiptRenderer(receipt, currencies, settings);
            using var picture = renderer.ToBitmap(110);
            using var form = new Form
            {
                Text = $"Receipt preview - {receipt.InvoiceNo}  ({settings.PaperWidthMm} mm)",
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(picture.Width + 60, Math.Min(picture.Height + 40, 820)),
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.FromArgb(225, 228, 232),
                AutoScroll = true,
            };
            form.Controls.Add(new PictureBox
            {
                Image = picture,
                SizeMode = PictureBoxSizeMode.AutoSize,
                Location = new Point(30, 20),
                BorderStyle = BorderStyle.FixedSingle,
            });
            form.KeyPreview = true;
            form.KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) form.Close(); };
            form.ShowDialog(owner);
        }
    }
}
