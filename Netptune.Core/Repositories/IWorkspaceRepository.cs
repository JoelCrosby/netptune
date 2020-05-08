using Netptune.Core.Repositories.Common;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Repositories
{
    public interface IWorkspaceRepository : IRepository<Workspace, int>
    {
        Task<Workspace> GetBySlug(string slug);

        Task<Workspace> GetBySlug(string slug, bool includeRelated);

        Task<List<Workspace>> GetWorkspaces(AppUser user);

        Task<Workspace> UpdateWorkspace(Workspace workspace, AppUser user);
    }
}