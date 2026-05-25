using ArchipeLemmeGo.Bot;
using ArchipeLemmeGo.Datamodel;
using ArchipeLemmeGo.Datamodel.Infos;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Archipelago
{
    /// <summary>
    /// The context that we're working in for what archipelago room/slot we're referring to
    /// </summary>
    public class ArchipelagoContext
    {
        public RoomInfo RoomInfo { get; set; }
        public SlotInfo SlotInfo { get; set; }

        /// <summary>
        /// Checks to see if the user is the admin of the room, or the owner of the bot
        /// </summary>
        /// <param name="userId">The user attempting to do admin stuff</param>
        /// <returns>The ID of the user</returns>
        public bool IsUserAdmin(ulong userId)
        {
            return userId == BotInfo.BotOwner || (RoomInfo != null && RoomInfo.AdminId == userId);
        }

        /// <summary>
        /// Gets the channel linker
        /// </summary>
        /// <returns>The channel linker object</returns>
        public static ChannelLinker GetChannelLinker()
        {
            var uri = InfoUri.New<ChannelLinker>("main");
            if (uri.Exists())
            {
                return uri.Load<ChannelLinker>();
            }
            else
            {
                return new ChannelLinker();
            }
        }

        public static RoomInfo LoadRoomInfo(InfoUri roomUri)
        {
            var roomInfo = roomUri.Load<RoomInfo>();
            roomInfo.HydrateArchStuff();
            return roomInfo;
        }

        public static ArchipelagoContext FromChannelUser(ulong channelId, ulong userId, bool allowNull = false, bool requireRegistered = false)
        {
            Console.WriteLine($"[FromChannelUser] channel={channelId} user={userId} allowNull={allowNull} requireRegistered={requireRegistered}");

            var discLinker = GetChannelLinker();
            Console.WriteLine($"[FromChannelUser] ChannelLinker has {discLinker.ChannelAssignments.Count} assignments");

            if (!discLinker.ChannelAssignments.ContainsKey(channelId))
            {
                Console.WriteLine($"[FromChannelUser] channel {channelId} NOT found in linker");
                if (allowNull)
                {
                    return null;
                }
                else
                {
                    throw new UserError($"You have to link this channel to a room first. Try `/setuproom`.");
                }
            }

            var roomUri = discLinker.ChannelAssignments[channelId];
            Console.WriteLine($"[FromChannelUser] channel {channelId} → room {roomUri}");

            var roomInfo = LoadRoomInfo(roomUri);
            Console.WriteLine($"[FromChannelUser] room loaded: seed={roomInfo.Seed} slots={roomInfo.SlotInfos.Count}");
            foreach (var s in roomInfo.SlotInfos)
                Console.WriteLine($"[FromChannelUser]   slot {s.SlotId} '{s.Name}' discordId={s.DiscordId}");

            var authorId = userId;
            var slotInfo = roomInfo.SlotInfos.FirstOrDefault(s => s.DiscordId == authorId);
            Console.WriteLine($"[FromChannelUser] direct slot lookup for {authorId}: {(slotInfo != null ? $"found slot {slotInfo.SlotId} '{slotInfo.Name}'" : "not found")}");

            if (slotInfo == null && roomInfo.CoPlayers.TryGetValue(authorId, out var coSlotId))
            {
                slotInfo = roomInfo.GetSlotInfo(coSlotId);
                Console.WriteLine($"[FromChannelUser] coplayer lookup for {authorId}: {(slotInfo != null ? $"found slot {slotInfo.SlotId} '{slotInfo.Name}'" : "not found")}");
            }

            if (slotInfo == null && requireRegistered)
            {
                Console.WriteLine($"[FromChannelUser] THROWING: user {authorId} not registered in room {roomUri}");
                throw new UserError($"You have to register as a player in this room with `/register`.");
            }

            Console.WriteLine($"[FromChannelUser] resolved: user={authorId} slot={slotInfo?.SlotId} ('{slotInfo?.Name}')");
            return new ArchipelagoContext
            {
                RoomInfo = roomInfo,
                SlotInfo = slotInfo
            };
        }

        /// <summary>
        /// Gets the archipelago context for the given discord interaction
        /// </summary>
        /// <param name="ctx">The discord context</param>
        /// <returns>The archipelago context</returns>
        public static ArchipelagoContext FromCtx(SocketInteractionContext ctx, bool allowNull = false, bool requireRegistered = false)
        {
            var channelId = ctx.Channel.Id;
            var authorId = ctx.User.Id;
            Console.WriteLine($"[FromCtx] channel={channelId} user={authorId} ({ctx.User.Username}) allowNull={allowNull} requireRegistered={requireRegistered}");

            var discLinker = GetChannelLinker();
            Console.WriteLine($"[FromCtx] ChannelLinker has {discLinker.ChannelAssignments.Count} assignments");

            if (!discLinker.ChannelAssignments.ContainsKey(channelId))
            {
                Console.WriteLine($"[FromCtx] channel {channelId} NOT found in linker. Known channels: {string.Join(", ", discLinker.ChannelAssignments.Keys)}");
                if (allowNull)
                {
                    return null;
                }
                else
                {
                    throw new UserError($"You have to link this channel to a room first. Try `/setuproom`.");
                }
            }

            var roomUri = discLinker.ChannelAssignments[channelId];
            Console.WriteLine($"[FromCtx] channel {channelId} → room {roomUri}");

            var roomInfo = LoadRoomInfo(roomUri);
            Console.WriteLine($"[FromCtx] room loaded: seed={roomInfo.Seed} slots={roomInfo.SlotInfos.Count} coPlayers={roomInfo.CoPlayers.Count}");
            foreach (var s in roomInfo.SlotInfos)
                Console.WriteLine($"[FromCtx]   slot {s.SlotId} '{s.Name}' game='{s.Game}' discordId={s.DiscordId}");
            foreach (var cp in roomInfo.CoPlayers)
                Console.WriteLine($"[FromCtx]   coplayer discordId={cp.Key} → slotId={cp.Value}");

            var slotInfo = roomInfo.SlotInfos.FirstOrDefault(s => s.DiscordId == authorId);
            Console.WriteLine($"[FromCtx] direct slot lookup for {authorId}: {(slotInfo != null ? $"found slot {slotInfo.SlotId} '{slotInfo.Name}'" : "not found")}");

            if (slotInfo == null && roomInfo.CoPlayers.TryGetValue(authorId, out var coSlotId))
            {
                slotInfo = roomInfo.GetSlotInfo(coSlotId);
                Console.WriteLine($"[FromCtx] coplayer lookup for {authorId}: {(slotInfo != null ? $"found slot {slotInfo.SlotId} '{slotInfo.Name}'" : "not found")}");
            }

            if (slotInfo == null && requireRegistered)
            {
                Console.WriteLine($"[FromCtx] THROWING: user {authorId} ({ctx.User.Username}) not registered in room {roomUri}");
                throw new UserError($"You have to register as a player in this room with `/register`.");
            }

            Console.WriteLine($"[FromCtx] resolved: user={authorId} slot={slotInfo?.SlotId} ('{slotInfo?.Name}')");
            return new ArchipelagoContext
            {
                RoomInfo = roomInfo,
                SlotInfo = slotInfo
            };
        }
    }
}
