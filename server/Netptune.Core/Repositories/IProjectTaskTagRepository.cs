using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories
{
    public interface IProjectTaskTagRepository : IRepository<ProjectTaskTag, int>
    {
        Task<List<int>> DeleteAllByTaskId(IEnumerable<int> taskIds);
    }
}
