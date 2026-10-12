namespace Inventory
{
    partial class CustomerEditForm
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
            phoneCaption = new Label();
            phoneText = new TextBox();
            addressCaption = new Label();
            addressText = new TextBox();
            emailCaption = new Label();
            emailText = new TextBox();
            limitCaption = new Label();
            limitPanel = new FlowLayoutPanel();
            limitText = new TextBox();
            noLimitCheck = new CheckBox();
            balanceCaption = new Label();
            balanceLabel = new Label();
            notesCaption = new Label();
            notesText = new TextBox();
            activeCheck = new CheckBox();
            buttonsPanel = new FlowLayoutPanel();
            cancelButton = new Button();
            saveButton = new Button();
            fieldsTable.SuspendLayout();
            limitPanel.SuspendLayout();
            buttonsPanel.SuspendLayout();
            SuspendLayout();
            // 
            // fieldsTable
            // 
            fieldsTable.ColumnCount = 2;
            fieldsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            fieldsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            fieldsTable.Controls.Add(nameCaption, 0, 0);
            fieldsTable.Controls.Add(nameText, 1, 0);
            fieldsTable.Controls.Add(phoneCaption, 0, 1);
            fieldsTable.Controls.Add(phoneText, 1, 1);
            fieldsTable.Controls.Add(addressCaption, 0, 2);
            fieldsTable.Controls.Add(addressText, 1, 2);
            fieldsTable.Controls.Add(emailCaption, 0, 3);
            fieldsTable.Controls.Add(emailText, 1, 3);
            fieldsTable.Controls.Add(limitCaption, 0, 4);
            fieldsTable.Controls.Add(limitPanel, 1, 4);
            fieldsTable.Controls.Add(balanceCaption, 0, 5);
            fieldsTable.Controls.Add(balanceLabel, 1, 5);
            fieldsTable.Controls.Add(notesCaption, 0, 6);
            fieldsTable.Controls.Add(notesText, 1, 6);
            fieldsTable.Controls.Add(activeCheck, 1, 7);
            fieldsTable.Dock = DockStyle.Fill;
            fieldsTable.Location = new Point(0, 0);
            fieldsTable.Name = "fieldsTable";
            fieldsTable.Padding = new Padding(12);
            fieldsTable.RowCount = 8;
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            fieldsTable.Size = new Size(580, 408);
            fieldsTable.TabIndex = 0;
            // 
            // nameCaption
            // 
            nameCaption.Dock = DockStyle.Fill;
            nameCaption.Location = new Point(15, 12);
            nameCaption.Name = "nameCaption";
            nameCaption.Size = new Size(124, 42);
            nameCaption.TabIndex = 0;
            nameCaption.Text = "Name *";
            nameCaption.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // nameText
            // 
            nameText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            nameText.Location = new Point(145, 19);
            nameText.MaxLength = 150;
            nameText.Name = "nameText";
            nameText.PlaceholderText = "Client or company name";
            nameText.Size = new Size(420, 27);
            nameText.TabIndex = 1;
            // 
            // phoneCaption
            // 
            phoneCaption.Dock = DockStyle.Fill;
            phoneCaption.Location = new Point(15, 54);
            phoneCaption.Name = "phoneCaption";
            phoneCaption.Size = new Size(124, 42);
            phoneCaption.TabIndex = 2;
            phoneCaption.Text = "Phone";
            phoneCaption.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // phoneText
            // 
            phoneText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            phoneText.Location = new Point(145, 61);
            phoneText.MaxLength = 30;
            phoneText.Name = "phoneText";
            phoneText.PlaceholderText = "e.g. 03 123 456";
            phoneText.Size = new Size(420, 27);
            phoneText.TabIndex = 3;
            // 
            // addressCaption
            // 
            addressCaption.Dock = DockStyle.Fill;
            addressCaption.Location = new Point(15, 96);
            addressCaption.Name = "addressCaption";
            addressCaption.Size = new Size(124, 42);
            addressCaption.TabIndex = 4;
            addressCaption.Text = "Address";
            addressCaption.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // addressText
            // 
            addressText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            addressText.Location = new Point(145, 103);
            addressText.MaxLength = 250;
            addressText.Name = "addressText";
            addressText.Size = new Size(420, 27);
            addressText.TabIndex = 5;
            addressText.TextChanged += addressText_TextChanged;
            // 
            // emailCaption
            // 
            emailCaption.Dock = DockStyle.Fill;
            emailCaption.Location = new Point(15, 138);
            emailCaption.Name = "emailCaption";
            emailCaption.Size = new Size(124, 42);
            emailCaption.TabIndex = 6;
            emailCaption.Text = "Email";
            emailCaption.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // emailText
            // 
            emailText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            emailText.Location = new Point(145, 145);
            emailText.MaxLength = 150;
            emailText.Name = "emailText";
            emailText.Size = new Size(420, 27);
            emailText.TabIndex = 7;
            // 
            // limitCaption
            // 
            limitCaption.Dock = DockStyle.Fill;
            limitCaption.Location = new Point(15, 180);
            limitCaption.Name = "limitCaption";
            limitCaption.Size = new Size(124, 42);
            limitCaption.TabIndex = 8;
            limitCaption.Text = "Credit limit";
            limitCaption.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // limitPanel
            // 
            limitPanel.Controls.Add(limitText);
            limitPanel.Controls.Add(noLimitCheck);
            limitPanel.Dock = DockStyle.Fill;
            limitPanel.Location = new Point(142, 180);
            limitPanel.Margin = new Padding(0);
            limitPanel.Name = "limitPanel";
            limitPanel.Size = new Size(426, 42);
            limitPanel.TabIndex = 9;
            limitPanel.WrapContents = false;
            // 
            // limitText
            // 
            limitText.Location = new Point(3, 7);
            limitText.Margin = new Padding(3, 7, 3, 3);
            limitText.Name = "limitText";
            limitText.Size = new Size(200, 27);
            limitText.TabIndex = 0;
            limitText.TextAlign = HorizontalAlignment.Right;
            // 
            // noLimitCheck
            // 
            noLimitCheck.AutoSize = true;
            noLimitCheck.Location = new Point(218, 10);
            noLimitCheck.Margin = new Padding(12, 10, 3, 3);
            noLimitCheck.Name = "noLimitCheck";
            noLimitCheck.Size = new Size(85, 24);
            noLimitCheck.TabIndex = 1;
            noLimitCheck.Text = "No limit";
            noLimitCheck.CheckedChanged += noLimitCheck_CheckedChanged;
            // 
            // balanceCaption
            // 
            balanceCaption.Dock = DockStyle.Fill;
            balanceCaption.Location = new Point(15, 222);
            balanceCaption.Name = "balanceCaption";
            balanceCaption.Size = new Size(124, 34);
            balanceCaption.TabIndex = 10;
            balanceCaption.Text = "Owes us";
            balanceCaption.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // balanceLabel
            // 
            balanceLabel.AutoEllipsis = true;
            balanceLabel.Dock = DockStyle.Fill;
            balanceLabel.ForeColor = Color.DimGray;
            balanceLabel.Location = new Point(145, 222);
            balanceLabel.Name = "balanceLabel";
            balanceLabel.Size = new Size(420, 34);
            balanceLabel.TabIndex = 11;
            balanceLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // notesCaption
            // 
            notesCaption.Dock = DockStyle.Fill;
            notesCaption.Location = new Point(15, 256);
            notesCaption.Name = "notesCaption";
            notesCaption.Padding = new Padding(0, 8, 0, 0);
            notesCaption.Size = new Size(124, 80);
            notesCaption.TabIndex = 12;
            notesCaption.Text = "Notes";
            // 
            // notesText
            // 
            notesText.Dock = DockStyle.Fill;
            notesText.Location = new Point(145, 259);
            notesText.MaxLength = 500;
            notesText.Multiline = true;
            notesText.Name = "notesText";
            notesText.ScrollBars = ScrollBars.Vertical;
            notesText.Size = new Size(420, 74);
            notesText.TabIndex = 13;
            // 
            // activeCheck
            // 
            activeCheck.AutoSize = true;
            activeCheck.Checked = true;
            activeCheck.CheckState = CheckState.Checked;
            activeCheck.Location = new Point(145, 339);
            activeCheck.Name = "activeCheck";
            activeCheck.Padding = new Padding(0, 6, 0, 0);
            activeCheck.Size = new Size(235, 30);
            activeCheck.TabIndex = 14;
            activeCheck.Text = "Active (can be picked on sales)";
            // 
            // buttonsPanel
            // 
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(saveButton);
            buttonsPanel.Dock = DockStyle.Bottom;
            buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonsPanel.Location = new Point(0, 408);
            buttonsPanel.Name = "buttonsPanel";
            buttonsPanel.Padding = new Padding(8, 6, 8, 6);
            buttonsPanel.Size = new Size(580, 52);
            buttonsPanel.TabIndex = 1;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(461, 9);
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
            saveButton.Location = new Point(355, 9);
            saveButton.Name = "saveButton";
            saveButton.Size = new Size(100, 36);
            saveButton.TabIndex = 0;
            saveButton.Text = "Save";
            saveButton.UseVisualStyleBackColor = false;
            saveButton.Click += saveButton_Click;
            // 
            // CustomerEditForm
            // 
            AcceptButton = saveButton;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(580, 460);
            Controls.Add(fieldsTable);
            Controls.Add(buttonsPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CustomerEditForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Client";
            fieldsTable.ResumeLayout(false);
            fieldsTable.PerformLayout();
            limitPanel.ResumeLayout(false);
            limitPanel.PerformLayout();
            buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel fieldsTable;
        private Label nameCaption;
        private TextBox nameText;
        private Label phoneCaption;
        private TextBox phoneText;
        private Label addressCaption;
        private TextBox addressText;
        private Label emailCaption;
        private TextBox emailText;
        private Label limitCaption;
        private FlowLayoutPanel limitPanel;
        private TextBox limitText;
        private CheckBox noLimitCheck;
        private Label balanceCaption;
        private Label balanceLabel;
        private Label notesCaption;
        private TextBox notesText;
        private CheckBox activeCheck;
        private FlowLayoutPanel buttonsPanel;
        private Button cancelButton;
        private Button saveButton;
    }
}
