using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Workspace;

namespace Netptune.Core.Services;

public interface IWorkspaceService
{
    Task<Workspace?> GetWorkspace(string slug);

    Task<ClientResponse<IsSlugUniqueResponse>> IsSlugUnique(string slug);

    Task<List<Workspace>> GetUserWorkspaces(string? userId = null);

    Task<Workspace?> GetUserWorkspace(string? userId = null);

    Task<List<Workspace>> GetAll();

    Task<ClientResponse<Workspace>> Update(UpdateWorkspaceRequest request);

    Task<ClientResponse<WorkspaceViewModel>> Create(AddWorkspaceRequest request);

    Task<ClientResponse<WorkspaceViewModel>> CreateNewUserWorkspace(AddWorkspaceRequest request, AppUser newUser);

    Task<ClientResponse> Delete(string key);

    Task<ClientResponse> DeletePermanent(string key);
}
