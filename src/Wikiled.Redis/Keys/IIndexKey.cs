namespace Wikiled.Redis.Keys
{
    public interface IIndexKey
    {
        string RepositoryKey { get; }

        string Key { get; }
    }
}