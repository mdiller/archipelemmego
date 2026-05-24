using ArchipeLemmeGo.Datamodel;
using ArchipeLemmeGo.Datamodel.Infos;
using ArchipeLemmeGo.IconMatching;

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
            api.MapGet("/deps", GetDeps);
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

        private static IResult GetWaiting(string channelId, IconAssignmentService icons, int? slot = null)
        {
            if (!TryLoadRoom(channelId, out var room) || room == null)
                return Results.NotFound();

            var hints = room.RequestedHints
                .Where(h => !h.IsFound)
                .Where(h => slot == null || h.RequesterSlot == slot)
                .OrderByDescending(h => h.Priority)
                .Select(h =>
                {
                    var itemGame = room.GetSlotInfo(h.Item.Slot)?.Game ?? "";
                    var locGame = room.GetSlotInfo(h.Location.Slot)?.Game ?? "";
                    return new
                    {
                        itemId = h.ItemId,
                        itemName = h.Item.Name,
                        itemIcon = icons.GetIcon(itemGame, h.Item.Name, isLocation: false),
                        requesterSlot = h.RequesterSlot,
                        requesterName = room.GetSlotInfo(h.RequesterSlot)?.Name ?? "Unknown",
                        finderSlot = h.FinderSlot,
                        finderName = room.GetSlotInfo(h.FinderSlot)?.Name ?? "Unknown",
                        locationId = h.LocationId,
                        locationName = h.Location.Name,
                        locationIcon = icons.GetIcon(locGame, h.Location.Name, isLocation: true),
                        priority = h.Priority,
                        count = h.Count,
                        information = h.Information ?? ""
                    };
                });

            return Results.Ok(hints);
        }

        private static IResult GetTodo(string channelId, IconAssignmentService icons, int? slot = null)
        {
            if (!TryLoadRoom(channelId, out var room) || room == null)
                return Results.NotFound();

            var hints = room.RequestedHints
                .Where(h => !h.IsFound)
                .Where(h => slot == null || h.FinderSlot == slot)
                .OrderByDescending(h => h.Priority)
                .Select(h =>
                {
                    var itemGame = room.GetSlotInfo(h.Item.Slot)?.Game ?? "";
                    var locGame = room.GetSlotInfo(h.Location.Slot)?.Game ?? "";
                    return new
                    {
                        locationId = h.LocationId,
                        locationName = h.Location.Name,
                        locationIcon = icons.GetIcon(locGame, h.Location.Name, isLocation: true),
                        finderSlot = h.FinderSlot,
                        finderName = room.GetSlotInfo(h.FinderSlot)?.Name ?? "Unknown",
                        itemId = h.ItemId,
                        itemName = h.Item.Name,
                        itemIcon = icons.GetIcon(itemGame, h.Item.Name, isLocation: false),
                        requesterSlot = h.RequesterSlot,
                        requesterName = room.GetSlotInfo(h.RequesterSlot)?.Name ?? "Unknown",
                        priority = h.Priority,
                        information = h.Information ?? ""
                    };
                });

            return Results.Ok(hints);
        }

        private static IResult GetDeps(string channelId, IconAssignmentService icons)
        {
            if (!TryLoadRoom(channelId, out var room) || room == null)
                return Results.NotFound();

            var nodeSet = new Dictionary<string, object>();
            var edges = new List<object>();

            foreach (var dep in room.Dependancies)
            {
                var locKey = $"loc-{dep.Dependant.Slot}-{dep.Dependant.LocationId}";
                if (!nodeSet.ContainsKey(locKey))
                {
                    var locGame = room.GetSlotInfo(dep.Dependant.Slot)?.Game ?? "";
                    nodeSet[locKey] = new
                    {
                        id = locKey,
                        type = "location",
                        name = dep.Dependant.Name,
                        iconName = icons.GetIcon(locGame, dep.Dependant.Name, isLocation: true),
                        slot = dep.Dependant.Slot,
                        slotName = room.GetSlotInfo(dep.Dependant.Slot)?.Name ?? $"Slot #{dep.Dependant.Slot}"
                    };
                }

                foreach (var prereq in dep.Prerequisites)
                {
                    var itemKey = $"item-{prereq.Slot}-{prereq.ItemId}";
                    if (!nodeSet.ContainsKey(itemKey))
                    {
                        var itemGame = room.GetSlotInfo(prereq.Slot)?.Game ?? "";
                        nodeSet[itemKey] = new
                        {
                            id = itemKey,
                            type = "item",
                            name = prereq.Name,
                            iconName = icons.GetIcon(itemGame, prereq.Name, isLocation: false),
                            slot = prereq.Slot,
                            slotName = room.GetSlotInfo(prereq.Slot)?.Name ?? $"Slot #{prereq.Slot}"
                        };
                    }

                    edges.Add(new { source = itemKey, target = locKey });
                }
            }

            return Results.Ok(new { nodes = nodeSet.Values, edges });
        }

        private static IResult GetItems(string channelId, IconAssignmentService icons, int? slot = null, string? q = null)
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
                    iconName = icons.GetIcon(s.Game, kvp.Key, isLocation: false),
                    itemId = kvp.Value,
                    slotId = s.SlotId,
                    slotName = s.Name,
                    game = s.Game
                }));

            if (!string.IsNullOrWhiteSpace(q))
                items = items.Where(i => i.name.Contains(q, StringComparison.OrdinalIgnoreCase));

            return Results.Ok(items.Take(150));
        }

        private static IResult GetLocations(string channelId, IconAssignmentService icons, int? slot = null, string? q = null)
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
                    iconName = icons.GetIcon(s.Game, kvp.Key, isLocation: true),
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
