using System.Collections.Generic;
using System.Linq;
using NLog;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Serialization
{
    public class HashSetSerialization : IObjectSerialization
    {
        private readonly IRedisLink link;

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public HashSetSerialization(IRedisLink link)
        {
            Guard.NotNull(() => link, link);
            this.link = link;
        }

        public string[] GetColumns<T>()
        {
            var definition = link.GetDefinition<T>();
            if(definition.Serializer == null)
            {
                log.Error("Serialzer not found");
                return new string[] {};
            }

            return definition.Serializer.Properties;
        }

        public IEnumerable<HashEntry> GetEntries<T>(T instance)
        {
            Guard.NotNull(() => instance, instance);
            var definition = link.GetDefinition<T>();
            if(definition.Serializer == null)
            {
                log.Error("Serializer not found");
                yield break;
            }

            var entries = definition.Serializer.Serialize(instance)
                                    .Select(
                                        item => new HashEntry(
                                                    item.Key,
                                                    string.IsNullOrEmpty(item.Value) ? RedisValue.EmptyString : (RedisValue)item.Value));

            foreach(var entry in entries)
            {
                yield return entry;
            }
        }

        public IEnumerable<T> GetInstances<T>(RedisValue[] values)
        {
            Guard.NotNull(() => values, values);
            var definition = link.GetDefinition<T>();
            return definition.Serializer.DeserializeStream(GetValues(definition.Serializer.Properties, values));
        }

        private IEnumerable<KeyValuePair<string, string>> GetValues(string[] names, RedisValue[] values)
        {
            var total = names.Length;
            for(int i = 0; i < values.Length; i++)
            {
                var name = names[i % total];
                var value = values[i];
                yield return new KeyValuePair<string, string>(name, value);
            }
        }
    }
}
