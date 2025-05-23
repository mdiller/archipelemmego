using Newtonsoft.Json;
using System;
using System.IO;

namespace ArchipeLemmeGo.Datamodel
{
    public class InfoService
    {
        private static InfoService _instance;
        private static readonly object _lock = new();

        public static InfoService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new InfoService();
                    }
                }
                return _instance;
            }
        }

        private readonly JsonSerializerSettings _jsonSettings;

        private InfoService()
        {
            _jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto // Enables polymorphic (de)serialization
            };
            _jsonSettings.Converters.Add(new InfoUriNewtonsoftConverter());
        }

        public void WriteFile<T>(T obj, string filePath)
        {
            var json = JsonConvert.SerializeObject(obj, _jsonSettings);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, json);
        }

        public T ReadFile<T>(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(json, _jsonSettings);
        }
    }
}
