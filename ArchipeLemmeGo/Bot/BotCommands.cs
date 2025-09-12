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

            await DeferAsync();

            if (archCtx != null && !archCtx.IsUserAdmin(Context.User.Id))
            {
                await FollowupAsync($"This channel has already been linked to a room.");
                return;
            }

            var roomInfo = await ArchipelagoService.RegisterRoomInfo(Context.User.Id, host, port);

            var channelLinker = ArchipelagoContext.GetChannelLinker();
            channelLinker.ChannelAssignments[Context.Channel.Id] = roomInfo.Uri;
            channelLinker.Save();

            await FollowupAsync($"Room has been set up! Internal Seed: `{roomInfo.Seed}`");
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

            var announceText = await ArchipelagoClient.DoResync(archCtx);

            await FollowupAsync("__done!__\n" + announceText);
        }
    }

}
