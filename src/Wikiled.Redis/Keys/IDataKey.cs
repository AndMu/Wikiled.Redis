namespace Wikiled.Redis.Keys
{
    public interface IDataKey
    {
        string FullKey { get; }

        string RecordId { get; }

        void AddIndex(IIndexKey key);

        IIndexKey[] Indexes { get; }
    }
}