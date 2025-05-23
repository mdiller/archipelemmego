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



        public static ArchipelagoContext FromChannelUser(ulong channelId, ulong userId, bool allowNull = false, bool requireRegistered = false)
        {
            var discLinker = GetChannelLinker();
            
            if (!discLinker.ChannelAssignments.ContainsKey(channelId))
            {
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

            var roomInfo = roomUri.Load<RoomInfo>();
            var authorId = userId;
            var slotInfo = roomInfo.SlotInfos.FirstOrDefault(s => s.DiscordId == authorId);

            if (slotInfo == null && requireRegistered)
            {
                throw new UserError($"You have to register as a player in this room with `/register`.");
            }

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
            var discLinker = GetChannelLinker();

            var channelId = ctx.Channel.Id;
            if (!discLinker.ChannelAssignments.ContainsKey(channelId))
            {
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

            var roomInfo = roomUri.Load<RoomInfo>();
            var authorId = ctx.User.Id;
            var slotInfo = roomInfo.SlotInfos.FirstOrDefault(s => s.DiscordId == authorId);

            if (slotInfo == null && requireRegistered)
            {
                throw new UserError($"You have to register as a player in this room with `/register`.");
            }

            return new ArchipelagoContext
            {
                RoomInfo = roomInfo,
                SlotInfo = slotInfo
            };
        }
    }
}
