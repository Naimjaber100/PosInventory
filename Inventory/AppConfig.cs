namespace Inventory
{
    internal static class AppConfig
    {
        public static string? ConnectionString { get; set; }

        // How scale labels (weight / price in the barcode) are read: "ScaleBarcode" in appsettings.json
        public static ScaleBarcodeSettings Scale { get; set; } = new();

        // AI reading of purchase invoice photos: "OpenAI" in appsettings.json
        public static Ai.OpenAiSettings OpenAi { get; set; } = new();

        // Settings > Database backup: "Backup" in appsettings.json
        public static BackupSettings Backup { get; set; } = new();

        // Recorded on sales, purchases and stock movements. Set by the login screen (AppSession.SignIn).
        public static int CurrentUserId { get; set; } = 1;
    }
}
