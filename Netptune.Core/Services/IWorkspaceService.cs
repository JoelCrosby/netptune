using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Services
{
    public interface IWorkspaceService
    {
        Task<Workspace> GetWorkspace(string slug);

        Task<Workspace> GetWorkspace(int id);

        Task<List<Workspace>> GetWorkspaces();

        Task<Workspace> UpdateWorkspace(Workspace workspace);

        Task<Workspace> AddWorkspace(Workspace workspace);

        Task<Workspace> DeleteWorkspace(int id);
    }
}
