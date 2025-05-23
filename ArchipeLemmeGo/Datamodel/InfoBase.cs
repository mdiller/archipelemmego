using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Datamodel
{
    /// <summary>
    /// The abstract class for Info objects. Mainly used for the file saving and loading functionality
    /// </summary>
    public abstract class InfoBase
    {
        /// <summary>
        /// The version of the parser used for this Infobase class
        /// </summary>
        public int ParserVersion { get; set; } = 1;

        /// <summary>
        /// The uri to represent this particular info object
        /// </summary>
        [JsonIgnore]
        public abstract InfoUri Uri { get; }

        /// <summary>
        /// Saves the InfoBase to a file
        /// </summary>
        public void Save()
        {
            InfoService.Instance.WriteFile(this, Uri.ToFilePath());
        }

        /// <summary>
        /// Loads the Info object from a file
        /// </summary>
        /// <typeparam name="T">The type of the info object to laod</typeparam>
        /// <param name="uri">The uri identifying the object</param>
        /// <returns>The initialized info file</returns>
        public static T Load<T>(InfoUri uri) where T : InfoBase
        {
            return InfoService.Instance.ReadFile<T>(uri.ToFilePath());
        } 
    }
}
