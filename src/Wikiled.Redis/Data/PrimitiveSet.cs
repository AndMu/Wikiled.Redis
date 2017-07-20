using StackExchange.Redis;

namespace Wikiled.Redis.Data
{
    public class PrimitiveSet
    {
        public PrimitiveSet(RedisValue value)
        {
            Value = value;
        }

        public RedisValue Value { get; }
    }
}
