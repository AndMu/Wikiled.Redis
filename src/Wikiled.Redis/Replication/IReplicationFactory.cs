using System.Net;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Replication
{
    public interface IReplicationFactory
    {
        IReplicationManager StartReplicationFrom(IRedisMultiplexer master, IRedisMultiplexer slave);
    }
}