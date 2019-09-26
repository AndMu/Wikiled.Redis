using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Wikiled.Common.Reflection;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Config;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.Keys;
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

        private readonly ConcurrentDictionary<Type, ISpecificPersistency> addRecordActions = new ConcurrentDictionary<Type, ISpecificPersistency>();
        
        private readonly Dictionary<Type, object> typeHandler = new Dictionary<Type, object>();

        private readonly ConcurrentDictionary<Type, string> typeIdTable = new ConcurrentDictionary<Type, string>();

        private readonly ConcurrentDictionary<string, Type> typeNameTable = new ConcurrentDictionary<string, Type>();

        private readonly IMainIndexManager mainIndexManager;

        public RedisLink(ILoggerFactory loggerFactory,
                         IRedisConfiguration configuration,
                         IRedisMultiplexer multiplexer,
                         IHandlingDefinitionFactory handlingDefinitionFactory)
            : base(configuration?.ServiceName)
        {
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            Multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));

            DefinitionFactory = handlingDefinitionFactory ?? throw new ArgumentNullException(nameof(handlingDefinitionFactory));
            log = loggerFactory.CreateLogger<RedisLink>();
            Generator = new ScriptGenerator();
            mainIndexManager = new MainIndexManager(new IndexManagerFactory(this));
            Client = new RedisClient(loggerFactory?.CreateLogger<RedisClient>(), this, mainIndexManager);
        }

        public IRedisClient Client { get; }

        public IHandlingDefinitionFactory DefinitionFactory { get; }

        public IDatabase Database => Multiplexer.Database;

        public IScriptGenerator Generator { get; }

        public long LinkId { get; private set; }

        public IRedisMultiplexer Multiplexer { get; }

        protected override ILogger Logger => log;

        public HandlingDefinition<T> GetDefinition<T>()
        {
            Type type = typeof(T);
            if (!typeHandler.TryGetValue(type, out var definition))
            {
                definition = DefinitionFactory.ConstructGeneric<T>(this);
                typeHandler[type] = definition;
            }

            return (HandlingDefinition<T>)definition;
        }

        public ISpecificPersistency GetSpecific<T>()
        {
            Type type = typeof(T);
            log.LogDebug("GetSpecific<{0}>", type);

            if (addRecordActions.TryGetValue(type, out var action))
            {
                return action;
            }

            var definition = GetDefinition<T>();
            var setList = definition.IsSet ? (IRedisSetList) new RedisSet(this, mainIndexManager) : new RedisList(this, mainIndexManager);

            if (typeof(T) == typeof(SortedSetEntry))
            {
                action = new SortedSetSerialization(this, mainIndexManager);
            }
            else if (definition.Serializer != null)
            {
                var serialization = new HashSetSerialization(this);

                action = definition.IsSingleInstance
                    ? (ISpecificPersistency) new SingleItemSerialization(this, serialization, mainIndexManager)
                    : new ObjectListSerialization(this, serialization, setList, mainIndexManager);
            }
            else if (!definition.IsSingleInstance &&
                     !definition.ExtractType &&
                     !definition.IsNormalized)
            {
                action = new ListSerialization(this, setList);
            }
            else
            {
                var serialization = new ObjectHashSetSerialization(this, definition.DataSerializer);

                action = definition.IsSingleInstance
                    ? new SingleItemSerialization(this, serialization, mainIndexManager)
                    : (ISpecificPersistency) new ObjectListSerialization(this, serialization, setList, mainIndexManager);
            }

            log.LogDebug("GetSpecific<{0}> - Constructed - {1}", type, action);
            addRecordActions[type] = action;

            return action;
        }

        public Type GetTypeByName(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(id));
            }

            Type type = null;
            if (string.IsNullOrEmpty(id) || !typeNameTable.TryGetValue(id, out type))
            {
                log.LogError("Type not found: {0}", id);
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

        public bool HasDefinition<T>()
        {
            Type type = typeof(T);
            return typeHandler.ContainsKey(type);
        }

        public IRedisTransaction StartTransaction()
        {
            log.LogDebug("StartTransaction");
            Multiplexer.CheckConnection();
            return new RedisTransaction(loggerFactory, this, Multiplexer.Database.CreateTransaction(), mainIndexManager);
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

            if (!(GetSpecific<T>() is ObjectListSerialization persistency))
            {
                log.LogWarning("Type persitency not supported");
                return null;
            }

            var typeKey = this.GetKey(new ObjectKey(persistency.GetKeyPrefix<T>(), "*"));
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

        protected override ChannelState OpenInternal()
        {
            log.LogDebug("OpenInternal: {0}", Multiplexer.Configuration);
            try
            {
                Multiplexer.Open();
                var key = this.GetKey(new SimpleKey("Connection", "Counter"));
                LinkId = Multiplexer.Database.StringIncrement(key);
                log.LogDebug("Link initialized with ID:{0}", LinkId);
            }
            catch (Exception e)
            {
                log.LogError(e, "Error");
                Multiplexer.Close();
                throw;
            }

            return base.OpenInternal();
        }

        public void RegisterDefinition<T>(HandlingDefinition<T> definition)
            where T : class
        {
            Type type = typeof(T);
            if (typeHandler.ContainsKey(type))
            {
                throw new PersistencyException("Type was already initialized");
            }

            typeHandler[type] = definition;
        }
    }
}
