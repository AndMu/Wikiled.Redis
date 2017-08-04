using System.Collections.Generic;
using System.Net;
using Moq;
using StackExchange.Redis;
using Wikiled.Redis.Information;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.UnitTests.Replication
{
    public class ReplicationTestManager
    {
        private readonly List<IServerInformation> serverInformations;

        public ReplicationTestManager()
        {
            serverInformations = new List<IServerInformation>();
            ClientAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6666);
            Master = new Mock<IRedisMultiplexer>();
            Master.Setup(item => item.IsActive).Returns(true);
            Slave = new Mock<IRedisMultiplexer>();
            Slave.Setup(item => item.IsActive).Returns(true);
            var client = new Mock<IServer>();
            client.Setup(item => item.EndPoint).Returns(ClientAddress);
            Slave.Setup(item => item.GetServers()).Returns(new[] {client.Object});
            Master.Setup(item => item.GetServers()).Returns(new[] {client.Object});
            Master.Setup(item => item.GetInfo(ReplicationInfo.Name)).Returns(serverInformations);
        }

        public IPEndPoint ClientAddress { get; }

        public Mock<IRedisMultiplexer> Master { get; }

        public Mock<IRedisMultiplexer> Slave { get; }

        public Mock<IReplicationInfo> SetupReplication()
        {
            Mock<IServerInformation> information = new Mock<IServerInformation>();
            Mock<IReplicationInfo> replicationInfo = new Mock<IReplicationInfo>();
            information.Setup(item => item.Replication).Returns(replicationInfo.Object);
            information.Setup(item => item.Server).Returns(new IPEndPoint(IPAddress.Any, 1717));
            serverInformations.Add(information.Object);
            replicationInfo.Setup(item => item.Role).Returns(ReplicationRole.Master);
            return replicationInfo;
        }
    }
}
