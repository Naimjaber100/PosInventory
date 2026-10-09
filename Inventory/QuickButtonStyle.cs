using System.Drawing.Drawing2D;
using System.Globalization;
using Inventory.Models;

namespace Inventory
{
    // One look for quick buttons and tabs: on the cashier screen and in the back office preview
    public static class QuickButtonStyle
    {
        public static readonly Color DefaultColor = Color.FromArgb(244, 246, 248);
        public static readonly Color Dark = Color.FromArgb(22, 32, 43);
        public static readonly Color Accent = Color.FromArgb(34, 139, 117);

        /// <summary>Settings key: item buttons per row on the cashier screen.</summary>
        public const string ColumnsSetting = "pos_quick_columns";
        public const int DefaultColumns = 3;
        public const int MinColumns = 2;
        public const int MaxColumns = 6;

        // Swatches offered in the back office
        public static readonly Color[] Palette =
        {
            DefaultColor,
            Color.FromArgb(58, 125, 44),     // green - salads, vegetables
            Color.FromArgb(139, 90, 20),     // brown - bakery
            Color.FromArgb(45, 106, 79),     // dark green
            Color.FromArgb(29, 78, 160),     // blue - drinks
            Color.FromArgb(122, 46, 94),     // plum - offers
            Color.FromArgb(192, 57, 43),     // red - meat
            Color.FromArgb(230, 126, 34),    // orange
            Color.FromArgb(241, 196, 15),    // yellow - fruit
            Accent,
            Dark,
        };

        /// <summary>'#RRGGBB' to a color; DefaultColor when empty or not valid.</summary>
        public static Color Parse(string? hex)
        {
            if (hex is { Length: 7 } && hex[0] == '#'
                && int.TryParse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
                return Color.FromArgb(255, Color.FromArgb(rgb));
            return DefaultColor;
        }

        /// <summary>The '#RRGGBB' to save; null for the default color.</summary>
        public static string? ToHex(Color color) =>
            color.ToArgb() == DefaultColor.ToArgb() ? null : $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        /// <summary>White text on dark colors, dark text on light ones.</summary>
        public static Color TextColorFor(Color back) =>
            back.R * 0.299 + back.G * 0.587 + back.B * 0.114 < 150 ? Color.White : Dark;

        /// <summary>The color a button shows: its own, else its tab's.</summary>
        public static Color ButtonColor(QuickButtonModel item, string? tabColor) => Parse(item.Color ?? tabColor);

        /// <summary>The second line of a button: price, "price / kg" or "price?".</summary>
        public static string PriceText(QuickButtonModel item) =>
            item.AskPrice ? "price?" : $"{item.SalePrice:#,0}{(item.IsWeighted ? " / " + item.BaseUnit : string.Empty)}";

        public static void Apply(Button button, QuickButtonModel item, string? tabColor)
        {
            var back = ButtonColor(item, tabColor);
            button.Text = $"{item.DisplayText}\n{PriceText(item)}";
            button.BackColor = back;
            button.ForeColor = TextColorFor(back);
            button.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = ControlPaint.Dark(back, 0.05f);
            button.UseVisualStyleBackColor = false;
            button.TabStop = false;
            button.AutoEllipsis = true;
        }

        /// <summary>A tab: outlined in its color; the open tab is filled with it.</summary>
        public static void ApplyGroup(Button button, string name, string? color, bool selected)
        {
            var tab = Parse(color);
            var accent = tab.ToArgb() == DefaultColor.ToArgb() ? Dark : tab;
            button.Text = name;
            button.BackColor = selected ? accent : Color.White;
            button.ForeColor = selected ? TextColorFor(accent) : Dark;
            button.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = selected ? 0 : 2;
            button.FlatAppearance.BorderColor = accent;
            button.UseVisualStyleBackColor = false;
            button.TabStop = false;
            button.AutoEllipsis = true;
        }

        /// <summary>A rounded rectangle path for custom drawing.</summary>
        public static GraphicsPath Rounded(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            var d = Math.Max(1, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>A small filled square, for menus.</summary>
        public static Bitmap Swatch(Color color, int size = 16)
        {
            var bitmap = new Bitmap(size, size);
            using var g = Graphics.FromImage(bitmap);
            using var brush = new SolidBrush(color);
            g.FillRectangle(brush, 0, 0, size - 1, size - 1);
            g.DrawRectangle(Pens.Gray, 0, 0, size - 1, size - 1);
            return bitmap;
        }
    }
}
