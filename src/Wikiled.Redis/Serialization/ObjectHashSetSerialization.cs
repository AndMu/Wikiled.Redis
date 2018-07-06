using System;
using System.Collections.Generic;
using System.Diagnostics;
using NLog;
using StackExchange.Redis;
using Wikiled.Redis.Data;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Serialization
{
    public class ObjectHashSetSerialization : IObjectSerialization
    {
        private readonly string[] columns =
        {
            FieldConstants.Data,
            FieldConstants.Compressed,
            FieldConstants.Type
        };

        private readonly IRedisLink link;

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly IDataSerializer serializer;

        public ObjectHashSetSerialization(IRedisLink link, IDataSerializer serializer)
        {
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public string[] GetColumns<T>()
        {
            return columns;
        }

        public IEnumerable<HashEntry> GetEntries<T>(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var data = serializer.Serialize(instance);
            var definition = link.GetDefinition<T>();
            yield return new HashEntry(FieldConstants.Data, data);
            yield return new HashEntry(FieldConstants.TimeStamp, Stopwatch.GetTimestamp());
            if(!definition.IsWellKnown)
            {
                yield return new HashEntry(FieldConstants.Type, link.GetTypeID(instance.GetType()));
            }
        }

        public IEnumerable<T> GetInstances<T>(RedisValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var definition = link.GetDefinition<T>();
            for(int i = 0; i < values.Length; i += 3)
            {
                byte[] data = values[i];
                if((data == null) ||
                   (data.Length == 0))
                {
                    log.Warn("Not Data Found in redis record");
                    continue;
                }

                if(definition.IsWellKnown)
                {
                    yield return serializer.Deserialize<T>(data);
                }
                else
                {
                    var type = link.GetTypeByName(values[i + 2]);
                    if(type == null)
                    {
                        log.Error("Type is not resolved");
                        continue;
                    }

                    yield return (T)serializer.Deserialize(type, data);
                }
            }
        }
    }
}
