using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.Services
{
    public interface ITaskService
    {
        Task<List<TaskViewModel>> GetTasks(string workspaceSlug);

        Task<TaskViewModel> GetTask(int id);

        Task<TaskViewModel> GetTask(string systemId, string workspaceSlug);

        Task<TaskViewModel> UpdateTask(ProjectTask projectTask);

        Task<TaskViewModel> AddTask(AddProjectTaskRequest request);

        Task<TaskViewModel> DeleteTask(int id);

        Task<ProjectTaskCounts> GetProjectTaskCount(int projectId);

        Task<ProjectTaskInBoardGroup> MoveTaskInBoardGroup(MoveTaskInGroupRequest request);
    }
}
