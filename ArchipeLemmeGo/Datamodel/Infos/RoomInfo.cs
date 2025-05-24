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
    }
}
