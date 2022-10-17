using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Core.Services;

public interface IProjectService
{
    Task<List<ProjectViewModel>> GetProjects();

    Task<ProjectViewModel?> GetProject(string key);

    Task<ClientResponse<ProjectViewModel>> UpdateProject(UpdateProjectRequest request);

    Task<ClientResponse<ProjectViewModel>> AddProject(AddProjectRequest request);

    Task<ClientResponse> Delete(int id);
}
