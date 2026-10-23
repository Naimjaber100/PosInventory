using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Back office "POS buttons": the tabs and item buttons on the right of the cashier screen (items without a barcode).
    // Left: tabs (Salads, Drinks...) in cashier order - drag or ▲▼ to reorder, double-click to edit.
    // Right: the buttons of the chosen tab, edited in place and saved at once:
    //        label (click to type), color (tab color or its own), ask price, show, × remove; drag ⋮⋮ to reorder,
    //        drag a row onto another tab to move it. "+ Add items" picks items from the item list.
    // Bottom: a preview of what the cashier sees. An open cashier screen reloads its buttons when this window closes.
    public partial class QuickButtonsForm : Form
    {
        private static readonly Color Muted = Color.FromArgb(120, 128, 138);
        private static readonly Color Line = Color.FromArgb(228, 232, 236);
        private const int PreviewButtonHeight = 64;

        private readonly QuickButtonRepository _quickButtonRepository = new();
        private readonly SaleRepository _saleRepository = new();
        private readonly List<QuickGroupModel> _tabs = new();      // real tabs in order, then "No tab"
        private List<QuickButtonModel> _allButtons = new();
        private BindingList<QuickButtonModel> _buttons = new();    // the buttons of the selected tab
        private QuickGroupModel? _tab;
        private int _columns = QuickButtonStyle.DefaultColumns;
        private bool _changed;
        private bool _loadingColumns;
        private string? _captionBeforeEdit;
        private int _dragTabIndex = -1;
        private int _dragRowIndex = -1;
        private Point _dragStart;

        public QuickButtonsForm()
        {
            InitializeComponent();
            AppTheme.Apply(this);
            itemsGrid.AutoGenerateColumns = false;
            columnsUpDown.Minimum = QuickButtonStyle.MinColumns;
            columnsUpDown.Maximum = QuickButtonStyle.MaxColumns;

            // The cashier's Voice button (say the items instead of scanning them): off unless ticked here
            _voiceCheck = new CheckBox
            {
                Text = "Voice order on the cashier",
                AutoSize = true,
                Dock = DockStyle.Right,
                Font = new Font("Segoe UI", 8.5F),
                Padding = new Padding(0, 4, 16, 0),
            };
            new ToolTip().SetToolTip(_voiceCheck, "Shows a Voice button (Ctrl+M) on the cashier screen: the cashier says the items " +
                                                  "(\"1 pepsi can, 3 bread\", English or Arabic) and they are added. Needs the OpenAI key and a microphone.");
            _voiceCheck.CheckedChanged += voiceCheck_CheckedChanged;
            columnsUpDown.Parent!.Controls.Add(_voiceCheck);
        }

        private readonly CheckBox _voiceCheck;
        private bool _loadingVoice;

        private async Task LoadVoiceSettingAsync()
        {
            _loadingVoice = true;
            try { _voiceCheck.Checked = await _saleRepository.GetSettingAsync(CashierForm.VoiceSetting, "0") == "1"; }
            catch { _voiceCheck.Checked = false; }
            _loadingVoice = false;
        }

        private async void voiceCheck_CheckedChanged(object? sender, EventArgs e)
        {
            if (_loadingVoice) return;
            try
            {
                await _saleRepository.SetSettingAsync(CashierForm.VoiceSetting, _voiceCheck.Checked ? "1" : "0");
                _changed = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save the setting:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void QuickButtonsForm_Load(object? sender, EventArgs e)
        {
            await LoadVoiceSettingAsync();
            await LoadColumnsAsync();
            await ReloadAsync();
        }

        #region Loading and showing

        // Reloads tabs and buttons; selects the given tab (or keeps the current one) and button
        private async Task ReloadAsync(int? selectTabId = null, int? selectButtonId = null)
        {
            selectTabId ??= _tab?.GroupId;
            try
            {
                var groups = (await _quickButtonRepository.GetGroupsAsync(forCashier: false)).ToList();
                _allButtons = (await _quickButtonRepository.GetAllAsync(forCashier: false)).ToList();

                _tabs.Clear();
                _tabs.AddRange(groups);
                _tabs.Add(new QuickGroupModel { GroupId = QuickGroupModel.NoGroupId, Name = "No tab" });

                tabsList.BeginUpdate();
                tabsList.Items.Clear();
                tabsList.Items.AddRange(_tabs.Cast<object>().ToArray());
                tabsList.EndUpdate();

                var tab = _tabs.FirstOrDefault(t => t.GroupId == selectTabId) ?? _tabs[0];
                tabsList.SelectedItem = tab;
                ShowTab(tab, selectButtonId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the POS buttons:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void tabsList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (tabsList.SelectedItem is QuickGroupModel tab && !ReferenceEquals(tab, _tab)) ShowTab(tab);
        }

        private void ShowTab(QuickGroupModel tab, int? selectButtonId = null)
        {
            if (itemsGrid.IsCurrentCellInEditMode) itemsGrid.EndEdit();
            _tab = tab;
            _buttons = new BindingList<QuickButtonModel>(ButtonsOf(tab).ToList());
            itemsGrid.DataSource = _buttons;

            var isTab = !tab.IsNoGroup;
            editTabButton.Visible = deleteTabButton.Enabled = upTabButton.Enabled = downTabButton.Enabled = isTab;
            if (selectButtonId is int id && _buttons.FirstOrDefault(b => b.ButtonId == id) is { } button)
                SelectRow(_buttons.IndexOf(button));
            RefreshViews();
        }

        private IEnumerable<QuickButtonModel> ButtonsOf(QuickGroupModel tab) =>
            _allButtons.Where(b => (b.GroupId ?? QuickGroupModel.NoGroupId) == tab.GroupId);

        private string? TabColorOf(QuickButtonModel button) =>
            _tabs.FirstOrDefault(t => t.GroupId == (button.GroupId ?? QuickGroupModel.NoGroupId))?.Color;

        // Header, tab counts and preview after any change
        private void RefreshViews()
        {
            foreach (var t in _tabs) t.ButtonCount = ButtonsOf(t).Count();
            tabsList.Invalidate();

            if (_tab == null) return;
            var shown = _buttons.Count(b => b.IsActive);
            tabSwatch.BackColor = _tab.IsNoGroup ? QuickButtonStyle.DefaultColor : QuickButtonStyle.Parse(_tab.Color);
            tabNameLabel.Text = _tab.IsNoGroup ? "No tab" : _tab.Name;
            tabInfoLabel.Text = _tab.IsNoGroup
                ? $"{Buttons(_buttons.Count)} without a tab · shown under \"Other\" when there are tabs"
                : $"{Buttons(_buttons.Count)} · {(_tab.IsActive ? $"{shown} shown on the cashier screen" : "the tab is hidden on the cashier screen")}";
            emptyLabel.Visible = _buttons.Count == 0;
            RefreshPreview();
        }

        private static string Buttons(long count) => count == 1 ? "1 button" : $"{count} buttons";

        private void SelectRow(int index)
        {
            if (index < 0 || index >= itemsGrid.Rows.Count) return;
            itemsGrid.CurrentCell = itemsGrid.Rows[index].Cells[itemColumn.Index];
        }

        private QuickButtonModel? SelectedButton => itemsGrid.CurrentRow?.DataBoundItem as QuickButtonModel;

        #endregion

        #region Tabs

        private async void newTabButton_Click(object? sender, EventArgs e)
        {
            using var form = new QuickGroupEditForm();
            if (form.ShowDialog(this) != DialogResult.OK) return;
            _changed = true;
            await ReloadAsync(form.SavedGroupId);
        }

        private async void editTabButton_Click(object? sender, EventArgs e) => await EditTabAsync();

        private async void tabsList_DoubleClick(object? sender, EventArgs e) => await EditTabAsync();

        private async Task EditTabAsync()
        {
            if (_tab is not { IsNoGroup: false } tab) return;
            using var form = new QuickGroupEditForm(tab);
            if (form.ShowDialog(this) != DialogResult.OK) return;
            _changed = true;
            await ReloadAsync(tab.GroupId);
        }

        private async void deleteTabButton_Click(object? sender, EventArgs e)
        {
            if (_tab is not { IsNoGroup: false } tab) return;

            var deleteButtons = false;
            if (tab.ButtonCount > 0)
            {
                var answer = MessageBox.Show(this,
                    $"Delete the tab \"{tab.Name}\"?\n\nIt has {tab.ButtonCount} button(s):\n" +
                    "  Yes = delete the buttons too\n  No = keep them under \"No tab\"\n\n(The items themselves are not deleted.)",
                    Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (answer == DialogResult.Cancel) return;
                deleteButtons = answer == DialogResult.Yes;
            }
            else if (MessageBox.Show(this, $"Delete the empty tab \"{tab.Name}\"?", Text,
                         MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                await _quickButtonRepository.DeleteGroupAsync(tab.GroupId, deleteButtons);
                _changed = true;
                var index = _tabs.IndexOf(tab);
                await ReloadAsync(_tabs[Math.Max(0, index - 1)].GroupId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not delete the tab:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void upTabButton_Click(object? sender, EventArgs e) => await MoveTabAsync(_tab, -1);

        private async void downTabButton_Click(object? sender, EventArgs e) => await MoveTabAsync(_tab, +1);

        private async Task MoveTabAsync(QuickGroupModel? tab, int step)
        {
            if (tab is not { IsNoGroup: false }) return;
            await MoveTabToAsync(tab, _tabs.IndexOf(tab) + step);
        }

        // "No tab" always stays last
        private async Task MoveTabToAsync(QuickGroupModel tab, int target)
        {
            var index = _tabs.IndexOf(tab);
            target = Math.Clamp(target, 0, _tabs.Count - 2);
            if (index < 0 || target == index) return;

            _tabs.RemoveAt(index);
            _tabs.Insert(target, tab);
            tabsList.BeginUpdate();
            tabsList.Items.RemoveAt(index);
            tabsList.Items.Insert(target, tab);
            tabsList.SelectedIndex = target;
            tabsList.EndUpdate();
            RefreshPreview();

            try
            {
                await _quickButtonRepository.ReorderGroupsAsync(_tabs.Where(t => !t.IsNoGroup).Select(t => t.GroupId));
                _changed = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save the order:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                await ReloadAsync(tab.GroupId);
            }
        }

        // Tab row: grip, color square, name, "7 buttons · hidden"; the selected tab has a light background and a bar
        private void tabsList_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= tabsList.Items.Count) return;
            var tab = (QuickGroupModel)tabsList.Items[e.Index];
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = e.Bounds;
            var selected = (e.State & DrawItemState.Selected) != 0;

            using (var back = new SolidBrush(selected ? Color.FromArgb(226, 242, 236) : Color.White)) g.FillRectangle(back, r);
            if (selected)
            {
                using var bar = new SolidBrush(QuickButtonStyle.Accent);
                g.FillRectangle(bar, r.X, r.Y, 4, r.Height);
            }
            using (var line = new Pen(Line)) g.DrawLine(line, r.X, r.Bottom - 1, r.Right, r.Bottom - 1);

            var dim = !tab.IsNoGroup && !tab.IsActive || tab.IsNoGroup;
            if (!tab.IsNoGroup)
                TextRenderer.DrawText(g, "⋮⋮", Font, new Rectangle(r.X + 8, r.Y, 16, r.Height), Color.Silver,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);

            var swatch = new Rectangle(r.X + 30, r.Y + (r.Height - 24) / 2, 24, 24);
            using (var path = QuickButtonStyle.Rounded(swatch, 5))
            {
                if (tab.IsNoGroup)
                {
                    using var dash = new Pen(Color.Silver) { DashStyle = DashStyle.Dash };
                    g.DrawPath(dash, path);
                }
                else
                {
                    using var fill = new SolidBrush(QuickButtonStyle.Parse(tab.Color));
                    g.FillPath(fill, path);
                    using var border = new Pen(Color.FromArgb(40, 0, 0, 0));
                    g.DrawPath(border, path);
                }
            }

            using var nameFont = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 8.5F);
            var sub = tab.IsNoGroup
                ? $"{Buttons(tab.ButtonCount)} · shown as \"Other\""
                : $"{Buttons(tab.ButtonCount)}{(tab.IsActive ? string.Empty : " · hidden")}";
            TextRenderer.DrawText(g, tab.IsNoGroup ? "No tab" : tab.Name, nameFont, new Point(r.X + 64, r.Y + 8),
                dim ? Muted : QuickButtonStyle.Dark, TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, sub, subFont, new Point(r.X + 64, r.Y + 30), Muted);
        }

        // Drag a tab to reorder it; drop a button row on a tab to move the button there
        private void tabsList_MouseDown(object? sender, MouseEventArgs e)
        {
            _dragTabIndex = e.Button == MouseButtons.Left ? tabsList.IndexFromPoint(e.Location) : -1;
            _dragStart = e.Location;
        }

        private void tabsList_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _dragTabIndex < 0 || !DragMoved(e.Location)) return;
            var tab = _tabs[_dragTabIndex];
            _dragTabIndex = -1;
            if (!tab.IsNoGroup) tabsList.DoDragDrop(tab, DragDropEffects.Move);
        }

        private void tabsList_DragOver(object? sender, DragEventArgs e)
        {
            e.Effect = e.Data?.GetDataPresent(typeof(QuickGroupModel)) == true || e.Data?.GetDataPresent(typeof(QuickButtonModel)) == true
                ? DragDropEffects.Move
                : DragDropEffects.None;
        }

        private async void tabsList_DragDrop(object? sender, DragEventArgs e)
        {
            var index = tabsList.IndexFromPoint(tabsList.PointToClient(new Point(e.X, e.Y)));
            if (e.Data?.GetData(typeof(QuickGroupModel)) is QuickGroupModel tab)
            {
                await MoveTabToAsync(tab, index < 0 ? _tabs.Count - 2 : index);
            }
            else if (e.Data?.GetData(typeof(QuickButtonModel)) is QuickButtonModel button && index >= 0)
            {
                await MoveButtonToTabAsync(button, _tabs[index]);
            }
        }

        private bool DragMoved(Point location) =>
            Math.Abs(location.X - _dragStart.X) > SystemInformation.DragSize.Width
            || Math.Abs(location.Y - _dragStart.Y) > SystemInformation.DragSize.Height;

        #endregion

        #region Buttons

        private async void addItemsButton_Click(object? sender, EventArgs e)
        {
            if (_tab == null) return;
            using var picker = new QuickItemPickerForm(_tab.IsNoGroup ? "No tab" : _tab.Name);
            if (picker.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                UseWaitCursor = true;
                var added = await _quickButtonRepository.AddItemsAsync(_tab.SaveId, picker.PickedPackagingIds);
                if (added > 0) _changed = true;
                await ReloadAsync(_tab.GroupId);
                if (_buttons.Count > 0) SelectRow(_buttons.Count - 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not add the items:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        // Saves one button after a change in the grid; reloads when it fails
        private async Task SaveButtonAsync(QuickButtonModel button)
        {
            itemsGrid.InvalidateRow(_buttons.IndexOf(button));
            RefreshViews();
            try
            {
                await _quickButtonRepository.SaveAsync(button);
                _changed = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save the button:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                await ReloadAsync(selectButtonId: button.ButtonId);
            }
        }

        private async Task RemoveButtonAsync(QuickButtonModel button)
        {
            if (MessageBox.Show(this, $"Remove the button \"{button.DisplayText}\"?\n(The item itself is not deleted.)", Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                await _quickButtonRepository.DeleteAsync(button.ButtonId);
                _changed = true;
                _allButtons.Remove(button);
                _buttons.Remove(button);
                RefreshViews();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not remove the button:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task MoveButtonToTabAsync(QuickButtonModel button, QuickGroupModel tab)
        {
            if ((button.GroupId ?? QuickGroupModel.NoGroupId) == tab.GroupId) return;
            button.GroupId = tab.SaveId;
            try
            {
                await _quickButtonRepository.SaveAsync(button);   // goes to the end of the new tab
                _changed = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not move the button:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            await ReloadAsync(_tab?.GroupId);
        }

        private async Task MoveRowAsync(QuickButtonModel button, int target)
        {
            var index = _buttons.IndexOf(button);
            target = Math.Clamp(target, 0, _buttons.Count - 1);
            if (index < 0 || target == index) return;

            _buttons.RemoveAt(index);
            _buttons.Insert(target, button);
            _allButtons = _allButtons.Where(b => !_buttons.Contains(b)).Concat(_buttons).ToList();
            SelectRow(target);
            RefreshViews();

            try
            {
                await _quickButtonRepository.ReorderAsync(_buttons.Select(b => b.ButtonId));
                _changed = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save the order:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                await ReloadAsync(selectButtonId: button.ButtonId);
            }
        }

        // One click does the cell's job: type the label, pick a color, flip a switch, remove
        private async void itemsGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _buttons.Count) return;
            var button = _buttons[e.RowIndex];

            if (e.ColumnIndex == labelColumn.Index)
            {
                itemsGrid.BeginEdit(true);
            }
            else if (e.ColumnIndex == colorColumn.Index)
            {
                var cell = itemsGrid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                ShowColorMenu(button, new Point(cell.Left, cell.Bottom));
            }
            else if (e.ColumnIndex == askPriceColumn.Index)
            {
                button.AskPrice = !button.AskPrice;
                await SaveButtonAsync(button);
            }
            else if (e.ColumnIndex == showColumn.Index)
            {
                button.IsActive = !button.IsActive;
                await SaveButtonAsync(button);
            }
            else if (e.ColumnIndex == removeColumn.Index)
            {
                await RemoveButtonAsync(button);
            }
        }

        private void itemsGrid_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            _captionBeforeEdit = e.RowIndex < _buttons.Count ? _buttons[e.RowIndex].Caption : null;
        }

        private async void itemsGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _buttons.Count || e.ColumnIndex != labelColumn.Index) return;
            var button = _buttons[e.RowIndex];
            var caption = button.Caption?.Trim();
            button.Caption = string.IsNullOrEmpty(caption) ? null : caption;
            if (button.Caption != _captionBeforeEdit) await SaveButtonAsync(button);
        }

        private async void itemsGrid_KeyDown(object? sender, KeyEventArgs e)
        {
            if (SelectedButton is not { } button) return;
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    e.Handled = true;
                    await RemoveButtonAsync(button);
                    break;
                case Keys.Up when e.Alt:
                    e.Handled = true;
                    await MoveRowAsync(button, _buttons.IndexOf(button) - 1);
                    break;
                case Keys.Down when e.Alt:
                    e.Handled = true;
                    await MoveRowAsync(button, _buttons.IndexOf(button) + 1);
                    break;
            }
        }

        // Color menu: "Tab color" (follows the tab), the palette, or any color
        private void ShowColorMenu(QuickButtonModel button, Point at)
        {
            colorMenu.Items.Clear();
            var tabColor = QuickButtonStyle.Parse(TabColorOf(button));
            AddColorItem("Tab color", tabColor, button.Color == null, () => button.Color = null);
            colorMenu.Items.Add(new ToolStripSeparator());
            foreach (var color in QuickButtonStyle.Palette)
            {
                var hex = QuickButtonStyle.ToHex(color) ?? $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                AddColorItem(hex, color, string.Equals(button.Color, hex, StringComparison.OrdinalIgnoreCase), () => button.Color = hex);
            }
            colorMenu.Items.Add(new ToolStripSeparator());
            colorMenu.Items.Add("More colors...", null, async (_, _) =>
            {
                using var dialog = new ColorDialog { Color = QuickButtonStyle.ButtonColor(button, TabColorOf(button)), FullOpen = true };
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                var c = dialog.Color;
                button.Color = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                await SaveButtonAsync(button);
            });
            colorMenu.Show(itemsGrid, at);

            void AddColorItem(string text, Color color, bool isChecked, Action apply)
            {
                var item = new ToolStripMenuItem(text, QuickButtonStyle.Swatch(color)) { Checked = isChecked };
                item.Click += async (_, _) => { apply(); await SaveButtonAsync(button); };
                colorMenu.Items.Add(item);
            }
        }

        // Right-click a row: move it to another tab, or remove it
        private void rowMenu_Opening(object? sender, CancelEventArgs e)
        {
            if (SelectedButton is not { } button) { e.Cancel = true; return; }
            moveToTabMenuItem.DropDownItems.Clear();
            foreach (var tab in _tabs.Where(t => t.GroupId != (button.GroupId ?? QuickGroupModel.NoGroupId)))
            {
                var target = tab;
                var item = new ToolStripMenuItem(tab.IsNoGroup ? "(no tab)" : tab.Name,
                    tab.IsNoGroup ? null : QuickButtonStyle.Swatch(QuickButtonStyle.Parse(tab.Color)));
                item.Click += async (_, _) => await MoveButtonToTabAsync(button, target);
                moveToTabMenuItem.DropDownItems.Add(item);
            }
            moveToTabMenuItem.Enabled = moveToTabMenuItem.DropDownItems.Count > 0;
        }

        private async void removeMenuItem_Click(object? sender, EventArgs e)
        {
            if (SelectedButton is { } button) await RemoveButtonAsync(button);
        }

        private void itemsGrid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.Button == MouseButtons.Right) SelectRow(e.RowIndex);

            // Drag starts from the ⋮⋮ grip
            _dragRowIndex = e.Button == MouseButtons.Left && e.ColumnIndex == gripColumn.Index ? e.RowIndex : -1;
            var cell = itemsGrid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            _dragStart = new Point(cell.X + e.X, cell.Y + e.Y);
        }

        private void itemsGrid_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _dragRowIndex < 0 || _dragRowIndex >= _buttons.Count || !DragMoved(e.Location)) return;
            var button = _buttons[_dragRowIndex];
            _dragRowIndex = -1;
            itemsGrid.DoDragDrop(button, DragDropEffects.Move);
        }

        private void itemsGrid_MouseUp(object? sender, MouseEventArgs e) => _dragRowIndex = -1;

        private void itemsGrid_DragOver(object? sender, DragEventArgs e)
        {
            e.Effect = e.Data?.GetDataPresent(typeof(QuickButtonModel)) == true ? DragDropEffects.Move : DragDropEffects.None;
        }

        private async void itemsGrid_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(typeof(QuickButtonModel)) is not QuickButtonModel button) return;
            var hit = itemsGrid.HitTest(itemsGrid.PointToClient(new Point(e.X, e.Y)).X, itemsGrid.PointToClient(new Point(e.X, e.Y)).Y);
            await MoveRowAsync(button, hit.RowIndex < 0 ? _buttons.Count - 1 : hit.RowIndex);
        }

        private void itemsGrid_CellMouseEnter(object? sender, DataGridViewCellEventArgs e)
        {
            var clickable = e.RowIndex >= 0 && (e.ColumnIndex == colorColumn.Index || e.ColumnIndex == askPriceColumn.Index
                                                || e.ColumnIndex == showColumn.Index || e.ColumnIndex == removeColumn.Index);
            itemsGrid.Cursor = e.RowIndex >= 0 && e.ColumnIndex == gripColumn.Index ? Cursors.SizeAll
                : clickable ? Cursors.Hand
                : e.RowIndex >= 0 && e.ColumnIndex == labelColumn.Index ? Cursors.IBeam
                : Cursors.Default;
        }

        private void itemsGrid_MouseLeave(object? sender, EventArgs e) => itemsGrid.Cursor = Cursors.Default;

        #endregion

        #region Grid drawing

        private void itemsGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _buttons.Count || e.CellStyle == null) return;
            var button = _buttons[e.RowIndex];
            if (!button.IsActive) e.CellStyle.ForeColor = e.CellStyle.SelectionForeColor = Muted;

            if (e.ColumnIndex == priceColumn.Index)
            {
                e.Value = QuickButtonStyle.PriceText(button);
                e.FormattingApplied = true;
            }
            else if (e.ColumnIndex == gripColumn.Index)
            {
                e.Value = "⋮⋮";
                e.CellStyle.ForeColor = e.CellStyle.SelectionForeColor = Color.Silver;
            }
            else if (e.ColumnIndex == removeColumn.Index)
            {
                e.Value = "×";
                e.CellStyle.ForeColor = e.CellStyle.SelectionForeColor = Color.Firebrick;
            }
        }

        private void itemsGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _buttons.Count || e.Graphics == null) return;
            var button = _buttons[e.RowIndex];
            var col = e.ColumnIndex;
            if (col != itemColumn.Index && col != labelColumn.Index && col != colorColumn.Index
                && col != askPriceColumn.Index && col != showColumn.Index)
                return;

            e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border | DataGridViewPaintParts.SelectionBackground);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = e.CellBounds;
            var font = e.CellStyle?.Font ?? itemsGrid.Font;
            var textColor = button.IsActive ? QuickButtonStyle.Dark : Muted;

            if (col == itemColumn.Index)
            {
                // Name, and a small "by kg" chip for weighted items
                var name = button.ItemName;
                var size = TextRenderer.MeasureText(g, name, font);
                var textRect = new Rectangle(r.X + 6, r.Y, r.Width - 12, r.Height);
                TextRenderer.DrawText(g, name, font, textRect, textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                if (button.IsWeighted && size.Width + 60 < r.Width)
                {
                    using var chipFont = new Font("Segoe UI", 7.5F);
                    var chipText = "by " + button.BaseUnit;
                    var chipSize = TextRenderer.MeasureText(chipText, chipFont);
                    var chip = new Rectangle(r.X + 6 + size.Width + 2, r.Y + (r.Height - 18) / 2, chipSize.Width + 4, 18);
                    using var path = QuickButtonStyle.Rounded(chip, 9);
                    using var chipBack = new SolidBrush(Color.FromArgb(235, 238, 242));
                    g.FillPath(chipBack, path);
                    TextRenderer.DrawText(g, chipText, chipFont, chip, Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
            else if (col == labelColumn.Index)
            {
                // A text box look; the item name in grey when there is no label of its own
                var box = new Rectangle(r.X + 4, r.Y + 5, r.Width - 10, r.Height - 11);
                using (var path = QuickButtonStyle.Rounded(box, 4))
                {
                    g.FillPath(Brushes.White, path);
                    using var border = new Pen(Color.FromArgb(200, 206, 212));
                    g.DrawPath(border, path);
                }
                var own = !string.IsNullOrWhiteSpace(button.Caption);
                TextRenderer.DrawText(g, own ? button.Caption : button.ItemName, font, Rectangle.Inflate(box, -6, 0),
                    own ? textColor : Color.Silver, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            else if (col == colorColumn.Index)
            {
                // [■ tab color] or [■ #9A4F12]
                var box = new Rectangle(r.X + 4, r.Y + 5, r.Width - 10, r.Height - 11);
                using (var path = QuickButtonStyle.Rounded(box, 4))
                {
                    g.FillPath(Brushes.White, path);
                    using var border = new Pen(Color.FromArgb(200, 206, 212));
                    g.DrawPath(border, path);
                }
                var swatch = new Rectangle(box.X + 7, box.Y + (box.Height - 14) / 2, 14, 14);
                using (var fill = new SolidBrush(QuickButtonStyle.ButtonColor(button, TabColorOf(button)))) g.FillRectangle(fill, swatch);
                g.DrawRectangle(Pens.Silver, swatch);
                using var small = new Font("Segoe UI", 8.5F);
                TextRenderer.DrawText(g, button.Color ?? "tab color", small,
                    new Rectangle(swatch.Right + 6, box.Y, box.Right - swatch.Right - 8, box.Height), Muted,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            else
            {
                DrawSwitch(g, r, col == askPriceColumn.Index ? button.AskPrice : button.IsActive);
            }
            e.Handled = true;
        }

        private static void DrawSwitch(Graphics g, Rectangle cell, bool on)
        {
            var track = new Rectangle(cell.X + (cell.Width - 38) / 2, cell.Y + (cell.Height - 20) / 2, 38, 20);
            using (var path = QuickButtonStyle.Rounded(track, 10))
            using (var fill = new SolidBrush(on ? QuickButtonStyle.Accent : Color.FromArgb(205, 210, 216)))
                g.FillPath(fill, path);
            var knob = new Rectangle(on ? track.Right - 18 : track.X + 2, track.Y + 2, 16, 16);
            g.FillEllipse(Brushes.White, knob);
        }

        #endregion

        #region Preview

        // Tabs as the cashier sees them (shown tabs that have shown buttons, then "Other"), and the selected tab's buttons
        private void RefreshPreview()
        {
            previewTabs.SuspendLayout();
            previewItems.SuspendLayout();
            Clear(previewTabs);
            Clear(previewItems);

            var shownTabs = _tabs.Where(t => !t.IsNoGroup && t.IsActive && ButtonsOf(t).Any(b => b.IsActive)).ToList();
            var otherShown = _allButtons.Any(b => b.GroupId == null && b.IsActive);
            if (shownTabs.Count > 0 && otherShown) shownTabs.Add(_tabs[^1]);

            foreach (var tab in shownTabs)
            {
                var button = new Button { Height = 36, Margin = new Padding(0, 0, 8, 0) };
                QuickButtonStyle.ApplyGroup(button, tab.IsNoGroup ? "Other" : tab.Name, tab.Color, ReferenceEquals(tab, _tab));
                button.Width = TextRenderer.MeasureText(button.Text, button.Font).Width + 32;
                var target = tab;
                button.Click += (_, _) => tabsList.SelectedItem = target;
                previewTabs.Controls.Add(button);
            }
            previewTabs.Visible = shownTabs.Count > 0;

            // No tabs: the cashier shows every button without a tab on one page
            var items = (shownTabs.Count == 0 ? _allButtons.Where(b => b.GroupId == null) : _buttons).Where(b => b.IsActive).ToList();
            var hidden = _tab != null && !_tab.IsNoGroup && !_tab.IsActive;
            previewNote.Text = hidden
                ? "This tab is hidden: the cashier does not see it."
                : "Saved at once · the cashier reloads when this window closes";
            previewNote.ForeColor = hidden ? Color.Firebrick : Muted;

            var width = ButtonWidth();
            foreach (var item in hidden ? new List<QuickButtonModel>() : items)
            {
                var button = new Button { Size = new Size(width, PreviewButtonHeight), Margin = new Padding(0, 0, 8, 8) };
                QuickButtonStyle.Apply(button, item, TabColorOf(item));
                var target = item;
                button.Click += (_, _) => { if (_buttons.Contains(target)) SelectRow(_buttons.IndexOf(target)); };
                previewItems.Controls.Add(button);
            }

            // Empty places to the end of the row, like the cashier screen
            var count = previewItems.Controls.Count;
            var empty = count == 0 ? _columns : (_columns - count % _columns) % _columns;
            for (var i = 0; i < empty; i++)
            {
                var slot = new Panel { Size = new Size(width, PreviewButtonHeight), Margin = new Padding(0, 0, 8, 8) };
                slot.Paint += (_, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var pen = new Pen(Color.FromArgb(205, 210, 216)) { DashStyle = DashStyle.Dash };
                    using var path = QuickButtonStyle.Rounded(new Rectangle(0, 0, slot.Width - 1, slot.Height - 1), 6);
                    pe.Graphics.DrawPath(pen, path);
                };
                previewItems.Controls.Add(slot);
            }

            previewTabs.ResumeLayout();
            previewItems.ResumeLayout();
        }

        private int ButtonWidth()
        {
            var width = previewItems.ClientSize.Width - previewItems.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
            return Math.Max(80, width / _columns - 8);
        }

        private void previewItems_Resize(object? sender, EventArgs e)
        {
            var width = ButtonWidth();
            foreach (Control c in previewItems.Controls) c.Width = width;
        }

        private static void Clear(Control panel)
        {
            foreach (var c in panel.Controls.Cast<Control>().ToList())
            {
                panel.Controls.Remove(c);
                c.Dispose();
            }
        }

        #endregion

        #region Buttons per row

        private async Task LoadColumnsAsync()
        {
            _loadingColumns = true;
            try
            {
                var value = await _saleRepository.GetSettingAsync(QuickButtonStyle.ColumnsSetting, QuickButtonStyle.DefaultColumns.ToString());
                _columns = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var columns)
                    ? Math.Clamp(columns, QuickButtonStyle.MinColumns, QuickButtonStyle.MaxColumns)
                    : QuickButtonStyle.DefaultColumns;
            }
            catch
            {
                _columns = QuickButtonStyle.DefaultColumns;
            }
            columnsUpDown.Value = _columns;
            _loadingColumns = false;
        }

        private async void columnsUpDown_ValueChanged(object? sender, EventArgs e)
        {
            if (_loadingColumns) return;
            _columns = (int)columnsUpDown.Value;
            RefreshPreview();
            try
            {
                await _saleRepository.SetSettingAsync(QuickButtonStyle.ColumnsSetting, _columns.ToString(CultureInfo.InvariantCulture));
                _changed = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save the setting:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        private void QuickButtonsForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (itemsGrid.IsCurrentCellInEditMode) itemsGrid.EndEdit();   // saves a label still being typed
        }

        private void QuickButtonsForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            if (!_changed) return;
            foreach (var cashier in Application.OpenForms.OfType<CashierForm>().ToList())
            {
                _ = cashier.LoadQuickButtonsAsync();
                _ = cashier.LoadVoiceSettingAsync();
            }
        }
    }
}
