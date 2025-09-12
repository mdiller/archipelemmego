using Archipelago.MultiClient.Net.Models;
using ArchipeLemmeGo.Archipelago;
using ArchipeLemmeGo.Datamodel.Arch;
using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Bot.AutocompleteHandlers
{
    public class LocationAutocompleteHandler : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var archCtx = ArchipelagoContext.FromChannelUser(context.Channel.Id, context.User.Id, true);

            var currentValue = autocompleteInteraction.Data.Current.Value?.ToString()?.ToLower();

            var slotInfo = archCtx?.SlotInfo;

            if (slotInfo == null)
            {
                // TODO: figure out how to use FromFailure here
                var errResponse = new[] { "Man u gotta go do `/register` brooo" }
                    .Select(i => new AutocompleteResult(i, i));
                return AutocompletionResult.FromSuccess(errResponse);
            }

            // If it starts with SLOTNAME. then suggest items from that
            var slotNames = archCtx.RoomInfo.SlotInfos
                .Where(s => !string.IsNullOrEmpty(s.Name))
                .ToDictionary(s => s.Name.ToLower() + ".", s => s);

            var prefix = "";
            foreach (var name in slotNames.Keys)
            {
                if (currentValue != null && currentValue.StartsWith(name))
                {
                    prefix = name;
                    slotInfo = slotNames[name];
                    break;
                }
            }

            var locations = slotInfo.Locations;

            if (currentValue == null)
            {
                return AutocompletionResult.FromSuccess();
            }

            var currentText = currentValue;
            if (prefix != "")
            {
                currentText = currentText[prefix.Length..];
            }

            // Create a collection with suggestions for autocomplete
            IEnumerable<AutocompleteResult> results = locations
                .Where(i => i.ToLower().Contains(currentText))
                .Select(i => new AutocompleteResult(i, new ArchLocation
                {
                    Slot = slotInfo.SlotId,
                    LocationId = slotInfo.LocationLookup[i],
                    RoomInfo = archCtx.RoomInfo
                }.ToDiscString()));

            // max - 25 suggestions at a time (API limit)
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }
}
