namespace Wikiled.Common.Persistency
{
    public interface IDataKey
    {
        string Item { get; }

        string Key { get; }

        CachingStrategy Caching { get; }
    }
}
