namespace Inventory
{
    partial class UserEditForm
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
        /// </summary>
        private void InitializeComponent()
        {
            fieldsTable = new TableLayoutPanel();
            nameCaption = new Label();
            nameText = new TextBox();
            passwordCaption = new Label();
            passwordText = new TextBox();
            confirmCaption = new Label();
            confirmText = new TextBox();
            passwordPanel = new Panel();
            passwordHint = new Label();
            showPasswordCheck = new CheckBox();
            roleCaption = new Label();
            roleCombo = new ComboBox();
            activeCheck = new CheckBox();
            buttonsPanel = new FlowLayoutPanel();
            cancelButton = new Button();
            saveButton = new Button();
            fieldsTable.SuspendLayout();
            passwordPanel.SuspendLayout();
            buttonsPanel.SuspendLayout();
            SuspendLayout();
            //
            // fieldsTable
            //
            fieldsTable.ColumnCount = 2;
            fieldsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            fieldsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            fieldsTable.Controls.Add(nameCaption, 0, 0);
            fieldsTable.Controls.Add(nameText, 1, 0);
            fieldsTable.Controls.Add(passwordCaption, 0, 1);
            fieldsTable.Controls.Add(passwordText, 1, 1);
            fieldsTable.Controls.Add(confirmCaption, 0, 2);
            fieldsTable.Controls.Add(confirmText, 1, 2);
            fieldsTable.Controls.Add(passwordPanel, 1, 3);
            fieldsTable.Controls.Add(roleCaption, 0, 4);
            fieldsTable.Controls.Add(roleCombo, 1, 4);
            fieldsTable.Controls.Add(activeCheck, 1, 5);
            fieldsTable.Dock = DockStyle.Fill;
            fieldsTable.Location = new Point(0, 0);
            fieldsTable.Name = "fieldsTable";
            fieldsTable.Padding = new Padding(12);
            fieldsTable.RowCount = 6;
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            fieldsTable.Size = new Size(560, 278);
            fieldsTable.TabIndex = 0;
            //
            // nameCaption
            //
            nameCaption.Dock = DockStyle.Fill;
            nameCaption.Name = "nameCaption";
            nameCaption.TabIndex = 0;
            nameCaption.Text = "Name";
            nameCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // nameText
            //
            nameText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            nameText.MaxLength = 50;
            nameText.Name = "nameText";
            nameText.PlaceholderText = "Full name, e.g. Rami Haddad";
            nameText.TabIndex = 1;
            //
            // passwordCaption
            //
            passwordCaption.Dock = DockStyle.Fill;
            passwordCaption.Name = "passwordCaption";
            passwordCaption.TabIndex = 4;
            passwordCaption.Text = "Password";
            passwordCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // passwordText
            //
            passwordText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            passwordText.MaxLength = 8;
            passwordText.Name = "passwordText";
            passwordText.TabIndex = 5;
            passwordText.UseSystemPasswordChar = true;
            //
            // confirmCaption
            //
            confirmCaption.Dock = DockStyle.Fill;
            confirmCaption.Name = "confirmCaption";
            confirmCaption.TabIndex = 6;
            confirmCaption.Text = "Confirm password";
            confirmCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // confirmText
            //
            confirmText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            confirmText.MaxLength = 8;
            confirmText.Name = "confirmText";
            confirmText.TabIndex = 7;
            confirmText.UseSystemPasswordChar = true;
            //
            // passwordPanel
            //
            passwordPanel.Controls.Add(passwordHint);
            passwordPanel.Controls.Add(showPasswordCheck);
            passwordPanel.Dock = DockStyle.Fill;
            passwordPanel.Margin = new Padding(3, 0, 3, 0);
            passwordPanel.Name = "passwordPanel";
            passwordPanel.TabIndex = 8;
            //
            // passwordHint
            //
            passwordHint.AutoEllipsis = true;
            passwordHint.Dock = DockStyle.Fill;
            passwordHint.Font = new Font("Segoe UI", 8.5F);
            passwordHint.ForeColor = Color.DimGray;
            passwordHint.Name = "passwordHint";
            passwordHint.TabIndex = 0;
            passwordHint.TextAlign = ContentAlignment.MiddleLeft;
            //
            // showPasswordCheck
            //
            showPasswordCheck.Dock = DockStyle.Right;
            showPasswordCheck.Name = "showPasswordCheck";
            showPasswordCheck.Size = new Size(70, 34);
            showPasswordCheck.TabIndex = 1;
            showPasswordCheck.TabStop = false;
            showPasswordCheck.Text = "Show";
            showPasswordCheck.CheckedChanged += showPasswordCheck_CheckedChanged;
            //
            // roleCaption
            //
            roleCaption.Dock = DockStyle.Fill;
            roleCaption.Name = "roleCaption";
            roleCaption.TabIndex = 9;
            roleCaption.Text = "Role";
            roleCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // roleCombo
            //
            roleCombo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            roleCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            roleCombo.Name = "roleCombo";
            roleCombo.TabIndex = 10;
            //
            // activeCheck
            //
            activeCheck.AutoSize = true;
            activeCheck.Checked = true;
            activeCheck.CheckState = CheckState.Checked;
            activeCheck.Name = "activeCheck";
            activeCheck.Padding = new Padding(0, 6, 0, 0);
            activeCheck.TabIndex = 11;
            activeCheck.Text = "Active (can log in)";
            //
            // buttonsPanel
            //
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(saveButton);
            buttonsPanel.Dock = DockStyle.Bottom;
            buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonsPanel.Location = new Point(0, 278);
            buttonsPanel.Name = "buttonsPanel";
            buttonsPanel.Padding = new Padding(8, 6, 8, 6);
            buttonsPanel.Size = new Size(560, 52);
            buttonsPanel.TabIndex = 1;
            //
            // cancelButton
            //
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(100, 36);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Cancel";
            //
            // saveButton
            //
            saveButton.BackColor = Color.FromArgb(34, 139, 117);
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.FlatStyle = FlatStyle.Flat;
            saveButton.ForeColor = Color.White;
            saveButton.Name = "saveButton";
            saveButton.Size = new Size(100, 36);
            saveButton.TabIndex = 0;
            saveButton.Text = "Save";
            saveButton.UseVisualStyleBackColor = false;
            saveButton.Click += saveButton_Click;
            //
            // UserEditForm
            //
            AcceptButton = saveButton;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(560, 330);
            Controls.Add(fieldsTable);
            Controls.Add(buttonsPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "UserEditForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "User";
            fieldsTable.ResumeLayout(false);
            fieldsTable.PerformLayout();
            passwordPanel.ResumeLayout(false);
            buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel fieldsTable;
        private Label nameCaption;
        private TextBox nameText;
        private Label passwordCaption;
        private TextBox passwordText;
        private Label confirmCaption;
        private TextBox confirmText;
        private Panel passwordPanel;
        private Label passwordHint;
        private CheckBox showPasswordCheck;
        private Label roleCaption;
        private ComboBox roleCombo;
        private CheckBox activeCheck;
        private FlowLayoutPanel buttonsPanel;
        private Button cancelButton;
        private Button saveButton;
    }
}
