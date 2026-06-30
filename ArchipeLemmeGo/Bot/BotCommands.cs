using ArchipeLemmeGo.Archipelago;
using ArchipeLemmeGo.Datamodel.Infos;
using ArchipeLemmeGo;
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

            await DeferAsync();

            if (archCtx != null && !archCtx.IsUserAdmin(Context.User.Id))
            {
                await FollowupAsync($"This channel has already been linked to a room.");
                return;
            }

            await LinkRoomToChannel(host, port, roomId: null);
        }

        [SlashCommand("setup", "Link this channel to an Archipelago room using a room URL")]
        public async Task SetupAsync(
            [Summary(description: "The Archipelago room URL (e.g. https://archipelago.gg/room/...)")] string url)
        {
            var archCtx = ArchipelagoContext.FromCtx(Context, true);

            await DeferAsync();

            if (archCtx != null && !archCtx.IsUserAdmin(Context.User.Id))
            {
                await FollowupAsync("This channel has already been linked to a room.");
                return;
            }

            var roomId = ArchipelagoWebService.ExtractRoomId(url);
            var port = await ArchipelagoWebService.FetchPortAsync(roomId);

            await LinkRoomToChannel("archipelago.gg", port, roomId);
        }

        private async Task LinkRoomToChannel(string host, int port, string? roomId)
        {
            var roomInfo = await ArchipelagoService.RegisterRoomInfo(Context.User.Id, host, port);
            roomInfo.RoomId = roomId;
            roomInfo.GuildId = Context.Guild?.Id ?? 0;
            roomInfo.Save();

            var channelLinker = ArchipelagoContext.GetChannelLinker();
            channelLinker.ChannelAssignments[Context.Channel.Id] = roomInfo.Uri;
            channelLinker.Save();

            await FollowupAsync($"Room has been set up! Internal Seed: `{roomInfo.Seed}`");
        }

        [SlashCommand("updateport", "Fetch the latest port for this channel's Archipelago room")]
        public async Task UpdatePortAsync(
            [Summary(description: "If true, verify the port works by attempting to connect, retrying up to 5 times.")] bool try_connect = false)
        {
            var archCtx = ArchipelagoContext.FromCtx(Context);

            await DeferAsync();

            if (string.IsNullOrEmpty(archCtx.RoomInfo.RoomId))
                throw new UserError("This room wasn't set up with a URL. Use `/setup` to re-link it.");

            if (!try_connect)
            {
                var port = await ArchipelagoWebService.FetchPortAsync(archCtx.RoomInfo.RoomId);
                archCtx.RoomInfo.Port = port;
                archCtx.RoomInfo.Save();
                await FollowupAsync($"New Port: `{port}`");
                return;
            }

            var firstSlot = archCtx.RoomInfo.SlotInfos.FirstOrDefault();
            if (firstSlot == null)
                throw new UserError("No registered slots in this room. Register a player first.");

            if (await TryConnectAsync(archCtx.RoomInfo, firstSlot))
            {
                await FollowupAsync($"Already connected on current port `{archCtx.RoomInfo.Port}`.");
                return;
            }

            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    var fetchedPort = await ArchipelagoWebService.FetchPortAsync(archCtx.RoomInfo.RoomId);
                    archCtx.RoomInfo.Port = fetchedPort;

                    if (await TryConnectAsync(archCtx.RoomInfo, firstSlot))
                    {
                        archCtx.RoomInfo.Save();
                        await FollowupAsync($"Connected on port `{fetchedPort}` (attempt {attempt}/5).");
                        return;
                    }
                }
                catch { }

                if (attempt < 5)
                    await Task.Delay(30_000);
            }

            await FollowupAsync("Failed to connect after 5 attempts. The room may not be active yet — try again later.");
        }

        [SlashCommand("reconnect", "Fetch the latest port and verify the connection, retrying up to 5 times")]
        public async Task ReconnectAsync()
        {
            var archCtx = ArchipelagoContext.FromCtx(Context);

            await DeferAsync();

            if (string.IsNullOrEmpty(archCtx.RoomInfo.RoomId))
                throw new UserError("This room wasn't set up with a URL. Use `/setup` to re-link it.");

            var firstSlot = archCtx.RoomInfo.SlotInfos.FirstOrDefault();
            if (firstSlot == null)
                throw new UserError("No registered slots in this room. Register a player first.");

            if (await TryConnectAsync(archCtx.RoomInfo, firstSlot))
            {
                await FollowupAsync($"Already connected on current port `{archCtx.RoomInfo.Port}`.");
                return;
            }

            var newPort = await ArchipelagoWebService.ReconnectAsync(archCtx.RoomInfo, firstSlot);
            await FollowupAsync($"Connected on new port `{newPort}`.");
        }

        private async Task<bool> TryConnectAsync(RoomInfo roomInfo, SlotInfo slotInfo)
        {
            var client = new ArchipelagoClient(roomInfo, slotInfo);
            try
            {
                await client.ConnectAsync();
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                await client.Disconnect();
            }
        }

        [SlashCommand("register", "Register a player with the bot.")]
        public async Task RegisterAsync(
            [Summary(description: "The Archipelago slot/player name.")] string playerName,
            [Summary(description: "The Discord user to register.")] IUser? user = null)
        {
            var archCtx = ArchipelagoContext.FromCtx(Context);

            await DeferAsync();

            if (user != null && !archCtx.IsUserAdmin(Context.User.Id))
            {
                await FollowupAsync($"Only the person who setup the room can register other people for themselves.");
                return;
            }

            if (archCtx.SlotInfo != null && user == null &&!archCtx.IsUserAdmin(Context.User.Id))
            {
                await FollowupAsync($"That slot has already been linked to a user. If it was linked to the wrong user, tell the person who setup the room to fix it.");
                return;
            }
            var targetUser = user ?? Context.User;

            await ArchipelagoService.RegisterSlotInfo(targetUser.Id, archCtx.RoomInfo, playerName);

            await FollowupAsync($"Registered player `{playerName}` to {targetUser.Username}");
        }

        [SlashCommand("status", "Show the current status of this room.")]
        public async Task StatusAsync()
        {
            await DeferAsync();
            var archCtx = ArchipelagoContext.FromCtx(Context);

            var embed = new EmbedBuilder()
                .WithTitle("Archipelago Room")
                .WithDescription($"This discord channel has been linked to an archipelago room.")
                .AddField("Registered Players", archCtx.RoomInfo.SlotInfos.Count, true)
                .AddField("Internal Seed", archCtx.RoomInfo.Seed, true)
                .AddField("Games", string.Join("\n", archCtx.RoomInfo.Games), false)
                .WithColor(Color.Blue)
                .Build();

            await FollowupAsync(embed: embed);
        }

        [SlashCommand("sync", "Syncs the item hint status and information")]
        public async Task Sync()
        {
            await DeferAsync();

            var archCtx = ArchipelagoContext.FromCtx(Context, requireRegistered: true);

            try
            {
                var announceText = await ArchipelagoClient.DoResync(archCtx);
                await FollowupAsync("__done!__\n" + announceText);
            }
            catch (ArchipelagoConnectionFailedException) when (!string.IsNullOrEmpty(archCtx.RoomInfo.RoomId))
            {
                var firstSlot = archCtx.RoomInfo.SlotInfos.FirstOrDefault()
                    ?? throw new UserError("No registered slots in this room.");
                var newPort = await ArchipelagoWebService.ReconnectAsync(archCtx.RoomInfo, firstSlot);
                await FollowupAsync($"Couldn't connect — reconnected on new port `{newPort}`. Please run `/sync` again.");
            }
        }
    }

}
