using Netptune.Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Models.Repositories
{
    public interface IWorkspaceRepository
    {
        Task<RepoResult<IEnumerable<Workspace>>> GetWorkspaces(AppUser user);

        Task<RepoResult<Workspace>> GetWorkspace(int id);

        Task<RepoResult<Workspace>> UpdateWorkspace(Workspace workspace, AppUser user);

        Task<RepoResult<Workspace>> AddWorkspace(Workspace workspace, AppUser user);

        Task<RepoResult<Workspace>> DeleteWorkspace(int id);
    }
}