using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Settings > Shifts (managers): every cashier's open shift and the shifts of the chosen days.
    // A shift opens by itself when a user opens the cashier screen and stays open (also through log out) until it is
    // ended here: End shift (the selected one) or End of day (every open shift, one after the other). The cash
    // counted in the drawer is typed for each; the next sale of that user opens a new shift.
    // Built in code, like the cashier's small windows.
    public class ShiftsForm : Form
    {
        private readonly ShiftRepository _shiftRepository = new();
        private readonly DataGridView _grid;
        private readonly DateTimePicker _fromPicker, _toPicker;
        private readonly Button _endButton, _endDayButton;
        private readonly Label _countLabel;
        private List<ShiftModel> _shifts = new();

        public ShiftsForm()
        {
            Text = "Shifts";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1200, 620);
            MinimumSize = new Size(900, 400);
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
            _grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0 && Selected is { IsOpen: true }) await EndSelectedAsync(); };

            var toolPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(8, 6, 8, 6), WrapContents = false };
            _endButton = new Button { Text = "End shift...", Size = new Size(130, 36), BackColor = AppTheme.Accent, ForeColor = Color.White, UseVisualStyleBackColor = false };
            _endButton.Click += async (_, _) => await EndSelectedAsync();
            _endDayButton = new Button { Text = "End of day...", Size = new Size(130, 36) };
            _endDayButton.Click += async (_, _) => await EndOfDayAsync();
            var printButton = new Button { Text = "Print report...", Size = new Size(120, 36) };
            printButton.Click += async (_, _) =>
            {
                if (_shifts.Count == 0) { MessageBox.Show(this, "There is nothing to print.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                await PrintAsync(_shifts, "Shifts report", _fromPicker.Value.Date, _toPicker.Value.Date);
            };
            var refreshButton = new Button { Text = "Refresh (F5)", Size = new Size(110, 36) };
            refreshButton.Click += async (_, _) => await LoadAsync();
            _fromPicker = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Value = DateTime.Today, Margin = new Padding(3, 8, 3, 3) };
            _toPicker = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Value = DateTime.Today, Margin = new Padding(3, 8, 3, 3) };
            _fromPicker.ValueChanged += async (_, _) => await LoadAsync();
            _toPicker.ValueChanged += async (_, _) => await LoadAsync();
            _countLabel = new Label { AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(16, 11, 3, 0) };
            toolPanel.Controls.AddRange(new Control[]
            {
                _endButton, _endDayButton, printButton, refreshButton,
                new Label { Text = "Shifts opened from", AutoSize = true, Margin = new Padding(20, 11, 3, 0) }, _fromPicker,
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
                if (e.KeyCode == Keys.F5) { e.SuppressKeyPress = true; await LoadAsync(); }
                else if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; Close(); }
            };
        }

        private ShiftModel? Selected => _grid.CurrentRow?.DataBoundItem as ShiftModel;

        private async Task LoadAsync(int? selectId = null)
        {
            selectId ??= Selected?.ShiftId;
            try
            {
                var from = _fromPicker.Value.Date;
                var to = _toPicker.Value.Date < from ? from : _toPicker.Value.Date;
                _shifts = (await _shiftRepository.GetListAsync(from, to)).ToList();
                _grid.DataSource = _shifts;
                var open = _shifts.Count(s => s.IsOpen);
                _countLabel.Text = _shifts.Count == 0 ? "No shifts: a shift opens when a user opens the cashier screen."
                    : $"{open} open  ·  {_shifts.Count - open} ended  ·  sales {_shifts.Sum(s => s.SalesLbp - s.RefundsLbp):#,0} LBP";
                if (_shifts.FindIndex(s => s.ShiftId == selectId) is var index and >= 0)
                    _grid.CurrentCell = _grid.Rows[index].Cells.Cast<DataGridViewCell>().First(c => c.Visible);
            }
            catch (Exception ex)
            {
                var message = ex is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UndefinedFunction }
                    ? "Shifts are not set up in the database yet. Apply Database/shift_functions.sql (psql -h localhost -U postgres -d Pos -f shift_functions.sql)."
                    : "Could not load the shifts:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message);
                MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            _endButton.Enabled = Selected is { IsOpen: true };
            _endDayButton.Enabled = _shifts.Any(s => s.IsOpen);
        }

        private async Task EndSelectedAsync()
        {
            if (Selected is not { IsOpen: true } shift) return;
            var ended = await EndAsync(shift, null);
            await LoadAsync(shift.ShiftId);
            if (ended is { } result) await PrintAsync(new[] { result }, "Shift report", result.OpenedAt.Date, DateTime.Today);
        }

        // Every open shift, one after the other; Cancel stops (the shifts already ended stay ended)
        private async Task EndOfDayAsync()
        {
            var open = _shifts.Where(s => s.IsOpen).ToList();
            if (open.Count == 0) return;
            if (MessageBox.Show(this, $"End of day: end the {open.Count} open shift(s)?\n\nFor each one you type the cash counted in the drawer.",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var ended = new List<ShiftModel>();
            for (var i = 0; i < open.Count; i++)
            {
                if (await EndAsync(open[i], $"End of day  ·  shift {i + 1} of {open.Count}") is not { } result) break;
                ended.Add(result);
            }
            await LoadAsync();
            if (ended.Count == 0) return;

            // One end-of-day slip with every shift ended now and the day's totals
            if (ended.Count < open.Count)
                MessageBox.Show(this, $"{ended.Count} of {open.Count} shift(s) ended; the others stay open.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            await PrintAsync(ended, "End of day", ended.Min(s => s.OpenedAt).Date, DateTime.Today);
        }

        // Shows the shift's figures worked out now, asks for the counted cash, ends it. Its final figures; null when cancelled.
        private async Task<ShiftModel?> EndAsync(ShiftModel shift, string? step)
        {
            ShiftModel? current;
            try { current = await _shiftRepository.GetSummaryAsync(shift.ShiftId); }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not read the shift:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            if (current is not { IsOpen: true })   // ended meanwhile (another manager)
                return current;

            using var dialog = new EndShiftForm(current, step);
            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.Result : null;
        }

        // The shift report on the 80 mm roll: previewed, then sent to this station's receipt printer
        private async Task PrintAsync(IReadOnlyList<ShiftModel> shifts, string title, DateTime from, DateTime to)
        {
            try
            {
                UseWaitCursor = true;
                var settings = Printing.ReceiptSettings.Current ?? await Printing.ReceiptSettings.LoadAsync();
                var currencies = await new CurrencyRepository().GetSetAsync();
                string company;
                try { company = await new CompanyRepository().GetNameAsync(); } catch { company = string.Empty; }
                var slip = ShiftSlip(shifts, title, from, to, company);
                UseWaitCursor = false;

                using var renderer = new Printing.SlipRenderer(slip, currencies, settings.PaperWidthMm);
                Printing.ReceiptPrinter.ShowPreview(this, renderer, $"{title} - {settings.PaperWidthMm} mm",
                    () => new Printing.SlipRenderer(slip, currencies, settings.PaperWidthMm), settings);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not print:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        //            My Store
        //           END OF DAY
        //           20/10/2026
        //   ------------------------------
        //   #12  Ali
        //   08:15 - 17:30  25 inv / 1 ret     2,450,000
        //   Cash counted / difference    1,900,000 / -5,000
        //   ------------------------------
        //   NET SALES                 4,150,000
        //   Cash expected LBP ...
        private static Printing.ReportSlip ShiftSlip(IReadOnlyList<ShiftModel> shifts, string title, DateTime from, DateTime to, string company)
        {
            static string M(decimal value) => value.ToString("#,0.##");
            static string Signed(decimal value) => value.ToString("+#,0.##;-#,0.##;0");

            var header = new List<string> { from.Date == to.Date ? $"{from:dd/MM/yyyy}" : $"{from:dd/MM/yyyy} - {to:dd/MM/yyyy}" };
            if (shifts.Count == 1) header.Add($"Shift #{shifts[0].ShiftId}  ·  {shifts[0].UserName}");
            else header.Add($"{shifts.Count} shifts");

            var rows = shifts.Select(s => new Printing.SlipRow(
                $"#{s.ShiftId}  {s.UserName}",
                $"{s.OpenedAt:HH:mm} - {(s.ClosedAt is { } c ? c.ToString(c.Date == s.OpenedAt.Date ? "HH:mm" : "dd/MM HH:mm") : "open")}  ·  {s.Invoices} inv / {s.Returns} ret",
                M(s.SalesLbp - s.RefundsLbp),
                s.IsOpen ? "Expected (open)" : "Counted / diff.",
                s.IsOpen ? $"{M(s.ExpectedCashLbp)} LBP" : $"{M(s.CountedCashLbp ?? 0)} / {Signed(s.DifferenceLbp ?? 0)} LBP")).ToList();

            var ended = shifts.Where(s => !s.IsOpen).ToList();
            var totals = new List<Printing.SlipTotal>
            {
                new("Sales", M(shifts.Sum(s => s.SalesLbp))),
                new("Refunds", M(-shifts.Sum(s => s.RefundsLbp))),
                new("NET SALES", M(shifts.Sum(s => s.SalesLbp - s.RefundsLbp)), Strong: true),
                new("Card", M(shifts.Sum(s => s.CardLbp))),
                new("On account (unpaid)", M(shifts.Sum(s => s.OnAccountLbp))),
            };
            if (shifts.Any(s => s.ClientCashLbp != 0 || s.ClientCashUsd != 0))
                totals.Add(new("Client payments (cash)", $"{M(shifts.Sum(s => s.ClientCashLbp))} LBP  {M(shifts.Sum(s => s.ClientCashUsd))} USD"));
            totals.Add(new("Cash expected LBP", M(shifts.Sum(s => s.ExpectedCashLbp))));
            totals.Add(new("Cash expected USD", M(shifts.Sum(s => s.ExpectedCashUsd))));
            if (ended.Count > 0)
            {
                totals.Add(new("Cash counted LBP", M(ended.Sum(s => s.CountedCashLbp ?? 0))));
                totals.Add(new("Cash counted USD", M(ended.Sum(s => s.CountedCashUsd ?? 0))));
                totals.Add(new("DIFFERENCE LBP", Signed(ended.Sum(s => s.DifferenceLbp ?? 0)), Strong: true));
                totals.Add(new("DIFFERENCE USD", Signed(ended.Sum(s => s.DifferenceUsd ?? 0)), Strong: true));
            }

            return new Printing.ReportSlip
            {
                Company = company,
                Title = title,
                Header = header,
                ColumnsLeft = "Time  ·  invoices / returns",
                ColumnsRight = "Net sales",
                Rows = rows,
                Totals = totals,
            };
        }

        private void FormatGrid()
        {
            foreach (DataGridViewColumn column in _grid.Columns)
            {
                if (column.ValueType == typeof(decimal) || column.ValueType == typeof(decimal?))
                {
                    column.DefaultCellStyle.Format = "#,0.##";
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (column.ValueType == typeof(DateTime) || column.ValueType == typeof(DateTime?))
                {
                    column.DefaultCellStyle.Format = "dd/MM HH:mm";
                }
            }
            if (_grid.Columns[nameof(ShiftModel.ShiftId)] is { } id) id.FillWeight = 50;
            if (_grid.Columns[nameof(ShiftModel.UserName)] is { } name) name.FillWeight = 130;
            if (_grid.Columns[nameof(ShiftModel.Invoices)] is { } invoices) invoices.FillWeight = 60;
            if (_grid.Columns[nameof(ShiftModel.Returns)] is { } returns) returns.FillWeight = 60;
            if (_grid.Columns[nameof(ShiftModel.StatusText)] is { } status) status.FillWeight = 60;
        }

        private void grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.CellStyle == null || _grid.Rows[e.RowIndex].DataBoundItem is not ShiftModel shift) return;
            var property = _grid.Columns[e.ColumnIndex].DataPropertyName;
            if (property == nameof(ShiftModel.StatusText))
                e.CellStyle.ForeColor = shift.IsOpen ? Color.FromArgb(34, 139, 117) : Color.DimGray;
            else if (property is nameof(ShiftModel.DifferenceLbp) or nameof(ShiftModel.DifferenceUsd) && e.Value is decimal d && d != 0)
                e.CellStyle.ForeColor = d < 0 ? Color.Firebrick : Color.FromArgb(217, 119, 6);
        }
    }

    // End shift: the shift's figures, the cash counted in the drawer (LBP and USD) and the difference with what is expected
    public class EndShiftForm : Form
    {
        private readonly ShiftModel _shift;
        private readonly NumericUpDown _countedLbp, _countedUsd;
        private readonly Label _differenceLabel;
        private readonly Button _endButton;

        public ShiftModel? Result { get; private set; }

        public EndShiftForm(ShiftModel shift, string? step)
        {
            _shift = shift;
            Text = step ?? "End shift";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(520, 560);
            Font = new Font("Segoe UI", 10F);

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(18, 14, 18, 4), AutoScroll = true };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));

            void Title(string text)
            {
                table.Controls.Add(new Label { Text = text, AutoSize = true, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), Margin = new Padding(0, 10, 0, 4) });
                table.SetColumnSpan(table.Controls[^1], 2);
            }
            void Row(string caption, string value, bool strong = false)
            {
                table.Controls.Add(new Label { Text = caption, AutoSize = true, ForeColor = Color.FromArgb(90, 98, 108), Margin = new Padding(0, 3, 0, 3) });
                table.Controls.Add(new Label
                {
                    Text = value, AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(0, 3, 0, 3),
                    Font = strong ? new Font("Segoe UI Semibold", 10F, FontStyle.Bold) : null,
                });
            }

            Title($"Shift #{shift.ShiftId}  ·  {shift.UserName}");
            Row("Opened", $"{shift.OpenedAt:dd/MM/yyyy HH:mm}  ({Duration(DateTime.Now - shift.OpenedAt)})");
            Row("Invoices / returns", $"{shift.Invoices} / {shift.Returns}");
            Row("Sales", Money(shift.SalesLbp));
            Row("Refunds", Money(-shift.RefundsLbp));
            Row("Card", Money(shift.CardLbp));
            Row("On account (unpaid)", Money(shift.OnAccountLbp));
            if (shift.ClientCashLbp != 0 || shift.ClientCashUsd != 0)
                Row("Client payments (cash)", $"{Money(shift.ClientCashLbp)}   {shift.ClientCashUsd:#,0.##} USD");

            Title("Cash in the drawer");
            Row("Expected LBP", Money(shift.ExpectedCashLbp), strong: true);
            Row("Expected USD", $"{shift.ExpectedCashUsd:#,0.##} USD", strong: true);

            _countedLbp = MoneyInput(0);
            _countedUsd = MoneyInput(2);
            table.Controls.Add(new Label { Text = "Counted LBP", AutoSize = true, Margin = new Padding(0, 10, 0, 3) });
            table.Controls.Add(_countedLbp);
            table.Controls.Add(new Label { Text = "Counted USD", AutoSize = true, Margin = new Padding(0, 10, 0, 3) });
            table.Controls.Add(_countedUsd);
            _differenceLabel = new Label { AutoSize = true, Margin = new Padding(0, 10, 0, 3) };
            table.Controls.Add(_differenceLabel);
            table.SetColumnSpan(_differenceLabel, 2);

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12, 10, 12, 8) };
            var cancel = new Button { Text = "Cancel", Size = new Size(110, 40), DialogResult = DialogResult.Cancel };
            _endButton = new Button
            {
                Text = "End shift", Size = new Size(150, 40), BackColor = Color.FromArgb(28, 110, 92), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
            };
            _endButton.FlatAppearance.BorderSize = 0;
            _endButton.Click += endButton_Click;
            bottom.Controls.AddRange(new Control[] { cancel, _endButton });

            Controls.Add(table);
            Controls.Add(bottom);
            CancelButton = cancel;
            AppTheme.Apply(this);

            _countedLbp.ValueChanged += (_, _) => ShowDifference();
            _countedUsd.ValueChanged += (_, _) => ShowDifference();
            Shown += (_, _) => { _countedLbp.Select(0, _countedLbp.Text.Length); _countedLbp.Focus(); };
            ShowDifference();
        }

        private void ShowDifference()
        {
            var lbp = _countedLbp.Value - _shift.ExpectedCashLbp;
            var usd = _countedUsd.Value - _shift.ExpectedCashUsd;
            _differenceLabel.Text = lbp == 0 && usd == 0 ? "Difference: none, the drawer is right."
                : $"Difference: {lbp:+#,0;-#,0;0} LBP   {usd:+#,0.##;-#,0.##;0} USD   ({(lbp < 0 || usd < 0 ? "missing" : "extra")})";
            _differenceLabel.ForeColor = lbp == 0 && usd == 0 ? Color.FromArgb(34, 139, 117) : lbp < 0 || usd < 0 ? Color.Firebrick : Color.FromArgb(217, 119, 6);
        }

        private async void endButton_Click(object? sender, EventArgs e)
        {
            _endButton.Enabled = false;
            UseWaitCursor = true;
            try
            {
                Result = await new ShiftRepository().CloseAsync(_shift.ShiftId, _countedLbp.Value, _countedUsd.Value);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not end the shift:\n" + (ex is Npgsql.PostgresException pg ? pg.MessageText : ex.Message),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _endButton.Enabled = true;
                UseWaitCursor = false;
            }
        }

        private static NumericUpDown MoneyInput(int decimals) => new()
        {
            Maximum = 100_000_000_000m,
            DecimalPlaces = decimals,
            ThousandsSeparator = true,
            TextAlign = HorizontalAlignment.Right,
            Width = 190,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 6, 0, 3),
        };

        private static string Money(decimal lbp) => $"{lbp:#,0} LBP";

        private static string Duration(TimeSpan time) =>
            time.TotalDays >= 1 ? $"{(int)time.TotalDays} d {time.Hours} h" : $"{(int)time.TotalHours} h {time.Minutes:00} min";
    }
}
