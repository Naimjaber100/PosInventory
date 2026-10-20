namespace Inventory
{
    partial class CompanyForm
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
            addressCaption = new Label();
            addressText = new TextBox();
            emailCaption = new Label();
            emailText = new TextBox();
            mobileCaption = new Label();
            mobileText = new TextBox();
            vatCaption = new Label();
            vatText = new TextBox();
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
            fieldsTable.Controls.Add(addressCaption, 0, 1);
            fieldsTable.Controls.Add(addressText, 1, 1);
            fieldsTable.Controls.Add(emailCaption, 0, 2);
            fieldsTable.Controls.Add(emailText, 1, 2);
            fieldsTable.Controls.Add(mobileCaption, 0, 3);
            fieldsTable.Controls.Add(mobileText, 1, 3);
            fieldsTable.Controls.Add(vatCaption, 0, 4);
            fieldsTable.Controls.Add(vatText, 1, 4);
            fieldsTable.Dock = DockStyle.Fill;
            fieldsTable.Location = new Point(0, 0);
            fieldsTable.Name = "fieldsTable";
            fieldsTable.Padding = new Padding(12);
            fieldsTable.RowCount = 6;
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            fieldsTable.Size = new Size(560, 284);
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
            nameText.PlaceholderText = "Shown on the login and cashier screens";
            nameText.TabIndex = 1;
            //
            // addressCaption
            //
            addressCaption.Dock = DockStyle.Fill;
            addressCaption.Name = "addressCaption";
            addressCaption.Padding = new Padding(0, 8, 0, 0);
            addressCaption.TabIndex = 2;
            addressCaption.Text = "Address";
            //
            // addressText
            //
            addressText.Dock = DockStyle.Fill;
            addressText.MaxLength = 200;
            addressText.Multiline = true;
            addressText.Name = "addressText";
            addressText.ScrollBars = ScrollBars.Vertical;
            addressText.TabIndex = 3;
            //
            // emailCaption
            //
            emailCaption.Dock = DockStyle.Fill;
            emailCaption.Name = "emailCaption";
            emailCaption.TabIndex = 4;
            emailCaption.Text = "Email";
            emailCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // emailText
            //
            emailText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            emailText.MaxLength = 150;
            emailText.Name = "emailText";
            emailText.PlaceholderText = "info@company.com";
            emailText.TabIndex = 5;
            //
            // mobileCaption
            //
            mobileCaption.Dock = DockStyle.Fill;
            mobileCaption.Name = "mobileCaption";
            mobileCaption.TabIndex = 6;
            mobileCaption.Text = "Mobile number";
            mobileCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // mobileText
            //
            mobileText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            mobileText.MaxLength = 16;
            mobileText.Name = "mobileText";
            mobileText.PlaceholderText = "+961 ...";
            mobileText.TabIndex = 7;
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
            vatText.TabIndex = 9;
            //
            // buttonsPanel
            //
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(saveButton);
            buttonsPanel.Dock = DockStyle.Bottom;
            buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonsPanel.Location = new Point(0, 284);
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
            // CompanyForm
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(560, 336);
            Controls.Add(fieldsTable);
            Controls.Add(buttonsPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CompanyForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Company";
            Load += CompanyForm_Load;
            fieldsTable.ResumeLayout(false);
            fieldsTable.PerformLayout();
            buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel fieldsTable;
        private Label nameCaption;
        private TextBox nameText;
        private Label addressCaption;
        private TextBox addressText;
        private Label emailCaption;
        private TextBox emailText;
        private Label mobileCaption;
        private TextBox mobileText;
        private Label vatCaption;
        private TextBox vatText;
        private FlowLayoutPanel buttonsPanel;
        private Button cancelButton;
        private Button saveButton;
    }
}
