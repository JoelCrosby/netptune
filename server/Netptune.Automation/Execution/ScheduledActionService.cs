using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Execution;

internal sealed class ScheduledActionService
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IEventPublisher EventPublisher;
    private readonly ILogger<ScheduledActionService> Logger;

    public ScheduledActionService(
        INetptuneUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<ScheduledActionService> logger)
    {
        UnitOfWork = unitOfWork;
        EventPublisher = eventPublisher;
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
        var scheduledActions = await UnitOfWork.Automations.GetDueScheduledActions(DateTime.UtcNow, cancellationToken);
        var completedActions = new List<ScheduledAutomationAction>();

        foreach (var scheduledAction in scheduledActions)
        {
            var isEligible = IsEligible(scheduledAction);

            if (!isEligible)
            {
                MarkProcessed(scheduledAction, ScheduledAutomationActionStatus.Cancelled);
                continue;
            }

            var affected = await UnitOfWork.Tasks.SoftDelete(
                scheduledAction.TaskId,
                scheduledAction.OwnerId!,
                cancellationToken);

            if (affected == 0)
            {
                MarkProcessed(scheduledAction, ScheduledAutomationActionStatus.Cancelled);
                continue;
            }

            MarkProcessed(scheduledAction, ScheduledAutomationActionStatus.Completed);
            completedActions.Add(scheduledAction);
        }

        await UnitOfWork.CompleteAsync(cancellationToken);
        await RemoveDeletedTasksFromSearch(completedActions);

        Activity.Current?.SetTag("automation.scheduled_actions.completed", completedActions.Count);
        Logger.LogInformation(
            "Completed {CompletedActionCount} of {DueActionCount} due scheduled automation actions",
            completedActions.Count,
            scheduledActions.Count);
    }

    private static bool IsEligible(ScheduledAutomationAction scheduledAction)
    {
        var rule = scheduledAction.AutomationRule;
        var action = scheduledAction.AutomationAction;
        var task = scheduledAction.Task;

        return scheduledAction.ActionType == AutomationActionType.DeleteTask &&
            !rule.IsDeleted &&
            rule.IsEnabled &&
            !action.IsDeleted &&
            !task.IsDeleted &&
            task.StatusId == scheduledAction.ExpectedStatusId;
    }

    private static void MarkProcessed(
        ScheduledAutomationAction scheduledAction,
        ScheduledAutomationActionStatus status)
    {
        scheduledAction.Status = status;
        scheduledAction.ProcessedAt = DateTime.UtcNow;
        scheduledAction.ModifiedByUserId = scheduledAction.OwnerId;
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
