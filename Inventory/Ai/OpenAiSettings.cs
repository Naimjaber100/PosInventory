using System.Text.Json;

namespace Inventory.Ai
{
    // "OpenAI" section of appsettings.json. The key can also come from the OPENAI_API_KEY environment variable
    // (better on a shared computer: it then is not in a file next to the program).
    public sealed class OpenAiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4o";

        /// <summary>Speech to text for voice notes ("whisper-1" also works).</summary>
        public string TranscriptionModel { get; set; } = "gpt-4o-transcribe";

        /// <summary>Language of the voice notes ("en", "ar", "fr"); empty = found by itself (best when languages are mixed).</summary>
        public string? TranscriptionLanguage { get; set; }
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public int TimeoutSeconds { get; set; } = 120;

        /// <summary>The key from appsettings.json, else from the OPENAI_API_KEY environment variable.</summary>
        public string? EffectiveApiKey =>
            !string.IsNullOrWhiteSpace(ApiKey) && !ApiKey.StartsWith("PUT-", StringComparison.Ordinal) ? ApiKey.Trim()
            : Environment.GetEnvironmentVariable("OPENAI_API_KEY") is { Length: > 0 } env ? env.Trim()
            : null;

        public static OpenAiSettings Read(JsonElement root)
        {
            if (!root.TryGetProperty("OpenAI", out var section)) return new OpenAiSettings();
            try
            {
                return section.Deserialize<OpenAiSettings>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new OpenAiSettings();
            }
            catch (JsonException)
            {
                return new OpenAiSettings();
            }
        }
    }
}
