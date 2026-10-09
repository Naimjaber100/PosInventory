namespace Inventory
{
    partial class CurrenciesForm
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
        /// The add / edit / rate dialogs are built in code (CurrenciesForm).
        /// </summary>
        private void InitializeComponent()
        {
            toolPanel = new FlowLayoutPanel();
            addButton = new Button();
            editButton = new Button();
            setRateButton = new Button();
            primaryButton = new Button();
            infoLabel = new Label();
            currenciesGrid = new DataGridView();
            ratesPanel = new Panel();
            ratesGrid = new DataGridView();
            ratesCaption = new Label();
            bottomPanel = new FlowLayoutPanel();
            closeButton = new Button();
            toolPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)currenciesGrid).BeginInit();
            ratesPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)ratesGrid).BeginInit();
            bottomPanel.SuspendLayout();
            SuspendLayout();
            //
            // toolPanel
            //
            toolPanel.Controls.Add(addButton);
            toolPanel.Controls.Add(editButton);
            toolPanel.Controls.Add(setRateButton);
            toolPanel.Controls.Add(primaryButton);
            toolPanel.Dock = DockStyle.Top;
            toolPanel.Name = "toolPanel";
            toolPanel.Padding = new Padding(8, 6, 8, 6);
            toolPanel.Size = new Size(900, 52);
            toolPanel.TabIndex = 0;
            toolPanel.WrapContents = false;
            //
            // addButton
            //
            addButton.Name = "addButton";
            addButton.Size = new Size(130, 36);
            addButton.TabIndex = 0;
            addButton.Text = "+ Add currency";
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
            // setRateButton
            //
            setRateButton.BackColor = Color.FromArgb(34, 139, 117);
            setRateButton.FlatAppearance.BorderSize = 0;
            setRateButton.FlatStyle = FlatStyle.Flat;
            setRateButton.ForeColor = Color.White;
            setRateButton.Margin = new Padding(20, 3, 3, 3);
            setRateButton.Name = "setRateButton";
            setRateButton.Size = new Size(140, 36);
            setRateButton.TabIndex = 2;
            setRateButton.Text = "Set exchange rate";
            setRateButton.UseVisualStyleBackColor = false;
            setRateButton.Click += setRateButton_Click;
            //
            // primaryButton
            //
            primaryButton.Margin = new Padding(20, 3, 3, 3);
            primaryButton.Name = "primaryButton";
            primaryButton.Size = new Size(140, 36);
            primaryButton.TabIndex = 3;
            primaryButton.Text = "Make primary";
            primaryButton.Click += primaryButton_Click;
            //
            // infoLabel
            //
            infoLabel.Dock = DockStyle.Top;
            infoLabel.ForeColor = Color.DimGray;
            infoLabel.Name = "infoLabel";
            infoLabel.Padding = new Padding(12, 0, 0, 0);
            infoLabel.Size = new Size(900, 28);
            infoLabel.TabIndex = 1;
            infoLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // currenciesGrid
            //
            currenciesGrid.AllowUserToAddRows = false;
            currenciesGrid.AllowUserToDeleteRows = false;
            currenciesGrid.AllowUserToResizeRows = false;
            currenciesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            currenciesGrid.BackgroundColor = Color.White;
            currenciesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            currenciesGrid.Dock = DockStyle.Fill;
            currenciesGrid.MultiSelect = false;
            currenciesGrid.Name = "currenciesGrid";
            currenciesGrid.ReadOnly = true;
            currenciesGrid.RowHeadersVisible = false;
            currenciesGrid.RowHeadersWidth = 51;
            currenciesGrid.RowTemplate.Height = 34;
            currenciesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            currenciesGrid.TabIndex = 2;
            currenciesGrid.CellDoubleClick += currenciesGrid_CellDoubleClick;
            currenciesGrid.CellFormatting += currenciesGrid_CellFormatting;
            currenciesGrid.DataBindingComplete += currenciesGrid_DataBindingComplete;
            currenciesGrid.SelectionChanged += currenciesGrid_SelectionChanged;
            //
            // ratesPanel
            //
            ratesPanel.Controls.Add(ratesGrid);
            ratesPanel.Controls.Add(ratesCaption);
            ratesPanel.Dock = DockStyle.Bottom;
            ratesPanel.Name = "ratesPanel";
            ratesPanel.Size = new Size(900, 230);
            ratesPanel.TabIndex = 3;
            //
            // ratesGrid
            //
            ratesGrid.AllowUserToAddRows = false;
            ratesGrid.AllowUserToDeleteRows = false;
            ratesGrid.AllowUserToResizeRows = false;
            ratesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            ratesGrid.BackgroundColor = Color.White;
            ratesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            ratesGrid.Dock = DockStyle.Fill;
            ratesGrid.Name = "ratesGrid";
            ratesGrid.ReadOnly = true;
            ratesGrid.RowHeadersVisible = false;
            ratesGrid.RowHeadersWidth = 51;
            ratesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            ratesGrid.TabIndex = 0;
            ratesGrid.TabStop = false;
            ratesGrid.DataBindingComplete += ratesGrid_DataBindingComplete;
            //
            // ratesCaption
            //
            ratesCaption.Dock = DockStyle.Top;
            ratesCaption.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            ratesCaption.Name = "ratesCaption";
            ratesCaption.Padding = new Padding(12, 0, 0, 0);
            ratesCaption.Size = new Size(900, 36);
            ratesCaption.TabIndex = 1;
            ratesCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // bottomPanel
            //
            bottomPanel.Controls.Add(closeButton);
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.FlowDirection = FlowDirection.RightToLeft;
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new Padding(8, 6, 8, 6);
            bottomPanel.Size = new Size(900, 52);
            bottomPanel.TabIndex = 4;
            //
            // closeButton
            //
            closeButton.DialogResult = DialogResult.Cancel;
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(100, 36);
            closeButton.TabIndex = 0;
            closeButton.Text = "Close";
            //
            // CurrenciesForm
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = closeButton;
            ClientSize = new Size(900, 600);
            Controls.Add(currenciesGrid);
            Controls.Add(infoLabel);
            Controls.Add(toolPanel);
            Controls.Add(ratesPanel);
            Controls.Add(bottomPanel);
            MinimizeBox = false;
            MinimumSize = new Size(760, 480);
            Name = "CurrenciesForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Currencies & exchange rates";
            FormClosed += CurrenciesForm_FormClosed;
            Load += CurrenciesForm_Load;
            toolPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)currenciesGrid).EndInit();
            ratesPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)ratesGrid).EndInit();
            bottomPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private FlowLayoutPanel toolPanel;
        private Button addButton;
        private Button editButton;
        private Button setRateButton;
        private Button primaryButton;
        private Label infoLabel;
        private DataGridView currenciesGrid;
        private Panel ratesPanel;
        private DataGridView ratesGrid;
        private Label ratesCaption;
        private FlowLayoutPanel bottomPanel;
        private Button closeButton;
    }
}
