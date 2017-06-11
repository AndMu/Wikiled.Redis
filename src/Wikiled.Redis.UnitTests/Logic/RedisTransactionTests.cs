using System;
using System.Threading.Tasks;
using Wikiled.Redis.Logic;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using Wikiled.Redis.Channels;

namespace Wikiled.Redis.UnitTests.Logic
{
    [TestFixture]
    public class RedisTransactionTests
    {
        private Mock<IRedisLink> link;

        private Mock<ITransaction> transaction;

        private RedisTransaction instance;

        [SetUp]
        public void Setup()
        {
            link = new Mock<IRedisLink>();
            transaction = new Mock<ITransaction>();
            instance = new RedisTransaction(link.Object, transaction.Object);
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new RedisTransaction(null, transaction.Object));
            Assert.Throws<ArgumentNullException>(() => new RedisTransaction(link.Object, null));
            Assert.IsNotNull(instance.Client);
        }

        [TestCase(ChannelState.Closed, 0)]
        [TestCase(ChannelState.Open, 1)]
        public async Task CommitClosed(ChannelState state, int times)
        {
            link.Setup(item => item.State).Returns(state);
            await instance.Commit().ConfigureAwait(false);
            transaction.Verify(item => item.ExecuteAsync(CommandFlags.None), Times.Exactly(times));
        }
    }
}
