using ArchipeLemmeGo.Datamodel;
using ArchipeLemmeGo.Datamodel.Infos;

namespace ArchipeLemmeGo.Web
{
    public static class ApiEndpoints
    {
        public static void Map(WebApplication app)
        {
            var api = app.MapGroup("/api/{channelId}");

            api.MapGet("/room", GetRoom);
            api.MapGet("/waiting", GetWaiting);
            api.MapGet("/todo", GetTodo);
            api.MapGet("/items", GetItems);
            api.MapGet("/locations", GetLocations);
        }

        private static bool TryLoadRoom(string channelId, out RoomInfo? room)
        {
            room = null;

            if (!ulong.TryParse(channelId, out var channelIdParsed))
                return false;

            try
            {
                var linkerUri = InfoUri.New<ChannelLinker>("main");
                if (!linkerUri.Exists()) return false;

                var linker = InfoBase.Load<ChannelLinker>(linkerUri);
                if (!linker.ChannelAssignments.TryGetValue(channelIdParsed, out var roomUri))
                    return false;

                if (!roomUri.Exists()) return false;

                room = InfoBase.Load<RoomInfo>(roomUri);
                room.HydrateArchStuff();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void LogValidChannels(ILogger logger)
        {
            try
            {
                var linkerUri = InfoUri.New<ChannelLinker>("main");
                if (!linkerUri.Exists())
                {
                    logger.LogWarning("ChannelLinker not found at {Path}", linkerUri.ToFilePath());
                    return;
                }
                var linker = InfoBase.Load<ChannelLinker>(linkerUri);
                foreach (var kvp in linker.ChannelAssignments)
                    logger.LogInformation("  Valid channel: {ChannelId} → {Room}", kvp.Key, kvp.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load ChannelLinker for debug logging");
            }
        }

        private static IResult GetRoom(string channelId, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("ApiEndpoints");
            if (!TryLoadRoom(channelId, out var room) || room == null)
            {
                logger.LogWarning("Channel not found: {ChannelId}. Valid channels:", channelId);
                LogValidChannels(logger);
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                seed = room.Seed,
                games = room.Games,
                hintCostPercentage = room.HintCostPercentage,
                slots = room.SlotInfos.Select(s => new
                {
                    slotId = s.SlotId,
                    name = s.Name,
                    game = s.Game,
                    discordId = s.DiscordId.ToString()
                })
            });
        }

        private static IResult GetWaiting(string channelId, int? slot = null)
        {
            if (!TryLoadRoom(channelId, out var room) || room == null)
                return Results.NotFound();

            var hints = room.RequestedHints
                .Where(h => !h.IsFound)
                .Where(h => slot == null || h.RequesterSlot == slot)
                .OrderByDescending(h => h.Priority)
                .Select(h => new
                {
                    itemId = h.ItemId,
                    itemName = h.Item.Name,
                    requesterSlot = h.RequesterSlot,
                    requesterName = room.GetSlotInfo(h.RequesterSlot)?.Name ?? "Unknown",
                    finderSlot = h.FinderSlot,
                    finderName = room.GetSlotInfo(h.FinderSlot)?.Name ?? "Unknown",
                    locationId = h.LocationId,
                    locationName = h.Location.Name,
                    priority = h.Priority,
                    count = h.Count,
                    information = h.Information ?? ""
                });

            return Results.Ok(hints);
        }

        private static IResult GetTodo(string channelId, int? slot = null)
        {
            if (!TryLoadRoom(channelId, out var room) || room == null)
                return Results.NotFound();

            var hints = room.RequestedHints
                .Where(h => !h.IsFound)
                .Where(h => slot == null || h.FinderSlot == slot)
                .OrderByDescending(h => h.Priority)
                .Select(h => new
                {
                    locationId = h.LocationId,
                    locationName = h.Location.Name,
                    finderSlot = h.FinderSlot,
                    finderName = room.GetSlotInfo(h.FinderSlot)?.Name ?? "Unknown",
                    itemId = h.ItemId,
                    itemName = h.Item.Name,
                    requesterSlot = h.RequesterSlot,
                    requesterName = room.GetSlotInfo(h.RequesterSlot)?.Name ?? "Unknown",
                    priority = h.Priority,
                    information = h.Information ?? ""
                });

            return Results.Ok(hints);
        }

        private static IResult GetItems(string channelId, int? slot = null, string? q = null)
        {
            if (!TryLoadRoom(channelId, out var room) || room == null)
                return Results.NotFound();

            var slots = slot.HasValue
                ? room.SlotInfos.Where(s => s.SlotId == slot.Value)
                : (IEnumerable<SlotInfo>)room.SlotInfos;

            var items = slots
                .Where(s => s.ItemLookup != null)
                .SelectMany(s => s.ItemLookup.Select(kvp => new
                {
                    name = kvp.Key,
                    itemId = kvp.Value,
                    slotId = s.SlotId,
                    slotName = s.Name,
                    game = s.Game
                }));

            if (!string.IsNullOrWhiteSpace(q))
                items = items.Where(i => i.name.Contains(q, StringComparison.OrdinalIgnoreCase));

            return Results.Ok(items.Take(150));
        }

        private static IResult GetLocations(string channelId, int? slot = null, string? q = null)
        {
            if (!TryLoadRoom(channelId, out var room) || room == null)
                return Results.NotFound();

            var slots = slot.HasValue
                ? room.SlotInfos.Where(s => s.SlotId == slot.Value)
                : (IEnumerable<SlotInfo>)room.SlotInfos;

            var locations = slots
                .Where(s => s.LocationLookup != null)
                .SelectMany(s => s.LocationLookup.Select(kvp => new
                {
                    name = kvp.Key,
                    locationId = kvp.Value,
                    slotId = s.SlotId,
                    slotName = s.Name,
                    game = s.Game
                }));

            if (!string.IsNullOrWhiteSpace(q))
                locations = locations.Where(l => l.name.Contains(q, StringComparison.OrdinalIgnoreCase));

            return Results.Ok(locations.Take(150));
        }
    }
}
