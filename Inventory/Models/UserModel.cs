using System.ComponentModel;

namespace Inventory.Models
{
    // Row returned by fn_user_get_all: a user of the system. Users log in with a password only.
    public class UserModel
    {
        public const short ManagerRole = 1;
        public const short UserRole = 2;

        [Browsable(false)] public int UserId { get; set; }
        [DisplayName("Name")] public string FullName { get; set; } = string.Empty;
        [Browsable(false)] public string UserName { get; set; } = string.Empty;   // filled from the name by the database
        [Browsable(false)] public short Role { get; set; } = UserRole;
        [DisplayName("Role")] public string RoleName => RoleText(Role);
        [Browsable(false)] public bool PasswordSet { get; set; }
        [DisplayName("Password")] public string PasswordStatus => PasswordSet ? "Set" : "Not set";
        [DisplayName("Active")] public bool IsActive { get; set; } = true;
        [Browsable(false)] public bool MustChangePassword { get; set; }

        [DisplayName("Created")]
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => _createdAt = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
        }
        private DateTime _createdAt;

        public static string RoleText(short role) => role == ManagerRole ? "Manager" : "User";
    }
}
