using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Models;

namespace Netptune.Core.Repositories
{
    public interface IProjectRepository
    {
        Task<IEnumerable<Project>> GetProjects(int workspaceId);

        Task<Project> GetProject(int id);

        Task<Project> UpdateProject(Project project, AppUser user);

        Task<Project> AddProject(Project project, AppUser user);

        Task<Project> DeleteProject(int id);
    }
}
