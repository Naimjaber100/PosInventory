using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Login with a password only (no username): type it, or tap it on the keypad, then Enter / Log in.
    // First start: when no active Manager has a password yet, it asks to create one for the first Manager.
    // After 3 wrong passwords each try waits a little longer (up to 30 s), against guessing.
    public partial class LoginForm : Form
    {
        private const int MinPasswordLength = 4;
        private const int FreeTries = 3;
        private const int MaxWaitSeconds = 30;

        private static int _failures;                 // kept across logouts while the app runs
        private static DateTime _lockedUntil = DateTime.MinValue;

        private readonly UserRepository _userRepository = new();
        private readonly SaleRepository _saleRepository = new();
        private UserModel? _setupManager;             // first start: the Manager whose password is being created
        private TextBox _entry;                       // where the keypad types
        private bool _prepared;                       // the database answered (store name, users)

        public LoginForm()
        {
            InitializeComponent();
            _entry = passwordText;
            BuildKeypad();
        }

        private async void LoginForm_Load(object? sender, EventArgs e)
        {
            clockTimer_Tick(null, EventArgs.Empty);
            clockTimer.Start();
            await PrepareAsync();
        }

        // Store name, and first-start setup when no Manager can log in yet
        private async Task PrepareAsync()
        {
            try
            {
                storeLabel.Text = await new CompanyRepository().GetNameAsync();
                var users = (await _userRepository.GetAllAsync()).ToList();
                var managers = users.Where(u => u.IsActive && u.Role == UserModel.ManagerRole).ToList();
                _setupManager = managers.Any(m => m.PasswordSet) ? null : managers.FirstOrDefault();
                _prepared = true;
            }
            catch (Exception ex)
            {
                ShowStatus("Cannot connect to the database: " + ex.Message, Color.Firebrick);
                return;
            }

            var setup = _setupManager != null;
            confirmPanel.Visible = setup;
            titleLabel.Text = setup ? "First start" : "Enter your password";
            hintLabel.Text = setup
                ? $"Create the password of {_setupManager!.FullName} (Manager). You will use it to log in."
                : "Type your password or tap it on the keypad.";
            loginButton.Text = setup ? "Save and log in" : "Log in";
            ShowStatus(string.Empty, Color.DimGray);
            FocusPassword();
        }

        private void clockTimer_Tick(object? sender, EventArgs e)
        {
            clockLabel.Text = DateTime.Now.ToString("ddd dd/MM/yyyy  HH:mm");
            if (DateTime.Now < _lockedUntil)
                ShowStatus($"Too many wrong passwords. Try again in {(int)Math.Ceiling((_lockedUntil - DateTime.Now).TotalSeconds)} s.", Color.Firebrick);
            else if (!loginButton.Enabled && !_busy)
            {
                loginButton.Enabled = true;
                ShowStatus(string.Empty, Color.DimGray);
            }
        }

        #region Keypad

        private void BuildKeypad()
        {
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "⌫" };
            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var button = new Button
                {
                    Text = key,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(4),
                    Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(244, 246, 248),
                    ForeColor = key == "C" ? Color.Firebrick : QuickButtonStyle.Dark,
                    TabStop = false,
                    UseVisualStyleBackColor = false,
                };
                button.FlatAppearance.BorderColor = Color.Silver;
                button.Click += (_, _) => KeyPressed(key);
                keypadTable.Controls.Add(button, i % 3, i / 3);
            }
        }

        private void KeyPressed(string key)
        {
            var box = _entry;
            switch (key)
            {
                case "C": box.Clear(); break;
                case "⌫": if (box.TextLength > 0) box.Text = box.Text[..^1]; break;
                default: box.AppendText(key); break;
            }
            box.Focus();
            box.SelectionStart = box.TextLength;
        }

        private void entry_Enter(object? sender, EventArgs e)
        {
            if (sender is TextBox box) _entry = box;
        }

        private void FocusPassword()
        {
            passwordText.Focus();
            _entry = passwordText;
        }

        #endregion

        #region Log in

        private bool _busy;

        private async void loginButton_Click(object? sender, EventArgs e) => await LogInAsync();

        private async void passwordText_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            if (confirmPanel.Visible && confirmText.TextLength == 0) { confirmText.Focus(); return; }
            await LogInAsync();
        }

        private async void confirmText_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await LogInAsync();
        }

        private async Task LogInAsync()
        {
            if (_busy || DateTime.Now < _lockedUntil) return;
            if (!_prepared)
            {
                await PrepareAsync();   // the database was not reachable: try again
                if (!_prepared || _setupManager != null) return;
            }
            var password = passwordText.Text;
            if (password.Length == 0) { ShowStatus("Type your password.", Color.DarkGoldenrod); FocusPassword(); return; }

            try
            {
                _busy = true;
                loginButton.Enabled = false;
                UseWaitCursor = true;

                if (_setupManager != null)
                {
                    if (!await CreateManagerPasswordAsync(password)) return;
                }

                var user = await _userRepository.FindByPasswordAsync(password);
                if (user == null)
                {
                    WrongPassword();
                    return;
                }

                _failures = 0;
                AppSession.SignIn(user);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ShowStatus("Cannot log in: " + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message), Color.Firebrick);
            }
            finally
            {
                _busy = false;
                UseWaitCursor = false;
                if (!IsDisposed && DateTime.Now >= _lockedUntil) loginButton.Enabled = true;
            }
        }

        // First start: saves the password of the first Manager (it must be typed twice)
        private async Task<bool> CreateManagerPasswordAsync(string password)
        {
            if (password.Length < MinPasswordLength)
            {
                ShowStatus($"The password needs at least {MinPasswordLength} characters.", Color.DarkGoldenrod);
                FocusPassword();
                return false;
            }
            if (password != confirmText.Text)
            {
                ShowStatus("The two passwords are not the same.", Color.DarkGoldenrod);
                confirmText.Clear();
                confirmText.Focus();
                return false;
            }
            await _userRepository.SaveAsync(_setupManager!, password);
            _setupManager = null;
            return true;
        }

        private void WrongPassword()
        {
            _failures++;
            passwordText.Clear();
            System.Media.SystemSounds.Hand.Play();
            if (_failures >= FreeTries)
            {
                var wait = Math.Min(MaxWaitSeconds, (_failures - FreeTries + 1) * 5);
                _lockedUntil = DateTime.Now.AddSeconds(wait);
                ShowStatus($"Wrong password. Try again in {wait} s.", Color.Firebrick);
                loginButton.Enabled = false;
            }
            else
            {
                ShowStatus("Wrong password.", Color.Firebrick);
            }
            FocusPassword();
        }

        #endregion

        private void exitButton_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void ShowStatus(string text, Color color)
        {
            statusLabel.Text = text;
            statusLabel.ForeColor = color;
        }

        private void hintLabel_Click(object sender, EventArgs e)
        {

        }
    }
}
