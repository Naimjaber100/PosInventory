using System.Runtime.InteropServices;

namespace Inventory.Printing
{
    // Prints a receipt on a thermal printer (XP-80C, POS-80, Epson TM...) the way it is made to be used: ESC/POS commands
    // sent "raw" through the Windows print queue. The receipt is drawn as a black and white picture exactly as wide as the
    // head (576 dots on 80 mm paper, 384 on 58 mm, at 203 dpi), so the Windows driver's paper size cannot cut it or
    // wrap it; then the paper is fed and cut.
    public static class EscPosPrinter
    {
        private const int Dpi = 203;
        private const int BandRows = 128;   // the picture is sent in bands: some printers refuse one very tall picture

        public static void Print(RollRenderer renderer, string printerName, int copies)
        {
            using var bitmap = renderer.ToBitmap(Dpi);
            var job = new List<byte>();
            job.AddRange(new byte[] { 0x1B, 0x40 });   // ESC @: reset
            job.AddRange(new byte[] { 0x1B, 0x61, 0 }); // ESC a 0: left (the picture is already the full width)
            for (var copy = 0; copy < Math.Clamp(copies, 1, 5); copy++)
            {
                AddPicture(job, bitmap);
                job.AddRange(new byte[] { 0x1B, 0x64, 4 });       // ESC d 4: feed 4 lines, past the cutter
                job.AddRange(new byte[] { 0x1D, 0x56, 0x42, 0 }); // GS V B 0: (partial) cut
            }
            SendRaw(printerName, renderer.DocumentName, job.ToArray());
        }

        // GS v 0: raster picture, 1 bit per dot, 8 dots per byte, left dot = high bit; dark pixels print
        private static void AddPicture(List<byte> job, Bitmap bitmap)
        {
            var widthBytes = (bitmap.Width + 7) / 8;
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                var row = new int[bitmap.Width];
                for (var top = 0; top < bitmap.Height; top += BandRows)
                {
                    var rows = Math.Min(BandRows, bitmap.Height - top);
                    job.AddRange(new byte[] { 0x1D, 0x76, 0x30, 0, (byte)(widthBytes % 256), (byte)(widthBytes / 256), (byte)(rows % 256), (byte)(rows / 256) });
                    for (var y = top; y < top + rows; y++)
                    {
                        Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, bitmap.Width);
                        var bytes = new byte[widthBytes];
                        for (var x = 0; x < bitmap.Width; x++)
                        {
                            var c = row[x];
                            var light = ((c >> 16) & 0xFF) * 299 + ((c >> 8) & 0xFF) * 587 + (c & 0xFF) * 114;   // x 1000
                            if (light < 140_000) bytes[x / 8] |= (byte)(0x80 >> (x % 8));
                        }
                        job.AddRange(bytes);
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        #region Raw printing (winspool)

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class DocInfo
        {
            public string? DocName;
            public string? OutputFile;
            public string? DataType;
        }

        [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool OpenPrinter(string printerName, out IntPtr printer, IntPtr defaults);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr printer);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int StartDocPrinter(IntPtr printer, int level, [In] DocInfo docInfo);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr printer);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr printer);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr printer);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr printer, byte[] bytes, int count, out int written);

        private static void SendRaw(string printerName, string documentName, byte[] bytes)
        {
            if (!OpenPrinter(printerName, out var printer, IntPtr.Zero))
                throw new InvalidOperationException($"Could not open the printer \"{printerName}\" (Windows error {Marshal.GetLastWin32Error()}).");
            try
            {
                if (StartDocPrinter(printer, 1, new DocInfo { DocName = documentName, DataType = "RAW" }) == 0)
                    throw new InvalidOperationException($"The printer \"{printerName}\" did not accept the receipt (Windows error {Marshal.GetLastWin32Error()}).");
                try
                {
                    StartPagePrinter(printer);
                    if (!WritePrinter(printer, bytes, bytes.Length, out var written) || written != bytes.Length)
                        throw new InvalidOperationException($"The receipt was not sent completely to \"{printerName}\" (Windows error {Marshal.GetLastWin32Error()}).");
                    EndPagePrinter(printer);
                }
                finally
                {
                    EndDocPrinter(printer);
                }
            }
            finally
            {
                ClosePrinter(printer);
            }
        }

        #endregion
    }
}
