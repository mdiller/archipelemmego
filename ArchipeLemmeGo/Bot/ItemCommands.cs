using Archipelago.MultiClient.Net.Models;
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

namespace ArchipeLemmeGo.Bot
{


    [Group("item", "Commands related to archipelago items")]
    public class ItemCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public async Task SendPossiblyLargeMessage(string message)
        {
            var charLimit = 1990;
            if (message.Length <= charLimit)
            {
                await FollowupAsync(message);
                return;
            }
            else
            {
                var parts = new List<string>();
                var currentPart = new StringBuilder();
                foreach (var line in message.Split('\n'))
                {
                    if (currentPart.Length + line.Length + 1 > charLimit)
                    {
                        currentPart.AppendLine("...");
                        parts.Add(currentPart.ToString());
                        currentPart.Clear();
                        currentPart.AppendLine("...");
                    }
                    currentPart.AppendLine(line);
                }
                if (currentPart.Length > 0)
                {
                    parts.Add(currentPart.ToString());
                }
                foreach (var part in parts)
                {
                    await FollowupAsync(part);
                }   
            }

        }

        [SlashCommand("request", "Submit a request for an item.")]
        public async Task RequestAsync(
            [Summary(description: "The item to request."), Autocomplete(typeof(ItemAutocompleteHandler))] string item_name,
            [Summary(description: "Priority (1 = low, 10 = high).")] int priority,
            [Summary(description: "The information or context for this request.")] string information = "",
            [Summary(description: "How many of this item are needed (defaults to all)")] int count = -1)
        {
            var archCtx = ArchipelagoContext.FromCtx(Context, requireRegistered: true);
            var item = ArchItem.FromDiscString(item_name, archCtx.RoomInfo);

            await DeferAsync();

            if (!archCtx.SlotInfo.ItemLookup.ContainsValue(item.ItemId))
            {
                await FollowupAsync($"Couldn't find an item with that name.");
                return;
            }

            var itemId = item.ItemId;

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

                    await Task.Delay(2000); // lets wait a full 2 seconds for hints to come in for this

                    hints = (await client.GetHints())
                        .Where(h => h.ItemId == itemId)
                        .ToList();
                }

                // If there still isnt, give up
                if (!hints.Any())
                {
                    await FollowupAsync($"I wasn't able to get the hint for that item. ¯\\_(ツ)_/¯. Maybe open the lua console and see if u can find it there or what it shows when you run this command? idk what goin wrong here bro");
                    return;
                }

                if (count == -1)
                {
                    count = hints.Count;
                }

                var hintInfos = hints.Select(h => RequestedHintInfo.Create(h, information, priority, count, archCtx.RoomInfo)).ToList();

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

                var result = $"__Requested: {count} `{hintInfos.First().Item.Name}` [priority={priority}]__";

                if (isFinished)
                {
                    result += $"Already have found {foundCount}/{count} of them! To request more, set count to a higher number when calling this command.";
                }
                else
                {
                    if (information != "")
                    {
                        result += $"\n**Information:** {information}";
                    }
                    result += $"\n**Locations:** (need {count - foundCount} of these)";
                    // if we now have a hint (or list of hints), add them to the list of requests for this room?
                    foreach (var hintInfo in hintInfos.Where(h => !h.IsFound))
                    {
                        result += $"\n • '{hintInfo.Location}' from {hintInfo.Location.Player.Mention}";
                    }
                }

                await FollowupAsync(result.Trim());
            }
            finally
            {
                await client.Disconnect();
            }
        }


        [SlashCommand("unrequest", "Remove an item request.")]
        public async Task UnRequestAsync(
            [Summary(description: "The item to remove."), Autocomplete(typeof(ItemAutocompleteHandler))] string existing_item)
        {
            await DeferAsync();
            var archCtx = ArchipelagoContext.FromCtx(Context, requireRegistered: true);
            var item = ArchItem.FromDiscString(existing_item, archCtx.RoomInfo);

            if (!archCtx.SlotInfo.ItemLookup.ContainsValue(item.ItemId))
            {
                await FollowupAsync($"Couldn't find this item in the list of items that you have requested.");
                return;
            }

            var item_id = item.ItemId;
            
            var roomInfo = archCtx.RoomInfo;
            var slotInfo = archCtx.SlotInfo;

            roomInfo.RequestedHints
                .RemoveAll(h => h.RequesterSlot == slotInfo.SlotId && h.ItemId == item_id && !h.IsFound);
            roomInfo.Save();

            await FollowupAsync($"Done!");
        }

        [SlashCommand("waiting", "List all the items are waiting on")]
        public async Task ItemsWaiting()
        {
            await DeferAsync();
            var archCtx = ArchipelagoContext.FromCtx(Context, requireRegistered: true);
            string announceText;
            try
            {
                announceText = await ArchipelagoClient.DoResync(archCtx);
            }
            catch (UserError e)
            {
                announceText = "*Errored while connecting to the archipelago room, so this may be out of date*";
            }

            var hintInfos = archCtx.RoomInfo.RequestedHints
                .Where(h => h.RequesterSlot == archCtx.SlotInfo.SlotId && !h.IsFound)
                .OrderBy(h => h.ItemId)
                .ToList();

            var result = announceText + "\n\n__Now Waiting For:__";
            long itemId = -1;

            foreach (var hintInfo in hintInfos)
            {
                if (itemId == -1 || itemId != hintInfo.Item.ItemId)
                {
                    result += $"\n`{hintInfo.Item}`:";
                    result += $" [prio={hintInfo.Priority}] {hintInfo.Information}";
                    itemId = hintInfo.Item.ItemId;
                }
                result += $"\n • '{hintInfo.Location}' ({hintInfo.Location.Player.Name})";
            }


            await SendPossiblyLargeMessage(result.Trim());
        }

        [SlashCommand("todo", "List all of the locations that you've been requested to do")]
        public async Task ItemsTodo()
        {
            await DeferAsync();
            var archCtx = ArchipelagoContext.FromCtx(Context, requireRegistered: true);

            string announceText;
            try
            {
                announceText = await ArchipelagoClient.DoResync(archCtx);
            }
            catch (UserError e)
            {
                announceText = "*Errored while connecting to the archipelago room, so this may be out of date*";
            }

            var hintInfos = archCtx.RoomInfo.RequestedHints
                .Where(h => h.FinderSlot == archCtx.SlotInfo.SlotId && !h.IsFound)
                .OrderBy(h => h.Priority)
                .Reverse()
                .ToList();

            var result = announceText + "\n\n__Things to do:__";
            long itemId = -1;

            foreach (var hintInfo in hintInfos)
            {
                if (itemId == -1 || itemId != hintInfo.Item.ItemId)
                {
                    result += $"\nFor `{hintInfo.Item}` ({hintInfo.Item.Player.Name}):";
                    result += $" [prio={hintInfo.Priority}] {hintInfo.Information}";
                    itemId = hintInfo.Item.ItemId;
                }
                result += $"\n • **{hintInfo.Location}**";
            }

            await SendPossiblyLargeMessage(result.Trim());
        }
    }
}
