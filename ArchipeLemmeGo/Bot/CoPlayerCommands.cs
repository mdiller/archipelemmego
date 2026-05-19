using ArchipeLemmeGo.Archipelago;
using ArchipeLemmeGo.Bot.AutocompleteHandlers;
using Discord;
using Discord.Interactions;

namespace ArchipeLemmeGo.Bot
{
    [Group("coplayer", "Manage co-players for a slot")]
    public class CoPlayerCommands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("add", "Add a Discord user as a co-player for a slot")]
        public async Task AddCoPlayerAsync(
            [Summary(description: "The slot name to add the co-player to.")][Autocomplete(typeof(SlotNameAutocompleteHandler))] string playerName,
            [Summary(description: "The Discord user to add as a co-player.")] IUser user)
        {
            var archCtx = ArchipelagoContext.FromCtx(Context);
            await DeferAsync();

            var slot = archCtx.RoomInfo.SlotInfos.FirstOrDefault(s => s.Name == playerName);
            if (slot == null)
                throw new UserError($"No registered slot named `{playerName}` was found.");

            if (!archCtx.IsUserAdmin(Context.User.Id) && slot.DiscordId != Context.User.Id)
                throw new UserError("Only the slot's primary owner or an admin can add co-players.");

            if (archCtx.RoomInfo.SlotInfos.Any(s => s.DiscordId == user.Id))
                throw new UserError($"{user.Username} is already registered as a primary player.");

            if (archCtx.RoomInfo.CoPlayers.ContainsKey(user.Id))
                throw new UserError($"{user.Username} is already a co-player.");

            archCtx.RoomInfo.CoPlayers[user.Id] = slot.SlotId;
            archCtx.RoomInfo.Save();

            await FollowupAsync($"Added {user.Mention} as a co-player for `{slot.Name}`.");
        }

        [SlashCommand("remove", "Remove a Discord user from co-players")]
        public async Task RemoveCoPlayerAsync(
            [Summary(description: "The Discord user to remove.")] IUser user)
        {
            var archCtx = ArchipelagoContext.FromCtx(Context);
            await DeferAsync();

            if (!archCtx.RoomInfo.CoPlayers.TryGetValue(user.Id, out var slotId))
                throw new UserError($"{user.Username} is not registered as a co-player.");

            var slot = archCtx.RoomInfo.GetSlotInfo(slotId);

            if (!archCtx.IsUserAdmin(Context.User.Id) && slot?.DiscordId != Context.User.Id)
                throw new UserError("Only the slot's primary owner or an admin can remove co-players.");

            archCtx.RoomInfo.CoPlayers.Remove(user.Id);
            archCtx.RoomInfo.Save();

            await FollowupAsync($"Removed {user.Mention} as a co-player{(slot != null ? $" from `{slot.Name}`" : "")}.");
        }
    }
}
