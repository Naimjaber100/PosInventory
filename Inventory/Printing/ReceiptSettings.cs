using System.Drawing.Printing;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inventory.Repositories;

namespace Inventory.Printing
{
    // Receipt printer of THIS POS station (Settings > Receipt printer): the "ReceiptPrinter" section of appsettings.json
    // next to the program, so each station can have its own printer. A station that must not print:
    //   "ReceiptPrinter": { "Enabled": false }
    // and one that prints:
    //   "ReceiptPrinter": { "Enabled": true, "PrinterName": "POS-80", "PrintAfterSale": true, "Copies": 1, "PaperWidthMm": 80 }
    // The footer ("receipt_footer") and the company logo are the same for every station (database).
    public sealed class ReceiptSettings
    {
        public const string Section = "ReceiptPrinter";
        public const string ConfigFile = "appsettings.json";

        public bool Enabled { get; set; } = true;                 // false = this station never prints receipts
        public string PrinterName { get; set; } = string.Empty;   // empty = no printer yet: sales work, nothing prints
        public bool PrintAfterSale { get; set; } = true;
        public int Copies { get; set; } = 1;
        public int PaperWidthMm { get; set; } = 80;               // 80 or 58
        public string Footer { get; set; } = "Thank you for shopping with us";

        /// <summary>The company logo for the top of the receipt (Settings > Company; CompanyLogo), null = none. Same for every computer.</summary>
        public byte[]? Logo { get; set; }

        /// <summary>Set when the cashier screen opens; null until then.</summary>
        public static ReceiptSettings? Current { get; private set; }

        /// <summary>The "ReceiptPrinter" section of appsettings.json read at start-up (Program), null when it has none.</summary>
        public static ReceiptSettings? FromConfig { get; set; }

        public static string ConfigFilePath => Path.Combine(AppContext.BaseDirectory, ConfigFile);

        public bool HasPrinter => Enabled && !string.IsNullOrWhiteSpace(PrinterName);

        /// <summary>True when the printer is installed on this computer (it may be added later).</summary>
        public bool PrinterInstalled =>
            HasPrinter && PrinterSettings.InstalledPrinters.Cast<string>().Any(p => string.Equals(p, PrinterName, StringComparison.OrdinalIgnoreCase));

        /// <summary>The "ReceiptPrinter" section of one appsettings file; null when it has none. Missing values keep their default.</summary>
        public static ReceiptSettings? Read(JsonElement root)
        {
            if (!root.TryGetProperty(Section, out var section) || section.ValueKind != JsonValueKind.Object) return null;
            var settings = new ReceiptSettings();
            foreach (var property in section.EnumerateObject())
            {
                var value = property.Value;
                switch (property.Name.ToLowerInvariant())
                {
                    case "enabled" when value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                        settings.Enabled = value.GetBoolean(); break;
                    case "printername" when value.ValueKind == JsonValueKind.String:
                        settings.PrinterName = value.GetString()?.Trim() ?? string.Empty; break;
                    case "printaftersale" when value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                        settings.PrintAfterSale = value.GetBoolean(); break;
                    case "copies" when value.TryGetInt32(out var copies):
                        settings.Copies = Math.Clamp(copies, 1, 5); break;
                    case "paperwidthmm" when value.TryGetInt32(out var width):
                        settings.PaperWidthMm = width == 58 ? 58 : 80; break;
                }
            }
            return settings;
        }

        public static async Task<ReceiptSettings> LoadAsync()
        {
            var repo = new SaleRepository();
            var config = FromConfig ?? new ReceiptSettings { PrinterName = string.Empty };   // no section: no printer
            var settings = new ReceiptSettings
            {
                Enabled = config.Enabled,
                PrinterName = config.PrinterName,
                PrintAfterSale = config.PrintAfterSale,
                Copies = config.Copies,
                PaperWidthMm = config.PaperWidthMm,
            };
            settings.Footer = await repo.GetSettingAsync("receipt_footer", settings.Footer) ?? string.Empty;
            try { settings.Logo = await new CompanyRepository().GetLogoAsync(); }
            catch { /* company_functions.sql not applied yet: receipts print without a logo */ }
            Current = settings;
            return settings;
        }

        /// <summary>Saves the printer in the "ReceiptPrinter" section of appsettings.json (the other sections are kept), the footer in the database.</summary>
        public async Task SaveAsync()
        {
            var path = ConfigFilePath;
            JsonObject root;
            try { root = File.Exists(path) ? JsonNode.Parse(await File.ReadAllTextAsync(path)) as JsonObject ?? new JsonObject() : new JsonObject(); }
            catch (JsonException) { root = new JsonObject(); }   // a broken file is written again
            root[Section] = new JsonObject
            {
                ["Enabled"] = Enabled,
                ["PrinterName"] = PrinterName.Trim(),
                ["PrintAfterSale"] = PrintAfterSale,
                ["Copies"] = Copies,
                ["PaperWidthMm"] = PaperWidthMm,
            };
            try
            {
                await File.WriteAllTextAsync(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                throw new InvalidOperationException($"Could not write {path}:\n{ex.Message}\n\nRun the program once as administrator, or give Windows users write access to its folder.");
            }
            FromConfig = new ReceiptSettings { Enabled = Enabled, PrinterName = PrinterName.Trim(), PrintAfterSale = PrintAfterSale, Copies = Copies, PaperWidthMm = PaperWidthMm };

            await new SaleRepository().SetSettingAsync("receipt_footer", Footer.Trim());
            Current = this;
        }
    }
}
