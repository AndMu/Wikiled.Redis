using System;
using System.Collections.Generic;
using System.Linq;

namespace Wikiled.Redis.Serialization
{
    public class DictionarySerializer : IKeyValueSerializer<Dictionary<string, string>>
    {
        public DictionarySerializer(string[] properties)
        {
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        public string[] Properties { get; }

        public Dictionary<string, string> Deserialize(IEnumerable<KeyValuePair<string, string>> entries)
        {
            return DeserializeStream(entries).First();
        }

        public IEnumerable<Dictionary<string, string>> DeserializeStream(IEnumerable<KeyValuePair<string, string>> entries)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var entry in entries)
            {
                if (dictionary.ContainsKey(entry.Key))
                {
                    yield return dictionary;
                    dictionary = new Dictionary<string, string>();
                }

                dictionary[entry.Key] = entry.Value;
            }

            if (dictionary.Count > 0)
            {
                yield return dictionary;
            }
        }

        public IEnumerable<KeyValuePair<string, string>> Serialize(Dictionary<string, string> instance)
        {
            return instance;
        }
    }
}
