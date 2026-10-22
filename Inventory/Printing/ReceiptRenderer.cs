using Inventory.Models;

namespace Inventory.Printing
{
    // Draws a receipt for an 80 mm (or 58 mm) roll (the drawing helpers are in RollRenderer).
    public sealed class ReceiptRenderer : RollRenderer
    {
        // The logo on top: at most 60 % of the paper width and 0.9 inch (about 23 mm) high
        private const float LogoMaxHeight = 90F;
        private const float LogoMaxWidthShare = 0.6F;

        private readonly Receipt _r;
        private readonly string _footer;
        private readonly Bitmap? _logo;   // its own copy (a receipt may print on a background thread)

        public ReceiptRenderer(Receipt receipt, CurrencySet currencies, ReceiptSettings settings)
            : base(currencies, settings.PaperWidthMm)
        {
            _r = receipt;
            _footer = settings.Footer;
            _logo = CompanyLogo.ToImage(settings.Logo);
        }

        public override string DocumentName => $"Receipt {_r.InvoiceNo}";

        public override float Render(Graphics? g)
        {
            var y = 0F;
            var c = _r.Company;

            // Logo, then the shop
            if (_logo != null) y = Logo(g, _logo, y) + 6;
            y = Text(g, c.Name ?? string.Empty, Title, y, Align.Center);
            if (!string.IsNullOrWhiteSpace(c.Address)) y = Text(g, c.Address, Small, y, Align.Center);
            var contact = string.Join("   ", new[] { c.Mobile is { Length: > 0 } m ? $"Tel {m}" : null, c.Email }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (contact.Length > 0) y = Text(g, contact, Small, y, Align.Center);
            if (!string.IsNullOrWhiteSpace(c.VatNumber)) y = Text(g, $"VAT no. {c.VatNumber}", Small, y, Align.Center);
            y = Separator(g, y + 4);

            // Invoice
            y = Text(g, _r.IsReturn ? "RETURN / REFUND" : "INVOICE", Bold, y, Align.Center);
            y = TwoColumns(g, _r.InvoiceNo, _r.InvoiceDate.ToLocalTime().ToString("dd/MM/yyyy  HH:mm"), Normal, y);
            if (!string.IsNullOrWhiteSpace(_r.Cashier)) y = Text(g, $"Cashier: {_r.Cashier}", Small, y, Align.Left);
            if (!string.IsNullOrWhiteSpace(_r.Customer)) y = Text(g, $"Client: {_r.Customer}", Small, y, Align.Left);
            y = Separator(g, y + 2);

            // Items: a table  Item | Qty | Price | Total  (a long name wraps in its column; a discount on the line under it).
            // 58 mm paper is narrow: the smaller font, so that big amounts still fit their column.
            var font = Width < 250 ? Small : Normal;
            y = Row(g, Bold, y, ItemColumns("Item", "Qty", "Price", "Total"));
            y = Separator(g, y);
            foreach (var line in _r.Lines)
            {
                var qty = line.Quantity == Math.Round(line.Quantity) ? line.Quantity.ToString("#,0") : line.Quantity.ToString("#,0.###");
                y = Row(g, font, y, ItemColumns(line.ItemName, qty, Number(line.UnitPrice), Number(line.LineTotal)));
                if (line.Discount > 0)
                {
                    var promo = string.IsNullOrWhiteSpace(line.PromoName) ? "Discount" : $"Promo: {line.PromoName}";
                    y = TwoColumns(g, "   " + promo, "-" + Number(line.Discount), Small, y);
                }
                y += 3;
            }
            y = Separator(g, y);

            // Totals
            var discounts = _r.LineDiscounts + _r.Discount;
            if (discounts > 0)
            {
                y = TwoColumns(g, "Sub total", Number(_r.SubTotal + _r.LineDiscounts), Normal, y);
                y = TwoColumns(g, "Discount", "-" + Number(discounts), Normal, y);
                if (_r.Discount > 0 && !string.IsNullOrWhiteSpace(_r.PromoName)) y = Text(g, $"   Promo: {_r.PromoName}", Small, y, Align.Left);
            }
            y = TwoColumns(g, _r.IsReturn ? "REFUND" : "TOTAL", Currencies.Primary.Format(_r.Total), Big, y + 2);
            if (_r.UsdRate > 1 && !string.Equals(_r.PrimaryCode, "USD", StringComparison.OrdinalIgnoreCase))
                y = TwoColumns(g, $"In USD (1 $ = {Number(_r.UsdRate)})", $"$ {_r.Total / _r.UsdRate:#,0.00}", Small, y);

            // Payments and change
            if (_r.Payments.Count > 0)
            {
                y += 4;
                foreach (var p in _r.Payments)
                {
                    var how = p.Method switch { 2 => "Card", 3 => "On account", _ => _r.IsReturn ? "Cash back" : "Cash" };
                    var amount = Currencies.Find(p.Currency) is { } cur ? cur.Format(p.Amount) : $"{p.Currency} {p.Amount:#,0.##}";
                    y = TwoColumns(g, $"{how} {p.Currency}", amount, Normal, y);
                }
                if (_r.ChangeLbp > 0) y = TwoColumns(g, "Change", Currencies.Primary.Format(_r.ChangeLbp), Bold, y);
            }

            // Footer
            y = Separator(g, y + 4);
            if (!string.IsNullOrWhiteSpace(_footer)) y = Text(g, _footer, Normal, y, Align.Center);
            y = Text(g, $"{_r.Lines.Count} item line(s)", Small, y + 2, Align.Center);
            return y + 20;   // a little paper after the text, before the cut
        }

        // The columns of the item table: the name gets what the numbers leave
        private static Cell[] ItemColumns(string item, string qty, string price, string total) => new[]
        {
            new Cell(item, 0.38F, Align.Auto),
            new Cell(qty, 0.13F, Align.Right),
            new Cell(price, 0.24F, Align.Right),
            new Cell(total, 0.25F, Align.Right),
        };

        // The logo centred, as big as fits in its box without changing its shape
        private float Logo(Graphics? g, Bitmap logo, float y)
        {
            var scale = Math.Min(Width * LogoMaxWidthShare / logo.Width, LogoMaxHeight / logo.Height);
            var (width, height) = (logo.Width * scale, logo.Height * scale);
            if (g != null)
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(logo, new RectangleF((Width - width) / 2, y, width, height));
            }
            return y + height;
        }

        public override void Dispose()
        {
            _logo?.Dispose();
            base.Dispose();
        }
    }
}
