using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Wikiled.Redis.Logic;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.Logic.Resilience;

namespace Wikiled.Redis.UnitTests.Logic
{
    [TestFixture]
    public class RedisTransactionTests
    {
        private Mock<IRedisLink> link;

        private Mock<ITransaction> transaction;

        private RedisTransaction instance;

        private Mock<IMainIndexManager> mainIndexManager;

        [SetUp]
        public void Setup()
        {
            mainIndexManager = new Mock<IMainIndexManager>();
            link = new Mock<IRedisLink>();
            link.Setup(item => item.Resilience).Returns(new ResilienceHandler(new NullLogger<ResilienceHandler>(), new ResilienceConfig()));
            transaction = new Mock<ITransaction>();
            instance = new RedisTransaction(new NullLoggerFactory(), link.Object, transaction.Object, mainIndexManager.Object);
        }

        [Test]
        public void Construct()
        {
            ClassicAssert.Throws<ArgumentNullException>(() => new RedisTransaction(new NullLoggerFactory(), null, transaction.Object, mainIndexManager.Object));
            ClassicAssert.Throws<ArgumentNullException>(() => new RedisTransaction(new NullLoggerFactory(), link.Object, null, mainIndexManager.Object));
            ClassicAssert.Throws<ArgumentNullException>(() => new RedisTransaction(new NullLoggerFactory(), link.Object, transaction.Object, null));
            ClassicAssert.Throws<ArgumentNullException>(() => new RedisTransaction(null, link.Object, transaction.Object, mainIndexManager.Object));
            ClassicAssert.IsNotNull(instance.Client);
        }

        [TestCase(ChannelState.Closed, 0)]
        [TestCase(ChannelState.Open, 1)]
        public async Task CommitClosed(ChannelState state, int times)
        {
            link.Setup(item => item.State).Returns(state);
            await instance.Commit();
            transaction.Verify(item => item.ExecuteAsync(CommandFlags.None), Times.Exactly(times));
        }
    }
}
