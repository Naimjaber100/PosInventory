namespace Inventory
{
    partial class PurchaseEditForm
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
            headerGroup = new GroupBox();
            numberLabel = new Label();
            numberValue = new Label();
            supplierInvoiceLabel = new Label();
            supplierInvoiceText = new TextBox();
            dateLabel = new Label();
            dateInput = new DateTimePicker();
            supplierLabel = new Label();
            supplierValue = new Label();
            statusLabel = new Label();
            statusValue = new Label();
            notesLabel = new Label();
            notesText = new TextBox();
            subTotalLabel = new Label();
            subTotalValue = new Label();
            discountLabel = new Label();
            discountInput = new NumericUpDown();
            totalLabel = new Label();
            totalValue = new Label();
            itemsGroup = new GroupBox();
            linesGrid = new DataGridView();
            scanPanel = new Panel();
            scanLabel = new Label();
            scanBox = new TextBox();
            addLineButton = new Button();
            removeLineButton = new Button();
            aiButton = new Button();
            voiceButton = new Button();
            hintLabel = new Label();
            buttonsPanel = new FlowLayoutPanel();
            cancelButton = new Button();
            postButton = new Button();
            saveDraftButton = new Button();
            headerGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)discountInput).BeginInit();
            itemsGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)linesGrid).BeginInit();
            scanPanel.SuspendLayout();
            buttonsPanel.SuspendLayout();
            SuspendLayout();
            // 
            // headerGroup
            // 
            headerGroup.Controls.Add(numberLabel);
            headerGroup.Controls.Add(numberValue);
            headerGroup.Controls.Add(supplierInvoiceLabel);
            headerGroup.Controls.Add(supplierInvoiceText);
            headerGroup.Controls.Add(dateLabel);
            headerGroup.Controls.Add(dateInput);
            headerGroup.Controls.Add(supplierLabel);
            headerGroup.Controls.Add(supplierValue);
            headerGroup.Controls.Add(statusLabel);
            headerGroup.Controls.Add(statusValue);
            headerGroup.Controls.Add(notesLabel);
            headerGroup.Controls.Add(notesText);
            headerGroup.Controls.Add(subTotalLabel);
            headerGroup.Controls.Add(subTotalValue);
            headerGroup.Controls.Add(discountLabel);
            headerGroup.Controls.Add(discountInput);
            headerGroup.Controls.Add(totalLabel);
            headerGroup.Controls.Add(totalValue);
            headerGroup.Dock = DockStyle.Top;
            headerGroup.Font = new Font("Segoe UI", 9.5F);
            headerGroup.Location = new Point(10, 10);
            headerGroup.Name = "headerGroup";
            headerGroup.Padding = new Padding(10);
            headerGroup.Size = new Size(1180, 150);
            headerGroup.TabIndex = 0;
            headerGroup.TabStop = false;
            headerGroup.Text = "Invoice";
            // 
            // numberLabel
            // 
            numberLabel.AutoSize = true;
            numberLabel.Location = new Point(16, 34);
            numberLabel.Name = "numberLabel";
            numberLabel.Size = new Size(68, 21);
            numberLabel.TabIndex = 0;
            numberLabel.Text = "Number";
            // 
            // numberValue
            // 
            numberValue.AutoSize = true;
            numberValue.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            numberValue.Location = new Point(170, 34);
            numberValue.Name = "numberValue";
            numberValue.Size = new Size(159, 21);
            numberValue.TabIndex = 1;
            numberValue.Text = "(given when posted)";
            // 
            // supplierInvoiceLabel
            // 
            supplierInvoiceLabel.AutoSize = true;
            supplierInvoiceLabel.Location = new Point(16, 72);
            supplierInvoiceLabel.Name = "supplierInvoiceLabel";
            supplierInvoiceLabel.Size = new Size(146, 21);
            supplierInvoiceLabel.TabIndex = 2;
            supplierInvoiceLabel.Text = "Supplier invoice no.";
            // 
            // supplierInvoiceText
            // 
            supplierInvoiceText.Location = new Point(170, 68);
            supplierInvoiceText.MaxLength = 50;
            supplierInvoiceText.Name = "supplierInvoiceText";
            supplierInvoiceText.Size = new Size(220, 29);
            supplierInvoiceText.TabIndex = 0;
            supplierInvoiceText.TextChanged += headerField_Changed;
            // 
            // dateLabel
            // 
            dateLabel.AutoSize = true;
            dateLabel.Location = new Point(16, 110);
            dateLabel.Name = "dateLabel";
            dateLabel.Size = new Size(93, 21);
            dateLabel.TabIndex = 3;
            dateLabel.Text = "Invoice date";
            // 
            // dateInput
            // 
            dateInput.CustomFormat = "yyyy-MM-dd";
            dateInput.Format = DateTimePickerFormat.Custom;
            dateInput.Location = new Point(170, 106);
            dateInput.Name = "dateInput";
            dateInput.Size = new Size(220, 29);
            dateInput.TabIndex = 1;
            dateInput.ValueChanged += headerField_Changed;
            // 
            // supplierLabel
            // 
            supplierLabel.AutoSize = true;
            supplierLabel.Location = new Point(420, 34);
            supplierLabel.Name = "supplierLabel";
            supplierLabel.Size = new Size(68, 21);
            supplierLabel.TabIndex = 4;
            supplierLabel.Text = "Supplier";
            // 
            // supplierValue
            // 
            supplierValue.AutoSize = true;
            supplierValue.ForeColor = Color.DimGray;
            supplierValue.Location = new Point(500, 34);
            supplierValue.Name = "supplierValue";
            supplierValue.Size = new Size(350, 21);
            supplierValue.TabIndex = 5;
            supplierValue.Text = "(optional for now - added with the supplier form)";
            // 
            // statusLabel
            // 
            statusLabel.AutoSize = true;
            statusLabel.Location = new Point(420, 72);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(52, 21);
            statusLabel.TabIndex = 6;
            statusLabel.Text = "Status";
            // 
            // statusValue
            // 
            statusValue.AutoSize = true;
            statusValue.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            statusValue.Location = new Point(500, 72);
            statusValue.Name = "statusValue";
            statusValue.Size = new Size(43, 21);
            statusValue.TabIndex = 7;
            statusValue.Text = "New";
            // 
            // notesLabel
            // 
            notesLabel.AutoSize = true;
            notesLabel.Location = new Point(420, 110);
            notesLabel.Name = "notesLabel";
            notesLabel.Size = new Size(51, 21);
            notesLabel.TabIndex = 8;
            notesLabel.Text = "Notes";
            // 
            // notesText
            // 
            notesText.Location = new Point(500, 106);
            notesText.MaxLength = 250;
            notesText.Name = "notesText";
            notesText.Size = new Size(320, 29);
            notesText.TabIndex = 2;
            notesText.TextChanged += headerField_Changed;
            // 
            // subTotalLabel
            // 
            subTotalLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            subTotalLabel.AutoSize = true;
            subTotalLabel.Location = new Point(880, 34);
            subTotalLabel.Name = "subTotalLabel";
            subTotalLabel.Size = new Size(72, 21);
            subTotalLabel.TabIndex = 9;
            subTotalLabel.Text = "Sub total";
            // 
            // subTotalValue
            // 
            subTotalValue.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            subTotalValue.Location = new Point(980, 30);
            subTotalValue.Name = "subTotalValue";
            subTotalValue.Size = new Size(180, 28);
            subTotalValue.TabIndex = 10;
            subTotalValue.Text = "0";
            subTotalValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // discountLabel
            // 
            discountLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            discountLabel.AutoSize = true;
            discountLabel.Location = new Point(880, 72);
            discountLabel.Name = "discountLabel";
            discountLabel.Size = new Size(71, 21);
            discountLabel.TabIndex = 11;
            discountLabel.Text = "Discount";
            // 
            // discountInput
            // 
            discountInput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            discountInput.DecimalPlaces = 2;
            discountInput.Location = new Point(980, 68);
            discountInput.Maximum = new decimal(new int[] { -727379968, 232, 0, 0 });
            discountInput.Name = "discountInput";
            discountInput.Size = new Size(180, 29);
            discountInput.TabIndex = 3;
            discountInput.TextAlign = HorizontalAlignment.Right;
            discountInput.ThousandsSeparator = true;
            discountInput.ValueChanged += discountInput_ValueChanged;
            // 
            // totalLabel
            // 
            totalLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            totalLabel.AutoSize = true;
            totalLabel.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            totalLabel.Location = new Point(880, 108);
            totalLabel.Name = "totalLabel";
            totalLabel.Size = new Size(53, 25);
            totalLabel.TabIndex = 12;
            totalLabel.Text = "Total";
            // 
            // totalValue
            // 
            totalValue.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            totalValue.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
            totalValue.Location = new Point(960, 102);
            totalValue.Name = "totalValue";
            totalValue.Size = new Size(200, 34);
            totalValue.TabIndex = 13;
            totalValue.Text = "0";
            totalValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // itemsGroup
            // 
            itemsGroup.Controls.Add(linesGrid);
            itemsGroup.Controls.Add(scanPanel);
            itemsGroup.Dock = DockStyle.Fill;
            itemsGroup.Font = new Font("Segoe UI", 9.5F);
            itemsGroup.Location = new Point(10, 160);
            itemsGroup.Name = "itemsGroup";
            itemsGroup.Padding = new Padding(10);
            itemsGroup.Size = new Size(1180, 478);
            itemsGroup.TabIndex = 1;
            itemsGroup.TabStop = false;
            itemsGroup.Text = "Items";
            // 
            // linesGrid
            // 
            linesGrid.AllowUserToAddRows = false;
            linesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            linesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            linesGrid.Dock = DockStyle.Fill;
            linesGrid.Location = new Point(10, 80);
            linesGrid.MultiSelect = false;
            linesGrid.Name = "linesGrid";
            linesGrid.RowHeadersVisible = false;
            linesGrid.RowHeadersWidth = 51;
            linesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            linesGrid.Size = new Size(1160, 388);
            linesGrid.TabIndex = 1;
            linesGrid.DataBindingComplete += linesGrid_DataBindingComplete;
            linesGrid.DataError += linesGrid_DataError;
            // 
            // scanPanel
            // 
            scanPanel.Controls.Add(scanLabel);
            scanPanel.Controls.Add(scanBox);
            scanPanel.Controls.Add(addLineButton);
            scanPanel.Controls.Add(removeLineButton);
            scanPanel.Controls.Add(aiButton);
            scanPanel.Controls.Add(voiceButton);
            scanPanel.Controls.Add(hintLabel);
            scanPanel.Dock = DockStyle.Top;
            scanPanel.Location = new Point(10, 32);
            scanPanel.Name = "scanPanel";
            scanPanel.Size = new Size(1160, 48);
            scanPanel.TabIndex = 0;
            // 
            // scanLabel
            // 
            scanLabel.AutoSize = true;
            scanLabel.Location = new Point(0, 14);
            scanLabel.Name = "scanLabel";
            scanLabel.Size = new Size(128, 21);
            scanLabel.TabIndex = 0;
            scanLabel.Text = "Barcode or name";
            // 
            // scanBox
            // 
            scanBox.Font = new Font("Segoe UI", 11F);
            scanBox.Location = new Point(130, 8);
            scanBox.Name = "scanBox";
            scanBox.PlaceholderText = "Scan or type, then Enter";
            scanBox.Size = new Size(340, 32);
            scanBox.TabIndex = 0;
            scanBox.KeyDown += scanBox_KeyDown;
            // 
            // addLineButton
            // 
            addLineButton.Location = new Point(480, 6);
            addLineButton.Name = "addLineButton";
            addLineButton.Size = new Size(90, 36);
            addLineButton.TabIndex = 1;
            addLineButton.Text = "Add";
            addLineButton.Click += addLineButton_Click;
            // 
            // removeLineButton
            // 
            removeLineButton.ForeColor = Color.Firebrick;
            removeLineButton.Location = new Point(580, 6);
            removeLineButton.Name = "removeLineButton";
            removeLineButton.Size = new Size(120, 36);
            removeLineButton.TabIndex = 2;
            removeLineButton.Text = "Remove line";
            removeLineButton.Click += removeLineButton_Click;
            //
            // aiButton
            //
            aiButton.BackColor = Color.FromArgb(37, 99, 235);
            aiButton.FlatAppearance.BorderSize = 0;
            aiButton.FlatStyle = FlatStyle.Flat;
            aiButton.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            aiButton.ForeColor = Color.White;
            aiButton.Location = new Point(712, 6);
            aiButton.Name = "aiButton";
            aiButton.Size = new Size(210, 36);
            aiButton.TabIndex = 4;
            aiButton.Text = "📷 Read invoice (AI)";
            aiButton.UseVisualStyleBackColor = false;
            aiButton.Click += aiButton_Click;
            //
            // voiceButton
            //
            voiceButton.BackColor = Color.FromArgb(124, 58, 237);
            voiceButton.FlatAppearance.BorderSize = 0;
            voiceButton.FlatStyle = FlatStyle.Flat;
            voiceButton.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            voiceButton.ForeColor = Color.White;
            voiceButton.Location = new Point(930, 6);
            voiceButton.Name = "voiceButton";
            voiceButton.Size = new Size(160, 36);
            voiceButton.TabIndex = 5;
            voiceButton.Text = "🎤 Voice note (AI)";
            voiceButton.UseVisualStyleBackColor = false;
            voiceButton.Click += voiceButton_Click;
            // 
            // hintLabel
            // 
            hintLabel.AutoSize = true;
            hintLabel.ForeColor = Color.DimGray;
            hintLabel.Location = new Point(1100, 14);
            hintLabel.Name = "hintLabel";
            hintLabel.Size = new Size(481, 21);
            hintLabel.TabIndex = 3;
            hintLabel.Text = "Qty and Unit cost (yellow) can be edited  ·  Delete key removes a line";
            // 
            // buttonsPanel
            // 
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(postButton);
            buttonsPanel.Controls.Add(saveDraftButton);
            buttonsPanel.Dock = DockStyle.Bottom;
            buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonsPanel.Location = new Point(10, 638);
            buttonsPanel.Name = "buttonsPanel";
            buttonsPanel.Padding = new Padding(0, 8, 0, 0);
            buttonsPanel.Size = new Size(1180, 52);
            buttonsPanel.TabIndex = 2;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(1067, 11);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(110, 38);
            cancelButton.TabIndex = 2;
            cancelButton.Text = "Cancel";
            cancelButton.Click += cancelButton_Click;
            // 
            // postButton
            // 
            postButton.BackColor = Color.FromArgb(34, 139, 117);
            postButton.FlatAppearance.BorderSize = 0;
            postButton.FlatStyle = FlatStyle.Flat;
            postButton.ForeColor = Color.White;
            postButton.Location = new Point(911, 11);
            postButton.Name = "postButton";
            postButton.Size = new Size(150, 38);
            postButton.TabIndex = 1;
            postButton.Text = "Save && post";
            postButton.UseVisualStyleBackColor = false;
            postButton.Click += postButton_Click;
            // 
            // saveDraftButton
            // 
            saveDraftButton.Location = new Point(775, 11);
            saveDraftButton.Name = "saveDraftButton";
            saveDraftButton.Size = new Size(130, 38);
            saveDraftButton.TabIndex = 0;
            saveDraftButton.Text = "Save draft";
            saveDraftButton.Click += saveDraftButton_Click;
            // 
            // PurchaseEditForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1200, 700);
            Controls.Add(itemsGroup);
            Controls.Add(headerGroup);
            Controls.Add(buttonsPanel);
            MinimumSize = new Size(1100, 600);
            Name = "PurchaseEditForm";
            Padding = new Padding(10);
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "New purchase";
            FormClosing += PurchaseEditForm_FormClosing;
            Load += PurchaseEditForm_Load;
            headerGroup.ResumeLayout(false);
            headerGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)discountInput).EndInit();
            itemsGroup.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)linesGrid).EndInit();
            scanPanel.ResumeLayout(false);
            scanPanel.PerformLayout();
            buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private GroupBox headerGroup;
        private Label numberLabel;
        private Label numberValue;
        private Label supplierInvoiceLabel;
        private TextBox supplierInvoiceText;
        private Label dateLabel;
        private DateTimePicker dateInput;
        private Label supplierLabel;
        private Label supplierValue;
        private Label statusLabel;
        private Label statusValue;
        private Label notesLabel;
        private TextBox notesText;
        private Label subTotalLabel;
        private Label subTotalValue;
        private Label discountLabel;
        private NumericUpDown discountInput;
        private Label totalLabel;
        private Label totalValue;
        private GroupBox itemsGroup;
        private DataGridView linesGrid;
        private Panel scanPanel;
        private Label scanLabel;
        private TextBox scanBox;
        private Button addLineButton;
        private Button removeLineButton;
        private Button aiButton;
        private Button voiceButton;
        private Label hintLabel;
        private FlowLayoutPanel buttonsPanel;
        private Button cancelButton;
        private Button postButton;
        private Button saveDraftButton;
    }
}
