using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.ProjectTasks;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Services
{
    public interface ITaskService
    {
        Task<List<TaskViewModel>> GetTasks(string workspaceSlug);

        Task<TaskViewModel> GetTask(int id);

        Task<TaskViewModel> UpdateTask(ProjectTask projectTask);

        Task<TaskViewModel> AddTask(AddProjectTaskRequest request, AppUser user);

        Task<TaskViewModel> DeleteTask(int id, AppUser user);

        Task<ProjectTaskCounts> GetProjectTaskCount(int projectId);

        Task<ProjectTaskInBoardGroup> MoveTaskInBoardGroup(MoveTaskInGroupRequest request, AppUser user);
    }
}
