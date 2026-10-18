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
        [JsonPropertyName("quantity")] public decimal Quantity { get; set; }
        [JsonPropertyName("unit_price")] public decimal? UnitPrice { get; set; }
        [JsonPropertyName("line_total")] public decimal? LineTotal { get; set; }
        [JsonPropertyName("barcode")] public string? Barcode { get; set; }
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
            - quantity: the number of packagings bought.
            - unit_price: price of ONE packaging; null if not written. line_total: the line amount; null if not written.
            - barcode: only if a barcode / EAN is printed on that line; else null.
            Numbers: plain numbers without thousands separators (12500, not 12,500). If only line_total is written, still give it.
            Also give, when visible: supplier_name, invoice_number, invoice_date (YYYY-MM-DD), currency (ISO code: USD for $, LBP for L.L / ل.ل),
            discount (amount) and total. Use null for anything you cannot read; never guess numbers.
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
                    "required": ["text", "item_name", "item_name_alt", "packaging", "quantity", "unit_price", "line_total", "barcode"],
                    "properties": {
                      "text":       { "type": "string" },
                      "item_name":  { "type": "string" },
                      "item_name_alt": { "type": ["string", "null"] },
                      "packaging":  { "type": ["string", "null"] },
                      "quantity":   { "type": "number" },
                      "unit_price": { "type": ["number", "null"] },
                      "line_total": { "type": ["number", "null"] },
                      "barcode":    { "type": ["string", "null"] }
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
            - quantity: the number of packagings; 1 when no number was said.
            - unit_price / line_total: only when a price was said ("at 9 dollars each" -> unit_price 9); else null.
            - barcode: null.
            supplier_name, invoice_number, invoice_date, currency (USD when dollars were said, LBP for lira / ليرة),
            discount and total: only when said; else null. Never guess numbers.
            """;

        /// <summary>Reads a photo of a supplier's invoice.</summary>
        public static Task<AiInvoice> ReadAsync(string imagePath, CancellationToken cancel = default)
        {
            var image = Convert.ToBase64String(PrepareImage(imagePath));
            var user = new JsonArray
            {
                new JsonObject { ["type"] = "text", ["text"] = "Read this purchase invoice." },
                new JsonObject
                {
                    ["type"] = "image_url",
                    ["image_url"] = new JsonObject { ["url"] = "data:image/jpeg;base64," + image, ["detail"] = "high" },
                },
            };
            return ExtractAsync(Instructions, user, "this image", cancel);
        }

        /// <summary>Turns the text of a voice note ("2 pepsi box, 1 unica pack...") into purchase lines.</summary>
        public static Task<AiInvoice> ReadSpokenTextAsync(string text, CancellationToken cancel = default) =>
            ExtractAsync(SpokenInstructions, "Voice note:\n" + text.Trim(), "this voice note", cancel);

        /// <summary>
        /// Writes out a voice note (wav, mp3, m4a, ogg - e.g. WhatsApp -, webm...): OpenAI speech to text,
        /// model "TranscriptionModel" in appsettings.json. Returns the text.
        /// </summary>
        public static async Task<string> TranscribeAsync(string audioPath, CancellationToken cancel = default)
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
            form.Add(new StringContent("A shop owner lists items bought for a purchase invoice: quantities, products and packaging " +
                                       "(box, pack, carton, bottle, kg), sometimes prices. English, Arabic or French."), "prompt");
            if (!string.IsNullOrWhiteSpace(settings.TranscriptionLanguage)) form.Add(new StringContent(settings.TranscriptionLanguage.Trim()), "language");

            var text = await SendAsync("/audio/transcriptions", form, settings.TranscriptionModel, cancel);
            var transcript = JsonNode.Parse(text)?["text"]?.GetValue<string>();
            return string.IsNullOrWhiteSpace(transcript)
                ? throw new InvalidOperationException("Nothing could be heard in this voice note. Record again, closer to the microphone.")
                : transcript.Trim();
        }

        // Chat completion with the strict JSON schema: the answer is always an AiInvoice
        private static async Task<AiInvoice> ExtractAsync(string instructions, JsonNode userContent, string what, CancellationToken cancel)
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
                    ["json_schema"] = new JsonObject { ["name"] = "purchase_invoice", ["strict"] = true, ["schema"] = Schema.DeepClone() },
                },
            };

            var text = await SendAsync("/chat/completions", new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"), settings.Model, cancel);
            var reply = JsonNode.Parse(text)?["choices"]?[0]?["message"];
            if (reply?["refusal"]?.GetValue<string>() is { Length: > 0 } refusal)
                throw new InvalidOperationException($"The AI could not read {what}: " + refusal);
            var json = reply?["content"]?.GetValue<string>()
                       ?? throw new InvalidOperationException($"The AI gave an empty answer for {what}. Try again.");
            return JsonSerializer.Deserialize<AiInvoice>(json)
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

        // The photo as a JPEG at most MaxImageSide pixels on its longest side (phone photos are often 4000+ px)
        private static byte[] PrepareImage(string path)
        {
            using var original = Image.FromFile(path);
            var scale = Math.Min(1.0, (double)MaxImageSide / Math.Max(original.Width, original.Height));
            using var resized = new Bitmap(original, Math.Max(1, (int)(original.Width * scale)), Math.Max(1, (int)(original.Height * scale)));
            using var stream = new MemoryStream();
            var jpeg = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            using var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L);
            resized.Save(stream, jpeg, parameters);
            return stream.ToArray();
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
