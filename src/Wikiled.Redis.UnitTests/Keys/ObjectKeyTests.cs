using System;
using Wikiled.Redis.Keys;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Wikiled.Redis.UnitTests.Keys
{
    [TestFixture]
    public class ObjectKeyTests
    {
        [Test]
        public void Construct()
        {
            ClassicAssert.Throws<ArgumentException>(() => new ObjectKey((string)null));
            var key = new ObjectKey("Test");
            ClassicAssert.AreEqual("object:Test", key.FullKey);
        }

         [Test]
        public void AddIndex()
        {
            var key = new ObjectKey("Test");
            ClassicAssert.Throws<ArgumentNullException>(() => key.AddIndex(null));
            key.AddIndex(new IndexKey("Test"));
            ClassicAssert.AreEqual(1, key.Indexes.Length);
        }

        [Test]
        public void ConstructArray()
        {
            ClassicAssert.Throws<ArgumentNullException>(() => new ObjectKey((string[])null));
            ClassicAssert.Throws<ArgumentException>(() => new ObjectKey());
            var key = new ObjectKey(new[] { "Test" });
            ClassicAssert.AreEqual("object:Test", key.FullKey);
            key = new ObjectKey("Test", "Any");
            ClassicAssert.AreEqual("object:Test:Any", key.FullKey);
        }

        [Test]
        public void TestEqual()
        {
            var key1 = new ObjectKey("Test");
            var key2 = new ObjectKey("Test");
            ClassicAssert.AreEqual(key1, key2);
        }
    }
}
