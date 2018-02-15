using ProtoBuf;

namespace Wikiled.Redis.UnitTests.Helpers
{
    [ProtoContract]
    public class TestData
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public int Total { get; set; }
    }
}
