using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Models;

namespace Netptune.Core.Services
{
    public interface IWorkspaceService
    {
        Task<Workspace> GetWorkspace(string slug);

        Task<Workspace> GetWorkspace(int id);

        Task<List<Workspace>> GetWorkspaces(AppUser user);

        Task<Workspace> UpdateWorkspace(Workspace workspace, AppUser user);

        Task<Workspace> AddWorkspace(Workspace workspace, AppUser user);

        Task<Workspace> DeleteWorkspace(int id);
    }
}
