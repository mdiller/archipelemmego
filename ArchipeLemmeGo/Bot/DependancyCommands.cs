using ArchipeLemmeGo.Archipelago;
using ArchipeLemmeGo.Bot.AutocompleteHandlers;
using ArchipeLemmeGo.Datamodel.Arch;
using ArchipeLemmeGo.Datamodel.Infos;
using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static TreeRenderer;

namespace ArchipeLemmeGo.Bot
{
    [Group("dep", "Commands related to archipelago dependancies")]
    public class DependancyCommands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("add", "Add a new dependancy between a location and an item")]
        public async Task AddAsync(
            [Summary(description: "The location you're unable to get to."), Autocomplete(typeof(LocationAutocompleteHandler))] string location_name,
            [Summary(description: "The item you need in order to get to the location."), Autocomplete(typeof(ItemAutocompleteHandler))] string item_name,
            [Summary(description: "The progression level needed for this item")] int progressionLevel = 0)
        {
            await DeferAsync();
            var archCtx = ArchipelagoContext.FromCtx(Context, requireRegistered: true);
            var location = ArchLocation.FromDiscString(location_name, archCtx.RoomInfo);
            var item = ArchItem.FromDiscString(item_name, archCtx.RoomInfo);

            item.ProgressionLevel = progressionLevel;
            var dep = new DependancyLink
            {
                Dependant = location,
                Prerequisites = new List<ArchItem> { item }
            };


            archCtx.RoomInfo.Dependancies.Add(dep);
            archCtx.RoomInfo.Save();

            await FollowupAsync($"{location.Name} => {item.Name}");
        }

        [SlashCommand("addregex", "Add a new dependancy between a set of locations and an item")]
        public async Task AddRegexAsync(
            [Summary(description: "The regex to match the location(s) that this unlocks.")] string location_regex,
            [Summary(description: "The item you need in order to get to the location."), Autocomplete(typeof(ItemAutocompleteHandler))] string item_name,
            [Summary(description: "The progression level needed for this item")] int progressionLevel = 0)
        {
            await DeferAsync();
            var archCtx = ArchipelagoContext.FromCtx(Context, requireRegistered: true);

            var pattern = new Regex(location_regex);
            var locations = archCtx.SlotInfo.LocationLookup
                .Where(kv => pattern.IsMatch(kv.Key))
                .Select(kv => new ArchLocation
                {
                    Slot = archCtx.SlotInfo.SlotId,
                    LocationId = kv.Value,
                    RoomInfo = archCtx.RoomInfo
                })
                .ToList();


            var item = ArchItem.FromDiscString(item_name, archCtx.RoomInfo);

            item.ProgressionLevel = progressionLevel;

            var results = new List<string>();

            locations
                .Select(loc => new DependancyLink
                {
                    Dependant = loc,
                    Prerequisites = new List<ArchItem> { item }
                }).ToList()
                .ForEach(dep =>
                {
                    archCtx.RoomInfo.Dependancies.Add(dep);
                    results.Add($"{dep.Dependant.Name} => {item.Name}");
                });

            var cutoff = 10;
            if (results.Count > cutoff)
            {
                var removed = results.Count - cutoff;
                results.RemoveRange(cutoff, removed);
                results.Add($"... (and {removed} more)...");
            }

            archCtx.RoomInfo.Save();

            await FollowupAsync(string.Join('\n', results));
        }

        [SlashCommand("show", "Shows the dependancy tree for a given item")]
        public async Task ShowAsync(
            [Summary(description: "The item we're checking on."), Autocomplete(typeof(ItemAutocompleteHandler))] string item_name,
            [Summary(description: "The progression level needed for this item")] int progressionLevel = 0)
        {
            await DeferAsync();
            var archCtx = ArchipelagoContext.FromCtx(Context, requireRegistered: true);
            var item = ArchItem.FromDiscString(item_name, archCtx.RoomInfo);
            item.ProgressionLevel = progressionLevel;

            var depTree = new DependancyTree(item);

            var filename = "my_tree.png";
            var drawableTree = TreeRenderer.FromDependancyTree(depTree);
            TreeRenderer.RenderAuto(drawableTree, filename);
            FileAttachment file = new FileAttachment(filename, "node_graph.png");

            await FollowupWithFileAsync(file, "Drew this graph for ya");
        }
    }
}
