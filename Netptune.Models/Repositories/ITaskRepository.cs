using Netptune.Models.Models;
using Netptune.Models.VeiwModels.ProjectTasks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Models.Repositories
{
    public interface ITaskRepository
    {
        Task<RepoResult<IEnumerable<TaskViewModel>>> GetTasksAsync(int workspaceId);

        Task<RepoResult<ProjectTask>> GetTask(int id);

        Task<RepoResult<ProjectTask>> UpdateTask(ProjectTask projectTask);

        Task<RepoResult<ProjectTask>> AddTask(ProjectTask projectTask, AppUser user);

        Task<RepoResult<ProjectTask>> DeleteTask(int id);

        Task<RepoResult<ProjectTaskCounts>> GetProjectTaskCount(int projectId);
    }
}