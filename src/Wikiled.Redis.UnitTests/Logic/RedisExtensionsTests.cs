using System;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;

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
            ClassicAssert.Throws<ArgumentNullException>(() => RedisExtensions.GetKey(null, key));
            ClassicAssert.Throws<ArgumentNullException>(() => RedisExtensions.GetKey(link.Object, (IDataKey)null));
            var result = link.Object.GetKey(key);
            ClassicAssert.AreEqual("Redis:object:Name", result.ToString());
        }
    }
}
