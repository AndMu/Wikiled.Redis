using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wikiled.Common.Logging;
using Wikiled.Redis.Data;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Serialization
{
    public class ObjectHashSetSerialization<T> : IObjectSerialization<T>
    {
        private readonly string[] columns =
        {
            FieldConstants.Data,
            FieldConstants.Compressed,
            FieldConstants.Type
        };

        private readonly IRedisLink link;

        private static readonly ILogger log = ApplicationLogging.CreateLogger<ObjectHashSetSerialization<T>>();

        private readonly IDataSerializer serializer;

        private readonly bool isWellKnown;

        public ObjectHashSetSerialization(IRedisLink link, IDataSerializer serializer, bool isWellKnown)
        {
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.isWellKnown = isWellKnown;
        }

        public string[] GetColumns()
        {
            return columns;
        }

        public IEnumerable<HashEntry> GetEntries(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var data = serializer.Serialize(instance);
            yield return new HashEntry(FieldConstants.Data, data);
            yield return new HashEntry(FieldConstants.TimeStamp, Stopwatch.GetTimestamp());
            if (!isWellKnown)
            {
                yield return new HashEntry(FieldConstants.Type, link.GetTypeID(instance.GetType()));
            }
        }

        public IEnumerable<T> GetInstances(RedisValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            for (int i = 0; i < values.Length; i += 3)
            {
                byte[] data = values[i];
                if (data == null || data.Length == 0)
                {
                    log.LogWarning("Not Data Found in redis record");
                    continue;
                }

                if (isWellKnown)
                {
                    yield return serializer.Deserialize<T>(data);
                }
                else
                {
                    var type = link.GetTypeByName(values[i + 2]);
                    if (type == null)
                    {
                        log.LogError("Type is not resolved");
                        continue;
                    }

                    yield return (T)serializer.Deserialize(type, data);
                }
            }
        }
    }
}
