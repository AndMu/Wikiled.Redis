using NUnit.Framework;
using System;
using Wikiled.Redis.Logic.Pool;

namespace Wikiled.Redis.UnitTests.Logic.Pool
{
    [TestFixture]
    public class RedisLinksPoolTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void Cleanup()
        {
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new RedisLinksPool(null));
        }
    }
}
