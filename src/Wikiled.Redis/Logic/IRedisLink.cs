using System;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Keys;
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
        /// Register type definition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="definition"></param>
        void RegisterDefinition<T>(HandlingDefinition<T> definition) where T : class;

        /// <summary>
        ///     Has registered definition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        bool HasDefinition<T>();

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
