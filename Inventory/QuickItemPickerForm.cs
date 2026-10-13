using System.ComponentModel;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // "Add items": picks items from the item list for a tab of quick buttons.
    // Search by name / barcode and filter by category; click a row (or Space) to tick it.
    // Items that already have a button show the tab they are on and cannot be ticked.
    public partial class QuickItemPickerForm : Form
    {
        private const int AllCategories = 0;

        private readonly QuickButtonRepository _quickButtonRepository = new();
        private readonly CategoryRepository _categoryRepository = new();
        private readonly List<int> _picked = new();                  // packaging ids in the order they were ticked
        private Dictionary<int, string> _categoryPaths = new();
        private BindingList<PickRow> _rows = new();
        private bool _loading;

        public QuickItemPickerForm(string tabName)
        {
            InitializeComponent();
            AppTheme.Apply(this);
            Text = $"Add items to \"{tabName}\"";
            itemsGrid.AutoGenerateColumns = false;
        }

        /// <summary>The ticked items, in the order they were ticked.</summary>
        public IReadOnlyList<int> PickedPackagingIds => _picked;

        // One row of the grid
        private sealed class PickRow
        {
            public required QuickItemModel Item { get; init; }
            public bool Picked { get; set; }
            public string ItemName => Item.ItemName;
            public string Category { get; init; } = string.Empty;
            public string? Barcode => Item.Barcode;
            public string Price => $"{Item.SalePrice:#,0}{(Item.IsWeighted ? " / " + Item.BaseUnit : string.Empty)}";
            public string OnTab => Item.ButtonTab ?? string.Empty;
            public bool CanPick => Item.ButtonTab == null;
        }

        private async void QuickItemPickerForm_Load(object? sender, EventArgs e)
        {
            try
            {
                var categories = (await _categoryRepository.GetAllAsync()).ToList();
                _categoryPaths = categories.ToDictionary(c => c.CategoryId, c => c.Path);

                var choices = new List<CategoryModel> { new() { CategoryId = AllCategories, Path = "All categories" } };
                choices.AddRange(categories);
                _loading = true;
                categoryCombo.DataSource = choices;
                categoryCombo.DisplayMember = nameof(CategoryModel.Path);
                categoryCombo.ValueMember = nameof(CategoryModel.CategoryId);
                _loading = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the categories:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            await LoadItemsAsync();
            searchText.Focus();
        }

        private async Task LoadItemsAsync()
        {
            var categoryId = categoryCombo.SelectedValue is int id && id != AllCategories ? id : (int?)null;
            try
            {
                var items = await _quickButtonRepository.GetItemListAsync(searchText.Text, categoryId);
                _rows = new BindingList<PickRow>(items.Select(i => new PickRow
                {
                    Item = i,
                    Picked = _picked.Contains(i.PackagingId),
                    Category = i.CategoryId is int c && _categoryPaths.TryGetValue(c, out var path) ? path : string.Empty,
                }).ToList());
                itemsGrid.DataSource = _rows;
                itemsGrid.ClearSelection();
                UpdateCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the items:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Search as you type (after a short pause)
        private void searchText_TextChanged(object? sender, EventArgs e)
        {
            searchTimer.Stop();
            searchTimer.Start();
        }

        private async void searchTimer_Tick(object? sender, EventArgs e)
        {
            searchTimer.Stop();
            await LoadItemsAsync();
        }

        private async void categoryCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_loading) await LoadItemsAsync();
        }

        private void searchText_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Down or Keys.Enter && itemsGrid.Rows.Count > 0)
            {
                e.SuppressKeyPress = true;
                itemsGrid.Focus();
            }
        }

        private void itemsGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) Toggle(_rows[e.RowIndex]);
        }

        private void itemsGrid_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Space || itemsGrid.CurrentRow == null) return;
            e.SuppressKeyPress = true;
            Toggle(_rows[itemsGrid.CurrentRow.Index]);
        }

        private void Toggle(PickRow row)
        {
            if (!row.CanPick) return;
            SetPicked(row, !row.Picked);
            itemsGrid.InvalidateRow(_rows.IndexOf(row));
            UpdateCount();
        }

        private void SetPicked(PickRow row, bool picked)
        {
            row.Picked = picked;
            _picked.Remove(row.Item.PackagingId);
            if (picked) _picked.Add(row.Item.PackagingId);
        }

        // Ticks (or unticks) every item shown that can be picked
        private void selectAllCheck_Click(object? sender, EventArgs e)
        {
            foreach (var row in _rows.Where(r => r.CanPick)) SetPicked(row, selectAllCheck.Checked);
            itemsGrid.Invalidate();
            UpdateCount();
        }

        private void UpdateCount()
        {
            var free = _rows.Count(r => r.CanPick);
            countLabel.Text = $"{_rows.Count} item(s) shown{(_rows.Count >= 500 ? " (first 500 - search to narrow)" : string.Empty)}   ·   {_picked.Count} ticked";
            addButton.Text = _picked.Count == 1 ? "Add 1 item" : $"Add {_picked.Count} items";
            addButton.Enabled = _picked.Count > 0;
            selectAllCheck.Checked = free > 0 && _rows.Where(r => r.CanPick).All(r => r.Picked);
        }

        private void itemsGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count || e.CellStyle == null) return;
            var row = _rows[e.RowIndex];
            if (!row.CanPick)
            {
                e.CellStyle.ForeColor = Color.Gray;
                e.CellStyle.SelectionForeColor = Color.Gainsboro;
            }
            else if (row.Picked)
            {
                e.CellStyle.BackColor = e.CellStyle.SelectionBackColor = Color.FromArgb(214, 238, 229);
                e.CellStyle.Font = new Font(itemsGrid.Font, FontStyle.Bold);
            }
        }

        private void addButton_Click(object? sender, EventArgs e)
        {
            if (_picked.Count == 0) return;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
