using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.UnitTests.Serialization
{
    [TestFixture]
    public class DictionarySerializerTests
    {
        private DictionarySerializer instance;

        [SetUp]
        public void Setup()
        {
            instance = new DictionarySerializer(new[] {"Data"});
        }

        [Test]
        public void Construct()
        {
            ClassicAssert.Throws<ArgumentNullException>(() => new DictionarySerializer(null));
            ClassicAssert.AreEqual(1, instance.Properties.Length);
        }

        [Test]
        public void Serialize()
        {
            Dictionary<string, string> table = new Dictionary<string, string>();
            table["Test"] = "1";
            table["Test2"] = "2";
            var data = instance.Serialize(table).ToArray();
            ClassicAssert.AreEqual(2, data.Length);
            var result = instance.Deserialize(data);
            ClassicAssert.AreEqual(2, result.Count);
            ClassicAssert.AreEqual("1", result["Test"]);
            ClassicAssert.AreEqual("2", result["Test2"]);

            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
            list.AddRange(data);
            list.AddRange(data);
            var stream = instance.DeserializeStream(list).ToArray();
            ClassicAssert.AreEqual(2, stream.Length);
        }
    }
}
