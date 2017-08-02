using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using NLog;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Core.Utility.Extensions;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Persistency;
using Wikiled.Redis.Replication;
using Wikiled.Redis.Scripts;
using Wikiled.Redis.Serialization;
using Wikiled.Redis.Serialization.Subscription;

namespace Wikiled.Redis.Logic
{
    public class RedisLink : BaseChannel, IRedisLink
    {
        private readonly ConcurrentDictionary<Type, ISpecificPersistency> addRecordActions =
            new ConcurrentDictionary<Type, ISpecificPersistency>();

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<Type, object> typeHandler = new Dictionary<Type, object>();

        private readonly ConcurrentDictionary<Type, string> typeIdTable = new ConcurrentDictionary<Type, string>();

        private readonly ConcurrentDictionary<string, Type> typeNameTable = new ConcurrentDictionary<string, Type>();

        public RedisLink(string name, IRedisMultiplexer multiplexer)
            : base(name)
        {
            Guard.NotNull(() => multiplexer, multiplexer);
            Multiplexer = multiplexer;
            Generator = new ScriptGenerator();
            Client = new RedisClient(this);
        }

        public IRedisClient Client { get; }

        public IDatabase Database => Multiplexer.Database;

        public IScriptGenerator Generator { get; }

        public long LinkId { get; private set; }

        public IRedisMultiplexer Multiplexer { get; }

        public HandlingDefinition<T> ConstructGeneric<T>(IDataSerializer serializer = null)
            where T : class
        {
            log.Info("ConstructGeneric<{0}>", typeof(T));
            var definition = HandlingDefinition<T>.ConstructGeneric(this, serializer);
            RegisterDefinition(definition);
            return definition;
        }

        public HandlingDefinition<T> GetDefinition<T>()
        {
            Type type = typeof(T);
            object definition;
            if (!typeHandler.TryGetValue(type, out definition))
            {
                definition = HandlingDefinition<T>.ConstructGeneric(this);
                typeHandler[type] = definition;
            }

            return (HandlingDefinition<T>)definition;
        }

        public ISpecificPersistency GetSpecific<T>()
        {
            Type type = typeof(T);
            ISpecificPersistency action;
            log.Debug("GetSpecific<{0}>", type);
            if (addRecordActions.TryGetValue(type, out action))
            {
                return action;
            }

            var definition = GetDefinition<T>();
            var setList = definition.IsSet ? (IRedisSetList)new RedisSet(this) : new RedisList(this);
            if (typeof(T) == typeof(SortedSetEntry))
            {
                action = new SortedSetSerialization(this);
            }
            else if (definition.Serializer != null)
            {
                var serialization = new HashSetSerialization(this);
                action = definition.IsSingleInstance
                             ? (ISpecificPersistency)new SingleItemSerialization(this, serialization)
                             : new ObjectListSerialization(this, serialization, setList);
            }
            else if (definition.IsWellKnown)
            {
                var serialization = new ObjectHashSetSerialization(this, definition.DataSerializer);
                action = definition.IsSingleInstance
                             ? (ISpecificPersistency)new SingleItemSerialization(this, serialization)
                             : new ObjectListSerialization(this, serialization, setList);
            }
            else if (!definition.IsSingleInstance &&
                    !definition.ExtractType)
            {
                action = new ListSerialization(this, setList);
            }
            else
            {
                var serialization = new ObjectHashSetSerialization(this, definition.DataSerializer);
                action = definition.IsSingleInstance
                             ? new SingleItemSerialization(this, serialization)
                             : (ISpecificPersistency)new ObjectListSerialization(this, serialization, setList);
            }

            log.Debug("GetSpecific<{0}> - Constructed - {1}", type, action);
            addRecordActions[type] = action;
            return action;
        }

        public Type GetTypeByName(string id)
        {
            Guard.NotNullOrEmpty(() => id, id);
            Type type = null;
            if (string.IsNullOrEmpty(id) ||
               !typeNameTable.TryGetValue(id, out type))
            {
                log.Error("Type not found: {0}", id);
            }

            return type;
        }

        public string GetTypeID(Type type)
        {
            log.Debug("Resolving {0}", type);
            string typeName;
            if (typeIdTable.TryGetValue(type, out typeName))
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
                    log.Warn("Too many types found: {0} for type: {1}", keys.Length, type);
                }

                log.Debug("Type found in Redis {0}:{1}]", type, keys[0]);
                typeIdTable[type] = keys[0];
                typeNameTable[keys[0]] = type;
                return keys[0];
            }

            log.Debug("Registering new type");
            var counterKey = this.GetKey(new SimpleKey("Type", "Counter"));

            var id = Multiplexer.Database.StringIncrement(counterKey);
            var typeIdKey = new SimpleKey("Type", id.ToString());

            var batch = Multiplexer.Database.CreateBatch();
            batch.SetAddAsync(this.GetKey(typeIdKey), typeName);
            batch.SetAddAsync(typeKey, typeIdKey.FullKey);
            batch.Execute();
            log.Debug("Key added: {0} for type: {1}", typeName, type);
            typeIdTable[type] = typeIdKey.FullKey;
            typeNameTable[typeIdKey.FullKey] = type;
            return typeIdKey.FullKey;
        }

        public bool HasDefinition<T>()
        {
            Type type = typeof(T);
            return typeHandler.ContainsKey(type);
        }

        public HandlingDefinition<T> RegisterHashType<T>(IKeyValueSerializer<T> serializer = null)
            where T : class, new()
        {
            log.Info("RegisterHashType<{0}>", typeof(T));
            serializer = serializer ?? new KeyValueSerializer<T>(() => new T());
            var definition = HandlingDefinition<T>
                .ConstructKeyValue(this, serializer);
            RegisterDefinition(definition);
            return definition;
        }

        public HandlingDefinition<T> RegisterWellknown<T>(IDataSerializer serializer = null)
            where T : class
        {
            log.Info("RegisterWellknown<{0}>", typeof(T));
            var definition = HandlingDefinition<T>.ConstructWellKnown(this, serializer);
            RegisterDefinition(definition);
            return definition;
        }

        public IReplicationManager SetupReplicationFrom(IPEndPoint master)
        {
            Guard.NotNull(() => master, master);
            return new ReplicationManager(new SimpleRedisFactory(), master, Multiplexer, TimeSpan.FromSeconds(1));
        }

        public IRedisTransaction StartTransaction()
        {
            log.Debug("StartTransaction");
            Multiplexer.CheckConnection();
            return new RedisTransaction(this, Multiplexer.Database.CreateTransaction());
        }

        public ISubscriber SubscribeKeyEvents(IDataKey key, Action<KeyspaceEvent> action)
        {
            Guard.NotNull(() => key, key);
            Guard.NotNull(() => action, action);
            var typeKey = this.GetKey(key);
            return Multiplexer.SubscribeKeyEvents(typeKey, action);
        }

        public ISubscriber SubscribeTypeEvents<T>(Action<KeyspaceEvent> action)
            where T : class
        {
            Guard.NotNull(() => action, action);
            var persistency = GetSpecific<T>() as ObjectListSerialization;
            if (persistency == null)
            {
                log.Warn("Type persitency not supported");
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
            log.Debug("OpenInternal: {0}", Multiplexer.Configuration);
            try
            {
                Multiplexer.Open();
                var key = this.GetKey(new SimpleKey("Connection", "Counter"));
                LinkId = Multiplexer.Database.StringIncrement(key);
                log.Debug("Link initialized with ID:{0}", LinkId);
            }
            catch (Exception e)
            {
                log.Error(e);
                throw;
            }

            return base.OpenInternal();
        }

        private void RegisterDefinition<T>(HandlingDefinition<T> definition) where T : class
        {
            Type type = typeof(T);
            if (typeHandler.ContainsKey(type))
            {
                throw new PersistencyException("Type was already initialized");
            }

            typeHandler[type] = definition;
        }

        public object ToArray()
        {
            throw new NotImplementedException();
        }
    }
}
