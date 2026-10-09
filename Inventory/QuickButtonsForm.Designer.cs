namespace Inventory
{
    partial class QuickButtonsForm
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
        /// The grid cells (label box, color, switches) and the tab rows are drawn in code.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            headerPanel = new Panel();
            newTabButton = new Button();
            subtitleLabel = new Label();
            titleLabel = new Label();
            bodyTable = new TableLayoutPanel();
            tabsCard = new Panel();
            tabsList = new ListBox();
            tabsFooter = new Panel();
            deleteTabButton = new Button();
            downTabButton = new Button();
            upTabButton = new Button();
            tabsHeader = new Panel();
            tabsHint = new Label();
            tabsTitle = new Label();
            rightTable = new TableLayoutPanel();
            itemsCard = new Panel();
            itemsGrid = new DataGridView();
            gripColumn = new DataGridViewTextBoxColumn();
            itemColumn = new DataGridViewTextBoxColumn();
            labelColumn = new DataGridViewTextBoxColumn();
            priceColumn = new DataGridViewTextBoxColumn();
            colorColumn = new DataGridViewTextBoxColumn();
            askPriceColumn = new DataGridViewTextBoxColumn();
            showColumn = new DataGridViewTextBoxColumn();
            removeColumn = new DataGridViewTextBoxColumn();
            rowMenu = new ContextMenuStrip(components);
            moveToTabMenuItem = new ToolStripMenuItem();
            removeMenuItem = new ToolStripMenuItem();
            emptyLabel = new Label();
            tabHeader = new Panel();
            editTabButton = new Button();
            addItemsButton = new Button();
            tabInfoLabel = new Label();
            tabNameLabel = new Label();
            tabSwatch = new Panel();
            previewCard = new Panel();
            previewItems = new FlowLayoutPanel();
            previewTabs = new FlowLayoutPanel();
            previewHeader = new Panel();
            previewNote = new Label();
            columnsLabel = new Label();
            columnsUpDown = new NumericUpDown();
            previewTitle = new Label();
            colorMenu = new ContextMenuStrip(components);
            headerPanel.SuspendLayout();
            bodyTable.SuspendLayout();
            tabsCard.SuspendLayout();
            tabsFooter.SuspendLayout();
            tabsHeader.SuspendLayout();
            rightTable.SuspendLayout();
            itemsCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)itemsGrid).BeginInit();
            rowMenu.SuspendLayout();
            tabHeader.SuspendLayout();
            previewCard.SuspendLayout();
            previewHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)columnsUpDown).BeginInit();
            SuspendLayout();
            // 
            // headerPanel
            // 
            headerPanel.Controls.Add(newTabButton);
            headerPanel.Controls.Add(subtitleLabel);
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Location = new Point(12, 12);
            headerPanel.Name = "headerPanel";
            headerPanel.Size = new Size(1256, 70);
            headerPanel.TabIndex = 0;
            // 
            // newTabButton
            // 
            newTabButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            newTabButton.BackColor = Color.FromArgb(28, 110, 92);
            newTabButton.FlatAppearance.BorderSize = 0;
            newTabButton.FlatStyle = FlatStyle.Flat;
            newTabButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            newTabButton.ForeColor = Color.White;
            newTabButton.Location = new Point(1116, 6);
            newTabButton.Name = "newTabButton";
            newTabButton.Size = new Size(140, 42);
            newTabButton.TabIndex = 2;
            newTabButton.Text = "+ New tab";
            newTabButton.UseVisualStyleBackColor = false;
            newTabButton.Click += newTabButton_Click;
            // 
            // subtitleLabel
            // 
            subtitleLabel.AutoSize = true;
            subtitleLabel.ForeColor = Color.FromArgb(90, 98, 108);
            subtitleLabel.Location = new Point(0, 42);
            subtitleLabel.Name = "subtitleLabel";
            subtitleLabel.Size = new Size(597, 20);
            subtitleLabel.TabIndex = 1;
            subtitleLabel.Text = "Tabs and item buttons on the right side of the cashier screen, for items without a barcode";
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(22, 32, 43);
            titleLabel.Location = new Point(-4, 0);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(190, 41);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "POS buttons";
            // 
            // bodyTable
            // 
            bodyTable.ColumnCount = 2;
            bodyTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            bodyTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bodyTable.Controls.Add(tabsCard, 0, 0);
            bodyTable.Controls.Add(rightTable, 1, 0);
            bodyTable.Dock = DockStyle.Fill;
            bodyTable.Location = new Point(12, 82);
            bodyTable.Name = "bodyTable";
            bodyTable.RowCount = 1;
            bodyTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            bodyTable.Size = new Size(1256, 726);
            bodyTable.TabIndex = 1;
            // 
            // tabsCard
            // 
            tabsCard.BackColor = Color.White;
            tabsCard.BorderStyle = BorderStyle.FixedSingle;
            tabsCard.Controls.Add(tabsList);
            tabsCard.Controls.Add(tabsFooter);
            tabsCard.Controls.Add(tabsHeader);
            tabsCard.Dock = DockStyle.Fill;
            tabsCard.Location = new Point(0, 0);
            tabsCard.Margin = new Padding(0, 0, 12, 0);
            tabsCard.Name = "tabsCard";
            tabsCard.Size = new Size(288, 726);
            tabsCard.TabIndex = 0;
            // 
            // tabsList
            // 
            tabsList.AllowDrop = true;
            tabsList.BorderStyle = BorderStyle.None;
            tabsList.Dock = DockStyle.Fill;
            tabsList.DrawMode = DrawMode.OwnerDrawFixed;
            tabsList.IntegralHeight = false;
            tabsList.ItemHeight = 56;
            tabsList.Location = new Point(0, 46);
            tabsList.Name = "tabsList";
            tabsList.Size = new Size(286, 618);
            tabsList.TabIndex = 0;
            tabsList.DrawItem += tabsList_DrawItem;
            tabsList.SelectedIndexChanged += tabsList_SelectedIndexChanged;
            tabsList.DragDrop += tabsList_DragDrop;
            tabsList.DragOver += tabsList_DragOver;
            tabsList.DoubleClick += tabsList_DoubleClick;
            tabsList.MouseDown += tabsList_MouseDown;
            tabsList.MouseMove += tabsList_MouseMove;
            // 
            // tabsFooter
            // 
            tabsFooter.Controls.Add(deleteTabButton);
            tabsFooter.Controls.Add(downTabButton);
            tabsFooter.Controls.Add(upTabButton);
            tabsFooter.Dock = DockStyle.Bottom;
            tabsFooter.Location = new Point(0, 664);
            tabsFooter.Name = "tabsFooter";
            tabsFooter.Size = new Size(286, 60);
            tabsFooter.TabIndex = 1;
            // 
            // deleteTabButton
            // 
            deleteTabButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            deleteTabButton.FlatAppearance.BorderColor = Color.FromArgb(230, 180, 180);
            deleteTabButton.FlatStyle = FlatStyle.Flat;
            deleteTabButton.ForeColor = Color.Firebrick;
            deleteTabButton.Location = new Point(176, 12);
            deleteTabButton.Name = "deleteTabButton";
            deleteTabButton.Size = new Size(98, 36);
            deleteTabButton.TabIndex = 2;
            deleteTabButton.Text = "Delete tab";
            deleteTabButton.Click += deleteTabButton_Click;
            // 
            // downTabButton
            // 
            downTabButton.FlatAppearance.BorderColor = Color.Silver;
            downTabButton.FlatStyle = FlatStyle.Flat;
            downTabButton.Location = new Point(56, 12);
            downTabButton.Name = "downTabButton";
            downTabButton.Size = new Size(40, 36);
            downTabButton.TabIndex = 1;
            downTabButton.Text = "▼";
            downTabButton.Click += downTabButton_Click;
            // 
            // upTabButton
            // 
            upTabButton.FlatAppearance.BorderColor = Color.Silver;
            upTabButton.FlatStyle = FlatStyle.Flat;
            upTabButton.Location = new Point(10, 12);
            upTabButton.Name = "upTabButton";
            upTabButton.Size = new Size(40, 36);
            upTabButton.TabIndex = 0;
            upTabButton.Text = "▲";
            upTabButton.Click += upTabButton_Click;
            // 
            // tabsHeader
            // 
            tabsHeader.Controls.Add(tabsHint);
            tabsHeader.Controls.Add(tabsTitle);
            tabsHeader.Dock = DockStyle.Top;
            tabsHeader.Location = new Point(0, 0);
            tabsHeader.Name = "tabsHeader";
            tabsHeader.Padding = new Padding(12, 0, 12, 0);
            tabsHeader.Size = new Size(286, 46);
            tabsHeader.TabIndex = 2;
            // 
            // tabsHint
            // 
            tabsHint.Dock = DockStyle.Right;
            tabsHint.Font = new Font("Segoe UI", 8.5F);
            tabsHint.ForeColor = Color.FromArgb(120, 128, 138);
            tabsHint.Location = new Point(154, 0);
            tabsHint.Name = "tabsHint";
            tabsHint.Size = new Size(120, 46);
            tabsHint.TabIndex = 1;
            tabsHint.Text = "drag to reorder";
            tabsHint.TextAlign = ContentAlignment.MiddleRight;
            // 
            // tabsTitle
            // 
            tabsTitle.Dock = DockStyle.Left;
            tabsTitle.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            tabsTitle.Location = new Point(12, 0);
            tabsTitle.Name = "tabsTitle";
            tabsTitle.Size = new Size(100, 46);
            tabsTitle.TabIndex = 0;
            tabsTitle.Text = "Tabs";
            tabsTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // rightTable
            // 
            rightTable.ColumnCount = 1;
            rightTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightTable.Controls.Add(itemsCard, 0, 0);
            rightTable.Controls.Add(previewCard, 0, 1);
            rightTable.Dock = DockStyle.Fill;
            rightTable.Location = new Point(300, 0);
            rightTable.Margin = new Padding(0);
            rightTable.Name = "rightTable";
            rightTable.RowCount = 2;
            rightTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rightTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 270F));
            rightTable.Size = new Size(956, 726);
            rightTable.TabIndex = 1;
            // 
            // itemsCard
            // 
            itemsCard.BackColor = Color.White;
            itemsCard.BorderStyle = BorderStyle.FixedSingle;
            itemsCard.Controls.Add(itemsGrid);
            itemsCard.Controls.Add(emptyLabel);
            itemsCard.Controls.Add(tabHeader);
            itemsCard.Dock = DockStyle.Fill;
            itemsCard.Location = new Point(0, 0);
            itemsCard.Margin = new Padding(0, 0, 0, 12);
            itemsCard.Name = "itemsCard";
            itemsCard.Size = new Size(956, 444);
            itemsCard.TabIndex = 0;
            // 
            // itemsGrid
            // 
            itemsGrid.AllowDrop = true;
            itemsGrid.AllowUserToAddRows = false;
            itemsGrid.AllowUserToDeleteRows = false;
            itemsGrid.AllowUserToResizeRows = false;
            itemsGrid.BackgroundColor = Color.White;
            itemsGrid.BorderStyle = BorderStyle.None;
            itemsGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            itemsGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = Color.White;
            dataGridViewCellStyle1.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = Color.FromArgb(90, 98, 108);
            dataGridViewCellStyle1.SelectionBackColor = Color.White;
            dataGridViewCellStyle1.SelectionForeColor = Color.FromArgb(90, 98, 108);
            itemsGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            itemsGrid.ColumnHeadersHeight = 38;
            itemsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            itemsGrid.Columns.AddRange(new DataGridViewColumn[] { gripColumn, itemColumn, labelColumn, priceColumn, colorColumn, askPriceColumn, showColumn, removeColumn });
            itemsGrid.ContextMenuStrip = rowMenu;
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = Color.White;
            dataGridViewCellStyle5.Font = new Font("Segoe UI", 10F);
            dataGridViewCellStyle5.ForeColor = Color.FromArgb(22, 32, 43);
            dataGridViewCellStyle5.SelectionBackColor = Color.FromArgb(240, 247, 244);
            dataGridViewCellStyle5.SelectionForeColor = Color.FromArgb(22, 32, 43);
            dataGridViewCellStyle5.WrapMode = DataGridViewTriState.False;
            itemsGrid.DefaultCellStyle = dataGridViewCellStyle5;
            itemsGrid.Dock = DockStyle.Fill;
            itemsGrid.EnableHeadersVisualStyles = false;
            itemsGrid.GridColor = Color.FromArgb(232, 235, 238);
            itemsGrid.Location = new Point(0, 120);
            itemsGrid.MultiSelect = false;
            itemsGrid.Name = "itemsGrid";
            itemsGrid.RowHeadersVisible = false;
            itemsGrid.RowHeadersWidth = 51;
            itemsGrid.RowTemplate.Height = 44;
            itemsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            itemsGrid.Size = new Size(954, 322);
            itemsGrid.TabIndex = 0;
            itemsGrid.CellBeginEdit += itemsGrid_CellBeginEdit;
            itemsGrid.CellClick += itemsGrid_CellClick;
            itemsGrid.CellEndEdit += itemsGrid_CellEndEdit;
            itemsGrid.CellFormatting += itemsGrid_CellFormatting;
            itemsGrid.CellMouseDown += itemsGrid_CellMouseDown;
            itemsGrid.CellMouseEnter += itemsGrid_CellMouseEnter;
            itemsGrid.CellPainting += itemsGrid_CellPainting;
            itemsGrid.DragDrop += itemsGrid_DragDrop;
            itemsGrid.DragOver += itemsGrid_DragOver;
            itemsGrid.KeyDown += itemsGrid_KeyDown;
            itemsGrid.MouseLeave += itemsGrid_MouseLeave;
            itemsGrid.MouseMove += itemsGrid_MouseMove;
            itemsGrid.MouseUp += itemsGrid_MouseUp;
            // 
            // gripColumn
            // 
            gripColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter;
            gripColumn.DefaultCellStyle = dataGridViewCellStyle2;
            gripColumn.HeaderText = "";
            gripColumn.MinimumWidth = 6;
            gripColumn.Name = "gripColumn";
            gripColumn.ReadOnly = true;
            gripColumn.Resizable = DataGridViewTriState.False;
            gripColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            gripColumn.Width = 30;
            // 
            // itemColumn
            // 
            itemColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            itemColumn.DataPropertyName = "ItemName";
            itemColumn.FillWeight = 160F;
            itemColumn.HeaderText = "ITEM IT SELLS";
            itemColumn.MinimumWidth = 6;
            itemColumn.Name = "itemColumn";
            itemColumn.ReadOnly = true;
            itemColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // labelColumn
            // 
            labelColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            labelColumn.DataPropertyName = "Caption";
            labelColumn.FillWeight = 110F;
            labelColumn.HeaderText = "LABEL ON BUTTON";
            labelColumn.MaxInputLength = 40;
            labelColumn.MinimumWidth = 6;
            labelColumn.Name = "labelColumn";
            labelColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // priceColumn
            // 
            priceColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            priceColumn.DataPropertyName = "SalePrice";
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle3.Font = new Font("Consolas", 10.5F, FontStyle.Bold);
            priceColumn.DefaultCellStyle = dataGridViewCellStyle3;
            priceColumn.HeaderText = "PRICE";
            priceColumn.MinimumWidth = 6;
            priceColumn.Name = "priceColumn";
            priceColumn.ReadOnly = true;
            priceColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            priceColumn.Width = 140;
            // 
            // colorColumn
            // 
            colorColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colorColumn.HeaderText = "COLOR";
            colorColumn.MinimumWidth = 6;
            colorColumn.Name = "colorColumn";
            colorColumn.ReadOnly = true;
            colorColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            colorColumn.Width = 130;
            // 
            // askPriceColumn
            // 
            askPriceColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            askPriceColumn.HeaderText = "ASK PRICE";
            askPriceColumn.MinimumWidth = 6;
            askPriceColumn.Name = "askPriceColumn";
            askPriceColumn.ReadOnly = true;
            askPriceColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            askPriceColumn.Width = 90;
            // 
            // showColumn
            // 
            showColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            showColumn.HeaderText = "SHOW";
            showColumn.MinimumWidth = 6;
            showColumn.Name = "showColumn";
            showColumn.ReadOnly = true;
            showColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            showColumn.Width = 70;
            // 
            // removeColumn
            // 
            removeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle4.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            removeColumn.DefaultCellStyle = dataGridViewCellStyle4;
            removeColumn.HeaderText = "";
            removeColumn.MinimumWidth = 6;
            removeColumn.Name = "removeColumn";
            removeColumn.ReadOnly = true;
            removeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            removeColumn.Width = 44;
            // 
            // rowMenu
            // 
            rowMenu.ImageScalingSize = new Size(20, 20);
            rowMenu.Items.AddRange(new ToolStripItem[] { moveToTabMenuItem, removeMenuItem });
            rowMenu.Name = "rowMenu";
            rowMenu.Size = new Size(181, 52);
            rowMenu.Opening += rowMenu_Opening;
            // 
            // moveToTabMenuItem
            // 
            moveToTabMenuItem.Name = "moveToTabMenuItem";
            moveToTabMenuItem.Size = new Size(180, 24);
            moveToTabMenuItem.Text = "Move to tab";
            // 
            // removeMenuItem
            // 
            removeMenuItem.ForeColor = Color.Firebrick;
            removeMenuItem.Name = "removeMenuItem";
            removeMenuItem.Size = new Size(180, 24);
            removeMenuItem.Text = "Remove button";
            removeMenuItem.Click += removeMenuItem_Click;
            // 
            // emptyLabel
            // 
            emptyLabel.Dock = DockStyle.Top;
            emptyLabel.ForeColor = Color.FromArgb(120, 128, 138);
            emptyLabel.Location = new Point(0, 76);
            emptyLabel.Name = "emptyLabel";
            emptyLabel.Padding = new Padding(16, 0, 0, 0);
            emptyLabel.Size = new Size(954, 44);
            emptyLabel.TabIndex = 2;
            emptyLabel.Text = "No buttons in this tab yet. Click \"+ Add items\" to pick items from the item list.";
            emptyLabel.TextAlign = ContentAlignment.MiddleLeft;
            emptyLabel.Visible = false;
            // 
            // tabHeader
            // 
            tabHeader.Controls.Add(editTabButton);
            tabHeader.Controls.Add(addItemsButton);
            tabHeader.Controls.Add(tabInfoLabel);
            tabHeader.Controls.Add(tabNameLabel);
            tabHeader.Controls.Add(tabSwatch);
            tabHeader.Dock = DockStyle.Top;
            tabHeader.Location = new Point(0, 0);
            tabHeader.Name = "tabHeader";
            tabHeader.Size = new Size(954, 76);
            tabHeader.TabIndex = 1;
            // 
            // editTabButton
            // 
            editTabButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            editTabButton.FlatAppearance.BorderColor = Color.Silver;
            editTabButton.FlatStyle = FlatStyle.Flat;
            editTabButton.Location = new Point(674, 18);
            editTabButton.Name = "editTabButton";
            editTabButton.Size = new Size(110, 40);
            editTabButton.TabIndex = 1;
            editTabButton.Text = "Edit tab";
            editTabButton.Click += editTabButton_Click;
            // 
            // addItemsButton
            // 
            addItemsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            addItemsButton.BackColor = Color.FromArgb(28, 110, 92);
            addItemsButton.FlatAppearance.BorderSize = 0;
            addItemsButton.FlatStyle = FlatStyle.Flat;
            addItemsButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            addItemsButton.ForeColor = Color.White;
            addItemsButton.Location = new Point(794, 18);
            addItemsButton.Name = "addItemsButton";
            addItemsButton.Size = new Size(144, 40);
            addItemsButton.TabIndex = 0;
            addItemsButton.Text = "+ Add items";
            addItemsButton.UseVisualStyleBackColor = false;
            addItemsButton.Click += addItemsButton_Click;
            // 
            // tabInfoLabel
            // 
            tabInfoLabel.AutoSize = true;
            tabInfoLabel.Font = new Font("Segoe UI", 8.5F);
            tabInfoLabel.ForeColor = Color.FromArgb(90, 98, 108);
            tabInfoLabel.Location = new Point(66, 42);
            tabInfoLabel.Name = "tabInfoLabel";
            tabInfoLabel.Size = new Size(0, 20);
            tabInfoLabel.TabIndex = 3;
            // 
            // tabNameLabel
            // 
            tabNameLabel.AutoSize = true;
            tabNameLabel.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
            tabNameLabel.Location = new Point(64, 12);
            tabNameLabel.Name = "tabNameLabel";
            tabNameLabel.Size = new Size(0, 30);
            tabNameLabel.TabIndex = 2;
            // 
            // tabSwatch
            // 
            tabSwatch.Location = new Point(16, 18);
            tabSwatch.Name = "tabSwatch";
            tabSwatch.Size = new Size(38, 38);
            tabSwatch.TabIndex = 4;
            // 
            // previewCard
            // 
            previewCard.BackColor = Color.White;
            previewCard.BorderStyle = BorderStyle.FixedSingle;
            previewCard.Controls.Add(previewItems);
            previewCard.Controls.Add(previewTabs);
            previewCard.Controls.Add(previewHeader);
            previewCard.Dock = DockStyle.Fill;
            previewCard.Location = new Point(0, 456);
            previewCard.Margin = new Padding(0);
            previewCard.Name = "previewCard";
            previewCard.Padding = new Padding(14, 0, 14, 8);
            previewCard.Size = new Size(956, 270);
            previewCard.TabIndex = 1;
            // 
            // previewItems
            // 
            previewItems.AutoScroll = true;
            previewItems.Dock = DockStyle.Fill;
            previewItems.Location = new Point(14, 44);
            previewItems.Name = "previewItems";
            previewItems.Padding = new Padding(0, 10, 0, 0);
            previewItems.Size = new Size(926, 216);
            previewItems.TabIndex = 2;
            previewItems.Resize += previewItems_Resize;
            // 
            // previewTabs
            // 
            previewTabs.AutoSize = true;
            previewTabs.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            previewTabs.Dock = DockStyle.Top;
            previewTabs.Location = new Point(14, 44);
            previewTabs.Name = "previewTabs";
            previewTabs.Size = new Size(926, 0);
            previewTabs.TabIndex = 1;
            // 
            // previewHeader
            // 
            previewHeader.Controls.Add(previewNote);
            previewHeader.Controls.Add(columnsLabel);
            previewHeader.Controls.Add(columnsUpDown);
            previewHeader.Controls.Add(previewTitle);
            previewHeader.Dock = DockStyle.Top;
            previewHeader.Location = new Point(14, 0);
            previewHeader.Name = "previewHeader";
            previewHeader.Padding = new Padding(0, 9, 0, 8);
            previewHeader.Size = new Size(926, 44);
            previewHeader.TabIndex = 0;
            // 
            // previewNote
            // 
            previewNote.Dock = DockStyle.Fill;
            previewNote.Font = new Font("Segoe UI", 8.5F);
            previewNote.ForeColor = Color.FromArgb(120, 128, 138);
            previewNote.Location = new Point(262, 9);
            previewNote.Name = "previewNote";
            previewNote.Padding = new Padding(0, 0, 16, 0);
            previewNote.Size = new Size(488, 27);
            previewNote.TabIndex = 3;
            previewNote.TextAlign = ContentAlignment.MiddleRight;
            // 
            // columnsLabel
            // 
            columnsLabel.Dock = DockStyle.Right;
            columnsLabel.Font = new Font("Segoe UI", 8.5F);
            columnsLabel.Location = new Point(750, 9);
            columnsLabel.Name = "columnsLabel";
            columnsLabel.Size = new Size(120, 27);
            columnsLabel.TabIndex = 1;
            columnsLabel.Text = "Buttons per row";
            columnsLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // columnsUpDown
            // 
            columnsUpDown.Dock = DockStyle.Right;
            columnsUpDown.Location = new Point(870, 9);
            columnsUpDown.Name = "columnsUpDown";
            columnsUpDown.Size = new Size(56, 27);
            columnsUpDown.TabIndex = 2;
            columnsUpDown.Value = new decimal(new int[] { 3, 0, 0, 0 });
            columnsUpDown.ValueChanged += columnsUpDown_ValueChanged;
            // 
            // previewTitle
            // 
            previewTitle.AutoSize = true;
            previewTitle.Dock = DockStyle.Left;
            previewTitle.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            previewTitle.Location = new Point(0, 9);
            previewTitle.Name = "previewTitle";
            previewTitle.Padding = new Padding(0, 3, 0, 0);
            previewTitle.Size = new Size(262, 28);
            previewTitle.TabIndex = 0;
            previewTitle.Text = "Preview: what the cashier sees";
            // 
            // colorMenu
            // 
            colorMenu.ImageScalingSize = new Size(20, 20);
            colorMenu.Name = "colorMenu";
            colorMenu.Size = new Size(61, 4);
            // 
            // QuickButtonsForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(238, 241, 244);
            ClientSize = new Size(1280, 820);
            Controls.Add(bodyTable);
            Controls.Add(headerPanel);
            KeyPreview = true;
            MinimumSize = new Size(1100, 640);
            Name = "QuickButtonsForm";
            Padding = new Padding(12);
            StartPosition = FormStartPosition.CenterParent;
            Text = "POS buttons";
            WindowState = FormWindowState.Maximized;
            FormClosing += QuickButtonsForm_FormClosing;
            FormClosed += QuickButtonsForm_FormClosed;
            Load += QuickButtonsForm_Load;
            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            bodyTable.ResumeLayout(false);
            tabsCard.ResumeLayout(false);
            tabsFooter.ResumeLayout(false);
            tabsHeader.ResumeLayout(false);
            rightTable.ResumeLayout(false);
            itemsCard.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)itemsGrid).EndInit();
            rowMenu.ResumeLayout(false);
            tabHeader.ResumeLayout(false);
            tabHeader.PerformLayout();
            previewCard.ResumeLayout(false);
            previewCard.PerformLayout();
            previewHeader.ResumeLayout(false);
            previewHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)columnsUpDown).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel headerPanel;
        private Button newTabButton;
        private Label subtitleLabel;
        private Label titleLabel;
        private TableLayoutPanel bodyTable;
        private Panel tabsCard;
        private ListBox tabsList;
        private Panel tabsFooter;
        private Button deleteTabButton;
        private Button downTabButton;
        private Button upTabButton;
        private Panel tabsHeader;
        private Label tabsHint;
        private Label tabsTitle;
        private TableLayoutPanel rightTable;
        private Panel itemsCard;
        private DataGridView itemsGrid;
        private DataGridViewTextBoxColumn gripColumn;
        private DataGridViewTextBoxColumn itemColumn;
        private DataGridViewTextBoxColumn labelColumn;
        private DataGridViewTextBoxColumn priceColumn;
        private DataGridViewTextBoxColumn colorColumn;
        private DataGridViewTextBoxColumn askPriceColumn;
        private DataGridViewTextBoxColumn showColumn;
        private DataGridViewTextBoxColumn removeColumn;
        private ContextMenuStrip rowMenu;
        private ToolStripMenuItem moveToTabMenuItem;
        private ToolStripMenuItem removeMenuItem;
        private Label emptyLabel;
        private Panel tabHeader;
        private Button editTabButton;
        private Button addItemsButton;
        private Label tabInfoLabel;
        private Label tabNameLabel;
        private Panel tabSwatch;
        private Panel previewCard;
        private FlowLayoutPanel previewItems;
        private FlowLayoutPanel previewTabs;
        private Panel previewHeader;
        private Label previewNote;
        private NumericUpDown columnsUpDown;
        private Label columnsLabel;
        private Label previewTitle;
        private ContextMenuStrip colorMenu;
    }
}
