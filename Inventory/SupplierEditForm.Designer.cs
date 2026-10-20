namespace Inventory
{
    partial class SupplierEditForm
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
            vatCaption = new Label();
            vatText = new TextBox();
            balanceCaption = new Label();
            balanceLabel = new Label();
            notesCaption = new Label();
            notesText = new TextBox();
            activeCheck = new CheckBox();
            buttonsPanel = new FlowLayoutPanel();
            cancelButton = new Button();
            saveButton = new Button();
            fieldsTable.SuspendLayout();
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
            fieldsTable.Controls.Add(vatCaption, 0, 4);
            fieldsTable.Controls.Add(vatText, 1, 4);
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
            nameCaption.Name = "nameCaption";
            nameCaption.TabIndex = 0;
            nameCaption.Text = "Name *";
            nameCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // nameText
            //
            nameText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            nameText.MaxLength = 50;
            nameText.Name = "nameText";
            nameText.PlaceholderText = "Supplier or company name";
            nameText.TabIndex = 1;
            //
            // phoneCaption
            //
            phoneCaption.Dock = DockStyle.Fill;
            phoneCaption.Name = "phoneCaption";
            phoneCaption.TabIndex = 2;
            phoneCaption.Text = "Phone";
            phoneCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // phoneText
            //
            phoneText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            phoneText.MaxLength = 30;
            phoneText.Name = "phoneText";
            phoneText.PlaceholderText = "e.g. 03123456";
            phoneText.TabIndex = 3;
            //
            // addressCaption
            //
            addressCaption.Dock = DockStyle.Fill;
            addressCaption.Name = "addressCaption";
            addressCaption.TabIndex = 4;
            addressCaption.Text = "Address";
            addressCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // addressText
            //
            addressText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            addressText.MaxLength = 200;
            addressText.Name = "addressText";
            addressText.TabIndex = 5;
            //
            // emailCaption
            //
            emailCaption.Dock = DockStyle.Fill;
            emailCaption.Name = "emailCaption";
            emailCaption.TabIndex = 6;
            emailCaption.Text = "Email";
            emailCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // emailText
            //
            emailText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            emailText.MaxLength = 150;
            emailText.Name = "emailText";
            emailText.TabIndex = 7;
            //
            // vatCaption
            //
            vatCaption.Dock = DockStyle.Fill;
            vatCaption.Name = "vatCaption";
            vatCaption.TabIndex = 8;
            vatCaption.Text = "VAT number";
            vatCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // vatText
            //
            vatText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            vatText.MaxLength = 50;
            vatText.Name = "vatText";
            vatText.PlaceholderText = "Supplier VAT / MOF registration number";
            vatText.TabIndex = 9;
            //
            // balanceCaption
            //
            balanceCaption.Dock = DockStyle.Fill;
            balanceCaption.Name = "balanceCaption";
            balanceCaption.TabIndex = 10;
            balanceCaption.Text = "We owe";
            balanceCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // balanceLabel
            //
            balanceLabel.AutoEllipsis = true;
            balanceLabel.Dock = DockStyle.Fill;
            balanceLabel.ForeColor = Color.DimGray;
            balanceLabel.Name = "balanceLabel";
            balanceLabel.TabIndex = 11;
            balanceLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // notesCaption
            //
            notesCaption.Dock = DockStyle.Fill;
            notesCaption.Name = "notesCaption";
            notesCaption.Padding = new Padding(0, 8, 0, 0);
            notesCaption.TabIndex = 12;
            notesCaption.Text = "Notes";
            //
            // notesText
            //
            notesText.Dock = DockStyle.Fill;
            notesText.MaxLength = 500;
            notesText.Multiline = true;
            notesText.Name = "notesText";
            notesText.ScrollBars = ScrollBars.Vertical;
            notesText.TabIndex = 13;
            //
            // activeCheck
            //
            activeCheck.AutoSize = true;
            activeCheck.Checked = true;
            activeCheck.CheckState = CheckState.Checked;
            activeCheck.Name = "activeCheck";
            activeCheck.Padding = new Padding(0, 6, 0, 0);
            activeCheck.TabIndex = 14;
            activeCheck.Text = "Active (can be picked on purchases)";
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
            // SupplierEditForm
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
            Name = "SupplierEditForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Supplier";
            fieldsTable.ResumeLayout(false);
            fieldsTable.PerformLayout();
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
        private Label vatCaption;
        private TextBox vatText;
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
