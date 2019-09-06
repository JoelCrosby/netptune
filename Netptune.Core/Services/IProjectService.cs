using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Models;
using Netptune.Models;

namespace Netptune.Core.Services
{
    public interface IProjectService
    {
        Task<ServiceResult<IEnumerable<Project>>> GetProjects(int workspaceId);

        Task<ServiceResult<Project>> GetProject(int id);

        Task<ServiceResult<Project>> UpdateProject(Project project, AppUser user);

        Task<ServiceResult<Project>> AddProject(Project project, AppUser user);

        Task<ServiceResult<Project>> DeleteProject(int id);
    }
}
