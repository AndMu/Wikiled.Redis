using System.Configuration;
using System.IO;
using NUnit.Framework;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.IntegrationTests
{
    [SetUpFixture]
    public class Global
    {
        private RedisProcessManager manager;

        [OneTimeSetUp]
        public void Setup()
        {
            manager = new RedisProcessManager();
            manager.Start(Path.Combine(TestContext.CurrentContext.TestDirectory, ConfigurationManager.AppSettings["Redis"]));
        }

        [OneTimeTearDown]
        public void Clean()
        {
            manager.Dispose();
        }
    }
}
