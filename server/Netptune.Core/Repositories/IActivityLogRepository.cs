using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Audit;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Activity;
using Netptune.Core.ViewModels.Audit;

namespace Netptune.Core.Repositories;

public interface IActivityLogRepository : IWorkspaceEntityRepository<ActivityLog, int>
{
    Task<List<ActivityViewModel>> GetActivities(EntityType entityType, int entityId, CancellationToken cancellationToken = default);

    Task<AuditLogPage> GetAuditLog(AuditLogFilter filter, CancellationToken cancellationToken = default);

    Task<List<AuditLogViewModel>> GetAuditLogForExport(AuditLogFilter filter, CancellationToken cancellationToken = default);

    Task<List<AuditActivityPoint>> GetActivitySummary(AuditLogFilter filter, CancellationToken cancellationToken = default);

    Task AnonymiseUser(string userId, int workspaceId, CancellationToken cancellationToken = default);
}