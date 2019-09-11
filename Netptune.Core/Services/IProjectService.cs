using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Models;
using Netptune.Models;
using Netptune.Models.VeiwModels.Projects;

namespace Netptune.Core.Services
{
    public interface IProjectService
    {
        Task<ServiceResult<IEnumerable<ProjectViewModel>>> GetProjects(int workspaceId);

        Task<ServiceResult<ProjectViewModel>> GetProject(int id);

        Task<ServiceResult<ProjectViewModel>> UpdateProject(Project project, AppUser user);

        Task<ServiceResult<ProjectViewModel>> AddProject(Project project, AppUser user);

        Task<ServiceResult<ProjectViewModel>> DeleteProject(int id);
    }
}
