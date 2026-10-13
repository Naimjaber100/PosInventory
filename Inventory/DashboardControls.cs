using System.Drawing.Drawing2D;

namespace Inventory
{
    // The pieces of the back-office dashboard (Form1): white rounded cards, the total tiles and the bar chart.
    // Painted in code, so they stay sharp at any window size.

    /// <summary>A white panel with rounded corners on the grey dashboard background.</summary>
    public class CardPanel : Panel
    {
        public const int Radius = 12;

        public CardPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.White;
            Padding = new Padding(20, 16, 20, 16);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // The corners show what is behind the card
            e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRectangle(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), Radius);
            using var fill = new SolidBrush(BackColor);
            using var border = new Pen(Color.FromArgb(226, 230, 236));
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
        }

        public static GraphicsPath RoundedRectangle(RectangleF r, float radius)
        {
            var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            var path = new GraphicsPath();
            if (d <= 0) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    /// <summary>A total on the dashboard: round icon, caption, big value and a small line under it. Click opens its screen.</summary>
    public class StatCard : CardPanel
    {
        public const string IconFont = "Segoe MDL2 Assets";

        private static readonly Color Ink = Color.FromArgb(22, 32, 43);
        private string _value = "-", _note = string.Empty;
        private Color _noteColor = Color.DimGray;

        public StatCard(string caption, string glyph, Color iconColor)
        {
            Caption = caption;
            Glyph = glyph;
            IconColor = iconColor;
            Cursor = Cursors.Hand;
        }

        public string Caption { get; }
        public string Glyph { get; }
        public Color IconColor { get; }

        public void Show(string value, string note, Color noteColor)
        {
            _value = value;
            _note = note;
            _noteColor = noteColor;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var left = Padding.Left;
            var width = Width - Padding.Horizontal;

            using (var circle = new SolidBrush(IconColor))
                g.FillEllipse(circle, left, 16, 40, 40);
            using (var iconFont = new Font(IconFont, 13F))
                TextRenderer.DrawText(g, Glyph, iconFont, new Rectangle(left, 16, 40, 40), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
            using var captionFont = new Font("Segoe UI", 10.5F);
            using var valueFont = new Font("Segoe UI Semibold", Width < 250 ? 16F : 20F, FontStyle.Bold);
            using var noteFont = new Font("Segoe UI", 9.5F);
            TextRenderer.DrawText(g, Caption, captionFont, new Rectangle(left, 64, width, 22), Color.DimGray, flags);
            TextRenderer.DrawText(g, _value, valueFont, new Rectangle(left, 86, width, 40), Ink, flags | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, _note, noteFont, new Rectangle(left, 128, width, 20), _noteColor, flags);
        }
    }

    /// <summary>
    /// A bar chart of one series (e.g. the sales of each day): thin bars with rounded tops on a light grid,
    /// the value of the last bar written on it, the others on hover.
    /// </summary>
    public class BarChart : Control
    {
        public sealed record Bar(string Label, decimal Value, string Tip);

        private static readonly Color BarColor = Color.FromArgb(23, 133, 196);
        private static readonly Color HoverColor = Color.FromArgb(15, 98, 148);
        private static readonly Color Muted = Color.FromArgb(120, 128, 138);
        private static readonly Color Grid = Color.FromArgb(232, 235, 240);

        private readonly ToolTip _tip = new();
        private List<Bar> _bars = new();
        private List<RectangleF> _hits = new();
        private int _hover = -1;

        public BarChart()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.White;
        }

        public void Show(IEnumerable<Bar> bars)
        {
            _bars = bars.ToList();
            _hover = -1;
            Invalidate();
        }

        /// <summary>1,250,000 -> "1.3M", 45,000 -> "45K": short enough for the axis.</summary>
        public static string Short(decimal value) =>
            Math.Abs(value) >= 1_000_000_000 ? $"{value / 1_000_000_000:0.#}B"
            : Math.Abs(value) >= 1_000_000 ? $"{value / 1_000_000:0.#}M"
            : Math.Abs(value) >= 1_000 ? $"{value / 1_000:0.#}K"
            : $"{value:0.##}";

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            _hits = new List<RectangleF>();
            if (_bars.Count == 0 || Width < 80 || Height < 80) return;

            using var font = new Font("Segoe UI", 9F);
            using var labelFont = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            var top = Math.Max(Niceceiling(_bars.Max(b => b.Value)), 1);
            const int lines = 4;
            var axisWidth = TextRenderer.MeasureText(Short(top), font).Width + 8;
            var plot = new RectangleF(axisWidth, 22, Width - axisWidth - 6, Height - 22 - 28);

            // Light grid with the amounts on the left; the baseline a little darker
            using (var gridPen = new Pen(Grid))
            using (var basePen = new Pen(Color.FromArgb(200, 205, 212)))
                for (var i = 0; i <= lines; i++)
                {
                    var y = plot.Bottom - plot.Height * i / lines;
                    g.DrawLine(i == 0 ? basePen : gridPen, plot.Left, y, plot.Right, y);
                    TextRenderer.DrawText(g, Short(top * i / lines), font, new Rectangle(0, (int)y - 9, axisWidth - 6, 18), Muted,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }

            var slot = plot.Width / _bars.Count;
            var barWidth = Math.Min(44, slot * 0.55f);
            for (var i = 0; i < _bars.Count; i++)
            {
                var bar = _bars[i];
                var height = (float)(Math.Max(bar.Value, 0) / top) * plot.Height;
                var x = plot.Left + slot * i + (slot - barWidth) / 2;
                var rect = new RectangleF(x, plot.Bottom - height, barWidth, height);
                _hits.Add(new RectangleF(plot.Left + slot * i, plot.Top, slot, plot.Height));

                if (height >= 1)
                {
                    // Rounded at the top, square on the baseline
                    using var path = new GraphicsPath();
                    var r = Math.Min(4f, Math.Min(height, barWidth / 2));
                    path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
                    path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
                    path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom);
                    path.CloseFigure();
                    using var brush = new SolidBrush(i == _hover ? HoverColor : BarColor);
                    g.FillPath(brush, path);
                }

                TextRenderer.DrawText(g, bar.Label, font, new Rectangle((int)(plot.Left + slot * i), (int)plot.Bottom + 6, (int)slot, 18), Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                if (i == _bars.Count - 1 || i == _hover)
                    TextRenderer.DrawText(g, Short(bar.Value), labelFont, new Rectangle((int)(plot.Left + slot * i), (int)rect.Y - 20, (int)slot, 18),
                        Color.FromArgb(22, 32, 43), TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
            }
        }

        // The top of the axis: the next round number above the biggest bar (1, 2, 2.5, 5 or 10 x a power of ten)
        private static decimal Niceceiling(decimal max)
        {
            if (max <= 0) return 0;
            var power = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)max)));
            foreach (var step in new[] { 1m, 2m, 2.5m, 4m, 5m, 8m, 10m })
                if (max <= step * power) return step * power;
            return 10 * power;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var index = _hits.FindIndex(h => h.Contains(e.Location));
            if (index == _hover) return;
            _hover = index;
            if (index >= 0) _tip.Show(_bars[index].Tip, this, (int)_hits[index].Left + 10, Math.Max(0, e.Y - 40));
            else _tip.Hide(this);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = -1;
            _tip.Hide(this);
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _tip.Dispose();
            base.Dispose(disposing);
        }
    }
}
