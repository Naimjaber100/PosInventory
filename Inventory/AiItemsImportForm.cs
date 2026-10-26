using System.Globalization;
using Inventory.Ai;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Add item > "Read from photo (AI)" when the photo shows a list of items (a price list, or a supplier's invoice):
    // one row per item read, prices already in the primary currency. The user corrects the rows, unticks the ones to
    // skip, and creates them all at once as standard items, in the category and base unit chosen on the item form.
    //   Pack = pieces in the packaging the row is listed in ("KINDER T4 *20", unit box -> 20). With a pack of 2 or more
    //   the item gets two packagings: the piece (base unit, default) and that packaging (contains Pack pieces);
    //   Cost is per packaging, so the item's cost is Cost / Pack, and Qty packagings make Qty x Pack pieces in stock.
    //   Barcode on = which of the two the barcode is printed on (usually the piece).
    // What the AI may have misread is marked: a barcode whose check digit is wrong, or Qty x Cost that is not the
    // Total of the row. Built in code, like the cashier's small windows.
    public class AiItemsImportForm : Form
    {
        private const string OnPiece = "piece", OnPackage = "package";
        private static readonly Color BadCell = Color.FromArgb(255, 226, 222);
        private static readonly string[] PieceWords = { "piece", "pieces", "pc", "pcs", "unit", "each", "حبة", "قطعة" };

        private readonly ProductRepository _productRepository = new();
        private readonly int? _categoryId;
        private readonly string _baseUnit;

        private readonly DataGridView _grid;
        private readonly DataGridViewCheckBoxColumn _useColumn;
        private readonly DataGridViewTextBoxColumn _nameColumn, _barcodeColumn, _unitColumn, _packColumn, _qtyColumn, _costColumn, _totalColumn,
            _priceColumn, _sizeColumn, _noteColumn;
        private readonly DataGridViewComboBoxColumn _barcodeOnColumn;
        private readonly Label _summaryLabel;
        private readonly Button _createButton;
        private bool _checking;

        /// <summary>How many items were created (the item list is refreshed when more than 0).</summary>
        public int CreatedCount { get; private set; }

        /// <param name="items">The items read, with SalePrice / Cost / LineTotal in the primary currency.</param>
        public AiItemsImportForm(IEnumerable<AiItem> items, int? categoryId, string categoryName, string baseUnit)
        {
            _categoryId = categoryId;
            _baseUnit = baseUnit;

            Text = "New items from a photo (AI)";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(1280, 660);
            MinimumSize = new Size(960, 420);
            Font = new Font("Segoe UI", 10F);

            var info = new Label
            {
                Dock = DockStyle.Top,
                Height = 74,
                Padding = new Padding(14, 8, 14, 0),
                ForeColor = Color.FromArgb(90, 98, 108),
                Text = $"Check each row (red = the AI may have misread it: a barcode whose last digit does not match, or Qty x Cost ≠ Total). " +
                       $"Untick a row to skip it.\nPack = pieces in the unit it is listed in (box of 20 -> 20): the item gets a \"{baseUnit}\" packaging " +
                       $"and that unit, its cost is Cost ÷ Pack and its stock Qty x Pack.  Category {categoryName}. Prices are in the primary currency.",
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
            _useColumn = new DataGridViewCheckBoxColumn { HeaderText = "Create", FillWeight = 40 };
            _nameColumn = TextColumn("Name", 190);
            _barcodeColumn = TextColumn("Barcode", 115);
            _barcodeOnColumn = new DataGridViewComboBoxColumn
            {
                HeaderText = "Barcode on", FillWeight = 65, FlatStyle = FlatStyle.Flat, DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            };
            _barcodeOnColumn.Items.AddRange(OnPiece, OnPackage);
            _unitColumn = TextColumn("Unit", 55);
            _packColumn = NumberColumn("Pack", 45);
            _qtyColumn = NumberColumn("Qty", 50);
            _costColumn = NumberColumn("Cost (unit)", 70);
            _totalColumn = NumberColumn("Total", 70);
            _priceColumn = NumberColumn("Sale price / piece", 75);
            _sizeColumn = TextColumn("Size", 55);
            _noteColumn = TextColumn("Note", 170);
            _noteColumn.ReadOnly = true;
            _noteColumn.DefaultCellStyle.ForeColor = Color.Firebrick;
            _grid.Columns.AddRange(_useColumn, _nameColumn, _barcodeColumn, _barcodeOnColumn, _unitColumn, _packColumn, _qtyColumn,
                _costColumn, _totalColumn, _priceColumn, _sizeColumn, _noteColumn);
            _grid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (_grid.CurrentCell is DataGridViewCheckBoxCell or DataGridViewComboBoxCell) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _grid.CellValueChanged += (_, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex != _noteColumn.Index && e.ColumnIndex != _useColumn.Index) Check(_grid.Rows[e.RowIndex]);
                UpdateSummary();
            };
            _grid.DataError += (_, e) => e.ThrowException = false;

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

            // The rows last: filling them raises CellValueChanged, which updates the summary and the Create button above
            foreach (var item in items)
            {
                var barcode = new string((item.Barcode ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
                var unit = item.Packaging?.Trim();
                var byPiece = unit == null || PieceWords.Contains(unit, StringComparer.OrdinalIgnoreCase);
                var pack = !byPiece && item.PackSize is > 1 and var p ? p : (decimal?)null;
                object?[] cells =
                {
                    !string.IsNullOrWhiteSpace(item.ItemName), item.ItemName?.Trim(), barcode.Length >= 4 ? barcode : null,
                    OnPiece, byPiece ? null : unit, pack, item.Quantity > 0 ? item.Quantity : null, item.Cost > 0 ? item.Cost : null,
                    item.LineTotal > 0 ? item.LineTotal : null, item.SalePrice > 0 ? item.SalePrice : null, item.Size?.Trim(), null,
                };
                var index = _grid.Rows.Add(cells!);   // an empty cell is null
                Check(_grid.Rows[index]);
            }

            Controls.Add(_grid);
            Controls.Add(info);
            Controls.Add(bottom);
            CancelButton = cancel;
            AppTheme.Apply(this);
            Load += async (_, _) => await MarkExistingBarcodesAsync();
            UpdateSummary();
        }

        #region Checks

        // What the AI may have misread, on this row: marked red, and written in Note (with "barcode already used", kept in Tag)
        private void Check(DataGridViewRow row)
        {
            if (_checking) return;
            _checking = true;
            try
            {
                var notes = new List<string>();
                var barcodeProblem = CellText(row, _barcodeColumn) is { } barcode ? BarcodeProblem(barcode) : null;
                var badBarcode = barcodeProblem != null;
                if (badBarcode) notes.Add(barcodeProblem!);

                var qty = Number(row, _qtyColumn);
                var cost = Number(row, _costColumn);
                var total = Number(row, _totalColumn);
                var badTotal = qty is > 0 && cost is > 0 && total is > 0
                               && Math.Abs(qty.Value * cost.Value - total.Value) > Math.Max(0.011m, total.Value * 0.005m);
                if (badTotal) notes.Add($"{qty:0.###} x {cost:#,0.##} = {qty * cost:#,0.##}, not {total:#,0.##}: check Qty and Cost");

                if (row.Tag is string used) notes.Add(used);
                row.Cells[_noteColumn.Index].Value = notes.Count == 0 ? null : string.Join("; ", notes);
                row.Cells[_barcodeColumn.Index].Style.BackColor = badBarcode ? BadCell : Color.Empty;
                foreach (var column in new[] { _qtyColumn, _costColumn, _totalColumn })
                    row.Cells[column.Index].Style.BackColor = badTotal ? BadCell : Color.Empty;
            }
            finally
            {
                _checking = false;
            }
        }

        // EAN-8, UPC-A, EAN-13 and GTIN-14 end with a check digit: a wrong one means a digit was misread.
        // Other lengths are the shop's or the supplier's own codes: not checked.
        private static string? BarcodeProblem(string barcode)
        {
            if (!barcode.All(char.IsAsciiDigit)) return "the barcode can only have digits";
            if (barcode.Length is not (8 or 12 or 13 or 14)) return null;
            return Barcodes.CheckDigit(barcode[..^1]) == barcode[^1] - '0' ? null : "barcode's last digit does not match: a digit is misread";
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
                    row.Tag = $"barcode already used by \"{existing.ItemName}\"";
                    row.Cells[_useColumn.Index].Value = false;
                    Check(row);
                }
                catch { /* only a warning; saving checks it again */ }
            }
            UpdateSummary();
        }

        #endregion

        private IEnumerable<DataGridViewRow> CheckedRows() =>
            _grid.Rows.Cast<DataGridViewRow>().Where(r => r.Cells[_useColumn.Index].Value is true);

        private void UpdateSummary()
        {
            var count = CheckedRows().Count();
            var marked = _grid.Rows.Cast<DataGridViewRow>().Count(r => r.Cells[_noteColumn.Index].Value != null);
            _summaryLabel.Text = $"{_grid.Rows.Count} item(s) read · {count} to create" + (marked > 0 ? $" · {marked} to check (red)" : string.Empty);
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
                var pack = Number(row, _packColumn);
                var note = CellText(row, _nameColumn) == null ? "type a name"
                    : CellText(row, _barcodeColumn) is { } b && !b.All(char.IsAsciiDigit) ? "the barcode can only have digits"
                    : Number(row, _priceColumn) < 0 || Number(row, _costColumn) < 0 || Number(row, _qtyColumn) < 0 ? "numbers cannot be negative"
                    : pack is { } p && (p < 1 || p % 1 != 0) ? "Pack is a whole number of pieces (blank or 1 = sold by piece)"
                    : pack > 1 && string.Equals(UnitName(row), _baseUnit, StringComparison.OrdinalIgnoreCase) ? $"the Unit cannot be \"{_baseUnit}\" when Pack is more than 1"
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

            var marked = rows.Count(r => r.Cells[_noteColumn.Index].Value != null);
            if (marked > 0 && MessageBox.Show(this, $"{marked} row(s) are still marked red (the AI may have misread a barcode or a cost).\n\nCreate them anyway?",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _createButton.Enabled = false;
            UseWaitCursor = true;
            var failed = 0;
            try
            {
                foreach (var row in rows)
                {
                    var pack = Number(row, _packColumn) is > 1 and var p ? p : 1;
                    var barcode = CellText(row, _barcodeColumn);
                    var onPackage = pack > 1 && Equals(row.Cells[_barcodeOnColumn.Index].Value, OnPackage);
                    var piecePrice = Number(row, _priceColumn) ?? 0;

                    var product = new ProductModel
                    {
                        Name = CellText(row, _nameColumn)!,
                        CategoryId = _categoryId,
                        ProductType = 1,
                        Size = CellText(row, _sizeColumn),
                        BaseUnit = _baseUnit,
                        AvgCost = Math.Round((Number(row, _costColumn) ?? 0) / pack, 4),   // per piece
                        IsActive = true,
                    };
                    var packagings = new List<PackagingModel>
                    {
                        new() { Name = _baseUnit, Factor = 1, Barcode = onPackage ? null : barcode, SalePrice = piecePrice, IsDefault = true },
                    };
                    if (pack > 1)
                        packagings.Add(new PackagingModel
                        {
                            Name = UnitName(row),
                            Factor = pack,
                            Barcode = onPackage ? barcode : null,
                            SalePrice = Math.Round(piecePrice * pack, 2),
                        });
                    try
                    {
                        await _productRepository.AddAsync(product, (Number(row, _qtyColumn) ?? 0) * pack, AppConfig.CurrentUserId, packagings);
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

        // The packaging of a row with a pack: its Unit, "box" when none was read
        private string UnitName(DataGridViewRow row) => CellText(row, _unitColumn) ?? "box";

        private static string? CellText(DataGridViewRow row, DataGridViewColumn column) =>
            row.Cells[column.Index].Value?.ToString()?.Trim() is { Length: > 0 } text ? text : null;

        private static decimal? Number(DataGridViewRow row, DataGridViewColumn column) => row.Cells[column.Index].Value switch
        {
            decimal d => d,
            double d => (decimal)d,
            int i => i,
            string s when decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out var d) => d,
            string s when decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) => d,
            _ => null,
        };

        private static DataGridViewTextBoxColumn TextColumn(string header, float weight) =>
            new() { HeaderText = header, FillWeight = weight, SortMode = DataGridViewColumnSortMode.NotSortable };

        private static DataGridViewTextBoxColumn NumberColumn(string header, float weight)
        {
            var column = TextColumn(header, weight);
            column.DefaultCellStyle.Format = "#,0.##";
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }
    }
}
