using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Repositories.Common;
using Netptune.Models;

namespace Netptune.Core.Repositories
{
    public interface IWorkspaceRepository : IRepository<Workspace, int>
    {
        Task<Workspace> GetBySlug(string slug);

        Task<List<Workspace>> GetWorkspaces(AppUser user);

        Task<Workspace> UpdateWorkspace(Workspace workspace, AppUser user);

        Task<Workspace> AddWorkspace(Workspace workspace);
    }
}