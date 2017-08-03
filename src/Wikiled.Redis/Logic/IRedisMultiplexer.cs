using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Redis.Config;
using Wikiled.Redis.Information;
using Wikiled.Redis.Serialization.Subscription;

namespace Wikiled.Redis.Logic
{
    public interface IRedisMultiplexer : IDisposable
    {
        /// <summary>
        ///     Get Connection configuration - read only
        /// </summary>
        /// <returns></returns>
        IRedisConfiguration Configuration { get; }

        /// <summary>
        /// Is Active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        ///     Underlying DB
        /// </summary>
        IDatabase Database { get; }

        /// <summary>
        /// Get Servers
        /// </summary>
        /// <returns></returns>
        IEnumerable<IServer> GetServers();

        /// <summary>
        ///     Verify connection
        /// </summary>
        void CheckConnection();

        /// <summary>
        ///     Close
        /// </summary>
        void Close();

        /// <summary>
        ///     This method is not intended to be for general use and is not using key mapping
        /// </summary>
        /// <param name="pattern"></param>
        Task DeleteKeys(string pattern);

        /// <summary>
        ///     Flush DB
        /// </summary>
        void Flush();

        /// <summary>
        ///     Get INFO from redis servers
        /// </summary>
        /// <returns></returns>
        /// <param name="section">If master null - all info</param>
        IEnumerable<IServerInformation> GetInfo(string section = null);

        /// <summary>
        ///     Dagerous get keys
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        IEnumerable<RedisKey> GetKeys(string pattern);

        /// <summary>
        ///     Returns a subscriber
        /// </summary>
        ISubscriber GetSubscriber();

        /// <summary>
        ///     Open
        /// </summary>
        void Open();

        /// <summary>
        ///     Setup slave/master
        /// </summary>
        /// <param name="master">If master null - revert to master</param>
        void SetupSlave(EndPoint master);

        /// <summary>
        ///     Subscribe to key events
        /// </summary>
        /// <param name="key">Subscription key</param>
        /// <param name="action">Action on event</param>
        ISubscriber SubscribeKeyEvents(string key, Action<KeyspaceEvent> action);
    }
}
