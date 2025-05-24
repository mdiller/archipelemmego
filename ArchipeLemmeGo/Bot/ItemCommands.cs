using ArchipeLemmeGo.Archipelago;
using ArchipeLemmeGo.Datamodel.Infos;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Bot
{
    [Group("item", "Commands related to archipelago items")]
    public class ItemCommands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("request", "Submit a request for an item.")]
        public async Task RequestAsync(
            [Summary(description: "The item to request."), Autocomplete(typeof(ExampleAutocompleteHandler))] string item,
            [Summary(description: "The information or context for this request.")] string information,
            [Summary(description: "Priority (1 = low, 10 = high).")] int priority = 5,
            [Summary(description: "How many of this item are needed")] int count = 1)
        {
            var archCtx = ArchipelagoContext.FromCtx(Context, requireRegistered: true);

            if (!archCtx.SlotInfo.ItemLookup.ContainsKey(item))
            {
                await RespondAsync($"Couldn't find an item with that name.");
                return;
            }
            //await DeferAsync();

            var itemId = archCtx.SlotInfo.ItemLookup[item];

            // create a client
            var client = new ArchipelagoClient(archCtx.RoomInfo, archCtx.SlotInfo);

            try
            {
                // connect
                await client.ConnectAsync();

                // check if there is an existing/known hint for this item
                var hints = (await client.GetHints())
                    .Where(h => h.ItemId == itemId)
                    .ToList();


                // - if there isn't, request one
                if (!hints.Any())
                {
                    await client.Say($"!hint {item}");

                    await Task.Delay(1000);

                    hints = (await client.GetHints())
                        .Where(h => h.ItemId == itemId)
                        .ToList();
                }

                // If there still isnt, give up
                if (!hints.Any())
                {
                    await RespondAsync($"I wasn't able to get the hint for that item. ¯\\_(ツ)_/¯.");
                    return;
                }

                var hintInfos = hints.Select(h => RequestedHintInfo.Create(h, information, priority, count)).ToList();

                var result = hintInfos.Count <= 1 ? "Added this request" : "Added the following requests";
                result = $"__{result}:__";
                // if we now have a hint (or list of hints), add them to the list of requests for this room?
                foreach (var hint in hintInfos)
                {
                    var hintWrapper = hint.ToHintWrapper(archCtx.RoomInfo);
                    result += $"\n - Need {hintWrapper.Item} which is from '{hintWrapper.Location}' ({hintWrapper.Finder})";
                }

                // TODO: check before adding:
                // - do these already exist? (if so update them with the new inputs)
                // - have we already found all of them? (if so, tell the user and remove any existing ones from the list)

                // TODO: add them to the list, then save

                await RespondAsync(result.Trim());
            }
            finally
            {
                await client.Disconnect();
            }
        }

        [SlashCommand("list", "List all the items you have requested and all the requests you have ")]
        public async Task ListItems()
        {
            await RespondAsync("Here are the items...");
        }
    }
}
