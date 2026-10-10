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
        /// Layout: header, a card with the product (left) and base unit + units (right), a card with stock.
        /// </summary>
        private void InitializeComponent()
        {
            DataGridViewCellStyle gridHeaderStyle = new DataGridViewCellStyle();
            headerPanel = new Panel();
            subtitleLabel = new Label();
            titleLabel = new Label();
            breadcrumbLabel = new Label();
            bodyPanel = new Panel();
            stockCard = new TableLayoutPanel();
            stockLabel = new Label();
            reorderLabel = new Label();
            stockInput = new NumericUpDown();
            reorderInput = new NumericUpDown();
            costLabel = new Label();
            costInput = new NumericUpDown();
            activeCheck = new CheckBox();
            spacerPanel = new Panel();
            mainCard = new TableLayoutPanel();
            leftStack = new TableLayoutPanel();
            nameLabel = new Label();
            nameText = new TextBox();
            categoryLabel = new Label();
            categoryCombo = new ComboBox();
            typeLabel = new Label();
            typeCombo = new ComboBox();
            sizeColorTable = new TableLayoutPanel();
            scaleLabel = new Label();
            scaleRow = new Panel();
            scaleCheck = new CheckBox();
            scaleCodeText = new TextBox();
            scaleHintLabel = new Label();
            sizeLabel = new Label();
            colorLabel = new Label();
            sizeText = new TextBox();
            colorText = new TextBox();
            rightStack = new TableLayoutPanel();
            baseTitleLabel = new Label();
            baseInfoLabel = new Label();
            unitLabel = new Label();
            unitRow = new Panel();
            unitCombo = new ComboBox();
            weightedCheck = new CheckBox();
            unitsTitleLabel = new Label();
            unitsSubLabel = new Label();
            packagingGrid = new DataGridView();
            packagingButtons = new FlowLayoutPanel();
            addPackagingButton = new Button();
            removePackagingButton = new Button();
            buttonsPanel = new FlowLayoutPanel();
            cancelButton = new Button();
            saveButton = new Button();
            headerPanel.SuspendLayout();
            bodyPanel.SuspendLayout();
            stockCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)stockInput).BeginInit();
            ((System.ComponentModel.ISupportInitialize)reorderInput).BeginInit();
            ((System.ComponentModel.ISupportInitialize)costInput).BeginInit();
            mainCard.SuspendLayout();
            leftStack.SuspendLayout();
            sizeColorTable.SuspendLayout();
            rightStack.SuspendLayout();
            unitRow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)packagingGrid).BeginInit();
            packagingButtons.SuspendLayout();
            buttonsPanel.SuspendLayout();
            SuspendLayout();
            //
            // headerPanel
            //
            headerPanel.Controls.Add(subtitleLabel);
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(breadcrumbLabel);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Name = "headerPanel";
            headerPanel.Padding = new Padding(22, 10, 22, 0);
            headerPanel.Size = new Size(1120, 112);
            headerPanel.TabIndex = 0;
            //
            // subtitleLabel
            //
            subtitleLabel.Dock = DockStyle.Top;
            subtitleLabel.Font = new Font("Segoe UI", 11F);
            subtitleLabel.ForeColor = Color.FromArgb(90, 98, 108);
            subtitleLabel.Name = "subtitleLabel";
            subtitleLabel.Size = new Size(1076, 28);
            subtitleLabel.Text = "Create a new product for your inventory system";
            //
            // titleLabel
            //
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(22, 32, 43);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(1076, 50);
            titleLabel.Text = "Add New Product";
            //
            // breadcrumbLabel
            //
            breadcrumbLabel.Dock = DockStyle.Top;
            breadcrumbLabel.Font = new Font("Segoe UI", 9F);
            breadcrumbLabel.ForeColor = Color.FromArgb(90, 98, 108);
            breadcrumbLabel.Name = "breadcrumbLabel";
            breadcrumbLabel.Size = new Size(1076, 22);
            breadcrumbLabel.Text = "Products  >  Add New Product";
            //
            // bodyPanel
            //
            bodyPanel.AutoScroll = true;
            bodyPanel.Controls.Add(stockCard);
            bodyPanel.Controls.Add(spacerPanel);
            bodyPanel.Controls.Add(mainCard);
            bodyPanel.Dock = DockStyle.Fill;
            bodyPanel.Name = "bodyPanel";
            bodyPanel.Padding = new Padding(20, 6, 20, 10);
            bodyPanel.TabIndex = 1;
            //
            // mainCard
            //
            mainCard.BackColor = Color.White;
            mainCard.ColumnCount = 2;
            mainCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            mainCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            mainCard.Controls.Add(leftStack, 0, 0);
            mainCard.Controls.Add(rightStack, 1, 0);
            mainCard.Dock = DockStyle.Top;
            mainCard.Name = "mainCard";
            mainCard.Padding = new Padding(18, 14, 18, 14);
            mainCard.RowCount = 1;
            mainCard.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainCard.Size = new Size(1080, 476);
            mainCard.TabIndex = 0;
            //
            // leftStack
            //
            leftStack.ColumnCount = 1;
            leftStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftStack.Controls.Add(nameLabel, 0, 0);
            leftStack.Controls.Add(nameText, 0, 1);
            leftStack.Controls.Add(categoryLabel, 0, 2);
            leftStack.Controls.Add(categoryCombo, 0, 3);
            leftStack.Controls.Add(typeLabel, 0, 4);
            leftStack.Controls.Add(typeCombo, 0, 5);
            leftStack.Controls.Add(sizeColorTable, 0, 6);
            leftStack.Controls.Add(scaleLabel, 0, 7);
            leftStack.Controls.Add(scaleRow, 0, 8);
            leftStack.Controls.Add(scaleHintLabel, 0, 9);
            leftStack.Dock = DockStyle.Fill;
            leftStack.Margin = new Padding(0, 0, 24, 0);
            leftStack.Name = "leftStack";
            leftStack.RowCount = 11;
            leftStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            leftStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            leftStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            leftStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            leftStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            leftStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            leftStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
            leftStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            leftStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            leftStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            leftStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftStack.TabIndex = 0;
            //
            // scaleLabel
            //
            scaleLabel.Dock = DockStyle.Fill;
            scaleLabel.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            scaleLabel.Name = "scaleLabel";
            scaleLabel.Text = "Scale product";
            scaleLabel.TextAlign = ContentAlignment.BottomLeft;
            //
            // scaleRow
            //
            scaleRow.Controls.Add(scaleCodeText);
            scaleRow.Controls.Add(scaleCheck);
            scaleRow.Dock = DockStyle.Fill;
            scaleRow.Margin = new Padding(0);
            scaleRow.Name = "scaleRow";
            scaleRow.Padding = new Padding(3, 4, 3, 2);
            scaleRow.TabIndex = 6;
            //
            // scaleCheck
            //
            scaleCheck.Dock = DockStyle.Left;
            scaleCheck.Font = new Font("Segoe UI", 10F);
            scaleCheck.Name = "scaleCheck";
            scaleCheck.Size = new Size(215, 36);
            scaleCheck.TabIndex = 0;
            scaleCheck.Text = "Sold with a scale label";
            scaleCheck.CheckedChanged += scaleCheck_CheckedChanged;
            //
            // scaleCodeText
            //
            scaleCodeText.Dock = DockStyle.Fill;
            scaleCodeText.Enabled = false;
            scaleCodeText.Font = new Font("Segoe UI", 11F);
            scaleCodeText.MaxLength = 10;
            scaleCodeText.Name = "scaleCodeText";
            scaleCodeText.PlaceholderText = "Scale code (PLU), e.g. 00123";
            scaleCodeText.TabIndex = 1;
            //
            // scaleHintLabel
            //
            scaleHintLabel.Dock = DockStyle.Fill;
            scaleHintLabel.Font = new Font("Segoe UI", 8.5F);
            scaleHintLabel.ForeColor = Color.FromArgb(90, 98, 108);
            scaleHintLabel.Name = "scaleHintLabel";
            scaleHintLabel.Text = "The item number set in the scale. The cashier reads the weight or price from the label barcode.";
            //
            // nameLabel
            //
            nameLabel.Dock = DockStyle.Fill;
            nameLabel.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            nameLabel.Name = "nameLabel";
            nameLabel.Text = "Product Name *";
            nameLabel.TextAlign = ContentAlignment.BottomLeft;
            //
            // nameText
            //
            nameText.Dock = DockStyle.Top;
            nameText.Font = new Font("Segoe UI", 11F);
            nameText.Margin = new Padding(3, 4, 3, 3);
            nameText.MaxLength = 150;
            nameText.Name = "nameText";
            nameText.TabIndex = 0;
            //
            // categoryLabel
            //
            categoryLabel.Dock = DockStyle.Fill;
            categoryLabel.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            categoryLabel.Name = "categoryLabel";
            categoryLabel.Text = "Category";
            categoryLabel.TextAlign = ContentAlignment.BottomLeft;
            //
            // categoryCombo
            //
            categoryCombo.Dock = DockStyle.Top;
            categoryCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            categoryCombo.Font = new Font("Segoe UI", 11F);
            categoryCombo.Margin = new Padding(3, 4, 3, 3);
            categoryCombo.Name = "categoryCombo";
            categoryCombo.TabIndex = 1;
            //
            // typeLabel
            //
            typeLabel.Dock = DockStyle.Fill;
            typeLabel.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            typeLabel.Name = "typeLabel";
            typeLabel.Text = "Type";
            typeLabel.TextAlign = ContentAlignment.BottomLeft;
            //
            // typeCombo
            //
            typeCombo.Dock = DockStyle.Top;
            typeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            typeCombo.Font = new Font("Segoe UI", 11F);
            typeCombo.Margin = new Padding(3, 4, 3, 3);
            typeCombo.Name = "typeCombo";
            typeCombo.TabIndex = 2;
            typeCombo.SelectedIndexChanged += typeCombo_SelectedIndexChanged;
            //
            // sizeColorTable
            //
            sizeColorTable.ColumnCount = 2;
            sizeColorTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            sizeColorTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            sizeColorTable.Controls.Add(sizeLabel, 0, 0);
            sizeColorTable.Controls.Add(colorLabel, 1, 0);
            sizeColorTable.Controls.Add(sizeText, 0, 1);
            sizeColorTable.Controls.Add(colorText, 1, 1);
            sizeColorTable.Dock = DockStyle.Fill;
            sizeColorTable.Margin = new Padding(0);
            sizeColorTable.Name = "sizeColorTable";
            sizeColorTable.RowCount = 2;
            sizeColorTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            sizeColorTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            sizeColorTable.TabIndex = 3;
            //
            // sizeLabel
            //
            sizeLabel.Dock = DockStyle.Fill;
            sizeLabel.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            sizeLabel.Name = "sizeLabel";
            sizeLabel.Text = "Size";
            sizeLabel.TextAlign = ContentAlignment.BottomLeft;
            //
            // colorLabel
            //
            colorLabel.Dock = DockStyle.Fill;
            colorLabel.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            colorLabel.Name = "colorLabel";
            colorLabel.Text = "Color";
            colorLabel.TextAlign = ContentAlignment.BottomLeft;
            //
            // sizeText
            //
            sizeText.Dock = DockStyle.Top;
            sizeText.Font = new Font("Segoe UI", 11F);
            sizeText.Margin = new Padding(3, 4, 10, 3);
            sizeText.MaxLength = 20;
            sizeText.Name = "sizeText";
            sizeText.TabIndex = 4;
            //
            // colorText
            //
            colorText.Dock = DockStyle.Top;
            colorText.Font = new Font("Segoe UI", 11F);
            colorText.Margin = new Padding(3, 4, 3, 3);
            colorText.MaxLength = 30;
            colorText.Name = "colorText";
            colorText.TabIndex = 5;
            //
            // rightStack
            //
            rightStack.ColumnCount = 1;
            rightStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightStack.Controls.Add(baseTitleLabel, 0, 0);
            rightStack.Controls.Add(baseInfoLabel, 0, 1);
            rightStack.Controls.Add(unitLabel, 0, 2);
            rightStack.Controls.Add(unitRow, 0, 3);
            rightStack.Controls.Add(unitsTitleLabel, 0, 4);
            rightStack.Controls.Add(unitsSubLabel, 0, 5);
            rightStack.Controls.Add(packagingGrid, 0, 6);
            rightStack.Controls.Add(packagingButtons, 0, 7);
            rightStack.Dock = DockStyle.Fill;
            rightStack.Margin = new Padding(0);
            rightStack.Name = "rightStack";
            rightStack.RowCount = 8;
            rightStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            rightStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            rightStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            rightStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            rightStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            rightStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            rightStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rightStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            rightStack.TabIndex = 1;
            //
            // baseTitleLabel
            //
            baseTitleLabel.Dock = DockStyle.Fill;
            baseTitleLabel.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            baseTitleLabel.Name = "baseTitleLabel";
            baseTitleLabel.Text = "Base Unit";
            baseTitleLabel.TextAlign = ContentAlignment.BottomLeft;
            //
            // baseInfoLabel
            //
            baseInfoLabel.BackColor = Color.FromArgb(240, 242, 245);
            baseInfoLabel.BorderStyle = BorderStyle.FixedSingle;
            baseInfoLabel.Dock = DockStyle.Fill;
            baseInfoLabel.Font = new Font("Segoe UI", 9F);
            baseInfoLabel.Margin = new Padding(3, 4, 3, 4);
            baseInfoLabel.Name = "baseInfoLabel";
            baseInfoLabel.Padding = new Padding(8, 0, 8, 0);
            baseInfoLabel.Text = "ⓘ  Base Unit is the smallest unit for stock tracking. Other units convert using the factor below (can = 1, box of 24 cans = 24). Each unit can have its own barcode and price.";
            baseInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // unitLabel
            //
            unitLabel.Dock = DockStyle.Fill;
            unitLabel.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            unitLabel.Name = "unitLabel";
            unitLabel.Text = "Base Unit Name *";
            unitLabel.TextAlign = ContentAlignment.BottomLeft;
            //
            // unitRow
            //
            unitRow.Controls.Add(unitCombo);
            unitRow.Controls.Add(weightedCheck);
            unitRow.Dock = DockStyle.Fill;
            unitRow.Margin = new Padding(0);
            unitRow.Name = "unitRow";
            unitRow.Padding = new Padding(3, 4, 3, 4);
            unitRow.TabIndex = 6;
            //
            // unitCombo
            //
            unitCombo.Dock = DockStyle.Fill;
            unitCombo.Font = new Font("Segoe UI", 11F);
            unitCombo.Items.AddRange(new object[] { "pcs", "can", "kg", "g", "l", "ml", "box" });
            unitCombo.MaxLength = 20;
            unitCombo.Name = "unitCombo";
            unitCombo.TabIndex = 0;
            unitCombo.Text = "pcs";
            //
            // weightedCheck
            //
            weightedCheck.Dock = DockStyle.Right;
            weightedCheck.Name = "weightedCheck";
            weightedCheck.Padding = new Padding(16, 0, 0, 0);
            weightedCheck.Size = new Size(170, 36);
            weightedCheck.TabIndex = 1;
            weightedCheck.Text = "Sold by weight";
            //
            // unitsTitleLabel
            //
            unitsTitleLabel.Dock = DockStyle.Fill;
            unitsTitleLabel.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            unitsTitleLabel.Name = "unitsTitleLabel";
            unitsTitleLabel.Text = "Units";
            unitsTitleLabel.TextAlign = ContentAlignment.BottomLeft;
            //
            // unitsSubLabel
            //
            unitsSubLabel.Dock = DockStyle.Fill;
            unitsSubLabel.Font = new Font("Segoe UI", 9F);
            unitsSubLabel.ForeColor = Color.FromArgb(90, 98, 108);
            unitsSubLabel.Name = "unitsSubLabel";
            unitsSubLabel.Text = "Add multiple units for pricing and barcode tracking";
            unitsSubLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // packagingGrid
            //
            packagingGrid.AllowUserToAddRows = false;
            packagingGrid.AllowUserToDeleteRows = false;
            packagingGrid.AllowUserToResizeRows = false;
            packagingGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            packagingGrid.BackgroundColor = Color.White;
            packagingGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            packagingGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            gridHeaderStyle.BackColor = Color.FromArgb(240, 242, 245);
            gridHeaderStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            gridHeaderStyle.ForeColor = Color.FromArgb(22, 32, 43);
            gridHeaderStyle.SelectionBackColor = Color.FromArgb(240, 242, 245);
            gridHeaderStyle.SelectionForeColor = Color.FromArgb(22, 32, 43);
            packagingGrid.ColumnHeadersDefaultCellStyle = gridHeaderStyle;
            packagingGrid.ColumnHeadersHeight = 36;
            packagingGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            packagingGrid.Dock = DockStyle.Fill;
            packagingGrid.EditMode = DataGridViewEditMode.EditOnEnter;
            packagingGrid.EnableHeadersVisualStyles = false;
            packagingGrid.GridColor = Color.FromArgb(225, 229, 234);
            packagingGrid.MultiSelect = false;
            packagingGrid.Name = "packagingGrid";
            packagingGrid.RowHeadersVisible = false;
            packagingGrid.RowHeadersWidth = 51;
            packagingGrid.RowTemplate.Height = 34;
            packagingGrid.TabIndex = 7;
            packagingGrid.CellPainting += packagingGrid_CellPainting;
            packagingGrid.CellValueChanged += packagingGrid_CellValueChanged;
            packagingGrid.CurrentCellDirtyStateChanged += packagingGrid_CurrentCellDirtyStateChanged;
            packagingGrid.DataBindingComplete += packagingGrid_DataBindingComplete;
            packagingGrid.DataError += packagingGrid_DataError;
            //
            // packagingButtons
            //
            packagingButtons.Controls.Add(addPackagingButton);
            packagingButtons.Controls.Add(removePackagingButton);
            packagingButtons.Dock = DockStyle.Fill;
            packagingButtons.Margin = new Padding(0);
            packagingButtons.Name = "packagingButtons";
            packagingButtons.Padding = new Padding(0, 6, 0, 0);
            packagingButtons.TabIndex = 8;
            //
            // addPackagingButton
            //
            addPackagingButton.FlatAppearance.BorderColor = Color.FromArgb(28, 110, 92);
            addPackagingButton.FlatStyle = FlatStyle.Flat;
            addPackagingButton.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            addPackagingButton.ForeColor = Color.FromArgb(28, 110, 92);
            addPackagingButton.Name = "addPackagingButton";
            addPackagingButton.Size = new Size(130, 36);
            addPackagingButton.TabIndex = 0;
            addPackagingButton.Text = "+ Add Unit";
            addPackagingButton.Click += addPackagingButton_Click;
            //
            // removePackagingButton
            //
            removePackagingButton.FlatAppearance.BorderColor = Color.FromArgb(230, 180, 180);
            removePackagingButton.FlatStyle = FlatStyle.Flat;
            removePackagingButton.ForeColor = Color.Firebrick;
            removePackagingButton.Name = "removePackagingButton";
            removePackagingButton.Size = new Size(110, 36);
            removePackagingButton.TabIndex = 1;
            removePackagingButton.Text = "Remove unit";
            removePackagingButton.Click += removePackagingButton_Click;
            //
            // spacerPanel
            //
            spacerPanel.Dock = DockStyle.Top;
            spacerPanel.Name = "spacerPanel";
            spacerPanel.Size = new Size(1080, 16);
            spacerPanel.TabIndex = 1;
            //
            // stockCard
            //
            stockCard.BackColor = Color.White;
            stockCard.ColumnCount = 2;
            stockCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            stockCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            stockCard.Controls.Add(stockLabel, 0, 0);
            stockCard.Controls.Add(reorderLabel, 1, 0);
            stockCard.Controls.Add(stockInput, 0, 1);
            stockCard.Controls.Add(reorderInput, 1, 1);
            stockCard.Controls.Add(costLabel, 0, 2);
            stockCard.Controls.Add(costInput, 0, 3);
            stockCard.Controls.Add(activeCheck, 1, 3);
            stockCard.Dock = DockStyle.Top;
            stockCard.Name = "stockCard";
            stockCard.Padding = new Padding(18, 8, 18, 12);
            stockCard.RowCount = 4;
            stockCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            stockCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            stockCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            stockCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            stockCard.Size = new Size(1080, 172);
            stockCard.TabIndex = 2;
            //
            // stockLabel
            //
            stockLabel.Dock = DockStyle.Fill;
            stockLabel.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            stockLabel.Name = "stockLabel";
            stockLabel.Text = "Stock Quantity";
            stockLabel.TextAlign = ContentAlignment.BottomLeft;
            //
            // reorderLabel
            //
            reorderLabel.Dock = DockStyle.Fill;
            reorderLabel.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            reorderLabel.Name = "reorderLabel";
            reorderLabel.Text = "Low Stock Threshold";
            reorderLabel.TextAlign = ContentAlignment.BottomLeft;
            //
            // stockInput
            //
            stockInput.DecimalPlaces = 3;
            stockInput.Dock = DockStyle.Top;
            stockInput.Font = new Font("Segoe UI", 11F);
            stockInput.Margin = new Padding(3, 4, 24, 3);
            stockInput.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            stockInput.Name = "stockInput";
            stockInput.TabIndex = 9;
            stockInput.ThousandsSeparator = true;
            //
            // reorderInput
            //
            reorderInput.DecimalPlaces = 3;
            reorderInput.Dock = DockStyle.Top;
            reorderInput.Font = new Font("Segoe UI", 11F);
            reorderInput.Margin = new Padding(3, 4, 3, 3);
            reorderInput.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            reorderInput.Name = "reorderInput";
            reorderInput.TabIndex = 10;
            reorderInput.ThousandsSeparator = true;
            //
            // costLabel
            //
            costLabel.Dock = DockStyle.Fill;
            costLabel.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            costLabel.Name = "costLabel";
            costLabel.Text = "Avg cost";
            costLabel.TextAlign = ContentAlignment.BottomLeft;
            //
            // costInput
            //
            costInput.DecimalPlaces = 2;
            costInput.Dock = DockStyle.Top;
            costInput.Font = new Font("Segoe UI", 11F);
            costInput.Margin = new Padding(3, 4, 24, 3);
            costInput.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            costInput.Name = "costInput";
            costInput.TabIndex = 11;
            costInput.ThousandsSeparator = true;
            //
            // activeCheck
            //
            activeCheck.Anchor = AnchorStyles.Left;
            activeCheck.AutoSize = true;
            activeCheck.Checked = true;
            activeCheck.CheckState = CheckState.Checked;
            activeCheck.Font = new Font("Segoe UI", 10.5F);
            activeCheck.Name = "activeCheck";
            activeCheck.TabIndex = 12;
            activeCheck.Text = "Active";
            //
            // buttonsPanel
            //
            buttonsPanel.BackColor = Color.White;
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(saveButton);
            buttonsPanel.Dock = DockStyle.Bottom;
            buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonsPanel.Name = "buttonsPanel";
            buttonsPanel.Padding = new Padding(16, 10, 16, 10);
            buttonsPanel.Size = new Size(1120, 64);
            buttonsPanel.TabIndex = 2;
            //
            // cancelButton
            //
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Font = new Font("Segoe UI", 10F);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(110, 40);
            cancelButton.TabIndex = 14;
            cancelButton.Text = "Cancel";
            //
            // saveButton
            //
            saveButton.BackColor = Color.FromArgb(28, 110, 92);
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.FlatStyle = FlatStyle.Flat;
            saveButton.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            saveButton.ForeColor = Color.White;
            saveButton.Name = "saveButton";
            saveButton.Size = new Size(150, 40);
            saveButton.TabIndex = 13;
            saveButton.Text = "Save Product";
            saveButton.UseVisualStyleBackColor = false;
            saveButton.Click += saveButton_Click;
            //
            // AddItemForm
            //
            AcceptButton = saveButton;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(238, 241, 244);
            CancelButton = cancelButton;
            ClientSize = new Size(1120, 870);
            Controls.Add(bodyPanel);
            Controls.Add(headerPanel);
            Controls.Add(buttonsPanel);
            MaximizeBox = true;
            MinimizeBox = false;
            MinimumSize = new Size(980, 700);
            Name = "AddItemForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add item";
            Load += AddItemForm_Load;
            headerPanel.ResumeLayout(false);
            bodyPanel.ResumeLayout(false);
            stockCard.ResumeLayout(false);
            stockCard.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)stockInput).EndInit();
            ((System.ComponentModel.ISupportInitialize)reorderInput).EndInit();
            ((System.ComponentModel.ISupportInitialize)costInput).EndInit();
            mainCard.ResumeLayout(false);
            leftStack.ResumeLayout(false);
            leftStack.PerformLayout();
            sizeColorTable.ResumeLayout(false);
            sizeColorTable.PerformLayout();
            rightStack.ResumeLayout(false);
            unitRow.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)packagingGrid).EndInit();
            packagingButtons.ResumeLayout(false);
            buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel headerPanel;
        private Label subtitleLabel;
        private Label titleLabel;
        private Label breadcrumbLabel;
        private Panel bodyPanel;
        private TableLayoutPanel mainCard;
        private TableLayoutPanel leftStack;
        private Label nameLabel;
        private TextBox nameText;
        private Label categoryLabel;
        private ComboBox categoryCombo;
        private Label typeLabel;
        private ComboBox typeCombo;
        private TableLayoutPanel sizeColorTable;
        private Label scaleLabel;
        private Panel scaleRow;
        private CheckBox scaleCheck;
        private TextBox scaleCodeText;
        private Label scaleHintLabel;
        private Label sizeLabel;
        private Label colorLabel;
        private TextBox sizeText;
        private TextBox colorText;
        private TableLayoutPanel rightStack;
        private Label baseTitleLabel;
        private Label baseInfoLabel;
        private Label unitLabel;
        private Panel unitRow;
        private ComboBox unitCombo;
        private CheckBox weightedCheck;
        private Label unitsTitleLabel;
        private Label unitsSubLabel;
        private DataGridView packagingGrid;
        private FlowLayoutPanel packagingButtons;
        private Button addPackagingButton;
        private Button removePackagingButton;
        private Panel spacerPanel;
        private TableLayoutPanel stockCard;
        private Label stockLabel;
        private Label reorderLabel;
        private NumericUpDown stockInput;
        private NumericUpDown reorderInput;
        private Label costLabel;
        private NumericUpDown costInput;
        private CheckBox activeCheck;
        private FlowLayoutPanel buttonsPanel;
        private Button cancelButton;
        private Button saveButton;
    }
}
