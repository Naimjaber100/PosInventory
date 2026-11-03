using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Item > Stock adjustment: every draft, and the adjustments made in the chosen days.
    // New adjustment: count items on the shelf and type what was found; Post sets their stock to it
    // (StockAdjustmentEditForm). A draft can be opened again, changed or deleted; a posted one is only shown.
    // Built in code, like the cashier's small windows.
    public class StockAdjustmentsForm : Form
    {
        private readonly StockAdjustmentRepository _repository = new();
        private readonly DataGridView _grid;
        private readonly DateTimePicker _fromPicker, _toPicker;
        private readonly Button _newButton, _openButton, _deleteButton;
        private readonly Label _countLabel;
        private List<StockAdjustmentModel> _adjustments = new();
        private bool _loading;

        public StockAdjustmentsForm()
        {
            Text = "Stock adjustments";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1100, 600);
            MinimumSize = new Size(800, 400);
            Font = new Font("Segoe UI", 9F);
            KeyPreview = true;

            _grid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                Dock = DockStyle.Fill,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            };
            _grid.RowTemplate.Height = 32;
            _grid.DataBindingComplete += (_, _) => FormatGrid();
            _grid.CellFormatting += grid_CellFormatting;
            _grid.SelectionChanged += (_, _) => UpdateButtons();
            _grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0) await OpenAsync(Selected); };
            _grid.KeyDown += async (_, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await OpenAsync(Selected); }
                else if (e.KeyCode == Keys.Delete) await DeleteSelectedAsync();
            };

            var toolPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(8, 6, 8, 6), WrapContents = false };
            _newButton = new Button { Text = "+ New adjustment (Ins)", Size = new Size(190, 36), BackColor = AppTheme.Accent, ForeColor = Color.White, UseVisualStyleBackColor = false };
            _newButton.Click += async (_, _) => await OpenAsync(null);
            _openButton = new Button { Text = "Open", Size = new Size(90, 36) };
            _openButton.Click += async (_, _) => await OpenAsync(Selected);
            _deleteButton = new Button { Text = "Delete draft", Size = new Size(110, 36), ForeColor = Color.Firebrick };
            _deleteButton.Click += async (_, _) => await DeleteSelectedAsync();
            var refreshButton = new Button { Text = "Refresh (F5)", Size = new Size(110, 36) };
            refreshButton.Click += async (_, _) => await LoadAsync();
            _fromPicker = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Value = DateTime.Today.AddDays(-30), Margin = new Padding(3, 8, 3, 3) };
            _toPicker = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Value = DateTime.Today, Margin = new Padding(3, 8, 3, 3) };
            _fromPicker.ValueChanged += async (_, _) => await LoadAsync();
            _toPicker.ValueChanged += async (_, _) => await LoadAsync();
            _countLabel = new Label { AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(16, 11, 3, 0) };
            toolPanel.Controls.AddRange(new Control[]
            {
                _newButton, _openButton, _deleteButton, refreshButton,
                new Label { Text = "From", AutoSize = true, Margin = new Padding(20, 11, 3, 0) }, _fromPicker,
                new Label { Text = "to", AutoSize = true, Margin = new Padding(3, 11, 3, 0) }, _toPicker,
                _countLabel,
            });

            var bottomPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8, 6, 8, 6) };
            var closeButton = new Button { Text = "Close", Size = new Size(100, 36) };
            closeButton.Click += (_, _) => Close();
            bottomPanel.Controls.Add(closeButton);

            Controls.Add(_grid);
            Controls.Add(toolPanel);
            Controls.Add(bottomPanel);
            AppTheme.Apply(this);

            Load += async (_, _) => await LoadAsync();
            KeyDown += async (_, e) =>
            {
                if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; Close(); }
                else if (e.KeyCode == Keys.Insert) { e.SuppressKeyPress = true; _newButton.PerformClick(); }
                else if (e.KeyCode == Keys.F5) { e.SuppressKeyPress = true; await LoadAsync(); }
            };
        }

        private StockAdjustmentModel? Selected => _grid.CurrentRow?.DataBoundItem as StockAdjustmentModel;

        private async Task LoadAsync(int? selectId = null)
        {
            if (_loading) return;
            _loading = true;
            selectId ??= Selected?.AdjustmentId;
            try
            {
                var from = _fromPicker.Value.Date;
                var to = _toPicker.Value.Date;
                _adjustments = (await _repository.GetAllAsync(from <= to ? from : to, from <= to ? to : from)).ToList();
                _grid.DataSource = _adjustments;
                var drafts = _adjustments.Count(a => a.IsDraft);
                _countLabel.Text = _adjustments.Count == 0
                    ? "No adjustments in these days."
                    : $"{_adjustments.Count} adjustment(s)" + (drafts > 0 ? $"  ·  {drafts} draft(s) not posted yet" : string.Empty);

                if (_adjustments.FindIndex(a => a.AdjustmentId == selectId) is var index and >= 0)
                    _grid.CurrentCell = _grid.Rows[index].Cells.Cast<DataGridViewCell>().First(c => c.Visible);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the stock adjustments:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message)
                                      + "\n\nApply Database\\stock_adjustment_functions.sql if it is not applied yet.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _loading = false;
                UpdateButtons();
            }
        }

        private void UpdateButtons()
        {
            _openButton.Enabled = Selected != null;
            _deleteButton.Enabled = Selected is { IsDraft: true };
        }

        private async Task OpenAsync(StockAdjustmentModel? adjustment)
        {
            using var form = new StockAdjustmentEditForm(adjustment);
            if (form.ShowDialog(this) != DialogResult.OK) return;
            await LoadAsync(form.SavedId);
        }

        private async Task DeleteSelectedAsync()
        {
            if (Selected is not { IsDraft: true } adjustment) return;
            if (MessageBox.Show(this, $"Delete the draft \"{adjustment.Reason}\" ({adjustment.LineCount} item(s))?\n\nThe stock does not change.",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                await _repository.DeleteAsync(adjustment.AdjustmentId);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not delete the draft:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatGrid()
        {
            SetColumn(nameof(StockAdjustmentModel.AdjustmentNo), 90);
            SetColumn(nameof(StockAdjustmentModel.AdjustmentDate), 90, "dd/MM/yyyy HH:mm");
            SetColumn(nameof(StockAdjustmentModel.Reason), 200);
            SetColumn(nameof(StockAdjustmentModel.StatusText), 60);
            SetColumn(nameof(StockAdjustmentModel.UserName), 90);
            SetColumn(nameof(StockAdjustmentModel.LineCount), 50, "#,0", right: true);
            SetColumn(nameof(StockAdjustmentModel.ValueChange), 90, "#,0.##", right: true);
        }

        private void SetColumn(string name, float weight, string? format = null, bool right = false)
        {
            if (_grid.Columns[name] is not { } column) return;
            column.FillWeight = weight;
            if (format != null) column.DefaultCellStyle.Format = format;
            if (right) column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        // A draft in orange; a value change in red when stock was lost, green when found
        private void grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _adjustments.Count || e.CellStyle == null) return;
            var adjustment = _adjustments[e.RowIndex];
            switch (_grid.Columns[e.ColumnIndex].DataPropertyName)
            {
                case nameof(StockAdjustmentModel.StatusText):
                    e.CellStyle.ForeColor = adjustment.IsDraft ? Color.DarkOrange : AppTheme.Accent;
                    break;
                case nameof(StockAdjustmentModel.AdjustmentNo) when adjustment.IsDraft:
                    e.Value = "(draft)";
                    e.CellStyle.ForeColor = Color.Gray;
                    e.FormattingApplied = true;
                    break;
                case nameof(StockAdjustmentModel.ValueChange) when adjustment.ValueChange is { } value && value != 0:
                    e.CellStyle.ForeColor = value < 0 ? Color.Firebrick : Color.SeaGreen;
                    break;
            }
        }
    }
}
