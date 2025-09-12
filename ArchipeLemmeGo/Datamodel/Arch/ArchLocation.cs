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
    /// An archipelago location
    /// </summary>
    public class ArchLocation
    {
        /// <summary>
        /// The slot ID of the player who this locatio belongs to
        /// </summary>
        public int Slot { get; set; }

        /// <summary>
        /// The ID of the location
        /// </summary>
        public long LocationId { get; set; }

        /// <summary>
        /// The string name of this location (looked up from the roominfo)
        /// </summary>
        [JsonIgnore]
        public string Name
        {
            get => RoomInfo?.GetName(this) ?? $"Location @id={LocationId}";
        }

        /// <summary>
        /// The string name of the player who owns this location (looked up from the roominfo)
        /// </summary>
        [JsonIgnore]
        public SlotInfo Player
        {
            get => RoomInfo?.GetSlotInfo(Slot);
        }

        public string ToDiscString()
        {
            return $"{Slot}.{LocationId}";
        }

        public static ArchLocation FromDiscString(string str, RoomInfo roomInfo)
        {
            var parts = str.Split('.');
            return new ArchLocation
            {
                Slot = int.Parse(parts[0]),
                LocationId = long.Parse(parts[1]),
                RoomInfo = roomInfo,
            };
        }

        /// <summary>
        /// The roominfo for the archipelago room that this location belongs to (used for serializing / lookups)
        /// </summary>
        [JsonIgnore]
        public RoomInfo RoomInfo { get; set; } = null;

        // Override Equals
        public override bool Equals(object? obj)
        {
            if (obj is not ArchLocation other)
                return false;

            return Slot == other.Slot
                && LocationId == other.LocationId;
        }

        public static bool operator ==(ArchLocation? a, ArchLocation? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.Equals(b);
        }

        public static bool operator !=(ArchLocation? a, ArchLocation? b)
        {
            return !(a == b);
        }

        // Override GetHashCode
        public override int GetHashCode()
        {
            return HashCode.Combine(Slot, LocationId);
        }

        // Override GetHashCode
        public override string ToString()
        {
            return Name;
        }
    }
}
