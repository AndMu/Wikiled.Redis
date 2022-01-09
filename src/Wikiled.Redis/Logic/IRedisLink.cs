using System;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic.Resilience;
using Wikiled.Redis.Persistency;
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

        IResilience Resilience { get; }

        IMainIndexManager IndexManager { get; }

        IPersistencyRegistrationHandler PersistencyRegistration { get; }

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

        IEntitySubscriber EntitySubscriber { get; }

        void Register<T>(ISpecificPersistency<T> persistency);

        ISpecificPersistency<T> GetSpecific<T>();

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
