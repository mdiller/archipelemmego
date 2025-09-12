using Archipelago.MultiClient.Net.Models;
using ArchipeLemmeGo.Datamodel.Infos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Datamodel.Arch
{
    /// <summary>
    /// An archipelago item
    /// </summary>
    public class ArchItem
    {
        /// <summary>
        /// The slot ID of the player who this item belongs to
        /// </summary>
        public int Slot { get; set; }

        /// <summary>
        /// The ID of the item
        /// </summary>
        public long ItemId { get; set; }

        /// <summary>
        /// The progression level of this item (0 == default/none/unset, 1+ == the level of progression)
        /// </summary>
        public int ProgressionLevel { get; set; } = 0;

        /// <summary>
        /// The roominfo for the archipelago room that this item belongs to (used for serializing / lookups)
        /// </summary>
        [JsonIgnore]
        public RoomInfo RoomInfo { get; set; } = null;

        /// <summary>
        /// The string name of this item (looked up from the roominfo)
        /// </summary>
        [JsonIgnore]
        public string Name
        {
            get {
                var name = RoomInfo?.GetName(this) ?? $"Item @id={ItemId}";
                if (ProgressionLevel > 0)
                {
                    name += $"[{ProgressionLevel}]";
                }
                return name;
            }
        }


        public string ToDiscString()
        {
            return $"{Slot}.{ItemId}.{ProgressionLevel}";
        }

        public static ArchItem FromDiscString(string str, RoomInfo roomInfo)
        {
            var parts = str.Split('.');
            return new ArchItem
            {
                Slot = int.Parse(parts[0]),
                ItemId = long.Parse(parts[1]),
                ProgressionLevel = int.Parse(parts[2]),
                RoomInfo = roomInfo,
            };
        }

        /// <summary>
        /// The string name of the player who owns this item (looked up from the roominfo)
        /// </summary>
        [JsonIgnore]
        public SlotInfo Player
        {
            get => RoomInfo?.GetSlotInfo(Slot);
        }

        // Override Equals
        public override bool Equals(object? obj)
        {
            if (obj is not ArchItem other)
                return false;

            return Slot == other.Slot
                && ItemId == other.ItemId;
        }

        public static bool operator ==(ArchItem? a, ArchItem? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.Equals(b);
        }

        public static bool operator !=(ArchItem? a, ArchItem? b)
        {
            return !(a == b);
        }

        // Override GetHashCode
        public override int GetHashCode()
        {
            return HashCode.Combine(Slot, ItemId);
        }

        // Override GetHashCode
        public override string ToString()
        {
            return Name;
        }
    }
}
