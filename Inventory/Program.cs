namespace Inventory
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            // Load connection string from appsettings.json (simple JSON read to avoid adding package refs)
            try
            {
                var path = System.IO.Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (System.IO.File.Exists(path))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(path));
                    if (doc.RootElement.TryGetProperty("ConnectionStrings", out var csSection) &&
                        csSection.TryGetProperty("DefaultConnection", out var defaultConn))
                    {
                        AppConfig.ConnectionString = defaultConn.GetString();
                    }
                }
            }
            catch
            {
                // ignore parse errors; ConnectionString will remain null
            }

            ApplicationConfiguration.Initialize();

            // Login, then the user's main window: back office for a Manager, cashier for a User.
            // Closing it (or Log out) comes back to the login screen; the app exits from there.
            while (true)
            {
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK) return;
                }

                Application.Run(AppSession.IsManager ? new Form1() : new CashierForm());
                foreach (var left in Application.OpenForms.Cast<Form>().ToList()) left.Dispose();   // nothing stays open behind the login
                AppSession.SignOut();
            }
        }
    }
}