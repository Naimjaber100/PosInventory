namespace Inventory
{
    partial class PurchaseForm
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
            toolbarPanel = new Panel();
            newPurchaseButton = new Button();
            openPurchaseButton = new Button();
            searchLabel = new Label();
            searchBox = new TextBox();
            searchTimer = new System.Windows.Forms.Timer(components);
            mainSplit = new SplitContainer();
            invoicesGrid = new DataGridView();
            invoicesTitle = new Label();
            pagerPanel = new Panel();
            prevButton = new Button();
            pageLabel = new Label();
            nextButton = new Button();
            pageSizeLabel = new Label();
            pageSizeCombo = new ComboBox();
            linesGrid = new DataGridView();
            linesTitle = new Label();
            toolbarPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mainSplit).BeginInit();
            mainSplit.Panel1.SuspendLayout();
            mainSplit.Panel2.SuspendLayout();
            mainSplit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)invoicesGrid).BeginInit();
            pagerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)linesGrid).BeginInit();
            SuspendLayout();
            //
            // toolbarPanel
            //
            toolbarPanel.Controls.Add(newPurchaseButton);
            toolbarPanel.Controls.Add(openPurchaseButton);
            toolbarPanel.Controls.Add(searchLabel);
            toolbarPanel.Controls.Add(searchBox);
            toolbarPanel.Dock = DockStyle.Top;
            toolbarPanel.Location = new Point(0, 0);
            toolbarPanel.Name = "toolbarPanel";
            toolbarPanel.Size = new Size(1300, 56);
            toolbarPanel.TabIndex = 0;
            //
            // searchLabel
            //
            searchLabel.AutoSize = true;
            searchLabel.Location = new Point(290, 18);
            searchLabel.Name = "searchLabel";
            searchLabel.TabIndex = 0;
            searchLabel.Text = "Search";
            //
            // searchBox
            //
            searchBox.Font = new Font("Segoe UI", 11F);
            searchBox.Location = new Point(352, 12);
            searchBox.Name = "searchBox";
            searchBox.PlaceholderText = "Invoice number, supplier invoice or supplier name";
            searchBox.Size = new Size(420, 32);
            searchBox.TabIndex = 1;
            searchBox.TextChanged += searchBox_TextChanged;
            searchBox.KeyDown += searchBox_KeyDown;
            //
            // searchTimer
            //
            searchTimer.Interval = 400;
            searchTimer.Tick += searchTimer_Tick;
            //
            // newPurchaseButton
            //
            newPurchaseButton.BackColor = Color.FromArgb(34, 139, 117);
            newPurchaseButton.FlatAppearance.BorderSize = 0;
            newPurchaseButton.FlatStyle = FlatStyle.Flat;
            newPurchaseButton.ForeColor = Color.White;
            newPurchaseButton.Location = new Point(12, 10);
            newPurchaseButton.Name = "newPurchaseButton";
            newPurchaseButton.Size = new Size(150, 36);
            newPurchaseButton.TabIndex = 2;
            newPurchaseButton.Text = "+ New purchase";
            newPurchaseButton.UseVisualStyleBackColor = false;
            newPurchaseButton.Click += newPurchaseButton_Click;
            //
            // openPurchaseButton
            //
            openPurchaseButton.Location = new Point(172, 10);
            openPurchaseButton.Name = "openPurchaseButton";
            openPurchaseButton.Size = new Size(90, 36);
            openPurchaseButton.TabIndex = 3;
            openPurchaseButton.Text = "Open";
            openPurchaseButton.Click += openPurchaseButton_Click;
            //
            // mainSplit
            //
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.Location = new Point(0, 56);
            mainSplit.Name = "mainSplit";
            mainSplit.Orientation = Orientation.Horizontal;
            //
            // mainSplit.Panel1
            //
            mainSplit.Panel1.Controls.Add(invoicesGrid);
            mainSplit.Panel1.Controls.Add(invoicesTitle);
            mainSplit.Panel1.Controls.Add(pagerPanel);
            mainSplit.Panel1.Padding = new Padding(10, 0, 10, 0);
            //
            // mainSplit.Panel2
            //
            mainSplit.Panel2.Controls.Add(linesGrid);
            mainSplit.Panel2.Controls.Add(linesTitle);
            mainSplit.Panel2.Padding = new Padding(10, 0, 10, 10);
            mainSplit.Size = new Size(1300, 744);
            mainSplit.SplitterDistance = 400;
            mainSplit.SplitterWidth = 8;
            mainSplit.TabIndex = 1;
            //
            // invoicesGrid
            //
            invoicesGrid.AllowUserToAddRows = false;
            invoicesGrid.AllowUserToDeleteRows = false;
            invoicesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            invoicesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            invoicesGrid.Dock = DockStyle.Fill;
            invoicesGrid.Location = new Point(10, 32);
            invoicesGrid.MultiSelect = false;
            invoicesGrid.Name = "invoicesGrid";
            invoicesGrid.ReadOnly = true;
            invoicesGrid.RowHeadersVisible = false;
            invoicesGrid.RowHeadersWidth = 51;
            invoicesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            invoicesGrid.Size = new Size(1280, 320);
            invoicesGrid.TabIndex = 1;
            invoicesGrid.DataBindingComplete += grid_DataBindingComplete;
            invoicesGrid.SelectionChanged += invoicesGrid_SelectionChanged;
            invoicesGrid.CellDoubleClick += invoicesGrid_CellDoubleClick;
            //
            // invoicesTitle
            //
            invoicesTitle.Dock = DockStyle.Top;
            invoicesTitle.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            invoicesTitle.Location = new Point(10, 0);
            invoicesTitle.Name = "invoicesTitle";
            invoicesTitle.Size = new Size(1280, 32);
            invoicesTitle.TabIndex = 0;
            invoicesTitle.Text = "Purchase invoices";
            invoicesTitle.TextAlign = ContentAlignment.MiddleLeft;
            //
            // pagerPanel
            //
            pagerPanel.Controls.Add(prevButton);
            pagerPanel.Controls.Add(pageLabel);
            pagerPanel.Controls.Add(nextButton);
            pagerPanel.Controls.Add(pageSizeLabel);
            pagerPanel.Controls.Add(pageSizeCombo);
            pagerPanel.Dock = DockStyle.Bottom;
            pagerPanel.Location = new Point(10, 352);
            pagerPanel.Name = "pagerPanel";
            pagerPanel.Size = new Size(1280, 48);
            pagerPanel.TabIndex = 2;
            //
            // prevButton
            //
            prevButton.Location = new Point(0, 6);
            prevButton.Name = "prevButton";
            prevButton.Size = new Size(100, 36);
            prevButton.TabIndex = 0;
            prevButton.Text = "< Prev";
            prevButton.Click += prevButton_Click;
            //
            // pageLabel
            //
            pageLabel.Location = new Point(106, 6);
            pageLabel.Name = "pageLabel";
            pageLabel.Size = new Size(160, 36);
            pageLabel.TabIndex = 1;
            pageLabel.Text = "Page 1 of 1";
            pageLabel.TextAlign = ContentAlignment.MiddleCenter;
            //
            // nextButton
            //
            nextButton.Location = new Point(272, 6);
            nextButton.Name = "nextButton";
            nextButton.Size = new Size(100, 36);
            nextButton.TabIndex = 2;
            nextButton.Text = "Next >";
            nextButton.Click += nextButton_Click;
            //
            // pageSizeLabel
            //
            pageSizeLabel.AutoSize = true;
            pageSizeLabel.Location = new Point(400, 14);
            pageSizeLabel.Name = "pageSizeLabel";
            pageSizeLabel.TabIndex = 3;
            pageSizeLabel.Text = "Rows per page";
            //
            // pageSizeCombo
            //
            pageSizeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            pageSizeCombo.Items.AddRange(new object[] { 25, 50, 100, 200 });
            pageSizeCombo.Location = new Point(520, 10);
            pageSizeCombo.Name = "pageSizeCombo";
            pageSizeCombo.Size = new Size(80, 28);
            pageSizeCombo.TabIndex = 4;
            pageSizeCombo.SelectedIndexChanged += pageSizeCombo_SelectedIndexChanged;
            //
            // linesGrid
            //
            linesGrid.AllowUserToAddRows = false;
            linesGrid.AllowUserToDeleteRows = false;
            linesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            linesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            linesGrid.Dock = DockStyle.Fill;
            linesGrid.Location = new Point(10, 32);
            linesGrid.Name = "linesGrid";
            linesGrid.ReadOnly = true;
            linesGrid.RowHeadersVisible = false;
            linesGrid.RowHeadersWidth = 51;
            linesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            linesGrid.Size = new Size(1280, 294);
            linesGrid.TabIndex = 1;
            linesGrid.DataBindingComplete += grid_DataBindingComplete;
            //
            // linesTitle
            //
            linesTitle.Dock = DockStyle.Top;
            linesTitle.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            linesTitle.Location = new Point(10, 0);
            linesTitle.Name = "linesTitle";
            linesTitle.Size = new Size(1280, 32);
            linesTitle.TabIndex = 0;
            linesTitle.Text = "Items";
            linesTitle.TextAlign = ContentAlignment.MiddleLeft;
            //
            // PurchaseForm
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1300, 800);
            Controls.Add(mainSplit);
            Controls.Add(toolbarPanel);
            Name = "PurchaseForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Purchases";
            Load += PurchaseForm_Load;
            toolbarPanel.ResumeLayout(false);
            toolbarPanel.PerformLayout();
            mainSplit.Panel1.ResumeLayout(false);
            mainSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)mainSplit).EndInit();
            mainSplit.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)invoicesGrid).EndInit();
            pagerPanel.ResumeLayout(false);
            pagerPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)linesGrid).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel toolbarPanel;
        private Button newPurchaseButton;
        private Button openPurchaseButton;
        private Label searchLabel;
        private TextBox searchBox;
        private System.Windows.Forms.Timer searchTimer;
        private SplitContainer mainSplit;
        private DataGridView invoicesGrid;
        private Label invoicesTitle;
        private Panel pagerPanel;
        private Button prevButton;
        private Label pageLabel;
        private Button nextButton;
        private Label pageSizeLabel;
        private ComboBox pageSizeCombo;
        private DataGridView linesGrid;
        private Label linesTitle;
    }
}
