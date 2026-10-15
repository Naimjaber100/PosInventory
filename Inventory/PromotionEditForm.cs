using System.ComponentModel;
using System.Globalization;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Adds a promotion (none passed) or edits one. The type decides what is asked:
    //   Price cut         items + the new price, or % off
    //   Multi-buy         items + how many make a group + the price of the group (each item by itself)
    //   Buy X get Y       items + buy X + get Y free (the cheapest of each group)
    //   Mix & match       items + how many make a group + the price of the group (across the items)
    //   Category %        a category + % off
    //   Invoice discount  the invoice total it starts from + % off
    // From / Until: the days it runs (not ticked = no limit). On: unticked stops it without deleting it.
    // The database checks the rules again (fn_promotion_save). Built in code, like the cashier's small windows.
    public class PromotionEditForm : Form
    {
        private static readonly string[] Hints =
        {
            "Example: Pepsi Can 40,000 instead of 45,000 this week. A new price, or % off, per unit.",
            "Example: Pepsi Can: 3 for 120,000. Every full group of 3 of the same item costs 120,000.",
            "Example: buy 2 shampoo, get 1 free. The cheapest piece of each group is free.",
            "Example: any 2 salads for 600,000. Like multi-buy, but the group is made across the items of the list.",
            "Example: all clothes 20% off. % off every item of the category and its sub-categories.",
            "Example: spend 5,000,000 or more: 5% off. On the invoice total, after the item promotions.",
        };

        private readonly PromotionRepository _promotionRepository = new();
        private readonly PromotionModel? _promotion;
        private readonly BindingList<PromotionItemModel> _items = new();
        private readonly List<(Label Caption, Control Field, Func<short, bool> Show)> _rows = new();

        private readonly TableLayoutPanel _fields;
        private readonly TextBox _nameText, _newPriceText, _percentText, _buyQtyText, _freeQtyText, _groupPriceText, _minTotalText;
        private readonly ComboBox _typeCombo, _categoryCombo;
        private readonly Label _hintLabel, _itemsCaption;
        private readonly DateTimePicker _fromPicker, _untilPicker;
        private readonly CheckBox _activeCheck;
        private readonly Panel _itemsPanel, _bottomPanel;
        private readonly DataGridView _itemsGrid;
        private readonly Button _saveButton;

        public PromotionEditForm(PromotionModel? promotion)
        {
            _promotion = promotion;

            Text = promotion == null ? "Add promotion" : $"Edit promotion - {promotion.Name}";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = new Size(680, 700);
            Font = new Font("Segoe UI", 10F);

            _fields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(16, 12, 16, 4) };
            _fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
            _fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _nameText = new TextBox { Width = 420, MaxLength = 100 };
            _typeCombo = new ComboBox { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            _typeCombo.Items.AddRange(PromotionType.All.Select(t => (object)t.Name).ToArray());
            _hintLabel = new Label { AutoSize = true, MaximumSize = new Size(430, 0), ForeColor = Color.DimGray, Margin = new Padding(3, 0, 3, 8) };
            _newPriceText = NumberBox();
            _percentText = NumberBox(90);
            _buyQtyText = NumberBox(90);
            _freeQtyText = NumberBox(90);
            _groupPriceText = NumberBox();
            _minTotalText = NumberBox();
            _categoryCombo = new ComboBox { Width = 420, DropDownStyle = ComboBoxStyle.DropDownList };
            _fromPicker = DayPicker();
            _untilPicker = DayPicker();
            _activeCheck = new CheckBox { Text = "On (untick to stop it without deleting it)", AutoSize = true, Checked = true, Margin = new Padding(3, 6, 3, 3) };

            AddRow("Name", _nameText, _ => true);
            AddRow("Type", _typeCombo, _ => true);
            AddRow(string.Empty, _hintLabel, _ => true);
            AddRow("New price (each)", _newPriceText, t => t == PromotionType.PriceCut);
            AddRow("% off", _percentText, t => t is PromotionType.PriceCut or PromotionType.Category or PromotionType.Invoice);
            AddRow("Category", _categoryCombo, t => t == PromotionType.Category);
            AddRow("Pieces in a group", _buyQtyText, t => t is PromotionType.MultiBuy or PromotionType.MixMatch or PromotionType.BuyXGetY);
            AddRow("Get free", _freeQtyText, t => t == PromotionType.BuyXGetY);
            AddRow("Price of the group", _groupPriceText, t => t is PromotionType.MultiBuy or PromotionType.MixMatch);
            AddRow("Invoice total from", _minTotalText, t => t == PromotionType.Invoice);
            AddRow("From (first day)", _fromPicker, _ => true);
            AddRow("Until (last day)", _untilPicker, _ => true);
            AddRow(string.Empty, _activeCheck, _ => true);

            // The items of a price cut, multi-buy, buy X get Y or mix & match
            _itemsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 0) };
            _itemsGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                DataSource = _items,
            };
            _itemsGrid.DataBindingComplete += (_, _) =>
            {
                if (_itemsGrid.Columns[nameof(PromotionItemModel.ItemName)] is { } name) name.FillWeight = 220;
                if (_itemsGrid.Columns[nameof(PromotionItemModel.SalePrice)] is { } price)
                {
                    price.DefaultCellStyle.Format = "#,0.##";
                    price.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            };
            _itemsGrid.KeyDown += (_, e) => { if (e.KeyCode == Keys.Delete) RemoveSelectedItems(); };
            var itemsBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, WrapContents = false, Padding = new Padding(0, 4, 0, 4) };
            var addItemsButton = new Button { Text = "+ Add items...", Size = new Size(140, 34) };
            addItemsButton.Click += (_, _) => AddItems();
            var removeButton = new Button { Text = "Remove", Size = new Size(100, 34), ForeColor = Color.Firebrick };
            removeButton.Click += (_, _) => RemoveSelectedItems();
            _itemsCaption = new Label { AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(12, 9, 3, 0) };
            itemsBar.Controls.AddRange(new Control[] { addItemsButton, removeButton, _itemsCaption });
            _itemsPanel.Controls.Add(_itemsGrid);
            _itemsPanel.Controls.Add(itemsBar);
            _items.ListChanged += (_, _) => UpdateItemsCaption();

            _bottomPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12, 8, 12, 8) };
            var cancelButton = new Button { Text = "Cancel", Size = new Size(110, 38), DialogResult = DialogResult.Cancel };
            _saveButton = new Button { Text = "Save", Size = new Size(140, 38) };
            _saveButton.Click += async (_, _) => await SaveAsync();
            _bottomPanel.Controls.Add(cancelButton);
            _bottomPanel.Controls.Add(_saveButton);

            // Docking goes from the last control added to the first: the fields on top, the items fill the rest
            Controls.AddRange(new Control[] { _itemsPanel, _fields, _bottomPanel });
            AcceptButton = _saveButton;
            CancelButton = cancelButton;
            AppTheme.Apply(this);

            if (promotion != null)
            {
                _nameText.Text = promotion.Name;
                _newPriceText.Text = Show(promotion.NewPrice);
                _percentText.Text = Show(promotion.Percent);
                _buyQtyText.Text = promotion.BuyQty?.ToString(CultureInfo.InvariantCulture);
                _freeQtyText.Text = promotion.FreeQty?.ToString(CultureInfo.InvariantCulture);
                _groupPriceText.Text = Show(promotion.GroupPrice);
                _minTotalText.Text = Show(promotion.MinTotal);
                SetDay(_fromPicker, promotion.StartsOn);
                SetDay(_untilPicker, promotion.EndsOn);
                _activeCheck.Checked = promotion.IsActive;
            }
            _typeCombo.SelectedIndexChanged += (_, _) => ShowType();
            _typeCombo.SelectedIndex = (promotion?.PromoType ?? PromotionType.PriceCut) - 1;   // shows the fields of the type
            Load += PromotionEditForm_Load;
        }

        /// <summary>The id of the saved promotion.</summary>
        public int SavedId { get; private set; }

        private short SelectedType => (short)(_typeCombo.SelectedIndex + 1);

        private static TextBox NumberBox(int width = 160) => new() { Width = width, TextAlign = HorizontalAlignment.Right };

        // Not ticked = no limit
        private static DateTimePicker DayPicker() => new()
        {
            Width = 160,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            ShowCheckBox = true,
            Checked = false,
        };

        private static void SetDay(DateTimePicker picker, DateTime? day)
        {
            if (day is not { } value) return;
            picker.Value = value.Date;
            picker.Checked = true;
        }

        private static string Show(decimal? value) => value?.ToString("#,0.##", CultureInfo.InvariantCulture) ?? string.Empty;

        private void AddRow(string caption, Control field, Func<short, bool> show)
        {
            var label = new Label { Text = caption, AutoSize = true, Margin = new Padding(3, 7, 3, 0) };
            var row = _fields.RowCount++;
            _fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _fields.Controls.Add(label, 0, row);
            _fields.Controls.Add(field, 1, row);
            _rows.Add((label, field, show));
        }

        // Shows what the chosen type asks for; the window is as tall as it needs
        private void ShowType()
        {
            var type = SelectedType;
            _fields.SuspendLayout();
            foreach (var (caption, field, show) in _rows)
            {
                caption.Visible = field.Visible = show(type);
                if (field == _percentText) caption.Text = type == PromotionType.PriceCut ? "or % off" : "% off";
                if (field == _buyQtyText) caption.Text = type == PromotionType.BuyXGetY ? "Buy (pieces)" : "Pieces in a group";
            }
            _hintLabel.Text = Hints[type - 1];
            _fields.ResumeLayout(true);

            var hasItems = PromotionType.HasItems(type);
            _itemsPanel.Visible = hasItems;
            var fieldsHeight = _fields.GetPreferredSize(new Size(ClientSize.Width, 0)).Height;
            ClientSize = new Size(ClientSize.Width, fieldsHeight + _bottomPanel.Height + (hasItems ? 300 : 8));
            UpdateItemsCaption();
        }

        private void UpdateItemsCaption()
        {
            _itemsCaption.Text = SelectedType switch
            {
                PromotionType.MultiBuy => $"{_items.Count} item(s)  ·  each item makes its own groups",
                PromotionType.BuyXGetY or PromotionType.MixMatch => $"{_items.Count} item(s)  ·  the groups are made across these items",
                _ => $"{_items.Count} item(s)",
            };
        }

        private async void PromotionEditForm_Load(object? sender, EventArgs e)
        {
            try
            {
                _categoryCombo.DisplayMember = nameof(CategoryModel.Path);
                _categoryCombo.ValueMember = nameof(CategoryModel.CategoryId);
                _categoryCombo.DataSource = (await new CategoryRepository().GetAllAsync()).ToList();
                if (_promotion?.CategoryId is int categoryId) _categoryCombo.SelectedValue = categoryId;

                if (_promotion != null && PromotionType.HasItems(_promotion.PromoType))
                    foreach (var item in await _promotionRepository.GetItemsAsync(_promotion.PromotionId)) _items.Add(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the promotion:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            _nameText.Focus();
        }

        private void AddItems()
        {
            using var picker = new QuickItemPickerForm("Add items to the promotion", anyItem: true);
            if (picker.ShowDialog(this) != DialogResult.OK) return;
            foreach (var item in picker.PickedItems.Where(i => _items.All(have => have.PackagingId != i.PackagingId)))
                _items.Add(new PromotionItemModel
                {
                    PackagingId = item.PackagingId,
                    ItemName = item.ItemName,
                    Barcode = item.Barcode,
                    SalePrice = item.SalePrice,
                    IsWeighted = item.IsWeighted,
                });
        }

        private void RemoveSelectedItems()
        {
            foreach (var item in _itemsGrid.SelectedRows.Cast<DataGridViewRow>().Select(r => r.DataBoundItem).OfType<PromotionItemModel>().ToList())
                _items.Remove(item);
        }

        private async Task SaveAsync()
        {
            var type = SelectedType;
            string? problem = null;
            Control? field = null;

            void Fail(string message, Control at)
            {
                if (problem == null) (problem, field) = (message, at);
            }
            decimal? Number(TextBox box, string what)
            {
                if (CustomerEditForm.TryParseAmount(box.Text, out var value)) return value;
                Fail($"Type {what}.", box);
                return null;
            }
            int? Whole(TextBox box, string what)
            {
                if (int.TryParse(box.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) return value;
                Fail($"Type {what} (a whole number).", box);
                return null;
            }

            var promotion = new PromotionModel
            {
                PromotionId = _promotion?.PromotionId ?? 0,
                Name = _nameText.Text.Trim(),
                PromoType = type,
                StartsOn = _fromPicker.Checked ? _fromPicker.Value.Date : null,
                EndsOn = _untilPicker.Checked ? _untilPicker.Value.Date : null,
                IsActive = _activeCheck.Checked,
            };
            if (promotion.Name.Length == 0) Fail("The name is required.", _nameText);
            if (promotion.EndsOn < promotion.StartsOn) Fail("The last day is before the first day.", _untilPicker);

            switch (type)
            {
                case PromotionType.PriceCut:
                    var hasPrice = _newPriceText.Text.Trim().Length > 0;
                    if (hasPrice == _percentText.Text.Trim().Length > 0) Fail("Type the new price, or the % off (one of the two).", _newPriceText);
                    else if (hasPrice) promotion.NewPrice = Number(_newPriceText, "the new price");
                    else promotion.Percent = Number(_percentText, "the % off");
                    break;
                case PromotionType.MultiBuy:
                case PromotionType.MixMatch:
                    promotion.BuyQty = Whole(_buyQtyText, "how many pieces make a group");
                    promotion.GroupPrice = Number(_groupPriceText, "the price of the group");
                    break;
                case PromotionType.BuyXGetY:
                    promotion.BuyQty = Whole(_buyQtyText, "how many pieces must be bought");
                    promotion.FreeQty = Whole(_freeQtyText, "how many pieces are free");
                    break;
                case PromotionType.Category:
                    promotion.CategoryId = _categoryCombo.SelectedValue as int?;
                    if (promotion.CategoryId == null) Fail("Choose the category.", _categoryCombo);
                    promotion.Percent = Number(_percentText, "the % off");
                    break;
                case PromotionType.Invoice:
                    promotion.MinTotal = Number(_minTotalText, "the invoice total the promotion starts from");
                    promotion.Percent = Number(_percentText, "the % off");
                    break;
            }
            if (PromotionType.HasItems(type) && _items.Count == 0) Fail("Add at least one item.", _itemsGrid);

            if (problem != null)
            {
                MessageBox.Show(this, problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                field?.Focus();
                return;
            }

            try
            {
                _saveButton.Enabled = false;
                SavedId = await _promotionRepository.SaveAsync(promotion, _items.Select(i => i.PackagingId));
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                var message = ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message;
                MessageBox.Show(this, "Could not save the promotion:\n" + message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _saveButton.Enabled = true;
            }
        }
    }
}
