namespace Inventory
{
    partial class LoginForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// The keypad buttons are created in code (LoginForm.BuildKeypad).
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            headerPanel = new Panel();
            clockLabel = new Label();
            storeLabel = new Label();
            bodyPanel = new Panel();
            loginButton = new Button();
            keypadTable = new TableLayoutPanel();
            statusLabel = new Label();
            confirmPanel = new Panel();
            confirmText = new TextBox();
            confirmLabel = new Label();
            passwordPanel = new Panel();
            passwordText = new TextBox();
            hintLabel = new Label();
            titleLabel = new Label();
            exitButton = new Button();
            clockTimer = new System.Windows.Forms.Timer(components);
            headerPanel.SuspendLayout();
            bodyPanel.SuspendLayout();
            confirmPanel.SuspendLayout();
            passwordPanel.SuspendLayout();
            SuspendLayout();
            // 
            // headerPanel
            // 
            headerPanel.BackColor = Color.FromArgb(22, 32, 43);
            headerPanel.Controls.Add(clockLabel);
            headerPanel.Controls.Add(storeLabel);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Location = new Point(0, 0);
            headerPanel.Name = "headerPanel";
            headerPanel.Padding = new Padding(24, 14, 24, 10);
            headerPanel.Size = new Size(440, 92);
            headerPanel.TabIndex = 0;
            // 
            // clockLabel
            // 
            clockLabel.Dock = DockStyle.Top;
            clockLabel.Font = new Font("Segoe UI", 10F);
            clockLabel.ForeColor = Color.Gainsboro;
            clockLabel.Location = new Point(24, 54);
            clockLabel.Name = "clockLabel";
            clockLabel.Size = new Size(392, 26);
            clockLabel.TabIndex = 1;
            // 
            // storeLabel
            // 
            storeLabel.AutoEllipsis = true;
            storeLabel.Dock = DockStyle.Top;
            storeLabel.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            storeLabel.ForeColor = Color.White;
            storeLabel.Location = new Point(24, 14);
            storeLabel.Name = "storeLabel";
            storeLabel.Size = new Size(392, 40);
            storeLabel.TabIndex = 0;
            storeLabel.Text = "My Store";
            // 
            // bodyPanel
            // 
            bodyPanel.BackColor = Color.White;
            bodyPanel.Controls.Add(loginButton);
            bodyPanel.Controls.Add(keypadTable);
            bodyPanel.Controls.Add(statusLabel);
            bodyPanel.Controls.Add(confirmPanel);
            bodyPanel.Controls.Add(passwordPanel);
            bodyPanel.Controls.Add(hintLabel);
            bodyPanel.Controls.Add(titleLabel);
            bodyPanel.Dock = DockStyle.Fill;
            bodyPanel.Location = new Point(0, 92);
            bodyPanel.Name = "bodyPanel";
            bodyPanel.Padding = new Padding(28, 16, 28, 8);
            bodyPanel.Size = new Size(440, 640);
            bodyPanel.TabIndex = 1;
            // 
            // loginButton
            // 
            loginButton.BackColor = Color.FromArgb(34, 139, 117);
            loginButton.Dock = DockStyle.Top;
            loginButton.FlatAppearance.BorderSize = 0;
            loginButton.FlatStyle = FlatStyle.Flat;
            loginButton.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
            loginButton.ForeColor = Color.White;
            loginButton.Location = new Point(28, 566);
            loginButton.Name = "loginButton";
            loginButton.Size = new Size(384, 60);
            loginButton.TabIndex = 2;
            loginButton.Text = "Log in";
            loginButton.UseVisualStyleBackColor = false;
            loginButton.Click += loginButton_Click;
            // 
            // keypadTable
            // 
            keypadTable.ColumnCount = 3;
            keypadTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            keypadTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            keypadTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            keypadTable.Dock = DockStyle.Top;
            keypadTable.Location = new Point(28, 276);
            keypadTable.Name = "keypadTable";
            keypadTable.Padding = new Padding(0, 0, 0, 10);
            keypadTable.RowCount = 4;
            keypadTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            keypadTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            keypadTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            keypadTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            keypadTable.Size = new Size(384, 290);
            keypadTable.TabIndex = 3;
            // 
            // statusLabel
            // 
            statusLabel.Dock = DockStyle.Top;
            statusLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            statusLabel.Location = new Point(28, 236);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(384, 40);
            statusLabel.TabIndex = 4;
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // confirmPanel
            // 
            confirmPanel.Controls.Add(confirmText);
            confirmPanel.Controls.Add(confirmLabel);
            confirmPanel.Dock = DockStyle.Top;
            confirmPanel.Location = new Point(28, 158);
            confirmPanel.Name = "confirmPanel";
            confirmPanel.Size = new Size(384, 78);
            confirmPanel.TabIndex = 1;
            confirmPanel.Visible = false;
            // 
            // confirmText
            // 
            confirmText.Dock = DockStyle.Top;
            confirmText.Font = new Font("Segoe UI", 20F);
            confirmText.Location = new Point(0, 26);
            confirmText.MaxLength = 100;
            confirmText.Name = "confirmText";
            confirmText.PlaceholderText = "Type it again";
            confirmText.Size = new Size(384, 52);
            confirmText.TabIndex = 0;
            confirmText.TextAlign = HorizontalAlignment.Center;
            confirmText.UseSystemPasswordChar = true;
            confirmText.Enter += entry_Enter;
            confirmText.KeyDown += confirmText_KeyDown;
            // 
            // confirmLabel
            // 
            confirmLabel.Dock = DockStyle.Top;
            confirmLabel.ForeColor = Color.DimGray;
            confirmLabel.Location = new Point(0, 0);
            confirmLabel.Name = "confirmLabel";
            confirmLabel.Size = new Size(384, 26);
            confirmLabel.TabIndex = 1;
            confirmLabel.Text = "Confirm the password";
            confirmLabel.TextAlign = ContentAlignment.BottomLeft;
            // 
            // passwordPanel
            // 
            passwordPanel.Controls.Add(passwordText);
            passwordPanel.Dock = DockStyle.Top;
            passwordPanel.Location = new Point(28, 100);
            passwordPanel.Name = "passwordPanel";
            passwordPanel.Padding = new Padding(0, 6, 0, 0);
            passwordPanel.Size = new Size(384, 58);
            passwordPanel.TabIndex = 0;
            // 
            // passwordText
            // 
            passwordText.Dock = DockStyle.Top;
            passwordText.Font = new Font("Segoe UI", 20F);
            passwordText.Location = new Point(0, 6);
            passwordText.MaxLength = 100;
            passwordText.Name = "passwordText";
            passwordText.PlaceholderText = "Password";
            passwordText.Size = new Size(384, 52);
            passwordText.TabIndex = 0;
            passwordText.TextAlign = HorizontalAlignment.Center;
            passwordText.UseSystemPasswordChar = true;
            passwordText.Enter += entry_Enter;
            passwordText.KeyDown += passwordText_KeyDown;
            // 
            // hintLabel
            // 
            hintLabel.Dock = DockStyle.Top;
            hintLabel.ForeColor = Color.DimGray;
            hintLabel.Location = new Point(28, 54);
            hintLabel.Name = "hintLabel";
            hintLabel.Size = new Size(384, 46);
            hintLabel.TabIndex = 5;
            hintLabel.Text = "Connecting...";
            hintLabel.Click += hintLabel_Click;
            // 
            // titleLabel
            // 
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(22, 32, 43);
            titleLabel.Location = new Point(28, 16);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(384, 38);
            titleLabel.TabIndex = 6;
            titleLabel.Text = "Enter your password";
            // 
            // exitButton
            // 
            exitButton.BackColor = Color.White;
            exitButton.Dock = DockStyle.Bottom;
            exitButton.FlatAppearance.BorderSize = 0;
            exitButton.FlatStyle = FlatStyle.Flat;
            exitButton.ForeColor = Color.DimGray;
            exitButton.Location = new Point(0, 732);
            exitButton.Name = "exitButton";
            exitButton.Size = new Size(440, 40);
            exitButton.TabIndex = 2;
            exitButton.TabStop = false;
            exitButton.Text = "Exit the program";
            exitButton.UseVisualStyleBackColor = false;
            exitButton.Click += exitButton_Click;
            // 
            // clockTimer
            // 
            clockTimer.Interval = 1000;
            clockTimer.Tick += clockTimer_Tick;
            // 
            // LoginForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(440, 772);
            Controls.Add(bodyPanel);
            Controls.Add(exitButton);
            Controls.Add(headerPanel);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Log in";
            Load += LoginForm_Load;
            headerPanel.ResumeLayout(false);
            bodyPanel.ResumeLayout(false);
            confirmPanel.ResumeLayout(false);
            confirmPanel.PerformLayout();
            passwordPanel.ResumeLayout(false);
            passwordPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel headerPanel;
        private Label clockLabel;
        private Label storeLabel;
        private Panel bodyPanel;
        private Button loginButton;
        private TableLayoutPanel keypadTable;
        private Label statusLabel;
        private Panel confirmPanel;
        private TextBox confirmText;
        private Label confirmLabel;
        private Panel passwordPanel;
        private TextBox passwordText;
        private Label hintLabel;
        private Label titleLabel;
        private Button exitButton;
        private System.Windows.Forms.Timer clockTimer;
    }
}
