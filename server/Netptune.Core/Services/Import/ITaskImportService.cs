using System.IO;
using System.Threading.Tasks;

using Netptune.Core.Models.Import;
using Netptune.Core.Responses.Common;

namespace Netptune.Core.Services.Import;

public interface ITaskImportService
{
    Task<ClientResponse<TaskImportResult>> ImportWorkspaceTasks(string boardId, Stream stream);
}