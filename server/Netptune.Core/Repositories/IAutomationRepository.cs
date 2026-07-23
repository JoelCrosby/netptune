using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Core.Repositories;

public interface IAutomationRepository : IWorkspaceEntityRepository<AutomationRule, int>
{
    Task<List<AutomationRule>> GetRulesInWorkspace(int workspaceId, bool enabledOnly = false, CancellationToken cancellationToken = default);

    Task<List<AutomationRule>> GetEnabledRulesForTrigger(AutomationTriggerType triggerType, int? workspaceId = null, CancellationToken cancellationToken = default);

    Task<AutomationRule?> GetRuleInWorkspace(int ruleId, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<bool> HasRun(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<List<string>> GetExistingRunKeys(IReadOnlyCollection<string> idempotencyKeys, CancellationToken cancellationToken = default);

    Task AddRunsAsync(IEnumerable<AutomationRun> runs, CancellationToken cancellationToken = default);

    Task AddScheduledActionsAsync(IEnumerable<ScheduledAutomationAction> actions, CancellationToken cancellationToken = default);

    Task<List<ScheduledAutomationAction>> ClaimDueScheduledActions(ScheduledActionClaim claim, CancellationToken cancellationToken = default);

    Task<int> CompleteClaimedScheduledAction(ScheduledActionCompletion completion, CancellationToken cancellationToken = default);

    Task<int> RetryClaimedScheduledAction(ScheduledActionRetry retry, CancellationToken cancellationToken = default);

    Task<int> CancelPendingTaskActions(int taskId, Guid currentEventId, string actorUserId, CancellationToken cancellationToken = default);

    Task<List<AutomationRunViewModel>> GetRuns(int ruleId, int workspaceId, int take = 50, CancellationToken cancellationToken = default);
}
