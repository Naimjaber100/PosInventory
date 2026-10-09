using System.Security.Cryptography;
using System.Text;

namespace Inventory
{
    // Password hashes for users.password_hash: PBKDF2 (SHA-256) with one fixed app salt, stored as "pin$" + Base64.
    // Users log in with the password only (no username), so the same password must always give the same hash:
    // the user is then found by it (fn_user_find_by_password) and no two active users can share a password.
    // The password itself is never stored.
    public static class PasswordHasher
    {
        private const string Prefix = "pin$";
        private const int Iterations = 100_000;
        private const int HashSize = 32;
        private static readonly byte[] AppSalt = Encoding.UTF8.GetBytes("Inventory.POS.users.v1");

        public static string Hash(string password)
        {
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, AppSalt, Iterations, HashAlgorithmName.SHA256, HashSize);
            return Prefix + Convert.ToBase64String(hash);
        }

        /// <summary>False for passwords saved before password-only login (they must be set again).</summary>
        public static bool IsCurrentFormat(string? stored) => stored?.StartsWith(Prefix, StringComparison.Ordinal) == true;
    }
}
