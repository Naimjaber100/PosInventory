using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.Json;

namespace Inventory
{
    // The banknotes shown under each cash box of the payment screen: "Banknotes" in appsettings.json, per currency code:
    //   "Banknotes": { "LBP": [ 10000, 20000, 50000, 100000 ], "USD": [ 10, 20, 50, 100 ] }
    // A picture of a note is used when there is one in the Banknotes folder next to the program, named
    // CODE_value.png (or .jpg), e.g. Banknotes\LBP_100000.png, Banknotes\USD_20.jpg; otherwise a note is drawn.
    public sealed class BanknoteSettings
    {
        public const string Section = "Banknotes";

        private static readonly Dictionary<string, decimal[]> Defaults = new(StringComparer.OrdinalIgnoreCase)
        {
            ["LBP"] = new decimal[] { 10000, 20000, 50000, 100000 },
            ["USD"] = new decimal[] { 10, 20, 50, 100 },
        };

        private readonly Dictionary<string, decimal[]> _notes;

        private BanknoteSettings(Dictionary<string, decimal[]> notes) => _notes = notes;

        public static BanknoteSettings Current { get; set; } = new(Defaults);

        /// <summary>The notes of a currency, smallest first (none = no note buttons for it).</summary>
        public IReadOnlyList<decimal> For(string currencyCode) =>
            _notes.TryGetValue(currencyCode, out var notes) ? notes : Array.Empty<decimal>();

        public static BanknoteSettings Read(JsonElement root)
        {
            if (!root.TryGetProperty(Section, out var section) || section.ValueKind != JsonValueKind.Object) return new BanknoteSettings(Defaults);
            var notes = new Dictionary<string, decimal[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var currency in section.EnumerateObject())
            {
                if (currency.Value.ValueKind != JsonValueKind.Array) continue;
                notes[currency.Name] = currency.Value.EnumerateArray()
                    .Where(v => v.ValueKind == JsonValueKind.Number && v.GetDecimal() > 0)
                    .Select(v => v.GetDecimal()).Distinct().Order().ToArray();
            }
            return new BanknoteSettings(notes);
        }

        // Banknotes\CODE_value.png / .jpg / .jpeg next to the program; null when there is none (or it cannot be read)
        internal static Image? LoadPicture(string currencyCode, decimal value)
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "Banknotes");
            var name = $"{currencyCode.ToUpperInvariant()}_{value.ToString("0.##", CultureInfo.InvariantCulture)}";
            foreach (var extension in new[] { ".png", ".jpg", ".jpeg" })
            {
                var path = Path.Combine(folder, name + extension);
                if (!File.Exists(path)) continue;
                try
                {
                    // Copied so the file is not kept locked while the screen is open
                    using var file = Image.FromFile(path);
                    return new Bitmap(file);
                }
                catch (Exception ex) when (ex is OutOfMemoryException or IOException or ArgumentException)
                {
                    return null;
                }
            }
            return null;
        }
    }

    // One banknote of the payment screen: its picture, or a drawn note with its value. Click = one more of this note,
    // right click = one less.
    public sealed class BanknoteButton : Control
    {
        private static readonly Color[] LbpColors =
        {
            Color.FromArgb(196, 120, 52), Color.FromArgb(200, 70, 60), Color.FromArgb(92, 84, 170), Color.FromArgb(46, 140, 90),
            Color.FromArgb(150, 110, 60), Color.FromArgb(60, 120, 160),
        };
        private static readonly Color UsdColor = Color.FromArgb(86, 128, 92);

        private readonly Image? _picture;
        private readonly Color _color;
        private bool _hover, _pressed;

        public decimal Value { get; }
        public string CurrencyCode { get; }

        public BanknoteButton(string currencyCode, decimal value, int index)
        {
            CurrencyCode = currencyCode;
            Value = value;
            _picture = BanknoteSettings.LoadPicture(currencyCode, value);
            _color = string.Equals(currencyCode, "USD", StringComparison.OrdinalIgnoreCase) ? UsdColor : LbpColors[index % LbpColors.Length];
            Size = new Size(100, 50);
            Margin = new Padding(0, 2, 6, 2);
            Cursor = Cursors.Hand;
            TabStop = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            AccessibleName = $"{ValueText} {currencyCode}";
        }

        private string ValueText => Value.ToString("#,0.##", CultureInfo.CurrentCulture);

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = _pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var offset = _pressed ? 2 : 0;
            var note = new Rectangle(1 + offset, 1 + offset, Width - 4, Height - 4);
            using var shape = Rounded(note, 6);

            if (_picture != null)
            {
                g.SetClip(shape);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_picture, note);
                g.ResetClip();
            }
            else
            {
                // A drawn note: the colour of the note, a lighter oval in the middle, the value and the currency
                using var fill = new LinearGradientBrush(note, ControlPaint.Light(_color, 0.35f), _color, LinearGradientMode.ForwardDiagonal);
                g.FillPath(fill, shape);
                var inner = Rectangle.Inflate(note, -4, -4);
                using (var frame = new Pen(Color.FromArgb(110, Color.White), 1f)) g.DrawRectangle(frame, inner);
                var oval = new Rectangle(note.X + note.Width / 2 - 20, note.Y + 8, 40, note.Height - 16);
                using (var light = new SolidBrush(Color.FromArgb(55, Color.White))) g.FillEllipse(light, oval);

                using var codeFont = new Font("Segoe UI", 7F, FontStyle.Bold);
                TextRenderer.DrawText(g, CurrencyCode, codeFont, new Rectangle(inner.X + 2, inner.Y + 1, 40, 12), Color.FromArgb(230, Color.White),
                    TextFormatFlags.Left | TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, ValueText, Font, new Rectangle(inner.X, inner.Y + 8, inner.Width, inner.Height - 8), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            using var border = new Pen(_hover ? AppTheme.Accent : Color.FromArgb(120, 0, 0, 0), _hover ? 2.5f : 1f);
            g.DrawPath(border, shape);
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            var d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _picture?.Dispose();
            base.Dispose(disposing);
        }
    }
}
