using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Wikiled.Common.Logging;
using Wikiled.Common.Serialization;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Config;
using Wikiled.Redis.IntegrationTests.Helpers;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Logic.Resilience;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.IntegrationTests.Persistency
{
    public class BaseIntegrationTests
    {
        private static readonly ILogger log = ApplicationLogging.CreateLogger<RedisPersistencyTests>();

        private RedisInside.Redis redisInstance;

        protected ObjectKey Key { get; private set; }

        protected IRedisLink Redis { get; private set; }

        protected Mock<ILimitedSizeRepository> Repository { get; private set; }

        protected RepositoryKey RepositoryKey { get; private set; }

        protected Identity Routing { get; private set; }

        protected IndexKey ListAll { get; private set; }

        protected IndexKey ListAll2 { get; private set; }

        protected IResilience Resilience { get; private set; }

        [SetUp]
        public void Setup()
        {
            redisInstance = new RedisInside.Redis(i => i.Port(6666).LogTo(item => log.LogDebug(item)));
            var config = XDocument.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, @"Config\redis.config")).XmlDeserialize<RedisConfiguration>();
            var provider = new ModuleHelper(config).Provider;
            Redis = provider.GetService<IRedisLink>();
            Redis.Open();
            Redis.Multiplexer.Flush();

            var redis2 = provider.GetService<IRedisLink>();
            Resilience = provider.GetService<IResilience>();
            redis2.Open();
            Key = new ObjectKey("Key1");
            Routing = new Identity();
            Routing.ApplicationId = "Test";
            Routing.Environment = "DEV";
            Repository = new Mock<ILimitedSizeRepository>();
            Repository.Setup(item => item.Name).Returns("Test");
            Repository.Setup(item => item.Size).Returns(2);
            RepositoryKey = new RepositoryKey(Repository.Object, Key);
            ListAll = new IndexKey(Repository.Object, "All", true);
            ListAll2 = new IndexKey(Repository.Object, "All2", true);
        }

        [TearDown]
        public void TearDown()
        {
            Redis.Dispose();
            redisInstance.Dispose();
        }
    }
}
