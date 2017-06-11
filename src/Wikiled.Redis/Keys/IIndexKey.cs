namespace Wikiled.Redis.Keys
{
    public interface IIndexKey
    {
        bool IsSet { get; }

        string RepositoryKey { get; }

        string Key { get; }
    }
}