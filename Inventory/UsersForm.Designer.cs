namespace Inventory
{
    partial class UsersForm
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
            usersGrid = new DataGridView();
            toolPanel = new FlowLayoutPanel();
            addButton = new Button();
            editButton = new Button();
            deleteButton = new Button();
            countLabel = new Label();
            bottomPanel = new FlowLayoutPanel();
            closeButton = new Button();
            ((System.ComponentModel.ISupportInitialize)usersGrid).BeginInit();
            toolPanel.SuspendLayout();
            bottomPanel.SuspendLayout();
            SuspendLayout();
            //
            // usersGrid
            //
            usersGrid.AllowUserToAddRows = false;
            usersGrid.AllowUserToDeleteRows = false;
            usersGrid.AllowUserToResizeRows = false;
            usersGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            usersGrid.BackgroundColor = Color.White;
            usersGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            usersGrid.Dock = DockStyle.Fill;
            usersGrid.Location = new Point(0, 52);
            usersGrid.MultiSelect = false;
            usersGrid.Name = "usersGrid";
            usersGrid.ReadOnly = true;
            usersGrid.RowHeadersVisible = false;
            usersGrid.RowHeadersWidth = 51;
            usersGrid.RowTemplate.Height = 34;
            usersGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            usersGrid.Size = new Size(820, 396);
            usersGrid.TabIndex = 0;
            usersGrid.CellDoubleClick += usersGrid_CellDoubleClick;
            usersGrid.CellFormatting += usersGrid_CellFormatting;
            usersGrid.DataBindingComplete += usersGrid_DataBindingComplete;
            usersGrid.KeyDown += usersGrid_KeyDown;
            //
            // toolPanel
            //
            toolPanel.Controls.Add(addButton);
            toolPanel.Controls.Add(editButton);
            toolPanel.Controls.Add(deleteButton);
            toolPanel.Controls.Add(countLabel);
            toolPanel.Dock = DockStyle.Top;
            toolPanel.Location = new Point(0, 0);
            toolPanel.Name = "toolPanel";
            toolPanel.Padding = new Padding(8, 6, 8, 6);
            toolPanel.Size = new Size(820, 52);
            toolPanel.TabIndex = 1;
            toolPanel.WrapContents = false;
            //
            // addButton
            //
            addButton.BackColor = Color.FromArgb(34, 139, 117);
            addButton.FlatAppearance.BorderSize = 0;
            addButton.FlatStyle = FlatStyle.Flat;
            addButton.ForeColor = Color.White;
            addButton.Name = "addButton";
            addButton.Size = new Size(130, 36);
            addButton.TabIndex = 0;
            addButton.Text = "+ Add user (Ins)";
            addButton.UseVisualStyleBackColor = false;
            addButton.Click += addButton_Click;
            //
            // editButton
            //
            editButton.Name = "editButton";
            editButton.Size = new Size(90, 36);
            editButton.TabIndex = 1;
            editButton.Text = "Edit";
            editButton.Click += editButton_Click;
            //
            // deleteButton
            //
            deleteButton.ForeColor = Color.Firebrick;
            deleteButton.Name = "deleteButton";
            deleteButton.Size = new Size(90, 36);
            deleteButton.TabIndex = 2;
            deleteButton.Text = "Delete";
            deleteButton.Click += deleteButton_Click;
            //
            // countLabel
            //
            countLabel.AutoSize = true;
            countLabel.ForeColor = Color.DimGray;
            countLabel.Margin = new Padding(16, 11, 3, 0);
            countLabel.Name = "countLabel";
            countLabel.TabIndex = 3;
            //
            // bottomPanel
            //
            bottomPanel.Controls.Add(closeButton);
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.FlowDirection = FlowDirection.RightToLeft;
            bottomPanel.Location = new Point(0, 448);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new Padding(8, 6, 8, 6);
            bottomPanel.Size = new Size(820, 52);
            bottomPanel.TabIndex = 2;
            //
            // closeButton
            //
            closeButton.DialogResult = DialogResult.Cancel;
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(100, 36);
            closeButton.TabIndex = 0;
            closeButton.Text = "Close";
            //
            // UsersForm
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = closeButton;
            ClientSize = new Size(820, 500);
            Controls.Add(usersGrid);
            Controls.Add(toolPanel);
            Controls.Add(bottomPanel);
            MinimizeBox = false;
            MinimumSize = new Size(640, 360);
            Name = "UsersForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Users";
            Load += UsersForm_Load;
            ((System.ComponentModel.ISupportInitialize)usersGrid).EndInit();
            toolPanel.ResumeLayout(false);
            toolPanel.PerformLayout();
            bottomPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private DataGridView usersGrid;
        private FlowLayoutPanel toolPanel;
        private Button addButton;
        private Button editButton;
        private Button deleteButton;
        private Label countLabel;
        private FlowLayoutPanel bottomPanel;
        private Button closeButton;
    }
}
