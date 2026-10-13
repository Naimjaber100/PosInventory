using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Adds a tab of POS buttons (no tab passed) or edits one: name (Salads, Drinks...), color, shown or hidden
    public partial class QuickGroupEditForm : Form
    {
        private readonly QuickButtonRepository _quickButtonRepository = new();
        private readonly QuickGroupModel? _group;

        public QuickGroupEditForm(QuickGroupModel? group = null)
        {
            InitializeComponent();
            AppTheme.Apply(this);
            _group = group;

            if (group != null)
            {
                Text = "Edit tab";
                nameText.Text = group.Name;
                activeCheck.Checked = group.IsActive;
                colorPicker.SelectedColor = QuickButtonStyle.Parse(group.Color);
            }
            else
            {
                Text = "New tab";
            }
            UpdatePreview();
        }

        /// <summary>The id of the saved tab.</summary>
        public int SavedGroupId { get; private set; }

        private void nameText_TextChanged(object? sender, EventArgs e) => UpdatePreview();

        private void colorPicker_ColorChanged(object? sender, EventArgs e) => UpdatePreview();

        private void UpdatePreview()
        {
            var name = nameText.Text.Trim();
            var color = QuickButtonStyle.ToHex(colorPicker.SelectedColor);
            QuickButtonStyle.ApplyGroup(previewButton, name.Length > 0 ? name : "Tab", color, selected: false);
            QuickButtonStyle.ApplyGroup(previewOpenButton, name.Length > 0 ? name : "Tab", color, selected: true);
        }

        private async void saveButton_Click(object? sender, EventArgs e)
        {
            var name = nameText.Text.Trim();
            if (name.Length == 0)
            {
                MessageBox.Show(this, "Name is required.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nameText.Focus();
                return;
            }

            try
            {
                saveButton.Enabled = false;
                SavedGroupId = await _quickButtonRepository.SaveGroupAsync(_group?.GroupId, name,
                    QuickButtonStyle.ToHex(colorPicker.SelectedColor), activeCheck.Checked);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save the tab:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                saveButton.Enabled = true;
            }
        }
    }
}
