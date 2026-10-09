using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    public partial class Item : Form
    {
        private const string AllItemsTag = "all";

        private readonly ProductRepository _productRepository = new();
        private readonly CategoryRepository _categoryRepository = new();
        private List<CategoryModel> _categories = new();
        private int? _categoryFilter;          // null = all items, ProductRepository.NoCategory = no category
        private string _categoryTitle = "All items";
        private int _page = 1;
        private int _pageSize = 50;
        private bool _loading;
        private bool _buildingTree;
        private string? _search;               // current search text (null = none)
        private bool _reloadPending;

        public Item()
        {
            InitializeComponent();
            pageSizeCombo.SelectedItem = _pageSize;
        }

        private async void Item_Load(object? sender, EventArgs e)
        {
            await RefreshAllAsync();
        }

        // Categories (item counts change too) and the current page
        private async Task RefreshAllAsync()
        {
            await LoadCategoriesAsync();
            await LoadItemsAsync();
        }

        #region Category tree

        private async Task LoadCategoriesAsync(int? selectCategoryId = null)
        {
            try
            {
                _categories = (await _categoryRepository.GetAllAsync()).ToList();
                BuildTree(selectCategoryId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load categories:\n" + ex.Message, "Items",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BuildTree(int? selectCategoryId)
        {
            var expanded = new HashSet<int>(AllNodes(categoryTree.Nodes)
                .Where(n => n.IsExpanded && n.Tag is CategoryModel)
                .Select(n => ((CategoryModel)n.Tag!).CategoryId));
            var keepFilter = selectCategoryId ?? _categoryFilter;

            // Items in each category including all of its sub-categories
            // (the list has parents before children, so walking it backwards adds children first)
            var totals = _categories.ToDictionary(c => c.CategoryId, c => c.ItemCount);
            for (var i = _categories.Count - 1; i >= 0; i--)
            {
                var c = _categories[i];
                if (c.ParentId.HasValue && totals.ContainsKey(c.ParentId.Value))
                    totals[c.ParentId.Value] += totals[c.CategoryId];
            }

            _buildingTree = true;
            categoryTree.BeginUpdate();
            try
            {
                categoryTree.Nodes.Clear();
                var allNode = categoryTree.Nodes.Add("All items");
                allNode.Tag = AllItemsTag;
                allNode.NodeFont = new Font(categoryTree.Font, FontStyle.Bold);

                var nodes = new Dictionary<int, TreeNode>();
                foreach (var c in _categories)
                {
                    var node = new TreeNode($"{c.Name}  ({totals[c.CategoryId]})") { Tag = c };
                    nodes[c.CategoryId] = node;
                    if (c.ParentId.HasValue && nodes.TryGetValue(c.ParentId.Value, out var parent))
                        parent.Nodes.Add(node);
                    else
                        categoryTree.Nodes.Add(node);
                }

                var noneNode = categoryTree.Nodes.Add("No category");
                noneNode.Tag = ProductRepository.NoCategory;
                noneNode.ForeColor = Color.DimGray;

                foreach (var (id, node) in nodes)
                    if (expanded.Contains(id)) node.Expand();

                // Reselect the previous node (or the requested category)
                TreeNode selected = keepFilter switch
                {
                    null => allNode,
                    ProductRepository.NoCategory => noneNode,
                    int id when nodes.TryGetValue(id, out var n) => n,
                    _ => allNode,
                };
                categoryTree.SelectedNode = selected;
                selected.EnsureVisible();
                ApplyNodeFilter(selected);
            }
            finally
            {
                categoryTree.EndUpdate();
                _buildingTree = false;
            }
        }

        private void ApplyNodeFilter(TreeNode node)
        {
            switch (node.Tag)
            {
                case CategoryModel c:
                    _categoryFilter = c.CategoryId;
                    _categoryTitle = c.Path;
                    break;
                case int none:
                    _categoryFilter = none;
                    _categoryTitle = "No category";
                    break;
                default:
                    _categoryFilter = null;
                    _categoryTitle = "All items";
                    break;
            }
        }

        private async void categoryTree_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (_buildingTree || e.Node == null) return;
            ApplyNodeFilter(e.Node);
            _page = 1;
            await LoadItemsAsync();
        }

        private void categoryTree_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            // Right-click selects the node so the menu acts on it
            if (e.Button == MouseButtons.Right) categoryTree.SelectedNode = e.Node;
        }

        private void categoryMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var isCategory = SelectedCategory() != null;
            addSubCategoryMenuItem.Enabled = isCategory;
            editCategoryMenuItem.Enabled = isCategory;
            deleteCategoryMenuItem.Enabled = isCategory;
        }

        private CategoryModel? SelectedCategory() => categoryTree.SelectedNode?.Tag as CategoryModel;

        private async void addCategoryButton_Click(object? sender, EventArgs e)
        {
            await ShowCategoryFormAsync(new CategoryForm(_categories));
        }

        private async void addSubCategoryButton_Click(object? sender, EventArgs e)
        {
            var parent = SelectedCategory();
            if (parent == null)
            {
                MessageBox.Show(this, "Select the group to add a sub-group under.", "Add sub-group",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            await ShowCategoryFormAsync(new CategoryForm(_categories, parentId: parent.CategoryId));
        }

        private async void editCategoryButton_Click(object? sender, EventArgs e)
        {
            var category = SelectedCategory();
            if (category == null)
            {
                MessageBox.Show(this, "Select a group to edit.", "Edit group",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            await ShowCategoryFormAsync(new CategoryForm(_categories, category));
        }

        private async Task ShowCategoryFormAsync(CategoryForm form)
        {
            using (form)
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;
                await LoadCategoriesAsync(form.SavedCategoryId);
                _page = 1;
                await LoadItemsAsync();
            }
        }

        private async void deleteCategoryButton_Click(object? sender, EventArgs e)
        {
            var category = SelectedCategory();
            if (category == null)
            {
                MessageBox.Show(this, "Select a group to delete.", "Delete group",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(this, $"Delete the group \"{category.Path}\"?", "Delete group",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                await _categoryRepository.DeleteAsync(category.CategoryId);
                await LoadCategoriesAsync(category.ParentId);   // select the parent (or All items)
                _page = 1;
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not delete the group:\n" + ex.Message, "Delete group",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static IEnumerable<TreeNode> AllNodes(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                yield return node;
                foreach (var child in AllNodes(node.Nodes)) yield return child;
            }
        }

        #endregion

        #region Search

        private void searchBox_TextChanged(object? sender, EventArgs e)
        {
            // Wait until typing pauses, so every key press does not hit the database
            searchTimer.Stop();
            searchTimer.Start();
        }

        private async void searchTimer_Tick(object? sender, EventArgs e)
        {
            searchTimer.Stop();
            await ApplySearchAsync();
        }

        private async void searchBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // Barcode scanners type the code and press Enter: search right away
                e.SuppressKeyPress = true;
                searchTimer.Stop();
                await ApplySearchAsync();
                if (itemsGrid.Rows.Count == 1) searchBox.SelectAll();   // ready for the next scan
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                searchBox.Clear();   // TextChanged starts the timer, which reloads without a search
            }
        }

        private async Task ApplySearchAsync()
        {
            var search = string.IsNullOrWhiteSpace(searchBox.Text) ? null : searchBox.Text.Trim();
            if (search == _search) return;
            _search = search;
            _page = 1;
            await LoadItemsAsync();
        }

        #endregion

        // Reloads the current page (after add / edit / delete / import as well)
        private async Task LoadItemsAsync()
        {
            if (_loading)
            {
                // Another load is running: reload again when it finishes so the latest filter wins
                _reloadPending = true;
                return;
            }
            try
            {
                _loading = true;
                UseWaitCursor = true;

                var result = await _productRepository.GetPageAsync(_page, _pageSize, _categoryFilter, _search);
                if (result.Items.Count == 0 && _page > 1)
                {
                    // The page no longer exists (e.g. last item on the last page was deleted)
                    _page = result.TotalCount == 0 ? 1 : result.TotalPages;
                    result = await _productRepository.GetPageAsync(_page, _pageSize, _categoryFilter, _search);
                }

                itemsGrid.DataSource = result.Items;
                Text = _search == null
                    ? $"Items - {_categoryTitle} ({result.TotalCount})"
                    : $"Items - {_categoryTitle} - \"{_search}\" ({result.TotalCount} found)";
                pageLabel.Text = $"Page {result.Page} of {result.TotalPages}  ·  {result.TotalCount} items";
                prevButton.Enabled = result.HasPrevious;
                nextButton.Enabled = result.HasNext;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load items:\n" + ex.Message, "Items",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                _loading = false;
            }

            if (_reloadPending)
            {
                _reloadPending = false;
                await LoadItemsAsync();
            }
        }

        private async void prevButton_Click(object? sender, EventArgs e)
        {
            if (_page <= 1) return;
            _page--;
            await LoadItemsAsync();
        }

        private async void nextButton_Click(object? sender, EventArgs e)
        {
            _page++;
            await LoadItemsAsync();
        }

        private async void pageSizeCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (pageSizeCombo.SelectedItem is not int size || size == _pageSize) return;
            _pageSize = size;
            _page = 1;
            await LoadItemsAsync();
        }

        private async void addButton_Click(object? sender, EventArgs e)
        {
            var defaultCategoryId = _categoryFilter is int id && id != ProductRepository.NoCategory ? id : (int?)null;
            using var addForm = new AddItemForm(null, defaultCategoryId);
            if (addForm.ShowDialog(this) == DialogResult.OK)
            {
                await RefreshAllAsync();
            }
        }

        private async void editButton_Click(object? sender, EventArgs e)
        {
            await EditSelectedAsync();
        }

        private async void itemsGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) await EditSelectedAsync();
        }

        private async Task EditSelectedAsync()
        {
            var product = SelectedProduct();
            if (product == null) return;

            using var editForm = new AddItemForm(product.ProductId);
            if (editForm.ShowDialog(this) == DialogResult.OK)
            {
                await RefreshAllAsync();
            }
        }

        private async void deleteButton_Click(object? sender, EventArgs e)
        {
            var product = SelectedProduct();
            if (product == null) return;

            if (MessageBox.Show(this, $"Delete \"{product.Name}\"?", "Delete item",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                var deleted = await _productRepository.DeleteAsync(product.ProductId);
                if (!deleted)
                {
                    MessageBox.Show(this, $"\"{product.Name}\" has stock or sales history, so it was deactivated instead of deleted.",
                        "Delete item", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                await RefreshAllAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not delete item:\n" + ex.Message, "Delete item",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void importButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Import items from Excel",
                Filter = "Excel workbook (*.xlsx)|*.xlsx",
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                UseWaitCursor = true;

                var (rows, errors) = ProductExcelReader.Read(dialog.FileName);
                if (errors.Count > 0)
                {
                    UseWaitCursor = false;
                    var shown = string.Join("\n", errors.Take(20));
                    var more = errors.Count > 20 ? $"\n... and {errors.Count - 20} more." : string.Empty;
                    MessageBox.Show(this, "Nothing was imported. Fix these problems and try again:\n\n" + shown + more,
                        "Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                UseWaitCursor = false;
                if (MessageBox.Show(this, $"Import {rows.Count} item(s) from \"{Path.GetFileName(dialog.FileName)}\"?",
                        "Import", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                UseWaitCursor = true;
                var imported = await _productRepository.ImportAsync(rows, AppConfig.CurrentUserId);
                await RefreshAllAsync();

                UseWaitCursor = false;
                MessageBox.Show(this, $"{imported} item(s) imported.", "Import",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (IOException)
            {
                MessageBox.Show(this, "The file could not be opened. Close it in Excel and try again.", "Import",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Nothing was imported:\n" + ex.Message, "Import",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private void templateButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Save import template",
                Filter = "Excel workbook (*.xlsx)|*.xlsx",
                FileName = "items-import-template.xlsx",
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                ProductExcelReader.WriteTemplate(dialog.FileName);
                MessageBox.Show(this, "Template saved. Fill in one item per row, then use Import from Excel.", "Excel template",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save the template:\n" + ex.Message, "Excel template",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private ProductModel? SelectedProduct()
        {
            if (itemsGrid.CurrentRow?.DataBoundItem is ProductModel product) return product;

            MessageBox.Show(this, "Select an item first.", "Items", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        private void itemsGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
