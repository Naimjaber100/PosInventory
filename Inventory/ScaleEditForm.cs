using Inventory.Models;
using Inventory.Repositories;
using Inventory.Scales;

namespace Inventory
{
    // Settings > Label scales > Add / Edit scale: its name, its model (the driver that sends the items, Scales/ScaleDrivers.cs)
    // and how it is reached: a file for the scale's PC program, an IP address and port, or a COM port.
    // Only the fields of the chosen model show. Built in code, like the cashier's small windows.
    public class ScaleEditForm : Form
    {
        private readonly ScaleRepository _scaleRepository = new();
        private readonly ScaleModel? _scale;
        private readonly TextBox _nameText, _fileText, _hostText, _serialText;
        private readonly NumericUpDown _portNumber, _baudNumber;
        private readonly ComboBox _driverCombo;
        private readonly CheckBox _activeCheck;
        private readonly Label _helpLabel;
        private readonly TableLayoutPanel _table;
        private readonly Control[] _fileRow, _hostRow, _serialRow;

        public ScaleEditForm(ScaleModel? scale)
        {
            _scale = scale;
            Text = scale == null ? "Add scale" : "Edit scale";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(640, 360);
            Font = new Font("Segoe UI", 10F);

            _table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(14, 12, 14, 0), AutoScroll = true };
            _table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            _table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));

            _nameText = new TextBox { Dock = DockStyle.Fill, MaxLength = 60, Margin = new Padding(3, 5, 3, 3) };
            AddRow(0, "Name", _nameText);

            _driverCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3, 5, 3, 3) };
            _driverCombo.Items.AddRange(ScaleDrivers.All.Cast<object>().ToArray());
            _driverCombo.Format += (_, e) => e.Value = (e.ListItem as IScaleDriver)?.Name;
            _driverCombo.SelectedIndexChanged += (_, _) => ShowDriver();
            AddRow(1, "Model", _driverCombo);

            _helpLabel = new Label { AutoSize = true, ForeColor = Color.FromArgb(90, 98, 108), MaximumSize = new Size(490, 0), Margin = new Padding(3, 2, 3, 8) };
            _table.Controls.Add(_helpLabel, 1, 2);
            _table.SetColumnSpan(_helpLabel, 2);

            _fileText = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 5, 3, 3) };
            var browseButton = new Button { Text = "Browse...", Size = new Size(100, 32) };
            browseButton.Click += (_, _) => ChooseFile();
            _fileRow = AddRow(3, "File", _fileText, browseButton);

            _hostText = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 5, 3, 3), PlaceholderText = "IP address, e.g. 192.168.1.87" };
            _portNumber = new NumericUpDown { Minimum = 1, Maximum = 65535, Width = 100, Margin = new Padding(3, 5, 3, 3) };
            _hostRow = AddRow(4, "IP / port", _hostText, _portNumber);

            _serialText = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 5, 3, 3), PlaceholderText = "COM1" };
            _baudNumber = new NumericUpDown { Minimum = 1200, Maximum = 921600, Increment = 1200, Width = 100, Margin = new Padding(3, 5, 3, 3) };
            _serialRow = AddRow(5, "COM / baud", _serialText, _baudNumber);

            _activeCheck = new CheckBox { Text = "In use", AutoSize = true, Checked = true, Margin = new Padding(3, 8, 3, 3) };
            _table.Controls.Add(_activeCheck, 1, 6);

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10, 8, 10, 8) };
            var cancelButton = new Button { Text = "Cancel", Size = new Size(100, 36), DialogResult = DialogResult.Cancel };
            var saveButton = new Button { Text = "Save", Size = new Size(100, 36), BackColor = AppTheme.Accent, ForeColor = Color.White, UseVisualStyleBackColor = false };
            saveButton.Click += async (_, _) => await SaveAsync();
            bottom.Controls.AddRange(new Control[] { cancelButton, saveButton });

            Controls.Add(_table);
            Controls.Add(bottom);
            AcceptButton = saveButton;
            CancelButton = cancelButton;
            AppTheme.Apply(this);

            _nameText.Text = scale?.Name ?? string.Empty;
            _fileText.Text = scale?.FilePath ?? string.Empty;
            _hostText.Text = scale?.Host ?? string.Empty;
            _serialText.Text = scale?.SerialPort ?? string.Empty;
            _activeCheck.Checked = scale?.IsActive ?? true;
            _driverCombo.SelectedItem = ScaleDrivers.Find(scale?.Driver) ?? ScaleDrivers.All[0];
            if (scale?.Port is { } port) _portNumber.Value = Math.Clamp(port, 1, 65535);
            if (scale?.BaudRate is { } baud) _baudNumber.Value = Math.Clamp(baud, 1200, 921600);
        }

        public int SavedId { get; private set; }

        private IScaleDriver Driver => (IScaleDriver)_driverCombo.SelectedItem!;

        private Control[] AddRow(int row, string caption, Control field, Control? extra = null)
        {
            var label = new Label { Text = caption, AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
            _table.Controls.Add(label, 0, row);
            _table.Controls.Add(field, 1, row);
            if (extra != null) _table.Controls.Add(extra, 2, row);
            return extra == null ? new Control[] { label, field } : new[] { label, field, extra };
        }

        // The fields of the chosen model; a new scale gets the model's usual port / speed
        private void ShowDriver()
        {
            var driver = Driver;
            _helpLabel.Text = driver.Help;
            foreach (var c in _fileRow) c.Visible = driver.Link == ScaleLink.File;
            foreach (var c in _hostRow) c.Visible = driver.Link == ScaleLink.Network;
            foreach (var c in _serialRow) c.Visible = driver.Link == ScaleLink.Serial;
            if (_scale?.Port == null && driver.DefaultPort > 0) _portNumber.Value = driver.DefaultPort;
            if (_scale?.BaudRate == null) _baudNumber.Value = driver.DefaultBaudRate;

            // A file of the other type (csv / xlsx) gets this model's extension
            if (driver.Link == ScaleLink.File && _fileText.Text.Trim().Length > 0)
                _fileText.Text = Path.ChangeExtension(_fileText.Text.Trim(), driver.FileExtension);
        }

        private void ChooseFile()
        {
            var driver = Driver;
            var current = _fileText.Text.Trim().Length > 0 ? _fileText.Text.Trim() : ScaleDrivers.DefaultFilePath(_nameText.Text, driver);
            using var dialog = new SaveFileDialog
            {
                Title = "File for the scale's PC program",
                Filter = $"{driver.FileExtension.TrimStart('.').ToUpperInvariant()} file|*{driver.FileExtension}",
                FileName = Path.GetFileName(current),
                InitialDirectory = Path.GetDirectoryName(current),
                OverwritePrompt = false,
            };
            if (dialog.ShowDialog(this) == DialogResult.OK) _fileText.Text = dialog.FileName;
        }

        private async Task SaveAsync()
        {
            var driver = Driver;
            string? problem = _nameText.Text.Trim().Length == 0 ? "Type a name for the scale (e.g. \"Cheese counter\")."
                : driver.Link == ScaleLink.Network && _hostText.Text.Trim().Length == 0 ? "Type the IP address of the scale (in the scale's settings menu)."
                : driver.Link == ScaleLink.Serial && _serialText.Text.Trim().Length == 0 ? "Type the COM port the scale is on (Windows Device Manager shows it)."
                : null;
            if (problem != null)
            {
                MessageBox.Show(this, problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var scale = new ScaleModel
            {
                ScaleId = _scale?.ScaleId ?? 0,
                Name = _nameText.Text.Trim(),
                Driver = driver.Key,
                FilePath = driver.Link == ScaleLink.File ? _fileText.Text.Trim() : null,
                Host = driver.Link == ScaleLink.Network ? _hostText.Text.Trim() : null,
                Port = driver.Link == ScaleLink.Network ? (int)_portNumber.Value : null,
                SerialPort = driver.Link == ScaleLink.Serial ? _serialText.Text.Trim().ToUpperInvariant() : null,
                BaudRate = driver.Link == ScaleLink.Serial ? (int)_baudNumber.Value : null,
                IsActive = _activeCheck.Checked,
            };
            try
            {
                SavedId = await _scaleRepository.SaveAsync(scale);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save the scale:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
