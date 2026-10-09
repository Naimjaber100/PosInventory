namespace Inventory
{
    partial class PackagingPickerForm
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
            resultsGrid = new DataGridView();
            buttonsPanel = new FlowLayoutPanel();
            cancelButton = new Button();
            selectButton = new Button();
            ((System.ComponentModel.ISupportInitialize)resultsGrid).BeginInit();
            buttonsPanel.SuspendLayout();
            SuspendLayout();
            //
            // resultsGrid
            //
            resultsGrid.AllowUserToAddRows = false;
            resultsGrid.AllowUserToDeleteRows = false;
            resultsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            resultsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resultsGrid.Dock = DockStyle.Fill;
            resultsGrid.Location = new Point(0, 0);
            resultsGrid.MultiSelect = false;
            resultsGrid.Name = "resultsGrid";
            resultsGrid.ReadOnly = true;
            resultsGrid.RowHeadersVisible = false;
            resultsGrid.RowHeadersWidth = 51;
            resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            resultsGrid.Size = new Size(820, 380);
            resultsGrid.TabIndex = 0;
            resultsGrid.CellDoubleClick += resultsGrid_CellDoubleClick;
            resultsGrid.DataBindingComplete += resultsGrid_DataBindingComplete;
            resultsGrid.KeyDown += resultsGrid_KeyDown;
            //
            // buttonsPanel
            //
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(selectButton);
            buttonsPanel.Dock = DockStyle.Bottom;
            buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonsPanel.Location = new Point(0, 380);
            buttonsPanel.Name = "buttonsPanel";
            buttonsPanel.Padding = new Padding(8, 6, 8, 6);
            buttonsPanel.Size = new Size(820, 52);
            buttonsPanel.TabIndex = 1;
            //
            // cancelButton
            //
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(100, 36);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Cancel";
            //
            // selectButton
            //
            selectButton.BackColor = Color.FromArgb(34, 139, 117);
            selectButton.FlatAppearance.BorderSize = 0;
            selectButton.FlatStyle = FlatStyle.Flat;
            selectButton.ForeColor = Color.White;
            selectButton.Name = "selectButton";
            selectButton.Size = new Size(100, 36);
            selectButton.TabIndex = 0;
            selectButton.Text = "Select";
            selectButton.UseVisualStyleBackColor = false;
            selectButton.Click += selectButton_Click;
            //
            // PackagingPickerForm
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(820, 432);
            Controls.Add(resultsGrid);
            Controls.Add(buttonsPanel);
            MinimizeBox = false;
            Name = "PackagingPickerForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Choose an item";
            ((System.ComponentModel.ISupportInitialize)resultsGrid).EndInit();
            buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private DataGridView resultsGrid;
        private FlowLayoutPanel buttonsPanel;
        private Button cancelButton;
        private Button selectButton;
    }
}
