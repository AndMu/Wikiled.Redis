using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.UnitTests.Helpers;

namespace Wikiled.Redis.UnitTests.Persistency
{
    [TestFixture]
    public class UserRepositoryTests
    {
        private ILogger<IdentityRepository> logger;

        private Mock<IRedisLink> mockRedisLink;

        private Mock<IRedisClient> mockClient;

        private Mock<IDatabase> database;

        private IdentityRepository instance;

        private Mock<IHandlingDefinitionFactory> handlingDefinition;

        private Identity data;

        [SetUp]
        public void SetUp()
        {
            data = new Identity();
            data.InstanceId = "TestId";
            logger = new NullLogger<IdentityRepository>();
            mockRedisLink = new Mock<IRedisLink>();
            handlingDefinition = new Mock<IHandlingDefinitionFactory>();
            mockRedisLink.Setup(item => item.DefinitionFactory).Returns(handlingDefinition.Object);

            handlingDefinition.Setup(item => item.ConstructGeneric<Identity>(It.IsAny<IRedisLink>(), null))
                               .Returns(new HandlingDefinition<Identity>(0, null));

            database = new Mock<IDatabase>();
            mockClient = new Mock<IRedisClient>();
            mockRedisLink.Setup(item => item.Database).Returns(database.Object);
            mockRedisLink.Setup(item => item.State).Returns(ChannelState.Open);
            mockRedisLink.Setup(item => item.Name).Returns("T");
            mockRedisLink.Setup(item => item.Client).Returns(mockClient.Object);
            instance = CreateUserRepository();
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new IdentityRepository(null, mockRedisLink.Object));
            Assert.Throws<ArgumentNullException>(() => new IdentityRepository(logger, null));
        }

        [Test]
        public void TestArguments()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.Save(null));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.LoadSingle(null));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.LoadPage(null));
        }

        public async Task Save()
        {
            await instance.Save(data);
            mockClient.Verify(item => item.AddRecord(It.IsAny<IDataKey>(), data));
        }

        [Test]
        public async Task LoadAll()
        {
            mockClient.Setup(item => item.GetRecords<Identity>(It.IsAny<IIndexKey>(), 0, -1)).Returns(Observable.Empty<Identity>());
            await instance.LoadAll(instance.Entity.AllIndex).ToArray();
            mockClient.Verify(item => item.GetRecords<Identity>(It.IsAny<IIndexKey>(), 0, -1));
        }

        [Test]
        public async Task LoadUserNotFound()
        {
            mockClient.Setup(item => item.GetRecords<Identity>(It.IsAny<IDataKey>())).Returns(Observable.Empty<Identity>());
            var result = await instance.LoadSingle("Test");
            Assert.IsNull(result);
            mockClient.Verify(item => item.GetRecords<Identity>(It.IsAny<IDataKey>()));
        }

        [Test]
        public async Task LoadSingle()
        {
            mockClient.Setup(item => item.GetRecords<Identity>(It.IsAny<IDataKey>())).Returns(new[] { new Identity() }.ToObservable);
            var result = await instance.LoadSingle("Test");
            Assert.IsNotNull(result);
            mockClient.Verify(item => item.GetRecords<Identity>(It.IsAny<IDataKey>()));
        }

        private IdentityRepository CreateUserRepository()
        {
            return new IdentityRepository(logger, mockRedisLink.Object);
        }
    }
}