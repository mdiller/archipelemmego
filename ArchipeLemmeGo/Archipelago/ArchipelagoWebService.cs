using ArchipeLemmeGo.Datamodel.Infos;
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

        /// <summary>
        /// Reconnect behavior: fetches a fresh port and retries connecting up to 5 times.
        /// Saves the new port on success. Throws UserError after all attempts fail.
        /// </summary>
        public static async Task<int> ReconnectAsync(RoomInfo roomInfo, SlotInfo firstSlot)
        {
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    var port = await FetchPortAsync(roomInfo.RoomId);
                    roomInfo.Port = port;

                    var client = new ArchipelagoClient(roomInfo, firstSlot);
                    try
                    {
                        await client.ConnectAsync();
                        roomInfo.Save();
                        return port;
                    }
                    catch { }
                    finally
                    {
                        await client.Disconnect();
                    }
                }
                catch { }

                if (attempt < 5)
                    await Task.Delay(30_000);
            }

            throw new UserError("Failed to reconnect after 5 attempts. The room may not be active yet — try again later.");
        }
    }
}
