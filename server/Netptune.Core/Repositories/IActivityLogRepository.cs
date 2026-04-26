using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Audit;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Activity;
using Netptune.Core.ViewModels.Audit;

namespace Netptune.Core.Repositories;

public interface IActivityLogRepository : IWorkspaceEntityRepository<ActivityLog, int>
{
    Task<List<ActivityViewModel>> GetActivities(EntityType entityType, int entityId);

    Task<AuditLogPage> GetAuditLog(AuditLogFilter filter);

    Task<List<AuditLogViewModel>> GetAuditLogForExport(AuditLogFilter filter);

    Task AnonymiseUser(string userId, int workspaceId);
}