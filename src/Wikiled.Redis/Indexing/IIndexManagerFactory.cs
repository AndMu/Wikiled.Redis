using Wikiled.Redis.Keys;

namespace Wikiled.Redis.Indexing
{
    public interface IIndexManagerFactory
    {
        IIndexManager Create(params IIndexKey[] index);
    }
}
