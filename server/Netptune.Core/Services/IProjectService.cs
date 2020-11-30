using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Core.Services
{
    public interface IProjectService
    {
        Task<List<ProjectViewModel>> GetProjects();

        Task<ProjectViewModel> GetProject(int id);

        Task<ProjectViewModel> UpdateProject(Project project);

        Task<ProjectViewModel> AddProject(AddProjectRequest request);

        Task<ClientResponse> Delete(int id);
    }
}
