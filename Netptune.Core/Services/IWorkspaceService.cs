using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Models;
using Netptune.Models;

namespace Netptune.Core.Services
{
    public interface IWorkspaceService
    {
        Task<ServiceResult<Workspace>> GetWorkspace(int id);

        Task<ServiceResult<IEnumerable<Workspace>>> GetWorkspaces(AppUser user);

        Task<ServiceResult<Workspace>> UpdateWorkspace(Workspace workspace, AppUser user);

        Task<ServiceResult<Workspace>> AddWorkspace(Workspace workspace, AppUser user);

        Task<ServiceResult<Workspace>> DeleteWorkspace(int id);
    }
}
