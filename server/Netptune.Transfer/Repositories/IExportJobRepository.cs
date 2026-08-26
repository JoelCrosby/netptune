using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Transfer.Entities;
using Netptune.Transfer.Enums;
using Netptune.Transfer.ViewModels;

namespace Netptune.Transfer.Repositories;

public interface IExportJobRepository : IWorkspaceEntityRepository<ExportJob, int>
{
    Task<ExportJob?> GetByPublicId(Guid publicId, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<ExportJob?> GetForProcessing(int id, CancellationToken cancellationToken = default);

    // Reads the status straight from the database rather than the tracked entity, so a job server run
    // can see a cancellation the API wrote after it started.
    Task<ExportJobStatus?> GetStatus(int id, CancellationToken cancellationToken = default);

    Task<PagedResponse<ExportJobViewModel>> GetExportJobs(int workspaceId, PageRequest page, CancellationToken cancellationToken = default);

    Task<ExportJobViewModel?> GetViewModel(Guid publicId, int workspaceId, CancellationToken cancellationToken = default);

    Task<int> CountUnfinished(int workspaceId, CancellationToken cancellationToken = default);

    Task<List<ExportJob>> GetExpired(DateTime expiredBefore, int take, CancellationToken cancellationToken = default);

    Task<List<ExportJob>> GetStaleRunning(DateTime startedBefore, CancellationToken cancellationToken = default);

    Task<long> GetExpectedStorageUsage(int workspaceId, CancellationToken cancellationToken = default);
}
