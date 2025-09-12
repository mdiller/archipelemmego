using Archipelago.MultiClient.Net.Models;
using ArchipeLemmeGo.Datamodel.Arch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Datamodel.Infos
{
    public class RoomInfo : InfoBase
    {
        public override InfoUri Uri => InfoUri.New<RoomInfo>(Seed);

        /// <summary>
        /// The host url for this room
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The port for this room
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// The password for connecting to this room
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The seed used to generate this room
        /// </summary>
        public string Seed { get; set; }

        /// <summary>
        /// How much a hint costs to get
        /// </summary>
        public int HintCostPercentage { get; set; }

        /// <summary>
        /// A list of the games being used on this server
        /// </summary>
        public string[] Games { get; set; }

        /// <summary>
        /// The discord ID of the admin of this room (usually whoever called /setup)
        /// </summary>
        public ulong AdminId { get; set; }

        /// <summary>
        /// The infos for each slot
        /// </summary>
        public List<SlotInfo> SlotInfos { get; set; } = new List<SlotInfo>();

        /// <summary>
        /// The current list of requested hints
        /// </summary>
        public List<RequestedHintInfo> RequestedHints { get; set; } = new List<RequestedHintInfo>();

        /// <summary>
        /// The list of dependancies
        /// </summary>
        public List<DependancyLink> Dependancies { get; set; } = new List<DependancyLink>();

        /// <summary>
        /// Gets the name of a location
        /// </summary>
        public string? GetName(ArchItem item)
        {
            return SlotInfos.FirstOrDefault(s => s.SlotId == item.Slot)?
                .ItemLookup?
                .Where(kvp => kvp.Value == item.ItemId)?
                .Select(kvp => kvp.Key)?
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the name of a location
        /// </summary>
        public string? GetName(ArchLocation location)
        {
            return SlotInfos.FirstOrDefault(s => s.SlotId == location.Slot)?
                .LocationLookup?
                .Where(kvp => kvp.Value == location.LocationId)?
                .Select(kvp => kvp.Key)?
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the slotinfo for the given slot
        /// </summary>
        public SlotInfo? GetSlotInfo(int slotId)
        {
            return SlotInfos.FirstOrDefault(s => s.SlotId == slotId);
        }

        /// <summary>
        /// Hydrates all the archipelago items in this room with the roominfo they need to print stuff etc
        /// </summary>
        public void HydrateArchStuff()
        {
            RequestedHints.ForEach(h =>
            {
                h.Item.RoomInfo = this;
                h.Location.RoomInfo = this;
            });
            Dependancies.ForEach(d =>
            {
                d.Dependant.RoomInfo = this;
                d.Prerequisites.ForEach(i => i.RoomInfo = this);
            });
        }
    }
}
