using System;
using Wikiled.Redis.Config;
using Wikiled.Redis.Logic;
using Moq;
using NUnit.Framework;

namespace Wikiled.Redis.UnitTests.Logic
{
    [TestFixture]
    public class RedisFactoryTests
    {
        private RedisConfiguration configuration;

        private Mock<IRedisFactory> internalFactory;

        [SetUp]
        public void Setup()
        {
            internalFactory = new Mock<IRedisFactory>();
            configuration = new RedisConfiguration("Test");
            configuration.Endpoints = new[] { new RedisEndpoint { Host = "localhost", Port = 6000 } };
        }

        [Test]
        public void CreateNonPooled()
        {
            RedisFactory factory = new RedisFactory(internalFactory.Object);
            Assert.Throws<ArgumentNullException>(() => factory.Create(null));
            internalFactory.Setup(item => item.Create(It.IsAny<RedisConfiguration>())).Returns(new Mock<IRedisMultiplexer>().Object);
            var instance1 = factory.Create(configuration);
            internalFactory.Setup(item => item.Create(It.IsAny<RedisConfiguration>())).Returns(new Mock<IRedisMultiplexer>().Object);
            var instance2 = factory.Create(configuration);
            Assert.AreNotSame(instance1, instance2);
            instance1.Dispose();
            instance2.Dispose();
        }

        [Test]
        public void CreatePooled()
        {
            RedisFactory factory = new RedisFactory(internalFactory.Object);
            configuration.PoolConnection = true;

            internalFactory.Setup(item => item.Create(It.IsAny<RedisConfiguration>())).Returns(new Mock<IRedisMultiplexer>().Object);
            var instance1 = factory.Create(configuration);
            internalFactory.Setup(item => item.Create(It.IsAny<RedisConfiguration>())).Returns(new Mock<IRedisMultiplexer>().Object);
            var instance2 = factory.Create(configuration);
            Assert.AreSame(instance1, instance2);
            instance2.Dispose();

            factory = new RedisFactory(internalFactory.Object);
            var instance3 = factory.Create(configuration);
            Assert.AreSame(instance1, instance3);

            instance3.Dispose();
            instance1.Dispose();

            var instance4 = factory.Create(configuration);
            Assert.AreNotSame(instance1, instance4);
            instance4.Dispose();
        }
    }
}
