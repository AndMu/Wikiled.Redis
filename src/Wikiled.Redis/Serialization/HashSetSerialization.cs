using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wikiled.Common.Logging;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Serialization
{
    public class HashSetSerialization : IObjectSerialization
    {
        private readonly IRedisLink link;

        private static readonly ILogger log = ApplicationLogging.CreateLogger<HashSetSerialization>();

        public HashSetSerialization(IRedisLink link)
        {
            this.link = link ?? throw new ArgumentNullException(nameof(link));
        }

        public string[] GetColumns<T>()
        {
            var definition = link.GetDefinition<T>();
            if (definition.KeyValueSerializer == null)
            {
                log.LogError("Serializer not found");
                return new string[] { };
            }

            return definition.KeyValueSerializer.Properties;
        }

        public IEnumerable<HashEntry> GetEntries<T>(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var definition = link.GetDefinition<T>();
            if (definition.KeyValueSerializer == null)
            {
                log.LogError("Serializer not found");
                yield break;
            }

            var entries = definition.KeyValueSerializer.Serialize(instance)
                                    .Select(
                                        item => new HashEntry(
                                                    item.Key,
                                                    string.IsNullOrEmpty(item.Value) ? RedisValue.EmptyString : (RedisValue)item.Value));

            foreach (var entry in entries)
            {
                yield return entry;
            }
        }

        public IEnumerable<T> GetInstances<T>(RedisValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var definition = link.GetDefinition<T>();
            return definition.KeyValueSerializer.DeserializeStream(GetValues(definition.KeyValueSerializer.Properties, values));
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
