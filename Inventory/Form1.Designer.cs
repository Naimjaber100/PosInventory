namespace Inventory
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            menuStrip1 = new MenuStrip();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            companyToolStripMenuItem = new ToolStripMenuItem();
            currenciesToolStripMenuItem = new ToolStripMenuItem();
            usersToolStripMenuItem = new ToolStripMenuItem();
            itemToolStripMenuItem = new ToolStripMenuItem();
            itemListToolStripMenuItem = new ToolStripMenuItem();
            categoryToolStripMenuItem = new ToolStripMenuItem();
            quickButtonsToolStripMenuItem = new ToolStripMenuItem();
            purchaseToolStripMenuItem = new ToolStripMenuItem();
            invoiceToolStripMenuItem = new ToolStripMenuItem();
            reportsToolStripMenuItem = new ToolStripMenuItem();
            dailySalesReportToolStripMenuItem = new ToolStripMenuItem();
            cashierToolStripMenuItem = new ToolStripMenuItem();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { settingsToolStripMenuItem, itemToolStripMenuItem, purchaseToolStripMenuItem, invoiceToolStripMenuItem, reportsToolStripMenuItem, cashierToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1684, 28);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            //
            // settingsToolStripMenuItem
            //
            settingsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { companyToolStripMenuItem, currenciesToolStripMenuItem, usersToolStripMenuItem });
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new Size(76, 24);
            settingsToolStripMenuItem.Text = "Settings";
            //
            // companyToolStripMenuItem
            //
            companyToolStripMenuItem.Name = "companyToolStripMenuItem";
            companyToolStripMenuItem.Size = new Size(224, 26);
            companyToolStripMenuItem.Text = "Company";
            companyToolStripMenuItem.Click += companyToolStripMenuItem_Click;
            //
            // currenciesToolStripMenuItem
            //
            currenciesToolStripMenuItem.Name = "currenciesToolStripMenuItem";
            currenciesToolStripMenuItem.Size = new Size(260, 26);
            currenciesToolStripMenuItem.Text = "Currencies && exchange rates";
            currenciesToolStripMenuItem.Click += currenciesToolStripMenuItem_Click;
            //
            // usersToolStripMenuItem
            //
            usersToolStripMenuItem.Name = "usersToolStripMenuItem";
            usersToolStripMenuItem.Size = new Size(224, 26);
            usersToolStripMenuItem.Text = "Users";
            usersToolStripMenuItem.Click += usersToolStripMenuItem_Click;
            //
            // itemToolStripMenuItem
            //
            itemToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { itemListToolStripMenuItem, categoryToolStripMenuItem, quickButtonsToolStripMenuItem });
            itemToolStripMenuItem.Name = "itemToolStripMenuItem";
            itemToolStripMenuItem.Size = new Size(53, 24);
            itemToolStripMenuItem.Text = "Item";
            // 
            // itemListToolStripMenuItem
            // 
            itemListToolStripMenuItem.Name = "itemListToolStripMenuItem";
            itemListToolStripMenuItem.Size = new Size(233, 26);
            itemListToolStripMenuItem.Text = "Item list";
            itemListToolStripMenuItem.Click += itemListToolStripMenuItem_Click;
            // 
            // categoryToolStripMenuItem
            // 
            categoryToolStripMenuItem.Name = "categoryToolStripMenuItem";
            categoryToolStripMenuItem.Size = new Size(233, 26);
            categoryToolStripMenuItem.Text = "Category";
            // 
            // quickButtonsToolStripMenuItem
            // 
            quickButtonsToolStripMenuItem.Name = "quickButtonsToolStripMenuItem";
            quickButtonsToolStripMenuItem.Size = new Size(233, 26);
            quickButtonsToolStripMenuItem.Text = "POS buttons (cashier)";
            quickButtonsToolStripMenuItem.Click += quickButtonsToolStripMenuItem_Click;
            // 
            // purchaseToolStripMenuItem
            // 
            purchaseToolStripMenuItem.Name = "purchaseToolStripMenuItem";
            purchaseToolStripMenuItem.Size = new Size(81, 24);
            purchaseToolStripMenuItem.Text = "Purchase";
            purchaseToolStripMenuItem.Click += purchaseToolStripMenuItem_Click;
            // 
            // invoiceToolStripMenuItem
            // 
            invoiceToolStripMenuItem.Name = "invoiceToolStripMenuItem";
            invoiceToolStripMenuItem.Size = new Size(70, 24);
            invoiceToolStripMenuItem.Text = "Invoice";
            //
            // reportsToolStripMenuItem
            //
            reportsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { dailySalesReportToolStripMenuItem });
            reportsToolStripMenuItem.Name = "reportsToolStripMenuItem";
            reportsToolStripMenuItem.Size = new Size(74, 24);
            reportsToolStripMenuItem.Text = "Reports";
            //
            // dailySalesReportToolStripMenuItem
            //
            dailySalesReportToolStripMenuItem.Name = "dailySalesReportToolStripMenuItem";
            dailySalesReportToolStripMenuItem.Size = new Size(240, 26);
            dailySalesReportToolStripMenuItem.Text = "Daily sales summary";
            dailySalesReportToolStripMenuItem.Click += dailySalesReportToolStripMenuItem_Click;
            // 
            // cashierToolStripMenuItem
            // 
            cashierToolStripMenuItem.Name = "cashierToolStripMenuItem";
            cashierToolStripMenuItem.Size = new Size(108, 24);
            cashierToolStripMenuItem.Text = "Cashier (F12)";
            cashierToolStripMenuItem.Click += cashierToolStripMenuItem_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1684, 889);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private ToolStripMenuItem companyToolStripMenuItem;
        private ToolStripMenuItem currenciesToolStripMenuItem;
        private ToolStripMenuItem usersToolStripMenuItem;
        private ToolStripMenuItem itemToolStripMenuItem;
        private ToolStripMenuItem itemListToolStripMenuItem;
        private ToolStripMenuItem categoryToolStripMenuItem;
        private ToolStripMenuItem quickButtonsToolStripMenuItem;
        private ToolStripMenuItem purchaseToolStripMenuItem;
        private ToolStripMenuItem invoiceToolStripMenuItem;
        private ToolStripMenuItem reportsToolStripMenuItem;
        private ToolStripMenuItem dailySalesReportToolStripMenuItem;
        private ToolStripMenuItem cashierToolStripMenuItem;
    }
}
