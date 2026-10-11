namespace Inventory
{
    partial class AiPurchaseImportForm
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
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle headerStyle = new DataGridViewCellStyle();
            DataGridViewCellStyle numberStyle = new DataGridViewCellStyle();
            DataGridViewCellStyle priceStyle = new DataGridViewCellStyle();
            DataGridViewCellStyle costStyle = new DataGridViewCellStyle();
            DataGridViewCellStyle matchStyle = new DataGridViewCellStyle();
            topPanel = new Panel();
            statusLabel = new Label();
            fileLabel = new Label();
            readAgainButton = new Button();
            chooseButton = new Button();
            recordButton = new Button();
            recordTimer = new System.Windows.Forms.Timer(components);
            transcriptPanel = new Panel();
            transcriptBox = new TextBox();
            transcriptLabel = new Label();
            split = new SplitContainer();
            photoBox = new PictureBox();
            linesGrid = new DataGridView();
            useColumn = new DataGridViewCheckBoxColumn();
            readColumn = new DataGridViewTextBoxColumn();
            qtyColumn = new DataGridViewTextBoxColumn();
            priceColumn = new DataGridViewTextBoxColumn();
            itemColumn = new DataGridViewComboBoxColumn();
            matchColumn = new DataGridViewTextBoxColumn();
            costColumn = new DataGridViewTextBoxColumn();
            gridTools = new FlowLayoutPanel();
            findButton = new Button();
            currencyCaption = new Label();
            currencyCombo = new ComboBox();
            rateLabel = new Label();
            invoiceInfoLabel = new Label();
            bottomPanel = new Panel();
            summaryLabel = new Label();
            cancelButton = new Button();
            addButton = new Button();
            topPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)split).BeginInit();
            split.Panel1.SuspendLayout();
            split.Panel2.SuspendLayout();
            split.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)photoBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)linesGrid).BeginInit();
            gridTools.SuspendLayout();
            bottomPanel.SuspendLayout();
            SuspendLayout();
            //
            // topPanel
            //
            topPanel.BackColor = Color.FromArgb(22, 32, 43);
            topPanel.Controls.Add(statusLabel);
            topPanel.Controls.Add(fileLabel);
            topPanel.Controls.Add(readAgainButton);
            topPanel.Controls.Add(chooseButton);
            topPanel.Controls.Add(recordButton);
            topPanel.Dock = DockStyle.Top;
            topPanel.Name = "topPanel";
            topPanel.Padding = new Padding(12, 10, 12, 10);
            topPanel.Size = new Size(1280, 60);
            topPanel.TabIndex = 0;
            //
            // chooseButton
            //
            chooseButton.BackColor = Color.FromArgb(37, 99, 235);
            chooseButton.Dock = DockStyle.Left;
            chooseButton.FlatAppearance.BorderSize = 0;
            chooseButton.FlatStyle = FlatStyle.Flat;
            chooseButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            chooseButton.ForeColor = Color.White;
            chooseButton.Name = "chooseButton";
            chooseButton.Size = new Size(190, 40);
            chooseButton.TabIndex = 0;
            chooseButton.Text = "📷  Choose photo...";
            chooseButton.UseVisualStyleBackColor = false;
            chooseButton.Click += chooseButton_Click;
            //
            // recordButton
            //
            recordButton.BackColor = Color.FromArgb(37, 99, 235);
            recordButton.Dock = DockStyle.Left;
            recordButton.FlatAppearance.BorderSize = 0;
            recordButton.FlatStyle = FlatStyle.Flat;
            recordButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            recordButton.ForeColor = Color.White;
            recordButton.Margin = new Padding(0, 0, 8, 0);
            recordButton.Name = "recordButton";
            recordButton.Size = new Size(160, 40);
            recordButton.TabIndex = 0;
            recordButton.Text = "🎤  Record";
            recordButton.UseVisualStyleBackColor = false;
            recordButton.Visible = false;
            recordButton.Click += recordButton_Click;
            //
            // recordTimer
            //
            recordTimer.Interval = 500;
            recordTimer.Tick += recordTimer_Tick;
            //
            // transcriptPanel
            //
            transcriptPanel.Controls.Add(transcriptBox);
            transcriptPanel.Controls.Add(transcriptLabel);
            transcriptPanel.Dock = DockStyle.Fill;
            transcriptPanel.Name = "transcriptPanel";
            transcriptPanel.Padding = new Padding(10);
            transcriptPanel.TabIndex = 1;
            transcriptPanel.Visible = false;
            //
            // transcriptBox
            //
            transcriptBox.Dock = DockStyle.Fill;
            transcriptBox.Font = new Font("Segoe UI", 12F);
            transcriptBox.Multiline = true;
            transcriptBox.Name = "transcriptBox";
            transcriptBox.ScrollBars = ScrollBars.Vertical;
            transcriptBox.TabIndex = 1;
            //
            // transcriptLabel
            //
            transcriptLabel.Dock = DockStyle.Top;
            transcriptLabel.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            transcriptLabel.Name = "transcriptLabel";
            transcriptLabel.Size = new Size(400, 48);
            transcriptLabel.Text = "What was heard. Correct it if needed, then press " + (char)34 + "Read the text again" + (char)34 + ".";
            //
            // readAgainButton
            //
            readAgainButton.BackColor = Color.FromArgb(52, 64, 78);
            readAgainButton.Dock = DockStyle.Left;
            readAgainButton.Enabled = false;
            readAgainButton.FlatAppearance.BorderSize = 0;
            readAgainButton.FlatStyle = FlatStyle.Flat;
            readAgainButton.ForeColor = Color.White;
            readAgainButton.Name = "readAgainButton";
            readAgainButton.Size = new Size(110, 40);
            readAgainButton.TabIndex = 1;
            readAgainButton.Text = "Read again";
            readAgainButton.UseVisualStyleBackColor = false;
            readAgainButton.Click += readAgainButton_Click;
            //
            // fileLabel
            //
            fileLabel.AutoEllipsis = true;
            fileLabel.Dock = DockStyle.Left;
            fileLabel.ForeColor = Color.Gainsboro;
            fileLabel.Name = "fileLabel";
            fileLabel.Padding = new Padding(12, 0, 0, 0);
            fileLabel.Size = new Size(220, 40);
            fileLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // statusLabel
            //
            statusLabel.AutoEllipsis = true;
            statusLabel.BackColor = Color.White;
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            statusLabel.Name = "statusLabel";
            statusLabel.Padding = new Padding(10, 0, 0, 0);
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // split
            //
            split.Dock = DockStyle.Fill;
            split.Name = "split";
            split.Panel1.Controls.Add(photoBox);
            split.Panel1.Controls.Add(transcriptPanel);
            split.Panel2.Controls.Add(linesGrid);
            split.Panel2.Controls.Add(gridTools);
            split.Panel2.Controls.Add(invoiceInfoLabel);
            split.Panel2.Padding = new Padding(8, 0, 8, 0);
            split.Size = new Size(1280, 600);
            split.SplitterDistance = 420;
            split.TabIndex = 1;
            //
            // photoBox
            //
            photoBox.BackColor = Color.FromArgb(52, 64, 78);
            photoBox.Dock = DockStyle.Fill;
            photoBox.Name = "photoBox";
            photoBox.SizeMode = PictureBoxSizeMode.Zoom;
            photoBox.TabIndex = 0;
            photoBox.TabStop = false;
            //
            // invoiceInfoLabel
            //
            invoiceInfoLabel.AutoEllipsis = true;
            invoiceInfoLabel.Dock = DockStyle.Top;
            invoiceInfoLabel.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            invoiceInfoLabel.Name = "invoiceInfoLabel";
            invoiceInfoLabel.Size = new Size(840, 40);
            invoiceInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // gridTools
            //
            gridTools.Controls.Add(findButton);
            gridTools.Controls.Add(currencyCaption);
            gridTools.Controls.Add(currencyCombo);
            gridTools.Controls.Add(rateLabel);
            gridTools.Dock = DockStyle.Top;
            gridTools.Name = "gridTools";
            gridTools.Padding = new Padding(0, 2, 0, 4);
            gridTools.Size = new Size(840, 46);
            gridTools.TabIndex = 1;
            gridTools.WrapContents = false;
            //
            // findButton
            //
            findButton.Name = "findButton";
            findButton.Size = new Size(190, 36);
            findButton.TabIndex = 0;
            findButton.Text = "Find item for this line...";
            findButton.Click += findButton_Click;
            //
            // currencyCaption
            //
            currencyCaption.AutoSize = true;
            currencyCaption.Margin = new Padding(24, 9, 3, 0);
            currencyCaption.Name = "currencyCaption";
            currencyCaption.TabIndex = 1;
            currencyCaption.Text = "Prices on the invoice are in";
            //
            // currencyCombo
            //
            currencyCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            currencyCombo.Margin = new Padding(3, 5, 3, 3);
            currencyCombo.Name = "currencyCombo";
            currencyCombo.Size = new Size(80, 28);
            currencyCombo.TabIndex = 2;
            currencyCombo.SelectedIndexChanged += currencyCombo_SelectedIndexChanged;
            //
            // rateLabel
            //
            rateLabel.AutoSize = true;
            rateLabel.ForeColor = Color.DimGray;
            rateLabel.Margin = new Padding(6, 9, 3, 0);
            rateLabel.Name = "rateLabel";
            rateLabel.TabIndex = 3;
            //
            // linesGrid
            //
            linesGrid.AllowUserToAddRows = false;
            linesGrid.AllowUserToDeleteRows = false;
            linesGrid.AllowUserToResizeRows = false;
            linesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            linesGrid.BackgroundColor = Color.White;
            headerStyle.BackColor = Color.FromArgb(240, 242, 245);
            headerStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            headerStyle.WrapMode = DataGridViewTriState.True;
            linesGrid.ColumnHeadersDefaultCellStyle = headerStyle;
            linesGrid.ColumnHeadersHeight = 50;
            linesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            linesGrid.Columns.AddRange(new DataGridViewColumn[] { useColumn, readColumn, qtyColumn, priceColumn, itemColumn, matchColumn, costColumn });
            linesGrid.Dock = DockStyle.Fill;
            linesGrid.EditMode = DataGridViewEditMode.EditOnEnter;
            linesGrid.EnableHeadersVisualStyles = false;
            linesGrid.Font = new Font("Segoe UI", 10F);
            linesGrid.MultiSelect = false;
            linesGrid.Name = "linesGrid";
            linesGrid.RowHeadersVisible = false;
            linesGrid.RowHeadersWidth = 51;
            linesGrid.RowTemplate.Height = 34;
            linesGrid.TabIndex = 2;
            linesGrid.CellValueChanged += linesGrid_CellValueChanged;
            linesGrid.CurrentCellDirtyStateChanged += linesGrid_CurrentCellDirtyStateChanged;
            linesGrid.DataError += linesGrid_DataError;
            //
            // useColumn
            //
            useColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            useColumn.HeaderText = "Add";
            useColumn.Name = "useColumn";
            useColumn.Width = 46;
            //
            // readColumn
            //
            readColumn.FillWeight = 120F;
            readColumn.HeaderText = "Read on the invoice";
            readColumn.Name = "readColumn";
            readColumn.ReadOnly = true;
            //
            // qtyColumn
            //
            numberStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            numberStyle.BackColor = Color.FromArgb(255, 252, 230);
            numberStyle.Format = "#,0.###";
            qtyColumn.DefaultCellStyle = numberStyle;
            qtyColumn.FillWeight = 40F;
            qtyColumn.HeaderText = "Qty";
            qtyColumn.Name = "qtyColumn";
            qtyColumn.ValueType = typeof(decimal);
            //
            // priceColumn
            //
            priceStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            priceStyle.BackColor = Color.FromArgb(255, 252, 230);
            priceStyle.Format = "#,0.##";
            priceColumn.DefaultCellStyle = priceStyle;
            priceColumn.FillWeight = 55F;
            priceColumn.HeaderText = "Unit price (invoice)";
            priceColumn.Name = "priceColumn";
            priceColumn.ValueType = typeof(decimal);
            //
            // itemColumn
            //
            itemColumn.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
            itemColumn.FillWeight = 150F;
            itemColumn.FlatStyle = FlatStyle.Flat;
            itemColumn.HeaderText = "Item in the system";
            itemColumn.Name = "itemColumn";
            //
            // matchColumn
            //
            matchStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            matchStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            matchColumn.DefaultCellStyle = matchStyle;
            matchColumn.FillWeight = 48F;
            matchColumn.HeaderText = "Match";
            matchColumn.Name = "matchColumn";
            matchColumn.ReadOnly = true;
            //
            // costColumn
            //
            costStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            costStyle.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            costStyle.Format = "#,0.##";
            costColumn.DefaultCellStyle = costStyle;
            costColumn.FillWeight = 55F;
            costColumn.HeaderText = "Unit cost";
            costColumn.Name = "costColumn";
            costColumn.ReadOnly = true;
            //
            // bottomPanel
            //
            bottomPanel.BackColor = Color.White;
            bottomPanel.Controls.Add(summaryLabel);
            bottomPanel.Controls.Add(cancelButton);
            bottomPanel.Controls.Add(addButton);
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new Padding(12, 10, 12, 10);
            bottomPanel.Size = new Size(1280, 62);
            bottomPanel.TabIndex = 2;
            //
            // summaryLabel
            //
            summaryLabel.Dock = DockStyle.Fill;
            summaryLabel.Name = "summaryLabel";
            summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // cancelButton
            //
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Dock = DockStyle.Right;
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(110, 42);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Cancel";
            //
            // addButton
            //
            addButton.BackColor = Color.FromArgb(34, 139, 117);
            addButton.Dock = DockStyle.Right;
            addButton.Enabled = false;
            addButton.FlatAppearance.BorderSize = 0;
            addButton.FlatStyle = FlatStyle.Flat;
            addButton.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            addButton.ForeColor = Color.White;
            addButton.Name = "addButton";
            addButton.Size = new Size(250, 42);
            addButton.TabIndex = 0;
            addButton.Text = "Add to the purchase";
            addButton.UseVisualStyleBackColor = false;
            addButton.Click += addButton_Click;
            //
            // AiPurchaseImportForm
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            CancelButton = cancelButton;
            ClientSize = new Size(1280, 722);
            Controls.Add(split);
            Controls.Add(topPanel);
            Controls.Add(bottomPanel);
            MinimizeBox = false;
            MinimumSize = new Size(1000, 560);
            Name = "AiPurchaseImportForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Read purchase invoice (AI)";
            FormClosing += AiPurchaseImportForm_FormClosing;
            Load += AiPurchaseImportForm_Load;
            Shown += AiPurchaseImportForm_Shown;
            topPanel.ResumeLayout(false);
            split.Panel1.ResumeLayout(false);
            split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)split).EndInit();
            split.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)photoBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)linesGrid).EndInit();
            gridTools.ResumeLayout(false);
            gridTools.PerformLayout();
            bottomPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel topPanel;
        private Label statusLabel;
        private Label fileLabel;
        private Button readAgainButton;
        private Button chooseButton;
        private Button recordButton;
        private System.Windows.Forms.Timer recordTimer;
        private Panel transcriptPanel;
        private TextBox transcriptBox;
        private Label transcriptLabel;
        private SplitContainer split;
        private PictureBox photoBox;
        private DataGridView linesGrid;
        private DataGridViewCheckBoxColumn useColumn;
        private DataGridViewTextBoxColumn readColumn;
        private DataGridViewTextBoxColumn qtyColumn;
        private DataGridViewTextBoxColumn priceColumn;
        private DataGridViewComboBoxColumn itemColumn;
        private DataGridViewTextBoxColumn matchColumn;
        private DataGridViewTextBoxColumn costColumn;
        private FlowLayoutPanel gridTools;
        private Button findButton;
        private Label currencyCaption;
        private ComboBox currencyCombo;
        private Label rateLabel;
        private Label invoiceInfoLabel;
        private Panel bottomPanel;
        private Label summaryLabel;
        private Button cancelButton;
        private Button addButton;
    }
}
