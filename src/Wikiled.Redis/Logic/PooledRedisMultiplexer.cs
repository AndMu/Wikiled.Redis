using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Config;
using Wikiled.Redis.Information;
using Wikiled.Redis.Serialization.Subscription;

namespace Wikiled.Redis.Logic
{
    public class PooledRedisMultiplexer : IRedisMultiplexer
    {
        public event EventHandler<EventArgs> Released;

        private readonly IRedisMultiplexer instance;

        private int counter;

        public PooledRedisMultiplexer(IRedisMultiplexer instance)
        {
            Guard.NotNull(() => instance, instance);
            this.instance = instance;
            counter = 0;
        }

        /// <summary>
        ///     Get Connection configuration - read only
        /// </summary>
        /// <returns></returns>
        public IRedisConfiguration Configuration => instance.Configuration;

        public bool IsActive => instance.IsActive;

        /// <summary>
        ///     Underlying DB
        /// </summary>
        public IDatabase Database => instance.Database;

        public IEnumerable<IServer> GetServers()
        {
            return instance.GetServers();
        }

        /// <summary>
        ///     Verify connection
        /// </summary>
        public void CheckConnection()
        {
            instance.CheckConnection();
        }

        /// <summary>
        ///     Close
        /// </summary>
        public void Close()
        {
            instance.Close();
        }

        /// <summary>
        ///     This method is not intended to be for general use and is not using key mapping
        /// </summary>
        /// <param name="pattern"></param>
        public Task DeleteKeys(string pattern)
        {
            return instance.DeleteKeys(pattern);
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref counter, 0, 1) == 0)
            {
                Released?.Invoke(this, new EventArgs());
                instance.Dispose();
            }
        }

        /// <summary>
        ///     Flush DB
        /// </summary>
        public void Flush()
        {
            instance.Flush();
        }

        /// <summary>
        ///     Get INFO from redis servers
        /// </summary>
        /// <returns></returns>
        /// <param name="section">If master null - all info</param>
        public IEnumerable<IServerInformation> GetInfo(string section = null)
        {
            return instance.GetInfo(section);
        }

        public IEnumerable<RedisKey> GetKeys(string pattern)
        {
            return instance.GetKeys(pattern);
        }

        /// <summary>
        ///     Returns a subscriber
        /// </summary>
        public ISubscriber GetSubscriber()
        {
            return instance.GetSubscriber();
        }

        public void Increment()
        {
            Interlocked.Increment(ref counter);
        }

        /// <summary>
        ///     Open
        /// </summary>
        public void Open()
        {
            instance.Open();
        }

        /// <summary>
        ///     Setup slave/master
        /// </summary>
        /// <param name="master">If master null - revert to master</param>
        public void SetupSlave(EndPoint master)
        {
            instance.SetupSlave(master);
        }

        /// <summary>
        ///     Subscribe to events
        /// </summary>
        /// <param name="mask">Subscribtion mask</param>
        /// <param name="action">Action on event</param>
        public ISubscriber SubscribeKeyEvents(string mask, Action<KeyspaceEvent> action)
        {
            return instance.SubscribeKeyEvents(mask, action);
        }
    }
}
