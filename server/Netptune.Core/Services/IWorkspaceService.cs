using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;

namespace Netptune.Core.Services
{
    public interface IWorkspaceService
    {
        Task<Workspace> GetWorkspace(string slug);

        Task<ClientResponse<IsSlugUniqueResponse>> IsSlugUnique(string slug);

        Task<Workspace> GetWorkspace(int id);

        Task<List<Workspace>> GetWorkspaces();

        Task<List<Workspace>> GetAll();

        Task<Workspace> UpdateWorkspace(Workspace workspace);

        Task<Workspace> AddWorkspace(AddWorkspaceRequest request);

        Task<ClientResponse> Delete(int id);
    }
}
