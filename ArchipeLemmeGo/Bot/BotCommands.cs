using ArchipeLemmeGo.Archipelago;
using ArchipeLemmeGo.Datamodel.Infos;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;


namespace ArchipeLemmeGo.Bot
{
    public class BotCommands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("setuproom", "Link this discord channel to an archipelago room")]
        public async Task SetupRoomAsync(
            [Summary(description: "The host address of the Archipelago server.")] string host,
            [Summary(description: "The port number to connect to.")] int port)
        {
            var archCtx = ArchipelagoContext.FromCtx(Context, true);

            if (archCtx != null && !archCtx.IsUserAdmin(Context.User.Id))
            {
                await RespondAsync($"This channel has already been linked to a room.");
                return;
            }

            var roomInfo = await ArchipelagoService.RegisterRoomInfo(Context.User.Id, host, port);

            var channelLinker = ArchipelagoContext.GetChannelLinker();
            channelLinker.ChannelAssignments[Context.Channel.Id] = roomInfo.Uri;
            channelLinker.Save();

            await RespondAsync($"Room has been set up! Internal Seed: `{roomInfo.Seed}`");
        }

        [SlashCommand("register", "Register a player with the bot.")]
        public async Task RegisterAsync(
            [Summary(description: "The Archipelago slot/player name.")] string playerName,
            [Summary(description: "The Discord user to register.")] IUser? user = null)
        {
            var archCtx = ArchipelagoContext.FromCtx(Context);

            if (user != null && !archCtx.IsUserAdmin(Context.User.Id))
            {
                await RespondAsync($"Only the person who setup the room can register other people for themselves.");
                return;
            }

            if (archCtx.SlotInfo != null && user == null &&!archCtx.IsUserAdmin(Context.User.Id))
            {
                await RespondAsync($"That slot has already been linked to a user. If it was linked to the wrong user, tell the person who setup the room to fix it.");
                return;
            }
            var targetUser = user ?? Context.User;

            await ArchipelagoService.RegisterSlotInfo(targetUser.Id, archCtx.RoomInfo, playerName);

            await RespondAsync($"Registered player `{playerName}` to {targetUser.Username}");
        }

        [SlashCommand("status", "Show the current status of this room.")]
        public async Task StatusAsync()
        {
            var archCtx = ArchipelagoContext.FromCtx(Context);

            var embed = new EmbedBuilder()
                .WithTitle("Archipelago Room")
                .WithDescription($"This discord channel has been linked to an archipelago room.")
                .AddField("Registered Players", archCtx.RoomInfo.SlotInfos.Count, true)
                .AddField("Internal Seed", archCtx.RoomInfo.Seed, true)
                .AddField("Games", string.Join("\n", archCtx.RoomInfo.Games), false)
                .WithColor(Color.Blue)
                .Build();

            await RespondAsync(embed: embed);
        }

        [SlashCommand("request", "Submit a request for an item.")]
        public async Task RequestAsync(
            [Summary(description: "The item to request."), Autocomplete(typeof(ExampleAutocompleteHandler))] string item,
            [Summary(description: "The information or context for this request.")] string information,
            [Summary(description: "Priority (1 = low, 10 = high).")] int priority = 5)
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


                var result = "";
                // if we now have a hint (or list of hints), add them to the list of requests for this room?
                foreach (var hint in hints)
                {
                    var hintWrapper = new HintWrapper(hint, archCtx.RoomInfo);
                    result += $"\n - Need {hintWrapper.Item} which is from '{hintWrapper.Location}' ({hintWrapper.Finder})";
                }
                await RespondAsync(result.Trim());
            }
            finally
            {
                await client.Disconnect();
            }
        }
    }

}
