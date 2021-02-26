using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Events
{
    public interface IAncestorService<TEntityType>
    {
        Task<List<int>> GetAncestors(int entityId);
    }
}
