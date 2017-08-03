using System.Net;

namespace Wikiled.Redis.Replication
{
    public class HostStatus
    {
        public HostStatus(EndPoint endPoint, long offset)
        {
            EndPoint = endPoint;
            Offset = offset;
        }

        public EndPoint EndPoint { get; }

        public long Offset { get; }
    }
}
