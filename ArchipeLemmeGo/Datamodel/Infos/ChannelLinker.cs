using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Datamodel.Infos
{
    /// <summary>
    /// Holds the linking of discord channels to archipelago rooms
    /// </summary>
    public class ChannelLinker : InfoBase
    {
        public override InfoUri Uri => InfoUri.New<ChannelLinker>("main");

        /// <summary>
        /// A linking of discord channel id to the infouri of an archipelago room
        /// </summary>
        public Dictionary<ulong, InfoUri> ChannelAssignments { get; set; } = new Dictionary<ulong, InfoUri>();
    }
}
