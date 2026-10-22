using System.ComponentModel;
using System.Text.Json;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // The customer's screen: the second monitor of the POS (built in, or a screen turned to the customer).
    // Full screen on that monitor, opened and fed by the cashier screen (CashierForm):
    //   no sale  -> "Welcome", the logo and the shop name
    //   a sale   -> the lines, the last item scanned, the total (and in the second currency)
    //   paid     -> "Thank you", the total and the change; "Welcome" again after a while or at the next item
    // It never takes the focus: the cashier keeps typing / scanning on the cashier screen.
    // Settings: "CustomerDisplay" in appsettings.json (each POS station has its own).
    public sealed class CustomerDisplaySettings
    {
        public bool Enabled { get; set; } = true;

        /// <summary>"Screen" = a second monitor (this window); "Pole" = a pole display on a COM port (PoleDisplay).</summary>
        public string Type { get; set; } = "Screen";

        // Pole display only: its COM port (Device Manager > Ports), speed, command set ("EscPos" or "CD5220"), characters per line
        public string? Port { get; set; }
        public int BaudRate { get; set; } = 9600;
        public string Commands { get; set; } = "EscPos";
        public int LineWidth { get; set; } = 20;

        /// <summary>0 = the first screen that is not the main one; 1, 2... = that screen (Windows display order).</summary>
        public int Screen { get; set; }

        public string WelcomeText { get; set; } = "Welcome";
        public string ThankYouText { get; set; } = "Thank you!";

        /// <summary>How long "Thank you" stays before "Welcome" comes back.</summary>
        public int ThankYouSeconds { get; set; } = 20;

        /// <summary>Reads the "CustomerDisplay" section of appsettings.json (defaults for anything missing or wrong).</summary>
        public static CustomerDisplaySettings Read(JsonElement root)
        {
            if (!root.TryGetProperty("CustomerDisplay", out var section)) return new CustomerDisplaySettings();
            try
            {
                return section.Deserialize<CustomerDisplaySettings>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? new CustomerDisplaySettings();
            }
            catch (JsonException)
            {
                return new CustomerDisplaySettings();
            }
        }
    }

    public class CustomerDisplayForm : Form, ICustomerDisplay
    {
        private static readonly Color Back = Color.FromArgb(22, 32, 43);
        private static readonly Color Panel = Color.FromArgb(32, 45, 58);
        private static readonly Color Soft = Color.FromArgb(160, 172, 186);
        private static readonly Color Accent = Color.FromArgb(46, 196, 160);

        private readonly CustomerDisplaySettings _settings;
        private readonly PictureBox _logoBox, _welcomeLogo;
        private readonly Label _storeLabel, _clockLabel;
        private readonly Label _itemName, _itemDetail, _totalCaption, _totalValue, _totalSecond, _discountLabel;
        private readonly Label _messageTitle, _messageText;
        private readonly DataGridView _grid;
        private readonly TableLayoutPanel _salePanel, _messagePanel;
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
        private DateTime? _thanksUntil;

        /// <summary>
        /// Opens the customer screen on the second monitor and shows the cashier's lines; null when it is turned off
        /// (CustomerDisplay.Enabled) or there is no second monitor.
        /// </summary>
        public static CustomerDisplayForm? Open(IBindingList lines)
        {
            var settings = AppConfig.CustomerDisplay;
            var screens = System.Windows.Forms.Screen.AllScreens;
            var screen = settings.Screen > 0
                ? screens.ElementAtOrDefault(settings.Screen - 1)
                : screens.FirstOrDefault(s => !s.Primary);
            if (screen == null) return null;

            var form = new CustomerDisplayForm(settings, screen, lines);
            form.Show();
            return form;
        }

        private CustomerDisplayForm(CustomerDisplaySettings settings, Screen screen, IBindingList lines)
        {
            _settings = settings;
            Text = "Customer display";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = screen.Bounds;
            ShowInTaskbar = false;
            BackColor = Back;
            ForeColor = Color.White;

            // Sizes follow the screen: u = 1% of its height
            var u = screen.Bounds.Height / 100f;
            Font Px(float size, FontStyle style = FontStyle.Regular) => new("Segoe UI", Math.Max(9f, size * u), style, GraphicsUnit.Pixel);
            Label NewLabel(Font font, Color color, ContentAlignment align) =>
                new() { Dock = DockStyle.Fill, Font = font, ForeColor = color, TextAlign = align, AutoEllipsis = true, UseMnemonic = false };

            // Top: logo, shop name, clock
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = Panel, Margin = new Padding(0), Padding = new Padding((int)(2 * u), (int)u, (int)(2 * u), (int)u) };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12 * u));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30 * u));
            _logoBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(0) };
            _storeLabel = NewLabel(Px(5.5f, FontStyle.Bold), Color.White, ContentAlignment.MiddleLeft);
            _clockLabel = NewLabel(Px(4f), Soft, ContentAlignment.MiddleRight);
            header.Controls.Add(_logoBox, 0, 0);
            header.Controls.Add(_storeLabel, 1, 0);
            header.Controls.Add(_clockLabel, 2, 0);

            // A sale: the lines on the left, the last item and the total on the right
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                BackgroundColor = Back,
                GridColor = Panel,
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = (int)(5 * u),
                ScrollBars = ScrollBars.None,
                TabStop = false,
            };
            _grid.RowTemplate.Height = (int)(6 * u);
            _grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Back, ForeColor = Color.White, SelectionBackColor = Back, SelectionForeColor = Color.White,
                Font = Px(3.2f), Padding = new Padding((int)u, 0, (int)u, 0),
            };
            _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Back, ForeColor = Soft, SelectionBackColor = Back, Font = Px(2.6f), Padding = new Padding((int)u, 0, (int)u, 0),
            };
            _grid.Columns.AddRange(
                GridColumn(nameof(PosLine.ItemName), "Item", 52, "", DataGridViewContentAlignment.MiddleLeft),
                GridColumn(nameof(PosLine.Quantity), "Qty", 14, "#,0.###", DataGridViewContentAlignment.MiddleRight),
                GridColumn(nameof(PosLine.UnitPrice), "Price", 17, "#,0", DataGridViewContentAlignment.MiddleRight),
                GridColumn(nameof(PosLine.LineTotal), "Total", 17, "#,0", DataGridViewContentAlignment.MiddleRight));
            _grid.DataSource = lines;
            _grid.DataBindingComplete += (_, _) => _grid.ClearSelection();
            lines.ListChanged += (_, _) => ScrollToEnd();

            var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 7, BackColor = Panel, Margin = new Padding((int)(2 * u), 0, 0, 0), Padding = new Padding((int)(2 * u)) };
            foreach (var percent in new[] { 14F, 10F, 26F, 8F, 22F, 10F, 10F })
                right.RowStyles.Add(new RowStyle(SizeType.Percent, percent));
            _itemName = NewLabel(Px(4.5f, FontStyle.Bold), Color.White, ContentAlignment.BottomLeft);
            _itemDetail = NewLabel(Px(3.5f), Soft, ContentAlignment.TopLeft);
            _totalCaption = NewLabel(Px(3.5f, FontStyle.Bold), Soft, ContentAlignment.BottomLeft);
            _totalValue = NewLabel(Px(10f, FontStyle.Bold), Accent, ContentAlignment.MiddleRight);
            _totalSecond = NewLabel(Px(4.5f, FontStyle.Bold), Color.White, ContentAlignment.TopRight);
            _discountLabel = NewLabel(Px(3f), Accent, ContentAlignment.TopRight);
            right.Controls.Add(_itemName, 0, 0);
            right.Controls.Add(_itemDetail, 0, 1);
            right.Controls.Add(_totalCaption, 0, 3);
            right.Controls.Add(_totalValue, 0, 4);
            right.Controls.Add(_totalSecond, 0, 5);
            right.Controls.Add(_discountLabel, 0, 6);

            _salePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = new Padding(0), Padding = new Padding((int)(2 * u)) };
            _salePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            _salePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            _salePanel.Controls.Add(_grid, 0, 0);
            _salePanel.Controls.Add(right, 1, 0);

            // No sale / paid: one big message
            _messagePanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Margin = new Padding(0), Padding = new Padding((int)(4 * u)) };
            _messagePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            _messagePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            _messagePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            _welcomeLogo = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            _messageTitle = NewLabel(Px(11f, FontStyle.Bold), Color.White, ContentAlignment.MiddleCenter);
            _messageText = NewLabel(Px(5f), Soft, ContentAlignment.TopCenter);
            _messagePanel.Controls.Add(_welcomeLogo, 0, 0);
            _messagePanel.Controls.Add(_messageTitle, 0, 1);
            _messagePanel.Controls.Add(_messageText, 0, 2);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = new Padding(0), Padding = new Padding(0) };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 88F));
            var body = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            body.Controls.Add(_salePanel);
            body.Controls.Add(_messagePanel);
            root.Controls.Add(header, 0, 0);
            root.Controls.Add(body, 0, 1);
            Controls.Add(root);

            _timer.Tick += (_, _) => Tick();
            _timer.Start();
            Tick();
            ShowWelcome();
            Load += async (_, _) => await LoadCompanyAsync();
            FormClosed += (_, _) => _timer.Dispose();
        }

        // Shown without taking the focus from the cashier screen, and never activated by a click
        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_NOACTIVATE = 0x08000000;
                const int WS_EX_TOOLWINDOW = 0x00000080;   // not in Alt+Tab
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        private static DataGridViewTextBoxColumn GridColumn(string property, string header, float weight, string format, DataGridViewContentAlignment align)
        {
            var column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = property, HeaderText = header, FillWeight = weight, SortMode = DataGridViewColumnSortMode.NotSortable,
            };
            column.DefaultCellStyle.Format = format;
            column.DefaultCellStyle.Alignment = align;
            column.HeaderCell.Style.Alignment = align;
            return column;
        }

        private async Task LoadCompanyAsync()
        {
            try
            {
                var repository = new CompanyRepository();
                _storeLabel.Text = await repository.GetNameAsync();
                var logo = Printing.CompanyLogo.ToImage(await repository.GetLogoAsync());
                _logoBox.Image = logo;
                _welcomeLogo.Image = logo;
                if (_messagePanel.Visible && _thanksUntil == null) ShowWelcome();
            }
            catch { /* no name / logo: the screen still shows the sale */ }
        }

        private void Tick()
        {
            _clockLabel.Text = DateTime.Now.ToString("dd/MM/yyyy  HH:mm");
            if (_thanksUntil is { } until && DateTime.Now >= until) ShowWelcome();
        }

        private void ScrollToEnd()
        {
            if (_grid.RowCount == 0) return;
            var visible = Math.Max(1, _grid.DisplayedRowCount(false));
            _grid.FirstDisplayedScrollingRowIndex = Math.Max(0, _grid.RowCount - visible);
            _grid.ClearSelection();
        }

        private void ShowMessage(string title, string text)
        {
            _messageTitle.Text = title;
            _messageText.Text = text;
            _messagePanel.Visible = true;
            _salePanel.Visible = false;
        }

        public void ShowWelcome()
        {
            _thanksUntil = null;
            _itemName.Text = _itemDetail.Text = string.Empty;
            ShowMessage(_settings.WelcomeText, _storeLabel.Text);
        }

        /// <summary>The item just scanned or changed.</summary>
        public void ShowItem(PosLine line)
        {
            _itemName.Text = line.ItemName;
            _itemDetail.Text = line.TotalDiscount > 0
                ? $"{line.Quantity:#,0.###} x {line.UnitPrice:#,0}  - {line.TotalDiscount:#,0}  =  {line.LineTotal:#,0}"
                : $"{line.Quantity:#,0.###} x {line.UnitPrice:#,0}  =  {line.LineTotal:#,0}";
        }

        /// <summary>
        /// The sale's total after the discounts. No line: "Welcome" (but a "Thank you" stays until its time is up).
        /// </summary>
        public void ShowTotals(int lineCount, decimal discount, decimal total, CurrencySet currencies, bool isReturn)
        {
            if (lineCount == 0)
            {
                if (_thanksUntil == null) ShowWelcome();
                return;
            }
            _thanksUntil = null;
            _salePanel.Visible = true;
            _messagePanel.Visible = false;
            if (_itemName.Text.Length == 0 && _grid.RowCount > 0 && _grid.Rows[^1].DataBoundItem is PosLine last) ShowItem(last);

            _totalCaption.Text = isReturn ? "REFUND" : $"TOTAL  ({lineCount} item{(lineCount == 1 ? "" : "s")})";
            _totalValue.Text = currencies.FormatPrimary(total);
            _totalValue.ForeColor = isReturn ? Color.FromArgb(240, 120, 100) : Accent;
            _totalSecond.Text = currencies.FormatSecond(total);
            _discountLabel.Text = discount > 0 ? $"You save {currencies.FormatPrimary(discount)}" : string.Empty;
            ScrollToEnd();
        }

        /// <summary>The sale is saved: thank you, the total and the change (null = nothing paid now, an unpaid sale).</summary>
        public void ShowThanks(decimal total, decimal? change, CurrencySet currencies)
        {
            var text = $"Total  {currencies.FormatPrimary(total)}";
            if (change is > 0)
            {
                text += $"\nChange  {currencies.FormatPrimary(change.Value)}";
                var second = currencies.FormatSecond(change.Value);
                if (second.Length > 0) text += $"   ({second})";
            }
            ShowMessage(_settings.ThankYouText, text);
            _itemName.Text = _itemDetail.Text = string.Empty;
            _thanksUntil = DateTime.Now.AddSeconds(Math.Max(3, _settings.ThankYouSeconds));
        }
    }
}
