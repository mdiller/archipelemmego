using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Datamodel.Infos
{
    /// <summary>
    /// Contains information about a particular slot in a given room
    /// </summary>
    public class SlotInfo
    {
        /// <summary>
        /// The ID of the slot
        /// </summary>
        public int SlotId { get; set; }

        /// <summary>
        /// The player name for this slot
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The game for this slot
        /// </summary>
        public string Game { get; set; }

        /// <summary>
        /// A mapping of all the item names to their IDs
        /// </summary>
        public Dictionary<string, long> ItemLookup { get; set; }

        /// <summary>
        /// A mapping of all the location names to their IDs
        /// </summary>
        public Dictionary<string, long> LocationLookup { get; set; }

        /// <summary>
        /// The items in the game for this slot
        /// </summary>
        [JsonIgnore]
        public string[] Items => ItemLookup.Keys.ToArray();

        /// <summary>
        /// The items in the game for this slot
        /// </summary>
        [JsonIgnore]
        public string[] Locations => LocationLookup.Keys.ToArray();

        /// <summary>
        /// The discord id of the user linked to this slot
        /// </summary>
        public ulong DiscordId { get; set; }

        /// <summary>
        /// Pretty print the name and @mention if avilable
        /// </summary>
        /// <returns></returns>
        public string ToSignature(bool doMention = true)
        {
            if (DiscordId == 0 || doMention == false)
            {
                return Name;
            }
            return $"<@!{DiscordId}> ({Name})";
        }
    }
}
