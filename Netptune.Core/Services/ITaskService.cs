using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Models.Files;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.Services
{
    public interface ITaskService
    {
        Task<List<TaskViewModel>> GetTasks(string workspaceSlug);

        Task<TaskViewModel> GetTask(int id);

        Task<TaskViewModel> GetTask(string systemId, string workspaceSlug);

        Task<TaskViewModel> Update(UpdateProjectTaskRequest request);

        Task<TaskViewModel> Create(AddProjectTaskRequest request);

        Task<ClientResponse> Delete(int id);

        Task<ProjectTaskCounts> GetProjectTaskCount(int projectId);

        Task<ProjectTaskInBoardGroup> MoveTaskInBoardGroup(MoveTaskInGroupRequest request);

        Task<FileResponse> ExportWorkspaceTasks(string workspaceSlug);
    }
}
