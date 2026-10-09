namespace Inventory
{
    partial class QuickGroupEditForm
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
            fieldsTable = new TableLayoutPanel();
            nameCaption = new Label();
            nameText = new TextBox();
            colorCaption = new Label();
            colorPicker = new ColorSwatchPicker();
            activeCheck = new CheckBox();
            previewCaption = new Label();
            previewPanel = new FlowLayoutPanel();
            previewButton = new Button();
            previewOpenButton = new Button();
            buttonsPanel = new FlowLayoutPanel();
            cancelButton = new Button();
            saveButton = new Button();
            fieldsTable.SuspendLayout();
            previewPanel.SuspendLayout();
            buttonsPanel.SuspendLayout();
            SuspendLayout();
            //
            // fieldsTable
            //
            fieldsTable.ColumnCount = 2;
            fieldsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            fieldsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            fieldsTable.Controls.Add(nameCaption, 0, 0);
            fieldsTable.Controls.Add(nameText, 1, 0);
            fieldsTable.Controls.Add(colorCaption, 0, 1);
            fieldsTable.Controls.Add(colorPicker, 1, 1);
            fieldsTable.Controls.Add(activeCheck, 1, 2);
            fieldsTable.Controls.Add(previewCaption, 0, 3);
            fieldsTable.Controls.Add(previewPanel, 1, 3);
            fieldsTable.Dock = DockStyle.Fill;
            fieldsTable.Location = new Point(0, 0);
            fieldsTable.Name = "fieldsTable";
            fieldsTable.Padding = new Padding(12);
            fieldsTable.RowCount = 4;
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            fieldsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            fieldsTable.Size = new Size(640, 230);
            fieldsTable.TabIndex = 0;
            //
            // nameCaption
            //
            nameCaption.Dock = DockStyle.Fill;
            nameCaption.Name = "nameCaption";
            nameCaption.TabIndex = 0;
            nameCaption.Text = "Name";
            nameCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // nameText
            //
            nameText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            nameText.MaxLength = 40;
            nameText.Name = "nameText";
            nameText.PlaceholderText = "e.g. Salads, Drinks, Bakery, Vegetables";
            nameText.TabIndex = 1;
            nameText.TextChanged += nameText_TextChanged;
            //
            // colorCaption
            //
            colorCaption.Dock = DockStyle.Fill;
            colorCaption.Name = "colorCaption";
            colorCaption.TabIndex = 2;
            colorCaption.Text = "Color";
            colorCaption.TextAlign = ContentAlignment.MiddleLeft;
            //
            // colorPicker
            //
            colorPicker.Dock = DockStyle.Fill;
            colorPicker.Name = "colorPicker";
            colorPicker.TabIndex = 3;
            colorPicker.ColorChanged += colorPicker_ColorChanged;
            //
            // activeCheck
            //
            activeCheck.AutoSize = true;
            activeCheck.Checked = true;
            activeCheck.CheckState = CheckState.Checked;
            activeCheck.Dock = DockStyle.Fill;
            activeCheck.Name = "activeCheck";
            activeCheck.TabIndex = 4;
            activeCheck.Text = "Show on the cashier screen";
            //
            // previewCaption
            //
            previewCaption.Name = "previewCaption";
            previewCaption.Padding = new Padding(0, 8, 0, 0);
            previewCaption.TabIndex = 5;
            previewCaption.Text = "Preview";
            //
            // previewPanel
            //
            previewPanel.Controls.Add(previewButton);
            previewPanel.Controls.Add(previewOpenButton);
            previewPanel.Dock = DockStyle.Fill;
            previewPanel.Name = "previewPanel";
            previewPanel.Padding = new Padding(0, 6, 0, 0);
            previewPanel.TabIndex = 6;
            //
            // previewButton
            //
            previewButton.Name = "previewButton";
            previewButton.Size = new Size(150, 56);
            previewButton.TabIndex = 0;
            previewButton.TabStop = false;
            //
            // previewOpenButton
            //
            previewOpenButton.Name = "previewOpenButton";
            previewOpenButton.Size = new Size(150, 56);
            previewOpenButton.TabIndex = 1;
            previewOpenButton.TabStop = false;
            //
            // buttonsPanel
            //
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(saveButton);
            buttonsPanel.Dock = DockStyle.Bottom;
            buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonsPanel.Location = new Point(0, 230);
            buttonsPanel.Name = "buttonsPanel";
            buttonsPanel.Padding = new Padding(8, 6, 8, 6);
            buttonsPanel.Size = new Size(640, 52);
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
            // saveButton
            //
            saveButton.BackColor = Color.FromArgb(34, 139, 117);
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.FlatStyle = FlatStyle.Flat;
            saveButton.ForeColor = Color.White;
            saveButton.Name = "saveButton";
            saveButton.Size = new Size(100, 36);
            saveButton.TabIndex = 0;
            saveButton.Text = "Save";
            saveButton.UseVisualStyleBackColor = false;
            saveButton.Click += saveButton_Click;
            //
            // QuickGroupEditForm
            //
            AcceptButton = saveButton;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(640, 282);
            Controls.Add(fieldsTable);
            Controls.Add(buttonsPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "QuickGroupEditForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Tab";
            fieldsTable.ResumeLayout(false);
            fieldsTable.PerformLayout();
            previewPanel.ResumeLayout(false);
            buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel fieldsTable;
        private Label nameCaption;
        private TextBox nameText;
        private Label colorCaption;
        private ColorSwatchPicker colorPicker;
        private CheckBox activeCheck;
        private Label previewCaption;
        private FlowLayoutPanel previewPanel;
        private Button previewButton;
        private Button previewOpenButton;
        private FlowLayoutPanel buttonsPanel;
        private Button cancelButton;
        private Button saveButton;
    }
}
