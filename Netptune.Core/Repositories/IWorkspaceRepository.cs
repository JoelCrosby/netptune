using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories
{
    public interface IWorkspaceRepository : IRepository<Workspace, int>
    {
        Task<Workspace> GetBySlug(string slug);

        Task<Workspace> GetBySlug(string slug, bool includeRelated);

        Task<List<Workspace>> GetWorkspaces(AppUser user);

        Task<bool> Exists(string slug);
    }
}
