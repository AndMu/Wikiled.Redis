using System;
using System.Linq;
using System.Net;
using Wikiled.Common.Arguments;

namespace Wikiled.Redis.Information
{
    /// <summary>
    /// ip=127.0.0.1,port=6027,state=online,offset=239,lag=0
    /// </summary>
    public class SlaveInformation : ISlaveInformation
    {
        private SlaveInformation()
        {
        }

        public static SlaveInformation Parse(string line)
        {
            Guard.NotNullOrEmpty(() => line, line);
            var blocks = line.Split(',').Select(item => item.Split('='))
                .ToDictionary(item => item[0], item => item[1], StringComparer.OrdinalIgnoreCase);
            if (blocks.Count < 5)
            {
                throw new ArgumentOutOfRangeException(nameof(line), "Invalid information: " + line);
            }

            SlaveInformation information = new SlaveInformation();
            information.EndPoint = new IPEndPoint(IPAddress.Parse(blocks["ip"]), int.Parse(blocks["port"]));
            information.State = blocks["state"];
            information.Offset = long.Parse(blocks["offset"]);
            information.Lag = int.Parse(blocks["lag"]);
            return information;
        }

        public IPEndPoint EndPoint { get; private set; }

        public string State { get; private set; }

        public long Offset { get; private set; }

        public long Lag { get; private set; }

    }
}
