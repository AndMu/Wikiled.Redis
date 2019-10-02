using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.Indexing
{
    public interface IMainIndexManager
    {
        Task[] Add(IDatabaseAsync database, IDataKey dataKey);


        Task[] Delete(IDatabaseAsync database, IDataKey dataKey);

        IIndexManager GetManager(IIndexKey index);
    }
}