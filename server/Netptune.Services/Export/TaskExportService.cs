using System;
using System.Threading.Tasks;

using Netptune.Core.Extensions;
using Netptune.Core.Models.Files;
using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.Services.Export;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Export;

public class TaskExportService : ITaskExportService
{
    private readonly IIdentityService Identity;
    private readonly ITaskRepository TaskRepository;

    public TaskExportService(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        Identity = identity;
        TaskRepository = unitOfWork.Tasks;
    }

    public async Task<FileResponse> ExportWorkspaceTasks()
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var tasks = await TaskRepository.GetExportTasksAsync(workspaceKey);
        var stream = await tasks.ToCsvStream();

        return new FileResponse
        {
            Stream = stream,
            ContentType = "application/octet-stream",
            Filename = $"Netptune-Task-Export_{workspaceKey}-{DateTime.UtcNow:yy-MMM-dd-HH-mm}.csv",
        };
    }

    public async Task<FileResponse> ExportBoardTasks(string boardIdentifier)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var tasks = await TaskRepository.GetBoardExportTasksAsync(workspaceKey, boardIdentifier);
        var stream = await tasks.ToCsvStream();

        return new FileResponse
        {
            Stream = stream,
            ContentType = "application/octet-stream",
            Filename = $"Netptune-Task-Export_{workspaceKey}-{boardIdentifier}-{DateTime.UtcNow:yy-MMM-dd-HH-mm}.csv",
        };
    }
}
