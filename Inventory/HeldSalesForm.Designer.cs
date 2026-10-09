namespace Inventory
{
    partial class HeldSalesForm
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
            heldGrid = new DataGridView();
            buttonsPanel = new FlowLayoutPanel();
            cancelButton = new Button();
            recallButton = new Button();
            deleteButton = new Button();
            ((System.ComponentModel.ISupportInitialize)heldGrid).BeginInit();
            buttonsPanel.SuspendLayout();
            SuspendLayout();
            //
            // heldGrid
            //
            heldGrid.AllowUserToAddRows = false;
            heldGrid.AllowUserToDeleteRows = false;
            heldGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            heldGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            heldGrid.Dock = DockStyle.Fill;
            heldGrid.Font = new Font("Segoe UI", 11F);
            heldGrid.MultiSelect = false;
            heldGrid.Name = "heldGrid";
            heldGrid.ReadOnly = true;
            heldGrid.RowHeadersVisible = false;
            heldGrid.RowHeadersWidth = 51;
            heldGrid.RowTemplate.Height = 36;
            heldGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            heldGrid.Size = new Size(680, 360);
            heldGrid.TabIndex = 0;
            heldGrid.CellDoubleClick += heldGrid_CellDoubleClick;
            heldGrid.DataBindingComplete += heldGrid_DataBindingComplete;
            heldGrid.KeyDown += heldGrid_KeyDown;
            //
            // buttonsPanel
            //
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(recallButton);
            buttonsPanel.Controls.Add(deleteButton);
            buttonsPanel.Dock = DockStyle.Bottom;
            buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonsPanel.Name = "buttonsPanel";
            buttonsPanel.Padding = new Padding(8, 6, 8, 6);
            buttonsPanel.Size = new Size(680, 64);
            buttonsPanel.TabIndex = 1;
            //
            // cancelButton
            //
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(120, 48);
            cancelButton.TabIndex = 2;
            cancelButton.Text = "Close";
            //
            // recallButton
            //
            recallButton.BackColor = Color.FromArgb(34, 139, 117);
            recallButton.FlatAppearance.BorderSize = 0;
            recallButton.FlatStyle = FlatStyle.Flat;
            recallButton.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            recallButton.ForeColor = Color.White;
            recallButton.Name = "recallButton";
            recallButton.Size = new Size(140, 48);
            recallButton.TabIndex = 0;
            recallButton.Text = "Recall";
            recallButton.UseVisualStyleBackColor = false;
            recallButton.Click += recallButton_Click;
            //
            // deleteButton
            //
            deleteButton.ForeColor = Color.Firebrick;
            deleteButton.Name = "deleteButton";
            deleteButton.Size = new Size(120, 48);
            deleteButton.TabIndex = 1;
            deleteButton.Text = "Delete";
            deleteButton.Click += deleteButton_Click;
            //
            // HeldSalesForm
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(680, 424);
            Controls.Add(heldGrid);
            Controls.Add(buttonsPanel);
            MinimizeBox = false;
            Name = "HeldSalesForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Held sales";
            Load += HeldSalesForm_Load;
            ((System.ComponentModel.ISupportInitialize)heldGrid).EndInit();
            buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private DataGridView heldGrid;
        private FlowLayoutPanel buttonsPanel;
        private Button cancelButton;
        private Button recallButton;
        private Button deleteButton;
    }
}
