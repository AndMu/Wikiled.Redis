using System;
using System.Net;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Replication;
using Wikiled.Redis.Scripts;
using Wikiled.Redis.Serialization;
using Wikiled.Redis.Serialization.Subscription;

namespace Wikiled.Redis.Logic
{
    public interface IRedisLink : IChannel
    {
        /// <summary>
        ///     Redis client
        /// </summary>
        IRedisClient Client { get; }

        /// <summary>
        ///     Redis database
        /// </summary>
        IDatabase Database { get; }

        /// <summary>
        ///     Script Generator
        /// </summary>
        IScriptGenerator Generator { get; }

        /// <summary>
        ///     Connection ID
        /// </summary>
        long LinkId { get; }

        /// <summary>
        ///     Raw DB multiplexer
        /// </summary>
        IRedisMultiplexer Multiplexer { get; }

        /// <summary>
        ///     Register generic
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer"></param>
        /// <returns></returns>
        HandlingDefinition<T> ConstructGeneric<T>(IDataSerializer serializer = null) where T : class;

        /// <summary>
        ///     Get handling definition
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>handling definition</returns>
        HandlingDefinition<T> GetDefinition<T>();

        ISpecificPersistency GetSpecific<T>();

        /// <summary>
        ///     Resolve type by name
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Type GetTypeByName(string id);

        /// <summary>
        ///     Get Type id
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        string GetTypeID(Type type);

        /// <summary>
        ///     Has registered definition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        bool HasDefinition<T>();

        /// <summary>
        ///     Register type which is persisted as hash
        /// </summary>
        /// <typeparam name="T"></typeparam>
        HandlingDefinition<T> RegisterHashType<T>(IKeyValueSerializer<T> serializer = null) where T : class, new();

        /// <summary>
        ///     Register known type for which we don't have to serialize information
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        HandlingDefinition<T> RegisterWellknown<T>(IDataSerializer serializer = null) where T : class;

        /// <summary>
        ///     Get client which performs actions in transaction
        /// </summary>
        /// <returns></returns>
        IRedisTransaction StartTransaction();

        /// <summary>
        ///     Subscribe to key events
        /// </summary>
        /// <param name="key">Subscription key</param>
        /// <param name="action">Action on event</param>
        ISubscriber SubscribeKeyEvents(IDataKey key, Action<KeyspaceEvent> action);

        /// <summary>
        ///     IF supported create subscription based o
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        ISubscriber SubscribeTypeEvents<T>(Action<KeyspaceEvent> action) where T : class;
    }
}
