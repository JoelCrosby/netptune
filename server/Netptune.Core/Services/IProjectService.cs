using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Core.Services
{
    public interface IProjectService
    {
        Task<List<ProjectViewModel>> GetProjects();

        Task<ProjectViewModel> GetProject(string key);

        Task<ProjectViewModel> UpdateProject(UpdateProjectRequest project);

        Task<ProjectViewModel> AddProject(AddProjectRequest request);

        Task<ClientResponse> Delete(int id);
    }
}
