using System;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Moq;
using NUnit.Framework;

namespace Wikiled.Redis.UnitTests.Logic
{
    [TestFixture]
    public class RedisExtensionsTests
    {
        private Mock<IRedisLink> link;

        private ObjectKey key;

        [SetUp]
        public void Setup()
        {
            link = new Mock<IRedisLink>();
            link.Setup(item => item.Name).Returns("Redis");
            key = new ObjectKey("Name");
        }

        [Test]
        public void GetKey()
        {
            Assert.Throws<ArgumentNullException>(() => RedisExtensions.GetKey(null, key));
            Assert.Throws<ArgumentNullException>(() => RedisExtensions.GetKey(link.Object, (IDataKey)null));
            var result = link.Object.GetKey(key);
            Assert.AreEqual("Redis:object:Name", result.ToString());
        }
    }
}
