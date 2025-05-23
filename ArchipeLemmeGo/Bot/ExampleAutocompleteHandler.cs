using ArchipeLemmeGo.Archipelago;
using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Bot
{
    public class ExampleAutocompleteHandler : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var archCtx = ArchipelagoContext.FromChannelUser(context.Channel.Id, context.User.Id, true);

            var slotInfo = archCtx?.SlotInfo;

            if (slotInfo == null)
            {
                // TODO: figure out how to use FromFailure here
                var errResponse = new[] { "Man u gotta go do `/register` brooo" }
                    .Select(i => new AutocompleteResult(i, i));
                return AutocompletionResult.FromSuccess(errResponse);
            }

            var items = slotInfo.Items;

            var currentValue = autocompleteInteraction.Data.Current.Value?.ToString()?.ToLower();

            if (currentValue == null)
            {
                return AutocompletionResult.FromSuccess();
            }

            // Create a collection with suggestions for autocomplete
            IEnumerable<AutocompleteResult> results = items
                .Where(i => i.ToLower().Contains(currentValue))
                .Select(i => new AutocompleteResult(i, i));

            // max - 25 suggestions at a time (API limit)
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }
}
