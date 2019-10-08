using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Models;
using Netptune.Models.VeiwModels.Projects;

namespace Netptune.Core.Services
{
    public interface IProjectService
    {
        Task<List<ProjectViewModel>> GetProjects(int workspaceId);

        Task<ProjectViewModel> GetProject(int id);

        Task<ProjectViewModel> UpdateProject(Project project, AppUser user);

        Task<ProjectViewModel> AddProject(Project project, AppUser user);

        Task<ProjectViewModel> DeleteProject(int id);
    }
}
