using System.Threading.Tasks;

namespace Netptune.Core.Cache.Common
{
    public interface IValueCache<TValue>
    {
        Task<TValue> Get(string key);

        void Remove(string key);

        Task Create(string key, TValue value);
    }
}
