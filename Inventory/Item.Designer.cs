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
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            itemsGrid = new DataGridView();
            mainSplit = new SplitContainer();
            categoryButtonsPanel = new FlowLayoutPanel();
            addCategoryButton = new Button();
            addSubCategoryButton = new Button();
            editCategoryButton = new Button();
            deleteCategoryButton = new Button();
            categoryTree = new TreeView();
            categoryMenu = new ContextMenuStrip(components);
            addCategoryMenuItem = new ToolStripMenuItem();
            addSubCategoryMenuItem = new ToolStripMenuItem();
            editCategoryMenuItem = new ToolStripMenuItem();
            deleteCategoryMenuItem = new ToolStripMenuItem();
            toolbarPanel = new Panel();
            addButton = new Button();
            editButton = new Button();
            deleteButton = new Button();
            importButton = new Button();
            templateButton = new Button();
            searchLabel = new Label();
            searchBox = new TextBox();
            searchTimer = new System.Windows.Forms.Timer(components);
            pagerPanel = new Panel();
            prevButton = new Button();
            pageLabel = new Label();
            nextButton = new Button();
            pageSizeLabel = new Label();
            pageSizeCombo = new ComboBox();
            ((System.ComponentModel.ISupportInitialize)itemsGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)mainSplit).BeginInit();
            mainSplit.Panel1.SuspendLayout();
            mainSplit.Panel2.SuspendLayout();
            mainSplit.SuspendLayout();
            categoryButtonsPanel.SuspendLayout();
            categoryMenu.SuspendLayout();
            toolbarPanel.SuspendLayout();
            pagerPanel.SuspendLayout();
            SuspendLayout();
            // 
            // itemsGrid
            // 
            itemsGrid.AllowUserToOrderColumns = true;
            itemsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            itemsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            itemsGrid.Dock = DockStyle.Fill;
            itemsGrid.Name = "itemsGrid";
            itemsGrid.RowHeadersVisible = false;
            itemsGrid.RowHeadersWidth = 51;
            itemsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            itemsGrid.Size = new Size(1065, 634);
            itemsGrid.TabIndex = 0;
            itemsGrid.CellContentClick += itemsGrid_CellContentClick;
            itemsGrid.CellDoubleClick += itemsGrid_CellDoubleClick;
            //
            // mainSplit
            //
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.FixedPanel = FixedPanel.Panel1;
            mainSplit.Location = new Point(0, 56);
            mainSplit.Name = "mainSplit";
            //
            // mainSplit.Panel1
            //
            mainSplit.Panel1.Controls.Add(categoryTree);
            mainSplit.Panel1.Controls.Add(categoryButtonsPanel);
            mainSplit.Panel1.Padding = new Padding(10, 0, 0, 10);
            mainSplit.Panel1MinSize = 180;
            //
            // mainSplit.Panel2
            //
            mainSplit.Panel2.Controls.Add(itemsGrid);
            mainSplit.Panel2.Padding = new Padding(0, 0, 10, 10);
            mainSplit.Size = new Size(1381, 644);
            mainSplit.SplitterDistance = 300;
            mainSplit.SplitterWidth = 6;
            mainSplit.TabIndex = 0;
            //
            // categoryButtonsPanel
            //
            categoryButtonsPanel.Controls.Add(addCategoryButton);
            categoryButtonsPanel.Controls.Add(addSubCategoryButton);
            categoryButtonsPanel.Controls.Add(editCategoryButton);
            categoryButtonsPanel.Controls.Add(deleteCategoryButton);
            categoryButtonsPanel.Dock = DockStyle.Top;
            categoryButtonsPanel.Location = new Point(10, 0);
            categoryButtonsPanel.Name = "categoryButtonsPanel";
            categoryButtonsPanel.Padding = new Padding(0, 0, 0, 4);
            categoryButtonsPanel.Size = new Size(290, 40);
            categoryButtonsPanel.TabIndex = 0;
            categoryButtonsPanel.WrapContents = false;
            //
            // addCategoryButton
            //
            addCategoryButton.Margin = new Padding(0, 0, 4, 0);
            addCategoryButton.Name = "addCategoryButton";
            addCategoryButton.Size = new Size(76, 32);
            addCategoryButton.TabIndex = 0;
            addCategoryButton.Text = "+ Group";
            addCategoryButton.Click += addCategoryButton_Click;
            //
            // addSubCategoryButton
            //
            addSubCategoryButton.Margin = new Padding(0, 0, 4, 0);
            addSubCategoryButton.Name = "addSubCategoryButton";
            addSubCategoryButton.Size = new Size(64, 32);
            addSubCategoryButton.TabIndex = 1;
            addSubCategoryButton.Text = "+ Sub";
            addSubCategoryButton.Click += addSubCategoryButton_Click;
            //
            // editCategoryButton
            //
            editCategoryButton.Margin = new Padding(0, 0, 4, 0);
            editCategoryButton.Name = "editCategoryButton";
            editCategoryButton.Size = new Size(60, 32);
            editCategoryButton.TabIndex = 2;
            editCategoryButton.Text = "Edit";
            editCategoryButton.Click += editCategoryButton_Click;
            //
            // deleteCategoryButton
            //
            deleteCategoryButton.ForeColor = Color.Firebrick;
            deleteCategoryButton.Margin = new Padding(0);
            deleteCategoryButton.Name = "deleteCategoryButton";
            deleteCategoryButton.Size = new Size(70, 32);
            deleteCategoryButton.TabIndex = 3;
            deleteCategoryButton.Text = "Delete";
            deleteCategoryButton.Click += deleteCategoryButton_Click;
            //
            // categoryTree
            //
            categoryTree.ContextMenuStrip = categoryMenu;
            categoryTree.Dock = DockStyle.Fill;
            categoryTree.Font = new Font("Segoe UI", 10F);
            categoryTree.FullRowSelect = true;
            categoryTree.HideSelection = false;
            categoryTree.ItemHeight = 26;
            categoryTree.Location = new Point(10, 40);
            categoryTree.Name = "categoryTree";
            categoryTree.ShowLines = false;
            categoryTree.Size = new Size(290, 594);
            categoryTree.TabIndex = 1;
            categoryTree.AfterSelect += categoryTree_AfterSelect;
            categoryTree.NodeMouseClick += categoryTree_NodeMouseClick;
            //
            // categoryMenu
            //
            categoryMenu.ImageScalingSize = new Size(20, 20);
            categoryMenu.Items.AddRange(new ToolStripItem[] { addCategoryMenuItem, addSubCategoryMenuItem, editCategoryMenuItem, deleteCategoryMenuItem });
            categoryMenu.Name = "categoryMenu";
            categoryMenu.Opening += categoryMenu_Opening;
            //
            // addCategoryMenuItem
            //
            addCategoryMenuItem.Name = "addCategoryMenuItem";
            addCategoryMenuItem.Text = "Add group";
            addCategoryMenuItem.Click += addCategoryButton_Click;
            //
            // addSubCategoryMenuItem
            //
            addSubCategoryMenuItem.Name = "addSubCategoryMenuItem";
            addSubCategoryMenuItem.Text = "Add sub-group";
            addSubCategoryMenuItem.Click += addSubCategoryButton_Click;
            //
            // editCategoryMenuItem
            //
            editCategoryMenuItem.Name = "editCategoryMenuItem";
            editCategoryMenuItem.Text = "Edit / move";
            editCategoryMenuItem.Click += editCategoryButton_Click;
            //
            // deleteCategoryMenuItem
            //
            deleteCategoryMenuItem.Name = "deleteCategoryMenuItem";
            deleteCategoryMenuItem.Text = "Delete";
            deleteCategoryMenuItem.Click += deleteCategoryButton_Click;
            // 
            // toolbarPanel
            // 
            toolbarPanel.Controls.Add(addButton);
            toolbarPanel.Controls.Add(editButton);
            toolbarPanel.Controls.Add(deleteButton);
            toolbarPanel.Controls.Add(importButton);
            toolbarPanel.Controls.Add(templateButton);
            toolbarPanel.Controls.Add(searchLabel);
            toolbarPanel.Controls.Add(searchBox);
            toolbarPanel.Dock = DockStyle.Top;
            toolbarPanel.Location = new Point(0, 0);
            toolbarPanel.Name = "toolbarPanel";
            toolbarPanel.Padding = new Padding(10, 8, 10, 8);
            toolbarPanel.Size = new Size(1381, 56);
            toolbarPanel.TabIndex = 1;
            // 
            // addButton
            // 
            addButton.BackColor = Color.FromArgb(34, 139, 117);
            addButton.FlatAppearance.BorderSize = 0;
            addButton.FlatStyle = FlatStyle.Flat;
            addButton.ForeColor = Color.White;
            addButton.Location = new Point(10, 10);
            addButton.Name = "addButton";
            addButton.Size = new Size(120, 36);
            addButton.TabIndex = 0;
            addButton.Text = "+ Add item";
            addButton.UseVisualStyleBackColor = false;
            addButton.Click += addButton_Click;
            // 
            // editButton
            // 
            editButton.Location = new Point(140, 10);
            editButton.Name = "editButton";
            editButton.Size = new Size(100, 36);
            editButton.TabIndex = 1;
            editButton.Text = "Edit";
            editButton.Click += editButton_Click;
            // 
            // deleteButton
            // 
            deleteButton.ForeColor = Color.Firebrick;
            deleteButton.Location = new Point(250, 10);
            deleteButton.Name = "deleteButton";
            deleteButton.Size = new Size(100, 36);
            deleteButton.TabIndex = 2;
            deleteButton.Text = "Delete";
            deleteButton.Click += deleteButton_Click;
            // 
            // importButton
            // 
            importButton.Location = new Point(380, 10);
            importButton.Name = "importButton";
            importButton.Size = new Size(160, 36);
            importButton.TabIndex = 3;
            importButton.Text = "Import from Excel";
            importButton.Click += importButton_Click;
            // 
            // templateButton
            // 
            templateButton.Location = new Point(550, 10);
            templateButton.Name = "templateButton";
            templateButton.Size = new Size(140, 36);
            templateButton.TabIndex = 4;
            templateButton.Text = "Excel template";
            templateButton.Click += templateButton_Click;
            //
            // searchLabel
            //
            searchLabel.AutoSize = true;
            searchLabel.Location = new Point(720, 18);
            searchLabel.Name = "searchLabel";
            searchLabel.TabIndex = 5;
            searchLabel.Text = "Search";
            //
            // searchBox
            //
            searchBox.Font = new Font("Segoe UI", 11F);
            searchBox.Location = new Point(782, 12);
            searchBox.Name = "searchBox";
            searchBox.PlaceholderText = "Name or barcode  (Enter to search, Esc to clear)";
            searchBox.Size = new Size(420, 32);
            searchBox.TabIndex = 6;
            searchBox.TextChanged += searchBox_TextChanged;
            searchBox.KeyDown += searchBox_KeyDown;
            //
            // searchTimer
            //
            searchTimer.Interval = 400;
            searchTimer.Tick += searchTimer_Tick;
            // 
            // pagerPanel
            // 
            pagerPanel.Controls.Add(prevButton);
            pagerPanel.Controls.Add(pageLabel);
            pagerPanel.Controls.Add(nextButton);
            pagerPanel.Controls.Add(pageSizeLabel);
            pagerPanel.Controls.Add(pageSizeCombo);
            pagerPanel.Dock = DockStyle.Bottom;
            pagerPanel.Location = new Point(0, 700);
            pagerPanel.Name = "pagerPanel";
            pagerPanel.Size = new Size(1381, 52);
            pagerPanel.TabIndex = 2;
            // 
            // prevButton
            // 
            prevButton.Location = new Point(12, 8);
            prevButton.Name = "prevButton";
            prevButton.Size = new Size(100, 36);
            prevButton.TabIndex = 0;
            prevButton.Text = "< Prev";
            prevButton.Click += prevButton_Click;
            // 
            // pageLabel
            // 
            pageLabel.Location = new Point(118, 8);
            pageLabel.Name = "pageLabel";
            pageLabel.Size = new Size(260, 36);
            pageLabel.TabIndex = 1;
            pageLabel.Text = "Page 1 of 1";
            pageLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // nextButton
            // 
            nextButton.Location = new Point(384, 8);
            nextButton.Name = "nextButton";
            nextButton.Size = new Size(100, 36);
            nextButton.TabIndex = 2;
            nextButton.Text = "Next >";
            nextButton.Click += nextButton_Click;
            // 
            // pageSizeLabel
            // 
            pageSizeLabel.AutoSize = true;
            pageSizeLabel.Location = new Point(520, 16);
            pageSizeLabel.Name = "pageSizeLabel";
            pageSizeLabel.TabIndex = 3;
            pageSizeLabel.Text = "Rows per page";
            // 
            // pageSizeCombo
            // 
            pageSizeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            pageSizeCombo.Items.AddRange(new object[] { 25, 50, 100, 200 });
            pageSizeCombo.Location = new Point(640, 12);
            pageSizeCombo.Name = "pageSizeCombo";
            pageSizeCombo.Size = new Size(80, 28);
            pageSizeCombo.TabIndex = 4;
            pageSizeCombo.SelectedIndexChanged += pageSizeCombo_SelectedIndexChanged;
            // 
            // Item
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1381, 752);
            Controls.Add(mainSplit);
            Controls.Add(toolbarPanel);
            Controls.Add(pagerPanel);
            Name = "Item";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Items";
            Load += Item_Load;
            ((System.ComponentModel.ISupportInitialize)itemsGrid).EndInit();
            mainSplit.Panel1.ResumeLayout(false);
            mainSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)mainSplit).EndInit();
            mainSplit.ResumeLayout(false);
            categoryButtonsPanel.ResumeLayout(false);
            categoryMenu.ResumeLayout(false);
            toolbarPanel.ResumeLayout(false);
            toolbarPanel.PerformLayout();
            pagerPanel.ResumeLayout(false);
            pagerPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView itemsGrid;
        private SplitContainer mainSplit;
        private FlowLayoutPanel categoryButtonsPanel;
        private Button addCategoryButton;
        private Button addSubCategoryButton;
        private Button editCategoryButton;
        private Button deleteCategoryButton;
        private TreeView categoryTree;
        private ContextMenuStrip categoryMenu;
        private ToolStripMenuItem addCategoryMenuItem;
        private ToolStripMenuItem addSubCategoryMenuItem;
        private ToolStripMenuItem editCategoryMenuItem;
        private ToolStripMenuItem deleteCategoryMenuItem;
        private Panel toolbarPanel;
        private Button addButton;
        private Button editButton;
        private Button deleteButton;
        private Button importButton;
        private Button templateButton;
        private Label searchLabel;
        private TextBox searchBox;
        private System.Windows.Forms.Timer searchTimer;
        private Panel pagerPanel;
        private Button prevButton;
        private Label pageLabel;
        private Button nextButton;
        private Label pageSizeLabel;
        private ComboBox pageSizeCombo;
    }
}
