namespace Inventory
{
    partial class ReceiptPrinterForm
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
            computerCaption = new Label();
            computerLabel = new Label();
            printerCaption = new Label();
            printerCombo = new ComboBox();
            printerStatusLabel = new Label();
            paperCaption = new Label();
            paperCombo = new ComboBox();
            copiesCaption = new Label();
            copiesInput = new NumericUpDown();
            autoCheck = new CheckBox();
            footerCaption = new Label();
            footerText = new TextBox();
            buttonsPanel = new FlowLayoutPanel();
            cancelButton = new Button();
            saveButton = new Button();
            testButton = new Button();
            previewButton = new Button();
            fieldsTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)copiesInput).BeginInit();
            buttonsPanel.SuspendLayout();
            SuspendLayout();
            //
            // fieldsTable
            //
            fieldsTable.ColumnCount = 2;
            fieldsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            fieldsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            fieldsTable.Controls.Add(computerCaption, 0, 0);
            fieldsTable.Controls.Add(computerLabel, 1, 0);
            fieldsTable.Controls.Add(printerCaption, 0, 1);
            fieldsTable.Controls.Add(printerCombo, 1, 1);
            fieldsTable.Controls.Add(printerStatusLabel, 1, 2);
            fieldsTable.Controls.Add(paperCaption, 0, 3);
            fieldsTable.Controls.Add(paperCombo, 1, 3);
            fieldsTable.Controls.Add(copiesCaption, 0, 4);
            fieldsTable.Controls.Add(copiesInput, 1, 4);
            fieldsTable.Controls.Add(autoCheck, 1, 5);
            fieldsTable.Controls.Add(footerCaption, 0, 6);
            fieldsTable.Controls.Add(footerText, 1, 6);
            fieldsTable.Dock = DockStyle.Fill;
            fieldsTable.Location = new Point(0, 0);
            fieldsTable.Name = "fieldsTable";
            fieldsTable.Padding = new Padding(12);
            fieldsTable.RowCount = 7;
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            fieldsTable.Size = new Size(620, 368);
            fieldsTable.TabIndex = 0;
            //
            // computerCaption
            //
            computerCaption.Dock = DockStyle.Fill;
            computerCaption.Name = "computerCaption";
            computerCaption.TabIndex = 0;
            computerCaption.Text = "This computer";
            computerCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // computerLabel
            //
            computerLabel.Dock = DockStyle.Fill;
            computerLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            computerLabel.Name = "computerLabel";
            computerLabel.TabIndex = 1;
            computerLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // printerCaption
            //
            printerCaption.Dock = DockStyle.Fill;
            printerCaption.Name = "printerCaption";
            printerCaption.TabIndex = 2;
            printerCaption.Text = "Receipt printer";
            printerCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // printerCombo
            //
            printerCombo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            printerCombo.Name = "printerCombo";
            printerCombo.TabIndex = 3;
            printerCombo.TextChanged += printerCombo_TextChanged;
            //
            // printerStatusLabel
            //
            printerStatusLabel.Dock = DockStyle.Fill;
            printerStatusLabel.Font = new Font("Segoe UI", 8.5F);
            printerStatusLabel.ForeColor = Color.DimGray;
            printerStatusLabel.Name = "printerStatusLabel";
            printerStatusLabel.TabIndex = 4;
            //
            // paperCaption
            //
            paperCaption.Dock = DockStyle.Fill;
            paperCaption.Name = "paperCaption";
            paperCaption.TabIndex = 5;
            paperCaption.Text = "Paper width";
            paperCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // paperCombo
            //
            paperCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            paperCombo.Items.AddRange(new object[] { "80 mm", "58 mm" });
            paperCombo.Name = "paperCombo";
            paperCombo.Size = new Size(120, 28);
            paperCombo.TabIndex = 6;
            //
            // copiesCaption
            //
            copiesCaption.Dock = DockStyle.Fill;
            copiesCaption.Name = "copiesCaption";
            copiesCaption.TabIndex = 7;
            copiesCaption.Text = "Copies";
            copiesCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // copiesInput
            //
            copiesInput.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            copiesInput.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            copiesInput.Name = "copiesInput";
            copiesInput.Size = new Size(80, 27);
            copiesInput.TabIndex = 8;
            copiesInput.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // autoCheck
            //
            autoCheck.AutoSize = true;
            autoCheck.Checked = true;
            autoCheck.CheckState = CheckState.Checked;
            autoCheck.Name = "autoCheck";
            autoCheck.Padding = new Padding(0, 6, 0, 0);
            autoCheck.TabIndex = 9;
            autoCheck.Text = "Print automatically after each sale and refund";
            //
            // footerCaption
            //
            footerCaption.Dock = DockStyle.Fill;
            footerCaption.Name = "footerCaption";
            footerCaption.Padding = new Padding(0, 8, 0, 0);
            footerCaption.TabIndex = 10;
            footerCaption.Text = "Footer text";
            //
            // footerText
            //
            footerText.Dock = DockStyle.Fill;
            footerText.MaxLength = 200;
            footerText.Multiline = true;
            footerText.Name = "footerText";
            footerText.PlaceholderText = "e.g. Thank you for shopping with us (same on every computer)";
            footerText.TabIndex = 11;
            //
            // buttonsPanel
            //
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(saveButton);
            buttonsPanel.Controls.Add(testButton);
            buttonsPanel.Controls.Add(previewButton);
            buttonsPanel.Dock = DockStyle.Bottom;
            buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonsPanel.Location = new Point(0, 368);
            buttonsPanel.Name = "buttonsPanel";
            buttonsPanel.Padding = new Padding(8, 6, 8, 6);
            buttonsPanel.Size = new Size(620, 52);
            buttonsPanel.TabIndex = 1;
            //
            // cancelButton
            //
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(100, 36);
            cancelButton.TabIndex = 3;
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
            saveButton.TabIndex = 2;
            saveButton.Text = "Save";
            saveButton.UseVisualStyleBackColor = false;
            saveButton.Click += saveButton_Click;
            //
            // testButton
            //
            testButton.Name = "testButton";
            testButton.Size = new Size(110, 36);
            testButton.TabIndex = 1;
            testButton.Text = "Test print";
            testButton.Click += testButton_Click;
            //
            // previewButton
            //
            previewButton.Name = "previewButton";
            previewButton.Size = new Size(110, 36);
            previewButton.TabIndex = 0;
            previewButton.Text = "Preview";
            previewButton.Click += previewButton_Click;
            //
            // ReceiptPrinterForm
            //
            AcceptButton = saveButton;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(620, 420);
            Controls.Add(fieldsTable);
            Controls.Add(buttonsPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ReceiptPrinterForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Receipt printer";
            Load += ReceiptPrinterForm_Load;
            fieldsTable.ResumeLayout(false);
            fieldsTable.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)copiesInput).EndInit();
            buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel fieldsTable;
        private Label computerCaption;
        private Label computerLabel;
        private Label printerCaption;
        private ComboBox printerCombo;
        private Label printerStatusLabel;
        private Label paperCaption;
        private ComboBox paperCombo;
        private Label copiesCaption;
        private NumericUpDown copiesInput;
        private CheckBox autoCheck;
        private Label footerCaption;
        private TextBox footerText;
        private FlowLayoutPanel buttonsPanel;
        private Button cancelButton;
        private Button saveButton;
        private Button testButton;
        private Button previewButton;
    }
}
