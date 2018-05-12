using NUnit.Framework;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;

namespace Wikiled.Redis.UnitTests.Data
{
    [TestFixture]
    public class RedisValueExtractorTests
    {
        [Test]
        public void Construct()
        {
            Assert.IsTrue(RedisValueExtractor.IsPrimitive<int>());
            Assert.IsTrue(RedisValueExtractor.IsPrimitive<int?>());
            Assert.IsTrue(RedisValueExtractor.IsPrimitive<bool>());
            Assert.IsTrue(RedisValueExtractor.IsPrimitive<byte[]>());
            Assert.IsTrue(RedisValueExtractor.IsPrimitive<string>());
            Assert.IsFalse(RedisValueExtractor.IsPrimitive<Identity>());
        }

        [Test]
        public void SafeConvert()
        {
            var result = RedisValueExtractor.SafeConvert<int>(1);
            Assert.AreEqual(1, result);
            var resultString = RedisValueExtractor.SafeConvert<string>("1");
            Assert.AreEqual("1", resultString);
            var resultBool = RedisValueExtractor.SafeConvert<bool>(true);
            Assert.AreEqual(true, resultBool);
        }

        [Test]
        public void TryParsePrimitiveComplex()
        {
            var success = RedisValueExtractor.TryParsePrimitive<int?>(1, out RedisValue result);
            Assert.AreEqual(true, success);
            Assert.AreEqual((RedisValue)1, result);

            success = RedisValueExtractor.TryParsePrimitive<int?>(null, out result);
            Assert.AreEqual(true, success);
            Assert.AreEqual(RedisValue.Null, result);
        }

        [TestCase(1, true, "1")]
        [TestCase((long)1, true, "1")]
        [TestCase("1", true, "1")]
        [TestCase(true, true, "True")]
        [TestCase(null, false, null)]
        public void TryParsePrimitive(object instance, bool isSuccess, object value)
        {
            var success = RedisValueExtractor.TryParsePrimitive(instance, out RedisValue result);
            Assert.AreEqual(isSuccess, success);
            Assert.AreEqual((RedisValue)(string)value, result);
        }
    }
}
