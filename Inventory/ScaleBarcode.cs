using System.Text.Json;

namespace Inventory
{
    // Barcodes printed by a weighing scale: the item AND the weight or the price are in the barcode.
    // Example (default settings, EAN-13):  21 12345 01250 7
    //   21     prefix: a weight label (WeightPrefixes) - 22 would be a price label (PricePrefixes)
    //   12345  the item's scale code (PLU), set on the product (Scale product)
    //   01250  the value: 1.250 kg (WeightDecimals 3) - or 1,250 for a price label (PriceDecimals 0)
    //   7      check digit (checked when CheckDigit is true)
    // All positions are 0-based and come from the "ScaleBarcode" section of appsettings.json,
    // so the system follows how the shop's scale is set up.
    public sealed class ScaleBarcodeSettings
    {
        public bool Enabled { get; set; } = true;
        public int Length { get; set; } = 13;
        public List<string> WeightPrefixes { get; set; } = new() { "21", "23" };
        public List<string> PricePrefixes { get; set; } = new() { "22", "24" };
        public int ItemCodeStart { get; set; } = 2;
        public int ItemCodeLength { get; set; } = 5;
        public int ValueStart { get; set; } = 7;
        public int ValueLength { get; set; } = 5;
        public int WeightDecimals { get; set; } = 3;
        public int PriceDecimals { get; set; }
        public bool CheckDigit { get; set; } = true;

        /// <summary>Reads the "ScaleBarcode" section of appsettings.json (defaults for anything missing or wrong).</summary>
        public static ScaleBarcodeSettings Read(JsonElement root)
        {
            if (!root.TryGetProperty("ScaleBarcode", out var section)) return new ScaleBarcodeSettings();
            try
            {
                return section.Deserialize<ScaleBarcodeSettings>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? new ScaleBarcodeSettings();
            }
            catch (JsonException)
            {
                return new ScaleBarcodeSettings();
            }
        }
    }

    public enum ScaleValueKind { Weight, Price }

    /// <summary>A decoded scale label: the item's scale code and the weight (base unit) or the price on it.</summary>
    public sealed record ScaleLabel(string ItemCode, ScaleValueKind Kind, decimal Value);

    public static class ScaleBarcode
    {
        public static ScaleBarcodeSettings Settings => AppConfig.Scale;

        /// <summary>True when the text is a scale label (right prefix, length, digits and check digit).</summary>
        public static bool TryParse(string text, out ScaleLabel label)
        {
            label = null!;
            var s = Settings;
            text = text.Trim();
            if (!s.Enabled || text.Length != s.Length || !text.All(char.IsAsciiDigit)) return false;
            if (s.ItemCodeStart + s.ItemCodeLength > text.Length || s.ValueStart + s.ValueLength > text.Length) return false;

            ScaleValueKind kind;
            if (s.WeightPrefixes.Any(p => p.Length > 0 && text.StartsWith(p, StringComparison.Ordinal))) kind = ScaleValueKind.Weight;
            else if (s.PricePrefixes.Any(p => p.Length > 0 && text.StartsWith(p, StringComparison.Ordinal))) kind = ScaleValueKind.Price;
            else return false;

            if (s.CheckDigit && !Ean13CheckOk(text)) return false;

            var code = text.Substring(s.ItemCodeStart, s.ItemCodeLength);
            var raw = decimal.Parse(text.Substring(s.ValueStart, s.ValueLength));
            var decimals = kind == ScaleValueKind.Weight ? s.WeightDecimals : s.PriceDecimals;
            label = new ScaleLabel(code, kind, raw / (decimal)Math.Pow(10, decimals));
            return true;
        }

        /// <summary>
        /// True when the text has the shape of a scale label (prefix, length, digits) but its check digit is wrong:
        /// a misread or mistyped label.
        /// </summary>
        public static bool IsBadScaleLabel(string text)
        {
            var s = Settings;
            text = text.Trim();
            if (!s.Enabled || !s.CheckDigit || text.Length != s.Length || !text.All(char.IsAsciiDigit)) return false;
            var prefixOk = s.WeightPrefixes.Concat(s.PricePrefixes).Any(p => p.Length > 0 && text.StartsWith(p, StringComparison.Ordinal));
            return prefixOk && !Ean13CheckOk(text);
        }

        /// <summary>A scale code as stored on the product: digits, padded with zeros to ItemCodeLength.</summary>
        public static string NormalizeCode(string code) => code.Trim().PadLeft(Settings.ItemCodeLength, '0');

        // EAN-13 / EAN-8 style check digit (the last digit): weights 3 and 1 from the right
        private static bool Ean13CheckOk(string text)
        {
            var sum = 0;
            var weight = 3;
            for (var i = text.Length - 2; i >= 0; i--)
            {
                sum += (text[i] - '0') * weight;
                weight = weight == 3 ? 1 : 3;
            }
            return (10 - sum % 10) % 10 == text[^1] - '0';
        }
    }
}
