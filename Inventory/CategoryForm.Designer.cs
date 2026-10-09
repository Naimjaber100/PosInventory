namespace Inventory
{
    partial class CategoryForm
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
            nameLabel = new Label();
            nameText = new TextBox();
            parentLabel = new Label();
            parentCombo = new ComboBox();
            saveButton = new Button();
            cancelButton = new Button();
            SuspendLayout();
            //
            // nameLabel
            //
            nameLabel.AutoSize = true;
            nameLabel.Location = new Point(16, 22);
            nameLabel.Name = "nameLabel";
            nameLabel.Text = "Name *";
            //
            // nameText
            //
            nameText.Location = new Point(110, 18);
            nameText.MaxLength = 100;
            nameText.Name = "nameText";
            nameText.Size = new Size(320, 27);
            nameText.TabIndex = 0;
            //
            // parentLabel
            //
            parentLabel.AutoSize = true;
            parentLabel.Location = new Point(16, 62);
            parentLabel.Name = "parentLabel";
            parentLabel.Text = "Parent";
            //
            // parentCombo
            //
            parentCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            parentCombo.Location = new Point(110, 58);
            parentCombo.Name = "parentCombo";
            parentCombo.Size = new Size(320, 28);
            parentCombo.TabIndex = 1;
            //
            // saveButton
            //
            saveButton.BackColor = Color.FromArgb(34, 139, 117);
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.FlatStyle = FlatStyle.Flat;
            saveButton.ForeColor = Color.White;
            saveButton.Location = new Point(224, 108);
            saveButton.Name = "saveButton";
            saveButton.Size = new Size(100, 36);
            saveButton.TabIndex = 2;
            saveButton.Text = "Save";
            saveButton.UseVisualStyleBackColor = false;
            saveButton.Click += saveButton_Click;
            //
            // cancelButton
            //
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(330, 108);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(100, 36);
            cancelButton.TabIndex = 3;
            cancelButton.Text = "Cancel";
            //
            // CategoryForm
            //
            AcceptButton = saveButton;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(448, 160);
            Controls.Add(nameLabel);
            Controls.Add(nameText);
            Controls.Add(parentLabel);
            Controls.Add(parentCombo);
            Controls.Add(saveButton);
            Controls.Add(cancelButton);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CategoryForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Category";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label nameLabel;
        private TextBox nameText;
        private Label parentLabel;
        private ComboBox parentCombo;
        private Button saveButton;
        private Button cancelButton;
    }
}
