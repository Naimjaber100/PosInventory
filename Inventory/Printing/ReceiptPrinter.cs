using System.Drawing.Printing;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory.Printing
{
    // Prints on this computer's receipt printer (Settings > Receipt printer): receipts, and the short reports
    // for the roll (SlipRenderer).
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
                if (WhyNot(settings) is { } problem) return problem;

                var receipt = await new SaleRepository().GetReceiptAsync(saleId);
                if (receipt == null) return "This invoice was not found.";
                return await PrintAsync(receipt, currencies, settings);
            }
            catch (Exception ex)
            {
                return "The receipt was not printed: " + ex.Message;
            }
        }

        /// <summary>Why nothing can be printed on this computer (no printer set, not installed), or null.</summary>
        public static string? WhyNot(ReceiptSettings settings) =>
            !settings.Enabled ? $"Receipt printing is off on this station (\"Enabled\": false in \"{ReceiptSettings.Section}\", {ReceiptSettings.ConfigFile})."
            : !settings.HasPrinter ? "No receipt printer is set (Settings > Receipt printer)."
            : !settings.PrinterInstalled ? $"The receipt printer \"{settings.PrinterName}\" is not installed on this computer."
            : null;

        /// <summary>Sends a receipt to the printer in the background. Returns null when done, otherwise the error.</summary>
        public static Task<string?> PrintAsync(Receipt receipt, CurrencySet currencies, ReceiptSettings settings) =>
            PrintAsync(() => new ReceiptRenderer(receipt, currencies, settings), settings);

        /// <summary>Sends what the renderer draws (made in the background) to the printer. Returns null when done, otherwise the error.</summary>
        public static Task<string?> PrintAsync(Func<RollRenderer> create, ReceiptSettings settings) =>
            Task.Run(() =>
            {
                try
                {
                    using var document = CreateDocument(create(), settings);
                    document.PrintController = new StandardPrintController();   // no "Printing page 1..." window
                    document.Print();
                    return (string?)null;
                }
                catch (Exception ex)
                {
                    return "Not printed: " + ex.Message;
                }
            });

        /// <summary>The print job for one receipt or slip; it disposes the renderer. The page is as long as what is drawn.</summary>
        public static PrintDocument CreateDocument(RollRenderer renderer, ReceiptSettings settings)
        {
            var height = renderer.Render(null);

            var document = new PrintDocument { DocumentName = renderer.DocumentName };
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
            ShowPreview(owner, renderer, $"Receipt preview - {receipt.InvoiceNo}  ({settings.PaperWidthMm} mm)");
        }

        /// <summary>
        /// Shows what the renderer draws as it will print. print: a Print button that sends it to the receipt printer
        /// (it gets a new renderer, made in the background).
        /// </summary>
        public static void ShowPreview(IWin32Window owner, RollRenderer renderer, string title,
            Func<RollRenderer>? print = null, ReceiptSettings? settings = null)
        {
            using var picture = renderer.ToBitmap(110);
            using var form = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(picture.Width + 60, Math.Min(picture.Height + 40 + (print != null ? 56 : 0), 820)),
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.FromArgb(225, 228, 232),
                Font = new Font("Segoe UI", 10F),
            };
            var paper = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            paper.Controls.Add(new PictureBox
            {
                Image = picture,
                SizeMode = PictureBoxSizeMode.AutoSize,
                Location = new Point(30, 20),
                BorderStyle = BorderStyle.FixedSingle,
            });
            form.Controls.Add(paper);

            if (print != null && settings != null)
            {
                var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8), BackColor = Color.White };
                var close = new Button { Text = "Close", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.White };
                close.FlatAppearance.BorderColor = Color.Silver;
                close.Click += (_, _) => form.Close();
                var send = new Button
                {
                    Text = $"Print on {(settings.HasPrinter ? settings.PrinterName : "the receipt printer")}",
                    AutoSize = true, MinimumSize = new Size(160, 38), FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(34, 139, 117), ForeColor = Color.White,
                };
                send.FlatAppearance.BorderSize = 0;
                send.Click += async (_, _) =>
                {
                    if (WhyNot(settings) is { } why)
                    {
                        MessageBox.Show(form, why, form.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    send.Enabled = false;
                    var problem = await PrintAsync(print, settings);
                    if (form.IsDisposed) return;
                    send.Enabled = true;
                    if (problem != null) MessageBox.Show(form, problem, form.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else form.Close();
                };
                bar.Controls.Add(close);
                bar.Controls.Add(send);
                form.Controls.Add(bar);
                form.AcceptButton = send;
            }

            form.KeyPreview = true;
            form.KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) form.Close(); };
            form.ShowDialog(owner);
        }
    }
}
