using System.Threading.Tasks;

using Netptune.Core.Models.Files;

namespace Netptune.Core.Services.Export;

public interface ITaskExportService
{
    Task<FileResponse> ExportWorkspaceTasks();

    Task<FileResponse> ExportBoardTasks(string boardIdentifier);
}
