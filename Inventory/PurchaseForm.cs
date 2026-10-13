using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Purchase invoices on top; the item lines of the selected invoice below
    public partial class PurchaseForm : Form
    {
        private readonly PurchaseRepository _purchaseRepository = new();
        private string? _search;
        private int _page = 1;
        private int _pageSize = 50;
        private bool _loading;
        private bool _reloadPending;
        private int _linesRequest;   // ignores line results that arrive after another invoice was selected

        public PurchaseForm()
        {
            InitializeComponent();
            AppTheme.Apply(this);
            pageSizeCombo.SelectedItem = _pageSize;
        }

        private async void PurchaseForm_Load(object? sender, EventArgs e)
        {
            await LoadInvoicesAsync();
        }

        #region Invoices

        private async Task LoadInvoicesAsync()
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

                var result = await _purchaseRepository.GetPageAsync(_page, _pageSize, _search);
                if (result.Items.Count == 0 && _page > 1)
                {
                    _page = result.TotalCount == 0 ? 1 : result.TotalPages;
                    result = await _purchaseRepository.GetPageAsync(_page, _pageSize, _search);
                }

                invoicesGrid.DataSource = result.Items;
                invoicesTitle.Text = _search == null
                    ? $"Purchase invoices ({result.TotalCount})"
                    : $"Purchase invoices - \"{_search}\" ({result.TotalCount} found)";
                pageLabel.Text = $"Page {result.Page} of {result.TotalPages}";
                prevButton.Enabled = result.HasPrevious;
                nextButton.Enabled = result.HasNext;

                if (result.Items.Count == 0) ShowLines(null, Array.Empty<PurchaseLineModel>());
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load purchase invoices:\n" + ex.Message, "Purchases",
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
                await LoadInvoicesAsync();
            }
        }

        private async void invoicesGrid_SelectionChanged(object? sender, EventArgs e)
        {
            await LoadLinesAsync(invoicesGrid.CurrentRow?.DataBoundItem as PurchaseInvoiceModel);
        }

        private async void newPurchaseButton_Click(object? sender, EventArgs e)
        {
            await OpenPurchaseAsync(null);
        }

        private async void openPurchaseButton_Click(object? sender, EventArgs e)
        {
            if (invoicesGrid.CurrentRow?.DataBoundItem is not PurchaseInvoiceModel invoice)
            {
                MessageBox.Show(this, "Select a purchase first.", "Purchases", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            await OpenPurchaseAsync(invoice.PurchaseId);
        }

        private async void invoicesGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && invoicesGrid.Rows[e.RowIndex].DataBoundItem is PurchaseInvoiceModel invoice)
                await OpenPurchaseAsync(invoice.PurchaseId);
        }

        // New (null) or existing purchase; drafts can be edited, posted ones open read-only
        private async Task OpenPurchaseAsync(int? purchaseId)
        {
            using var form = new PurchaseEditForm(purchaseId);
            if (form.ShowDialog(this) != DialogResult.OK) return;

            await LoadInvoicesAsync();
            SelectInvoice(form.SavedPurchaseId);
        }

        private void SelectInvoice(int purchaseId)
        {
            foreach (DataGridViewRow row in invoicesGrid.Rows)
            {
                if (row.DataBoundItem is not PurchaseInvoiceModel invoice || invoice.PurchaseId != purchaseId) continue;
                invoicesGrid.CurrentCell = row.Cells.Cast<DataGridViewCell>().First(c => c.Visible);
                return;
            }
        }

        private async void prevButton_Click(object? sender, EventArgs e)
        {
            if (_page <= 1) return;
            _page--;
            await LoadInvoicesAsync();
        }

        private async void nextButton_Click(object? sender, EventArgs e)
        {
            _page++;
            await LoadInvoicesAsync();
        }

        private async void pageSizeCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (pageSizeCombo.SelectedItem is not int size || size == _pageSize) return;
            _pageSize = size;
            _page = 1;
            await LoadInvoicesAsync();
        }

        #endregion

        #region Lines

        private async Task LoadLinesAsync(PurchaseInvoiceModel? invoice)
        {
            var request = ++_linesRequest;
            if (invoice == null)
            {
                ShowLines(null, Array.Empty<PurchaseLineModel>());
                return;
            }

            try
            {
                var lines = (await _purchaseRepository.GetLinesAsync(invoice.PurchaseId)).ToList();
                if (request != _linesRequest) return;   // another invoice was selected meanwhile
                ShowLines(invoice, lines);
            }
            catch (Exception ex)
            {
                if (request != _linesRequest) return;
                MessageBox.Show(this, "Could not load the invoice items:\n" + ex.Message, "Purchases",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowLines(PurchaseInvoiceModel? invoice, IReadOnlyList<PurchaseLineModel> lines)
        {
            linesGrid.DataSource = lines;
            linesTitle.Text = invoice == null
                ? "Items"
                : $"Items in {invoice.PurchaseNo ?? "#" + invoice.PurchaseId} - {invoice.SupplierName}   " +
                  $"({lines.Count} line(s), total {lines.Sum(l => l.LineTotal):#,0.##})";
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
                e.SuppressKeyPress = true;
                searchTimer.Stop();
                await ApplySearchAsync();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                searchBox.Clear();
            }
        }

        private async Task ApplySearchAsync()
        {
            var search = string.IsNullOrWhiteSpace(searchBox.Text) ? null : searchBox.Text.Trim();
            if (search == _search) return;
            _search = search;
            _page = 1;
            await LoadInvoicesAsync();
        }

        #endregion

        // Number and date formats for both grids (columns are generated from the models)
        private void grid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (sender is not DataGridView grid) return;
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.ValueType == null) continue;
                var type = Nullable.GetUnderlyingType(column.ValueType) ?? column.ValueType;
                if (type == typeof(decimal))
                {
                    column.DefaultCellStyle.Format = "#,0.##";
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (type == typeof(DateTime))
                {
                    column.DefaultCellStyle.Format = "yyyy-MM-dd";
                }
            }
        }
    }
}
