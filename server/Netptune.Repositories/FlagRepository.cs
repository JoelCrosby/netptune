using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Flags;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class FlagRepository : WorkspaceEntityRepository<DataContext, Flag, int>, IFlagRepository
{
    public FlagRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<List<Flag>> GetExistingAutomationTaskFlags(
        IReadOnlyCollection<int> ruleIds,
        IReadOnlyCollection<int> taskIds,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(flag =>
                !flag.IsDeleted &&
                flag.AutomationRuleId.HasValue &&
                flag.EntityType == EntityType.Task &&
                flag.EntityId.HasValue)
            .Where(flag => flag.AutomationRuleId.HasValue && ruleIds.Contains(flag.AutomationRuleId.Value))
            .Where(flag => flag.EntityId.HasValue && taskIds.Contains(flag.EntityId.Value))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<List<TaskFlagViewModel>> GetActiveTaskFlags(
        int taskId,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(flag =>
                flag.WorkspaceId == workspaceId &&
                flag.EntityType == EntityType.Task &&
                flag.EntityId == taskId &&
                !flag.IsDeleted)
            .OrderByDescending(flag => flag.CreatedAt)
            .Select(flag => new TaskFlagViewModel
            {
                Id = flag.Id,
                Name = flag.Name,
                Description = flag.Description,
                AutomationRuleId = flag.AutomationRuleId,
                CreatedAt = flag.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }

    public Task<Flag?> GetTaskFlagForUpdate(
        int flagId,
        int taskId,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities.SingleOrDefaultAsync(
            flag =>
                flag.Id == flagId &&
                flag.WorkspaceId == workspaceId &&
                flag.EntityType == EntityType.Task &&
                flag.EntityId == taskId &&
                !flag.IsDeleted,
            cancellationToken);
    }
}
