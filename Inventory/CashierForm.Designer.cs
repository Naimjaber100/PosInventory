namespace Inventory
{
    partial class CashierForm
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
        /// The keypad, function and action buttons are created in code (CashierForm.BuildButtons).
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            topBar = new Panel();
            infoLabel = new Label();
            clockLabel = new Label();
            storeLabel = new Label();
            bodyTable = new TableLayoutPanel();
            leftPanel = new Panel();
            linesGrid = new DataGridView();
            scanPanel = new Panel();
            scanBox = new TextBox();
            scanLabel = new Label();
            bottomTable = new TableLayoutPanel();
            keypadTable = new TableLayoutPanel();
            functionTable = new TableLayoutPanel();
            totalsTable = new TableLayoutPanel();
            subTotalCaption = new Label();
            subTotalValue = new Label();
            discountCaption = new Label();
            discountPanel = new Panel();
            discountBox = new TextBox();
            percentCheck = new CheckBox();
            totalCaption = new Label();
            totalValue = new Label();
            totalUsdValue = new Label();
            actionsTable = new TableLayoutPanel();
            rightPanel = new Panel();
            quickPanel = new FlowLayoutPanel();
            quickMenu = new ContextMenuStrip(components);
            reloadQuickMenuItem = new ToolStripMenuItem();
            quickHint = new Label();
            groupPanel = new FlowLayoutPanel();
            lastItemPanel = new Panel();
            statusLabel = new Label();
            lastItemPrice = new Label();
            lastItemName = new Label();
            clockTimer = new System.Windows.Forms.Timer(components);
            topBar.SuspendLayout();
            bodyTable.SuspendLayout();
            leftPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)linesGrid).BeginInit();
            scanPanel.SuspendLayout();
            bottomTable.SuspendLayout();
            totalsTable.SuspendLayout();
            discountPanel.SuspendLayout();
            rightPanel.SuspendLayout();
            quickPanel.SuspendLayout();
            quickMenu.SuspendLayout();
            lastItemPanel.SuspendLayout();
            SuspendLayout();
            // 
            // topBar
            // 
            topBar.BackColor = Color.FromArgb(22, 32, 43);
            topBar.Controls.Add(infoLabel);
            topBar.Controls.Add(clockLabel);
            topBar.Controls.Add(storeLabel);
            topBar.Dock = DockStyle.Top;
            topBar.Location = new Point(0, 0);
            topBar.Name = "topBar";
            topBar.Padding = new Padding(12, 0, 12, 0);
            topBar.Size = new Size(1651, 48);
            topBar.TabIndex = 0;
            // 
            // infoLabel
            // 
            infoLabel.Dock = DockStyle.Fill;
            infoLabel.Font = new Font("Segoe UI", 11F);
            infoLabel.ForeColor = Color.Gainsboro;
            infoLabel.Location = new Point(312, 0);
            infoLabel.Name = "infoLabel";
            infoLabel.Size = new Size(1087, 48);
            infoLabel.TabIndex = 0;
            infoLabel.Text = "Cashier";
            infoLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // clockLabel
            // 
            clockLabel.Dock = DockStyle.Right;
            clockLabel.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
            clockLabel.ForeColor = Color.White;
            clockLabel.Location = new Point(1399, 0);
            clockLabel.Name = "clockLabel";
            clockLabel.Size = new Size(240, 48);
            clockLabel.TabIndex = 1;
            clockLabel.Text = "00:00";
            clockLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // storeLabel
            // 
            storeLabel.Dock = DockStyle.Left;
            storeLabel.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
            storeLabel.ForeColor = Color.White;
            storeLabel.Location = new Point(12, 0);
            storeLabel.Name = "storeLabel";
            storeLabel.Size = new Size(300, 48);
            storeLabel.TabIndex = 2;
            storeLabel.Text = "My Store";
            storeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // bodyTable
            // 
            bodyTable.ColumnCount = 2;
            bodyTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            bodyTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            bodyTable.Controls.Add(leftPanel, 0, 0);
            bodyTable.Controls.Add(rightPanel, 1, 0);
            bodyTable.Dock = DockStyle.Fill;
            bodyTable.Location = new Point(0, 48);
            bodyTable.Name = "bodyTable";
            bodyTable.Padding = new Padding(6);
            bodyTable.RowCount = 1;
            bodyTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            bodyTable.Size = new Size(1651, 720);
            bodyTable.TabIndex = 1;
            // 
            // leftPanel
            // 
            leftPanel.Controls.Add(linesGrid);
            leftPanel.Controls.Add(scanPanel);
            leftPanel.Controls.Add(bottomTable);
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Location = new Point(9, 9);
            leftPanel.Name = "leftPanel";
            leftPanel.Size = new Size(1010, 702);
            leftPanel.TabIndex = 0;
            // 
            // linesGrid
            // 
            linesGrid.AllowUserToAddRows = false;
            linesGrid.AllowUserToDeleteRows = false;
            linesGrid.AllowUserToResizeRows = false;
            linesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            linesGrid.BackgroundColor = Color.White;
            linesGrid.ColumnHeadersHeight = 40;
            linesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            linesGrid.Dock = DockStyle.Fill;
            linesGrid.Font = new Font("Segoe UI", 13F);
            linesGrid.Location = new Point(0, 0);
            linesGrid.MultiSelect = false;
            linesGrid.Name = "linesGrid";
            linesGrid.ReadOnly = true;
            linesGrid.RowHeadersVisible = false;
            linesGrid.RowHeadersWidth = 51;
            linesGrid.RowTemplate.Height = 40;
            linesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            linesGrid.Size = new Size(1010, 308);
            linesGrid.TabIndex = 2;
            linesGrid.TabStop = false;
            linesGrid.DataBindingComplete += linesGrid_DataBindingComplete;
            // 
            // scanPanel
            // 
            scanPanel.Controls.Add(scanBox);
            scanPanel.Controls.Add(scanLabel);
            scanPanel.Dock = DockStyle.Bottom;
            scanPanel.Location = new Point(0, 308);
            scanPanel.Name = "scanPanel";
            scanPanel.Padding = new Padding(0, 8, 0, 4);
            scanPanel.Size = new Size(1010, 64);
            scanPanel.TabIndex = 0;
            // 
            // scanBox
            // 
            scanBox.Dock = DockStyle.Fill;
            scanBox.Font = new Font("Segoe UI", 20F);
            scanBox.Location = new Point(150, 8);
            scanBox.Name = "scanBox";
            scanBox.PlaceholderText = "Scan barcode or type name, Enter   (3*barcode = 3 pieces)";
            scanBox.Size = new Size(860, 52);
            scanBox.TabIndex = 0;
            scanBox.Enter += entry_Enter;
            scanBox.KeyDown += scanBox_KeyDown;
            // 
            // scanLabel
            // 
            scanLabel.Dock = DockStyle.Left;
            scanLabel.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            scanLabel.Location = new Point(0, 8);
            scanLabel.Name = "scanLabel";
            scanLabel.Size = new Size(150, 52);
            scanLabel.TabIndex = 1;
            scanLabel.Text = "Barcode (F1)";
            scanLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // bottomTable
            // 
            bottomTable.ColumnCount = 3;
            bottomTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            bottomTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            bottomTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47F));
            bottomTable.Controls.Add(keypadTable, 0, 0);
            bottomTable.Controls.Add(functionTable, 1, 0);
            bottomTable.Controls.Add(totalsTable, 2, 0);
            bottomTable.Dock = DockStyle.Bottom;
            bottomTable.Location = new Point(0, 372);
            bottomTable.Name = "bottomTable";
            bottomTable.RowCount = 1;
            bottomTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            bottomTable.Size = new Size(1010, 330);
            bottomTable.TabIndex = 1;
            // 
            // keypadTable
            // 
            keypadTable.ColumnCount = 4;
            keypadTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            keypadTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            keypadTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            keypadTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            keypadTable.Dock = DockStyle.Fill;
            keypadTable.Location = new Point(3, 3);
            keypadTable.Name = "keypadTable";
            keypadTable.RowCount = 4;
            keypadTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            keypadTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            keypadTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            keypadTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            keypadTable.Size = new Size(327, 324);
            keypadTable.TabIndex = 0;
            // 
            // functionTable
            // 
            functionTable.ColumnCount = 1;
            functionTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            functionTable.Dock = DockStyle.Fill;
            functionTable.Location = new Point(336, 3);
            functionTable.Name = "functionTable";
            functionTable.RowCount = 6;
            functionTable.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66F));
            functionTable.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66F));
            functionTable.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66F));
            functionTable.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66F));
            functionTable.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66F));
            functionTable.RowStyles.Add(new RowStyle(SizeType.Percent, 16.7F));
            functionTable.Size = new Size(196, 324);
            functionTable.TabIndex = 1;
            functionTable.Paint += functionTable_Paint;
            // 
            // totalsTable
            // 
            totalsTable.ColumnCount = 2;
            totalsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            totalsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            totalsTable.Controls.Add(subTotalCaption, 0, 0);
            totalsTable.Controls.Add(subTotalValue, 1, 0);
            totalsTable.Controls.Add(discountCaption, 0, 1);
            totalsTable.Controls.Add(discountPanel, 1, 1);
            totalsTable.Controls.Add(totalCaption, 0, 2);
            totalsTable.Controls.Add(totalValue, 1, 2);
            totalsTable.Controls.Add(totalUsdValue, 1, 3);
            totalsTable.Controls.Add(actionsTable, 0, 4);
            totalsTable.Dock = DockStyle.Fill;
            totalsTable.Location = new Point(538, 3);
            totalsTable.Name = "totalsTable";
            totalsTable.Padding = new Padding(6, 0, 0, 0);
            totalsTable.RowCount = 5;
            totalsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            totalsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            totalsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            totalsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            totalsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            totalsTable.Size = new Size(469, 324);
            totalsTable.TabIndex = 2;
            // 
            // subTotalCaption
            // 
            subTotalCaption.Dock = DockStyle.Fill;
            subTotalCaption.Font = new Font("Segoe UI", 12F);
            subTotalCaption.Location = new Point(9, 0);
            subTotalCaption.Name = "subTotalCaption";
            subTotalCaption.Size = new Size(114, 36);
            subTotalCaption.TabIndex = 0;
            subTotalCaption.Text = "Sub total";
            subTotalCaption.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // subTotalValue
            // 
            subTotalValue.Dock = DockStyle.Fill;
            subTotalValue.Font = new Font("Segoe UI", 13F);
            subTotalValue.Location = new Point(129, 0);
            subTotalValue.Name = "subTotalValue";
            subTotalValue.Size = new Size(337, 36);
            subTotalValue.TabIndex = 1;
            subTotalValue.Text = "0";
            subTotalValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // discountCaption
            // 
            discountCaption.Dock = DockStyle.Fill;
            discountCaption.Font = new Font("Segoe UI", 12F);
            discountCaption.Location = new Point(9, 36);
            discountCaption.Name = "discountCaption";
            discountCaption.Size = new Size(114, 46);
            discountCaption.TabIndex = 2;
            discountCaption.Text = "Discount (F6)";
            discountCaption.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // discountPanel
            // 
            discountPanel.Controls.Add(discountBox);
            discountPanel.Controls.Add(percentCheck);
            discountPanel.Dock = DockStyle.Fill;
            discountPanel.Location = new Point(129, 39);
            discountPanel.Name = "discountPanel";
            discountPanel.Padding = new Padding(0, 4, 0, 4);
            discountPanel.Size = new Size(337, 40);
            discountPanel.TabIndex = 3;
            // 
            // discountBox
            // 
            discountBox.Dock = DockStyle.Fill;
            discountBox.Font = new Font("Segoe UI", 15F);
            discountBox.Location = new Point(0, 4);
            discountBox.Name = "discountBox";
            discountBox.PlaceholderText = "0";
            discountBox.Size = new Size(283, 41);
            discountBox.TabIndex = 1;
            discountBox.TextAlign = HorizontalAlignment.Right;
            discountBox.TextChanged += discountBox_TextChanged;
            discountBox.Enter += entry_Enter;
            discountBox.KeyDown += discountBox_KeyDown;
            // 
            // percentCheck
            // 
            percentCheck.Appearance = Appearance.Button;
            percentCheck.Dock = DockStyle.Right;
            percentCheck.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            percentCheck.Location = new Point(283, 4);
            percentCheck.Name = "percentCheck";
            percentCheck.Size = new Size(54, 32);
            percentCheck.TabIndex = 2;
            percentCheck.TabStop = false;
            percentCheck.Text = "%";
            percentCheck.TextAlign = ContentAlignment.MiddleCenter;
            percentCheck.CheckedChanged += discountBox_TextChanged;
            // 
            // totalCaption
            // 
            totalCaption.Dock = DockStyle.Fill;
            totalCaption.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            totalCaption.Location = new Point(9, 82);
            totalCaption.Name = "totalCaption";
            totalCaption.Size = new Size(114, 56);
            totalCaption.TabIndex = 4;
            totalCaption.Text = "TOTAL";
            totalCaption.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // totalValue
            // 
            totalValue.Dock = DockStyle.Fill;
            totalValue.Font = new Font("Segoe UI", 26F, FontStyle.Bold);
            totalValue.ForeColor = Color.FromArgb(22, 32, 43);
            totalValue.Location = new Point(129, 82);
            totalValue.Name = "totalValue";
            totalValue.Size = new Size(337, 56);
            totalValue.TabIndex = 5;
            totalValue.Text = "0";
            totalValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // totalUsdValue
            // 
            totalUsdValue.Dock = DockStyle.Fill;
            totalUsdValue.Font = new Font("Segoe UI", 12F);
            totalUsdValue.ForeColor = Color.DimGray;
            totalUsdValue.Location = new Point(129, 138);
            totalUsdValue.Name = "totalUsdValue";
            totalUsdValue.Size = new Size(337, 30);
            totalUsdValue.TabIndex = 6;
            totalUsdValue.Text = "$ 0.00";
            totalUsdValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // actionsTable
            // 
            actionsTable.ColumnCount = 3;
            totalsTable.SetColumnSpan(actionsTable, 2);
            actionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            actionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            actionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            actionsTable.Dock = DockStyle.Fill;
            actionsTable.Location = new Point(9, 171);
            actionsTable.Name = "actionsTable";
            actionsTable.RowCount = 2;
            actionsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            actionsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            actionsTable.Size = new Size(457, 150);
            actionsTable.TabIndex = 2;
            actionsTable.Paint += actionsTable_Paint;
            // 
            // rightPanel
            // 
            rightPanel.Controls.Add(quickPanel);
            rightPanel.Controls.Add(groupPanel);
            rightPanel.Controls.Add(lastItemPanel);
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.Location = new Point(1025, 9);
            rightPanel.Name = "rightPanel";
            rightPanel.Padding = new Padding(6, 0, 0, 0);
            rightPanel.Size = new Size(617, 702);
            rightPanel.TabIndex = 1;
            // 
            // quickPanel
            // 
            quickPanel.AutoScroll = true;
            quickPanel.BackColor = Color.FromArgb(238, 241, 244);
            quickPanel.ContextMenuStrip = quickMenu;
            quickPanel.Controls.Add(quickHint);
            quickPanel.Dock = DockStyle.Fill;
            quickPanel.Location = new Point(6, 202);
            quickPanel.Name = "quickPanel";
            quickPanel.Padding = new Padding(8);
            quickPanel.Size = new Size(611, 500);
            quickPanel.TabIndex = 1;
            quickPanel.Paint += quickPanel_Paint;
            quickPanel.Resize += quickPanel_Resize;
            // 
            // quickMenu
            // 
            quickMenu.ImageScalingSize = new Size(20, 20);
            quickMenu.Items.AddRange(new ToolStripItem[] { reloadQuickMenuItem });
            quickMenu.Name = "quickMenu";
            quickMenu.Size = new Size(219, 28);
            // 
            // reloadQuickMenuItem
            // 
            reloadQuickMenuItem.Name = "reloadQuickMenuItem";
            reloadQuickMenuItem.Size = new Size(218, 24);
            reloadQuickMenuItem.Text = "Reload quick buttons";
            reloadQuickMenuItem.Click += reloadQuickMenuItem_Click;
            // 
            // quickHint
            // 
            quickHint.AutoSize = true;
            quickHint.Font = new Font("Segoe UI", 11F);
            quickHint.ForeColor = Color.Gray;
            quickHint.Location = new Point(16, 16);
            quickHint.Margin = new Padding(8);
            quickHint.MaximumSize = new Size(440, 0);
            quickHint.Name = "quickHint";
            quickHint.Size = new Size(211, 25);
            quickHint.TabIndex = 0;
            quickHint.Text = "Loading quick buttons...";
            // 
            // groupPanel
            // 
            groupPanel.AutoSize = true;
            groupPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            groupPanel.BackColor = Color.FromArgb(226, 231, 236);
            groupPanel.Dock = DockStyle.Top;
            groupPanel.Location = new Point(6, 190);
            groupPanel.Name = "groupPanel";
            groupPanel.Padding = new Padding(8, 6, 8, 6);
            groupPanel.Size = new Size(611, 12);
            groupPanel.TabIndex = 2;
            groupPanel.Visible = false;
            groupPanel.Resize += quickPanel_Resize;
            // 
            // lastItemPanel
            // 
            lastItemPanel.BackColor = Color.FromArgb(22, 32, 43);
            lastItemPanel.Controls.Add(statusLabel);
            lastItemPanel.Controls.Add(lastItemPrice);
            lastItemPanel.Controls.Add(lastItemName);
            lastItemPanel.Dock = DockStyle.Top;
            lastItemPanel.Location = new Point(6, 0);
            lastItemPanel.Name = "lastItemPanel";
            lastItemPanel.Padding = new Padding(14, 10, 14, 10);
            lastItemPanel.Size = new Size(611, 190);
            lastItemPanel.TabIndex = 0;
            // 
            // statusLabel
            // 
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.Font = new Font("Segoe UI", 12F);
            statusLabel.ForeColor = Color.Gainsboro;
            statusLabel.Location = new Point(14, 110);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(583, 70);
            statusLabel.TabIndex = 0;
            statusLabel.Text = "Ready";
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lastItemPrice
            // 
            lastItemPrice.Dock = DockStyle.Top;
            lastItemPrice.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            lastItemPrice.ForeColor = Color.FromArgb(120, 220, 190);
            lastItemPrice.Location = new Point(14, 54);
            lastItemPrice.Name = "lastItemPrice";
            lastItemPrice.Size = new Size(583, 56);
            lastItemPrice.TabIndex = 1;
            lastItemPrice.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lastItemName
            // 
            lastItemName.AutoEllipsis = true;
            lastItemName.Dock = DockStyle.Top;
            lastItemName.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold);
            lastItemName.ForeColor = Color.White;
            lastItemName.Location = new Point(14, 10);
            lastItemName.Name = "lastItemName";
            lastItemName.Size = new Size(583, 44);
            lastItemName.TabIndex = 2;
            lastItemName.Text = "Welcome";
            lastItemName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // clockTimer
            // 
            clockTimer.Interval = 1000;
            clockTimer.Tick += clockTimer_Tick;
            // 
            // CashierForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1651, 768);
            Controls.Add(bodyTable);
            Controls.Add(topBar);
            KeyPreview = true;
            MinimumSize = new Size(1200, 700);
            Name = "CashierForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Cashier";
            WindowState = FormWindowState.Maximized;
            FormClosing += CashierForm_FormClosing;
            Load += CashierForm_Load;
            Shown += CashierForm_Shown;
            topBar.ResumeLayout(false);
            bodyTable.ResumeLayout(false);
            leftPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)linesGrid).EndInit();
            scanPanel.ResumeLayout(false);
            scanPanel.PerformLayout();
            bottomTable.ResumeLayout(false);
            totalsTable.ResumeLayout(false);
            discountPanel.ResumeLayout(false);
            discountPanel.PerformLayout();
            rightPanel.ResumeLayout(false);
            rightPanel.PerformLayout();
            quickPanel.ResumeLayout(false);
            quickPanel.PerformLayout();
            quickMenu.ResumeLayout(false);
            lastItemPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel topBar;
        private Label infoLabel;
        private Label clockLabel;
        private Label storeLabel;
        private TableLayoutPanel bodyTable;
        private Panel leftPanel;
        private DataGridView linesGrid;
        private Panel scanPanel;
        private TextBox scanBox;
        private Label scanLabel;
        private TableLayoutPanel bottomTable;
        private TableLayoutPanel keypadTable;
        private TableLayoutPanel functionTable;
        private TableLayoutPanel totalsTable;
        private Label subTotalCaption;
        private Label subTotalValue;
        private Label discountCaption;
        private Panel discountPanel;
        private TextBox discountBox;
        private CheckBox percentCheck;
        private Label totalCaption;
        private Label totalValue;
        private Label totalUsdValue;
        private TableLayoutPanel actionsTable;
        private Panel rightPanel;
        private FlowLayoutPanel quickPanel;
        private FlowLayoutPanel groupPanel;
        private Label quickHint;
        private ContextMenuStrip quickMenu;
        private ToolStripMenuItem reloadQuickMenuItem;
        private Panel lastItemPanel;
        private Label statusLabel;
        private Label lastItemPrice;
        private Label lastItemName;
        private System.Windows.Forms.Timer clockTimer;
    }
}
