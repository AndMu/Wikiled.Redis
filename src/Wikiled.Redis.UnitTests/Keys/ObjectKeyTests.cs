using System;
using Wikiled.Redis.Keys;
using NUnit.Framework;

namespace Wikiled.Redis.UnitTests.Keys
{
    [TestFixture]
    public class ObjectKeyTests
    {
        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentException>(() => new ObjectKey((string)null));
            var key = new ObjectKey("Test");
            Assert.AreEqual("object:Test", key.FullKey);
        }

         [Test]
        public void AddIndex()
        {
            var key = new ObjectKey("Test");
            Assert.Throws<ArgumentNullException>(() => key.AddIndex(null));
            key.AddIndex(new IndexKey("Test", false));
            Assert.AreEqual(1, key.Indexes.Length);
        }

        [Test]
        public void ConstructArray()
        {
            Assert.Throws<ArgumentNullException>(() => new ObjectKey((string[])null));
            Assert.Throws<ArgumentException>(() => new ObjectKey());
            var key = new ObjectKey(new[] { "Test" });
            Assert.AreEqual("object:Test", key.FullKey);
            key = new ObjectKey("Test", "Any");
            Assert.AreEqual("object:Test:Any", key.FullKey);
        }

        [Test]
        public void TestEqual()
        {
            var key1 = new ObjectKey("Test");
            var key2 = new ObjectKey("Test");
            Assert.AreEqual(key1, key2);
        }
    }
}
