using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Requests;

namespace Netptune.Core.Services
{
    public interface IWorkspaceService
    {
        Task<Workspace> GetWorkspace(string slug);

        Task<Workspace> GetWorkspace(int id);

        Task<List<Workspace>> GetWorkspaces();

        Task<Workspace> UpdateWorkspace(Workspace workspace);

        Task<Workspace> AddWorkspace(AddWorkspaceRequest request);

        Task<Workspace> DeleteWorkspace(int id);
    }
}
