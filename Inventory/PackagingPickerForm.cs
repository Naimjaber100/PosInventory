using Inventory.Models;

namespace Inventory
{
    // Lets the user choose one item when a search matches several (Enter or double-click picks).
    // Works with any row type; columns come from its [DisplayName] / [Browsable(false)] attributes.
    public partial class PackagingPickerForm : Form
    {
        public PackagingPickerForm(System.Collections.IList matches, string search)
        {
            InitializeComponent();
            AppTheme.Apply(this);
            Text = $"Choose an item - \"{search}\" ({matches.Count} found)";
            resultsGrid.DataSource = matches;
        }

        /// <summary>The chosen row (any type).</summary>
        public object? SelectedItem { get; private set; }

        /// <summary>The chosen row when the list holds purchase packagings.</summary>
        public PackagingLookupModel? Selected => SelectedItem as PackagingLookupModel;

        private void Pick()
        {
            if (resultsGrid.CurrentRow?.DataBoundItem is not { } item) return;
            SelectedItem = item;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void selectButton_Click(object? sender, EventArgs e) => Pick();

        private void resultsGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) Pick();
        }

        private void resultsGrid_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;   // Enter would otherwise move to the next row
            Pick();
        }

        private void resultsGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewColumn column in resultsGrid.Columns)
            {
                if (column.ValueType != typeof(decimal)) continue;
                column.DefaultCellStyle.Format = "#,0.###";
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }
    }
}
