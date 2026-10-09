using System.ComponentModel;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Used for both adding (no productId) and editing (with productId) an item
    public partial class AddItemForm : Form
    {
        private const short StandardType = 1;

        private readonly ProductRepository _productRepository = new();
        private readonly CategoryRepository _categoryRepository = new();
        private readonly BindingList<PackagingModel> _packagings = new();
        private readonly int? _productId;
        private readonly int? _defaultCategoryId;
        private string _lastUnit = string.Empty;

        public AddItemForm() : this(null) { }

        /// <param name="productId">Item to edit; null to add a new item.</param>
        /// <param name="defaultCategoryId">Category pre-selected for a new item.</param>
        public AddItemForm(int? productId, int? defaultCategoryId = null)
        {
            InitializeComponent();
            _productId = productId;
            _defaultCategoryId = defaultCategoryId;

            typeCombo.DataSource = new[]
            {
                new KeyValuePair<short, string>(1, "Standard"),
                new KeyValuePair<short, string>(2, "Bundle"),
                new KeyValuePair<short, string>(3, "Model with variants"),
            };
            typeCombo.DisplayMember = "Value";
            typeCombo.ValueMember = "Key";

            // _lastUnit must be set before binding: binding raises DataBindingComplete -> UpdateFactorHeader
            _lastUnit = unitCombo.Text.Trim();
            packagingGrid.DataSource = _packagings;
            unitCombo.TextChanged += unitCombo_TextChanged;

            if (IsEdit)
            {
                Text = "Edit item";
                // Stock and cost change only through stock operations (purchases, adjustments, sales)
                costInput.Enabled = false;
                stockLabel.Text = "Stock";
                stockInput.Minimum = -1000000000; // stock can go negative after sales
            }
            else
            {
                // A new item starts with one packaging: the base unit itself
                _packagings.Add(new PackagingModel { Name = _lastUnit, Factor = 1, IsDefault = true });
            }
        }

        private bool IsEdit => _productId.HasValue;

        private async void AddItemForm_Load(object? sender, EventArgs e)
        {
            try
            {
                var categories = new List<CategoryModel> { new() { CategoryId = 0, Name = "(none)", Path = "(none)" } };
                categories.AddRange(await _categoryRepository.GetAllAsync());
                categoryCombo.DataSource = categories;
                categoryCombo.DisplayMember = nameof(CategoryModel.Path);
                categoryCombo.ValueMember = nameof(CategoryModel.CategoryId);

                if (!IsEdit && _defaultCategoryId.HasValue)
                    categoryCombo.SelectedValue = _defaultCategoryId.Value;

                if (IsEdit)
                {
                    var product = await _productRepository.GetByIdAsync(_productId!.Value);
                    if (product == null)
                    {
                        MessageBox.Show(this, "This item no longer exists.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Close();
                        return;
                    }
                    FillFields(product);

                    _packagings.Clear();
                    foreach (var p in await _productRepository.GetPackagingsAsync(product.ProductId))
                        _packagings.Add(p);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load data:\n" + ex.Message, Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FillFields(ProductModel product)
        {
            nameText.Text = product.Name;
            categoryCombo.SelectedValue = product.CategoryId ?? 0;
            typeCombo.SelectedValue = product.ProductType;
            unitCombo.Text = product.BaseUnit;
            sizeText.Text = product.Size;
            colorText.Text = product.Color;
            costInput.Value = product.AvgCost;
            stockInput.Value = product.StockQty;
            reorderInput.Value = product.ReorderLevel;
            weightedCheck.Checked = product.IsWeighted;
            activeCheck.Checked = product.IsActive;
        }

        private void typeCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (IsEdit)
            {
                stockInput.Enabled = false;
                return;
            }

            // The database only allows stock on standard items (products_check constraint)
            var isStandard = typeCombo.SelectedValue is short type && type == StandardType;
            stockInput.Enabled = isStandard;
            if (!isStandard) stockInput.Value = 0;
        }

        #region Packagings

        // Keep the base packaging's name in step with the base unit while it still matches
        private void unitCombo_TextChanged(object? sender, EventArgs e)
        {
            var unit = unitCombo.Text.Trim();
            var basePackaging = _packagings.FirstOrDefault(p => p.Factor == 1 &&
                string.Equals(p.Name, _lastUnit, StringComparison.OrdinalIgnoreCase));
            if (basePackaging != null && unit.Length > 0)
            {
                basePackaging.Name = unit;
                _packagings.ResetItem(_packagings.IndexOf(basePackaging));
            }
            _lastUnit = unit;
            UpdateFactorHeader();
        }

        private void addPackagingButton_Click(object? sender, EventArgs e)
        {
            var packaging = new PackagingModel { Name = string.Empty, Factor = 1, IsDefault = _packagings.Count == 0 };
            _packagings.Add(packaging);

            // Start typing the name of the new packaging (e.g. "box"), then its "Contains" (e.g. 24)
            var row = packagingGrid.Rows[_packagings.Count - 1];
            packagingGrid.CurrentCell = row.Cells[nameof(PackagingModel.Name)];
            packagingGrid.BeginEdit(true);
        }

        private void removePackagingButton_Click(object? sender, EventArgs e)
        {
            if (packagingGrid.CurrentRow?.DataBoundItem is not PackagingModel packaging) return;
            if (_packagings.Count == 1)
            {
                MessageBox.Show(this, "An item needs at least one packaging.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            packagingGrid.EndEdit();
            _packagings.Remove(packaging);
            if (packaging.IsDefault)
            {
                _packagings[0].IsDefault = true;
                _packagings.ResetItem(0);
            }
        }

        // Commit checkbox clicks immediately so "Default" reacts at once
        private void packagingGrid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (packagingGrid.CurrentCell is DataGridViewCheckBoxCell)
                packagingGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        // Only one packaging can be the default
        private void packagingGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || packagingGrid.Columns[e.ColumnIndex].DataPropertyName != nameof(PackagingModel.IsDefault)) return;
            if (_packagings[e.RowIndex].IsDefault)
            {
                for (var i = 0; i < _packagings.Count; i++)
                {
                    if (i == e.RowIndex || !_packagings[i].IsDefault) continue;
                    _packagings[i].IsDefault = false;
                    _packagings.ResetItem(i);
                }
            }
        }

        private void packagingGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewColumn column in packagingGrid.Columns)
            {
                if (column.ValueType != typeof(decimal)) continue;
                column.DefaultCellStyle.Format = "#,0.###";
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (packagingGrid.Columns[nameof(PackagingModel.Name)] is { } name) name.FillWeight = 120;
            if (packagingGrid.Columns[nameof(PackagingModel.Barcode)] is { } barcode) barcode.FillWeight = 140;
            UpdateFactorHeader();
        }

        private void UpdateFactorHeader()
        {
            if (packagingGrid.Columns[nameof(PackagingModel.Factor)] is { } factor)
                factor.HeaderText = string.IsNullOrEmpty(_lastUnit) ? "Contains" : $"Contains ({_lastUnit})";
        }

        private void packagingGrid_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            MessageBox.Show(this, "Enter a valid number.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }

        // Checks the packagings before saving; returns an error message or null
        private string? ValidatePackagings()
        {
            if (_packagings.Count == 0) return "Add at least one packaging.";
            foreach (var p in _packagings)
            {
                p.Name = p.Name?.Trim() ?? string.Empty;
                p.Barcode = string.IsNullOrWhiteSpace(p.Barcode) ? null : p.Barcode.Trim();
                if (p.Name.Length == 0) return "Every packaging needs a name (for example \"can\" or \"box\").";
                if (p.Factor <= 0) return $"Packaging \"{p.Name}\": \"Contains\" must be more than 0.";
                if (p.SalePrice < 0) return $"Packaging \"{p.Name}\": sale price cannot be negative.";
            }
            var duplicateName = _packagings.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (duplicateName != null) return $"Packaging name \"{duplicateName.Key}\" is used twice.";
            var duplicateBarcode = _packagings.Where(p => p.Barcode != null).GroupBy(p => p.Barcode).FirstOrDefault(g => g.Count() > 1);
            if (duplicateBarcode != null) return $"Barcode {duplicateBarcode.Key} is used twice.";
            if (_packagings.Count(p => p.IsDefault) != 1) return "Tick exactly one packaging as the default.";
            return null;
        }

        #endregion

        private async void saveButton_Click(object? sender, EventArgs e)
        {
            var name = nameText.Text.Trim();
            if (name.Length == 0)
            {
                MessageBox.Show(this, "Name is required.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nameText.Focus();
                return;
            }

            var baseUnit = unitCombo.Text.Trim();
            if (baseUnit.Length == 0)
            {
                MessageBox.Show(this, "Base unit is required.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                unitCombo.Focus();
                return;
            }

            if (!packagingGrid.EndEdit()) return;   // apply a cell that is still being edited
            var packagingError = ValidatePackagings();
            if (packagingError != null)
            {
                MessageBox.Show(this, packagingError, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                packagingGrid.Focus();
                return;
            }

            var categoryId = categoryCombo.SelectedValue is int id && id > 0 ? id : (int?)null;

            var product = new ProductModel
            {
                ProductId = _productId ?? 0,
                Name = name,
                CategoryId = categoryId,
                ProductType = (short)typeCombo.SelectedValue!,
                Size = NullIfEmpty(sizeText.Text),
                Color = NullIfEmpty(colorText.Text),
                BaseUnit = baseUnit,
                IsWeighted = weightedCheck.Checked,
                AvgCost = costInput.Value,
                ReorderLevel = reorderInput.Value,
                IsActive = activeCheck.Checked,
            };

            try
            {
                saveButton.Enabled = false;
                UseWaitCursor = true;
                if (IsEdit)
                    await _productRepository.UpdateAsync(product, _packagings);
                else
                    await _productRepository.AddAsync(product, stockInput.Value, AppConfig.CurrentUserId, _packagings);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save item:\n" + ex.Message, Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                saveButton.Enabled = true;
                UseWaitCursor = false;
            }
        }

        private static string? NullIfEmpty(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
