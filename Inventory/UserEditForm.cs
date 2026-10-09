using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Adds a user (no user passed) or edits one: name, password, role (Manager / User), active.
    // Users log in with the password only, so each active user needs a different password.
    // When editing, an empty password keeps the current one.
    public partial class UserEditForm : Form
    {
        private const int MinPasswordLength = 4;

        private readonly UserRepository _userRepository = new();
        private readonly UserModel? _user;

        public UserEditForm(UserModel? user = null)
        {
            InitializeComponent();
            _user = user;

            roleCombo.DataSource = new[]
            {
                new { Id = UserModel.ManagerRole, Text = "Manager - full access, back office and settings" },
                new { Id = UserModel.UserRole, Text = "User - cashier screen" },
            };
            roleCombo.DisplayMember = "Text";
            roleCombo.ValueMember = "Id";

            if (user != null)
            {
                Text = "Edit user";
                nameText.Text = user.FullName;
                roleCombo.SelectedValue = user.Role;
                activeCheck.Checked = user.IsActive;
                passwordHint.Text = user.PasswordSet ? "Leave empty to keep the current password." : "No usable password yet: set one.";
                if (!user.PasswordSet) passwordHint.ForeColor = Color.Firebrick;

                // You cannot lock yourself out
                if (user.UserId == AppConfig.CurrentUserId)
                {
                    activeCheck.Enabled = false;
                    passwordHint.Text += "  (This is you: you cannot deactivate yourself.)";
                }
            }
            else
            {
                Text = "Add user";
                roleCombo.SelectedValue = UserModel.UserRole;
                passwordHint.Text = $"At least {MinPasswordLength} characters, different for each user.";
            }
        }

        /// <summary>The id of the saved user.</summary>
        public int SavedUserId { get; private set; }

        private void showPasswordCheck_CheckedChanged(object? sender, EventArgs e)
        {
            passwordText.UseSystemPasswordChar = confirmText.UseSystemPasswordChar = !showPasswordCheck.Checked;
        }

        private async void saveButton_Click(object? sender, EventArgs e)
        {
            var name = nameText.Text.Trim();
            var password = passwordText.Text;

            string? problem = null;
            Control? field = null;
            if (name.Length == 0) (problem, field) = ("Name is required.", nameText);
            else if (password.Length == 0 && (_user == null || !_user.PasswordSet)) (problem, field) = ("Set a password for this user.", passwordText);
            else if (password.Length > 0 && password.Length < MinPasswordLength) (problem, field) = ($"The password needs at least {MinPasswordLength} characters.", passwordText);
            else if (password != confirmText.Text) (problem, field) = ("The two passwords are not the same.", confirmText);
            if (problem != null)
            {
                MessageBox.Show(this, problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                field?.Focus();
                return;
            }

            var user = new UserModel
            {
                UserId = _user?.UserId ?? 0,
                FullName = name,
                Role = roleCombo.SelectedValue is short role ? role : UserModel.UserRole,
                IsActive = activeCheck.Checked,
            };

            try
            {
                saveButton.Enabled = false;
                SavedUserId = await _userRepository.SaveAsync(user, password.Length > 0 ? password : null);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                var message = ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message;
                MessageBox.Show(this, "Could not save the user:\n" + message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (message.Contains("password", StringComparison.OrdinalIgnoreCase)) passwordText.Focus();
            }
            finally
            {
                saveButton.Enabled = true;
            }
        }
    }
}
