namespace Inventory
{
    partial class CustomersForm
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
            listGrid = new DataGridView();
            toolPanel = new FlowLayoutPanel();
            addButton = new Button();
            editButton = new Button();
            deleteButton = new Button();
            searchText = new TextBox();
            inactiveCheck = new CheckBox();
            countLabel = new Label();
            bottomPanel = new FlowLayoutPanel();
            closeButton = new Button();
            ((System.ComponentModel.ISupportInitialize)listGrid).BeginInit();
            toolPanel.SuspendLayout();
            bottomPanel.SuspendLayout();
            SuspendLayout();
            //
            // listGrid
            //
            listGrid.AllowUserToAddRows = false;
            listGrid.AllowUserToDeleteRows = false;
            listGrid.AllowUserToResizeRows = false;
            listGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            listGrid.BackgroundColor = Color.White;
            listGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            listGrid.Dock = DockStyle.Fill;
            listGrid.Location = new Point(0, 52);
            listGrid.MultiSelect = false;
            listGrid.Name = "listGrid";
            listGrid.ReadOnly = true;
            listGrid.RowHeadersVisible = false;
            listGrid.RowHeadersWidth = 51;
            listGrid.RowTemplate.Height = 34;
            listGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            listGrid.Size = new Size(1100, 496);
            listGrid.TabIndex = 0;
            listGrid.CellDoubleClick += listGrid_CellDoubleClick;
            listGrid.CellFormatting += listGrid_CellFormatting;
            listGrid.DataBindingComplete += listGrid_DataBindingComplete;
            listGrid.KeyDown += listGrid_KeyDown;
            //
            // toolPanel
            //
            toolPanel.Controls.Add(addButton);
            toolPanel.Controls.Add(editButton);
            toolPanel.Controls.Add(deleteButton);
            toolPanel.Controls.Add(searchText);
            toolPanel.Controls.Add(inactiveCheck);
            toolPanel.Controls.Add(countLabel);
            toolPanel.Dock = DockStyle.Top;
            toolPanel.Location = new Point(0, 0);
            toolPanel.Name = "toolPanel";
            toolPanel.Padding = new Padding(8, 6, 8, 6);
            toolPanel.Size = new Size(1100, 52);
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
            addButton.Size = new Size(150, 36);
            addButton.TabIndex = 0;
            addButton.Text = "+ Add client (Ins)";
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
            // searchText
            //
            searchText.Margin = new Padding(16, 7, 3, 3);
            searchText.Name = "searchText";
            searchText.PlaceholderText = "Search name, phone or email...";
            searchText.Size = new Size(260, 27);
            searchText.TabIndex = 3;
            searchText.TextChanged += searchText_TextChanged;
            searchText.KeyDown += searchText_KeyDown;
            //
            // inactiveCheck
            //
            inactiveCheck.AutoSize = true;
            inactiveCheck.Margin = new Padding(12, 10, 3, 3);
            inactiveCheck.Name = "inactiveCheck";
            inactiveCheck.TabIndex = 4;
            inactiveCheck.Text = "Show inactive";
            inactiveCheck.CheckedChanged += inactiveCheck_CheckedChanged;
            //
            // countLabel
            //
            countLabel.AutoSize = true;
            countLabel.ForeColor = Color.DimGray;
            countLabel.Margin = new Padding(16, 11, 3, 0);
            countLabel.Name = "countLabel";
            countLabel.TabIndex = 5;
            //
            // bottomPanel
            //
            bottomPanel.Controls.Add(closeButton);
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.FlowDirection = FlowDirection.RightToLeft;
            bottomPanel.Location = new Point(0, 548);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new Padding(8, 6, 8, 6);
            bottomPanel.Size = new Size(1100, 52);
            bottomPanel.TabIndex = 2;
            //
            // closeButton
            //
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(100, 36);
            closeButton.TabIndex = 0;
            closeButton.Text = "Close";
            closeButton.Click += closeButton_Click;
            //
            // CustomersForm
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 600);
            Controls.Add(listGrid);
            Controls.Add(toolPanel);
            Controls.Add(bottomPanel);
            KeyPreview = true;
            MinimumSize = new Size(800, 400);
            Name = "CustomersForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Clients";
            Load += CustomersForm_Load;
            KeyDown += CustomersForm_KeyDown;
            ((System.ComponentModel.ISupportInitialize)listGrid).EndInit();
            toolPanel.ResumeLayout(false);
            toolPanel.PerformLayout();
            bottomPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private DataGridView listGrid;
        private FlowLayoutPanel toolPanel;
        private Button addButton;
        private Button editButton;
        private Button deleteButton;
        private TextBox searchText;
        private CheckBox inactiveCheck;
        private Label countLabel;
        private FlowLayoutPanel bottomPanel;
        private Button closeButton;
    }
}
