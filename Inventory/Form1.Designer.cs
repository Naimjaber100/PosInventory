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
        ///  The menu on the left and the dashboard are created in code (Form1.BuildSidebar / BuildDashboard).
        /// </summary>
        private void InitializeComponent()
        {
            sidebarPanel = new Panel();
            statusPanel = new Panel();
            contentPanel = new Panel();
            SuspendLayout();
            //
            // sidebarPanel
            //
            sidebarPanel.BackColor = Color.FromArgb(22, 32, 43);
            sidebarPanel.Dock = DockStyle.Left;
            sidebarPanel.Name = "sidebarPanel";
            sidebarPanel.Size = new Size(250, 889);
            sidebarPanel.TabIndex = 0;
            //
            // statusPanel
            //
            statusPanel.BackColor = Color.White;
            statusPanel.Dock = DockStyle.Bottom;
            statusPanel.Name = "statusPanel";
            statusPanel.Padding = new Padding(22, 0, 22, 0);
            statusPanel.Size = new Size(1434, 34);
            statusPanel.TabIndex = 2;
            //
            // contentPanel
            //
            contentPanel.BackColor = Color.FromArgb(243, 245, 248);
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Name = "contentPanel";
            contentPanel.Padding = new Padding(28, 14, 28, 14);
            contentPanel.TabIndex = 1;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1684, 889);
            Controls.Add(contentPanel);
            Controls.Add(statusPanel);
            Controls.Add(sidebarPanel);
            MinimumSize = new Size(1100, 700);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
        }

        #endregion

        private Panel sidebarPanel;
        private Panel statusPanel;
        private Panel contentPanel;
    }
}
