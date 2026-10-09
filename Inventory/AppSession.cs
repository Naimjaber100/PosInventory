using Inventory.Models;

namespace Inventory
{
    // Who is logged in. Set by the login screen; AppConfig.CurrentUserId is what sales, purchases... record.
    public static class AppSession
    {
        public static UserModel? CurrentUser { get; private set; }

        public static string UserName => CurrentUser?.FullName ?? "?";

        public static bool IsManager => CurrentUser?.Role == UserModel.ManagerRole;

        public static void SignIn(UserModel user)
        {
            CurrentUser = user;
            AppConfig.CurrentUserId = user.UserId;
        }

        public static void SignOut()
        {
            CurrentUser = null;
        }

        /// <summary>
        /// Closes every window so Program shows the login screen again.
        /// Stops (returns false) when a window refuses to close, e.g. the cashier with a sale still open.
        /// </summary>
        public static bool LogOut()
        {
            foreach (var form in Application.OpenForms.Cast<Form>().Reverse().ToList())
            {
                form.Close();
                if (Application.OpenForms.Cast<Form>().Contains(form)) return false;
            }
            return true;
        }
    }
}
