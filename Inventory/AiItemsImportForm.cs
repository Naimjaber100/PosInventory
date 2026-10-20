using System.Globalization;
using Inventory.Ai;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Add item > "Read from photo (AI)" when the photo shows a list of items: one row per item read
    // (name, barcode, sale price, cost, quantity, size), prices already in the primary currency.
    // The user corrects the rows, unticks the ones to skip, and creates them all at once: standard items with
    // one packaging (the base unit), in the category and base unit chosen on the item form.
    // Built in code, like the cashier's small windows.
    public class AiItemsImportForm : Form
    {
        private readonly ProductRepository _productRepository = new();
        private readonly int? _categoryId;
        private readonly string _baseUnit;

        private readonly DataGridView _grid;
        private readonly DataGridViewCheckBoxColumn _useColumn;
        private readonly DataGridViewTextBoxColumn _nameColumn, _barcodeColumn, _priceColumn, _costColumn, _qtyColumn, _sizeColumn, _noteColumn;
        private readonly Label _summaryLabel;
        private readonly Button _createButton;

        /// <summary>How many items were created (the item list is refreshed when more than 0).</summary>
        public int CreatedCount { get; private set; }

        /// <param name="items">The items read, with SalePrice / Cost in the primary currency.</param>
        public AiItemsImportForm(IEnumerable<AiItem> items, int? categoryId, string categoryName, string baseUnit)
        {
            _categoryId = categoryId;
            _baseUnit = baseUnit;

            Text = "New items from a photo (AI)";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(1080, 620);
            MinimumSize = new Size(820, 420);
            Font = new Font("Segoe UI", 10F);

            var info = new Label
            {
                Dock = DockStyle.Top,
                Height = 56,
                Padding = new Padding(14, 8, 14, 0),
                ForeColor = Color.FromArgb(90, 98, 108),
                Text = $"Check each row: correct the name, barcode, prices or quantity, untick a row to skip it.\n" +
                       $"Every item is created as a standard item, unit \"{baseUnit}\", category {categoryName}. Prices are in the primary currency.",
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
            };
            _useColumn = new DataGridViewCheckBoxColumn { HeaderText = "Create", FillWeight = 45 };
            _nameColumn = TextColumn("Name", 200);
            _barcodeColumn = TextColumn("Barcode", 110);
            _priceColumn = NumberColumn("Sale price");
            _costColumn = NumberColumn("Cost");
            _qtyColumn = NumberColumn("Qty");
            _sizeColumn = TextColumn("Size", 60);
            _noteColumn = TextColumn("Note", 150);
            _noteColumn.ReadOnly = true;
            _noteColumn.DefaultCellStyle.ForeColor = Color.Firebrick;
            _grid.Columns.AddRange(_useColumn, _nameColumn, _barcodeColumn, _priceColumn, _costColumn, _qtyColumn, _sizeColumn, _noteColumn);
            _grid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (_grid.CurrentCell is DataGridViewCheckBoxCell) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _grid.CellValueChanged += (_, _) => UpdateSummary();
            _grid.DataError += (_, e) => e.ThrowException = false;

            foreach (var item in items)
            {
                var barcode = new string((item.Barcode ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
                _grid.Rows.Add(!string.IsNullOrWhiteSpace(item.ItemName), item.ItemName?.Trim(), barcode.Length >= 6 ? barcode : null,
                    item.SalePrice > 0 ? item.SalePrice : null, item.Cost > 0 ? item.Cost : null, item.Quantity > 0 ? item.Quantity : null,
                    item.Size?.Trim(), string.IsNullOrWhiteSpace(item.ItemName) ? "no name read" : null);
            }

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12, 10, 12, 8), BackColor = Color.White };
            var cancel = new Button { Text = "Cancel", Size = new Size(110, 40), DialogResult = DialogResult.Cancel };
            _createButton = new Button
            {
                Size = new Size(190, 40),
                BackColor = Color.FromArgb(28, 110, 92),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
            };
            _createButton.FlatAppearance.BorderSize = 0;
            _createButton.Click += createButton_Click;
            _summaryLabel = new Label { AutoSize = true, Margin = new Padding(3, 11, 16, 3), ForeColor = Color.FromArgb(90, 98, 108) };
            bottom.Controls.AddRange(new Control[] { cancel, _createButton, _summaryLabel });

            Controls.Add(_grid);
            Controls.Add(info);
            Controls.Add(bottom);
            CancelButton = cancel;
            AppTheme.Apply(this);
            Load += async (_, _) => await MarkExistingBarcodesAsync();
            UpdateSummary();
        }

        // A barcode that already belongs to an item: noted and unticked (the database would refuse it)
        private async Task MarkExistingBarcodesAsync()
        {
            var purchases = new PurchaseRepository();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (CellText(row, _barcodeColumn) is not { } barcode) continue;
                try
                {
                    var existing = (await purchases.FindPackagingAsync(barcode, 5)).FirstOrDefault(p => p.Barcode == barcode);
                    if (existing == null) continue;
                    row.Cells[_noteColumn.Index].Value = $"barcode already used by \"{existing.ItemName}\"";
                    row.Cells[_useColumn.Index].Value = false;
                }
                catch { /* only a warning; saving checks it again */ }
            }
        }

        private IEnumerable<DataGridViewRow> CheckedRows() =>
            _grid.Rows.Cast<DataGridViewRow>().Where(r => r.Cells[_useColumn.Index].Value is true);

        private void UpdateSummary()
        {
            var count = CheckedRows().Count();
            _summaryLabel.Text = $"{_grid.Rows.Count} item(s) read · {count} to create";
            _createButton.Text = count == 1 ? "Create 1 item" : $"Create {count} items";
            _createButton.Enabled = count > 0;
        }

        private async void createButton_Click(object? sender, EventArgs e)
        {
            if (_grid.IsCurrentCellInEditMode) _grid.EndEdit();
            var rows = CheckedRows().ToList();

            // Check every row first: nothing is created while one is wrong
            string? problem = null;
            foreach (var row in rows)
            {
                row.Cells[_noteColumn.Index].Value = null;
                var note = CellText(row, _nameColumn) == null ? "type a name"
                    : CellText(row, _barcodeColumn) is { } b && !b.All(char.IsAsciiDigit) ? "the barcode can only have digits"
                    : Number(row, _priceColumn) < 0 || Number(row, _costColumn) < 0 || Number(row, _qtyColumn) < 0 ? "numbers cannot be negative"
                    : rows.Any(r => r != row && CellText(r, _barcodeColumn) is { } other && other == CellText(row, _barcodeColumn)) ? "barcode used twice in this list"
                    : null;
                if (note == null) continue;
                row.Cells[_noteColumn.Index].Value = note;
                problem ??= $"Row {row.Index + 1}: {note}.";
            }
            if (problem != null)
            {
                MessageBox.Show(this, problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _createButton.Enabled = false;
            UseWaitCursor = true;
            var failed = 0;
            try
            {
                foreach (var row in rows)
                {
                    var product = new ProductModel
                    {
                        Name = CellText(row, _nameColumn)!,
                        CategoryId = _categoryId,
                        ProductType = 1,
                        Size = CellText(row, _sizeColumn),
                        BaseUnit = _baseUnit,
                        AvgCost = Number(row, _costColumn) ?? 0,
                        IsActive = true,
                    };
                    var packaging = new PackagingModel
                    {
                        Name = _baseUnit,
                        Factor = 1,
                        Barcode = CellText(row, _barcodeColumn),
                        SalePrice = Number(row, _priceColumn) ?? 0,
                        IsDefault = true,
                    };
                    try
                    {
                        await _productRepository.AddAsync(product, Number(row, _qtyColumn) ?? 0, AppConfig.CurrentUserId, new[] { packaging });
                        CreatedCount++;
                        _grid.Rows.Remove(row);
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        row.Cells[_noteColumn.Index].Value = ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message;
                    }
                }
            }
            finally
            {
                UseWaitCursor = false;
                UpdateSummary();
            }

            if (failed == 0)
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }
            MessageBox.Show(this, $"{CreatedCount} item(s) created. {failed} could not be created: see the note on each row, correct it and press Create again.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static string? CellText(DataGridViewRow row, DataGridViewColumn column) =>
            row.Cells[column.Index].Value?.ToString()?.Trim() is { Length: > 0 } text ? text : null;

        private static decimal? Number(DataGridViewRow row, DataGridViewColumn column) => row.Cells[column.Index].Value switch
        {
            decimal d => d,
            double d => (decimal)d,
            string s when decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out var d) => d,
            string s when decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) => d,
            _ => null,
        };

        private static DataGridViewTextBoxColumn TextColumn(string header, float weight) =>
            new() { HeaderText = header, FillWeight = weight, SortMode = DataGridViewColumnSortMode.NotSortable };

        private static DataGridViewTextBoxColumn NumberColumn(string header)
        {
            var column = TextColumn(header, 70);
            column.DefaultCellStyle.Format = "#,0.##";
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }
    }
}
