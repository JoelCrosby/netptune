using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Diagnostics;
using Netptune.Automation.Models;
using Netptune.Automation.Notifications;
using Netptune.Automation.Persistence;
using Netptune.Automation.Planning;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Execution;

internal sealed class RuleExecutor
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ActionPlanner ActionPlanner;
    private readonly FlagPlanner FlagPlanner;
    private readonly RunPersistenceService Persistence;
    private readonly NotificationPublisher NotificationPublisher;
    private readonly ILogger<RuleExecutor> Logger;

    public RuleExecutor(
        INetptuneUnitOfWork unitOfWork,
        ActionPlanner actionPlanner,
        FlagPlanner flagPlanner,
        RunPersistenceService persistence,
        NotificationPublisher notificationPublisher,
        ILogger<RuleExecutor> logger)
    {
        UnitOfWork = unitOfWork;
        ActionPlanner = actionPlanner;
        FlagPlanner = flagPlanner;
        Persistence = persistence;
        NotificationPublisher = notificationPublisher;
        Logger = logger;
    }

    internal async Task Execute(
        AutomationTriggerType triggerType,
        List<PendingAutomationExecution> executions,
        CancellationToken cancellationToken)
    {
        var activity = Activity.Current;

        Telemetry.RecordRunsRequested(triggerType, executions.Count);
        activity?.SetTag("automation.executions.requested", executions.Count);

        if (executions.Count == 0)
        {
            Logger.LogDebug("No automation executions requested for trigger {TriggerType}", triggerType);
            return;
        }

        var pending = await GetPendingExecutions(triggerType, executions, cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        var plan = ActionPlanner.Plan(pending);
        RecordRunResults(triggerType, plan.Runs);

        var flags = await FlagPlanner.BuildFlags(triggerType, plan.FlagPlans, cancellationToken);
        var persistencePlan = new AutomationPersistencePlan
        {
            TriggerType = triggerType,
            Runs = plan.Runs,
            NotificationPlans = plan.NotificationPlans,
            Flags = flags,
            TaskUpdatePlans = plan.TaskUpdatePlans,
            CommentPlans = plan.CommentPlans,
        };

        var notifications = await Persistence.Persist(persistencePlan, cancellationToken);

        await NotificationPublisher.Publish(triggerType, notifications, cancellationToken);
    }

    private async Task<List<PendingAutomationExecution>> GetPendingExecutions(
        AutomationTriggerType triggerType,
        List<PendingAutomationExecution> executions,
        CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        var idempotencyKeys = executions.Select(execution => execution.IdempotencyKey).Distinct().ToList();
        var existingKeys = await UnitOfWork.Automations.GetExistingRunKeys(idempotencyKeys, cancellationToken);
        var existingKeySet = existingKeys.ToHashSet();
        var pending = executions
            .Where(execution => !existingKeySet.Contains(execution.IdempotencyKey))
            .ToList();

        var skippedCount = executions.Count - pending.Count;
        Telemetry.RecordRunsSkipped(triggerType, skippedCount, "idempotency_key_exists");
        activity?.SetTag("automation.executions.pending", pending.Count);
        activity?.SetTag("automation.executions.skipped.idempotency", skippedCount);

        Logger.LogInformation(
            "Prepared {PendingExecutionCount} automation executions for trigger {TriggerType}; skipped {SkippedExecutionCount} existing idempotency keys",
            pending.Count,
            triggerType,
            skippedCount);

        return pending;
    }

    private static void RecordRunResults(AutomationTriggerType triggerType, List<Core.Entities.AutomationRun> runs)
    {
        var activity = Activity.Current;
        var succeededCount = runs.Count(run => run.Status == AutomationRunStatus.Succeeded);
        var failedCount = runs.Count - succeededCount;
        Telemetry.RecordRunResults(triggerType, succeededCount, failedCount);
        activity?.SetTag("automation.runs.succeeded", succeededCount);
        activity?.SetTag("automation.runs.failed", failedCount);
    }
}
