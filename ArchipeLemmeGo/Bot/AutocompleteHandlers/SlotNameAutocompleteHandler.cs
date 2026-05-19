using ArchipeLemmeGo.Archipelago;
using Discord;
using Discord.Interactions;

namespace ArchipeLemmeGo.Bot.AutocompleteHandlers
{
    public class SlotNameAutocompleteHandler : AutocompleteHandler
    {
        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var archCtx = ArchipelagoContext.FromChannelUser(context.Channel.Id, context.User.Id, true);
            if (archCtx?.RoomInfo == null)
                return Task.FromResult(AutocompletionResult.FromSuccess());

            var currentValue = autocompleteInteraction.Data.Current.Value?.ToString()?.ToLower() ?? "";

            var results = archCtx.RoomInfo.SlotInfos
                .Where(s => !string.IsNullOrEmpty(s.Name) && s.Name.ToLower().Contains(currentValue))
                .Select(s => new AutocompleteResult(s.Name, s.Name));

            return Task.FromResult(AutocompletionResult.FromSuccess(results.Take(25)));
        }
    }
}
