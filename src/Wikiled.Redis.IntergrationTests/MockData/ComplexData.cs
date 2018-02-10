using System;
using ProtoBuf;

namespace Wikiled.Redis.IntegrationTests.MockData
{
    [ProtoContract]
    public class ComplexData
    {
        [ProtoMember(1)]
        public DateTime Date { get; set; }
    }
}
