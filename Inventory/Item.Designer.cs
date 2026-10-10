namespace Inventory
{
    partial class Item
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
        /// Layout: blue header; categories (left), products (center), product details (right).
        /// The category rows, the product cells and the page buttons are drawn / built in code (Item.cs).
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle gridHeaderStyle = new DataGridViewCellStyle();
            DataGridViewCellStyle gridRowStyle = new DataGridViewCellStyle();
            DataGridViewCellStyle numberStyle = new DataGridViewCellStyle();
            DataGridViewCellStyle priceStyle = new DataGridViewCellStyle();
            DataGridViewCellStyle unitsHeaderStyle = new DataGridViewCellStyle();
            headerBar = new Panel();
            headerTitleLabel = new Label();
            userLabel = new Label();
            bodyTable = new TableLayoutPanel();
            categoriesCard = new Panel();
            categoryTree = new TreeView();
            categoryMenu = new ContextMenuStrip(components);
            addCategoryMenuItem = new ToolStripMenuItem();
            addSubCategoryMenuItem = new ToolStripMenuItem();
            editCategoryMenuItem = new ToolStripMenuItem();
            deleteCategoryMenuItem = new ToolStripMenuItem();
            categoryButtonsPanel = new FlowLayoutPanel();
            addSubCategoryButton = new Button();
            editCategoryButton = new Button();
            deleteCategoryButton = new Button();
            categoriesHeader = new Panel();
            categoriesTitleLabel = new Label();
            addCategoryButton = new Button();
            centerCard = new Panel();
            itemsGrid = new DataGridView();
            numberColumn = new DataGridViewTextBoxColumn();
            nameColumn = new DataGridViewTextBoxColumn();
            categoryColumn = new DataGridViewTextBoxColumn();
            stockColumn = new DataGridViewTextBoxColumn();
            priceColumn = new DataGridViewTextBoxColumn();
            statusColumn = new DataGridViewTextBoxColumn();
            emptyLabel = new Label();
            titlePanel = new Panel();
            listTitleLabel = new Label();
            showDetailsButton = new Button();
            topRow = new TableLayoutPanel();
            searchBox = new TextBox();
            chipsPanel = new FlowLayoutPanel();
            allChip = new Button();
            inStockChip = new Button();
            lowChip = new Button();
            outChip = new Button();
            excelButton = new Button();
            addButton = new Button();
            footerPanel = new Panel();
            showingLabel = new Label();
            pagesPanel = new FlowLayoutPanel();
            pageSizeLabel = new Label();
            pageSizeCombo = new ComboBox();
            detailsCard = new Panel();
            detailsContent = new Panel();
            buttonsRow = new FlowLayoutPanel();
            detailEditButton = new Button();
            detailDeleteButton = new Button();
            stockBox = new TableLayoutPanel();
            stockCaptionLabel = new Label();
            stockValueLabel = new Label();
            reorderCaptionLabel = new Label();
            reorderValueLabel = new Label();
            stockTitleLabel = new Label();
            unitsHint = new Label();
            unitsGrid = new DataGridView();
            unitsTitleLabel = new Label();
            detailStatusLabel = new Label();
            detailNameLabel = new Label();
            noSelectionLabel = new Label();
            detailsHeader = new Panel();
            detailsTitleLabel = new Label();
            closeDetailsButton = new Button();
            excelMenu = new ContextMenuStrip(components);
            importMenuItem = new ToolStripMenuItem();
            templateMenuItem = new ToolStripMenuItem();
            searchTimer = new System.Windows.Forms.Timer(components);
            headerBar.SuspendLayout();
            bodyTable.SuspendLayout();
            categoriesCard.SuspendLayout();
            categoryMenu.SuspendLayout();
            categoryButtonsPanel.SuspendLayout();
            categoriesHeader.SuspendLayout();
            centerCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)itemsGrid).BeginInit();
            titlePanel.SuspendLayout();
            topRow.SuspendLayout();
            chipsPanel.SuspendLayout();
            footerPanel.SuspendLayout();
            pagesPanel.SuspendLayout();
            detailsCard.SuspendLayout();
            detailsContent.SuspendLayout();
            buttonsRow.SuspendLayout();
            stockBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)unitsGrid).BeginInit();
            detailsHeader.SuspendLayout();
            excelMenu.SuspendLayout();
            SuspendLayout();
            //
            // headerBar
            //
            headerBar.BackColor = Color.FromArgb(37, 99, 235);
            headerBar.Controls.Add(headerTitleLabel);
            headerBar.Controls.Add(userLabel);
            headerBar.Dock = DockStyle.Top;
            headerBar.Name = "headerBar";
            headerBar.Padding = new Padding(22, 0, 22, 0);
            headerBar.Size = new Size(1400, 64);
            headerBar.TabIndex = 0;
            //
            // headerTitleLabel
            //
            headerTitleLabel.Dock = DockStyle.Fill;
            headerTitleLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            headerTitleLabel.ForeColor = Color.White;
            headerTitleLabel.Name = "headerTitleLabel";
            headerTitleLabel.Text = "Product Management";
            headerTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // userLabel
            //
            userLabel.Dock = DockStyle.Right;
            userLabel.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            userLabel.ForeColor = Color.White;
            userLabel.Name = "userLabel";
            userLabel.Size = new Size(260, 64);
            userLabel.TextAlign = ContentAlignment.MiddleRight;
            //
            // bodyTable
            //
            bodyTable.BackColor = Color.FromArgb(238, 241, 244);
            bodyTable.ColumnCount = 3;
            bodyTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270F));
            bodyTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bodyTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340F));
            bodyTable.Controls.Add(categoriesCard, 0, 0);
            bodyTable.Controls.Add(centerCard, 1, 0);
            bodyTable.Controls.Add(detailsCard, 2, 0);
            bodyTable.Dock = DockStyle.Fill;
            bodyTable.Name = "bodyTable";
            bodyTable.Padding = new Padding(10);
            bodyTable.RowCount = 1;
            bodyTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            bodyTable.TabIndex = 1;
            //
            // categoriesCard
            //
            categoriesCard.BackColor = Color.White;
            categoriesCard.Controls.Add(categoryTree);
            categoriesCard.Controls.Add(categoryButtonsPanel);
            categoriesCard.Controls.Add(categoriesHeader);
            categoriesCard.Dock = DockStyle.Fill;
            categoriesCard.Margin = new Padding(4);
            categoriesCard.Name = "categoriesCard";
            categoriesCard.Padding = new Padding(8, 4, 8, 8);
            categoriesCard.TabIndex = 0;
            //
            // categoriesHeader
            //
            categoriesHeader.Controls.Add(categoriesTitleLabel);
            categoriesHeader.Controls.Add(addCategoryButton);
            categoriesHeader.Dock = DockStyle.Top;
            categoriesHeader.Name = "categoriesHeader";
            categoriesHeader.Padding = new Padding(6, 10, 2, 8);
            categoriesHeader.Size = new Size(254, 56);
            categoriesHeader.TabIndex = 0;
            //
            // categoriesTitleLabel
            //
            categoriesTitleLabel.Dock = DockStyle.Fill;
            categoriesTitleLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            categoriesTitleLabel.ForeColor = Color.FromArgb(22, 32, 43);
            categoriesTitleLabel.Name = "categoriesTitleLabel";
            categoriesTitleLabel.Text = "Categories";
            categoriesTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // addCategoryButton
            //
            addCategoryButton.BackColor = Color.FromArgb(37, 99, 235);
            addCategoryButton.Dock = DockStyle.Right;
            addCategoryButton.FlatAppearance.BorderSize = 0;
            addCategoryButton.FlatStyle = FlatStyle.Flat;
            addCategoryButton.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            addCategoryButton.ForeColor = Color.White;
            addCategoryButton.Name = "addCategoryButton";
            addCategoryButton.Size = new Size(38, 38);
            addCategoryButton.TabIndex = 0;
            addCategoryButton.Text = "+";
            addCategoryButton.UseVisualStyleBackColor = false;
            addCategoryButton.Click += addCategoryButton_Click;
            //
            // categoryTree
            //
            categoryTree.BorderStyle = BorderStyle.None;
            categoryTree.ContextMenuStrip = categoryMenu;
            categoryTree.Dock = DockStyle.Fill;
            categoryTree.DrawMode = TreeViewDrawMode.OwnerDrawText;
            categoryTree.Font = new Font("Segoe UI", 10F);
            categoryTree.FullRowSelect = true;
            categoryTree.HideSelection = false;
            categoryTree.Indent = 14;
            categoryTree.ItemHeight = 40;
            categoryTree.Name = "categoryTree";
            categoryTree.ShowLines = false;
            categoryTree.TabIndex = 1;
            categoryTree.AfterSelect += categoryTree_AfterSelect;
            categoryTree.DrawNode += categoryTree_DrawNode;
            categoryTree.NodeMouseClick += categoryTree_NodeMouseClick;
            //
            // categoryMenu
            //
            categoryMenu.ImageScalingSize = new Size(20, 20);
            categoryMenu.Items.AddRange(new ToolStripItem[] { addCategoryMenuItem, addSubCategoryMenuItem, editCategoryMenuItem, deleteCategoryMenuItem });
            categoryMenu.Name = "categoryMenu";
            categoryMenu.Size = new Size(190, 100);
            categoryMenu.Opening += categoryMenu_Opening;
            //
            // addCategoryMenuItem
            //
            addCategoryMenuItem.Name = "addCategoryMenuItem";
            addCategoryMenuItem.Size = new Size(189, 24);
            addCategoryMenuItem.Text = "Add category";
            addCategoryMenuItem.Click += addCategoryButton_Click;
            //
            // addSubCategoryMenuItem
            //
            addSubCategoryMenuItem.Name = "addSubCategoryMenuItem";
            addSubCategoryMenuItem.Size = new Size(189, 24);
            addSubCategoryMenuItem.Text = "Add sub-category";
            addSubCategoryMenuItem.Click += addSubCategoryButton_Click;
            //
            // editCategoryMenuItem
            //
            editCategoryMenuItem.Name = "editCategoryMenuItem";
            editCategoryMenuItem.Size = new Size(189, 24);
            editCategoryMenuItem.Text = "Edit / move";
            editCategoryMenuItem.Click += editCategoryButton_Click;
            //
            // deleteCategoryMenuItem
            //
            deleteCategoryMenuItem.Name = "deleteCategoryMenuItem";
            deleteCategoryMenuItem.Size = new Size(189, 24);
            deleteCategoryMenuItem.Text = "Delete";
            deleteCategoryMenuItem.Click += deleteCategoryButton_Click;
            //
            // categoryButtonsPanel
            //
            categoryButtonsPanel.Controls.Add(addSubCategoryButton);
            categoryButtonsPanel.Controls.Add(editCategoryButton);
            categoryButtonsPanel.Controls.Add(deleteCategoryButton);
            categoryButtonsPanel.Dock = DockStyle.Bottom;
            categoryButtonsPanel.Name = "categoryButtonsPanel";
            categoryButtonsPanel.Padding = new Padding(0, 6, 0, 0);
            categoryButtonsPanel.Size = new Size(254, 44);
            categoryButtonsPanel.TabIndex = 2;
            categoryButtonsPanel.WrapContents = false;
            //
            // addSubCategoryButton
            //
            addSubCategoryButton.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 222);
            addSubCategoryButton.FlatStyle = FlatStyle.Flat;
            addSubCategoryButton.Margin = new Padding(0, 0, 6, 0);
            addSubCategoryButton.Name = "addSubCategoryButton";
            addSubCategoryButton.Size = new Size(70, 32);
            addSubCategoryButton.TabIndex = 0;
            addSubCategoryButton.Text = "+ Sub";
            addSubCategoryButton.Click += addSubCategoryButton_Click;
            //
            // editCategoryButton
            //
            editCategoryButton.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 222);
            editCategoryButton.FlatStyle = FlatStyle.Flat;
            editCategoryButton.Margin = new Padding(0, 0, 6, 0);
            editCategoryButton.Name = "editCategoryButton";
            editCategoryButton.Size = new Size(64, 32);
            editCategoryButton.TabIndex = 1;
            editCategoryButton.Text = "Edit";
            editCategoryButton.Click += editCategoryButton_Click;
            //
            // deleteCategoryButton
            //
            deleteCategoryButton.FlatAppearance.BorderColor = Color.FromArgb(240, 190, 190);
            deleteCategoryButton.FlatStyle = FlatStyle.Flat;
            deleteCategoryButton.ForeColor = Color.Firebrick;
            deleteCategoryButton.Margin = new Padding(0);
            deleteCategoryButton.Name = "deleteCategoryButton";
            deleteCategoryButton.Size = new Size(70, 32);
            deleteCategoryButton.TabIndex = 2;
            deleteCategoryButton.Text = "Delete";
            deleteCategoryButton.Click += deleteCategoryButton_Click;
            //
            // centerCard
            //
            centerCard.BackColor = Color.White;
            centerCard.Controls.Add(itemsGrid);
            centerCard.Controls.Add(emptyLabel);
            centerCard.Controls.Add(titlePanel);
            centerCard.Controls.Add(topRow);
            centerCard.Controls.Add(footerPanel);
            centerCard.Dock = DockStyle.Fill;
            centerCard.Margin = new Padding(8, 4, 8, 4);
            centerCard.Name = "centerCard";
            centerCard.Padding = new Padding(12, 8, 12, 4);
            centerCard.TabIndex = 1;
            //
            // topRow
            //
            topRow.ColumnCount = 3;
            topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98F));
            topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 156F));
            topRow.Controls.Add(searchBox, 0, 0);
            topRow.Controls.Add(excelButton, 1, 0);
            topRow.Controls.Add(addButton, 2, 0);
            topRow.Dock = DockStyle.Top;
            topRow.Name = "topRow";
            topRow.RowCount = 1;
            topRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            topRow.Size = new Size(600, 52);
            topRow.TabIndex = 0;
            //
            // searchBox
            //
            searchBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            searchBox.Font = new Font("Segoe UI", 11F);
            searchBox.Name = "searchBox";
            searchBox.PlaceholderText = "Search products by name, barcode...";
            searchBox.TabIndex = 0;
            searchBox.TextChanged += searchBox_TextChanged;
            searchBox.KeyDown += searchBox_KeyDown;
            //
            // chipsPanel
            //
            chipsPanel.Controls.Add(allChip);
            chipsPanel.Controls.Add(inStockChip);
            chipsPanel.Controls.Add(lowChip);
            chipsPanel.Controls.Add(outChip);
            chipsPanel.AutoSize = true;
            chipsPanel.Dock = DockStyle.Right;
            chipsPanel.Name = "chipsPanel";
            chipsPanel.Padding = new Padding(0, 4, 8, 0);
            chipsPanel.TabIndex = 1;
            chipsPanel.WrapContents = false;
            //
            // allChip
            //
            allChip.FlatStyle = FlatStyle.Flat;
            allChip.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            allChip.Margin = new Padding(0, 0, 6, 0);
            allChip.Name = "allChip";
            allChip.Size = new Size(48, 32);
            allChip.TabIndex = 0;
            allChip.Text = "All";
            allChip.UseVisualStyleBackColor = false;
            //
            // inStockChip
            //
            inStockChip.FlatStyle = FlatStyle.Flat;
            inStockChip.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            inStockChip.Margin = new Padding(0, 0, 6, 0);
            inStockChip.Name = "inStockChip";
            inStockChip.Size = new Size(84, 32);
            inStockChip.TabIndex = 1;
            inStockChip.Text = "In Stock";
            inStockChip.UseVisualStyleBackColor = false;
            //
            // lowChip
            //
            lowChip.FlatStyle = FlatStyle.Flat;
            lowChip.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            lowChip.Margin = new Padding(0, 0, 6, 0);
            lowChip.Name = "lowChip";
            lowChip.Size = new Size(92, 32);
            lowChip.TabIndex = 2;
            lowChip.Text = "Low Stock";
            lowChip.UseVisualStyleBackColor = false;
            //
            // outChip
            //
            outChip.FlatStyle = FlatStyle.Flat;
            outChip.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            outChip.Margin = new Padding(0);
            outChip.Name = "outChip";
            outChip.Size = new Size(108, 32);
            outChip.TabIndex = 3;
            outChip.Text = "Out of Stock";
            outChip.UseVisualStyleBackColor = false;
            //
            // excelButton
            //
            excelButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            excelButton.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 222);
            excelButton.FlatStyle = FlatStyle.Flat;
            excelButton.Name = "excelButton";
            excelButton.Size = new Size(86, 36);
            excelButton.TabIndex = 2;
            excelButton.Text = "Excel ▾";
            excelButton.Click += excelButton_Click;
            //
            // addButton
            //
            addButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            addButton.BackColor = Color.FromArgb(37, 99, 235);
            addButton.FlatAppearance.BorderSize = 0;
            addButton.FlatStyle = FlatStyle.Flat;
            addButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            addButton.ForeColor = Color.White;
            addButton.Name = "addButton";
            addButton.Size = new Size(144, 38);
            addButton.TabIndex = 3;
            addButton.Text = "+ Add Product";
            addButton.UseVisualStyleBackColor = false;
            addButton.Click += addButton_Click;
            //
            // titlePanel
            //
            titlePanel.Controls.Add(listTitleLabel);
            titlePanel.Controls.Add(chipsPanel);
            titlePanel.Controls.Add(showDetailsButton);
            titlePanel.Dock = DockStyle.Top;
            titlePanel.Name = "titlePanel";
            titlePanel.Padding = new Padding(0, 4, 0, 4);
            titlePanel.Size = new Size(600, 48);
            titlePanel.TabIndex = 1;
            //
            // listTitleLabel
            //
            listTitleLabel.AutoEllipsis = true;
            listTitleLabel.Dock = DockStyle.Fill;
            listTitleLabel.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            listTitleLabel.ForeColor = Color.FromArgb(22, 32, 43);
            listTitleLabel.Name = "listTitleLabel";
            listTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // showDetailsButton
            //
            showDetailsButton.Dock = DockStyle.Right;
            showDetailsButton.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 222);
            showDetailsButton.FlatStyle = FlatStyle.Flat;
            showDetailsButton.Name = "showDetailsButton";
            showDetailsButton.Size = new Size(110, 38);
            showDetailsButton.TabIndex = 0;
            showDetailsButton.Text = "Details ›";
            showDetailsButton.Visible = false;
            showDetailsButton.Click += showDetailsButton_Click;
            //
            // emptyLabel
            //
            emptyLabel.Dock = DockStyle.Top;
            emptyLabel.ForeColor = Color.FromArgb(100, 110, 122);
            emptyLabel.Name = "emptyLabel";
            emptyLabel.Size = new Size(600, 40);
            emptyLabel.Text = "No products match. Change the category, the search or the stock filter.";
            emptyLabel.TextAlign = ContentAlignment.MiddleLeft;
            emptyLabel.Visible = false;
            //
            // itemsGrid
            //
            itemsGrid.AllowUserToAddRows = false;
            itemsGrid.AllowUserToDeleteRows = false;
            itemsGrid.AllowUserToResizeRows = false;
            itemsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            itemsGrid.BackgroundColor = Color.White;
            itemsGrid.BorderStyle = BorderStyle.FixedSingle;
            itemsGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            itemsGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            gridHeaderStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            gridHeaderStyle.BackColor = Color.White;
            gridHeaderStyle.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            gridHeaderStyle.ForeColor = Color.FromArgb(70, 80, 92);
            gridHeaderStyle.SelectionBackColor = Color.White;
            gridHeaderStyle.SelectionForeColor = Color.FromArgb(70, 80, 92);
            gridHeaderStyle.WrapMode = DataGridViewTriState.True;
            itemsGrid.ColumnHeadersDefaultCellStyle = gridHeaderStyle;
            itemsGrid.ColumnHeadersHeight = 44;
            itemsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            itemsGrid.Columns.AddRange(new DataGridViewColumn[] { numberColumn, nameColumn, categoryColumn, stockColumn, priceColumn, statusColumn });
            gridRowStyle.BackColor = Color.White;
            gridRowStyle.Font = new Font("Segoe UI", 10F);
            gridRowStyle.ForeColor = Color.FromArgb(22, 32, 43);
            gridRowStyle.SelectionBackColor = Color.FromArgb(222, 233, 253);
            gridRowStyle.SelectionForeColor = Color.FromArgb(22, 32, 43);
            itemsGrid.DefaultCellStyle = gridRowStyle;
            itemsGrid.Dock = DockStyle.Fill;
            itemsGrid.EnableHeadersVisualStyles = false;
            itemsGrid.GridColor = Color.FromArgb(232, 235, 238);
            itemsGrid.MultiSelect = false;
            itemsGrid.Name = "itemsGrid";
            itemsGrid.ReadOnly = true;
            itemsGrid.RowHeadersVisible = false;
            itemsGrid.RowHeadersWidth = 51;
            itemsGrid.RowTemplate.Height = 54;
            itemsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            itemsGrid.TabIndex = 2;
            itemsGrid.CellClick += itemsGrid_CellClick;
            itemsGrid.CellDoubleClick += itemsGrid_CellDoubleClick;
            itemsGrid.CellFormatting += itemsGrid_CellFormatting;
            itemsGrid.CellMouseEnter += itemsGrid_CellMouseEnter;
            itemsGrid.CellPainting += itemsGrid_CellPainting;
            itemsGrid.SelectionChanged += itemsGrid_SelectionChanged;
            //
            // numberColumn
            //
            numberColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            numberStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            numberColumn.DefaultCellStyle = numberStyle;
            numberColumn.HeaderText = "";
            numberColumn.Name = "numberColumn";
            numberColumn.ReadOnly = true;
            numberColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            numberColumn.Width = 48;
            //
            // nameColumn
            //
            nameColumn.DataPropertyName = "Name";
            nameColumn.FillWeight = 170F;
            nameColumn.HeaderText = "PRODUCT NAME";
            nameColumn.Name = "nameColumn";
            nameColumn.ReadOnly = true;
            nameColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // categoryColumn
            //
            categoryColumn.DataPropertyName = "CategoryName";
            categoryColumn.FillWeight = 90F;
            categoryColumn.HeaderText = "CATEGORY";
            categoryColumn.Name = "categoryColumn";
            categoryColumn.ReadOnly = true;
            categoryColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // stockColumn
            //
            stockColumn.DataPropertyName = "StockQty";
            stockColumn.FillWeight = 70F;
            stockColumn.HeaderText = "STOCK QTY";
            stockColumn.Name = "stockColumn";
            stockColumn.ReadOnly = true;
            stockColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // priceColumn
            //
            priceColumn.DataPropertyName = "SalePrice";
            priceColumn.FillWeight = 70F;
            priceStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            priceStyle.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            priceColumn.DefaultCellStyle = priceStyle;
            priceColumn.HeaderText = "SALE PRICE";
            priceColumn.Name = "priceColumn";
            priceColumn.ReadOnly = true;
            priceColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // statusColumn
            //
            statusColumn.DataPropertyName = "Barcode";
            statusColumn.FillWeight = 135F;
            statusColumn.HeaderText = "BARCODE & STATUS";
            statusColumn.Name = "statusColumn";
            statusColumn.ReadOnly = true;
            statusColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // footerPanel
            //
            footerPanel.Controls.Add(showingLabel);
            footerPanel.Controls.Add(pagesPanel);
            footerPanel.Dock = DockStyle.Bottom;
            footerPanel.Name = "footerPanel";
            footerPanel.Padding = new Padding(0, 8, 0, 4);
            footerPanel.Size = new Size(600, 50);
            footerPanel.TabIndex = 3;
            //
            // showingLabel
            //
            showingLabel.Dock = DockStyle.Fill;
            showingLabel.ForeColor = Color.FromArgb(70, 80, 92);
            showingLabel.Name = "showingLabel";
            showingLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // pagesPanel
            //
            pagesPanel.AutoSize = true;
            pagesPanel.Controls.Add(pageSizeLabel);
            pagesPanel.Controls.Add(pageSizeCombo);
            pagesPanel.Dock = DockStyle.Right;
            pagesPanel.Name = "pagesPanel";
            pagesPanel.TabIndex = 0;
            pagesPanel.WrapContents = false;
            //
            // pageSizeLabel
            //
            pageSizeLabel.AutoSize = true;
            pageSizeLabel.ForeColor = Color.FromArgb(70, 80, 92);
            pageSizeLabel.Margin = new Padding(0, 7, 4, 0);
            pageSizeLabel.Name = "pageSizeLabel";
            pageSizeLabel.Text = "Per page";
            //
            // pageSizeCombo
            //
            pageSizeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            pageSizeCombo.Items.AddRange(new object[] { 25, 50, 100, 200 });
            pageSizeCombo.Margin = new Padding(0, 2, 10, 0);
            pageSizeCombo.Name = "pageSizeCombo";
            pageSizeCombo.Size = new Size(64, 28);
            pageSizeCombo.TabIndex = 0;
            pageSizeCombo.SelectedIndexChanged += pageSizeCombo_SelectedIndexChanged;
            //
            // detailsCard
            //
            detailsCard.BackColor = Color.White;
            detailsCard.Controls.Add(detailsContent);
            detailsCard.Controls.Add(noSelectionLabel);
            detailsCard.Controls.Add(detailsHeader);
            detailsCard.Dock = DockStyle.Fill;
            detailsCard.Margin = new Padding(4);
            detailsCard.Name = "detailsCard";
            detailsCard.Padding = new Padding(14, 4, 14, 10);
            detailsCard.TabIndex = 2;
            //
            // detailsHeader
            //
            detailsHeader.Controls.Add(detailsTitleLabel);
            detailsHeader.Controls.Add(closeDetailsButton);
            detailsHeader.Dock = DockStyle.Top;
            detailsHeader.Name = "detailsHeader";
            detailsHeader.Padding = new Padding(0, 8, 0, 6);
            detailsHeader.Size = new Size(304, 54);
            detailsHeader.TabIndex = 0;
            //
            // detailsTitleLabel
            //
            detailsTitleLabel.Dock = DockStyle.Fill;
            detailsTitleLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            detailsTitleLabel.ForeColor = Color.FromArgb(22, 32, 43);
            detailsTitleLabel.Name = "detailsTitleLabel";
            detailsTitleLabel.Text = "Product Details";
            detailsTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // closeDetailsButton
            //
            closeDetailsButton.Dock = DockStyle.Right;
            closeDetailsButton.FlatAppearance.BorderSize = 0;
            closeDetailsButton.FlatStyle = FlatStyle.Flat;
            closeDetailsButton.Font = new Font("Segoe UI", 12F);
            closeDetailsButton.ForeColor = Color.FromArgb(100, 110, 122);
            closeDetailsButton.Name = "closeDetailsButton";
            closeDetailsButton.Size = new Size(40, 40);
            closeDetailsButton.TabIndex = 0;
            closeDetailsButton.Text = "✕";
            closeDetailsButton.Click += closeDetailsButton_Click;
            //
            // noSelectionLabel
            //
            noSelectionLabel.Dock = DockStyle.Top;
            noSelectionLabel.ForeColor = Color.FromArgb(100, 110, 122);
            noSelectionLabel.Name = "noSelectionLabel";
            noSelectionLabel.Size = new Size(304, 60);
            noSelectionLabel.Text = "Select a product to see its units, prices and stock.";
            //
            // detailsContent
            //
            detailsContent.AutoScroll = true;
            detailsContent.Controls.Add(buttonsRow);
            detailsContent.Controls.Add(stockBox);
            detailsContent.Controls.Add(stockTitleLabel);
            detailsContent.Controls.Add(unitsHint);
            detailsContent.Controls.Add(unitsGrid);
            detailsContent.Controls.Add(unitsTitleLabel);
            detailsContent.Controls.Add(detailStatusLabel);
            detailsContent.Controls.Add(detailNameLabel);
            detailsContent.Dock = DockStyle.Fill;
            detailsContent.Name = "detailsContent";
            detailsContent.TabIndex = 1;
            //
            // detailNameLabel
            //
            detailNameLabel.Dock = DockStyle.Top;
            detailNameLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            detailNameLabel.ForeColor = Color.FromArgb(22, 32, 43);
            detailNameLabel.Name = "detailNameLabel";
            detailNameLabel.Size = new Size(304, 60);
            detailNameLabel.TextAlign = ContentAlignment.BottomLeft;
            //
            // detailStatusLabel
            //
            detailStatusLabel.Dock = DockStyle.Top;
            detailStatusLabel.Font = new Font("Segoe UI", 9.5F);
            detailStatusLabel.Name = "detailStatusLabel";
            detailStatusLabel.Size = new Size(304, 28);
            detailStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // unitsTitleLabel
            //
            unitsTitleLabel.Dock = DockStyle.Top;
            unitsTitleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            unitsTitleLabel.ForeColor = Color.FromArgb(22, 32, 43);
            unitsTitleLabel.Name = "unitsTitleLabel";
            unitsTitleLabel.Size = new Size(304, 40);
            unitsTitleLabel.Text = "Units & Pricing";
            unitsTitleLabel.TextAlign = ContentAlignment.BottomLeft;
            unitsTitleLabel.UseMnemonic = false;
            //
            // unitsGrid
            //
            unitsGrid.AllowUserToAddRows = false;
            unitsGrid.AllowUserToDeleteRows = false;
            unitsGrid.AllowUserToResizeRows = false;
            unitsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            unitsGrid.BackgroundColor = Color.White;
            unitsGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            unitsHeaderStyle.BackColor = Color.FromArgb(240, 242, 245);
            unitsHeaderStyle.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            unitsHeaderStyle.ForeColor = Color.FromArgb(22, 32, 43);
            unitsHeaderStyle.SelectionBackColor = Color.FromArgb(240, 242, 245);
            unitsGrid.ColumnHeadersDefaultCellStyle = unitsHeaderStyle;
            unitsGrid.ColumnHeadersHeight = 32;
            unitsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            unitsGrid.DefaultCellStyle.SelectionBackColor = Color.White;
            unitsGrid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(22, 32, 43);
            unitsGrid.Dock = DockStyle.Top;
            unitsGrid.EnableHeadersVisualStyles = false;
            unitsGrid.Font = new Font("Segoe UI", 9F);
            unitsGrid.GridColor = Color.FromArgb(225, 229, 234);
            unitsGrid.Name = "unitsGrid";
            unitsGrid.ReadOnly = true;
            unitsGrid.RowHeadersVisible = false;
            unitsGrid.RowHeadersWidth = 51;
            unitsGrid.RowTemplate.Height = 30;
            unitsGrid.ScrollBars = ScrollBars.Vertical;
            unitsGrid.Size = new Size(304, 160);
            unitsGrid.TabIndex = 2;
            unitsGrid.TabStop = false;
            unitsGrid.DataBindingComplete += unitsGrid_DataBindingComplete;
            //
            // unitsHint
            //
            unitsHint.Dock = DockStyle.Top;
            unitsHint.Font = new Font("Segoe UI", 8.5F);
            unitsHint.ForeColor = Color.FromArgb(100, 110, 122);
            unitsHint.Name = "unitsHint";
            unitsHint.Size = new Size(304, 24);
            unitsHint.TextAlign = ContentAlignment.MiddleLeft;
            //
            // stockTitleLabel
            //
            stockTitleLabel.Dock = DockStyle.Top;
            stockTitleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            stockTitleLabel.ForeColor = Color.FromArgb(22, 32, 43);
            stockTitleLabel.Name = "stockTitleLabel";
            stockTitleLabel.Size = new Size(304, 36);
            stockTitleLabel.Text = "Stock Information";
            stockTitleLabel.TextAlign = ContentAlignment.BottomLeft;
            //
            // stockBox
            //
            stockBox.BackColor = Color.FromArgb(248, 249, 251);
            stockBox.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            stockBox.ColumnCount = 2;
            stockBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            stockBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            stockBox.Controls.Add(stockCaptionLabel, 0, 0);
            stockBox.Controls.Add(stockValueLabel, 1, 0);
            stockBox.Controls.Add(reorderCaptionLabel, 0, 1);
            stockBox.Controls.Add(reorderValueLabel, 1, 1);
            stockBox.Dock = DockStyle.Top;
            stockBox.Margin = new Padding(0, 6, 0, 0);
            stockBox.Name = "stockBox";
            stockBox.RowCount = 2;
            stockBox.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            stockBox.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            stockBox.Size = new Size(304, 72);
            stockBox.TabIndex = 3;
            //
            // stockCaptionLabel
            //
            stockCaptionLabel.Dock = DockStyle.Fill;
            stockCaptionLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            stockCaptionLabel.Name = "stockCaptionLabel";
            stockCaptionLabel.Text = "Current Stock";
            stockCaptionLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // stockValueLabel
            //
            stockValueLabel.Dock = DockStyle.Fill;
            stockValueLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            stockValueLabel.Name = "stockValueLabel";
            stockValueLabel.TextAlign = ContentAlignment.MiddleRight;
            //
            // reorderCaptionLabel
            //
            reorderCaptionLabel.Dock = DockStyle.Fill;
            reorderCaptionLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            reorderCaptionLabel.Name = "reorderCaptionLabel";
            reorderCaptionLabel.Text = "Reorder Level";
            reorderCaptionLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // reorderValueLabel
            //
            reorderValueLabel.Dock = DockStyle.Fill;
            reorderValueLabel.ForeColor = Color.FromArgb(100, 110, 122);
            reorderValueLabel.Name = "reorderValueLabel";
            reorderValueLabel.TextAlign = ContentAlignment.MiddleRight;
            //
            // buttonsRow
            //
            buttonsRow.Controls.Add(detailEditButton);
            buttonsRow.Controls.Add(detailDeleteButton);
            buttonsRow.Dock = DockStyle.Top;
            buttonsRow.Name = "buttonsRow";
            buttonsRow.Padding = new Padding(0, 16, 0, 0);
            buttonsRow.Size = new Size(304, 68);
            buttonsRow.TabIndex = 4;
            buttonsRow.WrapContents = false;
            //
            // detailEditButton
            //
            detailEditButton.BackColor = Color.FromArgb(37, 99, 235);
            detailEditButton.FlatAppearance.BorderSize = 0;
            detailEditButton.FlatStyle = FlatStyle.Flat;
            detailEditButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            detailEditButton.ForeColor = Color.White;
            detailEditButton.Margin = new Padding(0, 0, 10, 0);
            detailEditButton.Name = "detailEditButton";
            detailEditButton.Size = new Size(160, 40);
            detailEditButton.TabIndex = 0;
            detailEditButton.Text = "✎  Edit Product";
            detailEditButton.UseVisualStyleBackColor = false;
            detailEditButton.Click += detailEditButton_Click;
            //
            // detailDeleteButton
            //
            detailDeleteButton.FlatAppearance.BorderColor = Color.FromArgb(240, 190, 190);
            detailDeleteButton.FlatStyle = FlatStyle.Flat;
            detailDeleteButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            detailDeleteButton.ForeColor = Color.Firebrick;
            detailDeleteButton.Margin = new Padding(0);
            detailDeleteButton.Name = "detailDeleteButton";
            detailDeleteButton.Size = new Size(110, 40);
            detailDeleteButton.TabIndex = 1;
            detailDeleteButton.Text = "Delete";
            detailDeleteButton.Click += detailDeleteButton_Click;
            //
            // excelMenu
            //
            excelMenu.ImageScalingSize = new Size(20, 20);
            excelMenu.Items.AddRange(new ToolStripItem[] { importMenuItem, templateMenuItem });
            excelMenu.Name = "excelMenu";
            excelMenu.Size = new Size(211, 52);
            //
            // importMenuItem
            //
            importMenuItem.Name = "importMenuItem";
            importMenuItem.Size = new Size(210, 24);
            importMenuItem.Text = "Import from Excel...";
            importMenuItem.Click += importButton_Click;
            //
            // templateMenuItem
            //
            templateMenuItem.Name = "templateMenuItem";
            templateMenuItem.Size = new Size(210, 24);
            templateMenuItem.Text = "Save Excel template...";
            templateMenuItem.Click += templateButton_Click;
            //
            // searchTimer
            //
            searchTimer.Interval = 400;
            searchTimer.Tick += searchTimer_Tick;
            //
            // Item
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(238, 241, 244);
            ClientSize = new Size(1400, 820);
            Controls.Add(bodyTable);
            Controls.Add(headerBar);
            MinimumSize = new Size(1150, 640);
            Name = "Item";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Product Management";
            WindowState = FormWindowState.Maximized;
            Load += Item_Load;
            headerBar.ResumeLayout(false);
            bodyTable.ResumeLayout(false);
            categoriesCard.ResumeLayout(false);
            categoryMenu.ResumeLayout(false);
            categoryButtonsPanel.ResumeLayout(false);
            categoriesHeader.ResumeLayout(false);
            centerCard.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)itemsGrid).EndInit();
            titlePanel.ResumeLayout(false);
            topRow.ResumeLayout(false);
            topRow.PerformLayout();
            chipsPanel.ResumeLayout(false);
            footerPanel.ResumeLayout(false);
            footerPanel.PerformLayout();
            pagesPanel.ResumeLayout(false);
            pagesPanel.PerformLayout();
            detailsCard.ResumeLayout(false);
            detailsContent.ResumeLayout(false);
            buttonsRow.ResumeLayout(false);
            stockBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)unitsGrid).EndInit();
            detailsHeader.ResumeLayout(false);
            excelMenu.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel headerBar;
        private Label headerTitleLabel;
        private Label userLabel;
        private TableLayoutPanel bodyTable;
        private Panel categoriesCard;
        private TreeView categoryTree;
        private ContextMenuStrip categoryMenu;
        private ToolStripMenuItem addCategoryMenuItem;
        private ToolStripMenuItem addSubCategoryMenuItem;
        private ToolStripMenuItem editCategoryMenuItem;
        private ToolStripMenuItem deleteCategoryMenuItem;
        private FlowLayoutPanel categoryButtonsPanel;
        private Button addSubCategoryButton;
        private Button editCategoryButton;
        private Button deleteCategoryButton;
        private Panel categoriesHeader;
        private Label categoriesTitleLabel;
        private Button addCategoryButton;
        private Panel centerCard;
        private DataGridView itemsGrid;
        private DataGridViewTextBoxColumn numberColumn;
        private DataGridViewTextBoxColumn nameColumn;
        private DataGridViewTextBoxColumn categoryColumn;
        private DataGridViewTextBoxColumn stockColumn;
        private DataGridViewTextBoxColumn priceColumn;
        private DataGridViewTextBoxColumn statusColumn;
        private Label emptyLabel;
        private Panel titlePanel;
        private Label listTitleLabel;
        private Button showDetailsButton;
        private TableLayoutPanel topRow;
        private TextBox searchBox;
        private FlowLayoutPanel chipsPanel;
        private Button allChip;
        private Button inStockChip;
        private Button lowChip;
        private Button outChip;
        private Button excelButton;
        private Button addButton;
        private Panel footerPanel;
        private Label showingLabel;
        private FlowLayoutPanel pagesPanel;
        private Label pageSizeLabel;
        private ComboBox pageSizeCombo;
        private Panel detailsCard;
        private Panel detailsContent;
        private FlowLayoutPanel buttonsRow;
        private Button detailEditButton;
        private Button detailDeleteButton;
        private TableLayoutPanel stockBox;
        private Label stockCaptionLabel;
        private Label stockValueLabel;
        private Label reorderCaptionLabel;
        private Label reorderValueLabel;
        private Label stockTitleLabel;
        private Label unitsHint;
        private DataGridView unitsGrid;
        private Label unitsTitleLabel;
        private Label detailStatusLabel;
        private Label detailNameLabel;
        private Label noSelectionLabel;
        private Panel detailsHeader;
        private Label detailsTitleLabel;
        private Button closeDetailsButton;
        private ContextMenuStrip excelMenu;
        private ToolStripMenuItem importMenuItem;
        private ToolStripMenuItem templateMenuItem;
        private System.Windows.Forms.Timer searchTimer;
    }
}
