using System.Text.Json;

namespace ArchipeLemmeGo.Bot
{
    public static class BotInfo
    {
        private static readonly Lazy<JsonElement> _config = new(() =>
        {
            // Check next to the binary first (Docker), then one level up (dev repo layout)
            var sameDir = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            var parentDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "config.json");
            var path = File.Exists(sameDir) ? sameDir : parentDir;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<JsonElement>(json);
        });

        public static string BotToken => _config.Value.GetProperty("BotToken").GetString()!;
        public static ulong BotOwner => _config.Value.GetProperty("BotOwner").GetUInt64();
        public static ulong TestGuildId => _config.Value.GetProperty("TestGuildId").GetUInt64();
        public static string DiscordClientId => _config.Value.GetProperty("DiscordClientId").GetString()!;
        public static string DiscordClientSecret => _config.Value.GetProperty("DiscordClientSecret").GetString()!;

        // Optional. Set in config.json to prevent Host-header injection in OAuth callback URI.
        // Example: "https://yourdomain.com"
        public static string? BaseUrl =>
            _config.Value.TryGetProperty("BaseUrl", out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString()
                : null;
    }
}
