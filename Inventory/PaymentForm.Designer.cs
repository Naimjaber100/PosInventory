namespace Inventory
{
    partial class PaymentForm
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
        /// The payment rows (one per currency, and card) are created in code (PaymentForm.AddRow).
        /// </summary>
        private void InitializeComponent()
        {
            headerPanel = new Panel();
            totalCaption = new Label();
            totalValue = new Label();
            totalSecondValue = new Label();
            rateValue = new Label();
            rowsTable = new TableLayoutPanel();
            footerPanel = new Panel();
            paidCaption = new Label();
            paidValue = new Label();
            changeCaption = new Label();
            changeValue = new Label();
            changeSecondValue = new Label();
            clearButton = new Button();
            cancelButton = new Button();
            payButton = new Button();
            headerPanel.SuspendLayout();
            footerPanel.SuspendLayout();
            SuspendLayout();
            //
            // headerPanel
            //
            headerPanel.Controls.Add(totalCaption);
            headerPanel.Controls.Add(totalValue);
            headerPanel.Controls.Add(totalSecondValue);
            headerPanel.Controls.Add(rateValue);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Name = "headerPanel";
            headerPanel.Size = new Size(616, 104);
            headerPanel.TabIndex = 0;
            //
            // totalCaption
            //
            totalCaption.AutoSize = true;
            totalCaption.Font = new Font("Segoe UI", 14F);
            totalCaption.Location = new Point(24, 22);
            totalCaption.Name = "totalCaption";
            totalCaption.Text = "Total";
            //
            // totalValue
            //
            totalValue.Font = new Font("Segoe UI", 26F, FontStyle.Bold);
            totalValue.Location = new Point(150, 10);
            totalValue.Name = "totalValue";
            totalValue.Size = new Size(440, 50);
            totalValue.Text = "0";
            totalValue.TextAlign = ContentAlignment.MiddleRight;
            //
            // totalSecondValue
            //
            totalSecondValue.Font = new Font("Segoe UI", 16F);
            totalSecondValue.ForeColor = Color.DimGray;
            totalSecondValue.Location = new Point(300, 60);
            totalSecondValue.Name = "totalSecondValue";
            totalSecondValue.Size = new Size(290, 34);
            totalSecondValue.TextAlign = ContentAlignment.MiddleRight;
            //
            // rateValue
            //
            rateValue.Font = new Font("Segoe UI", 8.5F);
            rateValue.ForeColor = Color.DimGray;
            rateValue.Location = new Point(26, 56);
            rateValue.Name = "rateValue";
            rateValue.Size = new Size(270, 46);
            //
            // rowsTable
            //
            rowsTable.ColumnCount = 3;
            rowsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            rowsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rowsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 156F));
            rowsTable.Dock = DockStyle.Fill;
            rowsTable.Name = "rowsTable";
            rowsTable.Padding = new Padding(22, 6, 26, 6);
            rowsTable.RowCount = 0;
            rowsTable.TabIndex = 1;
            //
            // footerPanel
            //
            footerPanel.Controls.Add(paidCaption);
            footerPanel.Controls.Add(paidValue);
            footerPanel.Controls.Add(changeCaption);
            footerPanel.Controls.Add(changeValue);
            footerPanel.Controls.Add(changeSecondValue);
            footerPanel.Controls.Add(clearButton);
            footerPanel.Controls.Add(cancelButton);
            footerPanel.Controls.Add(payButton);
            footerPanel.Dock = DockStyle.Bottom;
            footerPanel.Name = "footerPanel";
            footerPanel.Size = new Size(616, 220);
            footerPanel.TabIndex = 2;
            //
            // paidCaption
            //
            paidCaption.AutoSize = true;
            paidCaption.Font = new Font("Segoe UI", 13F);
            paidCaption.Location = new Point(24, 14);
            paidCaption.Name = "paidCaption";
            paidCaption.Text = "Paid";
            //
            // paidValue
            //
            paidValue.Font = new Font("Segoe UI", 16F);
            paidValue.Location = new Point(150, 6);
            paidValue.Name = "paidValue";
            paidValue.Size = new Size(440, 40);
            paidValue.Text = "0";
            paidValue.TextAlign = ContentAlignment.MiddleRight;
            //
            // changeCaption
            //
            changeCaption.AutoSize = true;
            changeCaption.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            changeCaption.Location = new Point(24, 64);
            changeCaption.Name = "changeCaption";
            changeCaption.Text = "Remaining";
            //
            // changeValue
            //
            changeValue.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            changeValue.ForeColor = Color.Firebrick;
            changeValue.Location = new Point(150, 52);
            changeValue.Name = "changeValue";
            changeValue.Size = new Size(440, 50);
            changeValue.Text = "0";
            changeValue.TextAlign = ContentAlignment.MiddleRight;
            //
            // changeSecondValue
            //
            changeSecondValue.Font = new Font("Segoe UI", 12F);
            changeSecondValue.ForeColor = Color.DimGray;
            changeSecondValue.Location = new Point(150, 102);
            changeSecondValue.Name = "changeSecondValue";
            changeSecondValue.Size = new Size(440, 28);
            changeSecondValue.TextAlign = ContentAlignment.MiddleRight;
            //
            // clearButton
            //
            clearButton.Location = new Point(24, 146);
            clearButton.Name = "clearButton";
            clearButton.Size = new Size(120, 56);
            clearButton.TabIndex = 102;
            clearButton.TabStop = false;
            clearButton.Text = "Clear";
            clearButton.Click += clearButton_Click;
            //
            // cancelButton
            //
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(270, 146);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(140, 56);
            cancelButton.TabIndex = 101;
            cancelButton.Text = "Back (Esc)";
            //
            // payButton
            //
            payButton.BackColor = Color.FromArgb(34, 139, 117);
            payButton.FlatAppearance.BorderSize = 0;
            payButton.FlatStyle = FlatStyle.Flat;
            payButton.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            payButton.ForeColor = Color.White;
            payButton.Location = new Point(420, 146);
            payButton.Name = "payButton";
            payButton.Size = new Size(170, 56);
            payButton.TabIndex = 100;
            payButton.Text = "PAY (Enter)";
            payButton.UseVisualStyleBackColor = false;
            payButton.Click += payButton_Click;
            //
            // PaymentForm
            //
            AcceptButton = payButton;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(616, 528);
            Controls.Add(rowsTable);
            Controls.Add(headerPanel);
            Controls.Add(footerPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PaymentForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Payment";
            Shown += PaymentForm_Shown;
            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            footerPanel.ResumeLayout(false);
            footerPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel headerPanel;
        private Label totalCaption;
        private Label totalValue;
        private Label totalSecondValue;
        private Label rateValue;
        private TableLayoutPanel rowsTable;
        private Panel footerPanel;
        private Label paidCaption;
        private Label paidValue;
        private Label changeCaption;
        private Label changeValue;
        private Label changeSecondValue;
        private Button clearButton;
        private Button cancelButton;
        private Button payButton;
    }
}
