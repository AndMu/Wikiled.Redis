using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using StackExchange.Redis;

namespace Wikiled.Redis.Information
{
    public class ServerInformation : IServerInformation
    {
        public ServerInformation(IServer server, IGrouping<string, KeyValuePair<string, string>>[] info)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            Server = server.EndPoint;
            RawData = info.ToDictionary(item => item.Key, item => item.ToDictionary(x => x.Key, x => x.Value));
            Memory = new MemoryInfo(this);
            Persistence = new PersistenceInfo(this);
            Stats = new StatsInfo(this);
            Replication = new ReplicationInfo(this);
        }

        public IMemoryInfo Memory { get; }

        public IPersistenceInfo Persistence { get; }

        public Dictionary<string, Dictionary<string, string>> RawData { get; }

        public IReplicationInfo Replication { get; }

        public EndPoint Server { get; }

        public IStatsInfo Stats { get; }
    }
}
