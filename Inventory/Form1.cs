using System.Drawing.Drawing2D;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Back office (a Manager's main window).
    // Left: the menu (Dashboard, Item, Contacts, Purchase, Invoice, Reports, Settings; a group opens its pages under it),
    //       the Cashier button (F12) and who is logged in.
    // Right: the dashboard - today's sales, today's orders, low stock, customers, the sales of the last 7 days
    //        and the latest invoices. It reloads when the window comes back to the front, or with F5.
    public partial class Form1 : Form
    {
        private static readonly Color Sidebar = Color.FromArgb(22, 32, 43);
        private static readonly Color SidebarHover = Color.FromArgb(36, 50, 68);
        private static readonly Color SidebarSelected = Color.FromArgb(52, 74, 110);
        private static readonly Color SidebarText = Color.FromArgb(222, 228, 236);
        private static readonly Color Ink = Color.FromArgb(22, 32, 43);
        private static readonly Color Green = Color.FromArgb(34, 139, 87);
        private static readonly Color Red = Color.FromArgb(192, 57, 43);
        private static readonly Color Orange = Color.FromArgb(217, 119, 6);
        private static readonly Color Blue = Color.FromArgb(23, 133, 196);
        private static readonly Color Purple = Color.FromArgb(124, 92, 196);
        private const int WalkInCustomerId = 1;
        private const int RecentCount = 8;
        private const int NavWidth = 226;

        // Segoe MDL2 Assets icons (the font comes with Windows 10 / 11)
        private const string HomeIcon = "", ItemIcon = "", PeopleIcon = "", CartIcon = "", PageIcon = "",
            ChartIcon = "", SettingsIcon = "", CashierIcon = "", ChevronDown = "", ChevronRight = "";

        private Label _companyLabel = null!, _updatedLabel = null!, _databaseLabel = null!, _printerLabel = null!, _readyLabel = null!;
        private StatCard _salesCard = null!, _ordersCard = null!, _stockCard = null!, _customersCard = null!;
        private BarChart _chart = null!;
        private DataGridView _recentGrid = null!;
        private List<SaleHistoryModel> _recent = new();
        private CurrencySet _currencies = CurrencySet.Default;
        private DateTime _loadedAt = DateTime.MinValue;
        private bool _loadingDashboard;
        private readonly List<(Button Group, List<Button> Pages)> _groups = new();
        private Button _dashboardButton = null!, _itemsButton = null!, _clientsButton = null!, _invoiceButton = null!;
        private List<Button> _reportButtons = new();
        private Button? _selectedNav;

        // The page showing in place of the dashboard (a list form shown inside this window), or null
        private Form? _page;
        private Control _dashboardView = null!;
        private Panel _pageHost = null!;
        private Label _pageTitle = null!;
        private bool _closed;

        public Form1()
        {
            Text = "My Store";
            WindowState = FormWindowState.Maximized;
            KeyPreview = true;

            InitializeComponent();
            BuildSidebar();
            BuildDashboard();
            BuildStatusBar();

            Text = $"My Store  -  {AppSession.UserName}";
            FormClosing += Form1_FormClosing;
            FormClosed += (_, _) => _closed = true;
            Activated += async (_, _) =>
            {
                // Back from the cashier or another window: the numbers may have changed
                if (_page == null && (DateTime.Now - _loadedAt).TotalSeconds > 20) await LoadDashboardAsync();
            };
        }

        // The cashier closes first, so it can ask about a sale that is still open
        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            foreach (var cashier in Application.OpenForms.OfType<CashierForm>().ToList())
            {
                cashier.Close();
                if (Application.OpenForms.Cast<Form>().Contains(cashier))
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private async void Form1_Load(object? sender, EventArgs e)
        {
            await ShowCompanyNameAsync();
            await LoadDashboardAsync();
        }

        // Title and top of the menu: company name
        private async Task ShowCompanyNameAsync()
        {
            try
            {
                var name = await new CompanyRepository().GetNameAsync();
                Text = $"{name}  -  {AppSession.UserName}";
                _companyLabel.Text = name;
            }
            catch { /* the status bar says the database is not connected */ }
        }

        #region Menu on the left

        private void BuildSidebar()
        {
            // Company name on top
            var logo = new Panel { Dock = DockStyle.Top, Height = 92 };
            logo.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var circle = new SolidBrush(Blue);
                e.Graphics.FillEllipse(circle, 20, 24, 44, 44);
                using var iconFont = new Font(StatCard.IconFont, 15F);
                TextRenderer.DrawText(e.Graphics, PageIcon, iconFont, new Rectangle(20, 24, 44, 44), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                using var line = new Pen(Color.FromArgb(44, 58, 76));
                e.Graphics.DrawLine(line, 20, logo.Height - 1, logo.Width - 20, logo.Height - 1);
            };
            _companyLabel = new Label
            {
                Text = "My Store",
                Location = new Point(74, 24),
                Size = new Size(168, 26),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                AutoEllipsis = true,
            };
            logo.Controls.Add(_companyLabel);
            logo.Controls.Add(new Label { Text = "Back office", Location = new Point(76, 50), AutoSize = true, ForeColor = Color.FromArgb(130, 150, 180), Font = new Font("Segoe UI", 9F) });

            var nav = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(12, 10, 0, 8),
            };

            // The lists open as a page inside this window (ShowPage); the settings are small windows of their own
            _dashboardButton = AddPage(nav, "Dashboard", HomeIcon, _ => ShowDashboard());
            Select(_dashboardButton);

            _itemsButton = AddGroup(nav, "Item", ItemIcon,
                ("Item list", b => ShowPage(b, () => new Item())),
                ("POS buttons (cashier)", _ => ShowDialog<QuickButtonsForm>()))[0];
            _clientsButton = AddGroup(nav, "Contacts", PeopleIcon,
                ("Clients", b => ShowPage(b, () => new CustomersForm())),
                ("Suppliers", b => ShowPage(b, () => new SuppliersForm())))[0];
            AddPage(nav, "Purchase", CartIcon, b => ShowPage(b, () => new PurchaseForm()));
            _invoiceButton = AddPage(nav, "Invoice", PageIcon, _ => ShowSales(DateTime.Today, DateTime.Today));
            _reportButtons = AddGroup(nav, "Reports", ChartIcon,
                ("Daily sales summary", b => OpenReport(new Reports.DailySalesReport(), b)),
                ("Sales by invoice (profit)", b => OpenReport(new Reports.SalesByInvoiceReport(), b)),
                ("Sales by item (profit)", b => OpenReport(new Reports.SalesByItemReport(), b)),
                ("Sales and unpaid", b => OpenReport(new Reports.SalesUnpaidReport(), b)),
                ("Unpaid by client", b => OpenReport(new Reports.UnpaidByCustomerReport(), b)),
                ("Client statement", b => OpenReport(new Reports.CustomerStatementReport(), b)));
            AddGroup(nav, "Settings", SettingsIcon,
                ("Company", async _ =>
                {
                    using var form = new CompanyForm();
                    if (form.ShowDialog(this) == DialogResult.OK) await ShowCompanyNameAsync();
                }),
                ("Currencies & exchange rates", _ => ShowDialog<CurrenciesForm>()),
                ("Users", _ => ShowDialog<UsersForm>()),
                ("Receipt printer", _ => ShowDialog<ReceiptPrinterForm>()));

            // Cashier and who is logged in, at the bottom
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 148, Padding = new Padding(12, 8, 12, 10) };
            var cashier = new Button
            {
                Text = "Cashier   (F12)",
                Dock = DockStyle.Top,
                Height = 56,
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(34, 160, 90),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(52, 0, 0, 0),
                TabStop = false,
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand,
            };
            cashier.FlatAppearance.BorderSize = 0;
            cashier.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 140, 78);
            cashier.Paint += (_, e) => DrawIcon(e.Graphics, CashierIcon, new Rectangle(16, 0, 30, cashier.Height), Color.White, 15F);
            cashier.Click += (_, _) => OpenCashier();

            var logOut = new Button
            {
                Text = "Log out",
                Dock = DockStyle.Bottom,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Sidebar,
                ForeColor = SidebarText,
                Font = new Font("Segoe UI", 10F),
                TabStop = false,
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand,
            };
            logOut.FlatAppearance.BorderColor = Color.FromArgb(70, 84, 104);
            logOut.FlatAppearance.MouseOverBackColor = SidebarHover;
            logOut.Click += (_, _) => AppSession.LogOut();
            var user = new Label
            {
                Text = $"Logged in as: {AppSession.UserName}",
                Dock = DockStyle.Bottom,
                Height = 30,
                ForeColor = Color.FromArgb(130, 150, 180),
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
            };
            bottom.Controls.Add(cashier);
            bottom.Controls.Add(user);
            bottom.Controls.Add(logOut);

            // Docking goes from the last control added to the first
            sidebarPanel.Controls.Add(nav);
            sidebarPanel.Controls.Add(bottom);
            sidebarPanel.Controls.Add(logo);
        }

        // A menu line that opens a page
        private static Button AddPage(FlowLayoutPanel nav, string text, string icon, Action<Button> open)
        {
            var button = NewNavButton(text, child: false);
            button.Paint += (_, e) => DrawIcon(e.Graphics, icon, new Rectangle(12, 0, 30, button.Height), SidebarText, 14F);
            button.Click += (_, _) => open(button);
            nav.Controls.Add(button);
            return button;
        }

        // A menu line with pages under it: a click opens or closes them (one group open at a time). Returns the page lines.
        private List<Button> AddGroup(FlowLayoutPanel nav, string text, string icon, params (string Text, Action<Button> Open)[] pages)
        {
            var group = NewNavButton(text, child: false);
            var children = new List<Button>();
            group.Paint += (_, e) =>
            {
                DrawIcon(e.Graphics, icon, new Rectangle(12, 0, 30, group.Height), SidebarText, 14F);
                var open = children.Count > 0 && children[0].Visible;
                DrawIcon(e.Graphics, open ? ChevronDown : ChevronRight, new Rectangle(group.Width - 34, 0, 24, group.Height), Color.FromArgb(130, 150, 180), 8F);
            };
            nav.Controls.Add(group);
            foreach (var page in pages)
            {
                var child = NewNavButton(page.Text, child: true);
                child.Visible = false;
                child.Click += (_, _) => page.Open(child);
                nav.Controls.Add(child);
                children.Add(child);
            }
            _groups.Add((group, children));
            group.Click += (_, _) => OpenGroup(children[0].Visible ? null : group);
            return children;
        }

        // Shows the pages of one group and hides those of the others (null = all closed)
        private void OpenGroup(Button? group)
        {
            var nav = _dashboardButton.Parent!;
            nav.SuspendLayout();
            foreach (var (other, pages) in _groups)
            {
                foreach (var p in pages) p.Visible = ReferenceEquals(other, group);
                other.Invalidate();
            }
            nav.ResumeLayout();
        }

        // Marks the menu line of the page that is showing (and opens its group)
        private void Select(Button button)
        {
            if (_selectedNav != null)
            {
                _selectedNav.BackColor = Sidebar;
                _selectedNav.FlatAppearance.MouseOverBackColor = SidebarHover;
            }
            _selectedNav = button;
            button.BackColor = SidebarSelected;
            button.FlatAppearance.MouseOverBackColor = SidebarSelected;
            if (_groups.FirstOrDefault(g => g.Pages.Contains(button)).Group is { } group && !button.Visible) OpenGroup(group);
        }

        private static Button NewNavButton(string text, bool child)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(NavWidth, child ? 36 : 46),
                Margin = new Padding(0, 1, 0, 1),
                FlatStyle = FlatStyle.Flat,
                BackColor = Sidebar,
                ForeColor = child ? Color.FromArgb(190, 200, 214) : SidebarText,
                Font = new Font("Segoe UI", child ? 10F : 11.5F),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(child ? 50 : 46, 0, 0, 0),
                TabStop = false,
                UseVisualStyleBackColor = false,
                UseMnemonic = false,
                Cursor = Cursors.Hand,
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = SidebarHover;
            button.FlatAppearance.MouseDownBackColor = SidebarSelected;
            return button;
        }

        private static void DrawIcon(Graphics g, string icon, Rectangle area, Color color, float size)
        {
            using var font = new Font(StatCard.IconFont, size);
            TextRenderer.DrawText(g, icon, font, area, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        #endregion

        #region Dashboard

        private void BuildDashboard()
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = contentPanel.BackColor };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Title, Refresh, who is logged in
            var header = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            var title = new Label { Text = "Dashboard", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold), ForeColor = Ink, TextAlign = ContentAlignment.MiddleLeft };
            var userPanel = new Panel { Dock = DockStyle.Right, Width = 230 };
            userPanel.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var circle = new SolidBrush(Blue);
                e.Graphics.FillEllipse(circle, 8, 17, 40, 40);
                using var initialsFont = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
                using var nameFont = new Font("Segoe UI", 11.5F);
                TextRenderer.DrawText(e.Graphics, Initials(AppSession.UserName), initialsFont, new Rectangle(8, 17, 40, 40), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                TextRenderer.DrawText(e.Graphics, AppSession.UserName, nameFont, new Rectangle(58, 17, userPanel.Width - 60, 40), Ink,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            };
            var refresh = new Button
            {
                Text = "Refresh  (F5)",
                Size = new Size(126, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Ink,
                TabStop = false,
                UseVisualStyleBackColor = false,
            };
            refresh.FlatAppearance.BorderColor = Color.FromArgb(214, 219, 226);
            refresh.Click += async (_, _) => await LoadDashboardAsync();
            var refreshHolder = new Panel { Dock = DockStyle.Right, Width = 142 };
            refresh.Location = new Point(0, 18);
            refreshHolder.Controls.Add(refresh);
            header.Controls.Add(title);
            header.Controls.Add(refreshHolder);
            header.Controls.Add(userPanel);

            // The four totals
            var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = Padding.Empty };
            for (var i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _salesCard = AddCard(cards, 0, "Today's sales", CashierIcon, Blue, () => OpenReport(new Reports.DailySalesReport(), _reportButtons[0]));
            _ordersCard = AddCard(cards, 1, "Today's orders", CartIcon, Orange, () => ShowSales(DateTime.Today, DateTime.Today));
            _stockCard = AddCard(cards, 2, "Low stock items", ItemIcon, Color.FromArgb(202, 138, 4), () => ShowPage(_itemsButton, () => new Item()));
            _customersCard = AddCard(cards, 3, "Customers", PeopleIcon, Purple, () => ShowPage(_clientsButton, () => new CustomersForm()));

            // Sales of the last 7 days | latest invoices
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var chartCard = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 16, 0) };
            _chart = new BarChart { Dock = DockStyle.Fill };
            chartCard.Controls.Add(_chart);
            chartCard.Controls.Add(CardTitle("Sales, last 7 days"));

            var recentCard = new CardPanel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            var recentHeader = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.White };
            var viewAll = new LinkLabel
            {
                Text = "View all  →",
                Dock = DockStyle.Right,
                Width = 100,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 10.5F),
                LinkColor = Blue,
                ActiveLinkColor = Blue,
                LinkBehavior = LinkBehavior.HoverUnderline,
                BackColor = Color.White,
            };
            viewAll.LinkClicked += (_, _) => ShowSales(DateTime.Today.AddDays(-6), DateTime.Today);
            var recentTitle = CardTitle("Recent transactions");
            recentTitle.Dock = DockStyle.Fill;
            recentHeader.Controls.Add(recentTitle);
            recentHeader.Controls.Add(viewAll);
            _recentGrid = BuildRecentGrid();
            recentCard.Controls.Add(_recentGrid);
            recentCard.Controls.Add(recentHeader);

            body.Controls.Add(chartCard, 0, 0);
            body.Controls.Add(recentCard, 1, 0);
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(cards, 0, 1);
            layout.Controls.Add(body, 0, 2);
            contentPanel.Controls.Add(layout);
            _dashboardView = layout;

            // Where a list page shows instead of the dashboard: its title on top, the page under it
            _pageHost = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = contentPanel.BackColor };
            _pageTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(24, 0, 0, 0),
                Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold),
                ForeColor = Ink,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
            };
            _pageHost.Controls.Add(_pageTitle);
            contentPanel.Controls.Add(_pageHost);
        }

        #endregion

        #region Pages inside the window

        /// <summary>
        /// Shows a list (items, purchases, clients, a report...) inside this window, in place of the dashboard, like a
        /// page of a web site. Its Add / Edit windows still open on top. button = its line in the menu on the left.
        /// </summary>
        private void ShowPage(Button button, Func<Form> create)
        {
            if (!ClosePage()) return;

            var page = create();
            page.TopLevel = false;
            page.FormBorderStyle = FormBorderStyle.None;
            page.Dock = DockStyle.Fill;
            _page = page;

            // Close / Esc on the page goes back to the dashboard
            page.FormClosed += (_, _) =>
            {
                if (!ReferenceEquals(_page, page)) return;
                _page = null;
                if (!_closed) ShowDashboard();
            };
            foreach (var close in Descendants(page).OfType<Button>().Where(b => b.DialogResult == DialogResult.Cancel))
                close.Click += (_, _) => page.Close();
            page.TextChanged += (_, _) => _pageTitle.Text = page.Text;

            _pageTitle.Text = page.Text;
            _dashboardView.Visible = false;
            contentPanel.Padding = Padding.Empty;
            _pageHost.Controls.Add(page);
            _pageHost.Visible = true;
            page.Show();
            page.BringToFront();   // under the title, which is docked first
            Select(button);
            page.Select();
        }

        private void ShowDashboard()
        {
            if (!ClosePage()) return;
            _pageHost.Visible = false;
            contentPanel.Padding = new Padding(28, 14, 28, 14);
            _dashboardView.Visible = true;
            Select(_dashboardButton);
            _ = LoadDashboardAsync();
        }

        // Closes the page that is showing; false when it refuses to close
        private bool ClosePage()
        {
            if (_page is not { } page) return true;
            _page = null;   // not "back to the dashboard" from its FormClosed
            page.Close();
            if (!page.IsDisposed && page.Visible)
            {
                _page = page;
                return false;
            }
            return true;
        }

        private static IEnumerable<Control> Descendants(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                yield return child;
                foreach (var inner in Descendants(child)) yield return inner;
            }
        }

        private static Label CardTitle(string text) => new()
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 40,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
            ForeColor = Ink,
            BackColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        private static StatCard AddCard(TableLayoutPanel cards, int column, string caption, string icon, Color color, Action open)
        {
            var card = new StatCard(caption, icon, color) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, column == 3 ? 0 : 16, 16) };
            card.Click += (_, _) => open();
            cards.Controls.Add(card, column, 0);
            return card;
        }

        private DataGridView BuildRecentGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeight = 38,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(232, 235, 240),
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 10.5F),
                TabStop = false,
            };
            grid.RowTemplate.Height = 40;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 242, 245);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(70, 80, 92);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 242, 245);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            grid.DefaultCellStyle.ForeColor = Ink;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 254);
            grid.DefaultCellStyle.SelectionForeColor = Ink;

            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Invoice", DataPropertyName = nameof(SaleHistoryModel.InvoiceNo), FillWeight = 115 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Customer", DataPropertyName = nameof(SaleHistoryModel.Customer), FillWeight = 125 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Date", DataPropertyName = nameof(SaleHistoryModel.InvoiceDate), FillWeight = 85, DefaultCellStyle = { Format = "dd/MM HH:mm" } });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Amount",
                DataPropertyName = nameof(SaleHistoryModel.Total),
                FillWeight = 90,
                DefaultCellStyle = { Format = "#,0.##", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold) },
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } },
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 95, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } } });
            foreach (DataGridViewColumn column in grid.Columns) column.SortMode = DataGridViewColumnSortMode.NotSortable;

            grid.CellFormatting += (_, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _recent.Count) return;
                if (grid.Columns[e.ColumnIndex].DataPropertyName == nameof(SaleHistoryModel.Customer) && _recent[e.RowIndex].Customer == null)
                {
                    e.Value = "Walk-in customer";
                    e.FormattingApplied = true;
                }
            };
            grid.CellPainting += recentGrid_CellPainting;
            grid.DataBindingComplete += (_, _) => grid.ClearSelection();
            grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < _recent.Count) ShowSales(_recent[e.RowIndex].InvoiceDate, _recent[e.RowIndex].InvoiceDate);
            };
            return grid;
        }

        // Paid / Unpaid / Partly paid / Return of an invoice of the list, with its color
        private static (string Text, Color Color) StatusOf(SaleHistoryModel sale) =>
            sale.IsReturn ? ("Return", Red)
            : sale.OnAccountLbp <= 0 || sale.DueLbp <= 0 ? ("Paid", Green)
            : sale.DueLbp >= sale.OnAccountLbp ? ("Unpaid", Orange)
            : ("Partly paid", Orange);

        // The status as a rounded colored label
        private void recentGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _recent.Count || e.Graphics == null || _recentGrid.Columns[e.ColumnIndex].Name != "Status") return;
            e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);

            var (text, color) = StatusOf(_recent[e.RowIndex]);
            using var font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            var width = Math.Min(e.CellBounds.Width - 8, TextRenderer.MeasureText(text, font).Width + 22);
            var pill = new RectangleF(e.CellBounds.X + (e.CellBounds.Width - width) / 2f, e.CellBounds.Y + (e.CellBounds.Height - 24) / 2f, width, 24);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = CardPanel.RoundedRectangle(pill, 12))
            using (var brush = new SolidBrush(color))
                e.Graphics.FillPath(brush, path);
            TextRenderer.DrawText(e.Graphics, text, font, Rectangle.Round(pill), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            e.Handled = true;
        }

        private void BuildStatusBar()
        {
            statusPanel.Paint += (_, e) =>
            {
                using var line = new Pen(Color.FromArgb(226, 230, 236));
                e.Graphics.DrawLine(line, 0, 0, statusPanel.Width, 0);
            };
            Label Part(string text, Color color) => new()
            {
                Text = text,
                AutoSize = true,
                Dock = DockStyle.Left,
                Padding = new Padding(0, 7, 28, 0),
                ForeColor = color,
                Font = new Font("Segoe UI", 9.5F),
            };
            _readyLabel = Part("●  Loading...", Color.DimGray);
            _databaseLabel = Part("Database: ...", Color.DimGray);
            _printerLabel = Part(string.Empty, Color.DimGray);
            _updatedLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 9.5F) };
            statusPanel.Controls.Add(_updatedLabel);
            statusPanel.Controls.Add(_printerLabel);
            statusPanel.Controls.Add(_databaseLabel);
            statusPanel.Controls.Add(_readyLabel);
        }

        /// <summary>Reads the numbers of the dashboard again (on start, with F5 and when the window comes back to the front).</summary>
        private async Task LoadDashboardAsync()
        {
            if (_loadingDashboard) return;
            try
            {
                _loadingDashboard = true;
                _loadedAt = DateTime.Now;
                var today = DateTime.Today;

                try { _currencies = await new CurrencyRepository().GetSetAsync(); } catch { /* LBP / USD defaults */ }

                // Today against yesterday, and the bars of the last 7 days
                var days = await new ReportRepository().GetDailySalesAsync(today.AddDays(-6), today, null);
                var now = days.LastOrDefault(d => d.Day.Date == today) ?? new DailySalesRow { Day = today };
                var before = days.LastOrDefault(d => d.Day.Date == today.AddDays(-1));
                if (before is { NetSales: > 0 })
                {
                    var change = (now.NetSales - before.NetSales) / before.NetSales * 100;
                    _salesCard.Show(_currencies.FormatPrimary(now.NetSales), $"{(change >= 0 ? "▲ +" : "▼ ")}{change:0.#}% vs yesterday", change >= 0 ? Green : Red);
                }
                else
                {
                    _salesCard.Show(_currencies.FormatPrimary(now.NetSales), "no sales yesterday", Color.DimGray);
                }
                _ordersCard.Show($"{now.SaleCount:#,0}",
                    now.ReturnCount > 0 ? $"{now.ReturnCount:#,0} return(s)"
                    : now.AverageSale is { } average ? $"average {_currencies.FormatPrimary(average)}" : "no sales yet today",
                    now.ReturnCount > 0 ? Red : Color.DimGray);
                _chart.Show(days.Select(d => new BarChart.Bar(d.Day.ToString("ddd"), d.NetSales,
                    $"{d.Day:ddd dd/MM}:  {_currencies.FormatPrimary(d.NetSales)}  ·  {d.SaleCount:#,0} sale(s)")));

                // Stock: under the reorder level, and nothing left
                var products = new ProductRepository();
                var low = (await products.GetPageAsync(1, 1, stock: ProductRepository.StockLow)).TotalCount;
                var outOf = (await products.GetPageAsync(1, 1, stock: ProductRepository.StockOut)).TotalCount;
                _stockCard.Show($"{low:#,0}", outOf > 0 ? $"⚠ {outOf:#,0} item(s) out of stock" : "nothing out of stock", outOf > 0 ? Orange : Green);

                // Clients, and what they still owe for unpaid sales
                var customers = (await new CustomerRepository().GetAllAsync()).Where(c => c.CustomerId != WalkInCustomerId).ToList();
                var owed = customers.Sum(c => Math.Max(c.BalanceLbp, 0));
                _customersCard.Show($"{customers.Count:#,0}", owed > 0 ? $"they owe {_currencies.FormatPrimary(owed)}" : "nothing unpaid", owed > 0 ? Orange : Green);

                // The latest invoices of the last 7 days
                _recent = (await new SaleRepository().GetSalesByUserAsync(null, today.AddDays(-6), today)).Take(RecentCount).ToList();
                _recentGrid.DataSource = _recent;

                _readyLabel.Text = "●  Ready";
                _readyLabel.ForeColor = Green;
                _databaseLabel.Text = "Database: connected";
                _databaseLabel.ForeColor = Color.DimGray;
                _updatedLabel.Text = $"Updated {DateTime.Now:HH:mm}  ·  {DateTime.Now:ddd dd MMM yyyy}";
            }
            catch (Exception ex)
            {
                _readyLabel.Text = "●  Not ready";
                _readyLabel.ForeColor = Red;
                _databaseLabel.Text = "Database: not connected  (" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message) + ")";
                _databaseLabel.ForeColor = Red;
            }
            finally
            {
                _loadingDashboard = false;
            }

            try
            {
                var printer = Printing.ReceiptSettings.Current ?? await Printing.ReceiptSettings.LoadAsync();
                _printerLabel.Text = printer is { HasPrinter: true } ? $"Printer: {printer.PrinterName}" : "Printer: not set";
            }
            catch { _printerLabel.Text = string.Empty; }
        }

        private static string Initials(string name)
        {
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Length == 0 ? "?"
                : words.Length == 1 ? words[0][..Math.Min(2, words[0].Length)].ToUpper()
                : $"{words[0][0]}{words[^1][0]}".ToUpper();
        }

        #endregion

        #region Opening the pages

        // A settings window: it is closed before going on
        private void ShowDialog<T>() where T : Form, new()
        {
            using var form = new T();
            form.ShowDialog(this);
        }

        // Invoice: the sales and returns of a period (all cashiers), with their Unpaid / Paid status and the receipt
        private void ShowSales(DateTime from, DateTime to) => ShowPage(_invoiceButton, () => new SalesHistoryForm(from, to, null));

        // Each report shows in the shared report screen, as a page
        private void OpenReport(Reports.ReportDefinition report, Button button) => ShowPage(button, () => new Reports.ReportForm(report));

        // One cashier screen at a time: bring it to the front if it is already open
        private static void OpenCashier()
        {
            var open = Application.OpenForms.OfType<CashierForm>().FirstOrDefault();
            if (open != null)
            {
                if (open.WindowState == FormWindowState.Minimized) open.WindowState = FormWindowState.Maximized;
                open.Activate();
                return;
            }
            new CashierForm().Show();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12)
            {
                OpenCashier();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F5)
            {
                _ = LoadDashboardAsync();
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        #endregion
    }
}
