﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Wikiled.Redis.Logic;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Indexing;

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
            transaction = new Mock<ITransaction>();
            instance = new RedisTransaction(new NullLoggerFactory(), link.Object, transaction.Object, mainIndexManager.Object);
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new RedisTransaction(new NullLoggerFactory(), null, transaction.Object, mainIndexManager.Object));
            Assert.Throws<ArgumentNullException>(() => new RedisTransaction(new NullLoggerFactory(), link.Object, null, mainIndexManager.Object));
            Assert.Throws<ArgumentNullException>(() => new RedisTransaction(new NullLoggerFactory(), link.Object, transaction.Object, null));
            Assert.Throws<ArgumentNullException>(() => new RedisTransaction(null, link.Object, transaction.Object, mainIndexManager.Object));
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
