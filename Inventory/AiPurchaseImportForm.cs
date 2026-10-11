using System.Globalization;
using Inventory.Ai;
using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // Purchase > "Read invoice (AI)": choose a photo of the supplier's invoice, the AI (OpenAI, appsettings.json)
    // reads its lines and each line is matched to an item of the system (fuzzy name, packaging, barcode).
    // Nothing is added before the user checked it: change the item, quantity or price, untick a line to skip it.
    // Prices stay in the invoice currency; the purchase converts them to its own currency when it differs.
    public partial class AiPurchaseImportForm : Form
    {
        private const string ChooseText = "— choose an item —";
        private const decimal GoodMatch = 0.5m;

        private readonly PurchaseRepository _purchaseRepository = new();
        private CurrencySet _currencies = CurrencySet.Default;
        private readonly List<RowData> _rows = new();
        private AiInvoice? _invoice;
        private string? _imagePath;
        private CancellationTokenSource? _reading;

        // One line read by the AI, its candidate items and the chosen one
        private sealed class RowData
        {
            public required AiInvoiceLine Line { get; init; }
            public List<PackagingLookupModel> Candidates { get; } = new();
            public PackagingLookupModel? Chosen { get; set; }

            public string TextOf(PackagingLookupModel p)
            {
                var text = $"{p.ItemName} · {p.PackagingName}";
                return Candidates.Count(c => $"{c.ItemName} · {c.PackagingName}" == text) > 1 ? $"{text} ({p.Barcode ?? "#" + p.PackagingId})" : text;
            }
        }

        private readonly bool _voice;                   // false = invoice photo, true = voice note
        private readonly VoiceRecorder _recorder = new();
        private DateTime _recordStarted;

        /// <param name="voice">true: a voice note (record or choose a file) instead of an invoice photo.</param>
        public AiPurchaseImportForm(bool voice = false)
        {
            InitializeComponent();
            _voice = voice;
            if (voice)
            {
                Text = "Purchase from a voice note (AI)";
                chooseButton.Text = "Choose voice note...";
                recordButton.Visible = true;
                photoBox.Visible = false;
                transcriptPanel.Visible = true;
                readAgainButton.Text = "Read the text again";
                readAgainButton.Width = 160;
            }
        }

        /// <summary>The checked lines (after DialogResult.OK): item, quantity, unit cost in the invoice currency (CurrencyCode).</summary>
        public List<(PackagingLookupModel Item, decimal Quantity, decimal UnitCost)> Lines { get; } = new();

        public string? SupplierName => string.IsNullOrWhiteSpace(_invoice?.SupplierName) ? null : _invoice.SupplierName.Trim();
        public string? InvoiceNumber =>string.IsNullOrWhiteSpace(_invoice?.InvoiceNumber) ? null : _invoice.InvoiceNumber.Trim();

        public DateTime? InvoiceDate =>
            DateTime.TryParseExact(_invoice?.InvoiceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

        /// <summary>The invoice discount in the invoice currency (null when none was read).</summary>
        public decimal? Discount => _invoice?.Discount is decimal d && d > 0 ? Math.Round(d, 2) : null;

        /// <summary>The currency the prices are in (chosen on this screen, or read on the invoice) and its rate to the primary.</summary>
        public string CurrencyCode => SelectedCurrency?.Code ?? _currencies.Primary.Code;
        public decimal CurrencyRate => Rate;

        private async void AiPurchaseImportForm_Load(object? sender, EventArgs e)
        {
            try { _currencies = await new CurrencyRepository().GetSetAsync(); } catch { /* LBP / USD defaults */ }
            currencyCombo.Items.AddRange(_currencies.Payable.Select(c => (object)c.Code).ToArray());
            currencyCombo.SelectedItem = _currencies.Primary.Code;

            if (AppConfig.OpenAi.EffectiveApiKey == null)
                ShowStatus("No OpenAI key yet: add it to appsettings.local.json, then restart the program.", Color.Firebrick);
            else if (_voice)
                ShowStatus("Press Record and say what you bought (e.g. \"2 pepsi box, 1 unica pack, 10 water bottle\"), or choose a voice note.", Color.DimGray);
            else
                ShowStatus("Choose a photo of the supplier's invoice (JPG or PNG). A clear, straight photo reads best.", Color.DimGray);
        }

        private async void AiPurchaseImportForm_Shown(object? sender, EventArgs e)
        {
            if (!_voice && AppConfig.OpenAi.EffectiveApiKey != null) await ChooseAndReadAsync();
        }

        #region Read the photo or the voice note

        private async void chooseButton_Click(object? sender, EventArgs e) => await ChooseAndReadAsync();

        // Photo: read it again. Voice: read the text again (after correcting it in the box)
        private async void readAgainButton_Click(object? sender, EventArgs e)
        {
            if (_voice)
            {
                if (transcriptBox.TextLength == 0) { ShowStatus("There is no text to read. Record or choose a voice note first.", Color.DarkGoldenrod); return; }
                await RunAsync("Reading the text", ct => InvoiceReader.ReadSpokenTextAsync(transcriptBox.Text, ct));
            }
            else if (_imagePath != null)
            {
                await ReadPhotoAsync(_imagePath);
            }
        }

        private async Task ChooseAndReadAsync()
        {
            using var dialog = new OpenFileDialog
            {
                Title = _voice ? "Voice note" : "Photo of the purchase invoice",
                Filter = _voice
                    ? "Voice notes (*.ogg;*.opus;*.mp3;*.m4a;*.wav;*.webm;*.mp4;*.flac)|*.ogg;*.opus;*.mp3;*.m4a;*.wav;*.webm;*.mp4;*.mpeg;*.mpga;*.flac"
                    : "Images (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            if (_voice) await ReadVoiceAsync(dialog.FileName);
            else await ReadPhotoAsync(dialog.FileName);
        }

        private async Task ReadPhotoAsync(string path)
        {
            _imagePath = path;
            try
            {
                photoBox.Image?.Dispose();
                using (var stream = File.OpenRead(path)) photoBox.Image = Image.FromStream(stream);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "This file is not an image that can be opened:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            fileLabel.Text = Path.GetFileName(path);
            await RunAsync($"Reading the invoice with {AppConfig.OpenAi.Model} (10-40 seconds)", ct => InvoiceReader.ReadAsync(path, ct));
        }

        // Voice note: written out first (shown in the box, can be corrected), then turned into lines
        private async Task ReadVoiceAsync(string path)
        {
            fileLabel.Text = Path.GetFileName(path);
            await RunAsync("Listening to the voice note", async ct =>
            {
                var text = await InvoiceReader.TranscribeAsync(path, ct);
                transcriptBox.Text = text;
                ShowStatus("Turning what was said into purchase lines...", Color.FromArgb(29, 78, 160));
                return await InvoiceReader.ReadSpokenTextAsync(text, ct);
            });
        }

        // Runs one AI reading: busy state, timing, the lines, clear errors
        private async Task RunAsync(string doing, Func<CancellationToken, Task<AiInvoice>> read)
        {
            if (AppConfig.OpenAi.EffectiveApiKey == null)
            {
                MessageBox.Show(this, "There is no OpenAI key yet.\n\nPut it in appsettings.local.json next to the program:\n{ \"OpenAI\": { \"ApiKey\": \"sk-...\" } }\nthen restart the program.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _reading?.Cancel();
            _reading = new CancellationTokenSource();
            var started = DateTime.Now;
            SetBusy(true);
            ShowStatus(doing + "...", Color.FromArgb(29, 78, 160));
            try
            {
                _invoice = await read(_reading.Token);
                await MatchAsync(_invoice);
                ShowInvoice();
                ShowStatus(_invoice.Lines.Count == 0
                        ? "No items were found. " + (_voice ? "Say the quantity and the item, e.g. \"2 pepsi box\", or correct the text and read it again." : "Try a clearer photo.")
                        : $"Read {_invoice.Lines.Count} line(s) in {(DateTime.Now - started).TotalSeconds:0} s. Check each line, then add them to the purchase.",
                    _invoice.Lines.Count == 0 ? Color.DarkGoldenrod : Color.FromArgb(34, 139, 117));
            }
            catch (OperationCanceledException)
            {
                ShowStatus("Stopped.", Color.DimGray);
            }
            catch (Exception ex)
            {
                ShowStatus(_voice ? "The voice note could not be read." : "The invoice could not be read.", Color.Firebrick);
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // Record / Stop: the recording is read as soon as it is stopped
        private async void recordButton_Click(object? sender, EventArgs e)
        {
            if (!_recorder.IsRecording)
            {
                try
                {
                    _recorder.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                _recordStarted = DateTime.Now;
                recordButton.BackColor = Color.FromArgb(192, 57, 43);
                chooseButton.Enabled = readAgainButton.Enabled = false;
                recordTimer.Start();
                recordTimer_Tick(null, EventArgs.Empty);
                ShowStatus("Recording... say the items, then press Stop.", Color.FromArgb(192, 57, 43));
                return;
            }

            recordTimer.Stop();
            recordButton.Text = "🎤  Record";
            recordButton.BackColor = Color.FromArgb(37, 99, 235);
            string path;
            try
            {
                path = _recorder.Stop();
            }
            catch (Exception ex)
            {
                SetBusy(false);
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if ((DateTime.Now - _recordStarted).TotalSeconds < 1)
            {
                SetBusy(false);
                ShowStatus("That was too short. Press Record, speak, then press Stop.", Color.DarkGoldenrod);
                return;
            }
            try
            {
                await ReadVoiceAsync(path);
            }
            finally
            {
                try { File.Delete(path); } catch { /* temp file */ }
            }
        }

        private void recordTimer_Tick(object? sender, EventArgs e) =>
            recordButton.Text = $"■  Stop  {DateTime.Now - _recordStarted:mm\\:ss}";

        // Each line: the best matching items; a good match is chosen at once
        private async Task MatchAsync(AiInvoice invoice)
        {
            _rows.Clear();
            foreach (var line in invoice.Lines.Where(l => l.Quantity > 0 || l.LineTotal > 0))
            {
                var row = new RowData { Line = line };
                var name = string.IsNullOrWhiteSpace(line.ItemName) ? line.Text : line.ItemName;
                row.Candidates.AddRange(await _purchaseRepository.MatchPackagingAsync(name, line.Packaging, line.Barcode));
                if (row.Candidates.Count > 0 && row.Candidates[0].Score >= GoodMatch) row.Chosen = row.Candidates[0];
                _rows.Add(row);
            }

            // The invoice currency, when it is one of ours
            if (invoice.Currency is { Length: > 0 } code && _currencies.Find(code.Trim()) is { HasRate: true, IsActive: true } currency)
                currencyCombo.SelectedItem = currency.Code;
        }

        private void ShowInvoice()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(_invoice?.SupplierName)) parts.Add($"Supplier: {_invoice.SupplierName}");
            if (InvoiceNumber != null) parts.Add($"Invoice no.: {InvoiceNumber}");
            if (InvoiceDate is { } date) parts.Add($"Date: {date:dd/MM/yyyy}");
            if (_invoice?.Total is { } total) parts.Add($"Total on the invoice: {total:#,0.##}");
            if (_invoice?.Discount is > 0 and var discount) parts.Add($"Discount: {discount:#,0.##}");
            invoiceInfoLabel.Text = parts.Count > 0 ? string.Join("    ·    ", parts)
                : _voice ? "Items said in the voice note:"
                : "No supplier, number or date could be read.";

            linesGrid.Rows.Clear();
            foreach (var row in _rows)
            {
                var index = linesGrid.Rows.Add();
                var gridRow = linesGrid.Rows[index];
                gridRow.Tag = row;
                gridRow.Cells[useColumn.Index].Value = row.Chosen != null;
                gridRow.Cells[readColumn.Index].Value = row.Line.Text;
                gridRow.Cells[qtyColumn.Index].Value = row.Line.Quantity;
                gridRow.Cells[priceColumn.Index].Value = UnitPriceOf(row.Line);
                FillItemCell(gridRow);
            }
            UpdateRows();
        }

        // The price of one packaging: written on the line, else line total / quantity
        private static decimal? UnitPriceOf(AiInvoiceLine line) =>
            line.UnitPrice ?? (line.LineTotal is { } t && line.Quantity > 0 ? Math.Round(t / line.Quantity, 2) : null);

        private void FillItemCell(DataGridViewRow gridRow)
        {
            var row = (RowData)gridRow.Tag!;
            var cell = (DataGridViewComboBoxCell)gridRow.Cells[itemColumn.Index];
            cell.Items.Clear();
            cell.Items.Add(ChooseText);
            foreach (var c in row.Candidates) cell.Items.Add(row.TextOf(c));
            cell.Value = row.Chosen != null ? row.TextOf(row.Chosen) : ChooseText;
        }

        #endregion

        #region Checking the lines

        private void linesGrid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            // Apply combo / checkbox choices at once
            if (linesGrid.CurrentCell is DataGridViewComboBoxCell or DataGridViewCheckBoxCell)
                linesGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void linesGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || linesGrid.Rows[e.RowIndex].Tag is not RowData row) return;
            if (e.ColumnIndex == itemColumn.Index)
            {
                var text = linesGrid.Rows[e.RowIndex].Cells[itemColumn.Index].Value as string;
                row.Chosen = row.Candidates.FirstOrDefault(c => row.TextOf(c) == text);
                linesGrid.Rows[e.RowIndex].Cells[useColumn.Index].Value = row.Chosen != null;
            }
            UpdateRows();
        }

        // A line with no price read: the item's last cost, so the user only has to check it
        private void UpdateRows()
        {
            var rate = Rate;
            foreach (DataGridViewRow gridRow in linesGrid.Rows)
            {
                if (gridRow.Tag is not RowData row) continue;
                var score = row.Chosen?.Score;
                gridRow.Cells[matchColumn.Index].Value = row.Chosen == null ? "none" : row.Chosen.Score >= 1 ? "sure" : $"{score:P0}";
                gridRow.Cells[matchColumn.Index].Style.ForeColor = row.Chosen == null ? Color.Firebrick
                    : score >= 0.75m ? Color.FromArgb(34, 139, 117) : score >= GoodMatch ? Color.FromArgb(217, 119, 6) : Color.Firebrick;

                var price = ParseDecimal(gridRow.Cells[priceColumn.Index].Value);
                var cost = price ?? (row.Chosen is { } chosen ? Math.Round(chosen.LastCost / rate, 2) : null);   // in the invoice currency
                gridRow.Cells[costColumn.Index].Value = cost;
                gridRow.Cells[costColumn.Index].Style.ForeColor = price == null ? Color.DarkGoldenrod : Color.FromArgb(22, 32, 43);
                gridRow.Cells[costColumn.Index].ToolTipText = price == null ? "No price was read: the item's last cost is used. Check it." : string.Empty;
                gridRow.DefaultCellStyle.BackColor = row.Chosen == null ? Color.FromArgb(255, 241, 241) : Color.White;
            }

            var ready = ReadyRows().Count();
            var missing = linesGrid.Rows.Cast<DataGridViewRow>().Count(r => r.Tag is RowData { Chosen: null });
            summaryLabel.Text = linesGrid.Rows.Count == 0 ? string.Empty
                : $"{ready} line(s) ready" + (missing > 0 ? $"   ·   {missing} need an item (red): choose one, use \"Find item...\", or untick" : string.Empty);
            addButton.Enabled = ready > 0;
            addButton.Text = ready == 1 ? "Add 1 line to the purchase" : $"Add {ready} lines to the purchase";
            var currency = SelectedCurrency;
            rateLabel.Text = currency == null || currency.IsPrimary ? string.Empty : currency.RateText;
        }

        private IEnumerable<DataGridViewRow> ReadyRows() =>
            linesGrid.Rows.Cast<DataGridViewRow>()
                .Where(r => r.Tag is RowData { Chosen: not null } && r.Cells[useColumn.Index].Value is true
                            && ParseDecimal(r.Cells[qtyColumn.Index].Value) > 0);

        private CurrencyModel? SelectedCurrency => currencyCombo.SelectedItem is string code ? _currencies.Find(code) : null;

        private decimal Rate => SelectedCurrency is { IsPrimary: false, Rate: > 0 } c ? c.Rate!.Value : 1;

        private void currencyCombo_SelectedIndexChanged(object? sender, EventArgs e) => UpdateRows();

        // The current line: search the item list (name or barcode) and choose another item
        private async void findButton_Click(object? sender, EventArgs e)
        {
            if (linesGrid.CurrentRow is not { Tag: RowData row } gridRow)
            {
                MessageBox.Show(this, "Click a line first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var search = Prompt("Find item", $"Search the item for \"{row.Line.Text}\" (name or barcode):",
                string.IsNullOrWhiteSpace(row.Line.ItemName) ? row.Line.Text : row.Line.ItemName);
            if (string.IsNullOrWhiteSpace(search)) return;

            try
            {
                var matches = (await _purchaseRepository.FindPackagingAsync(search)).ToList();
                if (matches.Count == 0)
                {
                    MessageBox.Show(this, $"No item found for \"{search}\". If it is a new item, add it first (Item > Item list).",
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var chosen = matches[0];
                if (matches.Count > 1)
                {
                    using var picker = new PackagingPickerForm(matches, search);
                    if (picker.ShowDialog(this) != DialogResult.OK || picker.Selected == null) return;
                    chosen = picker.Selected;
                }
                chosen.Score = 1;   // chosen by the user
                row.Candidates.RemoveAll(c => c.PackagingId == chosen.PackagingId);
                row.Candidates.Insert(0, chosen);
                row.Chosen = chosen;
                FillItemCell(gridRow);
                gridRow.Cells[useColumn.Index].Value = true;
                UpdateRows();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not search items:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linesGrid_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            ShowStatus("Type a number.", Color.Firebrick);
        }

        #endregion

        private void addButton_Click(object? sender, EventArgs e)
        {
            if (linesGrid.IsCurrentCellInEditMode) linesGrid.EndEdit();
            Lines.Clear();
            foreach (var gridRow in ReadyRows())
            {
                var row = (RowData)gridRow.Tag!;
                var qty = ParseDecimal(gridRow.Cells[qtyColumn.Index].Value) ?? 0;
                var cost = ParseDecimal(gridRow.Cells[costColumn.Index].Value) ?? Math.Round(row.Chosen!.LastCost / Rate, 2);
                Lines.Add((row.Chosen!, qty, Math.Max(0, cost)));
            }
            if (Lines.Count == 0) return;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void AiPurchaseImportForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _reading?.Cancel();
            recordTimer.Stop();
            _recorder.Dispose();
            photoBox.Image?.Dispose();
        }

        private void SetBusy(bool busy)
        {
            UseWaitCursor = busy;
            chooseButton.Enabled = recordButton.Enabled = !busy;
            readAgainButton.Enabled = !busy && (_voice ? transcriptBox.TextLength > 0 : _imagePath != null);
            if (busy) addButton.Enabled = false;
        }

        private void ShowStatus(string text, Color color)
        {
            statusLabel.Text = text;
            statusLabel.ForeColor = color;
        }

        private static decimal? ParseDecimal(object? value) => value switch
        {
            decimal d => d,
            double d => (decimal)d,
            string s when decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out var d) => d,
            string s when decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) => d,
            _ => null,
        };

        // A small text input box
        private string? Prompt(string title, string caption, string text)
        {
            using var dialog = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(460, 140),
            };
            var label = new Label { Text = caption, Location = new Point(12, 12), Size = new Size(436, 40) };
            var box = new TextBox { Text = text, Location = new Point(12, 54), Width = 436 };
            var ok = new Button { Text = "Search", DialogResult = DialogResult.OK, Location = new Point(242, 92), Size = new Size(100, 34) };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(348, 92), Size = new Size(100, 34) };
            dialog.Controls.AddRange(new Control[] { label, box, ok, cancel });
            dialog.AcceptButton = ok;
            dialog.CancelButton = cancel;
            return dialog.ShowDialog(this) == DialogResult.OK ? box.Text.Trim() : null;
        }
    }
}
