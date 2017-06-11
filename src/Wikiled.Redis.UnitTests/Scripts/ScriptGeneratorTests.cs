using Wikiled.Redis.Scripts;
using NUnit.Framework;

namespace Wikiled.Redis.UnitTests.Scripts
{
    [TestFixture]
    public class ScriptGeneratorTests
    {
        [Test]
        public void Test()
        {
            ScriptGenerator generator = new ScriptGenerator();
            Assert.AreEqual(generator.GenerateInsertScript(true, 1), generator.GenerateInsertScript(true, 1));
        }
    }
}
