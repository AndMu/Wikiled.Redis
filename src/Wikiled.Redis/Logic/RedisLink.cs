using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wikiled.Common.Reflection;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Config;
using Wikiled.Redis.Data;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic.Resilience;
using Wikiled.Redis.Persistency;
using Wikiled.Redis.Scripts;
using Wikiled.Redis.Serialization;
using Wikiled.Redis.Serialization.Subscription;
using IDataKey = Wikiled.Redis.Keys.IDataKey;

namespace Wikiled.Redis.Logic
{
    public class RedisLink : BaseChannel, IRedisLink
    {
        private readonly ILoggerFactory loggerFactory;

        private readonly ILogger<RedisLink> log;

        private readonly Dictionary<Type, object> persistencyTable = new Dictionary<Type, object>();

        private readonly ConcurrentDictionary<Type, string> typeIdTable = new ConcurrentDictionary<Type, string>();

        private readonly ConcurrentDictionary<string, Type> typeNameTable = new ConcurrentDictionary<string, Type>();

        private readonly IDataSerializer defaultSerialiser; 

        public RedisLink(ILoggerFactory loggerFactory,
                         IRedisConfiguration configuration,
                         IRedisMultiplexer multiplexer,
                         IResilience resilience,
                         IEntitySubscriber entitySubscriber,
                         IDataSerializer defaultSerialiser)
            : base(configuration?.ServiceName)
        {
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            Multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));

            Resilience = resilience ?? throw new ArgumentNullException(nameof(resilience));
            EntitySubscriber = entitySubscriber;
            this.defaultSerialiser = defaultSerialiser ?? throw new ArgumentNullException(nameof(defaultSerialiser));
            log = loggerFactory.CreateLogger<RedisLink>();
            Generator = new ScriptGenerator();
            IndexManager = new MainIndexManager(new IndexManagerFactory(loggerFactory, this));
            Client = new RedisClient(loggerFactory?.CreateLogger<RedisClient>(), this, IndexManager);
            PersistencyRegistration = new PersistencyRegistrationHandler(loggerFactory, this);
        }

        public IMainIndexManager IndexManager { get; }

        public IPersistencyRegistrationHandler PersistencyRegistration { get; }

        public IRedisClient Client { get; }

        public IResilience Resilience { get; }

        public IDatabase Database => Multiplexer.Database;

        public IScriptGenerator Generator { get; }

        public long LinkId { get; private set; }

        public IRedisMultiplexer Multiplexer { get; }

        public IEntitySubscriber EntitySubscriber { get; }

        protected override ILogger Logger => log;

        public void Register<T>(ISpecificPersistency<T> persistency)
        {
            if (persistency == null)
            {
                throw new ArgumentNullException(nameof(persistency));
            }

            persistencyTable.Add(typeof(T), persistency);
        }

        public ISpecificPersistency<T> GetSpecific<T>()
        {
            Type type = typeof(T);
            log.LogDebug("GetSpecific<{0}>", type);

            if (persistencyTable.TryGetValue(type, out var persistency))
            {
                return (ISpecificPersistency<T>)persistency;
            }

            if (type == typeof(SortedSetEntry))
            {
                log.LogDebug("Creating default sorted set persistency handler");
                persistency = new SortedSetSerialization<T>(loggerFactory.CreateLogger<SortedSetSerialization<T>>(), this, IndexManager);
            }
            else
            {
                log.LogDebug("Creating default list persistency handler");
                persistency = new ListSerialization<T>(loggerFactory.CreateLogger<ListSerialization<T>>(),
                                                     this,
                                                     new RedisList(loggerFactory.CreateLogger<RedisList>(), this, IndexManager),
                                                     IndexManager,
                                                     defaultSerialiser);
            }

            persistencyTable.Add(type, persistency);
            return (ISpecificPersistency<T>) persistency;
        }

        public Type GetTypeByName(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Type is most likely saved as WellKnown!");
            }

            if (string.IsNullOrEmpty(id) || !typeNameTable.TryGetValue(id, out var type))
            {
                throw new Exception($"Type not found: {id}");
            }

            return type;
        }

        public string GetTypeID(Type type)
        {
            log.LogDebug("Resolving {0}", type);
            if (typeIdTable.TryGetValue(type, out var typeName))
            {
                return typeName;
            }

            typeName = type.GetTypeName();
            var typeKey = this.GetKey(new SimpleKey("Type", type.Name));
            var keys = Multiplexer.Database.SetMembers(typeKey);
            if (keys.Length > 0)
            {
                if (keys.Length > 1)
                {
                    log.LogWarning("Too many types found: {0} for type: {1}", keys.Length, type);
                }

                log.LogDebug("Type found in Redis [{0}:{1}]", type, keys[0]);
                typeIdTable[type] = keys[0];
                typeNameTable[keys[0]] = type;
                return keys[0];
            }

            log.LogDebug("Registering new type");
            var counterKey = this.GetKey(new SimpleKey("Type", "Counter"));

            var id = Multiplexer.Database.StringIncrement(counterKey);
            var typeIdKey = new SimpleKey("Type", id.ToString());

            var batch = Multiplexer.Database.CreateBatch();
            batch.SetAddAsync(this.GetKey(typeIdKey), typeName);
            batch.SetAddAsync(typeKey, typeIdKey.FullKey);
            batch.Execute();
            log.LogDebug("Key added: {0} for type: {1}", typeName, type);
            typeIdTable[type] = typeIdKey.FullKey;
            typeNameTable[typeIdKey.FullKey] = type;
            return typeIdKey.FullKey;
        }

        public IRedisTransaction StartTransaction()
        {
            log.LogDebug("StartTransaction");
            Multiplexer.CheckConnection();
            return new RedisTransaction(loggerFactory, this, Multiplexer.Database.CreateTransaction(), IndexManager);
        }

        public ISubscriber SubscribeKeyEvents(IDataKey key, Action<KeyspaceEvent> action)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var typeKey = this.GetKey(key);
            return Multiplexer.SubscribeKeyEvents(typeKey, action);
        }

        public ISubscriber SubscribeTypeEvents<T>(Action<KeyspaceEvent> action)
            where T : class
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (!(GetSpecific<T>() is ObjectListSerialization<T> persistence))
            {
                log.LogWarning("Type persistence not supported");
                return null;
            }

            var typeKey = this.GetKey(new ObjectKey(persistence.GetKeyPrefix(), "*"));
            return Multiplexer.SubscribeKeyEvents(typeKey, action);
        }

        protected override void CloseInternal()
        {
            base.CloseInternal();
            Multiplexer.Close();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Multiplexer.Dispose();
            }
        }

        protected override async Task<ChannelState> OpenInternal()
        {
            log.LogDebug("OpenInternal: {0}", Multiplexer.Configuration);
            try
            {
                await Multiplexer.Open().ConfigureAwait(false);
                var key = this.GetKey(new SimpleKey("Connection", "Counter"));
                LinkId = await Multiplexer.Database.StringIncrementAsync(key).ConfigureAwait(false);
                log.LogDebug("Link initialized with ID:{0}", LinkId);
            }
            catch (Exception e)
            {
                log.LogError(e, "Error");
                Multiplexer.Close();
                throw;
            }

            return await base.OpenInternal().ConfigureAwait(false);
        }
    }
}
