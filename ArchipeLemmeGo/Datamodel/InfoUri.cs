using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Datamodel
{
    /// <summary>
    /// A serializable reference to a particular info
    /// </summary>
    public class InfoUri
    {
        private static string infoPathBase = Path.Combine("resources", "info");

        /// <summary>
        /// The category of info (often refers to a specific type)
        /// </summary>
        public string Category { get; private set; }

        /// <summary>
        /// The specific identifier for this info object
        /// </summary>
        public string Identifier { get; private set; }

        /// <summary>
        /// Creates an infouri from a type and an identifier
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static InfoUri New<T>(string identifier) where T : InfoBase
        {
            return new InfoUri
            {
                Category = typeof(T).Name,
                Identifier = identifier,
            };
        }

        /// <summary>
        /// Deserializes a uri from a string
        /// </summary>
        /// <param name="str">The string to deserialize</param>
        /// <returns>The infouri</returns>
        public static InfoUri FromString(string str)
        {
            var parts = str.Split(':');
            if (parts.Length != 2)
            {
                throw new ArgumentException($"BAD URI IDENTIFIER (SHOULD HAVE 1 COLON): '{str}'");
            }
            return new InfoUri
            {
                Category = parts[0],
                Identifier = parts[1]
            };
        }

        private static string SanitizeFileName(string input, char replacement = '_')
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input cannot be null or empty.", nameof(input));
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(input.Select(c => invalidChars.Contains(c) ? replacement : c).ToArray());
            return sanitized.Trim(' ', '.');
        }

        /// <summary>
        /// Gets the filepath to this infouri
        /// </summary>
        /// <returns>The filepath</returns>
        public string ToFilePath()
        {
            return Path.Combine(infoPathBase, Category, SanitizeFileName(Identifier) + ".json");
        }

        public override string ToString()
        {
            return $"{Category}:{Identifier}";
        }

        /// <summary>
        /// Loads the Info object from a file
        /// </summary>
        /// <typeparam name="T">The type of the info object to laod</typeparam>
        /// <returns>The initialized info file</returns>
        public T Load<T>() where T : InfoBase
        {
            if (typeof(T).Name != Category)
            {
                throw new Exception($"WRONG INFO TYPE '{typeof(T).Name}' != '{Category}'");
            }
            return InfoBase.Load<T>(this);
        }

        /// <summary>
        /// Whether or not this infouri already exists
        /// </summary>
        /// <returns>True if the file exists, false if not.</returns>
        public bool Exists()
        {
            return Path.Exists(ToFilePath());
        }
    }

    /// <summary>
    /// custom serializer for infouris
    /// </summary>
    public class InfoUriNewtonsoftConverter : JsonConverter<InfoUri>
    {
        public override void WriteJson(JsonWriter writer, InfoUri value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override InfoUri ReadJson(JsonReader reader, Type objectType, InfoUri existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var str = (string)reader.Value;
            return InfoUri.FromString(str);
        }
    }

}
