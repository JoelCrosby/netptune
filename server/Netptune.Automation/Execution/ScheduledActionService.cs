using System.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Automation.Configuration;
using Netptune.Automation.Execution.Actions;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Automations;
using Netptune.Core.Models.Search;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Execution;

internal sealed class ScheduledActionService
{
    private static readonly TimeSpan MaximumRetryDelay = TimeSpan.FromDays(1);

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IEventPublisher EventPublisher;
    private readonly ScheduledActionEligibilityEvaluator EligibilityEvaluator;
    private readonly ScheduledActionHandlerRegistry HandlerRegistry;
    private readonly ScheduleOptions Options;
    private readonly ILogger<ScheduledActionService> Logger;

    public ScheduledActionService(
        INetptuneUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ScheduledActionEligibilityEvaluator eligibilityEvaluator,
        ScheduledActionHandlerRegistry handlerRegistry,
        IOptions<ScheduleOptions> options,
        ILogger<ScheduledActionService> logger)
    {
        UnitOfWork = unitOfWork;
        EventPublisher = eventPublisher;
        EligibilityEvaluator = eligibilityEvaluator;
        HandlerRegistry = handlerRegistry;
        Options = options.Value;
        Logger = logger;
    }

    internal async Task CancelForStatusChange(TaskChangedMessage message, CancellationToken cancellationToken)
    {
        var statusChanged = message.Changes.Any(change => change.Field == TaskChangeField.Status);

        if (!statusChanged)
        {
            return;
        }

        var cancelledCount = await UnitOfWork.Automations.CancelPendingTaskActions(
            message.TaskId,
            message.EventId,
            message.ActorUserId,
            cancellationToken);

        Activity.Current?.SetTag("automation.scheduled_actions.cancelled", cancelledCount);

        Logger.LogInformation(
            "Cancelled {CancelledActionCount} scheduled automation actions after task {TaskId} changed status",
            cancelledCount,
            message.TaskId);
    }

    internal async Task ExecuteDue(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var claimId = Guid.NewGuid();
        var claim = new ScheduledActionClaim
        {
            DueBefore = now,
            LeaseExpiresAt = now.Add(Options.DelayedActionClaimLease),
            ClaimId = claimId,
            BatchSize = Options.DelayedActionBatchSize,
        };
        var scheduledActions = await UnitOfWork.Automations.ClaimDueScheduledActions(
            claim,
            cancellationToken);
        var searchRemovals = new List<ScheduledAutomationAction>();
        var completedCount = 0;
        var cancelledCount = 0;
        var retryCount = 0;
        var failedCount = 0;

        foreach (var scheduledAction in scheduledActions)
        {
            try
            {
                var outcome = await ExecuteClaimedAction(scheduledAction, claimId, now, cancellationToken);

                if (outcome.Status == ScheduledAutomationActionStatus.Completed)
                {
                    completedCount++;
                }

                if (outcome.Status == ScheduledAutomationActionStatus.Cancelled)
                {
                    cancelledCount++;
                }

                if (outcome.RemoveTaskFromSearch)
                {
                    searchRemovals.Add(scheduledAction);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var wasRetried = await RecordFailure(scheduledAction, claimId, ex, cancellationToken);

                if (wasRetried)
                {
                    retryCount++;
                }
                else
                {
                    failedCount++;
                }
            }
        }

        await RemoveDeletedTasksFromSearch(searchRemovals);

        Activity.Current?.SetTag("automation.scheduled_actions.claimed", scheduledActions.Count);
        Activity.Current?.SetTag("automation.scheduled_actions.completed", completedCount);
        Activity.Current?.SetTag("automation.scheduled_actions.cancelled", cancelledCount);
        Activity.Current?.SetTag("automation.scheduled_actions.retried", retryCount);
        Activity.Current?.SetTag("automation.scheduled_actions.failed", failedCount);

        Logger.LogInformation(
            "Processed {ClaimedActionCount} claimed scheduled automation actions: {CompletedActionCount} completed, {CancelledActionCount} cancelled, {RetryActionCount} retried, {FailedActionCount} failed",
            scheduledActions.Count,
            completedCount,
            cancelledCount,
            retryCount,
            failedCount);
    }

    private async Task<ScheduledActionOutcome> ExecuteClaimedAction(
        ScheduledAutomationAction scheduledAction,
        Guid claimId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var isEligible = EligibilityEvaluator.IsEligible(scheduledAction, now);

        if (!isEligible)
        {
            var completion = new ScheduledActionCompletion
            {
                ActionId = scheduledAction.Id,
                ClaimId = claimId,
                Status = ScheduledAutomationActionStatus.Cancelled,
                ProcessedAt = now,
                Error = "The rule, action, or task no longer matches.",
            };
            var cancelled = await UnitOfWork.Automations.CompleteClaimedScheduledAction(
                completion,
                cancellationToken);

            EnsureClaimUpdated(scheduledAction.Id, cancelled);

            return new ScheduledActionOutcome(ScheduledAutomationActionStatus.Cancelled);
        }

        var handler = HandlerRegistry.Find(scheduledAction.ActionType);

        if (handler is null)
        {
            throw new InvalidOperationException(
                $"No scheduled automation handler is registered for action type '{scheduledAction.ActionType}'.");
        }

        var outcome = await UnitOfWork.Transaction(async () =>
        {
            var executionOutcome = await handler.Execute(scheduledAction, cancellationToken);
            var completion = new ScheduledActionCompletion
            {
                ActionId = scheduledAction.Id,
                ClaimId = claimId,
                Status = executionOutcome.Status,
                ProcessedAt = now,
            };
            var completed = await UnitOfWork.Automations.CompleteClaimedScheduledAction(
                completion,
                cancellationToken);

            EnsureClaimUpdated(scheduledAction.Id, completed);

            return executionOutcome;
        });

        return outcome;
    }

    private async Task<bool> RecordFailure(
        ScheduledAutomationAction scheduledAction,
        Guid claimId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var error = TruncateError(exception.GetBaseException().Message);
        var hasAttemptsRemaining = scheduledAction.AttemptCount < Options.DelayedActionMaxAttempts;

        Logger.LogError(
            exception,
            "Scheduled automation action {ScheduledActionId} failed on attempt {AttemptCount} of {MaximumAttempts}",
            scheduledAction.Id,
            scheduledAction.AttemptCount,
            Options.DelayedActionMaxAttempts);

        if (hasAttemptsRemaining)
        {
            var retryDelay = CalculateRetryDelay(scheduledAction.AttemptCount);
            var retry = new ScheduledActionRetry
            {
                ActionId = scheduledAction.Id,
                ClaimId = claimId,
                ExecuteAt = DateTime.UtcNow.Add(retryDelay),
                Error = error,
            };
            var updated = await UnitOfWork.Automations.RetryClaimedScheduledAction(retry, cancellationToken);

            return updated == 1;
        }

        var completion = new ScheduledActionCompletion
        {
            ActionId = scheduledAction.Id,
            ClaimId = claimId,
            Status = ScheduledAutomationActionStatus.Failed,
            ProcessedAt = DateTime.UtcNow,
            Error = error,
        };
        var failed = await UnitOfWork.Automations.CompleteClaimedScheduledAction(
            completion,
            cancellationToken);

        if (failed != 1)
        {
            Logger.LogInformation(
                "Scheduled automation action {ScheduledActionId} was no longer owned when recording terminal failure",
                scheduledAction.Id);
        }

        return false;
    }

    private TimeSpan CalculateRetryDelay(int attemptCount)
    {
        var exponent = Math.Max(0, attemptCount - 1);
        var multiplier = Math.Pow(2, exponent);
        var delaySeconds = Options.DelayedActionRetryDelay.TotalSeconds * multiplier;
        var boundedSeconds = Math.Min(delaySeconds, MaximumRetryDelay.TotalSeconds);

        return TimeSpan.FromSeconds(boundedSeconds);
    }

    private static string TruncateError(string error)
    {
        const int maximumLength = 2048;

        if (error.Length <= maximumLength)
        {
            return error;
        }

        return error[..maximumLength];
    }

    private static void EnsureClaimUpdated(int actionId, int updatedCount)
    {
        if (updatedCount != 1)
        {
            throw new InvalidOperationException(
                $"The claim for scheduled automation action '{actionId}' is no longer owned by this worker.");
        }
    }

    private async Task RemoveDeletedTasksFromSearch(List<ScheduledAutomationAction> completedActions)
    {
        foreach (var workspaceGroup in completedActions.GroupBy(action => action.Task.Workspace.Slug))
        {
            var taskIds = workspaceGroup.Select(action => action.TaskId).Distinct().ToList();

            await EventPublisher.Dispatch(new SearchIndexEvent
            {
                Operation = SearchIndexOperation.Delete,
                EntityType = "task",
                EntityIds = taskIds,
                WorkspaceSlug = workspaceGroup.Key,
            });
        }
    }
}
