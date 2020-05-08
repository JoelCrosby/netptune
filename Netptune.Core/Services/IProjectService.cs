using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Projects;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Services
{
    public interface IProjectService
    {
        Task<List<ProjectViewModel>> GetProjects(string workspaceSlug);

        Task<ProjectViewModel> GetProject(int id);

        Task<ProjectViewModel> UpdateProject(Project project, AppUser user);

        Task<ProjectViewModel> AddProject(AddProjectRequest request, AppUser user);

        Task<ProjectViewModel> DeleteProject(int id, AppUser user);
    }
}
