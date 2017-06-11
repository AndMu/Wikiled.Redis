using System.Collections.Generic;
using System.Net;

namespace Wikiled.Redis.Information
{
    public interface IServerInformation
    {
        Dictionary<string, Dictionary<string, string>> RawData { get; }

        EndPoint Server { get; }

        IMemoryInfo Memory { get; }

        IPersistenceInfo Persistence { get; }

        IReplicationInfo Replication { get; }

        IStatsInfo Stats { get; }
    }
}