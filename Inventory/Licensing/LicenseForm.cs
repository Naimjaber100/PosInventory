namespace Inventory.Licensing
{
    // Activation: shows this PC's machine code (sent to whoever makes the licenses with the License app)
    // and takes the license key back, pasted or from a .lic file. OK = a valid license is installed.
    // Built in code, like the cashier's small windows.
    public class LicenseForm : Form
    {
        private readonly TextBox _keyText;
        private readonly Label _statusLabel;

        public LicenseForm(LicenseInfo current)
        {
            Text = "Inventory - License";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = MaximizeBox = false;
            ClientSize = new Size(640, 430);
            Font = new Font("Segoe UI", 10F);

            _statusLabel = new Label
            {
                Dock = DockStyle.Top, Height = 48, Padding = new Padding(14, 12, 14, 0),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = current.IsValid ? Color.FromArgb(28, 110, 92) : Color.Firebrick,
                Text = current.Message,
            };

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(14, 4, 14, 0) };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var codeCaption = new Label { Text = "Machine code of this PC (send it to get a license):", AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
            table.Controls.Add(codeCaption, 0, 0);
            table.SetColumnSpan(codeCaption, 2);
            var codeText = new TextBox
            {
                Text = LicenseManager.MachineCode, ReadOnly = true, Dock = DockStyle.Fill,
                Font = new Font("Consolas", 14F, FontStyle.Bold), TextAlign = HorizontalAlignment.Center,
            };
            table.Controls.Add(codeText, 0, 1);
            var copyButton = new Button { Text = "Copy", Size = new Size(110, 34) };
            copyButton.Click += (_, _) => { Clipboard.SetText(LicenseManager.MachineCode); copyButton.Text = "Copied"; };
            table.Controls.Add(copyButton, 1, 1);

            var keyCaption = new Label { Text = "License key (paste it, or load the .lic file):", AutoSize = true, Margin = new Padding(3, 14, 3, 3) };
            table.Controls.Add(keyCaption, 0, 2);
            table.SetColumnSpan(keyCaption, 2);
            _keyText = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Consolas", 9.5F) };
            table.Controls.Add(_keyText, 0, 3);
            var loadButton = new Button { Text = "Load file...", Size = new Size(110, 34), Anchor = AnchorStyles.Top };
            loadButton.Click += (_, _) => LoadFile();
            table.Controls.Add(loadButton, 1, 3);

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10, 10, 10, 8) };
            var closeButton = new Button { Text = current.IsValid ? "Close" : "Exit", Size = new Size(110, 36), DialogResult = DialogResult.Cancel };
            var activateButton = new Button
            {
                Text = "Activate", Size = new Size(140, 36), BackColor = Color.FromArgb(28, 110, 92), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            };
            activateButton.FlatAppearance.BorderSize = 0;
            activateButton.Click += (_, _) => Activate_();
            bottom.Controls.AddRange(new Control[] { closeButton, activateButton });

            Controls.Add(table);
            Controls.Add(bottom);
            Controls.Add(_statusLabel);
            AcceptButton = activateButton;
            CancelButton = closeButton;
            ActiveControl = _keyText;
        }

        private void LoadFile()
        {
            using var dialog = new OpenFileDialog { Title = "License file", Filter = "License (*.lic)|*.lic|All files (*.*)|*.*" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try { _keyText.Text = File.ReadAllText(dialog.FileName).Trim(); }
            catch (Exception ex) { MessageBox.Show(this, "Could not read the file:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void Activate_()
        {
            if (string.IsNullOrWhiteSpace(_keyText.Text))
            {
                MessageBox.Show(this, "Paste the license key first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                _keyText.Focus();
                return;
            }
            LicenseInfo info;
            try { info = LicenseManager.Install(_keyText.Text); }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save the license:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!info.IsValid)
            {
                _statusLabel.Text = info.Message;
                _statusLabel.ForeColor = Color.Firebrick;
                MessageBox.Show(this, info.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            MessageBox.Show(this, "The program is activated.\n\n" + info.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
