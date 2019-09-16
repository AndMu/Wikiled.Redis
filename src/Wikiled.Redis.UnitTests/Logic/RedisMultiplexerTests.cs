using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Wikiled.Redis.Config;
using Wikiled.Redis.Logic;
using NUnit.Framework;
using StackExchange.Redis;

namespace Wikiled.Redis.UnitTests.Logic
{
    [TestFixture]
    public class RedisMultiplexerTests
    {
        private RedisConfiguration option;

        private RedisMultiplexer multiplexer;

        private Func<ConfigurationOptions, IConnectionMultiplexer> multiplexerFactory;

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
            multiplexerFactory = options => connectionMultiplexer.Object;
            option = new RedisConfiguration("Test");
            option.Endpoints = new[] { new RedisEndpoint { Host = "localhost", Port = 7000 } };
            multiplexer = new RedisMultiplexer(new NullLogger<RedisMultiplexer>(), option, multiplexerFactory);
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new RedisMultiplexer(new NullLogger<RedisMultiplexer>(), null, multiplexerFactory));
            Assert.Throws<ArgumentNullException>(() => new RedisMultiplexer(null, option, multiplexerFactory));
            Assert.Throws<ArgumentNullException>(() => new RedisMultiplexer(new NullLogger<RedisMultiplexer>(), option, null));
            Assert.IsNotNull(multiplexer);
            Assert.IsNull(multiplexer.Database);
            Assert.IsFalse(multiplexer.UsingSentinel);
        }

        [Test]
        public void Configuration()
        {
            Assert.IsNotNull(multiplexer.Configuration);
            Assert.AreEqual("Unspecified/localhost:7000", multiplexer.Configuration.GetOptions().EndPoints[0].ToString());
        }

        [Test]
        public void OpenNoEndPoints()
        {
            Assert.Throws<RedisConnectionException>(() => multiplexer.Open());
        }

        [Test]
        public void OpenSimple()
        {
            connectionMultiplexer.Setup(item => item.GetEndPoints(false)).Returns(new EndPoint[] { new DnsEndPoint("localhost", 6377) });
            server.Setup(item => item.ServerType).Returns(ServerType.Standalone);
            server.Setup(item => item.IsSlave).Returns(false);
            multiplexer.Open();
            Assert.AreEqual(database.Object, multiplexer.Database);
        }

        [Test]
        public void OpenSimpleSlave()
        {
            connectionMultiplexer.Setup(item => item.GetEndPoints(false)).Returns(new EndPoint[] { new DnsEndPoint("localhost", 6377) });
            server.Setup(item => item.ServerType).Returns(ServerType.Standalone);
            server.Setup(item => item.IsSlave).Returns(true);
            Assert.Throws<RedisConnectionException>(() => multiplexer.Open());
        }

        [Test]
        public void OpenSentinelCircular()
        {
            connectionMultiplexer.Setup(item => item.GetEndPoints(false)).Returns(new EndPoint[] { new DnsEndPoint("localhost", 6377) });
            server.Setup(item => item.ServerType).Returns(ServerType.Sentinel);
            Assert.Throws<RedisConnectionException>(() => multiplexer.Open());
        }

        [Test]
        public void OpenSentinel()
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

            multiplexer.Open();
            Assert.AreEqual(database.Object, multiplexer.Database);
            connectionMultiplexer.Verify(item => item.Close(true));
        }

        [Test]
        public void CheckConnection()
        {
            Assert.Throws<InvalidOperationException>(multiplexer.CheckConnection);
        }
    }
}
