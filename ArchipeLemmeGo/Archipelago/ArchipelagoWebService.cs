using System.Net.Http;
using System.Text.RegularExpressions;

namespace ArchipeLemmeGo.Archipelago
{
    public static class ArchipelagoWebService
    {
        private static readonly HttpClient _http = new HttpClient();

        public static string ExtractRoomId(string url)
        {
            var match = Regex.Match(url, @"archipelago\.gg/room/([A-Za-z0-9_-]+)");
            if (!match.Success)
                throw new UserError("Invalid Archipelago room URL. Expected format: https://archipelago.gg/room/...");
            return match.Groups[1].Value;
        }

        public static async Task<int> FetchPortAsync(string roomId)
        {
            var html = await _http.GetStringAsync($"https://archipelago.gg/room/{roomId}");
            var match = Regex.Match(html, @"/connect archipelago\.gg:(\d+)");
            if (!match.Success)
                throw new UserError("Could not find a port on the room page. The room may not be active yet.");
            return int.Parse(match.Groups[1].Value);
        }
    }
}
