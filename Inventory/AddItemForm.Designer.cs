namespace Inventory
{
    partial class AddItemForm
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
            layout = new TableLayoutPanel();
            nameLabel = new Label();
            nameText = new TextBox();
            categoryLabel = new Label();
            categoryCombo = new ComboBox();
            typeLabel = new Label();
            typeCombo = new ComboBox();
            unitLabel = new Label();
            unitCombo = new ComboBox();
            sizeLabel = new Label();
            sizeText = new TextBox();
            colorLabel = new Label();
            colorText = new TextBox();
            costLabel = new Label();
            costInput = new NumericUpDown();
            stockLabel = new Label();
            stockInput = new NumericUpDown();
            reorderLabel = new Label();
            reorderInput = new NumericUpDown();
            weightedCheck = new CheckBox();
            activeCheck = new CheckBox();
            packagingGroup = new GroupBox();
            packagingGrid = new DataGridView();
            packagingButtons = new FlowLayoutPanel();
            addPackagingButton = new Button();
            removePackagingButton = new Button();
            packagingHint = new Label();
            buttonsPanel = new FlowLayoutPanel();
            cancelButton = new Button();
            saveButton = new Button();
            layout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)costInput).BeginInit();
            ((System.ComponentModel.ISupportInitialize)stockInput).BeginInit();
            ((System.ComponentModel.ISupportInitialize)reorderInput).BeginInit();
            packagingGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)packagingGrid).BeginInit();
            packagingButtons.SuspendLayout();
            buttonsPanel.SuspendLayout();
            SuspendLayout();
            //
            // layout
            //
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(nameLabel, 0, 0);
            layout.Controls.Add(nameText, 1, 0);
            layout.Controls.Add(categoryLabel, 0, 1);
            layout.Controls.Add(categoryCombo, 1, 1);
            layout.Controls.Add(typeLabel, 0, 2);
            layout.Controls.Add(typeCombo, 1, 2);
            layout.Controls.Add(unitLabel, 0, 3);
            layout.Controls.Add(unitCombo, 1, 3);
            layout.Controls.Add(sizeLabel, 0, 4);
            layout.Controls.Add(sizeText, 1, 4);
            layout.Controls.Add(colorLabel, 0, 5);
            layout.Controls.Add(colorText, 1, 5);
            layout.Controls.Add(costLabel, 0, 6);
            layout.Controls.Add(costInput, 1, 6);
            layout.Controls.Add(stockLabel, 0, 7);
            layout.Controls.Add(stockInput, 1, 7);
            layout.Controls.Add(reorderLabel, 0, 8);
            layout.Controls.Add(reorderInput, 1, 8);
            layout.Controls.Add(weightedCheck, 1, 9);
            layout.Controls.Add(activeCheck, 1, 10);
            layout.Dock = DockStyle.Left;
            layout.Location = new Point(0, 0);
            layout.Name = "layout";
            layout.Padding = new Padding(16);
            layout.RowCount = 12;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.Size = new Size(480, 484);
            layout.TabIndex = 0;
            //
            // nameLabel
            //
            nameLabel.Anchor = AnchorStyles.Left;
            nameLabel.AutoSize = true;
            nameLabel.Name = "nameLabel";
            nameLabel.Text = "Name *";
            //
            // nameText
            //
            nameText.Dock = DockStyle.Fill;
            nameText.MaxLength = 150;
            nameText.Name = "nameText";
            nameText.TabIndex = 0;
            //
            // categoryLabel
            //
            categoryLabel.Anchor = AnchorStyles.Left;
            categoryLabel.AutoSize = true;
            categoryLabel.Name = "categoryLabel";
            categoryLabel.Text = "Category";
            //
            // categoryCombo
            //
            categoryCombo.Dock = DockStyle.Fill;
            categoryCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            categoryCombo.Name = "categoryCombo";
            categoryCombo.TabIndex = 1;
            //
            // typeLabel
            //
            typeLabel.Anchor = AnchorStyles.Left;
            typeLabel.AutoSize = true;
            typeLabel.Name = "typeLabel";
            typeLabel.Text = "Type";
            //
            // typeCombo
            //
            typeCombo.Dock = DockStyle.Fill;
            typeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            typeCombo.Name = "typeCombo";
            typeCombo.TabIndex = 2;
            typeCombo.SelectedIndexChanged += typeCombo_SelectedIndexChanged;
            //
            // unitLabel
            //
            unitLabel.Anchor = AnchorStyles.Left;
            unitLabel.AutoSize = true;
            unitLabel.Name = "unitLabel";
            unitLabel.Text = "Base unit *";
            //
            // unitCombo
            //
            unitCombo.Dock = DockStyle.Fill;
            unitCombo.Items.AddRange(new object[] { "pcs", "can", "kg", "g", "l", "ml", "box" });
            unitCombo.MaxLength = 20;
            unitCombo.Name = "unitCombo";
            unitCombo.TabIndex = 3;
            unitCombo.Text = "pcs";
            //
            // sizeLabel
            //
            sizeLabel.Anchor = AnchorStyles.Left;
            sizeLabel.AutoSize = true;
            sizeLabel.Name = "sizeLabel";
            sizeLabel.Text = "Size";
            //
            // sizeText
            //
            sizeText.Dock = DockStyle.Fill;
            sizeText.MaxLength = 20;
            sizeText.Name = "sizeText";
            sizeText.TabIndex = 4;
            //
            // colorLabel
            //
            colorLabel.Anchor = AnchorStyles.Left;
            colorLabel.AutoSize = true;
            colorLabel.Name = "colorLabel";
            colorLabel.Text = "Color";
            //
            // colorText
            //
            colorText.Dock = DockStyle.Fill;
            colorText.MaxLength = 30;
            colorText.Name = "colorText";
            colorText.TabIndex = 5;
            //
            // costLabel
            //
            costLabel.Anchor = AnchorStyles.Left;
            costLabel.AutoSize = true;
            costLabel.Name = "costLabel";
            costLabel.Text = "Avg cost";
            //
            // costInput
            //
            costInput.DecimalPlaces = 2;
            costInput.Dock = DockStyle.Fill;
            costInput.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            costInput.Name = "costInput";
            costInput.TabIndex = 6;
            costInput.ThousandsSeparator = true;
            // 
            // stockLabel
            //
            stockLabel.Anchor = AnchorStyles.Left;
            stockLabel.AutoSize = true;
            stockLabel.Name = "stockLabel";
            stockLabel.Text = "Opening stock";
            //
            // stockInput
            //
            stockInput.DecimalPlaces = 3;
            stockInput.Dock = DockStyle.Fill;
            stockInput.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            stockInput.Name = "stockInput";
            stockInput.TabIndex = 9;
            stockInput.ThousandsSeparator = true;
            //
            // reorderLabel
            //
            reorderLabel.Anchor = AnchorStyles.Left;
            reorderLabel.AutoSize = true;
            reorderLabel.Name = "reorderLabel";
            reorderLabel.Text = "Reorder level";
            //
            // reorderInput
            //
            reorderInput.DecimalPlaces = 3;
            reorderInput.Dock = DockStyle.Fill;
            reorderInput.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            reorderInput.Name = "reorderInput";
            reorderInput.TabIndex = 10;
            reorderInput.ThousandsSeparator = true;
            //
            // weightedCheck
            //
            weightedCheck.Anchor = AnchorStyles.Left;
            weightedCheck.AutoSize = true;
            weightedCheck.Name = "weightedCheck";
            weightedCheck.TabIndex = 11;
            weightedCheck.Text = "Sold by weight";
            //
            // activeCheck
            //
            activeCheck.Anchor = AnchorStyles.Left;
            activeCheck.AutoSize = true;
            activeCheck.Checked = true;
            activeCheck.CheckState = CheckState.Checked;
            activeCheck.Name = "activeCheck";
            activeCheck.TabIndex = 12;
            activeCheck.Text = "Active";
            //
            // packagingGroup
            //
            packagingGroup.Controls.Add(packagingGrid);
            packagingGroup.Controls.Add(packagingButtons);
            packagingGroup.Controls.Add(packagingHint);
            packagingGroup.Dock = DockStyle.Fill;
            packagingGroup.Location = new Point(480, 0);
            packagingGroup.Name = "packagingGroup";
            packagingGroup.Padding = new Padding(10);
            packagingGroup.Size = new Size(620, 484);
            packagingGroup.TabIndex = 2;
            packagingGroup.TabStop = false;
            packagingGroup.Text = "Packaging && prices";
            //
            // packagingHint
            //
            packagingHint.Dock = DockStyle.Top;
            packagingHint.ForeColor = Color.DimGray;
            packagingHint.Name = "packagingHint";
            packagingHint.Size = new Size(600, 44);
            packagingHint.Text = "Stock is counted in the base unit. \"Contains\" = how many base units are in one packaging (can = 1, box of 24 cans = 24). Each packaging can have its own barcode and price.";
            //
            // packagingButtons
            //
            packagingButtons.Controls.Add(addPackagingButton);
            packagingButtons.Controls.Add(removePackagingButton);
            packagingButtons.Dock = DockStyle.Top;
            packagingButtons.Name = "packagingButtons";
            packagingButtons.Size = new Size(600, 42);
            packagingButtons.TabIndex = 0;
            //
            // addPackagingButton
            //
            addPackagingButton.Name = "addPackagingButton";
            addPackagingButton.Size = new Size(150, 34);
            addPackagingButton.TabIndex = 0;
            addPackagingButton.Text = "+ Add packaging";
            addPackagingButton.Click += addPackagingButton_Click;
            //
            // removePackagingButton
            //
            removePackagingButton.ForeColor = Color.Firebrick;
            removePackagingButton.Name = "removePackagingButton";
            removePackagingButton.Size = new Size(100, 34);
            removePackagingButton.TabIndex = 1;
            removePackagingButton.Text = "Remove";
            removePackagingButton.Click += removePackagingButton_Click;
            //
            // packagingGrid
            //
            packagingGrid.AllowUserToAddRows = false;
            packagingGrid.AllowUserToDeleteRows = false;
            packagingGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            packagingGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            packagingGrid.Dock = DockStyle.Fill;
            packagingGrid.EditMode = DataGridViewEditMode.EditOnEnter;
            packagingGrid.MultiSelect = false;
            packagingGrid.Name = "packagingGrid";
            packagingGrid.RowHeadersVisible = false;
            packagingGrid.RowHeadersWidth = 51;
            packagingGrid.Size = new Size(600, 368);
            packagingGrid.TabIndex = 1;
            packagingGrid.CellValueChanged += packagingGrid_CellValueChanged;
            packagingGrid.CurrentCellDirtyStateChanged += packagingGrid_CurrentCellDirtyStateChanged;
            packagingGrid.DataBindingComplete += packagingGrid_DataBindingComplete;
            packagingGrid.DataError += packagingGrid_DataError;
            //
            // buttonsPanel
            //
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(saveButton);
            buttonsPanel.Dock = DockStyle.Bottom;
            buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonsPanel.Height = 56;
            buttonsPanel.Name = "buttonsPanel";
            buttonsPanel.Padding = new Padding(12, 8, 12, 8);
            buttonsPanel.TabIndex = 1;
            //
            // cancelButton
            //
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(100, 36);
            cancelButton.TabIndex = 14;
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
            saveButton.TabIndex = 13;
            saveButton.Text = "Save";
            saveButton.UseVisualStyleBackColor = false;
            saveButton.Click += saveButton_Click;
            //
            // AddItemForm
            //
            AcceptButton = saveButton;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(1100, 540);
            Controls.Add(packagingGroup);
            Controls.Add(layout);
            Controls.Add(buttonsPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddItemForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add item";
            Load += AddItemForm_Load;
            layout.ResumeLayout(false);
            layout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)costInput).EndInit();
            ((System.ComponentModel.ISupportInitialize)stockInput).EndInit();
            ((System.ComponentModel.ISupportInitialize)reorderInput).EndInit();
            packagingGroup.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)packagingGrid).EndInit();
            packagingButtons.ResumeLayout(false);
            packagingButtons.PerformLayout();
            buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel layout;
        private Label nameLabel;
        private TextBox nameText;
        private Label categoryLabel;
        private ComboBox categoryCombo;
        private Label typeLabel;
        private ComboBox typeCombo;
        private Label unitLabel;
        private ComboBox unitCombo;
        private Label sizeLabel;
        private TextBox sizeText;
        private Label colorLabel;
        private TextBox colorText;
        private Label costLabel;
        private NumericUpDown costInput;
        private Label stockLabel;
        private NumericUpDown stockInput;
        private Label reorderLabel;
        private NumericUpDown reorderInput;
        private CheckBox weightedCheck;
        private CheckBox activeCheck;
        private GroupBox packagingGroup;
        private DataGridView packagingGrid;
        private FlowLayoutPanel packagingButtons;
        private Button addPackagingButton;
        private Button removePackagingButton;
        private Label packagingHint;
        private FlowLayoutPanel buttonsPanel;
        private Button cancelButton;
        private Button saveButton;
    }
}
