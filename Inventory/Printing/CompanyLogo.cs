using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Inventory.Printing
{
    // The company logo (Settings > Company) for the top of the receipt.
    // A picture chosen by the user (PNG, JPG, BMP, GIF, of any size) is made ready for a receipt printer once, before
    // it is saved: at most 576 x 240 dots (the width of an 80 mm roll at 203 dpi), transparent parts white, grey.
    // Saved as PNG in company.logo: a few KB, so it costs nothing to load.
    public static class CompanyLogo
    {
        private const int MaxWidth = 576;
        private const int MaxHeight = 240;

        /// <summary>The picture file made ready for the receipt, as PNG bytes. Throws when it is not a picture.</summary>
        public static byte[] Prepare(string path)
        {
            using var original = LoadUnlocked(path);
            var scale = Math.Min(1.0, Math.Min(MaxWidth / (double)original.Width, MaxHeight / (double)original.Height));
            var width = Math.Max(1, (int)Math.Round(original.Width * scale));
            var height = Math.Max(1, (int)Math.Round(original.Height * scale));

            using var ready = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(ready))
            {
                g.Clear(Color.White);   // transparent = white paper
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                // Grey: a thermal printer prints black dots only, colours come out better as greys
                using var grey = new ImageAttributes();
                grey.SetColorMatrix(new ColorMatrix(new[]
                {
                    new[] { 0.299f, 0.299f, 0.299f, 0f, 0f },
                    new[] { 0.587f, 0.587f, 0.587f, 0f, 0f },
                    new[] { 0.114f, 0.114f, 0.114f, 0f, 0f },
                    new[] { 0f, 0f, 0f, 1f, 0f },
                    new[] { 0f, 0f, 0f, 0f, 1f },
                }));
                g.DrawImage(original, new Rectangle(0, 0, width, height), 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, grey);
            }

            using var stream = new MemoryStream();
            ready.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>The saved logo as a picture (the caller disposes it), or null when there is none or it cannot be read.</summary>
        public static Bitmap? ToImage(byte[]? logo)
        {
            if (logo is not { Length: > 0 }) return null;
            try
            {
                using var stream = new MemoryStream(logo);
                using var image = Image.FromStream(stream);
                return new Bitmap(image);   // a copy that does not need the stream
            }
            catch
            {
                return null;   // a damaged logo: the receipt prints without it
            }
        }

        // Image.FromFile keeps the file locked until the image is disposed; a copy does not
        private static Bitmap LoadUnlocked(string path)
        {
            using var stream = File.OpenRead(path);
            using var image = Image.FromStream(stream);
            return new Bitmap(image);
        }
    }
}
