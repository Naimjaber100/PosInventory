using System.ComponentModel;
using System.Globalization;
using Inventory.Models;
using Inventory.Printing;
using Inventory.Repositories;
using Inventory.Validation;

namespace Inventory
{
    // Cashier (point of sale) screen.
    // Left: invoice lines, barcode box, keypad, line functions, discount, totals, Pay / Hold / Recall / Void.
    // Right: last scanned item / change due, and the quick buttons for items without a barcode
    //        (set up in the back office: Item > POS buttons). Type a quantity or weight first, then tap.
    //        With groups (Salads, Drinks...) a row of group tabs shows above; tap a group, then the item.
    //        Without groups all buttons show on one page.
    //
    // Return (F4): the screen turns red, the scanned items come back, REFUND (F12) saves a return invoice (R-...).
    //
    // Keys: F1 barcode box, F2 qty, F4 return, F5 line discount, F6 invoice discount, F7 sales list, F8 hold, F9 recall,
    //       F10 void sale, F12 pay, +/- quantity, Del delete line, Up/Down choose line, Esc clear box.
    public partial class CashierForm : Form
    {
        private static readonly Color Accent = Color.FromArgb(34, 139, 117);
        private static readonly Color Dark = Color.FromArgb(22, 32, 43);
        private const int GroupColumns = 4;
        private const int GroupButtonHeight = 52;

        private readonly SaleRepository _saleRepository = new();
        private readonly QuickButtonRepository _quickButtonRepository = new();
        private readonly BindingList<PosLine> _lines = new();
        private List<QuickGroupModel> _quickGroups = new();     // the group tabs ("Other" = no group); empty = one page
        private List<QuickButtonModel> _quickButtons = new();
        private int _quickGroupId = -1;                           // the open tab (-1 = none yet: open the first one)
        private int _quickColumns = QuickButtonStyle.DefaultColumns;
        private CurrencySet _currencies = CurrencySet.Default;   // primary currency, the second one and the rates
        private int? _heldSaleId;          // the recalled held sale this screen is finishing
        private TextBox _entry;            // where the keypad types (barcode box or discount box)
        private bool _returnMode;          // Return (F4): the scanned items come back and are refunded
        private UserModel? _returnApprover;  // the Manager who opened the return (password asked at Return)
        private Button _payButton = null!, _quickCashButton = null!, _returnButton = null!, _holdButton = null!, _recallButton = null!;
        private static readonly Color ReturnRed = Color.FromArgb(192, 57, 43);

        public CashierForm()
        {
            InitializeComponent();
            _entry = scanBox;
            BuildButtons();

            _lines.ListChanged += (_, _) => UpdateTotals();
            linesGrid.DataSource = _lines;
            UpdateTotals();
        }

        #region Start

        private async void CashierForm_Load(object? sender, EventArgs e)
        {
            clockTimer.Start();
            clockTimer_Tick(null, EventArgs.Empty);
            try
            {
                storeLabel.Text = await new CompanyRepository().GetNameAsync();
            }
            catch (Exception ex)
            {
                ShowStatus("Could not connect to the database: " + ex.Message, Color.Salmon);
            }
            await LoadQuickButtonsAsync();
            UpdateInfo();
            UpdateTotals();
            await LoadCurrenciesAsync();
            try { await ReceiptSettings.LoadAsync(); }
            catch { /* no receipt settings: sales still work, nothing prints */ }
        }

        #region Receipt printing

        private int? _lastSaleId;   // the last sale / return saved here, for Reprint (Ctrl+P)
        private bool _noPrinterShown;

        // Prints in the background right after the sale; a problem only shows in the status line (the sale is saved)
        private async void PrintReceipt(int saleId, bool reprint = false)
        {
            _lastSaleId = saleId;
            var settings = ReceiptSettings.Current;
            if (!reprint && (settings == null || !settings.PrintAfterSale)) return;
            if (!reprint && settings is { HasPrinter: false })
            {
                if (_noPrinterShown) return;   // say it once, not after every sale
                _noPrinterShown = true;
            }

            var problem = await ReceiptPrinter.PrintSaleAsync(saleId, _currencies);
            if (problem != null) ShowStatus(problem + "  ·  Ctrl+P to print it again", Color.Khaki);
            else if (reprint) ShowStatus("Receipt printed again.", Color.Gainsboro);
        }

        private void ReprintLast()
        {
            if (_lastSaleId is not int id) { ShowStatus("No receipt to print yet: finish a sale first (or use Sales F7).", Color.Khaki); return; }
            PrintReceipt(id, reprint: true);
            FocusScan();
        }

        #endregion

        private void CashierForm_Shown(object? sender, EventArgs e) => FocusScan();

        private void clockTimer_Tick(object? sender, EventArgs e) =>
            clockLabel.Text = DateTime.Now.ToString("ddd dd/MM/yyyy  HH:mm");

        private void UpdateInfo()
        {
            var held = _heldSaleId.HasValue ? $"   ·   Recalled held sale #{_heldSaleId}" : string.Empty;
            var mode = _returnMode ? "RETURN   ·   " : string.Empty;
            var rate = _currencies.SecondRateText.Length > 0 ? $"   ·   {_currencies.SecondRateText}" : string.Empty;
            infoLabel.Text = $"{mode}Cashier: {AppSession.UserName}{rate}{held}";
        }

        /// <summary>(Re)loads the currencies and rates. Also called when they are changed in Settings.</summary>
        public async Task LoadCurrenciesAsync()
        {
            try
            {
                _currencies = await new CurrencyRepository().GetSetAsync();
            }
            catch (Exception ex)
            {
                ShowStatus("Could not load the currencies: " + ex.Message, Color.Salmon);
                return;
            }
            UpdateInfo();
            UpdateTotals();
            if (_currencies.SecondRateMissing)
                ShowStatus($"No exchange rate for {_currencies.Second!.Code} - sales cannot be saved. Set it in Settings > Currencies.", Color.Salmon);
        }

        // Keypad, line functions and actions: built here so they all share one look
        private void BuildButtons()
        {
            string[,] keys = { { "7", "8", "9", "⌫" }, { "4", "5", "6", "C" }, { "1", "2", "3", "Enter" }, { "0", "00", ".", "*" } };
            for (var r = 0; r < 4; r++)
                for (var c = 0; c < 4; c++)
                {
                    var key = keys[r, c];
                    var isDigit = char.IsDigit(key[0]) || key == ".";
                    var button = MakeButton(key, isDigit ? 20F : 14F, (_, _) => KeypadPressed(key));
                    if (key == "Enter") { button.BackColor = Dark; button.ForeColor = Color.White; }
                    if (key == "C") button.ForeColor = Color.Firebrick;
                    keypadTable.Controls.Add(button, c, r);
                }

            (string Text, EventHandler Click)[] functions =
            {
                ("Qty  (F2)", (_, _) => SetQuantity()),
                ("Return  (F4)", (_, _) => ToggleReturnMode()),
                ("Line disc.  (F5)", (_, _) => SetLineDiscount()),
                ("+1", (_, _) => ChangeQuantity(+1)),
                ("−1", (_, _) => ChangeQuantity(-1)),
                ("Delete line  (Del)", (_, _) => DeleteSelectedLine()),
            };
            for (var i = 0; i < functions.Length; i++)
            {
                var button = MakeButton(functions[i].Text, 11F, functions[i].Click);
                if (i == functions.Length - 1) button.ForeColor = Color.Firebrick;
                if (i == 1) { _returnButton = button; button.ForeColor = ReturnRed; }
                functionTable.Controls.Add(button, 0, i);
            }

            _holdButton = MakeButton("Hold  (F8)", 11F, async (_, _) => await HoldAsync());
            _recallButton = MakeButton("Recall  (F9)", 11F, async (_, _) => await RecallAsync());
            actionsTable.Controls.Add(_holdButton, 0, 0);
            actionsTable.Controls.Add(_recallButton, 1, 0);
            var voidButton = MakeButton("Void sale  (F10)", 11F, (_, _) => VoidSale());
            voidButton.ForeColor = Color.Firebrick;
            actionsTable.Controls.Add(voidButton, 2, 0);

            // Sales (top bar, F7): this cashier's invoices with total sales and profit
            var salesButton = new Button
            {
                Text = "Sales  (F7)",
                Dock = DockStyle.Right,
                Width = 120,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(34, 139, 117),
                TabStop = false,
                UseVisualStyleBackColor = false,
            };
            salesButton.FlatAppearance.BorderSize = 0;
            salesButton.Click += (_, _) => ShowSales();
            topBar.Controls.Add(salesButton);

            // Reprint (top bar, Ctrl+P): the receipt of the last sale or return again
            var reprintButton = new Button
            {
                Text = "Reprint",
                Dock = DockStyle.Right,
                Width = 100,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Dark,
                TabStop = false,
                UseVisualStyleBackColor = false,
            };
            reprintButton.FlatAppearance.BorderColor = Color.Gray;
            reprintButton.Click += (_, _) => ReprintLast();
            topBar.Controls.Add(reprintButton);

            // Log out (top bar, right): back to the login screen, after the open-sale check of FormClosing
            var logOutButton = new Button
            {
                Text = "Log out",
                Dock = DockStyle.Right,
                Width = 110,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Dark,
                TabStop = false,
                UseVisualStyleBackColor = false,
            };
            logOutButton.FlatAppearance.BorderColor = Color.Gray;
            logOutButton.Click += (_, _) => { if (!AppSession.LogOut()) FocusScan(); };
            topBar.Controls.Add(logOutButton);

            // Quick cash (F11): the exact total paid in cash (primary currency), no payment window
            _quickCashButton = MakeButton("CASH\n(F11)", 15F, async (_, _) => await QuickCashAsync());
            _quickCashButton.BackColor = Color.FromArgb(46, 125, 50);
            _quickCashButton.ForeColor = Color.White;
            _quickCashButton.FlatAppearance.BorderSize = 0;
            actionsTable.Controls.Add(_quickCashButton, 0, 1);

            _payButton = MakeButton("PAY  (F12)", 22F, async (_, _) => await PayOrRefundAsync());
            _payButton.BackColor = Accent;
            _payButton.ForeColor = Color.White;
            _payButton.FlatAppearance.BorderSize = 0;
            actionsTable.Controls.Add(_payButton, 1, 1);
            actionsTable.SetColumnSpan(_payButton, 2);
        }

        private static Button MakeButton(string text, float fontSize, EventHandler onClick)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                Font = new Font("Segoe UI Semibold", fontSize, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(244, 246, 248),
                TabStop = false,
                UseVisualStyleBackColor = false,
            };
            button.FlatAppearance.BorderColor = Color.Silver;
            button.Click += onClick;
            return button;
        }

        #endregion

        #region Quick buttons

        /// <summary>(Re)loads the groups and quick buttons on the right. Also called when they are changed in the back office.</summary>
        public async Task LoadQuickButtonsAsync()
        {
            List<QuickGroupModel> groups;
            List<QuickButtonModel> buttons;
            try
            {
                groups = (await _quickButtonRepository.GetGroupsAsync(forCashier: true)).ToList();
                buttons = (await _quickButtonRepository.GetAllAsync(forCashier: true)).ToList();
                var columns = await _saleRepository.GetSettingAsync(QuickButtonStyle.ColumnsSetting);
                _quickColumns = int.TryParse(columns, NumberStyles.Integer, CultureInfo.InvariantCulture, out var c)
                    ? Math.Clamp(c, QuickButtonStyle.MinColumns, QuickButtonStyle.MaxColumns)
                    : QuickButtonStyle.DefaultColumns;
            }
            catch (Exception ex)
            {
                quickHint.Text = "Could not load the quick buttons: " + ex.Message;
                return;
            }

            // Buttons of inactive or hidden groups do not show; buttons without a group go under "Other"
            var shownGroupIds = groups.Select(g => g.GroupId).ToHashSet();
            _quickButtons = buttons.Where(b => b.GroupId == null || shownGroupIds.Contains(b.GroupId.Value)).ToList();
            if (groups.Count > 0 && _quickButtons.Any(b => b.GroupId == null))
                groups.Add(new QuickGroupModel { GroupId = QuickGroupModel.NoGroupId, Name = "Other" });
            _quickGroups = groups;

            BuildGroupTabs();
            ShowQuickGroup(_quickGroups.Any(g => g.GroupId == _quickGroupId) ? _quickGroupId : _quickGroups.FirstOrDefault()?.GroupId ?? 0);
        }

        // The row of group tabs above the item buttons (hidden when there are no groups)
        private void BuildGroupTabs()
        {
            groupPanel.SuspendLayout();
            ClearButtons(groupPanel);
            foreach (var group in _quickGroups)
            {
                var button = new Button { Margin = new Padding(3), Tag = group };
                button.Click += (_, _) => { ShowQuickGroup(group.GroupId); FocusScan(); };
                groupPanel.Controls.Add(button);
            }
            groupPanel.Visible = _quickGroups.Count > 0;
            groupPanel.ResumeLayout();
        }

        // The item buttons of one group (all of them when there are no groups)
        private void ShowQuickGroup(int groupId)
        {
            _quickGroupId = groupId;
            foreach (var tab in groupPanel.Controls.OfType<Button>())
            {
                var group = (QuickGroupModel)tab.Tag!;
                QuickButtonStyle.ApplyGroup(tab, group.Name, group.Color, group.GroupId == groupId);
            }

            var items = _quickGroups.Count == 0
                ? _quickButtons
                : _quickButtons.Where(b => (b.GroupId ?? QuickGroupModel.NoGroupId) == groupId).ToList();

            quickPanel.SuspendLayout();
            ClearButtons(quickPanel);
            foreach (var item in items)
            {
                var button = new Button { Margin = new Padding(3) };
                var tabColor = _quickGroups.FirstOrDefault(g => g.GroupId == (item.GroupId ?? QuickGroupModel.NoGroupId))?.Color;
                QuickButtonStyle.Apply(button, item, tabColor);
                button.Click += (_, _) => QuickButtonPressed(item);
                quickPanel.Controls.Add(button);
            }
            quickHint.Visible = items.Count == 0;
            quickHint.Text = "No quick buttons yet. Add items without a barcode (bread, vegetables, dishes...) " +
                             "in the back office: Item > POS buttons.";
            quickPanel.ResumeLayout();
            SizeQuickButtons();
            quickPanel.AutoScrollPosition = Point.Empty;
        }

        private static void ClearButtons(Control panel)
        {
            foreach (var old in panel.Controls.OfType<Button>().ToList())
            {
                panel.Controls.Remove(old);
                old.Dispose();
            }
        }

        // _quickColumns item buttons and GroupColumns group tabs per row, filling the panel width
        private void SizeQuickButtons()
        {
            var width = quickPanel.ClientSize.Width - quickPanel.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
            var buttonWidth = Math.Max(80, width / _quickColumns - 6);
            var buttonHeight = Math.Clamp(buttonWidth * 3 / 5, 64, 100);
            foreach (var button in quickPanel.Controls.OfType<Button>())
                button.Size = new Size(buttonWidth, buttonHeight);

            var groupWidth = Math.Max(80, (groupPanel.ClientSize.Width - groupPanel.Padding.Horizontal) / GroupColumns - 6);
            foreach (var tab in groupPanel.Controls.OfType<Button>())
                tab.Size = new Size(groupWidth, GroupButtonHeight);
        }

        private void quickPanel_Resize(object? sender, EventArgs e) => SizeQuickButtons();

        private async void reloadQuickMenuItem_Click(object? sender, EventArgs e)
        {
            await LoadQuickButtonsAsync();
            FocusScan();
        }

        // The number in the barcode box (from the keypad) is the quantity, or the weight for a weighted item.
        // Ask-price buttons: the number is the price ("2*15000" = 2 at 15,000).
        private void QuickButtonPressed(QuickButtonModel item)
        {
            if (item.AskPrice)
            {
                SellAtTypedPrice(item);
                return;
            }

            var text = scanBox.Text.Trim().TrimEnd('*').Trim();
            var quantity = 1m;
            if (text.Length > 0 && (!TryParseNumber(text, out quantity) || quantity <= 0))
            {
                ShowStatus("Type a quantity (or weight) with the keypad, then tap the item.", Color.Khaki);
                FocusScan();
                return;
            }
            if (text.Length == 0 && item.IsWeighted)
            {
                System.Media.SystemSounds.Hand.Play();
                ShowStatus($"Type the weight ({item.BaseUnit}) with the keypad, then tap {item.DisplayText}.", Color.Khaki);
                FocusScan();
                return;
            }

            if (AddItem(item, quantity)) scanBox.Clear();
            FocusScan();
        }

        private void SellAtTypedPrice(QuickButtonModel item)
        {
            var text = scanBox.Text.Trim();
            var quantity = 1m;
            var star = text.IndexOf('*');
            if (star > 0 && TryParseNumber(text[..star], out var q) && q > 0)
            {
                quantity = q;
                text = text[(star + 1)..].Trim();
            }
            if (!TryParseNumber(text, out var price) || price <= 0)
            {
                System.Media.SystemSounds.Hand.Play();
                ShowStatus($"Type the price of {item.DisplayText} with the keypad, then tap it.", Color.Khaki);
                FocusScan();
                return;
            }

            if (AddItem(item.WithPrice(Math.Round(price, 2)), quantity)) scanBox.Clear();
            FocusScan();
        }

        #endregion

        #region Keyboard and keypad

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var scanEmpty = scanBox.Text.Length == 0;
            switch (keyData)
            {
                case Keys.Control | Keys.P: ReprintLast(); return true;
                case Keys.F1: FocusScan(); return true;
                case Keys.F2: SetQuantity(); return true;
                case Keys.F4: ToggleReturnMode(); return true;
                case Keys.F5: SetLineDiscount(); return true;
                case Keys.F6: if (!_returnMode) { discountBox.Focus(); discountBox.SelectAll(); } return true;
                case Keys.F7: ShowSales(); return true;
                case Keys.F8: _ = HoldAsync(); return true;
                case Keys.F9: _ = RecallAsync(); return true;
                case Keys.F10: VoidSale(); return true;
                case Keys.F11: _ = QuickCashAsync(); return true;
                case Keys.F12: _ = PayOrRefundAsync(); return true;
                case Keys.Escape:
                    if (_entry.Text.Length > 0) _entry.Clear(); else FocusScan();
                    return true;
                case Keys.Add when scanEmpty && ActiveControl == scanBox:
                    ChangeQuantity(+1); return true;
                case Keys.Subtract when scanEmpty && ActiveControl == scanBox:
                    ChangeQuantity(-1); return true;
                case Keys.Delete when scanEmpty && ActiveControl == scanBox:
                    DeleteSelectedLine(); return true;
                case Keys.Up when ActiveControl == scanBox:
                    MoveSelection(-1); return true;
                case Keys.Down when ActiveControl == scanBox:
                    MoveSelection(+1); return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void entry_Enter(object? sender, EventArgs e)
        {
            if (sender is TextBox box) _entry = box;
        }

        private void KeypadPressed(string key)
        {
            var box = _entry;
            switch (key)
            {
                case "⌫":
                    if (box.SelectionLength > 0) box.SelectedText = string.Empty;
                    else if (box.SelectionStart > 0)
                    {
                        var at = box.SelectionStart;
                        box.Text = box.Text.Remove(at - 1, 1);
                        box.SelectionStart = at - 1;
                    }
                    break;
                case "C":
                    box.Clear();
                    break;
                case "Enter":
                    if (box == scanBox) _ = ScanAsync();
                    else FocusScan();
                    return;
                default:
                    box.SelectedText = key;   // types at the caret (replaces a selection)
                    break;
            }
            box.Focus();
        }

        private async void scanBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await ScanAsync();
        }

        private void discountBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            FocusScan();
        }

        private void FocusScan()
        {
            scanBox.Focus();
            scanBox.SelectAll();
            _entry = scanBox;
        }

        #endregion

        #region Scanning and lines

        private async Task ScanAsync()
        {
            var text = scanBox.Text.Trim();
            if (text.Length == 0) return;

            // A scale label: the item and its weight or price are in the barcode (appsettings.json "ScaleBarcode")
            if (ScaleBarcode.TryParse(text, out var label))
            {
                await AddScaleLabelAsync(label);
                return;
            }

            // "3*barcode" or "3*" + name: add 3 pieces
            var quantity = 1m;
            var star = text.IndexOf('*');
            if (star > 0 && TryParseNumber(text[..star], out var q) && q > 0)
            {
                quantity = q;
                text = text[(star + 1)..].Trim();
                if (text.Length == 0) { ShowStatus("Type or scan the item after the *.", Color.Khaki); return; }
            }

            try
            {
                var matches = (await _saleRepository.FindItemAsync(text)).ToList();
                PosItemModel? item;
                if (matches.Count == 0)
                {
                    System.Media.SystemSounds.Hand.Play();
                    ShowStatus(ScaleBarcode.IsBadScaleLabel(text)
                        ? $"\"{text}\" looks like a scale label, but its last digit (check digit) is wrong. Scan the label again."
                        : $"Not found: \"{text}\"", Color.Salmon);
                    scanBox.SelectAll();
                    return;
                }
                if (matches.Count == 1)
                {
                    item = matches[0];
                }
                else
                {
                    using var picker = new PackagingPickerForm(matches, text);
                    if (picker.ShowDialog(this) != DialogResult.OK) { FocusScan(); return; }
                    item = picker.SelectedItem as PosItemModel;
                }

                if (item != null) AddItem(item, quantity);
                scanBox.Clear();
                FocusScan();
            }
            catch (Exception ex)
            {
                ShowStatus("Search failed: " + ex.Message, Color.Salmon);
            }
        }

        // Weight label: the weight is the quantity at the item's price.
        // Price label: a weighed item gets the weight that this price buys (price / price per kg);
        //              an item sold by piece is added once at the price on the label.
        private async Task AddScaleLabelAsync(ScaleLabel label)
        {
            try
            {
                var item = await _saleRepository.FindScaleItemAsync(label.ItemCode);
                if (item == null)
                {
                    System.Media.SystemSounds.Hand.Play();
                    ShowStatus($"Scale label: no scale product has code {label.ItemCode}. Set it on the item (Scale product).", Color.Salmon);
                    scanBox.SelectAll();
                    return;
                }

                bool added;
                if (label.Kind == ScaleValueKind.Weight)
                {
                    if (label.Value <= 0) { ShowStatus("The weight on the label is 0.", Color.Khaki); scanBox.SelectAll(); return; }
                    if (!item.IsWeighted)
                    {
                        ShowStatus($"{item.ItemName} is not sold by weight: tick \"Sold by weight\" on the item.", Color.Khaki);
                        scanBox.SelectAll();
                        return;
                    }
                    added = AddItem(item, label.Value);
                }
                else if (item.IsWeighted && item.SalePrice > 0)
                {
                    var weight = Math.Round(label.Value / item.SalePrice, 3);
                    if (weight <= 0) { ShowStatus("The price on the label is too small.", Color.Khaki); scanBox.SelectAll(); return; }
                    added = AddItem(item, weight);
                }
                else
                {
                    if (label.Value <= 0) { ShowStatus("The price on the label is 0.", Color.Khaki); scanBox.SelectAll(); return; }
                    added = AddItem(item.WithPrice(Math.Round(label.Value, 2)), 1);
                }

                if (added) scanBox.Clear();   // the big panel shows "weight x price = total"; stock warnings stay
                FocusScan();
            }
            catch (Exception ex)
            {
                ShowStatus("Search failed: " + ex.Message, Color.Salmon);
            }
        }

        // The same packaging at the same price adds to its line; otherwise a new line. False when not added.
        private bool AddItem(PosItemModel item, decimal quantity)
        {
            if (!item.IsWeighted && quantity % 1 != 0)
            {
                ShowStatus($"{item.ItemName} is sold in whole units only.", Color.Khaki);
                return false;
            }

            var line = _lines.FirstOrDefault(l => l.PackagingId == item.PackagingId && l.UnitPrice == item.SalePrice && l.Discount == 0);
            if (line != null)
            {
                line.Quantity += quantity;
            }
            else
            {
                line = new PosLine
                {
                    PackagingId = item.PackagingId,
                    ProductId = item.ProductId,
                    ItemName = item.ItemName,
                    Barcode = item.Barcode,
                    Factor = item.Factor,
                    IsWeighted = item.IsWeighted,
                    BaseUnit = item.BaseUnit,
                    StockQty = item.StockQty,
                    Quantity = quantity,
                    UnitPrice = item.SalePrice,
                };
                _lines.Add(line);
            }

            SelectLine(line);
            ShowLastItem(line);

            var needed = _lines.Where(l => l.ProductId == line.ProductId).Sum(l => l.Quantity * l.Factor);
            if (_returnMode)
                ShowStatus("Coming back. Scan the next item, or REFUND (F12).", Color.White);
            else if (needed > item.StockQty)
                ShowStatus($"Stock check: only {item.StockQty:#,0.###} {item.BaseUnit} in stock.", Color.Khaki);
            else if (item.SalePrice == 0)
                ShowStatus("This item has no price. A manager must set it in the item list.", Color.Khaki);
            else
                ShowStatus("Ready", Color.Gainsboro);
            return true;
        }

        private PosLine? SelectedLine => linesGrid.CurrentRow?.DataBoundItem as PosLine;

        private void SelectLine(PosLine line)
        {
            var index = _lines.IndexOf(line);
            if (index < 0 || index >= linesGrid.Rows.Count) return;
            linesGrid.CurrentCell = linesGrid.Rows[index].Cells.Cast<DataGridViewCell>().First(c => c.Visible);
            linesGrid.FirstDisplayedScrollingRowIndex = Math.Max(0, index - 3);
        }

        private void MoveSelection(int step)
        {
            if (_lines.Count == 0) return;
            var index = SelectedLine == null ? _lines.Count - 1 : _lines.IndexOf(SelectedLine) + step;
            SelectLine(_lines[Math.Clamp(index, 0, _lines.Count - 1)]);
        }

        // Qty / Line disc. use the number typed in the barcode box for the selected line (the cashier cannot change prices)
        private bool TryTakeNumber(string what, out decimal value, out bool percent)
        {
            value = 0;
            percent = false;
            if (SelectedLine == null)
            {
                ShowStatus("Select a line first.", Color.Khaki);
                return false;
            }
            var text = scanBox.Text.Trim();
            if (text.EndsWith('%')) { percent = true; text = text[..^1]; }
            if (!TryParseNumber(text, out value))
            {
                ShowStatus($"Type the {what} with the keypad, then press the button.", Color.Khaki);
                FocusScan();
                return false;
            }
            return true;
        }

        private void SetQuantity()
        {
            if (!TryTakeNumber("quantity", out var qty, out _)) return;
            var line = SelectedLine!;
            var check = SaleValidator.ValidateLine(new PosLine { ItemName = line.ItemName, IsWeighted = line.IsWeighted, Quantity = qty, UnitPrice = line.UnitPrice }, _lines.IndexOf(line));
            if (!check.IsValid) { ShowStatus(check.Errors[0].Message, Color.Khaki); return; }

            line.Quantity = qty;
            if (line.Discount > line.Quantity * line.UnitPrice) line.Discount = 0;
            DoneWithNumber(line);
        }

        private void SetLineDiscount()
        {
            if (!TryTakeNumber("discount (amount or 10%)", out var value, out var percent)) return;
            var line = SelectedLine!;
            var gross = line.Quantity * line.UnitPrice;
            var discount = Math.Round(percent ? gross * value / 100 : value, 2);
            if (discount < 0 || discount > gross)
            {
                ShowStatus("The discount must be between 0 and the line amount.", Color.Khaki);
                return;
            }
            line.Discount = discount;
            DoneWithNumber(line);
        }

        private void DoneWithNumber(PosLine line)
        {
            scanBox.Clear();
            ShowLastItem(line);
            ShowStatus("Ready", Color.Gainsboro);
            FocusScan();
        }

        private void ChangeQuantity(int step)
        {
            var line = SelectedLine;
            if (line == null) return;
            if (line.Quantity + step <= 0)
            {
                DeleteSelectedLine();
                return;
            }
            line.Quantity += step;
            if (line.Discount > line.Quantity * line.UnitPrice) line.Discount = 0;
            ShowLastItem(line);
            FocusScan();
        }

        private void DeleteSelectedLine()
        {
            var line = SelectedLine;
            if (line == null) return;
            var index = _lines.IndexOf(line);
            _lines.Remove(line);
            ShowStatus($"Removed: {line.ItemName}", Color.Gainsboro);
            if (_lines.Count > 0) SelectLine(_lines[Math.Min(index, _lines.Count - 1)]);
            FocusScan();
        }

        #endregion

        #region Totals

        private decimal SubTotal => _lines.Sum(l => l.LineTotal);

        // Invoice discount from the box: an amount, or a percent of the sub total when % is pressed
        private bool TryGetInvoiceDiscount(out decimal discount)
        {
            discount = 0;
            var text = discountBox.Text.Trim();
            if (text.Length == 0) return true;
            if (!TryParseNumber(text, out var value) || value < 0) return false;
            discount = Math.Round(percentCheck.Checked ? SubTotal * value / 100 : value, 2);
            return true;
        }

        private void discountBox_TextChanged(object? sender, EventArgs e) => UpdateTotals();

        private void UpdateTotals()
        {
            var sub = SubTotal;
            var validDiscount = TryGetInvoiceDiscount(out var discount);
            discountBox.BackColor = validDiscount && discount <= sub ? SystemColors.Window : Color.MistyRose;
            var total = sub - (validDiscount ? discount : 0);

            subTotalCaption.Text = $"Sub total ({_lines.Count})";
            subTotalValue.Text = sub.ToString("#,0");
            totalValue.Text = total.ToString("#,0");
            totalUsdValue.Text = _currencies.FormatSecond(total);
        }

        private void linesGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewColumn column in linesGrid.Columns)
            {
                if (column.ValueType == typeof(decimal))
                {
                    column.DefaultCellStyle.Format = column.DataPropertyName == nameof(PosLine.Quantity) ? "#,0.###" : "#,0";
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    column.FillWeight = 22;
                }
            }
            if (linesGrid.Columns[nameof(PosLine.ItemName)] is { } name) name.FillWeight = 60;
            if (linesGrid.Columns[nameof(PosLine.LineTotal)] is { } total)
                total.DefaultCellStyle.Font = new Font(linesGrid.Font, FontStyle.Bold);
        }

        #endregion

        #region Pay, hold, recall, void

        private async Task PayAsync()
        {
            if (!ValidateSale(out var discount)) return;
            var total = SubTotal - discount;

            using var payment = new PaymentForm(total, _currencies);
            if (payment.ShowDialog(this) != DialogResult.OK) { FocusScan(); return; }

            await SaveSaleAsync(payment.Payments, discount);
        }

        // Quick cash (F11): the customer pays the exact total in cash, in the primary currency. No payment window, no change.
        private async Task QuickCashAsync()
        {
            if (_returnMode) { ShowStatus("Quick cash is for sales. Use REFUND (F12) for a return.", Color.Khaki); return; }
            if (_saving) return;
            if (!ValidateSale(out var discount)) return;
            var total = SubTotal - discount;

            var cash = new SalePaymentModel { Method = PaymentMethod.Cash, Currency = _currencies.Primary.Code, Amount = total };
            await SaveSaleAsync(new[] { cash }, discount);
        }

        private bool _saving;   // stops a double press of F11 / F12 from saving the same sale twice

        private async Task SaveSaleAsync(IEnumerable<SalePaymentModel> payments, decimal discount)
        {
            if (_saving) return;
            try
            {
                _saving = true;
                UseWaitCursor = true;
                var result = await _saleRepository.SaveSaleAsync(_lines, payments, discount, _heldSaleId, AppConfig.CurrentUserId);
                PrintReceipt(result.SaleId);
                ClearSale();

                lastItemName.Text = $"Invoice {result.InvoiceNo}";
                lastItemPrice.Text = $"Change  {result.ChangeLbp:#,0}";
                var changeSecond = result.ChangeLbp > 0 ? _currencies.FormatSecond(result.ChangeLbp) : string.Empty;
                ShowStatus($"Total {_currencies.FormatPrimary(result.TotalLbp)}"
                           + (changeSecond.Length > 0 ? $"  ·  change {changeSecond}" : string.Empty) + "  ·  next customer",
                    Color.FromArgb(120, 220, 190));
            }
            catch (Exception ex)
            {
                // Nothing was saved (fn_save_sale is all or nothing); the sale stays on the screen
                MessageBox.Show(this, "The sale was not saved:\n" + ex.Message, "Pay", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _saving = false;
                UseWaitCursor = false;
                FocusScan();
            }
        }

        private async Task HoldAsync()
        {
            if (_returnMode) { ShowStatus("A return cannot be held: refund (F12) or cancel it (F10).", Color.Khaki); return; }
            if (_lines.Count == 0) { ShowStatus("Nothing to hold.", Color.Khaki); return; }
            if (!ValidateSale(out var discount)) return;
            if (discount > 0 && MessageBox.Show(this, "The invoice discount is not kept on a held sale. Hold anyway?", "Hold",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                var id = await _saleRepository.HoldSaleAsync(_lines, _heldSaleId, AppConfig.CurrentUserId);
                ClearSale();
                lastItemName.Text = "Sale held";
                lastItemPrice.Text = $"#{id}";
                ShowStatus("Recall it with F9 to finish it.", Color.Gainsboro);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not hold the sale:\n" + ex.Message, "Hold", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                FocusScan();
            }
        }

        private async Task RecallAsync()
        {
            if (_returnMode) { ShowStatus("Go back to selling (F4) to recall a held sale.", Color.Khaki); return; }
            if (_lines.Count > 0)
            {
                ShowStatus("Finish, hold (F8) or void (F10) the current sale first.", Color.Khaki);
                return;
            }

            using var form = new HeldSalesForm();
            if (form.ShowDialog(this) != DialogResult.OK || form.Selected == null) { FocusScan(); return; }

            try
            {
                var lines = await _saleRepository.GetHeldLinesAsync(form.Selected.SaleId);
                ClearSale();
                foreach (var l in lines)
                {
                    _lines.Add(new PosLine
                    {
                        PackagingId = l.PackagingId,
                        ProductId = l.ProductId,
                        ItemName = l.ItemName,
                        Barcode = l.Barcode,
                        Factor = l.Factor,
                        IsWeighted = l.IsWeighted,
                        BaseUnit = l.BaseUnit,
                        StockQty = l.StockQty,
                        Quantity = l.Quantity,
                        UnitPrice = l.UnitPrice,
                        Discount = l.Discount,
                    });
                }
                _heldSaleId = form.Selected.SaleId;
                UpdateInfo();
                lastItemName.Text = $"Recalled sale #{_heldSaleId}";
                lastItemPrice.Text = SubTotal.ToString("#,0");
                ShowStatus("Pay (F12) to finish it, or hold it again (F8).", Color.Gainsboro);
                if (_lines.Count > 0) SelectLine(_lines[^1]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not recall the sale:\n" + ex.Message, "Recall", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                FocusScan();
            }
        }

        // Sales (F7): the invoices of this cashier (a Manager can see every cashier); the sale on the screen stays
        private void ShowSales()
        {
            using var form = new SalesHistoryForm();
            form.ShowDialog(this);
            FocusScan();
        }

        private Task PayOrRefundAsync() => _returnMode ? RefundAsync() : PayAsync();

        // Return (F4): only with a Manager's password (a logged-in Manager goes straight in).
        // The screen turns red; the items on it are the ones the customer brings back.
        // Items already scanned can be turned into a return (scan first, then Return), and back into a sale.
        // F4 again goes back to selling.
        private async void ToggleReturnMode()
        {
            if (_lines.Count > 0)
            {
                if (_heldSaleId.HasValue)
                {
                    MessageBox.Show(this, "This is a recalled held sale: it cannot become a return.\nPay it, hold it again or void it first.",
                        "Return", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    FocusScan();
                    return;
                }
                var question = _returnMode
                    ? $"Turn these {_lines.Count} line(s) back into a sale?"
                    : $"Turn these {_lines.Count} line(s) into a RETURN?\nThe customer brings them back and gets the money.";
                if (MessageBox.Show(this, question, "Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    FocusScan();
                    return;
                }
            }

            if (!_returnMode)
            {
                _returnApprover = AppSession.IsManager ? AppSession.CurrentUser : await CashierDialogs.AskManagerApprovalAsync(this, "Only a manager can open a return.");
                if (_returnApprover == null)
                {
                    ShowStatus("Return not allowed without a manager password.", Color.Salmon);
                    FocusScan();
                    return;
                }
            }
            SetReturnMode(!_returnMode);
            if (_lines.Count > 0 && SelectedLine is { } line) ShowLastItem(line);
        }

        private void SetReturnMode(bool on)
        {
            _returnMode = on;
            if (!on) _returnApprover = null;   // the next return needs a manager again
            var back = on ? ReturnRed : Dark;
            topBar.BackColor = back;
            lastItemPanel.BackColor = back;
            _payButton.Text = on ? "REFUND  (F12)" : "PAY  (F12)";
            _payButton.BackColor = on ? ReturnRed : Accent;
            _returnButton.Text = on ? "Sale  (F4)" : "Return  (F4)";
            _returnButton.BackColor = on ? Color.FromArgb(253, 226, 222) : Color.FromArgb(244, 246, 248);
            _holdButton.Enabled = _recallButton.Enabled = _quickCashButton.Enabled = !on;
            _quickCashButton.BackColor = on ? Color.FromArgb(200, 205, 210) : Color.FromArgb(46, 125, 50);
            discountBox.Clear();
            discountBox.Enabled = percentCheck.Enabled = !on;   // a return has no invoice discount
            totalCaption.Text = on ? "Refund" : "TOTAL";
            totalValue.ForeColor = on ? ReturnRed : Dark;
            lastItemName.Text = on ? "RETURN" : "Welcome";
            lastItemPrice.Text = on ? "Scan what comes back" : string.Empty;
            ShowStatus(on ? "Scan the items the customer brings back, then REFUND (F12). F4 = back to selling."
                          : "Ready", on ? Color.White : Color.Gainsboro);
            UpdateInfo();
            FocusScan();
        }

        // Saves the return invoice (the manager approved it when Return was pressed); asks how the money goes back
        private async Task RefundAsync()
        {
            if (_returnApprover is not { } approver) { SetReturnMode(false); return; }   // never without a manager
            if (_lines.Count == 0) { ShowStatus("Scan the items that come back first.", Color.Khaki); return; }
            if (!ValidateSale(out _)) return;
            var total = SubTotal;

            if (CashierDialogs.ChooseRefund(this, total, _currencies) is not { } how) { FocusScan(); return; }

            try
            {
                UseWaitCursor = true;
                var result = await _saleRepository.SaveReturnAsync(_lines, how.Method, how.Currency, AppConfig.CurrentUserId);
                PrintReceipt(result.ReturnId);
                try
                {
                    await _saleRepository.AddAuditAsync("refund_approved", "sales_invoices", result.ReturnId,
                        new { ApprovedBy = approver.UserId, ApprovedByName = approver.FullName, result.InvoiceNo, result.RefundLbp });
                }
                catch { /* the return is saved; the approval note is extra */ }

                ClearSale();
                SetReturnMode(false);
                var give = _currencies.Find(how.Currency) is { IsPrimary: false } paidIn
                    ? paidIn.Format(result.RefundAmount)
                    : _currencies.FormatPrimary(result.RefundLbp);
                lastItemName.Text = $"Return {result.InvoiceNo}";
                lastItemPrice.Text = $"Give back {give}";
                ShowStatus("Items are back in stock  ·  next customer", Color.FromArgb(120, 220, 190));
            }
            catch (Exception ex)
            {
                // Nothing was saved (fn_save_return is all or nothing); the return stays on the screen
                MessageBox.Show(this, "The return was not saved:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message),
                    "Refund", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                FocusScan();
            }
        }

        private void VoidSale()
        {
            if (_lines.Count == 0)
            {
                ClearSale();
                if (_returnMode) SetReturnMode(false);
                FocusScan();
                return;
            }
            var message = _returnMode ? "Cancel this return? All lines are removed."
                : _heldSaleId.HasValue
                    ? $"Clear this sale from the screen?\nHeld sale #{_heldSaleId} stays held (delete it from Recall if needed)."
                    : "Void this sale? All lines are removed.";
            if (MessageBox.Show(this, message, _returnMode ? "Cancel return" : "Void sale", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            var wasReturn = _returnMode;
            ClearSale();
            if (wasReturn) SetReturnMode(false);
            ShowStatus(wasReturn ? "Return cancelled." : "Sale voided.", Color.Gainsboro);
            FocusScan();
        }

        // Lines + invoice discount; marks the wrong lines in the grid
        private bool ValidateSale(out decimal discount)
        {
            if (!TryGetInvoiceDiscount(out discount))
            {
                ShowStatus("The invoice discount is not a valid number.", Color.Khaki);
                discountBox.Focus();
                return false;
            }

            var validation = SaleValidator.ValidateSale(_lines, discount);
            foreach (DataGridViewRow row in linesGrid.Rows) row.Cells.Cast<DataGridViewCell>().ToList().ForEach(c => c.ErrorText = string.Empty);
            foreach (var error in validation.Errors.Where(e => e.LineIndex is int i && i < linesGrid.Rows.Count))
            {
                var column = error.Field != null ? linesGrid.Columns[error.Field] : null;
                linesGrid[(column ?? linesGrid.Columns[nameof(PosLine.ItemName)]!).Index, error.LineIndex!.Value].ErrorText = error.Message;
            }
            if (validation.IsValid) return true;

            MessageBox.Show(this, validation.ToMessage(), "Check the sale", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private void ClearSale()
        {
            _lines.Clear();
            _heldSaleId = null;
            discountBox.Clear();
            percentCheck.Checked = false;
            scanBox.Clear();
            lastItemName.Text = "Welcome";
            lastItemPrice.Text = string.Empty;
            UpdateInfo();
            UpdateTotals();
        }

        private void CashierForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_lines.Count == 0) return;
            if (MessageBox.Show(this, "A sale is still open. Close the cashier anyway? (The open sale is lost - hold it first to keep it.)",
                    "Cashier", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                e.Cancel = true;
        }

        #endregion

        #region Helpers

        private void ShowLastItem(PosLine line)
        {
            lastItemName.Text = line.ItemName;
            lastItemPrice.Text = line.Discount > 0
                ? $"{line.Quantity:#,0.###} x {line.UnitPrice:#,0} - {line.Discount:#,0} = {line.LineTotal:#,0}"
                : $"{line.Quantity:#,0.###} x {line.UnitPrice:#,0} = {line.LineTotal:#,0}";
        }

        private void ShowStatus(string text, Color color)
        {
            statusLabel.Text = text;
            statusLabel.ForeColor = color;
        }

        private static bool TryParseNumber(string text, out decimal value) =>
            decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out value)
            || decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out value);

        #endregion

        private void quickPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void functionTable_Paint(object sender, PaintEventArgs e)
        {

        }

        private void actionsTable_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
