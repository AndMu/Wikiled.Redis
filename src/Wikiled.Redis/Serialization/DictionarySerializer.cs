using System.Collections.Generic;
using System.Linq;
using Wikiled.Core.Utility.Arguments;

namespace Wikiled.Redis.Serialization
{
    public class DictionarySerializer : IKeyValueSerializer<Dictionary<string, string>>
    {
        public DictionarySerializer(string[] properties)
        {
            Guard.NotNull(() => properties, properties);
            Properties = properties;
        }

        public string[] Properties { get; }

        public Dictionary<string, string> Deserialize(IEnumerable<KeyValuePair<string, string>> entries)
        {
            return DeserializeStream(entries).First();
        }

        public IEnumerable<Dictionary<string, string>> DeserializeStream(IEnumerable<KeyValuePair<string, string>> entries)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
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
