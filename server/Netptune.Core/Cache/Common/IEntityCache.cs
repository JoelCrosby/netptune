using System.Threading.Tasks;

namespace Netptune.Core.Cache.Common
{
    public interface IEntityCache<TEntity, TKey>
    {
        Task<TEntity> Get(TKey key);

        void Remove(TKey key);
    }
}
