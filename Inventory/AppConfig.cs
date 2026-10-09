namespace Inventory
{
    internal static class AppConfig
    {
        public static string? ConnectionString { get; set; }

        // Recorded on sales, purchases and stock movements. Set by the login screen (AppSession.SignIn).
        public static int CurrentUserId { get; set; } = 1;
    }
}
