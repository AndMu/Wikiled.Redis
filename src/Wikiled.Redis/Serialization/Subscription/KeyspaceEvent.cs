using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;

namespace Wikiled.Redis.Serialization.Subscription
{
    public class KeyspaceEvent
    {
        public KeyspaceEvent(string key, RedisChannel channel, RedisValue value)
        {
            Guard.NotNullOrEmpty(() => key, key);
            Guard.IsValid(() => channel, channel, item => !channel.IsNullOrEmpty, nameof(channel));
            Guard.IsValid(() => value, value, item => !value.IsNullOrEmpty, nameof(value));
            
            Channel = channel;
            Key = key;
            Value = value;
        }

        public RedisChannel Channel { get; }

        public string Key { get; }

        public RedisValue Value { get; }
    }
}
