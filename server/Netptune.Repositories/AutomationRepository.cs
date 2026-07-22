using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Automations;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class AutomationRepository : WorkspaceEntityRepository<DataContext, AutomationRule, int>, IAutomationRepository
{
    public AutomationRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<List<AutomationRule>> GetRulesInWorkspace(
        int workspaceId,
        bool enabledOnly = false,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(rule => rule.WorkspaceId == workspaceId && !rule.IsDeleted)
            .Where(rule => !enabledOnly || rule.IsEnabled)
            .Include(rule => rule.Actions.Where(action => !action.IsDeleted))
            .OrderBy(rule => rule.Name)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public Task<List<AutomationRule>> GetEnabledRulesForTrigger(
        AutomationTriggerType triggerType,
        int? workspaceId = null,
        CancellationToken cancellationToken = default)
    {
        var query = Entities
            .Where(rule =>
                !rule.IsDeleted &&
                rule.IsEnabled &&
                rule.TriggerType == triggerType);

        if (workspaceId is not null)
        {
            query = query.Where(rule => rule.WorkspaceId == workspaceId.Value);
        }

        return query
            .Include(rule => rule.Actions.Where(action => !action.IsDeleted))
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public Task<AutomationRule?> GetRuleInWorkspace(
        int ruleId,
        int workspaceId,
        bool isReadonly = false,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(rule => rule.Id == ruleId && rule.WorkspaceId == workspaceId && !rule.IsDeleted)
            .Include(rule => rule.Actions.Where(action => !action.IsDeleted))
            .AsSplitQuery()
            .IsReadonly(isReadonly)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasRun(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return Context.Set<AutomationRun>()
            .AnyAsync(run => run.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public Task<List<string>> GetExistingRunKeys(IReadOnlyCollection<string> idempotencyKeys, CancellationToken cancellationToken = default)
    {
        return Context.Set<AutomationRun>()
            .Where(run => idempotencyKeys.Contains(run.IdempotencyKey))
            .Select(run => run.IdempotencyKey)
            .ToListAsync(cancellationToken);
    }

    public Task AddRunsAsync(IEnumerable<AutomationRun> runs, CancellationToken cancellationToken = default)
    {
        return Context.Set<AutomationRun>().AddRangeAsync(runs, cancellationToken);
    }

    public Task AddScheduledActionsAsync(IEnumerable<ScheduledAutomationAction> actions, CancellationToken cancellationToken = default)
    {
        return Context.Set<ScheduledAutomationAction>().AddRangeAsync(actions, cancellationToken);
    }

    public Task<List<ScheduledAutomationAction>> GetDueScheduledActions(DateTime executeBefore, CancellationToken cancellationToken = default)
    {
        return Context.Set<ScheduledAutomationAction>()
            .Where(action => action.Status == ScheduledAutomationActionStatus.Pending)
            .Where(action => action.ExecuteAt <= executeBefore)
            .Include(action => action.AutomationRule)
            .Include(action => action.AutomationAction)
            .Include(action => action.Task)
                .ThenInclude(task => task.Workspace)
            .OrderBy(action => action.ExecuteAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CancelPendingTaskActions(
        int taskId,
        Guid currentEventId,
        string actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var currentEventKey = $":event:{currentEventId}:";

        return Context.Set<ScheduledAutomationAction>()
            .Where(action => action.TaskId == taskId)
            .Where(action => action.Status == ScheduledAutomationActionStatus.Pending)
            .Where(action => !action.IdempotencyKey.Contains(currentEventKey))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(action => action.Status, ScheduledAutomationActionStatus.Cancelled)
                .SetProperty(action => action.ProcessedAt, now)
                .SetProperty(action => action.ModifiedByUserId, actorUserId)
                .SetProperty(action => action.UpdatedAt, now), cancellationToken);
    }

    public Task<List<AutomationRunViewModel>> GetRuns(
        int ruleId,
        int workspaceId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return Context.Set<AutomationRun>()
            .Where(run => run.AutomationRuleId == ruleId)
            .Where(run => run.AutomationRule.WorkspaceId == workspaceId)
            .OrderByDescending(run => run.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .Select(run => new AutomationRunViewModel
            {
                Id = run.Id,
                AutomationRuleId = run.AutomationRuleId,
                EntityId = run.EntityId,
                EntityType = run.EntityType,
                TriggerType = run.TriggerType,
                Status = run.Status,
                IdempotencyKey = run.IdempotencyKey,
                Message = run.Message,
                CreatedAt = run.CreatedAt,
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
