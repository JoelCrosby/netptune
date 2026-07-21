using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Audit;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Activity;
using Netptune.Core.ViewModels.Audit;

namespace Netptune.Core.Repositories;

public interface IEventRecordRepository : IRepository<EventRecord, long>
{
    Task<EventRecord> AppendAsync(EventRecord record, bool publish, CancellationToken cancellationToken = default);

    Task<List<ActivityViewModel>> GetActivities(EntityType entityType, int entityId, CancellationToken cancellationToken = default, int? take = null, string? cursor = null);

    Task<HashSet<Guid>> GetExistingEventIds(IEnumerable<Guid> eventIds, CancellationToken cancellationToken = default);

    Task<PagedResponse<AuditLogViewModel>> GetAuditLog(int workspaceId, AuditLogFilter filter, CancellationToken cancellationToken = default);

    Task<AuditLogDetailViewModel?> GetAuditLogDetail(int workspaceId, long id, CancellationToken cancellationToken = default);

    Task<List<AuditLogViewModel>> GetAuditLogForExport(int workspaceId, AuditLogFilter filter, CancellationToken cancellationToken = default);

    Task<List<AuditActivityPoint>> GetActivitySummary(int workspaceId, AuditLogFilter filter, CancellationToken cancellationToken = default);
}
