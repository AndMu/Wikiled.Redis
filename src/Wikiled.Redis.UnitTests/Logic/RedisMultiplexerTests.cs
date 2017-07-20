using System;
using Wikiled.Redis.Config;
using Wikiled.Redis.Logic;
using NUnit.Framework;

namespace Wikiled.Redis.UnitTests.Logic
{
    [TestFixture]
    public class RedisMultiplexerTests
    {
        private RedisConfiguration option;

        private RedisMultiplexer multiplexer;

        [SetUp]
        public void Setup()
        {
            option = new RedisConfiguration("Test");
            option.Endpoints = new[] { new RedisEndpoint { Host = "localhost", Port = 7000 } };
            multiplexer = new RedisMultiplexer(option);
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new RedisMultiplexer(null));
            Assert.IsNotNull(multiplexer);
            Assert.IsNull(multiplexer.Database);
        }

        [Test]
        public void Configuration()
        {
            Assert.IsNotNull(multiplexer.Configuration);
            Assert.AreEqual("Unspecified/localhost:7000", multiplexer.Configuration.GetOptions().EndPoints[0].ToString());
        }

        [Test]
        public void CheckConnection()
        {
            Assert.Throws<InvalidOperationException>(multiplexer.CheckConnection);
        }
    }
}
