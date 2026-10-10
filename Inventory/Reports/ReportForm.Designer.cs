namespace Inventory.Reports
{
    partial class ReportForm
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
        /// The total cards are created in code (ReportForm.ShowCards).
        /// </summary>
        private void InitializeComponent()
        {
            DataGridViewCellStyle headerStyle = new DataGridViewCellStyle();
            headerPanel = new Panel();
            headerButtons = new FlowLayoutPanel();
            descriptionLabel = new Label();
            titleLabel = new Label();
            filterPanel = new FlowLayoutPanel();
            todayButton = new Button();
            yesterdayButton = new Button();
            weekButton = new Button();
            monthButton = new Button();
            lastMonthButton = new Button();
            yearButton = new Button();
            fromCaption = new Label();
            fromPicker = new DateTimePicker();
            toCaption = new Label();
            toPicker = new DateTimePicker();
            userCaption = new Label();
            userCombo = new ComboBox();
            refreshButton = new Button();
            excelButton = new Button();
            cardsPanel = new FlowLayoutPanel();
            filterLabel = new Label();
            grid = new DataGridView();
            footerPanel = new Panel();
            hintLabel = new Label();
            timeLabel = new Label();
            headerPanel.SuspendLayout();
            filterPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
            footerPanel.SuspendLayout();
            SuspendLayout();
            //
            // headerPanel
            //
            headerPanel.BackColor = Color.FromArgb(37, 99, 235);
            headerPanel.Controls.Add(descriptionLabel);
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(headerButtons);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Name = "headerPanel";
            headerPanel.Padding = new Padding(20, 8, 20, 6);
            headerPanel.Size = new Size(1280, 78);
            headerPanel.TabIndex = 0;
            //
            // headerButtons
            //
            headerButtons.Controls.Add(excelButton);
            headerButtons.Controls.Add(refreshButton);
            headerButtons.Dock = DockStyle.Right;
            headerButtons.FlowDirection = FlowDirection.RightToLeft;
            headerButtons.Name = "headerButtons";
            headerButtons.Padding = new Padding(0, 14, 0, 0);
            headerButtons.Size = new Size(240, 64);
            headerButtons.TabIndex = 2;
            headerButtons.WrapContents = false;
            //
            // descriptionLabel
            //
            descriptionLabel.Dock = DockStyle.Top;
            descriptionLabel.Font = new Font("Segoe UI", 10F);
            descriptionLabel.ForeColor = Color.FromArgb(220, 230, 250);
            descriptionLabel.Name = "descriptionLabel";
            descriptionLabel.Size = new Size(1240, 24);
            //
            // titleLabel
            //
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(1240, 40);
            //
            // filterPanel
            //
            filterPanel.BackColor = Color.White;
            filterPanel.Controls.Add(todayButton);
            filterPanel.Controls.Add(yesterdayButton);
            filterPanel.Controls.Add(weekButton);
            filterPanel.Controls.Add(monthButton);
            filterPanel.Controls.Add(lastMonthButton);
            filterPanel.Controls.Add(yearButton);
            filterPanel.Controls.Add(fromCaption);
            filterPanel.Controls.Add(fromPicker);
            filterPanel.Controls.Add(toCaption);
            filterPanel.Controls.Add(toPicker);
            filterPanel.Controls.Add(userCaption);
            filterPanel.Controls.Add(userCombo);
            filterPanel.Dock = DockStyle.Top;
            filterPanel.Name = "filterPanel";
            filterPanel.Padding = new Padding(14, 9, 10, 4);
            filterPanel.Size = new Size(1280, 58);
            filterPanel.TabIndex = 1;
            filterPanel.WrapContents = false;
            //
            // todayButton
            //
            todayButton.Margin = new Padding(0, 0, 4, 0);
            todayButton.Name = "todayButton";
            todayButton.Size = new Size(70, 36);
            todayButton.TabIndex = 0;
            todayButton.Text = "Today";
            todayButton.Click += todayButton_Click;
            //
            // yesterdayButton
            //
            yesterdayButton.Margin = new Padding(0, 0, 4, 0);
            yesterdayButton.Name = "yesterdayButton";
            yesterdayButton.Size = new Size(88, 36);
            yesterdayButton.TabIndex = 1;
            yesterdayButton.Text = "Yesterday";
            yesterdayButton.Click += yesterdayButton_Click;
            //
            // weekButton
            //
            weekButton.Margin = new Padding(0, 0, 4, 0);
            weekButton.Name = "weekButton";
            weekButton.Size = new Size(88, 36);
            weekButton.TabIndex = 2;
            weekButton.Text = "This week";
            weekButton.Click += weekButton_Click;
            //
            // monthButton
            //
            monthButton.Margin = new Padding(0, 0, 4, 0);
            monthButton.Name = "monthButton";
            monthButton.Size = new Size(96, 36);
            monthButton.TabIndex = 3;
            monthButton.Text = "This month";
            monthButton.Click += monthButton_Click;
            //
            // lastMonthButton
            //
            lastMonthButton.Margin = new Padding(0, 0, 4, 0);
            lastMonthButton.Name = "lastMonthButton";
            lastMonthButton.Size = new Size(96, 36);
            lastMonthButton.TabIndex = 4;
            lastMonthButton.Text = "Last month";
            lastMonthButton.Click += lastMonthButton_Click;
            //
            // yearButton
            //
            yearButton.Margin = new Padding(0);
            yearButton.Name = "yearButton";
            yearButton.Size = new Size(84, 36);
            yearButton.TabIndex = 5;
            yearButton.Text = "This year";
            yearButton.Click += yearButton_Click;
            //
            // fromCaption
            //
            fromCaption.AutoSize = true;
            fromCaption.Margin = new Padding(18, 9, 3, 0);
            fromCaption.Name = "fromCaption";
            fromCaption.TabIndex = 6;
            fromCaption.Text = "From";
            //
            // fromPicker
            //
            fromPicker.Format = DateTimePickerFormat.Short;
            fromPicker.Margin = new Padding(3, 5, 3, 3);
            fromPicker.Name = "fromPicker";
            fromPicker.Size = new Size(122, 27);
            fromPicker.TabIndex = 7;
            fromPicker.ValueChanged += period_ValueChanged;
            //
            // toCaption
            //
            toCaption.AutoSize = true;
            toCaption.Margin = new Padding(8, 9, 3, 0);
            toCaption.Name = "toCaption";
            toCaption.TabIndex = 8;
            toCaption.Text = "To";
            //
            // toPicker
            //
            toPicker.Format = DateTimePickerFormat.Short;
            toPicker.Margin = new Padding(3, 5, 3, 3);
            toPicker.Name = "toPicker";
            toPicker.Size = new Size(122, 27);
            toPicker.TabIndex = 9;
            toPicker.ValueChanged += period_ValueChanged;
            //
            // userCaption
            //
            userCaption.AutoSize = true;
            userCaption.Margin = new Padding(18, 9, 3, 0);
            userCaption.Name = "userCaption";
            userCaption.TabIndex = 10;
            userCaption.Text = "Cashier";
            //
            // userCombo
            //
            userCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            userCombo.Margin = new Padding(3, 5, 3, 3);
            userCombo.Name = "userCombo";
            userCombo.Size = new Size(170, 28);
            userCombo.TabIndex = 11;
            userCombo.SelectedIndexChanged += userCombo_SelectedIndexChanged;
            //
            // refreshButton
            //
            refreshButton.BackColor = Color.White;
            refreshButton.FlatAppearance.BorderSize = 0;
            refreshButton.FlatStyle = FlatStyle.Flat;
            refreshButton.Margin = new Padding(0, 0, 8, 0);
            refreshButton.UseVisualStyleBackColor = false;
            refreshButton.Name = "refreshButton";
            refreshButton.Size = new Size(84, 36);
            refreshButton.TabIndex = 12;
            refreshButton.Text = "Refresh";
            refreshButton.Click += refreshButton_Click;
            //
            // excelButton
            //
            excelButton.BackColor = Color.FromArgb(33, 115, 70);
            excelButton.FlatAppearance.BorderSize = 0;
            excelButton.FlatStyle = FlatStyle.Flat;
            excelButton.ForeColor = Color.White;
            excelButton.Margin = new Padding(0, 0, 8, 0);
            excelButton.Name = "excelButton";
            excelButton.Size = new Size(110, 36);
            excelButton.TabIndex = 13;
            excelButton.Text = "Excel";
            excelButton.UseVisualStyleBackColor = false;
            excelButton.Click += excelButton_Click;
            //
            // cardsPanel
            //
            cardsPanel.Dock = DockStyle.Top;
            cardsPanel.Name = "cardsPanel";
            cardsPanel.Padding = new Padding(14, 12, 14, 4);
            cardsPanel.Size = new Size(1280, 110);
            cardsPanel.TabIndex = 2;
            cardsPanel.WrapContents = false;
            //
            // filterLabel
            //
            filterLabel.Dock = DockStyle.Top;
            filterLabel.ForeColor = Color.DimGray;
            filterLabel.Name = "filterLabel";
            filterLabel.Padding = new Padding(16, 0, 0, 0);
            filterLabel.Size = new Size(1280, 28);
            filterLabel.TabIndex = 3;
            filterLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // grid
            //
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            headerStyle.BackColor = Color.FromArgb(240, 242, 245);
            headerStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            headerStyle.ForeColor = Color.FromArgb(22, 32, 43);
            headerStyle.SelectionBackColor = Color.FromArgb(240, 242, 245);
            headerStyle.SelectionForeColor = Color.FromArgb(22, 32, 43);
            headerStyle.WrapMode = DataGridViewTriState.True;
            grid.ColumnHeadersDefaultCellStyle = headerStyle;
            grid.ColumnHeadersHeight = 40;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(222, 233, 253);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(22, 32, 43);
            grid.Dock = DockStyle.Fill;
            grid.EnableHeadersVisualStyles = false;
            grid.Font = new Font("Segoe UI", 10F);
            grid.GridColor = Color.FromArgb(232, 235, 238);
            grid.Name = "grid";
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.RowHeadersWidth = 51;
            grid.RowTemplate.Height = 32;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.TabIndex = 4;
            grid.CellDoubleClick += grid_CellDoubleClick;
            grid.CellFormatting += grid_CellFormatting;
            grid.DataBindingComplete += grid_DataBindingComplete;
            //
            // footerPanel
            //
            footerPanel.BackColor = Color.White;
            footerPanel.Controls.Add(hintLabel);
            footerPanel.Controls.Add(timeLabel);
            footerPanel.Dock = DockStyle.Bottom;
            footerPanel.Name = "footerPanel";
            footerPanel.Padding = new Padding(16, 0, 16, 0);
            footerPanel.Size = new Size(1280, 34);
            footerPanel.TabIndex = 5;
            //
            // hintLabel
            //
            hintLabel.Dock = DockStyle.Fill;
            hintLabel.ForeColor = Color.DimGray;
            hintLabel.Name = "hintLabel";
            hintLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // timeLabel
            //
            timeLabel.Dock = DockStyle.Right;
            timeLabel.ForeColor = Color.DarkGray;
            timeLabel.Name = "timeLabel";
            timeLabel.Size = new Size(260, 34);
            timeLabel.TextAlign = ContentAlignment.MiddleRight;
            //
            // ReportForm
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(238, 241, 244);
            ClientSize = new Size(1280, 780);
            Controls.Add(grid);
            Controls.Add(filterLabel);
            Controls.Add(cardsPanel);
            Controls.Add(filterPanel);
            Controls.Add(headerPanel);
            Controls.Add(footerPanel);
            KeyPreview = true;
            MinimumSize = new Size(1100, 600);
            Name = "ReportForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Report";
            WindowState = FormWindowState.Maximized;
            Load += ReportForm_Load;
            headerPanel.ResumeLayout(false);
            filterPanel.ResumeLayout(false);
            filterPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)grid).EndInit();
            footerPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel headerPanel;
        private FlowLayoutPanel headerButtons;
        private Label descriptionLabel;
        private Label titleLabel;
        private FlowLayoutPanel filterPanel;
        private Button todayButton;
        private Button yesterdayButton;
        private Button weekButton;
        private Button monthButton;
        private Button lastMonthButton;
        private Button yearButton;
        private Label fromCaption;
        private DateTimePicker fromPicker;
        private Label toCaption;
        private DateTimePicker toPicker;
        private Label userCaption;
        private ComboBox userCombo;
        private Button refreshButton;
        private Button excelButton;
        private FlowLayoutPanel cardsPanel;
        private Label filterLabel;
        private DataGridView grid;
        private Panel footerPanel;
        private Label hintLabel;
        private Label timeLabel;
    }
}
