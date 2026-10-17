using System.Drawing.Printing;
using System.Globalization;
using Inventory.Repositories;

namespace Inventory.Printing
{
    // Receipt printer of THIS computer (Settings > Receipt printer). Kept in the settings table under the
    // computer's name, so each cashier PC has its own printer and an update of the program does not lose it.
    // The footer ("receipt_footer") and the company logo are the same for every computer.
    public sealed class ReceiptSettings
    {
        public string PrinterName { get; set; } = string.Empty;   // empty = no printer yet: sales work, nothing prints
        public bool PrintAfterSale { get; set; } = true;
        public int Copies { get; set; } = 1;
        public int PaperWidthMm { get; set; } = 80;               // 80 or 58
        public string Footer { get; set; } = "Thank you for shopping with us";

        /// <summary>The company logo for the top of the receipt (Settings > Company; CompanyLogo), null = none. Same for every computer.</summary>
        public byte[]? Logo { get; set; }

        /// <summary>Set when the cashier screen opens; null until then.</summary>
        public static ReceiptSettings? Current { get; private set; }

        private static string Key(string name) => $"printer.{Environment.MachineName}.{name}".ToLowerInvariant();

        public bool HasPrinter => !string.IsNullOrWhiteSpace(PrinterName);

        /// <summary>True when the printer is installed on this computer (it may be added later).</summary>
        public bool PrinterInstalled =>
            HasPrinter && PrinterSettings.InstalledPrinters.Cast<string>().Any(p => string.Equals(p, PrinterName, StringComparison.OrdinalIgnoreCase));

        public static async Task<ReceiptSettings> LoadAsync()
        {
            var repo = new SaleRepository();
            var settings = new ReceiptSettings
            {
                PrinterName = await repo.GetSettingAsync(Key("name"), string.Empty) ?? string.Empty,
                PrintAfterSale = await repo.GetSettingAsync(Key("auto"), "1") != "0",
                Copies = int.TryParse(await repo.GetSettingAsync(Key("copies"), "1"), out var c) ? Math.Clamp(c, 1, 5) : 1,
                PaperWidthMm = await repo.GetSettingAsync(Key("width"), "80") == "58" ? 58 : 80,
            };
            settings.Footer = await repo.GetSettingAsync("receipt_footer", settings.Footer) ?? string.Empty;
            try { settings.Logo = await new CompanyRepository().GetLogoAsync(); }
            catch { /* company_functions.sql not applied yet: receipts print without a logo */ }
            Current = settings;
            return settings;
        }

        public async Task SaveAsync()
        {
            var repo = new SaleRepository();
            await repo.SetSettingAsync(Key("name"), PrinterName.Trim());
            await repo.SetSettingAsync(Key("auto"), PrintAfterSale ? "1" : "0");
            await repo.SetSettingAsync(Key("copies"), Copies.ToString(CultureInfo.InvariantCulture));
            await repo.SetSettingAsync(Key("width"), PaperWidthMm.ToString(CultureInfo.InvariantCulture));
            await repo.SetSettingAsync("receipt_footer", Footer.Trim());
            Current = this;
        }
    }
}
