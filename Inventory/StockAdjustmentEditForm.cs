using System.ComponentModel;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // A new stock adjustment (none passed), a draft to go on with, or a posted one (only shown).
    // Scan a barcode or type a name + Enter: the item is added with the stock the system has now; type what was
    // counted on the shelf. Save draft keeps it to finish later (the stock does not change); Post sets the stock of
    // every item to its counted quantity, minus / plus what was sold / bought since it was counted.
    // Built in code, like the cashier's small windows.
    public class StockAdjustmentEditForm : Form
    {
        private readonly StockAdjustmentRepository _repository = new();
        private readonly StockAdjustmentModel? _adjustment;
        private readonly BindingList<StockAdjustmentLineModel> _lines = new();
        private readonly bool _readOnly;

        private readonly TextBox _reasonText, _searchText;
        private readonly DataGridView _grid;
        private readonly Label _summaryLabel;
        private readonly Button _saveButton, _postButton;
        private bool _dirty;

        /// <summary>The adjustment saved or posted (to select it in the list).</summary>
        public int? SavedId { get; private set; }

        public StockAdjustmentEditForm(StockAdjustmentModel? adjustment)
        {
            _adjustment = adjustment;
            _readOnly = adjustment is { IsDraft: false };

            Text = adjustment == null ? "New stock adjustment"
                : _readOnly ? $"Stock adjustment {adjustment.AdjustmentNo}"
                : $"Stock adjustment (draft) - {adjustment.Reason}";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(1000, 640);
            MinimumSize = new Size(760, 450);
            Font = new Font("Segoe UI", 10F);
            KeyPreview = true;

            // Reason, and what this adjustment is
            var header = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(16, 12, 16, 4) };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _reasonText = new TextBox { Width = 460, MaxLength = 200, Text = adjustment?.Reason ?? "Stock count", ReadOnly = _readOnly };
            _reasonText.TextChanged += (_, _) => _dirty = true;
            header.Controls.Add(new Label { Text = "Reason", AutoSize = true, Margin = new Padding(3, 6, 3, 3) }, 0, 0);
            header.Controls.Add(_reasonText, 1, 0);
            var infoLabel = new Label
            {
                AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(3, 4, 3, 6),
                Text = _readOnly
                    ? $"Posted {adjustment!.PostedAt ?? adjustment.AdjustmentDate:dd/MM/yyyy HH:mm} by {adjustment.UserName}. A posted adjustment cannot be changed: make a new one."
                    : "System qty = the stock when the item was added. Post sets each item to its counted quantity.",
            };
            header.Controls.Add(infoLabel, 1, 1);

            // Scan / search bar
            var searchBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, WrapContents = false, Padding = new Padding(16, 6, 16, 4), Visible = !_readOnly };
            _searchText = new TextBox { Width = 380, PlaceholderText = "Scan a barcode or type a name, then Enter", Margin = new Padding(3, 5, 3, 3) };
            _searchText.KeyDown += async (_, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                await FindAndAddAsync();
            };
            var addButton = new Button { Text = "Add", Size = new Size(80, 34) };
            addButton.Click += async (_, _) => await FindAndAddAsync();
            var removeButton = new Button { Text = "Remove", Size = new Size(100, 34), ForeColor = Color.Firebrick };
            removeButton.Click += (_, _) => RemoveSelected();
            searchBar.Controls.AddRange(new Control[] { _searchText, addButton, removeButton });

            _grid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                Dock = DockStyle.Fill,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
                ReadOnly = _readOnly,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                DataSource = _lines,
            };
            _grid.RowTemplate.Height = 32;
            _grid.DataBindingComplete += (_, _) => FormatGrid();
            _grid.CellFormatting += grid_CellFormatting;
            _grid.CellValueChanged += (_, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _lines.Count) return;
                _dirty = true;
                _grid.InvalidateRow(e.RowIndex);
                UpdateSummary();
            };
            _grid.DataError += (_, e) =>
            {
                MessageBox.Show(this, "Type a quantity of 0 or more.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            };
            _grid.CellValidating += (_, e) =>
            {
                if (_grid.Columns[e.ColumnIndex].DataPropertyName != nameof(StockAdjustmentLineModel.CountedQty)) return;
                if (decimal.TryParse(e.FormattedValue?.ToString(), out var qty) && qty >= 0) return;
                MessageBox.Show(this, "Type a quantity of 0 or more.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            };
            _grid.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Delete && !_readOnly && !_grid.IsCurrentCellInEditMode) { e.SuppressKeyPress = true; RemoveSelected(); }
            };
            _lines.ListChanged += (_, _) => UpdateSummary();

            var gridPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 0) };
            gridPanel.Controls.Add(_grid);

            var bottomPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12, 8, 12, 8) };
            var cancelButton = new Button { Text = _readOnly ? "Close" : "Cancel", Size = new Size(110, 38), DialogResult = DialogResult.Cancel };
            _postButton = new Button { Text = "Post", Size = new Size(130, 38), BackColor = AppTheme.Accent, ForeColor = Color.White, UseVisualStyleBackColor = false, Visible = !_readOnly };
            _postButton.Click += async (_, _) => await SaveAsync(post: true);
            _saveButton = new Button { Text = "Save draft", Size = new Size(130, 38), Visible = !_readOnly };
            _saveButton.Click += async (_, _) => await SaveAsync(post: false);
            _summaryLabel = new Label { AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(3, 11, 20, 3) };
            bottomPanel.Controls.AddRange(new Control[] { cancelButton, _postButton, _saveButton, _summaryLabel });

            // Docking goes from the last control added to the first: header, search bar, then the grid fills the rest
            Controls.AddRange(new Control[] { gridPanel, searchBar, header, bottomPanel });
            CancelButton = cancelButton;
            AppTheme.Apply(this);

            Load += async (_, _) => await LoadAsync();
            Shown += (_, _) => { if (!_readOnly) _searchText.Focus(); };
            FormClosing += StockAdjustmentEditForm_FormClosing;
        }

        private async Task LoadAsync()
        {
            if (_adjustment == null) { UpdateSummary(); return; }
            try
            {
                foreach (var line in await _repository.GetLinesAsync(_adjustment.AdjustmentId))
                {
                    if (!_readOnly) line.FollowCounted();   // a draft: the difference follows what is typed
                    _lines.Add(line);
                }
                _dirty = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the adjustment:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Barcode (exact) or name: one item is added at once, several are listed to pick from
        private async Task FindAndAddAsync()
        {
            var search = _searchText.Text.Trim();
            if (search.Length == 0) return;
            List<StockItemModel> found;
            try
            {
                found = (await _repository.FindItemsAsync(search)).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not search the items:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var item = found.Count switch
            {
                0 => null,
                1 => found[0],
                _ => PickItem(found),
            };
            if (found.Count == 0)
                MessageBox.Show(this, $"No stock item found for \"{search}\".\n\nOnly standard items hold stock (not bundles or models).",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (item == null) { _searchText.SelectAll(); _searchText.Focus(); return; }

            _searchText.Clear();
            AddLine(item);
        }

        // An item already in the list is not added twice: its counted cell is selected instead
        private void AddLine(StockItemModel item)
        {
            var line = _lines.FirstOrDefault(l => l.ProductId == item.ProductId);
            if (line == null)
            {
                line = new StockAdjustmentLineModel
                {
                    ProductId = item.ProductId,
                    ItemName = item.ItemName,
                    BaseUnit = item.BaseUnit,
                    QtyBefore = item.StockQty,
                    CountedQty = Math.Max(item.StockQty, 0),
                    UnitCost = item.AvgCost,
                    StockQty = item.StockQty,
                };
                line.FollowCounted();
                _lines.Add(line);
                _dirty = true;
            }

            var row = _grid.Rows[_lines.IndexOf(line)];
            _grid.CurrentCell = row.Cells[nameof(StockAdjustmentLineModel.CountedQty)];
            _grid.Focus();
            _grid.BeginEdit(true);
        }

        private StockItemModel? PickItem(List<StockItemModel> items)
        {
            using var picker = new Form
            {
                Text = "Pick the item",
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(560, 400),
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                Font = Font,
            };
            var list = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, ItemHeight = 24 };
            foreach (var i in items)
                list.Items.Add(new PickEntry(i));
            list.SelectedIndex = 0;
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8, 6, 8, 6) };
            var cancel = new Button { Text = "Cancel", Size = new Size(100, 36), DialogResult = DialogResult.Cancel };
            var ok = new Button { Text = "Add", Size = new Size(100, 36), DialogResult = DialogResult.OK };
            buttons.Controls.AddRange(new Control[] { cancel, ok });
            list.DoubleClick += (_, _) => { if (list.SelectedItem != null) picker.DialogResult = DialogResult.OK; };
            picker.Controls.Add(list);
            picker.Controls.Add(buttons);
            picker.AcceptButton = ok;
            picker.CancelButton = cancel;
            AppTheme.Apply(picker);
            return picker.ShowDialog(this) == DialogResult.OK && list.SelectedItem is PickEntry entry ? entry.Item : null;
        }

        private sealed record PickEntry(StockItemModel Item)
        {
            public override string ToString() =>
                $"{Item.ItemName}    (stock {Item.StockQty:#,0.###} {Item.BaseUnit}{(Item.Barcode != null ? ", " + Item.Barcode : string.Empty)})";
        }

        private void RemoveSelected()
        {
            var lines = _grid.SelectedCells.Cast<DataGridViewCell>().Select(c => c.RowIndex).Distinct()
                .Where(i => i >= 0 && i < _lines.Count).Select(i => _lines[i]).ToList();
            if (lines.Count == 0) return;
            foreach (var line in lines)
                _lines.Remove(line);
            _dirty = true;
        }

        private void UpdateSummary()
        {
            var changed = _lines.Count(l => l.Difference != 0);
            var value = _lines.Sum(l => l.ValueChange);
            _summaryLabel.Text = _lines.Count == 0
                ? (_readOnly ? "No items." : "No items yet.")
                : $"{_lines.Count} item(s), {changed} changed  ·  value change {value:#,0.##}";
        }

        private async Task SaveAsync(bool post)
        {
            if (!_grid.EndEdit()) return;
            var reason = _reasonText.Text.Trim();
            if (reason.Length == 0)
            {
                MessageBox.Show(this, "Type the reason of the adjustment (for example \"Stock count\").", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _reasonText.Focus();
                return;
            }
            if (_lines.Count == 0)
            {
                MessageBox.Show(this, "Add at least one item: scan its barcode or type its name.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _searchText.Focus();
                return;
            }
            if (post)
            {
                var changed = _lines.Count(l => l.Difference != 0);
                if (MessageBox.Show(this, $"Post this adjustment?\n\n{changed} of {_lines.Count} item(s) change stock, value change {_lines.Sum(l => l.ValueChange):#,0.##}.\n" +
                                          "A posted adjustment cannot be changed or deleted.",
                        Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }

            try
            {
                _saveButton.Enabled = _postButton.Enabled = false;
                UseWaitCursor = true;
                SavedId = await _repository.SaveAsync(_adjustment?.AdjustmentId, reason, _lines, post, AppConfig.CurrentUserId);
                _dirty = false;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save the adjustment:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _saveButton.Enabled = _postButton.Enabled = true;
                UseWaitCursor = false;
            }
        }

        private void StockAdjustmentEditForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_readOnly || !_dirty || DialogResult == DialogResult.OK || _lines.Count == 0) return;
            if (MessageBox.Show(this, "Close without saving? The items counted here are lost.\n\nSave draft keeps them to finish later.",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                e.Cancel = true;
        }

        private void FormatGrid()
        {
            foreach (DataGridViewColumn column in _grid.Columns)
            {
                column.ReadOnly = _readOnly || column.DataPropertyName != nameof(StockAdjustmentLineModel.CountedQty);
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (column.ValueType != typeof(decimal)) continue;
                column.DefaultCellStyle.Format = column.DataPropertyName is nameof(StockAdjustmentLineModel.UnitCost) or nameof(StockAdjustmentLineModel.ValueChange)
                    ? "#,0.##" : "#,0.###";
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.FillWeight = 60;
            }
            if (_grid.Columns[nameof(StockAdjustmentLineModel.ItemName)] is { } name) name.FillWeight = 220;
            if (_grid.Columns[nameof(StockAdjustmentLineModel.BaseUnit)] is { } unit) unit.FillWeight = 45;
            if (_grid.Columns[nameof(StockAdjustmentLineModel.CountedQty)] is { } counted && !_readOnly)
            {
                counted.DefaultCellStyle.BackColor = Color.FromArgb(255, 251, 230);
                counted.DefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
            }
        }

        // Stock lost in red, stock found in green
        private void grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _lines.Count || e.CellStyle == null) return;
            var column = _grid.Columns[e.ColumnIndex].DataPropertyName;
            if (column is not (nameof(StockAdjustmentLineModel.Difference) or nameof(StockAdjustmentLineModel.ValueChange))) return;
            var difference = _lines[e.RowIndex].Difference;
            if (difference != 0) e.CellStyle.ForeColor = difference < 0 ? Color.Firebrick : Color.SeaGreen;
        }
    }
}
