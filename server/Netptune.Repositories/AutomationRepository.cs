using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Automations;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.Sql;

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
            .Include(rule => rule.Workspace)
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
            .Include(rule => rule.Workspace)
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

    public async Task<List<ScheduledAutomationAction>> ClaimDueScheduledActions(
        ScheduledActionClaim claim,
        CancellationToken cancellationToken = default)
    {
        var pendingStatus = (int)ScheduledAutomationActionStatus.Pending;
        var processingStatus = (int)ScheduledAutomationActionStatus.Processing;

        using var connection = ConnectionFactory.StartConnection();

        var command = new CommandDefinition(
            SqlScripts.ClaimDueScheduledActions,
            new
            {
                pendingStatus,
                processingStatus,
                dueBefore = claim.DueBefore,
                batchSize = claim.BatchSize,
                claimId = claim.ClaimId,
                leaseExpiresAt = claim.LeaseExpiresAt,
            },
            cancellationToken: cancellationToken);

        var claimedIds = await connection.QueryAsync<int>(command);
        var claimedIdList = claimedIds.AsList();

        if (claimedIdList.Count == 0)
        {
            return [];
        }

        return await Context.Set<ScheduledAutomationAction>()
            .IgnoreQueryFilters()
            .Where(action => !action.IsDeleted)
            .Where(action => claimedIdList.Contains(action.Id))
            .Where(action => action.ClaimId == claim.ClaimId)
            .Include(action => action.AutomationRule)
                .ThenInclude(rule => rule.Workspace)
            .Include(action => action.AutomationAction)
            .Include(action => action.Task)
                .ThenInclude(task => task.Workspace)
            .Include(action => action.Task)
                .ThenInclude(task => task.ProjectTaskAppUsers)
            .Include(action => action.Task)
                .ThenInclude(task => task.Tags)
            .Include(action => action.Task)
                .ThenInclude(task => task.ProjectTaskInBoardGroups)
                .ThenInclude(link => link.BoardGroup)
            .OrderBy(action => action.ExecuteAt)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public Task<int> CompleteClaimedScheduledAction(
        ScheduledActionCompletion completion,
        CancellationToken cancellationToken = default)
    {
        return Context.Set<ScheduledAutomationAction>()
            .Where(action => action.Id == completion.ActionId)
            .Where(action => action.Status == ScheduledAutomationActionStatus.Processing)
            .Where(action => action.ClaimId == completion.ClaimId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(action => action.Status, completion.Status)
                .SetProperty(action => action.ProcessedAt, completion.ProcessedAt)
                .SetProperty(action => action.ClaimId, (Guid?)null)
                .SetProperty(action => action.LeaseExpiresAt, (DateTime?)null)
                .SetProperty(action => action.LastError, completion.Error)
                .SetProperty(action => action.UpdatedAt, completion.ProcessedAt), cancellationToken);
    }

    public Task<int> RetryClaimedScheduledAction(
        ScheduledActionRetry retry,
        CancellationToken cancellationToken = default)
    {
        return Context.Set<ScheduledAutomationAction>()
            .Where(action => action.Id == retry.ActionId)
            .Where(action => action.Status == ScheduledAutomationActionStatus.Processing)
            .Where(action => action.ClaimId == retry.ClaimId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(action => action.Status, ScheduledAutomationActionStatus.Pending)
                .SetProperty(action => action.ExecuteAt, retry.ExecuteAt)
                .SetProperty(action => action.ClaimId, (Guid?)null)
                .SetProperty(action => action.LeaseExpiresAt, (DateTime?)null)
                .SetProperty(action => action.LastError, retry.Error)
                .SetProperty(action => action.UpdatedAt, DateTime.UtcNow), cancellationToken);
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
            .Where(action =>
                action.Status == ScheduledAutomationActionStatus.Pending ||
                action.Status == ScheduledAutomationActionStatus.Processing)
            .Where(action => !action.IdempotencyKey.Contains(currentEventKey))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(action => action.Status, ScheduledAutomationActionStatus.Cancelled)
                .SetProperty(action => action.ProcessedAt, now)
                .SetProperty(action => action.ClaimId, (Guid?)null)
                .SetProperty(action => action.LeaseExpiresAt, (DateTime?)null)
                .SetProperty(action => action.ModifiedByUserId, actorUserId)
                .SetProperty(action => action.UpdatedAt, now), cancellationToken);
    }

    public async Task<PagedResponse<AutomationRule>> GetRulesPaged(
        int workspaceId,
        AutomationRuleFilter filter,
        CancellationToken cancellationToken = default)
    {
        var pagination = filter.GetPagination();
        var query = Entities
            .Where(rule => rule.WorkspaceId == workspaceId && !rule.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();

            query = query.Where(rule => EF.Functions.ILike(rule.Name, $"%{search}%"));
        }

        if (filter.IsEnabled.HasValue)
        {
            query = query.Where(rule => rule.IsEnabled == filter.IsEnabled.Value);
        }

        var triggerTypes = filter.GetTriggerTypes();

        if (triggerTypes.Count > 0)
        {
            query = query.Where(rule => triggerTypes.Contains(rule.TriggerType));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rules = await Sort(query, filter)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Include(rule => rule.Actions.Where(action => !action.IsDeleted))
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new PagedResponse<AutomationRule>(rules, pagination.Page, pagination.PageSize, totalCount);
    }

    public async Task<Dictionary<int, AutomationRunViewModel>> GetLatestRuns(
        IReadOnlyCollection<int> ruleIds,
        CancellationToken cancellationToken = default)
    {
        if (ruleIds.Count == 0)
        {
            return [];
        }

        var runs = await Entities
            .Where(rule => ruleIds.Contains(rule.Id))
            .SelectMany(rule => Context.Set<AutomationRun>()
                .Where(run => run.AutomationRuleId == rule.Id)
                .OrderByDescending(run => run.CreatedAt)
                .Take(1))
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

        return runs.ToDictionary(run => run.AutomationRuleId);
    }

    public async Task<AutomationRuleSummaryViewModel> GetRuleSummary(
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        var rules = await Entities
            .Where(rule => rule.WorkspaceId == workspaceId && !rule.IsDeleted)
            .Select(rule => new { rule.Id, rule.IsEnabled })
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var ruleIds = rules.Select(rule => rule.Id).ToList();
        var latestRuns = await GetLatestRuns(ruleIds, cancellationToken);
        var recentFailureCount = latestRuns.Values
            .Count(run => run.Status == AutomationRunStatus.Failed);

        return new AutomationRuleSummaryViewModel
        {
            RuleCount = rules.Count,
            EnabledCount = rules.Count(rule => rule.IsEnabled),
            RecentFailureCount = recentFailureCount,
        };
    }

    private static IQueryable<AutomationRule> Sort(IQueryable<AutomationRule> query, AutomationRuleFilter filter)
    {
        var isDescending = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return filter.SortBy?.ToLowerInvariant() switch
        {
            "isenabled" => isDescending
                ? query.OrderByDescending(rule => rule.IsEnabled).ThenBy(rule => rule.Name)
                : query.OrderBy(rule => rule.IsEnabled).ThenBy(rule => rule.Name),
            "triggertype" => isDescending
                ? query.OrderByDescending(rule => rule.TriggerType).ThenBy(rule => rule.Name)
                : query.OrderBy(rule => rule.TriggerType).ThenBy(rule => rule.Name),
            "createdat" => isDescending
                ? query.OrderByDescending(rule => rule.CreatedAt)
                : query.OrderBy(rule => rule.CreatedAt),
            _ => isDescending
                ? query.OrderByDescending(rule => rule.Name)
                : query.OrderBy(rule => rule.Name),
        };
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
                ActionResults = run.ActionResults
                    .OrderBy(result => result.SortOrder)
                    .ThenBy(result => result.Id)
                    .Select(result => new AutomationActionResultViewModel
                    {
                        Id = result.Id,
                        AutomationActionId = result.AutomationActionId,
                        ActionType = result.ActionType,
                        SortOrder = result.SortOrder,
                        Status = result.Status,
                        IdempotencyKey = result.IdempotencyKey,
                        StartedAt = result.StartedAt,
                        CompletedAt = result.CompletedAt,
                        Message = result.Message,
                        Output = result.Output,
                    })
                    .ToList(),
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
