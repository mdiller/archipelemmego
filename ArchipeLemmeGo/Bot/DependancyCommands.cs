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
            TreeRenderer.Render(drawableTree,  1400, 1000, filename);
            FileAttachment file = new FileAttachment(filename, "node_graph.png");

            await FollowupWithFileAsync(file, "Drew this graph for ya");
        }
    }
}
