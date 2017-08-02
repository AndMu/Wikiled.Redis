using System;
using System.Net;
using Wikiled.Redis.Logic;
using Moq;
using NUnit.Framework;

namespace Wikiled.Redis.UnitTests.Logic
{
    [TestFixture]
    public class PooledRedisMultiplexerTests
    {
        private PooledRedisMultiplexer instance;

        private Mock<IRedisMultiplexer> underlying;

        [SetUp]
        public void Setup()
        {
            underlying = new Mock<IRedisMultiplexer>();
            instance = new PooledRedisMultiplexer(underlying.Object);
        }

        [Test]
        public void Construct()
        {
            Assert.IsNotNull(instance);
            Assert.Throws<ArgumentNullException>(() => new PooledRedisMultiplexer(null));
        }

        [Test]
        public void VerifyCalls()
        {
            instance.CheckConnection();
            underlying.Verify(item => item.CheckConnection());

            instance.Close();
            underlying.Verify(item => item.Close());

            instance.DeleteKeys("Test");
            underlying.Verify(item => item.DeleteKeys("Test"));
            
            instance.Flush();
            underlying.Verify(item => item.Flush());

            instance.GetInfo();
            underlying.Verify(item => item.GetInfo(null));

            instance.GetSubscriber();
            underlying.Verify(item => item.GetSubscriber());

            instance.Open();
            underlying.Verify(item => item.Open());

            instance.SetupSlave(new IPEndPoint(IPAddress.Any, 29));
            underlying.Verify(item => item.SetupSlave(It.IsAny<IPEndPoint>()));

            instance.SubscribeKeyEvents("Test", null);
            underlying.Verify(item => item.SubscribeKeyEvents("Test", null));
        }

        [Test]
        public void VerifySingleUse()
        {
            int released = 0;
            instance.Released += (sender, args) => released++;
            instance.Dispose();
            underlying.Verify(item => item.Dispose());
            Assert.AreEqual(1, released);
        }

        [Test]
        public void VerifyMultiUseUse()
        {
            int released = 0;
            instance.Released += (sender, args) => released++;
            instance.Increment();
            instance.Dispose();
            Assert.AreEqual(0, released);
            underlying.Verify(item => item.Dispose(), Times.Never);
            instance.Dispose();
            underlying.Verify(item => item.Dispose());
            Assert.AreEqual(1, released);
        }
    }
}
