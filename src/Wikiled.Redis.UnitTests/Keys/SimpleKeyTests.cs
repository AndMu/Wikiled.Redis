using Wikiled.Redis.Keys;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Wikiled.Redis.UnitTests.Keys
{
    [TestFixture]
    public class SimpleKeyTests
    {
        [Test]
        public void ConstructArray()
        {
            var key = new SimpleKey("Test");
            ClassicAssert.AreEqual("Test", key.FullKey);
            key = new SimpleKey("Test", "Any");
            ClassicAssert.AreEqual("Test:Any", key.FullKey);
        }

        [Test]
        public void GenerateKey()
        {
            var result = SimpleKey.GenerateKey("Repo", "Test");
            ClassicAssert.AreEqual("Repo:object:Test", result.FullKey);
        }
    }
}