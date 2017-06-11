using Wikiled.Redis.Keys;
using NUnit.Framework;

namespace Wikiled.Redis.UnitTests.Keys
{
    [TestFixture]
    public class SimpleKeyTests
    {
        [Test]
        public void ConstructArray()
        {
            var key = new SimpleKey("Test");
            Assert.AreEqual("Test", key.FullKey);
            key = new SimpleKey("Test", "Any");
            Assert.AreEqual("Test:Any", key.FullKey);
        }

        [Test]
        public void GenerateKey()
        {
            var result = SimpleKey.GenerateKey("Repo", "Test");
            Assert.AreEqual("Repo:object:Test", result.FullKey);
        }
    }
}