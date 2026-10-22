using System.ComponentModel;
using Inventory.Models;
using Inventory.Repositories;
using Inventory.Scales;

namespace Inventory
{
    // Settings > Label scales: the shop's label scales (any brand: each has a model / driver, Scales/ScaleDrivers.cs)
    // and the items loaded in each one, with their PLU, price and hot key (the key on the scale's keyboard).
    // PLU and hot key are typed in the list; the price is the item's price (change it on the item).
    // "Send changed" sends the new items and those whose name, PLU, price or hot key changed since the last send;
    // "Send all" sends every item again. Built in code, like the cashier's small windows.
    public class ScalesForm : Form
    {
        private readonly ScaleRepository _scaleRepository = new();
        private readonly ComboBox _scaleCombo;
        private readonly Label _modelLabel, _countLabel;
        private readonly DataGridView _grid;
        private readonly Button _editScaleButton, _deleteScaleButton, _addItemsButton, _removeButton, _sendChangedButton, _sendAllButton;
        private BindingList<ScaleItemModel> _items = new();
        private bool _loadingScales, _busy;

        public ScalesForm()
        {
            Text = "Label scales";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1100, 640);
            MinimumSize = new Size(860, 420);
            Font = new Font("Segoe UI", 9F);
            KeyPreview = true;

            // Line 1: the scale
            var scalePanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(8, 8, 8, 6), WrapContents = false };
            _scaleCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), Margin = new Padding(3, 3, 8, 3) };
            _scaleCombo.Format += (_, e) => e.Value = e.ListItem is ScaleModel s ? s.IsActive ? s.Name : s.Name + "  (not in use)" : e.Value;
            _scaleCombo.SelectedIndexChanged += async (_, _) => { if (!_loadingScales) await LoadItemsAsync(); };
            var addScaleButton = new Button { Text = "+ Add scale", Size = new Size(110, 32) };
            addScaleButton.Click += async (_, _) => await EditScaleAsync(null);
            _editScaleButton = new Button { Text = "Edit scale", Size = new Size(100, 32) };
            _editScaleButton.Click += async (_, _) => { if (SelectedScale is { } s) await EditScaleAsync(s); };
            _deleteScaleButton = new Button { Text = "Delete scale", Size = new Size(110, 32), ForeColor = Color.Firebrick };
            _deleteScaleButton.Click += async (_, _) => await DeleteScaleAsync();
            _modelLabel = new Label { AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(12, 9, 3, 0) };
            scalePanel.Controls.AddRange(new Control[] { new Label { Text = "Scale", AutoSize = true, Margin = new Padding(3, 9, 6, 0) }, _scaleCombo, addScaleButton, _editScaleButton, _deleteScaleButton, _modelLabel });

            // Line 2: its items
            var toolPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(8, 6, 8, 6), WrapContents = false };
            _addItemsButton = new Button { Text = "+ Add items (Ins)", Size = new Size(150, 36), BackColor = AppTheme.Accent, ForeColor = Color.White, UseVisualStyleBackColor = false };
            _addItemsButton.Click += async (_, _) => await AddItemsAsync();
            _removeButton = new Button { Text = "Remove", Size = new Size(90, 36), ForeColor = Color.Firebrick };
            _removeButton.Click += async (_, _) => await RemoveItemsAsync();
            _sendChangedButton = new Button { Text = "Send changed to scale", Size = new Size(180, 36), BackColor = Color.FromArgb(29, 78, 160), ForeColor = Color.White, UseVisualStyleBackColor = false, Margin = new Padding(24, 3, 3, 3) };
            _sendChangedButton.Click += async (_, _) => await SendAsync(changedOnly: true);
            _sendAllButton = new Button { Text = "Send all", Size = new Size(100, 36) };
            _sendAllButton.Click += async (_, _) => await SendAsync(changedOnly: false);
            _countLabel = new Label { AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(16, 11, 3, 0) };
            toolPanel.Controls.AddRange(new Control[] { _addItemsButton, _removeButton, _sendChangedButton, _sendAllButton, _countLabel });

            _grid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                Dock = DockStyle.Fill,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            };
            _grid.RowTemplate.Height = 32;
            _grid.Columns.AddRange(
                Column(nameof(ScaleItemModel.HotKey), "Hot key (position)", 60, editable: true, right: true),
                Column(nameof(ScaleItemModel.ScaleCode), "PLU", 60, editable: true),
                Column(nameof(ScaleItemModel.ItemName), "Item", 220),
                Column(nameof(ScaleItemModel.Price), "Price", 80, right: true),
                Column(nameof(ScaleItemModel.BaseUnit), "Sold by", 60),
                Column(nameof(ScaleItemModel.Status), "Status", 60),
                Column(nameof(ScaleItemModel.SentAt), "Last sent", 90));
            _grid.CellFormatting += grid_CellFormatting;
            _grid.CellParsing += grid_CellParsing;
            _grid.CellValueChanged += async (_, e) => { if (e.RowIndex >= 0) await SaveRowAsync(e.RowIndex); };
            _grid.DataError += (_, e) => e.ThrowException = false;
            _grid.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Delete && !_grid.IsCurrentCellInEditMode) { e.SuppressKeyPress = true; _removeButton.PerformClick(); }
            };

            var bottomPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8, 6, 8, 6) };
            var closeButton = new Button { Text = "Close", Size = new Size(100, 36) };
            closeButton.Click += (_, _) => Close();
            var hint = new Label
            {
                AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(3, 4, 20, 3), MaximumSize = new Size(880, 0),
                Text = "Type the hot key and the PLU in the list (F2 or just type). The price is the item's sale price: change it on the item, "
                       + "the item shows \"Changed\" here, then press Send changed.",
            };
            bottomPanel.Controls.AddRange(new Control[] { closeButton, hint });

            Controls.Add(_grid);
            Controls.Add(toolPanel);
            Controls.Add(scalePanel);
            Controls.Add(bottomPanel);
            AppTheme.Apply(this);

            Load += async (_, _) => await LoadScalesAsync();
            KeyDown += (_, e) =>
            {
                if (_grid.IsCurrentCellInEditMode) return;
                if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; Close(); }
                else if (e.KeyCode == Keys.Insert) { e.SuppressKeyPress = true; _addItemsButton.PerformClick(); }
            };
        }

        private ScaleModel? SelectedScale => _scaleCombo.SelectedItem as ScaleModel;

        private static DataGridViewTextBoxColumn Column(string property, string header, float weight, bool editable = false, bool right = false)
        {
            var column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = property, Name = property, HeaderText = header, FillWeight = weight, ReadOnly = !editable,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            };
            if (right) column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if (editable) column.DefaultCellStyle.BackColor = Color.FromArgb(255, 252, 235);
            return column;
        }

        private static string ErrorText(Exception ex) => ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message;

        #region Scales

        private async Task LoadScalesAsync(int? selectId = null)
        {
            selectId ??= SelectedScale?.ScaleId;
            try
            {
                var scales = (await _scaleRepository.GetAllAsync()).ToList();
                _loadingScales = true;
                _scaleCombo.Items.Clear();
                _scaleCombo.Items.AddRange(scales.Cast<object>().ToArray());
                _scaleCombo.SelectedItem = scales.FirstOrDefault(s => s.ScaleId == selectId) ?? scales.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the scales:\n" + ErrorText(ex) + "\n\nWas Database/scale_functions.sql applied?",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _loadingScales = false;
            }
            await LoadItemsAsync();
        }

        private async Task EditScaleAsync(ScaleModel? scale)
        {
            using var form = new ScaleEditForm(scale);
            if (form.ShowDialog(this) != DialogResult.OK) return;
            await LoadScalesAsync(form.SavedId);
        }

        private async Task DeleteScaleAsync()
        {
            if (SelectedScale is not { } scale) return;
            if (MessageBox.Show(this, $"Delete the scale \"{scale.Name}\" and its list of {scale.ItemCount} item(s)?\n\n" +
                                      "The items keep their PLU. What is already in the scale stays in it.",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                await _scaleRepository.DeleteAsync(scale.ScaleId);
                await LoadScalesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not delete the scale:\n" + ErrorText(ex), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Items

        private async Task LoadItemsAsync(int? selectProductId = null)
        {
            var scale = SelectedScale;
            selectProductId ??= (_grid.CurrentRow?.DataBoundItem as ScaleItemModel)?.ProductId;
            var firstRow = _grid.FirstDisplayedScrollingRowIndex;
            try
            {
                _items = new BindingList<ScaleItemModel>(scale == null ? new List<ScaleItemModel>() : (await _scaleRepository.GetItemsAsync(scale.ScaleId)).ToList());
                _grid.DataSource = _items;

                if (_items.ToList().FindIndex(i => i.ProductId == selectProductId) is var index and >= 0)
                {
                    _grid.CurrentCell = _grid.Rows[index].Cells[0];
                    if (firstRow >= 0 && firstRow < _grid.RowCount) _grid.FirstDisplayedScrollingRowIndex = firstRow;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the items of the scale:\n" + ErrorText(ex), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            ShowState();
        }

        private void ShowState()
        {
            var scale = SelectedScale;
            var driver = ScaleDrivers.Find(scale?.Driver);
            _modelLabel.Text = scale == null ? string.Empty
                : driver == null ? $"Unknown model \"{scale.Driver}\": edit the scale and choose its model."
                : driver.Name + driver.Link switch
                {
                    ScaleLink.Network => $"  ·  {scale.Host}:{scale.Port}",
                    ScaleLink.Serial => $"  ·  {scale.SerialPort} {scale.BaudRate}",
                    _ => string.Empty,
                };

            var toSend = _items.Count(i => i.Status != "Sent");
            _countLabel.Text = scale == null ? "No scale yet: press \"+ Add scale\" first."
                : _items.Count == 0 ? "No items on this scale yet: press \"+ Add items\"."
                : $"{_items.Count} item(s)  ·  {toSend} to send";

            _editScaleButton.Enabled = _deleteScaleButton.Enabled = scale != null && !_busy;
            _addItemsButton.Enabled = _removeButton.Enabled = scale != null && !_busy;
            _sendChangedButton.Enabled = scale != null && driver != null && toSend > 0 && !_busy;
            _sendAllButton.Enabled = scale != null && driver != null && _items.Count > 0 && !_busy;
            _grid.ReadOnly = _busy;
        }

        private async Task AddItemsAsync()
        {
            if (SelectedScale is not { } scale) return;
            using var picker = new QuickItemPickerForm($"Add items to the scale \"{scale.Name}\"", anyItem: true);
            if (picker.ShowDialog(this) != DialogResult.OK || picker.PickedItems.Count == 0) return;

            try
            {
                var productIds = picker.PickedItems.Select(i => i.ProductId).ToList();
                await _scaleRepository.AddItemsAsync(scale.ScaleId, productIds);
                await LoadItemsAsync(productIds[0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not add the items:\n" + ErrorText(ex), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                await LoadItemsAsync();
            }
        }

        private async Task RemoveItemsAsync()
        {
            if (SelectedScale is not { } scale) return;
            var selected = _grid.SelectedRows.Cast<DataGridViewRow>().Select(r => r.DataBoundItem).OfType<ScaleItemModel>().ToList();
            if (selected.Count == 0) return;
            var what = selected.Count == 1 ? $"\"{selected[0].ItemName}\"" : $"these {selected.Count} items";
            if (MessageBox.Show(this, $"Take {what} off the list of \"{scale.Name}\"?\n\n" +
                                      "The item keeps its PLU. It stays in the scale's memory until it is deleted on the scale (or in its PC program).",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                await _scaleRepository.RemoveItemsAsync(scale.ScaleId, selected.Select(i => i.ProductId));
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not remove the items:\n" + ErrorText(ex), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Hot key: blank = none. PLU: digits, padded like the item screen (ScaleBarcode.NormalizeCode).
        private void grid_CellParsing(object? sender, DataGridViewCellParsingEventArgs e)
        {
            var text = e.Value?.ToString()?.Trim() ?? string.Empty;
            switch (_grid.Columns[e.ColumnIndex].Name)
            {
                case nameof(ScaleItemModel.HotKey):
                    e.Value = text.Length == 0 ? null : int.TryParse(text, out var key) ? key : -1;   // -1: refused in SaveRowAsync
                    e.ParsingApplied = true;
                    break;
                case nameof(ScaleItemModel.ScaleCode):
                    e.Value = text;
                    e.ParsingApplied = true;
                    break;
            }
        }

        private async Task SaveRowAsync(int rowIndex)
        {
            if (SelectedScale is not { } scale || rowIndex >= _items.Count) return;
            var item = _items[rowIndex];
            var code = item.ScaleCode?.Trim() ?? string.Empty;
            var length = ScaleBarcode.Settings.ItemCodeLength;
            var problem = code.Length == 0 ? "An item on a scale needs a PLU."
                : !code.All(char.IsAsciiDigit) ? "The PLU can only have digits."
                : code.Length > length ? $"The PLU can have at most {length} digits (ScaleBarcode.ItemCodeLength in appsettings.json)."
                : item.HotKey is <= 0 ? "The hot key is a number from 1 (blank = no key)."
                : null;

            if (problem == null)
            {
                try
                {
                    await _scaleRepository.SaveItemAsync(scale.ScaleId, item.ProductId, ScaleBarcode.NormalizeCode(code), item.HotKey);
                }
                catch (Exception ex)
                {
                    problem = ErrorText(ex);
                }
            }
            if (problem != null) MessageBox.Show(this, problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);

            // After the edit has ended: the saved values (or the old ones) and the new status
            BeginInvoke(async () => await LoadItemsAsync(item.ProductId));
        }

        private void grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count || e.CellStyle == null) return;
            var item = _items[e.RowIndex];
            if (!item.IsActive) e.CellStyle.ForeColor = Color.Gray;

            switch (_grid.Columns[e.ColumnIndex].Name)
            {
                case nameof(ScaleItemModel.Price):
                    e.Value = item.Price is { } price ? price.ToString("#,0.##") : "no price";
                    if (item.Price == null) e.CellStyle.ForeColor = Color.Firebrick;
                    e.FormattingApplied = true;
                    break;
                case nameof(ScaleItemModel.BaseUnit):
                    e.Value = item.IsWeighted ? $"weight ({item.BaseUnit})" : $"piece ({item.BaseUnit})";
                    e.FormattingApplied = true;
                    break;
                case nameof(ScaleItemModel.SentAt):
                    e.Value = item.SentAt is { } at ? at.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "-";
                    e.FormattingApplied = true;
                    break;
                case nameof(ScaleItemModel.Status):
                    e.CellStyle.ForeColor = item.Status switch { "Sent" => AppTheme.Accent, "Changed" => Color.DarkOrange, _ => Color.FromArgb(29, 78, 160) };
                    break;
            }
        }

        #endregion

        #region Sending

        // Sends with the scale's driver; items without a PLU or a price, or not in use, are left out (and listed)
        private async Task SendAsync(bool changedOnly)
        {
            if (SelectedScale is not { } scale) return;
            if (ScaleDrivers.Find(scale.Driver) is not { } driver)
            {
                MessageBox.Show(this, $"This program has no driver \"{scale.Driver}\": edit the scale and choose its model.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var chosen = _items.Where(i => !changedOnly || i.Status != "Sent").ToList();
            var skipped = chosen.Where(i => string.IsNullOrEmpty(i.ScaleCode) || i.Price == null || !i.IsActive).ToList();
            var plus = chosen.Except(skipped)
                .Select(i => new ScalePlu(i.ProductId, i.ScaleCode!, i.ItemName, i.Price!.Value, i.IsWeighted, i.BaseUnit, i.HotKey))
                .ToList();

            if (plus.Count == 0)
            {
                MessageBox.Show(this, skipped.Count == 0 ? "Nothing to send." : "None of these items can be sent: each needs a PLU, a price and to be in use.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _busy = true;
            ShowState();
            UseWaitCursor = true;
            try
            {
                var message = await driver.SendAsync(scale, plus, CancellationToken.None);
                await _scaleRepository.MarkSentAsync(scale.ScaleId, plus);
                if (skipped.Count > 0)
                    message += $"\n\nNot sent ({skipped.Count}): need a PLU, a price and to be in use:\n"
                               + string.Join("\n", skipped.Take(10).Select(i => "  - " + i.ItemName)) + (skipped.Count > 10 ? "\n  ..." : string.Empty);
                MessageBox.Show(this, message, Text, MessageBoxButtons.OK, skipped.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not send to \"{scale.Name}\":\n" + ErrorText(ex), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                _busy = false;
                await LoadItemsAsync();
            }
        }

        #endregion
    }
}
