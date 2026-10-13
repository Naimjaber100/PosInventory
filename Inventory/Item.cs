using System.Drawing.Drawing2D;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Product Management (Item > Item list).
    // Left: categories with item counts (right-click or the buttons: add, sub-group, edit, delete).
    // Center: search (name or barcode; Enter for scanners), stock chips (All / In stock / Low / Out), the products,
    //         pages. Click the Active switch to turn an item on or off.
    // Right: details of the selected product: units & pricing, stock, Edit / Delete.
    // Items have no image, so there is no image column.
    public partial class Item : Form
    {
        private const string AllItemsTag = "all";
        private static readonly Color Blue = AppTheme.Accent;   // the accent of the whole back office (was blue)
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);
        private static readonly Color Muted = Color.FromArgb(100, 110, 122);
        private static readonly Color Green = Color.FromArgb(22, 163, 74);
        private static readonly Color Orange = Color.FromArgb(234, 88, 12);
        private static readonly Color Red = Color.FromArgb(220, 38, 38);

        private readonly ProductRepository _productRepository = new();
        private readonly CategoryRepository _categoryRepository = new();
        private List<CategoryModel> _categories = new();
        private readonly Dictionary<TreeNode, long> _counts = new();   // item count shown on each category
        private int? _categoryFilter;          // null = all items, ProductRepository.NoCategory = no category
        private string _categoryTitle = "All products";
        private string? _stockFilter;          // null = all, ProductRepository.StockIn / StockLow / StockOut
        private int _page = 1;
        private int _pageSize = 50;
        private bool _loading;
        private bool _buildingTree;
        private string? _search;               // current search text (null = none)
        private bool _reloadPending;
        private PagedResult<ProductModel> _result = new();
        private ProductModel? _shown;          // the product in the details panel

        public Item()
        {
            InitializeComponent();
            AppTheme.Apply(this);
            itemsGrid.AutoGenerateColumns = false;
            pageSizeCombo.SelectedItem = _pageSize;
            userLabel.Text = AppSession.CurrentUser != null ? $"{AppSession.UserName}  " : string.Empty;
            foreach (var chip in new[] { allChip, inStockChip, lowChip, outChip }) chip.Click += stockChip_Click;
            StyleChips();
            ShowDetails(null);
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
                // Counts for "All products" and "No category" (one row each, only the total is used)
                var all = (await _productRepository.GetPageAsync(1, 1)).TotalCount;
                var none = (await _productRepository.GetPageAsync(1, 1, ProductRepository.NoCategory)).TotalCount;
                BuildTree(selectCategoryId, all, none);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load categories:\n" + ex.Message, "Items",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BuildTree(int? selectCategoryId, long allCount, long noneCount)
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
                _counts.Clear();
                var allNode = categoryTree.Nodes.Add("All products");
                allNode.Tag = AllItemsTag;
                _counts[allNode] = allCount;

                var nodes = new Dictionary<int, TreeNode>();
                foreach (var c in _categories)
                {
                    var node = new TreeNode(c.Name) { Tag = c };
                    _counts[node] = totals[c.CategoryId];
                    nodes[c.CategoryId] = node;
                    if (c.ParentId.HasValue && nodes.TryGetValue(c.ParentId.Value, out var parent))
                        parent.Nodes.Add(node);
                    else
                        categoryTree.Nodes.Add(node);
                }

                var noneNode = categoryTree.Nodes.Add("No category");
                noneNode.Tag = ProductRepository.NoCategory;
                _counts[noneNode] = noneCount;

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
                    _categoryTitle = "All products";
                    break;
            }
        }

        // A category row: the selected one is blue with a bar; its item count in a pill on the right
        private void categoryTree_DrawNode(object? sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node == null || e.Bounds.IsEmpty) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var row = new Rectangle(0, e.Bounds.Y, categoryTree.ClientSize.Width, e.Bounds.Height);
            var selected = (e.State & TreeNodeStates.Selected) != 0;

            using (var back = new SolidBrush(selected ? AppTheme.Selection : categoryTree.BackColor)) g.FillRectangle(back, row);
            if (selected)
            {
                using var bar = new SolidBrush(Blue);
                g.FillRectangle(bar, 0, row.Y + 4, 4, row.Height - 8);
            }

            var isAll = e.Node.Tag is string;
            var isNone = e.Node.Tag is int;
            using var font = new Font(categoryTree.Font, isAll || selected ? FontStyle.Bold : FontStyle.Regular);
            var textColor = selected ? Blue : isNone ? Muted : Dark;

            var count = _counts.TryGetValue(e.Node, out var c) ? c.ToString("#,0") : string.Empty;
            var pillWidth = count.Length == 0 ? 0 : TextRenderer.MeasureText(count, categoryTree.Font).Width + 12;
            var textRect = new Rectangle(e.Bounds.X, row.Y, row.Right - e.Bounds.X - pillWidth - 14, row.Height);
            TextRenderer.DrawText(g, e.Node.Text, font, textRect, textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if (count.Length > 0)
            {
                var pill = new Rectangle(row.Right - pillWidth - 8, row.Y + (row.Height - 22) / 2, pillWidth, 22);
                using var path = QuickButtonStyle.Rounded(pill, 11);
                using var pillBrush = new SolidBrush(selected ? Color.FromArgb(190, 224, 214) : Color.FromArgb(238, 241, 244));
                g.FillPath(pillBrush, path);
                TextRenderer.DrawText(g, count, categoryTree.Font, pill, Dark, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private async void categoryTree_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            categoryTree.Invalidate();
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
                MessageBox.Show(this, "Select the category to add a sub-category under.", "Add sub-category",
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
                MessageBox.Show(this, "Select a category to edit.", "Edit category",
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
                MessageBox.Show(this, "Select a category to delete.", "Delete category",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(this, $"Delete the category \"{category.Path}\"?", "Delete category",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                await _categoryRepository.DeleteAsync(category.CategoryId);
                await LoadCategoriesAsync(category.ParentId);   // select the parent (or All products)
                _page = 1;
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not delete the category:\n" + ex.Message, "Delete category",
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

        #region Search and stock chips

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

        private async void stockChip_Click(object? sender, EventArgs e)
        {
            _stockFilter = sender == inStockChip ? ProductRepository.StockIn
                : sender == lowChip ? ProductRepository.StockLow
                : sender == outChip ? ProductRepository.StockOut
                : null;
            StyleChips();
            _page = 1;
            await LoadItemsAsync();
        }

        // The chosen chip is filled (Low stock red, Out of stock dark red, the others blue)
        private void StyleChips()
        {
            Style(allChip, _stockFilter == null, Blue);
            Style(inStockChip, _stockFilter == ProductRepository.StockIn, Green);
            Style(lowChip, _stockFilter == ProductRepository.StockLow, Orange);
            Style(outChip, _stockFilter == ProductRepository.StockOut, Red);

            static void Style(Button chip, bool on, Color color)
            {
                chip.BackColor = on ? color : Color.White;
                chip.ForeColor = on ? Color.White : color;
                chip.FlatAppearance.BorderColor = on ? color : Color.FromArgb(210, 215, 222);
            }
        }

        #endregion

        #region Products and pages

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
                var selectedId = (itemsGrid.CurrentRow?.DataBoundItem as ProductModel)?.ProductId;

                var result = await _productRepository.GetPageAsync(_page, _pageSize, _categoryFilter, _search, _stockFilter);
                if (result.Items.Count == 0 && _page > 1)
                {
                    // The page no longer exists (e.g. last item on the last page was deleted)
                    _page = result.TotalCount == 0 ? 1 : result.TotalPages;
                    result = await _productRepository.GetPageAsync(_page, _pageSize, _categoryFilter, _search, _stockFilter);
                }

                _result = result;
                itemsGrid.DataSource = result.Items.ToList();
                Text = $"Product Management - {_categoryTitle} ({result.TotalCount})";
                listTitleLabel.Text = _search == null ? _categoryTitle : $"{_categoryTitle}  ·  \"{_search}\"";
                var first = result.TotalCount == 0 ? 0 : (result.Page - 1) * result.PageSize + 1;
                var last = (result.Page - 1) * result.PageSize + result.Items.Count;
                showingLabel.Text = result.TotalCount == 0 ? "No products" : $"Showing {first}–{last} of {result.TotalCount:#,0} products";
                BuildPageButtons();
                emptyLabel.Visible = result.Items.Count == 0;

                // Keep the same product selected (or the first one) in the details panel
                var index = result.Items.ToList().FindIndex(p => p.ProductId == selectedId);
                if (result.Items.Count > 0)
                {
                    SelectRow(index >= 0 ? index : 0);
                    await ShowSelectedAsync();
                }
                else
                {
                    ShowDetails(null);
                }
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

        private void SelectRow(int index)
        {
            if (index < 0 || index >= itemsGrid.Rows.Count) return;
            itemsGrid.CurrentCell = itemsGrid.Rows[index].Cells[nameColumn.Index];
        }

        // ‹ 1 2 3 › : up to 5 page numbers around the current page
        private void BuildPageButtons()
        {
            pagesPanel.SuspendLayout();
            foreach (var old in pagesPanel.Controls.OfType<Button>().ToList())
            {
                pagesPanel.Controls.Remove(old);
                old.Dispose();
            }
            var total = _result.TotalPages;
            var from = Math.Max(1, Math.Min(_result.Page - 2, total - 4));
            var to = Math.Min(total, from + 4);

            AddPageButton("‹", _result.Page - 1, _result.HasPrevious, false);
            for (var p = from; p <= to; p++) AddPageButton(p.ToString(), p, true, p == _result.Page);
            AddPageButton("›", _result.Page + 1, _result.HasNext, false);
            pagesPanel.ResumeLayout();

            void AddPageButton(string text, int page, bool enabled, bool current)
            {
                var button = new Button
                {
                    Text = text,
                    Size = new Size(text.Length > 2 ? 44 : 34, 32),
                    Margin = new Padding(3, 0, 0, 0),
                    FlatStyle = FlatStyle.Flat,
                    Enabled = enabled,
                    BackColor = current ? Blue : Color.White,
                    ForeColor = current ? Color.White : Dark,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    UseVisualStyleBackColor = false,
                };
                button.FlatAppearance.BorderColor = current ? Blue : Color.FromArgb(210, 215, 222);
                button.Click += async (_, _) => { _page = page; await LoadItemsAsync(); };
                pagesPanel.Controls.Add(button);
            }
        }

        private async void pageSizeCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (pageSizeCombo.SelectedItem is not int size || size == _pageSize) return;
            _pageSize = size;
            _page = 1;
            await LoadItemsAsync();
        }

        private async void itemsGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (!_loading) await ShowSelectedAsync();
        }

        private async Task ShowSelectedAsync()
        {
            var product = itemsGrid.CurrentRow?.DataBoundItem as ProductModel;
            if (product?.ProductId == _shown?.ProductId && product != null && ReferenceEquals(product, _shown)) return;
            ShowDetails(product);
            if (product == null) return;
            try
            {
                var packagings = (await _productRepository.GetPackagingsAsync(product.ProductId)).ToList();
                if (!ReferenceEquals(_shown, product)) return;   // another product was clicked meanwhile
                unitsGrid.DataSource = packagings
                    .Select(p => new { Unit = p.Name.ToUpperInvariant() + (p.IsDefault ? " *" : string.Empty), Barcode = p.Barcode ?? "—", Price = p.SalePrice })
                    .ToList();
                unitsHint.Text = packagings.Count == 0 ? "No units yet." : "* default unit (shown in the list)";
            }
            catch (Exception ex)
            {
                unitsHint.Text = "Could not load the units: " + ex.Message;
            }
        }

        #endregion

        #region Details panel

        private void ShowDetails(ProductModel? product)
        {
            _shown = product;
            detailsContent.Visible = product != null;
            noSelectionLabel.Visible = product == null;
            if (product == null) return;

            detailNameLabel.Text = product.Name + (string.IsNullOrWhiteSpace(product.Size) ? string.Empty : $"  {product.Size}")
                                   + (string.IsNullOrWhiteSpace(product.Color) ? string.Empty : $"  {product.Color}");
            detailStatusLabel.Text = $"●  {product.CategoryName ?? "No category"}  ·  {(product.IsActive ? "Active" : "Inactive")}";
            detailStatusLabel.ForeColor = product.IsActive ? Green : Muted;

            var state = StockState(product);
            stockValueLabel.Text = product.ProductType == 1 ? $"{product.StockQty:#,0.###} {product.BaseUnit}" : "— (bundle)";
            stockValueLabel.ForeColor = state switch { StockLevel.Out => Red, StockLevel.Low => Orange, _ => Dark };
            reorderValueLabel.Text = $"{product.ReorderLevel:#,0.###} {product.BaseUnit}";
            unitsGrid.DataSource = null;
            unitsHint.Text = "Loading...";
        }

        private void closeDetailsButton_Click(object? sender, EventArgs e) => SetDetailsVisible(false);

        private void showDetailsButton_Click(object? sender, EventArgs e) => SetDetailsVisible(true);

        private void SetDetailsVisible(bool visible)
        {
            bodyTable.ColumnStyles[2].Width = visible ? 340 : 0;
            detailsCard.Visible = visible;
            showDetailsButton.Visible = !visible;
        }

        private async void detailEditButton_Click(object? sender, EventArgs e) => await EditSelectedAsync();

        private async void detailDeleteButton_Click(object? sender, EventArgs e) => await DeleteSelectedAsync();

        #endregion

        #region Add, edit, delete, active

        private async void addButton_Click(object? sender, EventArgs e)
        {
            var defaultCategoryId = _categoryFilter is int id && id != ProductRepository.NoCategory ? id : (int?)null;
            using var addForm = new AddItemForm(null, defaultCategoryId);
            if (addForm.ShowDialog(this) == DialogResult.OK)
            {
                await RefreshAllAsync();
            }
        }

        private async void itemsGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex != statusColumn.Index) await EditSelectedAsync();
        }

        private async Task EditSelectedAsync()
        {
            var product = SelectedProduct();
            if (product == null) return;

            using var editForm = new AddItemForm(product.ProductId);
            if (editForm.ShowDialog(this) == DialogResult.OK)
            {
                _shown = null;
                await RefreshAllAsync();
            }
        }

        private async Task DeleteSelectedAsync()
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
                _shown = null;
                await RefreshAllAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not delete item:\n" + ex.Message, "Delete item",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // The Active switch in the last column turns the item on or off at once
        private async void itemsGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != statusColumn.Index) return;
            if (itemsGrid.Rows[e.RowIndex].DataBoundItem is not ProductModel product) return;

            var turnOn = !product.IsActive;
            try
            {
                await _productRepository.SetActiveAsync(product.ProductId, turnOn);
                product.IsActive = turnOn;
                itemsGrid.InvalidateRow(e.RowIndex);
                if (ReferenceEquals(product, _shown)) { _shown = null; await ShowSelectedAsync(); }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not change the item:\n" + ex.Message, "Items", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private ProductModel? SelectedProduct()
        {
            if (itemsGrid.CurrentRow?.DataBoundItem is ProductModel product) return product;

            MessageBox.Show(this, "Select an item first.", "Items", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        #endregion

        #region Excel

        private void excelButton_Click(object? sender, EventArgs e) =>
            excelMenu.Show(excelButton, new Point(0, excelButton.Height));

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

        #endregion

        #region Grid drawing

        private enum StockLevel { Ok, Low, Out }

        private static StockLevel StockState(ProductModel p) =>
            p.ProductType != 1 ? StockLevel.Ok
            : p.StockQty <= 0 ? StockLevel.Out
            : p.StockQty <= p.ReorderLevel ? StockLevel.Low
            : StockLevel.Ok;

        private void itemsGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.CellStyle == null) return;
            if (e.ColumnIndex == numberColumn.Index)
            {
                e.Value = $"{(_result.Page - 1) * _result.PageSize + e.RowIndex + 1}.";
                e.FormattingApplied = true;
            }
            else if (e.ColumnIndex == priceColumn.Index && e.Value is decimal price)
            {
                e.Value = price.ToString("#,0.##");
                e.FormattingApplied = true;
            }
            if (itemsGrid.Rows[e.RowIndex].DataBoundItem is ProductModel { IsActive: false })
                e.CellStyle.ForeColor = e.CellStyle.SelectionForeColor = Color.Gray;
        }

        private void itemsGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null || itemsGrid.Rows[e.RowIndex].DataBoundItem is not ProductModel p) return;
            var col = e.ColumnIndex;
            if (col != nameColumn.Index && col != categoryColumn.Index && col != stockColumn.Index && col != statusColumn.Index) return;

            e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border | DataGridViewPaintParts.SelectionBackground);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = e.CellBounds;
            var dim = !p.IsActive;

            if (col == nameColumn.Index)
            {
                // Name in bold, under it: size · color · base unit
                using var bold = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
                using var small = new Font("Segoe UI", 8.5F);
                var sub = string.Join("  ·  ", new[] { p.Size, p.Color, p.BaseUnit }.Where(s => !string.IsNullOrWhiteSpace(s)));
                TextRenderer.DrawText(g, p.Name, bold, new Rectangle(r.X + 6, r.Y + 6, r.Width - 12, 22), dim ? Color.Gray : Dark, TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(g, sub, small, new Rectangle(r.X + 6, r.Y + 27, r.Width - 12, 18), Muted, TextFormatFlags.EndEllipsis);
            }
            else if (col == categoryColumn.Index)
            {
                var text = p.CategoryName ?? "No category";
                using var font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
                var size = TextRenderer.MeasureText(text, font);
                var pill = new Rectangle(r.X + 6, r.Y + (r.Height - 24) / 2, Math.Min(size.Width + 16, r.Width - 12), 24);
                using var path = QuickButtonStyle.Rounded(pill, 6);
                using var back = new SolidBrush(p.CategoryName == null ? Color.FromArgb(245, 245, 245) : Color.FromArgb(238, 241, 244));
                g.FillPath(back, path);
                TextRenderer.DrawText(g, text, font, pill, p.CategoryName == null ? Muted : Dark, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            else if (col == stockColumn.Index)
            {
                // The quantity in a colored box (green / orange low / red out) and a small note under it
                var state = StockState(p);
                var color = p.ProductType != 1 ? Muted : state switch { StockLevel.Out => Red, StockLevel.Low => Orange, _ => Green };
                var text = p.ProductType != 1 ? "—" : p.StockQty.ToString("#,0.###");
                using var font = new Font("Segoe UI", 10F, FontStyle.Bold);
                var size = TextRenderer.MeasureText(text, font);
                var box = new Rectangle(r.X + 6, r.Y + 7, Math.Max(36, size.Width + 12), 24);
                using (var path = QuickButtonStyle.Rounded(box, 5))
                using (var back = new SolidBrush(p.ProductType != 1 ? Color.FromArgb(238, 241, 244) : color))
                    g.FillPath(back, path);
                TextRenderer.DrawText(g, text, font, box, p.ProductType != 1 ? Muted : Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                if (state != StockLevel.Ok)
                {
                    using var small = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold);
                    TextRenderer.DrawText(g, state == StockLevel.Out ? "Out of stock" : "Low stock", small,
                        new Rectangle(r.X + 6, box.Bottom + 1, r.Width - 8, 16), color, TextFormatFlags.Left);
                }
            }
            else
            {
                // Barcode, then the Active switch on the right
                using var font = new Font("Segoe UI", 9F);
                TextRenderer.DrawText(g, p.Barcode ?? "—", font, new Rectangle(r.X + 6, r.Y, r.Width - 64, r.Height), dim ? Color.Gray : Dark,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                var track = new Rectangle(r.Right - 52, r.Y + (r.Height - 22) / 2, 42, 22);
                using (var path = QuickButtonStyle.Rounded(track, 11))
                using (var fill = new SolidBrush(p.IsActive ? Blue : Color.FromArgb(205, 210, 216)))
                    g.FillPath(fill, path);
                g.FillEllipse(Brushes.White, new Rectangle(p.IsActive ? track.Right - 20 : track.X + 2, track.Y + 2, 18, 18));
            }
            e.Handled = true;
        }

        private void itemsGrid_CellMouseEnter(object? sender, DataGridViewCellEventArgs e) =>
            itemsGrid.Cursor = e.RowIndex >= 0 && e.ColumnIndex == statusColumn.Index ? Cursors.Hand : Cursors.Default;

        private void unitsGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (unitsGrid.Columns["Price"] is { } price)
            {
                price.DefaultCellStyle.Format = "#,0.##";
                price.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                price.DefaultCellStyle.Font = new Font(unitsGrid.Font, FontStyle.Bold);
                price.FillWeight = 70;
            }
            if (unitsGrid.Columns["Unit"] is { } unit)
            {
                unit.DefaultCellStyle.Font = new Font(unitsGrid.Font, FontStyle.Bold);
                unit.FillWeight = 70;
            }
            unitsGrid.ClearSelection();
        }

        #endregion
    }
}
