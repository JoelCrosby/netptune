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
        Task<Workspace> GetWorkspace(int id);

        Task<Workspace> GetWorkspace(string slug);

        Task<ClientResponse<IsSlugUniqueResponse>> IsSlugUnique(string slug);

        Task<List<Workspace>> GetUserWorkspaces();

        Task<List<Workspace>> GetAll();

        Task<Workspace> UpdateWorkspace(Workspace workspace);

        Task<Workspace> AddWorkspace(AddWorkspaceRequest request);

        public Task<Workspace> AddWorkspace(AddWorkspaceRequest request, AppUser user);

        Task<ClientResponse> Delete(int id);

        Task<ClientResponse> Delete(string key);
    }
}
