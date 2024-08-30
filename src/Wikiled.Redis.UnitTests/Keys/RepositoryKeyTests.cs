using System;
using Wikiled.Redis.Keys;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.UnitTests.Keys
{
    [TestFixture]
    public class RepositoryKeyTests
    {
        private IRepository repository;

        [SetUp]
        public void Setup()
        {
            var mockRepository = new Mock<IRepository>();
            mockRepository.Setup(item => item.Name).Returns("Test1");
            repository = mockRepository.Object;
        }

        [Test]
        public void Construct()
        {
            var key = new RepositoryKey(repository, new ObjectKey("Test"));
            ClassicAssert.AreEqual("Test1:object:Test", key.FullKey);
        }

        [Test]
        public void TestEqual()
        {
            var key1 = new RepositoryKey(repository, new ObjectKey("Test"));
            var key2 = new RepositoryKey(repository, new ObjectKey("Test"));
            ClassicAssert.AreEqual(key1, key2);
        }
    }
}
