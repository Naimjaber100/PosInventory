using System.Diagnostics;
using Npgsql;

namespace Inventory
{
    // Settings > Database backup: back up the whole database now (DatabaseBackup), choose the folder,
    // and see the backups already made. Built in code, like the cashier's small windows.
    public class BackupForm : Form
    {
        private readonly TextBox _folderText;
        private readonly Label _toolLabel, _statusLabel;
        private readonly ListView _list;
        private readonly Button _backupButton;
        private CancellationTokenSource? _running;

        public BackupForm()
        {
            Text = "Database backup";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(720, 520);
            Font = new Font("Segoe UI", 10F);

            var cs = new NpgsqlConnectionStringBuilder(AppConfig.ConnectionString);
            var table = new TableLayoutPanel { Dock = DockStyle.Top, Height = 150, ColumnCount = 3, Padding = new Padding(14, 12, 14, 0) };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210F));

            table.Controls.Add(Caption("Database"), 0, 0);
            table.Controls.Add(new Label { Text = $"{cs.Database}  on  {cs.Host}:{cs.Port}", AutoSize = true, Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold), Margin = new Padding(3, 7, 3, 3) }, 1, 0);

            table.Controls.Add(Caption("Backup tool"), 0, 1);
            _toolLabel = new Label { AutoSize = true, ForeColor = Color.FromArgb(90, 98, 108), Margin = new Padding(3, 7, 3, 3), MaximumSize = new Size(560, 0) };
            table.Controls.Add(_toolLabel, 1, 1);
            table.SetColumnSpan(_toolLabel, 2);

            table.Controls.Add(Caption("Folder"), 0, 2);
            _folderText = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Text = AppConfig.Backup.EffectiveFolder, Margin = new Padding(3, 5, 3, 3) };
            table.Controls.Add(_folderText, 1, 2);
            var folderButtons = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            var browseButton = new Button { Text = "Change...", Size = new Size(96, 32) };
            browseButton.Click += (_, _) => ChooseFolder();
            var openButton = new Button { Text = "Open folder", Size = new Size(106, 32) };
            openButton.Click += (_, _) => OpenFolder();
            folderButtons.Controls.AddRange(new Control[] { browseButton, openButton });
            table.Controls.Add(folderButtons, 2, 2);

            _backupButton = new Button
            {
                Text = "Back up now", Size = new Size(200, 44), BackColor = Color.FromArgb(28, 110, 92), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), Margin = new Padding(3, 10, 3, 3),
            };
            _backupButton.FlatAppearance.BorderSize = 0;
            _backupButton.Click += async (_, _) => await BackupAsync();
            _statusLabel = new Label { AutoSize = true, Margin = new Padding(12, 22, 3, 3), MaximumSize = new Size(470, 0) };
            var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(14, 0, 14, 0), WrapContents = false };
            actionPanel.Controls.AddRange(new Control[] { _backupButton, _statusLabel });

            _list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };
            _list.Columns.Add("Backups in this folder (newest first)", 380);
            _list.Columns.Add("Made", 170);
            _list.Columns.Add("Size", 110, HorizontalAlignment.Right);
            var listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 4, 14, 4) };
            listPanel.Controls.Add(_list);

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10, 8, 10, 8) };
            var closeButton = new Button { Text = "Close", Size = new Size(100, 36), DialogResult = DialogResult.Cancel };
            var hint = new Label
            {
                AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(3, 4, 20, 3), MaximumSize = new Size(560, 0),
                Text = $"Keeps the last {AppConfig.Backup.KeepLast} backups (Backup.KeepLast in appsettings.json). Copy them to a USB key or another computer too.",
            };
            if (AppConfig.Backup.KeepLast <= 0) hint.Text = "Every backup is kept. Copy them to a USB key or another computer too.";
            bottom.Controls.AddRange(new Control[] { closeButton, hint });

            Controls.Add(listPanel);
            Controls.Add(actionPanel);
            Controls.Add(table);
            Controls.Add(bottom);
            CancelButton = closeButton;
            AppTheme.Apply(this);

            Load += (_, _) => { ShowTool(); ShowList(); };
            FormClosing += (_, e) =>
            {
                if (_running == null) return;
                if (MessageBox.Show(this, "A backup is still running. Stop it?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) _running.Cancel();
                else e.Cancel = true;
            };
        }

        private static Label Caption(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(3, 7, 3, 3) };

        private void ShowTool()
        {
            var tool = DatabaseBackup.FindPgDump();
            _toolLabel.Text = tool ?? "pg_dump.exe not found: install the PostgreSQL command line tools, or set Backup.PgDumpPath in appsettings.json.";
            _toolLabel.ForeColor = tool == null ? Color.Firebrick : Color.FromArgb(90, 98, 108);
            _backupButton.Enabled = tool != null;
        }

        private void ShowList()
        {
            _list.Items.Clear();
            foreach (var file in DatabaseBackup.List(_folderText.Text))
                _list.Items.Add(new ListViewItem(new[] { file.Name, file.LastWriteTime.ToString("dd/MM/yyyy HH:mm"), FileSize(file.Length) }) { Tag = file.FullName });
            if (_list.Items.Count == 0) _list.Items.Add(new ListViewItem(new[] { "No backup yet in this folder.", string.Empty, string.Empty }) { ForeColor = Color.DimGray });
        }

        private async Task BackupAsync()
        {
            _running = new CancellationTokenSource();
            _backupButton.Enabled = false;
            UseWaitCursor = true;
            ShowStatus("Backing up the database...", Color.FromArgb(29, 78, 160));
            var started = DateTime.Now;
            try
            {
                var file = await DatabaseBackup.RunAsync(_folderText.Text, _running.Token);
                ShowStatus($"Done in {(DateTime.Now - started).TotalSeconds:0} s: {file.Name} ({FileSize(file.Length)}).", Color.FromArgb(34, 139, 117));
                ShowList();
            }
            catch (OperationCanceledException)
            {
                ShowStatus("Stopped: no backup was made.", Color.DimGray);
            }
            catch (Exception ex)
            {
                ShowStatus("The backup failed.", Color.Firebrick);
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _running = null;
                UseWaitCursor = false;
                _backupButton.Enabled = true;
            }
        }

        // The folder is remembered in appsettings.json ("Backup": { "Folder": ... })
        private void ChooseFolder()
        {
            using var dialog = new FolderBrowserDialog { Description = "Folder for the database backups", SelectedPath = _folderText.Text, UseDescriptionForTitle = true };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var settings = AppConfig.Backup;
                var old = settings.Folder;
                settings.Folder = dialog.SelectedPath;
                try { settings.Save(); }
                catch { settings.Folder = old; throw; }
                _folderText.Text = settings.EffectiveFolder;
                ShowList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenFolder()
        {
            try
            {
                Directory.CreateDirectory(_folderText.Text);
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_folderText.Text}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not open the folder:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowStatus(string text, Color color)
        {
            _statusLabel.Text = text;
            _statusLabel.ForeColor = color;
        }

        private static string FileSize(long bytes) => bytes >= 1024 * 1024 ? $"{bytes / 1024d / 1024d:0.0} MB" : $"{Math.Max(1, bytes / 1024)} KB";
    }
}
