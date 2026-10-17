using Inventory.Models;
using Inventory.Repositories;

namespace Inventory
{
    // The cashier's item list in memory, so a scan or a name search does not wait for the database
    // (with 100,000 items a scan is a dictionary lookup, a name search a few milliseconds).
    //   - by barcode and by scale code: dictionaries (a scan);
    //   - by name: every word must be in the product name, or the barcode starts with the text (as fn_pos_find_item).
    // Filled once (fn_pos_item_list), in the background, when the first cashier screen opens; kept for the next one.
    // Refresh() then reads only what changed since the last time: prices, names, barcodes, new items, stock,
    // items that can no longer be sold. It runs as soon as the database says something changed (LISTEN / NOTIFY:
    // a price changed in the back office reaches every cashier within a moment), after each sale, and every few
    // minutes in case a notification was lost; every 4 hours it reads everything again.
    // Not found here = the cashier asks the database as before, and what it finds is added here.
    // Used on the UI thread only; the first load builds its dictionaries on a background thread and swaps them in.
    public sealed class PosItemCache
    {
        public static PosItemCache Shared { get; } = new();

        // Ask for changes a little before the last answer: a sale still being saved at that moment is not missed
        private static readonly TimeSpan Overlap = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan FullReloadAge = TimeSpan.FromHours(4);

        private Dictionary<int, PosItemRow> _byPackaging = new();
        private Dictionary<string, PosItemRow> _byBarcode = new(StringComparer.Ordinal);
        private Dictionary<string, PosItemRow> _byScaleCode = new(StringComparer.Ordinal);
        private DateTimeOffset? _asOf;     // database time of the last answer
        private DateTime _fullLoadAt;
        private Task? _loading;
        private bool _refreshing;

        public bool IsLoaded { get; private set; }

        public int Count => _byPackaging.Count;

        /// <summary>When the list was last brought up to date (local time).</summary>
        public DateTime UpdatedAt { get; private set; }

        /// <summary>Loads every sellable item, once (later calls wait for the same load). Throws when the database fails.</summary>
        public Task LoadAsync()
        {
            if (IsLoaded) return Task.CompletedTask;
            return _loading ??= LoadAllAsync();
        }

        private async Task LoadAllAsync()
        {
            try
            {
                var rows = await new SaleRepository().GetItemListAsync(null);
                var (byPackaging, byBarcode, byScaleCode) = await Task.Run(() =>
                {
                    var packagings = new Dictionary<int, PosItemRow>(rows.Count);
                    var barcodes = new Dictionary<string, PosItemRow>(rows.Count, StringComparer.Ordinal);
                    var scaleCodes = new Dictionary<string, PosItemRow>(StringComparer.Ordinal);
                    foreach (var row in rows)
                    {
                        packagings[row.PackagingId] = row;
                        if (!string.IsNullOrEmpty(row.Barcode)) barcodes[row.Barcode] = row;
                        if (!string.IsNullOrEmpty(row.ScaleCode)) scaleCodes[row.ScaleCode] = row;
                    }
                    return (packagings, barcodes, scaleCodes);
                });
                (_byPackaging, _byBarcode, _byScaleCode) = (byPackaging, byBarcode, byScaleCode);
                _asOf = rows.Count > 0 ? new DateTimeOffset(DateTime.SpecifyKind(rows[0].AsOf, DateTimeKind.Utc)) : null;
                IsLoaded = true;
                UpdatedAt = _fullLoadAt = DateTime.Now;
            }
            finally
            {
                if (!IsLoaded) _loading = null;   // a failed load is tried again next time
            }
        }

        /// <summary>
        /// Reads what changed since the last time (quick). Nothing happens before the first load. Throws when the database fails.
        /// Asked again while one is running (a notification came meanwhile): it runs once more right after, so no change is missed.
        /// </summary>
        public async Task RefreshAsync()
        {
            if (!IsLoaded) return;
            if (_refreshing)
            {
                _again = true;
                return;
            }
            try
            {
                _refreshing = true;
                do
                {
                    _again = false;
                    // No items at all yet, a long day, or many packagings deleted: everything again.
                    // The old list keeps answering until the new one is ready.
                    if (_asOf == null || _reload || DateTime.Now - _fullLoadAt > FullReloadAge)
                    {
                        _reload = false;
                        await LoadAllAsync();
                        continue;
                    }
                    var rows = await new SaleRepository().GetItemListAsync(_asOf - Overlap);
                    foreach (var row in rows)
                    {
                        Remove(row.PackagingId);
                        if (row.Sellable) Add(row);
                    }
                    if (rows.Count > 0) _asOf = new DateTimeOffset(DateTime.SpecifyKind(rows.Max(r => r.AsOf), DateTimeKind.Utc));
                    UpdatedAt = DateTime.Now;
                } while (_again);
            }
            finally
            {
                _refreshing = false;
            }
        }

        #region Told by the database

        // A price or an item changed somewhere (back office, another computer, a sale): the database says so at once
        // (NOTIFY 'pos_items', Database/pos_functions.sql) and the list reads the change. One connection of its own
        // stays open for this; when it breaks, it is opened again and the list catches up with a refresh.
        private bool _again, _reload, _listening;
        private SynchronizationContext? _ui;

        /// <summary>Raised on the UI thread after the list read a change the database told it about (e.g. to show it).</summary>
        public event Action? ChangedByDatabase;

        /// <summary>True while the database can tell the list about changes; false = the cashier's timer is the only refresh.</summary>
        public bool IsListening { get; private set; }

        /// <summary>Starts listening (once, for as long as the program runs). Call it on the UI thread.</summary>
        public void StartListening()
        {
            if (_listening) return;
            _listening = true;
            _ui = SynchronizationContext.Current ?? throw new InvalidOperationException("Start listening on the UI thread.");
            _ = Task.Run(ListenAsync);
        }

        private async Task ListenAsync()
        {
            while (true)
            {
                try
                {
                    await using var connection = BaseRepository.NewLongLivedConnection();
                    connection.Notification += (_, e) => _ui!.Post(_ => OnNotified(e.Payload), null);
                    await connection.OpenAsync();
                    await using (var listen = new Npgsql.NpgsqlCommand("LISTEN pos_items", connection))
                        await listen.ExecuteNonQueryAsync();
                    _ui!.Post(_ =>
                    {
                        IsListening = true;
                        OnNotified("changed");   // what changed while nobody was listening
                    }, null);
                    while (true) await connection.WaitAsync();
                }
                catch
                {
                    _ui!.Post(_ => IsListening = false, null);
                    await Task.Delay(TimeSpan.FromSeconds(5));   // the database is not reachable: try again
                }
            }
        }

        // On the UI thread
        private async void OnNotified(string payload)
        {
            if (!IsLoaded) return;
            if (payload.StartsWith("deleted:", StringComparison.Ordinal))
            {
                foreach (var id in payload["deleted:".Length..].Split(','))
                    if (int.TryParse(id, out var packagingId)) Remove(packagingId);
                ChangedByDatabase?.Invoke();
                return;
            }
            if (payload == "reload") _reload = true;
            try
            {
                await RefreshAsync();
                ChangedByDatabase?.Invoke();
            }
            catch { /* the next notification or the cashier's timer tries again */ }
        }

        #endregion

        /// <summary>
        /// As fn_pos_find_item: an exact barcode gives that one packaging (also when the label has the check digit the
        /// item was saved without, or a leading 0 more or less); otherwise every word must be in the product
        /// name, or the barcode starts with the text. By name, then pack size. Empty when nothing matches (or not loaded).
        /// </summary>
        public List<PosItemModel> Find(string search, int limit = 30)
        {
            search = search.Trim();
            if (!IsLoaded || search.Length == 0) return new();
            if (_byBarcode.TryGetValue(search, out var exact)) return new() { exact };
            // The same label written another way (saved without its check digit, a leading 0): Barcodes
            foreach (var variant in Barcodes.Variants(search))
                if (_byBarcode.TryGetValue(variant, out var same)) return new() { same };

            var words = search.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            return _byPackaging.Values
                .Where(r => words.All(w => r.ProductName.Contains(w, StringComparison.OrdinalIgnoreCase))
                            || (r.Barcode != null && r.Barcode.StartsWith(search, StringComparison.Ordinal)))
                .OrderBy(r => r.ProductName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(r => r.Factor)
                .ThenBy(r => r.PackagingId)
                .Take(Math.Max(limit, 1))
                .Cast<PosItemModel>()
                .ToList();
        }

        /// <summary>The packaging of a scale label's item code (as fn_pos_find_scale_item), or null.</summary>
        public PosItemModel? FindScaleItem(string scaleCode) =>
            IsLoaded && _byScaleCode.TryGetValue(scaleCode.Trim(), out var row) ? row : null;

        /// <summary>
        /// Items the database found that are not here yet (added since the last refresh): kept, so the next scan
        /// of them is fast. Their scale code is not known, so a scale label still asks the database once.
        /// </summary>
        public void Remember(IEnumerable<PosItemModel> items)
        {
            if (!IsLoaded) return;
            foreach (var item in items)
            {
                if (_byPackaging.ContainsKey(item.PackagingId)) continue;
                var row = new PosItemRow
                {
                    PackagingId = item.PackagingId, ProductId = item.ProductId, ItemName = item.ItemName,
                    PackagingName = item.PackagingName, Barcode = item.Barcode, SalePrice = item.SalePrice, Factor = item.Factor,
                    IsWeighted = item.IsWeighted, BaseUnit = item.BaseUnit, StockQty = item.StockQty,
                    ProductName = item.ProductName.Length > 0 ? item.ProductName : item.ItemName,
                };
                Add(row);
            }
        }

        private void Add(PosItemRow row)
        {
            _byPackaging[row.PackagingId] = row;
            if (!string.IsNullOrEmpty(row.Barcode)) _byBarcode[row.Barcode] = row;
            if (!string.IsNullOrEmpty(row.ScaleCode)) _byScaleCode[row.ScaleCode] = row;
        }

        private void Remove(int packagingId)
        {
            if (!_byPackaging.Remove(packagingId, out var old)) return;
            if (old.Barcode != null && _byBarcode.TryGetValue(old.Barcode, out var b) && b.PackagingId == packagingId) _byBarcode.Remove(old.Barcode);
            if (old.ScaleCode != null && _byScaleCode.TryGetValue(old.ScaleCode, out var s) && s.PackagingId == packagingId) _byScaleCode.Remove(old.ScaleCode);
        }
    }
}
