using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Replication
{
    public interface IReplicationFactory
    {
        Task<ReplicationProgress> Replicate(DnsEndPoint master, DnsEndPoint slave, CancellationToken token);

        Task<IReplicationManager> StartReplicationFrom(IRedisMultiplexer master, IRedisMultiplexer slave);
    }
}