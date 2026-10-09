using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Adds a category (no category passed) or renames / moves one (category passed)
    public partial class CategoryForm : Form
    {
        private const int TopLevelId = 0;

        private readonly CategoryRepository _categoryRepository = new();
        private readonly CategoryModel? _category;

        public CategoryForm(IReadOnlyList<CategoryModel> categories, CategoryModel? category = null, int? parentId = null)
        {
            InitializeComponent();
            _category = category;

            // A category cannot be moved under itself or one of its sub-categories
            var excluded = category == null ? new HashSet<int>() : DescendantsAndSelf(categories, category.CategoryId);

            var parents = new List<CategoryModel> { new() { CategoryId = TopLevelId, Path = "(top level)" } };
            parents.AddRange(categories.Where(c => !excluded.Contains(c.CategoryId)));
            parentCombo.DataSource = parents;
            parentCombo.DisplayMember = nameof(CategoryModel.Path);
            parentCombo.ValueMember = nameof(CategoryModel.CategoryId);

            if (category != null)
            {
                Text = "Edit category";
                nameText.Text = category.Name;
                parentCombo.SelectedValue = category.ParentId ?? TopLevelId;
            }
            else
            {
                Text = parentId.HasValue ? "Add sub-category" : "Add category";
                parentCombo.SelectedValue = parentId ?? TopLevelId;
            }
        }

        /// <summary>The id of the saved category.</summary>
        public int SavedCategoryId { get; private set; }

        private async void saveButton_Click(object? sender, EventArgs e)
        {
            var name = nameText.Text.Trim();
            if (name.Length == 0)
            {
                MessageBox.Show(this, "Name is required.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nameText.Focus();
                return;
            }
            if (name.Contains('>'))
            {
                MessageBox.Show(this, "Name cannot contain \">\" (it separates levels in a path).", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nameText.Focus();
                return;
            }

            var parentId = parentCombo.SelectedValue is int id && id != TopLevelId ? id : (int?)null;

            try
            {
                saveButton.Enabled = false;
                if (_category == null)
                {
                    SavedCategoryId = await _categoryRepository.AddAsync(name, parentId);
                }
                else
                {
                    await _categoryRepository.UpdateAsync(_category.CategoryId, name, parentId);
                    SavedCategoryId = _category.CategoryId;
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save category:\n" + ex.Message, Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                saveButton.Enabled = true;
            }
        }

        private static HashSet<int> DescendantsAndSelf(IReadOnlyList<CategoryModel> categories, int categoryId)
        {
            var result = new HashSet<int> { categoryId };
            bool added;
            do
            {
                added = false;
                foreach (var c in categories)
                {
                    if (c.ParentId.HasValue && result.Contains(c.ParentId.Value) && result.Add(c.CategoryId))
                        added = true;
                }
            } while (added);
            return result;
        }
    }
}
