using System;
using System.Threading.Tasks;

using Netptune.Core.Extensions;
using Netptune.Core.Models.Files;
using Netptune.Core.Repositories;
using Netptune.Core.Services.Export;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Export
{
    public class TaskExportService : ITaskExportService
    {
        private readonly ITaskRepository TaskRepository;

        public TaskExportService(INetptuneUnitOfWork unitOfWork)
        {
            TaskRepository = unitOfWork.Tasks;
        }

        public async Task<FileResponse> ExportWorkspaceTasks(string workspaceSlug)
        {
            var tasks = await TaskRepository.GetExportTasksAsync(workspaceSlug);
            var stream = await tasks.ToCsvStream();

            return new FileResponse
            {
                Stream = stream,
                ContentType = "application/octet-stream",
                Filename = $"Netptune-Task-Export_{workspaceSlug}-{DateTime.UtcNow:yy-MMM-dd-HH-mm}.csv",
            };
        }
    }
}
