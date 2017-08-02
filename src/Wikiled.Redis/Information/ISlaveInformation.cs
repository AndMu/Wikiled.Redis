using System.Net;

namespace Wikiled.Redis.Information
{
    public interface ISlaveInformation
    {
        IPEndPoint EndPoint { get; }

        string State { get; }

        long Offset { get; }

        long Lag { get; }
    }
}