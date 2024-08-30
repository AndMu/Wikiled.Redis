using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Wikiled.Redis.Config;
using Wikiled.Redis.Logic;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using StackExchange.Redis;

namespace Wikiled.Redis.UnitTests.Logic
{
    [TestFixture]
    public class RedisMultiplexerTests
    {
        private RedisConfiguration option;

        private RedisMultiplexer multiplexer;

        private Func<ConfigurationOptions, Task<IConnectionMultiplexer>> multiplexerFactory;

        private Mock<IConnectionMultiplexer> connectionMultiplexer;

        private Mock<IServer> server;

        private Mock<IDatabase> database;

        [SetUp]
        public void Setup()
        {
            database = new Mock<IDatabase>();
            server = new Mock<IServer>();
            connectionMultiplexer = new Mock<IConnectionMultiplexer>();
            connectionMultiplexer.Setup(item => item.GetServer(It.IsAny<EndPoint>(), null)).Returns(server.Object);
            server.Setup(item => item.Multiplexer).Returns(connectionMultiplexer.Object);
            connectionMultiplexer.Setup(item => item.GetDatabase(-1, null)).Returns(database.Object);
            multiplexerFactory = options => Task.FromResult(connectionMultiplexer.Object);
            option = new RedisConfiguration("Test");
            option.Endpoints = new[] { new RedisEndpoint { Host = "localhost", Port = 7000 } };
            multiplexer = new RedisMultiplexer(new NullLogger<RedisMultiplexer>(), option, multiplexerFactory);
        }

        [Test]
        public void Construct()
        {
            ClassicAssert.Throws<ArgumentNullException>(() => new RedisMultiplexer(new NullLogger<RedisMultiplexer>(), null, multiplexerFactory));
            ClassicAssert.Throws<ArgumentNullException>(() => new RedisMultiplexer(null, option, multiplexerFactory));
            ClassicAssert.Throws<ArgumentNullException>(() => new RedisMultiplexer(new NullLogger<RedisMultiplexer>(), option, null));
            ClassicAssert.IsNotNull(multiplexer);
            ClassicAssert.IsNull(multiplexer.Database);
            ClassicAssert.IsFalse(multiplexer.UsingSentinel);
        }

        [Test]
        public void Configuration()
        {
            ClassicAssert.IsNotNull(multiplexer.Configuration);
            ClassicAssert.AreEqual("Unspecified/localhost:7000", multiplexer.Configuration.GetOptions().EndPoints[0].ToString());
        }

        [Test]
        public void OpenNoEndPoints()
        {
            ClassicAssert.ThrowsAsync<RedisConnectionException>(multiplexer.Open);
        }

        [Test]
        public async Task OpenSimple()
        {
            connectionMultiplexer.Setup(item => item.GetEndPoints(false)).Returns(new EndPoint[] { new DnsEndPoint("localhost", 6377) });
            server.Setup(item => item.ServerType).Returns(ServerType.Standalone);
            server.Setup(item => item.IsSlave).Returns(false);
            await multiplexer.Open();
            ClassicAssert.AreEqual(database.Object, multiplexer.Database);
        }

        [Test]
        public void OpenSimpleSlave()
        {
            connectionMultiplexer.Setup(item => item.GetEndPoints(false)).Returns(new EndPoint[] { new DnsEndPoint("localhost", 6377) });
            server.Setup(item => item.ServerType).Returns(ServerType.Standalone);
            server.Setup(item => item.IsSlave).Returns(true);
            ClassicAssert.ThrowsAsync<RedisConnectionException>(multiplexer.Open);
        }

        [Test]
        public void OpenSentinelCircular()
        {
            connectionMultiplexer.Setup(item => item.GetEndPoints(false)).Returns(new EndPoint[] { new DnsEndPoint("localhost", 6377) });
            server.Setup(item => item.ServerType).Returns(ServerType.Sentinel);
            ClassicAssert.ThrowsAsync<RedisConnectionException>(multiplexer.Open);
        }

        [Test]
        public async Task OpenSentinel()
        {
            connectionMultiplexer.Setup(item => item.GetEndPoints(false)).Returns(new EndPoint[] { new DnsEndPoint("localhost", 6377) });
            server.SetupSequence(item => item.ServerType).Returns(ServerType.Sentinel).Returns(ServerType.Standalone);
            server.Setup(item => item.IsSlave).Returns(false);

            server.Setup(item => item.SentinelMasters(CommandFlags.None))
                  .Returns(
                      new[]
                      {
                          new[]
                          {
                              new KeyValuePair<string, string>("name", "Test"),
                              new KeyValuePair<string, string>("ip", "localhost"),
                              new KeyValuePair<string, string>("port", "6379"),
                          }
                      });

            await multiplexer.Open();
            ClassicAssert.AreEqual(database.Object, multiplexer.Database);
            connectionMultiplexer.Verify(item => item.Close(true));
        }

        [Test]
        public void CheckConnection()
        {
            ClassicAssert.Throws<InvalidOperationException>(multiplexer.CheckConnection);
        }
    }
}
