using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Wikiled.Redis.Serialization
{
    public class HashSetSerialization<T> : IObjectSerialization<T>
    {
        private readonly ILogger<HashSetSerialization<T>> log;

        private readonly IKeyValueSerializer<T> serializer;

        public HashSetSerialization(ILogger<HashSetSerialization<T>> log, IKeyValueSerializer<T> serializer)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public string[] GetColumns()
        {
            return serializer.Properties;
        }

        public IEnumerable<HashEntry> GetEntries(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var entries = serializer.Serialize(instance)
                                    .Select(
                                        item => new HashEntry(
                                                    item.Key,
                                                    string.IsNullOrEmpty(item.Value) ? RedisValue.EmptyString : (RedisValue)item.Value));

            foreach (var entry in entries)
            {
                yield return entry;
            }
        }

        public IEnumerable<T> GetInstances(RedisValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }
            
            return serializer.DeserializeStream(GetValues(serializer.Properties, values));
        }

        private IEnumerable<KeyValuePair<string, string>> GetValues(string[] names, RedisValue[] values)
        {
            var total = names.Length;
            for (int i = 0; i < values.Length; i++)
            {
                var name = names[i % total];
                var value = values[i];
                yield return new KeyValuePair<string, string>(name, value);
            }
        }
    }
}
