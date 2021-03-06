using StackExchange.Redis;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.Indexing
{
    public interface IIndexManagerFactory
    {
        IIndexManager Create(IIndexKey index);
    }
}
