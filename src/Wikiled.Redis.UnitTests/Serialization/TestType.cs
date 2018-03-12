using System;
using ProtoBuf;
using Wikiled.Common.Extensions;

namespace Wikiled.Redis.UnitTests.Serialization
{
    [ProtoContract]
    public class TestType
    {
        [ProtoMember(1)]
        public BasicTypes Status1 { get; set; }

        [ProtoMember(2)]
        public string Data { get; set; }

        [ProtoMember(3)]
        public int? Value { get; set; }

        [ProtoMember(4)]
        public int Another { get; set; }

        [ProtoMember(5)]
        public DateTime Date { get; set; }
    }
}
