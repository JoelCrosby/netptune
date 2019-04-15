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

        Task<RepoResult<TaskViewModel>> UpdateTask(ProjectTask projectTask);

        Task<RepoResult<TaskViewModel>> AddTask(ProjectTask projectTask, AppUser user);

        Task<RepoResult<int>> DeleteTask(int id);

        Task<RepoResult<ProjectTaskCounts>> GetProjectTaskCount(int projectId);
    }
}