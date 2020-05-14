using Netptune.Core.Entities;
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

        Task<TaskViewModel> AddTask(AddProjectTaskRequest request);

        Task<TaskViewModel> DeleteTask(int id);

        Task<ProjectTaskCounts> GetProjectTaskCount(int projectId);

        Task<ProjectTaskInBoardGroup> MoveTaskInBoardGroup(MoveTaskInGroupRequest request);
    }
}
