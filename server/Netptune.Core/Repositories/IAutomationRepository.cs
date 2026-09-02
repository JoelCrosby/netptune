using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Core.Repositories;

public interface IAutomationRepository : IWorkspaceEntityRepository<AutomationRule, int>
{
    Task<List<AutomationRule>> GetRulesInWorkspace(int workspaceId, bool enabledOnly = false, CancellationToken cancellationToken = default);

    Task<List<AutomationRule>> GetEnabledRulesForTrigger(AutomationTriggerType triggerType, int? workspaceId = null, CancellationToken cancellationToken = default);

    Task<AutomationRule?> GetRuleInWorkspace(int ruleId, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<string>> GetExistingRunKeys(IReadOnlyCollection<string> idempotencyKeys, CancellationToken cancellationToken = default);

    Task AddRunsAsync(IEnumerable<AutomationRun> runs, CancellationToken cancellationToken = default);

    Task AddScheduledActionsAsync(IEnumerable<ScheduledAutomationAction> actions, CancellationToken cancellationToken = default);

    Task<List<ScheduledAutomationAction>> ClaimDueScheduledActions(ScheduledActionClaim claim, CancellationToken cancellationToken = default);

    Task<int> CompleteClaimedScheduledAction(ScheduledActionCompletion completion, CancellationToken cancellationToken = default);

    Task<int> RetryClaimedScheduledAction(ScheduledActionRetry retry, CancellationToken cancellationToken = default);

    Task<int> CancelPendingTaskActions(int taskId, Guid currentEventId, string actorUserId, CancellationToken cancellationToken = default);

    Task<PagedResponse<AutomationRule>> GetRulesPaged(int workspaceId, AutomationRuleFilter filter, CancellationToken cancellationToken = default);

    Task<Dictionary<int, AutomationRunViewModel>> GetLatestRuns(IReadOnlyCollection<int> ruleIds, CancellationToken cancellationToken = default);

    Task<AutomationRuleSummaryViewModel> GetRuleSummary(int workspaceId, CancellationToken cancellationToken = default);

    Task<PagedResponse<AutomationRunViewModel>> GetRunsPaged(int ruleId, int workspaceId, AutomationRunFilter filter, CancellationToken cancellationToken = default);

    Task<List<AutomationRunStats>> GetRunStats(IReadOnlyCollection<int> ruleIds, DateTime since, CancellationToken cancellationToken = default);

    Task<int> AutoDisableRules(IReadOnlyCollection<int> ruleIds, string reason, DateTime disabledAt, CancellationToken cancellationToken = default);

    Task<int> GetWorkspaceRunCount(int workspaceId, DateTime since, CancellationToken cancellationToken = default);
}
