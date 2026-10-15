using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Item > Promotions: price cuts, multi-buy, buy X get Y, mix & match, category % and invoice discounts.
    // The cashier screen applies the ones that are running today by itself (PromotionEngine); an open cashier
    // screen gets every change at once. Built in code, like the cashier's small windows.
    public class PromotionsForm : Form
    {
        private readonly PromotionRepository _promotionRepository = new();
        private List<PromotionModel> _all = new();
        private readonly DataGridView _listGrid;
        private readonly Button _addButton, _editButton, _deleteButton;
        private readonly Label _countLabel;

        public PromotionsForm()
        {
            Text = "Promotions";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1100, 600);
            MinimumSize = new Size(800, 400);
            Font = new Font("Segoe UI", 9F);
            KeyPreview = true;

            _listGrid = new DataGridView
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
            _listGrid.RowTemplate.Height = 34;
            _listGrid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0) await EditSelectedAsync(); };
            _listGrid.CellFormatting += listGrid_CellFormatting;
            _listGrid.DataBindingComplete += listGrid_DataBindingComplete;
            _listGrid.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _editButton!.PerformClick(); }
                else if (e.KeyCode == Keys.Delete) _deleteButton!.PerformClick();
            };

            var toolPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(8, 6, 8, 6), WrapContents = false };
            _addButton = new Button { Text = "+ Add promotion (Ins)", Size = new Size(180, 36), BackColor = AppTheme.Accent, ForeColor = Color.White, UseVisualStyleBackColor = false };
            _addButton.Click += async (_, _) => await EditAsync(null);
            _editButton = new Button { Text = "Edit", Size = new Size(90, 36) };
            _editButton.Click += async (_, _) => await EditSelectedAsync();
            _deleteButton = new Button { Text = "Delete", Size = new Size(90, 36), ForeColor = Color.Firebrick };
            _deleteButton.Click += async (_, _) => await DeleteSelectedAsync();
            _countLabel = new Label { AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(16, 11, 3, 0) };
            toolPanel.Controls.AddRange(new Control[] { _addButton, _editButton, _deleteButton, _countLabel });

            var bottomPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8, 6, 8, 6) };
            var closeButton = new Button { Text = "Close", Size = new Size(100, 36) };
            closeButton.Click += (_, _) => Close();
            bottomPanel.Controls.Add(closeButton);

            Controls.Add(_listGrid);
            Controls.Add(toolPanel);
            Controls.Add(bottomPanel);
            AppTheme.Apply(this);

            Load += async (_, _) => await LoadAsync();
            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; Close(); }
                else if (e.KeyCode == Keys.Insert) { e.SuppressKeyPress = true; _addButton.PerformClick(); }
            };
        }

        private PromotionModel? Selected => _listGrid.CurrentRow?.DataBoundItem as PromotionModel;

        private async Task LoadAsync(int? selectId = null)
        {
            selectId ??= Selected?.PromotionId;
            try
            {
                _all = (await _promotionRepository.GetAllAsync()).ToList();
                _listGrid.DataSource = _all;
                var running = _all.Count(p => p.RunsToday);
                _countLabel.Text = _all.Count == 0
                    ? "No promotions yet. The cashier applies them by itself while the items are scanned."
                    : $"{running} running today  ·  {_all.Count} promotion(s)";

                if (_all.FindIndex(p => p.PromotionId == selectId) is var index and >= 0)
                    _listGrid.CurrentCell = _listGrid.Rows[index].Cells.Cast<DataGridViewCell>().First(c => c.Visible);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the promotions:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task EditSelectedAsync()
        {
            if (Selected is { } promotion) await EditAsync(promotion);
        }

        private async Task EditAsync(PromotionModel? promotion)
        {
            using var form = new PromotionEditForm(promotion);
            if (form.ShowDialog(this) != DialogResult.OK) return;
            await LoadAsync(form.SavedId);
            TellCashier();
        }

        private async Task DeleteSelectedAsync()
        {
            if (Selected is not { } promotion) return;
            if (MessageBox.Show(this, $"Delete the promotion \"{promotion.Name}\"?\n\n" +
                                      "Sales already made keep the discount they got from it. To stop it for a while, edit it and untick On.",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                await _promotionRepository.DeleteAsync(promotion.PromotionId);
                await LoadAsync();
                TellCashier();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not delete the promotion:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // An open cashier screen uses the change from its next item on
        private static void TellCashier()
        {
            foreach (var cashier in Application.OpenForms.OfType<CashierForm>().ToList())
                _ = cashier.LoadPromotionsAsync();
        }

        private void listGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            SetWeight(nameof(PromotionModel.Name), 150);
            SetWeight(nameof(PromotionModel.TypeText), 80);
            SetWeight(nameof(PromotionModel.Summary), 230);
            SetWeight(nameof(PromotionModel.StartsOn), 70);
            SetWeight(nameof(PromotionModel.EndsOn), 70);
            SetWeight(nameof(PromotionModel.Status), 70);
        }

        private void SetWeight(string column, float weight)
        {
            if (_listGrid.Columns[column] is { } c) c.FillWeight = weight;
        }

        // Dates as dd/MM/yyyy ("-" = no limit); a promotion that is not running in grey, a running one with a green status
        private void listGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _all.Count || e.CellStyle == null) return;
            var promotion = _all[e.RowIndex];
            if (!promotion.RunsToday) e.CellStyle.ForeColor = Color.Gray;

            switch (_listGrid.Columns[e.ColumnIndex].DataPropertyName)
            {
                case nameof(PromotionModel.StartsOn):
                case nameof(PromotionModel.EndsOn):
                    e.Value = e.Value is DateTime day ? day.ToString("dd/MM/yyyy") : "-";
                    e.FormattingApplied = true;
                    break;
                case nameof(PromotionModel.Status):
                    if (promotion.RunsToday) e.CellStyle.ForeColor = AppTheme.Accent;
                    else if (promotion.IsActive && promotion.Status != "Ended") e.CellStyle.ForeColor = Color.DarkOrange;
                    break;
            }
        }
    }
}
