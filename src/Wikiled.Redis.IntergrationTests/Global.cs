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
            manager.Start(TestContext.CurrentContext.TestDirectory);
        }

        [OneTimeTearDown]
        public void Clean()
        {
            manager.Dispose();
        }
    }
}
