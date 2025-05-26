using Archipelago.MultiClient.Net.Models;
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
                var hintWrappers = hintInfos.Select(h => h.ToHintWrapper(archCtx.RoomInfo)).ToList();

                // Update with new found info
                RequestedHintInfo.UpdateHintInfos(hintInfos, hints);

                var isUpdate = false;
                var isFinished = false;

                if (archCtx.RoomInfo.RequestedHints.Any(h => h.SameItem(hintInfos.First())))
                { // already have a hint for this item requested
                    isUpdate = true;
                    archCtx.RoomInfo.RequestedHints.RemoveAll(h => h.SameItem(hintInfos.First()));
                }

                var foundCount = hintInfos.Count(h => h.IsFound);
                if (foundCount >= count)
                {
                    isFinished = true;
                    hintInfos.RemoveAll(h => !h.IsFound);
                }

                archCtx.RoomInfo.RequestedHints.AddRange(hintInfos);
                archCtx.RoomInfo.Save();

                var result = $"__Requested: {count} {hintWrappers.First().Item} [priority={priority}]__";

                if (isFinished)
                {
                    result += $"Already have found {foundCount}/{count} of them! To request more, set count to a higher number when calling this command.";
                }
                else
                {
                    result += $"\n**Information:** {information}";
                    result += $"\n**Locations:** (need {count - foundCount} of these)";
                    // if we now have a hint (or list of hints), add them to the list of requests for this room?
                    foreach (var hintWrapper in hintWrappers.Where(h => !h.HintInfo.IsFound))
                    {
                        result += $"\n • '{hintWrapper.Location}' ({hintWrapper.FinderMention})";
                    }
                }

                await RespondAsync(result.Trim());
            }
            finally
            {
                await client.Disconnect();
            }
        }

        [SlashCommand("waiting", "List all the items are waiting on")]
        public async Task ItemsWaiting()
        {
            var archCtx = ArchipelagoContext.FromCtx(Context, requireRegistered: true);

            var hintInfos = archCtx.RoomInfo.RequestedHints
                .Where(h => h.RequesterSlot == archCtx.SlotInfo.SlotId && !h.IsFound)
                .OrderBy(h => h.ItemId)
                .ToList();
            var hintWrappers = hintInfos.Select(h => h.ToHintWrapper(archCtx.RoomInfo)).ToList();

            var result = "__Locations to get:__";
            long itemId = -1;

            foreach (var hintWrapper in hintWrappers)
            {
                if (itemId == -1 || itemId != hintWrapper.HintInfo.ItemId)
                {
                    result += $"\n**{hintWrapper.Item}:**";
                    result += $" [prio={hintWrapper.HintInfo.Priority}] '{hintWrapper.HintInfo.Information}'";
                    itemId = hintWrapper.HintInfo.ItemId;
                }
                result += $"\n • '{hintWrapper.Location}' ({hintWrapper.FinderName})";
            }

            await RespondAsync(result.Trim());
        }

        [SlashCommand("todo", "List all of the locations that you've been requested to do")]
        public async Task ItemsTodo()
        {
            var archCtx = ArchipelagoContext.FromCtx(Context, requireRegistered: true);

            var hintInfos = archCtx.RoomInfo.RequestedHints
                .Where(h => h.FinderSlot == archCtx.SlotInfo.SlotId && !h.IsFound)
                .OrderBy(h => h.ItemId)
                .ToList();
            var hintWrappers = hintInfos.Select(h => h.ToHintWrapper(archCtx.RoomInfo)).ToList();

            var result = "__Waiting For:__";
            long itemId = -1;

            foreach (var hintWrapper in hintWrappers)
            {
                if (itemId == -1 || itemId != hintWrapper.HintInfo.ItemId)
                {
                    result += $"\nFor {hintWrapper.Item} ({hintWrapper.RecieverName}):";
                    result += $" [prio={hintWrapper.HintInfo.Priority}] '{hintWrapper.HintInfo.Information}'";
                    itemId = hintWrapper.HintInfo.ItemId;
                }
                result += $"\n • **{hintWrapper.Location}**";
            }

            await RespondAsync(result.Trim());
        }
    }
}
