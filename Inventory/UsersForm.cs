using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Settings > Users: the people who use the system and their role (Manager / User).
    // Delete removes a user without activity; one with sales, purchases... is deactivated instead.
    public partial class UsersForm : Form
    {
        private readonly UserRepository _userRepository = new();
        private List<UserModel> _users = new();

        public UsersForm()
        {
            InitializeComponent();
        }

        private async void UsersForm_Load(object? sender, EventArgs e) => await LoadUsersAsync();

        private async Task LoadUsersAsync(int? selectId = null)
        {
            selectId ??= Selected?.UserId;
            try
            {
                _users = (await _userRepository.GetAllAsync()).ToList();
                usersGrid.DataSource = _users;
                var managers = _users.Count(u => u.IsActive && u.Role == UserModel.ManagerRole);
                countLabel.Text = $"{_users.Count(u => u.IsActive)} active user(s) · {managers} manager(s)";
                if (_users.FindIndex(u => u.UserId == selectId) is var index and >= 0)
                    usersGrid.CurrentCell = usersGrid.Rows[index].Cells.Cast<DataGridViewCell>().First(c => c.Visible);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the users:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private UserModel? Selected => usersGrid.CurrentRow?.DataBoundItem as UserModel;

        private async void addButton_Click(object? sender, EventArgs e)
        {
            using var form = new UserEditForm();
            if (form.ShowDialog(this) != DialogResult.OK) return;
            await LoadUsersAsync(form.SavedUserId);
        }

        private async void editButton_Click(object? sender, EventArgs e) => await EditSelectedAsync();

        private async void usersGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) await EditSelectedAsync();
        }

        private async Task EditSelectedAsync()
        {
            if (Selected is not { } user) return;
            using var form = new UserEditForm(user);
            if (form.ShowDialog(this) != DialogResult.OK) return;
            await LoadUsersAsync(user.UserId);
        }

        private async void deleteButton_Click(object? sender, EventArgs e)
        {
            if (Selected is not { } user) return;
            if (user.UserId == AppConfig.CurrentUserId)
            {
                MessageBox.Show(this, "You cannot delete yourself.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show(this, $"Delete the user \"{user.FullName}\"?\n\n" +
                                      "A user who already has sales, purchases or other activity is deactivated instead, to keep the history.",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                var deleted = await _userRepository.DeleteAsync(user.UserId);
                if (!deleted)
                    MessageBox.Show(this, $"\"{user.FullName}\" has activity in the system, so the user was deactivated instead of deleted.",
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadUsersAsync(deleted ? null : user.UserId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not delete the user:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void usersGrid_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter: e.SuppressKeyPress = true; editButton.PerformClick(); break;
                case Keys.Insert: addButton.PerformClick(); break;
                case Keys.Delete: deleteButton.PerformClick(); break;
            }
        }

        private void usersGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (usersGrid.Columns[nameof(UserModel.FullName)] is { } name) name.FillWeight = 160;
            if (usersGrid.Columns[nameof(UserModel.IsActive)] is { } active) active.FillWeight = 50;
            if (usersGrid.Columns[nameof(UserModel.CreatedAt)] is { } created) created.DefaultCellStyle.Format = "dd/MM/yyyy";
        }

        // Inactive users in grey, managers in bold, a missing password in red
        private void usersGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _users.Count || e.CellStyle == null) return;
            var user = _users[e.RowIndex];
            if (!user.IsActive) e.CellStyle.ForeColor = Color.Gray;
            else if (!user.PasswordSet && usersGrid.Columns[e.ColumnIndex].DataPropertyName == nameof(UserModel.PasswordStatus))
                e.CellStyle.ForeColor = Color.Firebrick;
            if (usersGrid.Columns[e.ColumnIndex].DataPropertyName == nameof(UserModel.RoleName) && user.Role == UserModel.ManagerRole)
                e.CellStyle.Font = new Font(usersGrid.Font, FontStyle.Bold);
        }
    }
}
