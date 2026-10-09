namespace Inventory
{
    partial class QuickItemPickerForm
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
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            filterPanel = new Panel();
            searchText = new TextBox();
            categoryCombo = new ComboBox();
            itemsGrid = new DataGridView();
            pickedColumn = new DataGridViewCheckBoxColumn();
            itemColumn = new DataGridViewTextBoxColumn();
            categoryColumn = new DataGridViewTextBoxColumn();
            barcodeColumn = new DataGridViewTextBoxColumn();
            priceColumn = new DataGridViewTextBoxColumn();
            tabColumn = new DataGridViewTextBoxColumn();
            bottomPanel = new Panel();
            countLabel = new Label();
            selectAllCheck = new CheckBox();
            buttonsPanel = new FlowLayoutPanel();
            cancelButton = new Button();
            addButton = new Button();
            searchTimer = new System.Windows.Forms.Timer(components);
            filterPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)itemsGrid).BeginInit();
            bottomPanel.SuspendLayout();
            buttonsPanel.SuspendLayout();
            SuspendLayout();
            // 
            // filterPanel
            // 
            filterPanel.Controls.Add(searchText);
            filterPanel.Controls.Add(categoryCombo);
            filterPanel.Dock = DockStyle.Top;
            filterPanel.Location = new Point(0, 0);
            filterPanel.Name = "filterPanel";
            filterPanel.Padding = new Padding(10, 10, 10, 8);
            filterPanel.Size = new Size(900, 50);
            filterPanel.TabIndex = 0;
            // 
            // searchText
            // 
            searchText.Dock = DockStyle.Fill;
            searchText.Font = new Font("Segoe UI", 11F);
            searchText.Location = new Point(10, 10);
            searchText.Name = "searchText";
            searchText.PlaceholderText = "Search the item list by name or barcode...";
            searchText.Size = new Size(600, 32);
            searchText.TabIndex = 0;
            searchText.TextChanged += searchText_TextChanged;
            searchText.KeyDown += searchText_KeyDown;
            // 
            // categoryCombo
            // 
            categoryCombo.Dock = DockStyle.Right;
            categoryCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            categoryCombo.Font = new Font("Segoe UI", 11F);
            categoryCombo.Location = new Point(610, 10);
            categoryCombo.Name = "categoryCombo";
            categoryCombo.Size = new Size(280, 33);
            categoryCombo.TabIndex = 1;
            categoryCombo.SelectedIndexChanged += categoryCombo_SelectedIndexChanged;
            // 
            // itemsGrid
            // 
            itemsGrid.AllowUserToAddRows = false;
            itemsGrid.AllowUserToDeleteRows = false;
            itemsGrid.AllowUserToResizeRows = false;
            itemsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            itemsGrid.BackgroundColor = Color.White;
            itemsGrid.BorderStyle = BorderStyle.None;
            itemsGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            itemsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            itemsGrid.Columns.AddRange(new DataGridViewColumn[] { pickedColumn, itemColumn, categoryColumn, barcodeColumn, priceColumn, tabColumn });
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.Window;
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(240, 247, 244);
            dataGridViewCellStyle2.SelectionForeColor = Color.FromArgb(22, 32, 43);
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            itemsGrid.DefaultCellStyle = dataGridViewCellStyle2;
            itemsGrid.Dock = DockStyle.Fill;
            itemsGrid.GridColor = Color.FromArgb(232, 235, 238);
            itemsGrid.Location = new Point(0, 50);
            itemsGrid.MultiSelect = false;
            itemsGrid.Name = "itemsGrid";
            itemsGrid.ReadOnly = true;
            itemsGrid.RowHeadersVisible = false;
            itemsGrid.RowHeadersWidth = 51;
            itemsGrid.RowTemplate.Height = 34;
            itemsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            itemsGrid.Size = new Size(900, 458);
            itemsGrid.TabIndex = 1;
            itemsGrid.CellClick += itemsGrid_CellClick;
            itemsGrid.CellFormatting += itemsGrid_CellFormatting;
            itemsGrid.KeyDown += itemsGrid_KeyDown;
            // 
            // pickedColumn
            // 
            pickedColumn.DataPropertyName = "Picked";
            pickedColumn.FillWeight = 12F;
            pickedColumn.HeaderText = "";
            pickedColumn.MinimumWidth = 6;
            pickedColumn.Name = "pickedColumn";
            pickedColumn.ReadOnly = true;
            // 
            // itemColumn
            // 
            itemColumn.DataPropertyName = "ItemName";
            itemColumn.FillWeight = 120F;
            itemColumn.HeaderText = "Item";
            itemColumn.MinimumWidth = 6;
            itemColumn.Name = "itemColumn";
            itemColumn.ReadOnly = true;
            // 
            // categoryColumn
            // 
            categoryColumn.DataPropertyName = "Category";
            categoryColumn.FillWeight = 70F;
            categoryColumn.HeaderText = "Category";
            categoryColumn.MinimumWidth = 6;
            categoryColumn.Name = "categoryColumn";
            categoryColumn.ReadOnly = true;
            // 
            // barcodeColumn
            // 
            barcodeColumn.DataPropertyName = "Barcode";
            barcodeColumn.FillWeight = 50F;
            barcodeColumn.HeaderText = "Barcode";
            barcodeColumn.MinimumWidth = 6;
            barcodeColumn.Name = "barcodeColumn";
            barcodeColumn.ReadOnly = true;
            // 
            // priceColumn
            // 
            priceColumn.DataPropertyName = "Price";
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleRight;
            priceColumn.DefaultCellStyle = dataGridViewCellStyle1;
            priceColumn.FillWeight = 45F;
            priceColumn.HeaderText = "Price";
            priceColumn.MinimumWidth = 6;
            priceColumn.Name = "priceColumn";
            priceColumn.ReadOnly = true;
            // 
            // tabColumn
            // 
            tabColumn.DataPropertyName = "OnTab";
            tabColumn.FillWeight = 50F;
            tabColumn.HeaderText = "Already on tab";
            tabColumn.MinimumWidth = 6;
            tabColumn.Name = "tabColumn";
            tabColumn.ReadOnly = true;
            // 
            // bottomPanel
            // 
            bottomPanel.Controls.Add(countLabel);
            bottomPanel.Controls.Add(selectAllCheck);
            bottomPanel.Controls.Add(buttonsPanel);
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Location = new Point(0, 508);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new Padding(10, 6, 6, 6);
            bottomPanel.Size = new Size(900, 52);
            bottomPanel.TabIndex = 2;
            // 
            // countLabel
            // 
            countLabel.Dock = DockStyle.Fill;
            countLabel.ForeColor = Color.DimGray;
            countLabel.Location = new Point(180, 6);
            countLabel.Name = "countLabel";
            countLabel.Size = new Size(424, 40);
            countLabel.TabIndex = 1;
            countLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // selectAllCheck
            // 
            selectAllCheck.Dock = DockStyle.Left;
            selectAllCheck.Location = new Point(10, 6);
            selectAllCheck.Name = "selectAllCheck";
            selectAllCheck.Size = new Size(170, 40);
            selectAllCheck.TabIndex = 0;
            selectAllCheck.Text = "Tick all shown";
            selectAllCheck.Click += selectAllCheck_Click;
            // 
            // buttonsPanel
            // 
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(addButton);
            buttonsPanel.Dock = DockStyle.Right;
            buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonsPanel.Location = new Point(604, 6);
            buttonsPanel.Name = "buttonsPanel";
            buttonsPanel.Size = new Size(290, 40);
            buttonsPanel.TabIndex = 2;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(187, 3);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(100, 36);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Cancel";
            // 
            // addButton
            // 
            addButton.BackColor = Color.FromArgb(34, 139, 117);
            addButton.Enabled = false;
            addButton.FlatAppearance.BorderSize = 0;
            addButton.FlatStyle = FlatStyle.Flat;
            addButton.ForeColor = Color.White;
            addButton.Location = new Point(11, 3);
            addButton.Name = "addButton";
            addButton.Size = new Size(170, 36);
            addButton.TabIndex = 0;
            addButton.Text = "Add 0 items";
            addButton.UseVisualStyleBackColor = false;
            addButton.Click += addButton_Click;
            // 
            // searchTimer
            // 
            searchTimer.Interval = 300;
            searchTimer.Tick += searchTimer_Tick;
            // 
            // QuickItemPickerForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(900, 560);
            Controls.Add(itemsGrid);
            Controls.Add(filterPanel);
            Controls.Add(bottomPanel);
            MinimizeBox = false;
            MinimumSize = new Size(700, 400);
            Name = "QuickItemPickerForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add items";
            Load += QuickItemPickerForm_Load;
            filterPanel.ResumeLayout(false);
            filterPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)itemsGrid).EndInit();
            bottomPanel.ResumeLayout(false);
            buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel filterPanel;
        private TextBox searchText;
        private ComboBox categoryCombo;
        private DataGridView itemsGrid;
        private DataGridViewCheckBoxColumn pickedColumn;
        private DataGridViewTextBoxColumn itemColumn;
        private DataGridViewTextBoxColumn categoryColumn;
        private DataGridViewTextBoxColumn barcodeColumn;
        private DataGridViewTextBoxColumn priceColumn;
        private DataGridViewTextBoxColumn tabColumn;
        private Panel bottomPanel;
        private CheckBox selectAllCheck;
        private Label countLabel;
        private FlowLayoutPanel buttonsPanel;
        private Button cancelButton;
        private Button addButton;
        private System.Windows.Forms.Timer searchTimer;
    }
}
