using System;
using Microsoft.Extensions.Logging;
using Wikiled.Redis.Data;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.Logic
{
    public class PersistencyRegistrationHandler : IPersistencyRegistrationHandler
    {
        private readonly ILoggerFactory loggerFactory;

        private readonly IRedisLink link;

        public PersistencyRegistrationHandler(ILoggerFactory loggerFactory, IRedisLink link)
        {
            this.loggerFactory = loggerFactory;
            this.link = link;
        }

        public void RegisterList<T>(IDataSerializer serializer)
        {
            var persistency = new ListSerialization<T>(loggerFactory.CreateLogger<ListSerialization<T>>(),
                                                   link,
                                                   new RedisList(link, link.IndexManager),
                                                   link.IndexManager,
                                                   serializer);
            link.Register(persistency);
        }

        public void RegisterSet<T>(IDataSerializer serializer)
        {
            var persistency = new ListSerialization<T>(loggerFactory.CreateLogger<ListSerialization<T>>(),
                                                       link,
                                                       new RedisSet(link, link.IndexManager),
                                                       link.IndexManager,
                                                       serializer);
            link.Register(persistency);
        }

        public void RegisterHashsetSingle<T>(IKeyValueSerializer<T> serializer = null)
            where T : class, new()
        {
            serializer ??= new KeyValueSerializer<T>();

            var serialization = new HashSetSerialization<T>(loggerFactory.CreateLogger<HashSetSerialization<T>>(), serializer);

            var persistency = new SingleItemSerialization<T>(loggerFactory.CreateLogger<SingleItemSerialization<T>>(),
                                                             link,
                                                             serialization,
                                                             link.IndexManager);
            link.Register(persistency);
        }

        public void RegisterHashsetList<T>(IKeyValueSerializer<T> serializer = null)
            where T : class, new()
        {
            serializer ??= new KeyValueSerializer<T>();
            var serialization = new HashSetSerialization<T>(loggerFactory.CreateLogger<HashSetSerialization<T>>(), serializer);
            var persistency = new ObjectListSerialization<T>(link, serialization, new RedisSet(link, link.IndexManager), link.IndexManager);
            link.Register(persistency);
        }

        public void RegisterObjectHashSingle<T>(IDataSerializer serializer, bool isWellKnown = false)
            where T : class
        {
            if (isWellKnown &&
                RedisValueExtractor.IsPrimitive<T>())
            {
                throw new ArgumentOutOfRangeException(nameof(isWellKnown), "Primitive type can't be well known ");
            }

            var serialization = new ObjectHashSetSerialization<T>(link, serializer, isWellKnown);
            var persistency = new SingleItemSerialization<T>(loggerFactory.CreateLogger<SingleItemSerialization<T>>(),
                                                             link,
                                                             serialization,
                                                             link.IndexManager);
            link.Register(persistency);
        }

        public void RegisterObjectHashList<T>(IDataSerializer serializer, bool isWellKnown = false)
            where T : class
        {
            if (isWellKnown &&
                RedisValueExtractor.IsPrimitive<T>())
            {
                throw new ArgumentOutOfRangeException(nameof(isWellKnown), "Primitive type can't be well known ");
            }

            var serialization = new ObjectHashSetSerialization<T>(link, serializer, isWellKnown);
            var persistency = new ObjectListSerialization<T>(link, serialization, new RedisSet(link, link.IndexManager), link.IndexManager);
            link.Register(persistency);
        }
    }
}
