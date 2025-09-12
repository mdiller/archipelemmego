using ArchipeLemmeGo.Datamodel.Arch;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Datamodel.Infos
{
    /// <summary>
    /// Represents a dependency of a location on an item
    /// </summary>
    public class DependancyLink
    {
        /// <summary>
        /// The location that is blocked
        /// </summary>
        public ArchLocation Dependant { get; set; }

        /// <summary>
        /// The item(s) that will unblock the location.
        /// </summary>
        public List<ArchItem> Prerequisites { get; set; }

        /// <summary>
        /// The number of prerequisites needed to unlock the dependant
        /// </summary>
        [JsonIgnore]
        public int RequiredCount => 1;
    }
}
