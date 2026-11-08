using System.Drawing.Imaging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Inventory.Ai
{
    // What the AI read on a supplier's invoice
    public sealed class AiInvoice
    {
        [JsonPropertyName("supplier_name")] public string? SupplierName { get; set; }
        [JsonPropertyName("invoice_number")] public string? InvoiceNumber { get; set; }
        [JsonPropertyName("invoice_date")] public string? InvoiceDate { get; set; }   // YYYY-MM-DD
        [JsonPropertyName("currency")] public string? Currency { get; set; }          // ISO code: LBP, USD...
        [JsonPropertyName("discount")] public decimal? Discount { get; set; }
        [JsonPropertyName("total")] public decimal? Total { get; set; }
        [JsonPropertyName("lines")] public List<AiInvoiceLine> Lines { get; set; } = new();
    }

    // One item line of the invoice
    public sealed class AiInvoiceLine
    {
        [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;           // as written on the invoice
        [JsonPropertyName("item_name")] public string ItemName { get; set; } = string.Empty;  // cleaned up, in the script it was written / said in
        [JsonPropertyName("item_name_alt")] public string? ItemNameAlt { get; set; }          // the same name in the other script (سيدرز <-> Cedars), or null
        [JsonPropertyName("packaging")] public string? Packaging { get; set; }                // box, pack, bottle, can, kg...
        [JsonPropertyName("pack_size")] public decimal? PackSize { get; set; }                // pieces in one packaging bought (الوحدة 24), or null
        [JsonPropertyName("quantity")] public decimal Quantity { get; set; }
        [JsonPropertyName("unit_price")] public decimal? UnitPrice { get; set; }
        [JsonPropertyName("line_total")] public decimal? LineTotal { get; set; }
        [JsonPropertyName("barcode")] public string? Barcode { get; set; }
        [JsonPropertyName("shop_item")] public string? ShopItem { get; set; }                 // voice: the shop's item name it is (from the list given), or null
    }

    // The items read on a photo for new items: one product, or a list (a written / printed list, several labels...)
    public sealed class AiItemList
    {
        [JsonPropertyName("items")] public List<AiItem> Items { get; set; } = new();
    }

    // One item read on the photo
    public sealed class AiItem
    {
        [JsonPropertyName("item_name")] public string? ItemName { get; set; }
        [JsonPropertyName("barcode")] public string? Barcode { get; set; }
        [JsonPropertyName("size")] public string? Size { get; set; }               // 330ml, 1kg...
        [JsonPropertyName("sale_price")] public decimal? SalePrice { get; set; }
        [JsonPropertyName("cost")] public decimal? Cost { get; set; }
        [JsonPropertyName("quantity")] public decimal? Quantity { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }       // ISO code: LBP, USD...
        [JsonPropertyName("packaging")] public string? Packaging { get; set; }     // the unit it is listed in: box, piece, jar...
        [JsonPropertyName("pack_size")] public decimal? PackSize { get; set; }     // pieces in that packaging ("*24" -> 24)
        [JsonPropertyName("line_total")] public decimal? LineTotal { get; set; }   // a list's Total column: checks quantity x cost
    }

    // Turns a photo of a supplier's invoice, or a voice note listing what was bought, into structured purchase lines
    // with OpenAI (settings: "OpenAI" in appsettings.json, the key in appsettings.local.json).
    // Photo: vision model; the photo is made smaller first (faster, cheaper).
    // Voice: speech to text, then the same model turns the text into lines.
    public static class InvoiceReader
    {
        private const int MaxImageSide = 2000;

        private const string Instructions =
            """
            You read photos of supplier purchase invoices for a shop in Lebanon (Arabic, English or French; printed or handwritten).
            Return every item line of the invoice. Do not invent lines, do not merge lines, keep the order of the invoice.
            For each line:
            - text: the line exactly as written (item and packaging words).
            - item_name: the product name cleaned up and spelled correctly (e.g. "pepsi bix" -> "Pepsi"), without quantity or packaging words,
              in the same script as written: Arabic stays in Arabic letters ("سيدرز قديم"), Latin stays Latin. Never translate it.
            - item_name_alt: always the same item in the other language, so it is found whether the shop saved it in Arabic or
              in English: Arabic -> English, English / French -> Arabic. Brands are written in the other letters, ordinary
              words are translated ("سيدرز قديم" -> "Cedars Old", "Pepsi" -> "بيبسي", "بيض بلدي" -> "farm eggs",
              "Basmati rice" -> "رز بسمتي"). null only for a code or number that has no other form.
            - packaging: the packaging or unit if written, in English, singular: box, pack, carton, bottle, can, piece, kg...
              Common short forms: "bx" = box, "bp" / "pk" = pack, "ctn" = carton, "pc" / "pcs" = piece. null if not written.
            - pack_size: how many pieces are in ONE of the packagings bought, when the invoice shows it: a Unit / Pack / Pcs
              column holding a number ("الوحدة" 24, "التعبئة" 12, "Unit 30"), or the name ("*24", "x12", "24'S", "6 pcs").
              1 when that number is 1 or the line is per piece. null when it is not written. It is not the size (330ml, 8G)
              and not the quantity: on "الكمية 2 · الوحدة 12 · السعر 13" quantity is 2, pack_size is 12, unit_price is 13.
            - quantity: the number of packagings bought.
            - unit_price: price of ONE packaging; null if not written. line_total: the line amount; null if not written.
            - barcode: only if a barcode / EAN is printed on that line; else null.
            - shop_item: null.
            Numbers: plain numbers without thousands separators (12500, not 12,500). If only line_total is written, still give it.
            Also give, when visible: supplier_name, invoice_number, invoice_date (YYYY-MM-DD), currency (ISO code: USD for $, LBP for L.L / ل.ل),
            discount (amount) and total. Use null for anything you cannot read; never guess numbers.
            Rows: read each row straight across the page: its barcode, name, quantity, unit price and total are on the same row,
            even when the photo is slanted or the paper is folded. Check every row: quantity x unit_price must equal its line_total;
            if it does not, a number was taken from the row above or below - read that row again. The line totals must add up to
            the invoice's total before discount.
            """;

        // Strict JSON schema: the model must answer exactly this shape
        private static readonly JsonNode Schema = JsonNode.Parse(
            """
            {
              "type": "object",
              "additionalProperties": false,
              "required": ["supplier_name", "invoice_number", "invoice_date", "currency", "discount", "total", "lines"],
              "properties": {
                "supplier_name":  { "type": ["string", "null"] },
                "invoice_number": { "type": ["string", "null"] },
                "invoice_date":   { "type": ["string", "null"] },
                "currency":       { "type": ["string", "null"] },
                "discount":       { "type": ["number", "null"] },
                "total":          { "type": ["number", "null"] },
                "lines": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "additionalProperties": false,
                    "required": ["text", "item_name", "item_name_alt", "packaging", "pack_size", "quantity", "unit_price", "line_total", "barcode", "shop_item"],
                    "properties": {
                      "text":       { "type": "string" },
                      "item_name":  { "type": "string" },
                      "item_name_alt": { "type": ["string", "null"] },
                      "packaging":  { "type": ["string", "null"] },
                      "pack_size":  { "type": ["number", "null"] },
                      "quantity":   { "type": "number" },
                      "unit_price": { "type": ["number", "null"] },
                      "line_total": { "type": ["number", "null"] },
                      "barcode":    { "type": ["string", "null"] },
                      "shop_item":  { "type": ["string", "null"] }
                    }
                  }
                }
              }
            }
            """)!;

        // Spoken purchase lists (voice notes): the same answer shape as an invoice photo
        private const string SpokenInstructions =
            """
            You receive the text of a voice note in which a shop owner in Lebanon lists what was bought from a supplier
            (English, Arabic - also Lebanese dialect - or French, often mixed in one sentence; it was written out automatically,
            so words can be misspelled, and English words may come out in Arabic letters or the other way round).
            Understand every item whatever language it was said in.
            Return one line per item said. Do not invent items. Keep the order in which they were said.
            Spoken numbers are quantities: "two pepsi box", "تنين بيبسي", "deux" -> 2; "a" / "one" / "وحدة" -> 1; "half a kilo" -> 0.5.
            For each line:
            - text: the words said for that item (e.g. "2 pepsi box").
            - item_name: the product name cleaned up and spelled correctly ("pepsi bix" -> "Pepsi"), without quantity or
              packaging words, in the same script as said: Arabic stays in Arabic letters ("سيدرز قديم", "مارلبورو أحمر"),
              Latin stays Latin. Never translate it: the shop's items are often written in Arabic.
            - item_name_alt: always the same item in the other language, so it is found whether the shop saved it in Arabic or
              in English: Arabic -> English, English / French -> Arabic. Brands are written in the other letters, ordinary
              words are translated ("سيدرز قديم" -> "Cedars Old", "مارلبورو أحمر" -> "Marlboro Red", "Pepsi" -> "بيبسي",
              "بيض" -> "eggs", "rice basmati" -> "رز بسمتي", "حلوم" -> "halloumi cheese"). null only for a code or number.
            - packaging: box, pack, carton, bottle, can, piece, kg... in English, singular ("علبة"/"كرتونة" -> box / carton,
              "كروز" (of cigarettes) -> carton, "قنينة" -> bottle, "كيلو" -> kg, "bp" / "pk" -> pack); null if not said.
            - pack_size: how many pieces are in one of those packagings when it is said ("a box of 24" -> 24); else null.
            - quantity: the number of packagings; 1 when no number was said.
            - unit_price / line_total: only when a price was said ("at 9 dollars each" -> unit_price 9); else null.
            - barcode: null.
            - shop_item: when the list of the shop's items is given, the name of the item this line is, copied EXACTLY as it
              is written in that list (even when the list spells it wrongly, or in the other script: "صحة" -> "Sohat water",
              "Aquafina" -> "aquafena", "بيبسي" -> "Pepsi can"). Think of the brand: a Lebanese brand said in Arabic is often saved
              under its Latin name and the other way round. null when no item of the list is this one, or no list is given.
            supplier_name, invoice_number, invoice_date, currency (USD when dollars were said, LBP for lira / ليرة),
            discount and total: only when said; else null. Never guess numbers.
            """;

        // Photo for new items: one product (its package, shelf label / price tag) or a list of items (written, printed, a table...)
        private const string ItemInstructions =
            """
            You read one photo taken to add new items to a shop's inventory in Lebanon (Arabic, English or French; printed or handwritten).
            The photo shows either ONE product (its package, its shelf label / price tag, or a note written for it),
            or a LIST of items (a handwritten or printed list, a table, a price list, several labels or products side by side).
            Return one entry in "items" per item: one entry for a single product, one per line / label / product for a list.
            Do not invent items, do not merge items, keep the order of the photo (top to bottom, left to right).
            Column headers of a list tell which number is which (sale / price / سعر, cost / تكلفة, qty / الكمية / عدد, barcode / باركود).
            For each item:
            - item_name: the product name as a shop would list it: brand + product + variant (e.g. "Pepsi", "Almarai Full Fat Milk"),
              spelled correctly, without the size and without price words. Keep the script it is written in; if both Arabic and
              Latin are printed, use the Latin one. null if no name can be read.
            - barcode: the digits printed under the barcode (EAN-13, EAN-8, UPC...), digits only, no spaces. Copy every digit exactly;
              null if no barcode is visible or a digit cannot be read for sure.
            - size: the size or weight printed on the product ("330ml", "1L", "500g"); null if not visible.
            - sale_price: the selling price if a price tag / label / note shows one; null otherwise.
            - cost: the cost / purchase price only if it is written as such (e.g. "cost 1.2"); null otherwise.
            - quantity: the quantity in stock only if it is written (e.g. "qty 24"); null otherwise.
            - currency: ISO code of the prices (USD for $, LBP for L.L / ل.ل); null if no price or no sign.
            - packaging: the unit the item is listed in, in English, singular: box, piece, jar, pack, carton, bag, bottle...
              ("علبة" = box, "PC" / "PCS" = piece, "JAR" = jar, "كرتونة" = carton, "ربطة" = pack); null if not written.
            - pack_size: how many pieces are in that packaging, when written: in the name ("KINDER T4 *20" -> 20,
              "GUM 8G*60" -> 60, "12CM*6" -> 6, "20*5" -> 100, "x24" / "24'S" / "24 pcs" -> 24) or in a pack column.
              null when the item is listed per piece or the number is not written. Do not take the size (8G, 130G, 612.5G) for it.
            - line_total: the line amount if the list has a Total / Amount column; null otherwise.
            A supplier's invoice or sales invoice (columns like Item / Barcode, Name, Unit, Qty, Unit Price, Disc, Total):
            the Item / code column is the barcode; Unit Price is the cost of ONE packaging (put it in cost, not sale_price);
            Qty is quantity. Read each row straight across: the barcode, name, qty and price of a row are on the same line.
            Barcodes: read them digit by digit, they are long (13 digits for EAN-13); never fill a digit you cannot see.
            Numbers: plain numbers without thousands separators (12500, not 12,500). Use null for anything you cannot read; never guess.
            """;

        private static readonly JsonNode ItemSchema = JsonNode.Parse(
            """
            {
              "type": "object",
              "additionalProperties": false,
              "required": ["items"],
              "properties": {
                "items": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "additionalProperties": false,
                    "required": ["item_name", "barcode", "size", "sale_price", "cost", "quantity", "currency", "packaging", "pack_size", "line_total"],
                    "properties": {
                      "item_name":  { "type": ["string", "null"] },
                      "barcode":    { "type": ["string", "null"] },
                      "size":       { "type": ["string", "null"] },
                      "sale_price": { "type": ["number", "null"] },
                      "cost":       { "type": ["number", "null"] },
                      "quantity":   { "type": ["number", "null"] },
                      "currency":   { "type": ["string", "null"] },
                      "packaging":  { "type": ["string", "null"] },
                      "pack_size":  { "type": ["number", "null"] },
                      "line_total": { "type": ["number", "null"] }
                    }
                  }
                }
              }
            }
            """)!;

        /// <summary>Reads a photo of a supplier's invoice (read again once when its numbers do not add up).</summary>
        public static Task<AiInvoice> ReadAsync(string imagePath, CancellationToken cancel = default) =>
            ReadCheckedAsync<AiInvoice>(Instructions, imagePath, "Read this purchase invoice.", "purchase_invoice", Schema,
                // A line on the edge of two strips may come twice
                invoice => invoice.Lines = invoice.Lines.DistinctBy(l => (l.Text, l.Barcode, l.Quantity, l.UnitPrice, l.LineTotal)).ToList(),
                InvoiceProblems, cancel);

        /// <summary>Reads a photo of one item (product, label, price tag) or a list of items: name, barcode, prices, quantity.</summary>
        public static Task<AiItemList> ReadItemsAsync(string imagePath, CancellationToken cancel = default) =>
            ReadCheckedAsync<AiItemList>(ItemInstructions, imagePath, "Read the item(s) on this photo.", "new_items", ItemSchema,
                list => list.Items = list.Items.DistinctBy(i => (i.ItemName, i.Barcode, i.Quantity, i.Cost, i.LineTotal)).ToList(),
                list => list.Items.Select(i => RowProblem(i.ItemName, i.Quantity, i.Cost, i.LineTotal)).OfType<string>().ToList(),
                cancel);

        /// <summary>Quantity x unit price that is not the line's total (a number read from another row), or null.</summary>
        public static string? RowProblem(string? name, decimal? quantity, decimal? unitPrice, decimal? lineTotal) =>
            quantity is > 0 && unitPrice is > 0 && lineTotal is > 0
            && Math.Abs(quantity.Value * unitPrice.Value - lineTotal.Value) > Math.Max(0.011m, lineTotal.Value * 0.005m)
                ? $"\"{name}\": {quantity:0.###} x {unitPrice:0.####} = {quantity * unitPrice:0.##}, but its total is {lineTotal:0.##}"
                : null;

        // Lines whose quantity x price is not their total, and line totals that do not make the invoice's total
        private static List<string> InvoiceProblems(AiInvoice invoice)
        {
            var problems = invoice.Lines.Select(l => RowProblem(l.Text, l.Quantity, l.UnitPrice, l.LineTotal)).OfType<string>().ToList();
            if (invoice.Total is > 0 && invoice.Lines.Count > 0 && invoice.Lines.All(l => l.LineTotal != null))
            {
                var sum = invoice.Lines.Sum(l => l.LineTotal!.Value);
                var expected = invoice.Total.Value + (invoice.Discount ?? 0);
                if (Math.Abs(sum - expected) > 0.02m)
                    problems.Add($"the line totals add up to {sum:0.##}, but the invoice's total before discount is {expected:0.##} (a line is missing, extra or misread)");
            }
            return problems;
        }

        // Reads the photo; when its numbers do not add up, sends it once more with the first reading and what is wrong,
        // and keeps the reading with fewer problems
        private static async Task<T> ReadCheckedAsync<T>(string instructions, string imagePath, string ask, string schemaName, JsonNode schema,
            Action<T> clean, Func<T, List<string>> problemsOf, CancellationToken cancel) where T : class
        {
            var first = await ExtractAsync<T>(instructions, ImageContent(imagePath, ask), "this image", schemaName, schema, cancel);
            clean(first);
            var problems = problemsOf(first);
            if (problems.Count == 0) return first;

            var again = ask + "\nYou read this photo once already; that reading (JSON) does not add up:\n- " + string.Join("\n- ", problems.Take(30)) +
                        "\nLook at those rows on the photo again, straight across each row, and return the WHOLE reading again, corrected " +
                        "(every row, not only the wrong ones). Your first reading:\n" + JsonSerializer.Serialize(first);
            try
            {
                var second = await ExtractAsync<T>(instructions, ImageContent(imagePath, again), "this image", schemaName, schema, cancel);
                clean(second);
                return problemsOf(second).Count < problems.Count ? second : first;
            }
            catch (Exception) when (!cancel.IsCancellationRequested)
            {
                return first;   // the first reading is still shown, with its problems marked
            }
        }

        /// <summary>Turns the text of a voice note ("2 pepsi box, 1 unica pack...") into purchase lines.</summary>
        /// shopItems = the names of the shop's items: the AI then says which one each line is (shop_item), so an item
        /// saved in the other script or misspelled ("صحه" said, "Sohat water" saved) is still found.
        public static Task<AiInvoice> ReadSpokenTextAsync(string text, CancellationToken cancel = default, IReadOnlyCollection<string>? shopItems = null) =>
            ExtractAsync<AiInvoice>(SpokenInstructions,
                "Voice note:\n" + text.Trim()
                + (shopItems is { Count: > 0 } ? "\n\nThe shop's items (one per line):\n" + string.Join("\n", shopItems) : string.Empty),
                "this voice note", "purchase_invoice", Schema, cancel);

        // The cashier: the same reading, for what a customer buys ("1 pepsi can, 3 bread", "تلاتة خبز")
        private static readonly string CashierSpokenInstructions =
            SpokenInstructions.Replace("lists what was bought from a supplier", "tells the cashier what a customer is buying");

        /// <summary>Turns what the cashier said ("1 pepsi can, 3 bread") into lines: quantity, item (in both languages), packaging.</summary>
        public static Task<AiInvoice> ReadSpokenSaleAsync(string text, CancellationToken cancel = default) =>
            ExtractAsync<AiInvoice>(CashierSpokenInstructions, "Voice note:\n" + text.Trim(), "this voice note", "purchase_invoice", Schema, cancel);

        // The user message: a short text and the photo. A tall photo (a whole invoice or list) also goes as zoomed strips:
        // OpenAI shrinks every image to about 768 pixels on its short side, so on a full page the small digits of the
        // barcodes and prices become a few pixels high; in a strip they are 2-3 times bigger.
        private static JsonArray ImageContent(string imagePath, string text)
        {
            var (whole, strips) = PrepareImages(imagePath);
            var content = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = strips.Count == 0 ? text
                        : text + $"\nThe first image is the whole photo. The next {strips.Count} images are zoomed strips of it, from top to bottom, " +
                          "that overlap a little: read the barcodes, numbers and names from the strips, use the whole photo for the columns. " +
                          "A line on the edge of two strips is in both: return it only once.",
                },
            };
            foreach (var image in strips.Prepend(whole))
                content.Add(new JsonObject
                {
                    ["type"] = "image_url",
                    ["image_url"] = new JsonObject { ["url"] = "data:image/jpeg;base64," + Convert.ToBase64String(image), ["detail"] = "high" },
                });
            return content;
        }

        /// <summary>
        /// Writes out a voice note (wav, mp3, m4a, ogg - e.g. WhatsApp -, webm...): OpenAI speech to text,
        /// model "TranscriptionModel" in appsettings.json. Returns the text.
        /// </summary>
        public static async Task<string> TranscribeAsync(string audioPath, CancellationToken cancel = default, string? prompt = null)
        {
            var settings = AppConfig.OpenAi;
            var file = new FileInfo(audioPath);
            if (file.Length > 25 * 1024 * 1024) throw new InvalidOperationException("The voice note is too long (more than 25 MB). Record shorter notes.");

            using var form = new MultipartFormDataContent();
            var audio = new ByteArrayContent(await File.ReadAllBytesAsync(audioPath, cancel));
            audio.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            // WhatsApp (Android) saves voice notes as .opus: the same Ogg audio OpenAI reads as .ogg
            var name = Path.GetExtension(audioPath).Equals(".opus", StringComparison.OrdinalIgnoreCase)
                ? Path.ChangeExtension(Path.GetFileName(audioPath), ".ogg")
                : Path.GetFileName(audioPath);
            form.Add(audio, "file", name);
            form.Add(new StringContent(settings.TranscriptionModel), "model");
            form.Add(new StringContent(prompt ?? "A shop owner lists items bought for a purchase invoice: quantities, products and packaging " +
                                       "(box, pack, carton, bottle, kg), sometimes prices. English, Arabic or French."), "prompt");
            if (!string.IsNullOrWhiteSpace(settings.TranscriptionLanguage)) form.Add(new StringContent(settings.TranscriptionLanguage.Trim()), "language");

            var text = await SendAsync("/audio/transcriptions", form, settings.TranscriptionModel, cancel);
            var transcript = JsonNode.Parse(text)?["text"]?.GetValue<string>();
            return string.IsNullOrWhiteSpace(transcript)
                ? throw new InvalidOperationException("Nothing could be heard in this voice note. Record again, closer to the microphone.")
                : transcript.Trim();
        }

        // Chat completion with a strict JSON schema: the answer always has the shape of T
        private static async Task<T> ExtractAsync<T>(string instructions, JsonNode userContent, string what, string schemaName, JsonNode schema, CancellationToken cancel)
        {
            var settings = AppConfig.OpenAi;
            var body = new JsonObject
            {
                ["model"] = settings.Model,
                ["temperature"] = 0,
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["role"] = "system", ["content"] = instructions },
                    new JsonObject { ["role"] = "user", ["content"] = userContent },
                },
                ["response_format"] = new JsonObject
                {
                    ["type"] = "json_schema",
                    ["json_schema"] = new JsonObject { ["name"] = schemaName, ["strict"] = true, ["schema"] = schema.DeepClone() },
                },
            };

            var text = await SendAsync("/chat/completions", new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"), settings.Model, cancel);
            var reply = JsonNode.Parse(text)?["choices"]?[0]?["message"];
            if (reply?["refusal"]?.GetValue<string>() is { Length: > 0 } refusal)
                throw new InvalidOperationException($"The AI could not read {what}: " + refusal);
            var json = reply?["content"]?.GetValue<string>()
                       ?? throw new InvalidOperationException($"The AI gave an empty answer for {what}. Try again.");
            return JsonSerializer.Deserialize<T>(json)
                   ?? throw new InvalidOperationException("The AI answer could not be read.");
        }

        // One OpenAI call; returns the answer text, or a clear error
        private static async Task<string> SendAsync(string path, HttpContent content, string model, CancellationToken cancel)
        {
            var settings = AppConfig.OpenAi;
            var key = settings.EffectiveApiKey
                      ?? throw new InvalidOperationException("No OpenAI API key. Put it in appsettings.local.json next to the program ({ \"OpenAI\": { \"ApiKey\": \"sk-...\" } }) and restart the program.");

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(Math.Max(20, settings.TimeoutSeconds)) };
            using var request = new HttpRequestMessage(HttpMethod.Post, settings.BaseUrl.TrimEnd('/') + path) { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

            HttpResponseMessage response;
            try
            {
                response = await http.SendAsync(request, cancel);
            }
            catch (TaskCanceledException) when (!cancel.IsCancellationRequested)
            {
                throw new InvalidOperationException("The AI did not answer in time. Check the internet connection and try again.");
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("Could not reach OpenAI: " + ex.Message + "\nCheck the internet connection.");
            }

            using (response)
            {
                var text = await response.Content.ReadAsStringAsync(cancel);
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException(ErrorMessage(response.StatusCode, text, model));
                return text;
            }
        }

        // The photo as a JPEG at most MaxImageSide pixels on its longest side (phone photos are often 4000+ px),
        // and, when it is tall (a page), strips of it about 3/4 as high as wide, overlapping by StripOverlap
        private const int MaxStripWidth = 2000;
        private const double StripOverlap = 0.15;

        private static (byte[] Whole, List<byte[]> Strips) PrepareImages(string path)
        {
            using var original = Image.FromFile(path);
            if (original.PropertyIdList.Contains(0x0112)) FixOrientation(original);   // phone photos: turned as they were taken

            var scale = Math.Min(1.0, (double)MaxImageSide / Math.Max(original.Width, original.Height));
            using var resized = new Bitmap(original, Math.Max(1, (int)(original.Width * scale)), Math.Max(1, (int)(original.Height * scale)));
            var whole = Jpeg(resized);

            var strips = new List<byte[]>();
            if (original.Height > original.Width * 1.3)
            {
                // From the full photo (not the smaller one): the strips keep every pixel of the digits
                var stripScale = Math.Min(1.0, (double)MaxStripWidth / original.Width);
                var stripHeight = (int)(original.Width * 0.75);
                var step = (int)(stripHeight * (1 - StripOverlap));
                for (var top = 0; top < original.Height; top += step)
                {
                    var height = Math.Min(stripHeight, original.Height - top);
                    if (height < stripHeight * 0.3 && strips.Count > 0) break;   // a thin rest is already in the last strip
                    using var strip = new Bitmap(Math.Max(1, (int)(original.Width * stripScale)), Math.Max(1, (int)(height * stripScale)));
                    using (var g = Graphics.FromImage(strip))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(original, new Rectangle(0, 0, strip.Width, strip.Height), new Rectangle(0, top, original.Width, height), GraphicsUnit.Pixel);
                    }
                    strips.Add(Jpeg(strip));
                    if (top + height >= original.Height) break;
                }
            }
            return (whole, strips);
        }

        private static byte[] Jpeg(Image image)
        {
            using var stream = new MemoryStream();
            var jpeg = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            using var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 88L);
            image.Save(stream, jpeg, parameters);
            return stream.ToArray();
        }

        // EXIF orientation (tag 0x0112): turn the picture the way the phone was held
        private static void FixOrientation(Image image)
        {
            var flip = image.GetPropertyItem(0x0112)?.Value is { Length: > 0 } value ? value[0] switch
            {
                3 => RotateFlipType.Rotate180FlipNone,
                6 => RotateFlipType.Rotate90FlipNone,
                8 => RotateFlipType.Rotate270FlipNone,
                _ => RotateFlipType.RotateNoneFlipNone,
            } : RotateFlipType.RotateNoneFlipNone;
            if (flip == RotateFlipType.RotateNoneFlipNone) return;
            image.RotateFlip(flip);
            image.RemovePropertyItem(0x0112);
        }

        // A clear message for the usual OpenAI errors
        private static string ErrorMessage(HttpStatusCode status, string body, string model)
        {
            string? apiMessage = null;
            try { apiMessage = JsonNode.Parse(body)?["error"]?["message"]?.GetValue<string>(); } catch (JsonException) { }
            var why = status switch
            {
                HttpStatusCode.Unauthorized => "The OpenAI API key is not valid. Check \"ApiKey\" in appsettings.local.json.",
                HttpStatusCode.TooManyRequests => "OpenAI refused: too many requests or no credit left on the account (platform.openai.com > Billing).",
                HttpStatusCode.NotFound => $"The model \"{model}\" was not found. Check the \"OpenAI\" models in appsettings.json.",
                HttpStatusCode.BadRequest => "OpenAI could not use this request (file too large, or a format it does not read?).",
                _ => $"OpenAI answered with an error ({(int)status}).",
            };
            return apiMessage == null ? why : why + "\n\n" + apiMessage;
        }
    }
}
