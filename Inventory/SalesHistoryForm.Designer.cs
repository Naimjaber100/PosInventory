namespace Inventory
{
    partial class SalesHistoryForm
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
        /// The total cards are created in code (SalesHistoryForm.BuildCards).
        /// </summary>
        private void InitializeComponent()
        {
            filterPanel = new FlowLayoutPanel();
            todayButton = new Button();
            yesterdayButton = new Button();
            monthButton = new Button();
            fromCaption = new Label();
            fromPicker = new DateTimePicker();
            toCaption = new Label();
            toPicker = new DateTimePicker();
            userCaption = new Label();
            userCombo = new ComboBox();
            refreshButton = new Button();
            cardsPanel = new FlowLayoutPanel();
            summaryLabel = new Label();
            invoicesGrid = new DataGridView();
            linesPanel = new Panel();
            linesGrid = new DataGridView();
            linesCaption = new Label();
            bottomPanel = new FlowLayoutPanel();
            closeButton = new Button();
            filterPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)invoicesGrid).BeginInit();
            linesPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)linesGrid).BeginInit();
            bottomPanel.SuspendLayout();
            SuspendLayout();
            //
            // filterPanel
            //
            filterPanel.Controls.Add(todayButton);
            filterPanel.Controls.Add(yesterdayButton);
            filterPanel.Controls.Add(monthButton);
            filterPanel.Controls.Add(fromCaption);
            filterPanel.Controls.Add(fromPicker);
            filterPanel.Controls.Add(toCaption);
            filterPanel.Controls.Add(toPicker);
            filterPanel.Controls.Add(userCaption);
            filterPanel.Controls.Add(userCombo);
            filterPanel.Controls.Add(refreshButton);
            filterPanel.Dock = DockStyle.Top;
            filterPanel.Name = "filterPanel";
            filterPanel.Padding = new Padding(10, 8, 10, 4);
            filterPanel.Size = new Size(1180, 56);
            filterPanel.TabIndex = 0;
            filterPanel.WrapContents = false;
            //
            // todayButton
            //
            todayButton.Name = "todayButton";
            todayButton.Size = new Size(84, 38);
            todayButton.TabIndex = 0;
            todayButton.Text = "Today";
            todayButton.Click += todayButton_Click;
            //
            // yesterdayButton
            //
            yesterdayButton.Name = "yesterdayButton";
            yesterdayButton.Size = new Size(96, 38);
            yesterdayButton.TabIndex = 1;
            yesterdayButton.Text = "Yesterday";
            yesterdayButton.Click += yesterdayButton_Click;
            //
            // monthButton
            //
            monthButton.Name = "monthButton";
            monthButton.Size = new Size(104, 38);
            monthButton.TabIndex = 2;
            monthButton.Text = "This month";
            monthButton.Click += monthButton_Click;
            //
            // fromCaption
            //
            fromCaption.AutoSize = true;
            fromCaption.Margin = new Padding(20, 11, 3, 0);
            fromCaption.Name = "fromCaption";
            fromCaption.TabIndex = 3;
            fromCaption.Text = "From";
            //
            // fromPicker
            //
            fromPicker.Format = DateTimePickerFormat.Short;
            fromPicker.Margin = new Padding(3, 7, 3, 3);
            fromPicker.Name = "fromPicker";
            fromPicker.Size = new Size(130, 27);
            fromPicker.TabIndex = 4;
            fromPicker.ValueChanged += period_ValueChanged;
            //
            // toCaption
            //
            toCaption.AutoSize = true;
            toCaption.Margin = new Padding(10, 11, 3, 0);
            toCaption.Name = "toCaption";
            toCaption.TabIndex = 5;
            toCaption.Text = "To";
            //
            // toPicker
            //
            toPicker.Format = DateTimePickerFormat.Short;
            toPicker.Margin = new Padding(3, 7, 3, 3);
            toPicker.Name = "toPicker";
            toPicker.Size = new Size(130, 27);
            toPicker.TabIndex = 6;
            toPicker.ValueChanged += period_ValueChanged;
            //
            // userCaption
            //
            userCaption.AutoSize = true;
            userCaption.Margin = new Padding(20, 11, 3, 0);
            userCaption.Name = "userCaption";
            userCaption.TabIndex = 7;
            userCaption.Text = "Cashier";
            //
            // userCombo
            //
            userCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            userCombo.Margin = new Padding(3, 7, 3, 3);
            userCombo.Name = "userCombo";
            userCombo.Size = new Size(200, 28);
            userCombo.TabIndex = 8;
            userCombo.SelectedIndexChanged += userCombo_SelectedIndexChanged;
            //
            // refreshButton
            //
            refreshButton.Margin = new Padding(20, 3, 3, 3);
            refreshButton.Name = "refreshButton";
            refreshButton.Size = new Size(96, 38);
            refreshButton.TabIndex = 9;
            refreshButton.Text = "Refresh";
            refreshButton.Click += refreshButton_Click;
            //
            // cardsPanel
            //
            cardsPanel.Dock = DockStyle.Top;
            cardsPanel.Name = "cardsPanel";
            cardsPanel.Padding = new Padding(12, 8, 12, 4);
            cardsPanel.Size = new Size(1180, 96);
            cardsPanel.TabIndex = 1;
            cardsPanel.WrapContents = false;
            //
            // summaryLabel
            //
            summaryLabel.Dock = DockStyle.Top;
            summaryLabel.ForeColor = Color.DimGray;
            summaryLabel.Name = "summaryLabel";
            summaryLabel.Padding = new Padding(12, 0, 0, 0);
            summaryLabel.Size = new Size(1180, 28);
            summaryLabel.TabIndex = 2;
            summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // invoicesGrid
            //
            invoicesGrid.AllowUserToAddRows = false;
            invoicesGrid.AllowUserToDeleteRows = false;
            invoicesGrid.AllowUserToResizeRows = false;
            invoicesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            invoicesGrid.BackgroundColor = Color.White;
            invoicesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            invoicesGrid.Dock = DockStyle.Fill;
            invoicesGrid.Font = new Font("Segoe UI", 10F);
            invoicesGrid.MultiSelect = false;
            invoicesGrid.Name = "invoicesGrid";
            invoicesGrid.ReadOnly = true;
            invoicesGrid.RowHeadersVisible = false;
            invoicesGrid.RowHeadersWidth = 51;
            invoicesGrid.RowTemplate.Height = 34;
            invoicesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            invoicesGrid.TabIndex = 3;
            invoicesGrid.CellFormatting += invoicesGrid_CellFormatting;
            invoicesGrid.DataBindingComplete += invoicesGrid_DataBindingComplete;
            invoicesGrid.SelectionChanged += invoicesGrid_SelectionChanged;
            //
            // linesPanel
            //
            linesPanel.Controls.Add(linesGrid);
            linesPanel.Controls.Add(linesCaption);
            linesPanel.Dock = DockStyle.Bottom;
            linesPanel.Name = "linesPanel";
            linesPanel.Size = new Size(1180, 250);
            linesPanel.TabIndex = 4;
            //
            // linesGrid
            //
            linesGrid.AllowUserToAddRows = false;
            linesGrid.AllowUserToDeleteRows = false;
            linesGrid.AllowUserToResizeRows = false;
            linesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            linesGrid.BackgroundColor = Color.White;
            linesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            linesGrid.Dock = DockStyle.Fill;
            linesGrid.Font = new Font("Segoe UI", 10F);
            linesGrid.Name = "linesGrid";
            linesGrid.ReadOnly = true;
            linesGrid.RowHeadersVisible = false;
            linesGrid.RowHeadersWidth = 51;
            linesGrid.RowTemplate.Height = 30;
            linesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            linesGrid.TabIndex = 0;
            linesGrid.TabStop = false;
            linesGrid.CellFormatting += linesGrid_CellFormatting;
            linesGrid.DataBindingComplete += linesGrid_DataBindingComplete;
            //
            // linesCaption
            //
            linesCaption.AutoEllipsis = true;
            linesCaption.Dock = DockStyle.Top;
            linesCaption.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            linesCaption.Name = "linesCaption";
            linesCaption.Padding = new Padding(12, 0, 0, 0);
            linesCaption.Size = new Size(1180, 38);
            linesCaption.TabIndex = 1;
            linesCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // bottomPanel
            //
            bottomPanel.Controls.Add(closeButton);
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.FlowDirection = FlowDirection.RightToLeft;
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new Padding(8, 6, 8, 6);
            bottomPanel.Size = new Size(1180, 56);
            bottomPanel.TabIndex = 5;
            //
            // closeButton
            //
            closeButton.DialogResult = DialogResult.Cancel;
            closeButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(120, 42);
            closeButton.TabIndex = 0;
            closeButton.Text = "Close (Esc)";
            //
            // SalesHistoryForm
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(238, 241, 244);
            CancelButton = closeButton;
            ClientSize = new Size(1180, 760);
            Controls.Add(invoicesGrid);
            Controls.Add(summaryLabel);
            Controls.Add(cardsPanel);
            Controls.Add(filterPanel);
            Controls.Add(linesPanel);
            Controls.Add(bottomPanel);
            MinimizeBox = false;
            MinimumSize = new Size(1000, 600);
            Name = "SalesHistoryForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "My sales";
            Load += SalesHistoryForm_Load;
            filterPanel.ResumeLayout(false);
            filterPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)invoicesGrid).EndInit();
            linesPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)linesGrid).EndInit();
            bottomPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private FlowLayoutPanel filterPanel;
        private Button todayButton;
        private Button yesterdayButton;
        private Button monthButton;
        private Label fromCaption;
        private DateTimePicker fromPicker;
        private Label toCaption;
        private DateTimePicker toPicker;
        private Label userCaption;
        private ComboBox userCombo;
        private Button refreshButton;
        private FlowLayoutPanel cardsPanel;
        private Label summaryLabel;
        private DataGridView invoicesGrid;
        private Panel linesPanel;
        private DataGridView linesGrid;
        private Label linesCaption;
        private FlowLayoutPanel bottomPanel;
        private Button closeButton;
    }
}
