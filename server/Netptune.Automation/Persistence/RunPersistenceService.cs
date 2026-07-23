using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Models;
using Netptune.Automation.Persistence.Actions;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Search;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Persistence;

internal sealed class RunPersistenceService
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger<RunPersistenceService> Logger;
    private readonly IEventPublisher EventPublisher;
    private readonly ITaskMutationPipeline TaskMutationPipeline;
    private readonly ActionHandlerRegistry HandlerRegistry;

    public RunPersistenceService(
        INetptuneUnitOfWork unitOfWork,
        ILogger<RunPersistenceService> logger,
        IEventPublisher eventPublisher,
        ITaskMutationPipeline taskMutationPipeline,
        ActionHandlerRegistry handlerRegistry)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
        EventPublisher = eventPublisher;
        TaskMutationPipeline = taskMutationPipeline;
        HandlerRegistry = handlerRegistry;
    }

    internal async Task<List<Notification>> Persist(AutomationPersistencePlan plan, CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        var notificationCount = CountActions(plan.Actions, action => action.Contribution.Notification is not null);
        var flagCount = plan.Flags.Count;
        var taskUpdateCount = CountActions(plan.Actions, action => action.Contribution.TaskUpdate is not null);
        var commentCount = CountActions(plan.Actions, action => action.Contribution.CommentBody is not null);
        var taskDeletionCount = CountActions(plan.Actions, action => action.Contribution.TaskDeletion is not null);
        var scheduledActionCount = CountActions(
            plan.Actions,
            action => action.Contribution.TaskDeletion?.Delay > TimeSpan.Zero);

        activity?.SetTag("automation.event_records.created", notificationCount);
        activity?.SetTag("automation.flags.created", flagCount);
        activity?.SetTag("automation.task_updates.planned", taskUpdateCount);
        activity?.SetTag("automation.comments.planned", commentCount);
        activity?.SetTag("automation.task_deletions.planned", taskDeletionCount);
        activity?.SetTag("automation.actions.scheduled", scheduledActionCount);

        Logger.LogInformation(
            "Persisting {ActionCount} ordered automation actions for trigger {TriggerType}: {RunCount} runs, {NotificationCount} notifications, {FlagCount} flags, {TaskUpdateCount} task updates, {CommentCount} comments, {TaskDeletionCount} task deletions",
            plan.Actions.Count,
            plan.TriggerType,
            plan.Runs.Count,
            notificationCount,
            flagCount,
            taskUpdateCount,
            commentCount,
            taskDeletionCount);

        var state = new AutomationPersistenceState
        {
            Flags = plan.Flags,
            Notifications = [],
            AppliedTaskDeletions = [],
            DeletedTaskIds = [],
            TaskMutations = [],
        };

        await UnitOfWork.Transaction(async () =>
        {
            await UnitOfWork.Automations.AddRunsAsync(plan.Runs, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);

            foreach (var action in plan.Actions)
            {
                action.Result.StartedAt = DateTime.UtcNow;

                var outcome = await ApplyAction(action, state, cancellationToken);

                action.Result.Status = outcome.Status;
                action.Result.Message = outcome.Message;
                action.Result.CompletedAt = DateTime.UtcNow;

                await UnitOfWork.CompleteAsync(cancellationToken);
            }
        });

        activity?.SetTag("automation.notifications.created", state.Notifications.Count);

        await PublishTaskMutations(state.TaskMutations);
        await RemoveDeletedTasksFromSearch(state.AppliedTaskDeletions);

        Logger.LogInformation(
            "Persisted ordered automation actions for trigger {TriggerType}: {NotificationCount} notifications",
            plan.TriggerType,
            state.Notifications.Count);

        return state.Notifications;
    }

    private async Task<ActionOutcome> ApplyAction(
        PlannedAutomationAction action,
        AutomationPersistenceState state,
        CancellationToken cancellationToken)
    {
        var taskId = action.Execution.Task.Id;

        if (state.DeletedTaskIds.Contains(taskId))
        {
            Logger.LogInformation(
                "Skipping automation action {ActionId} because task {TaskId} was deleted by an earlier action",
                action.Action.Id,
                taskId);

            return new ActionOutcome(
                AutomationActionResultStatus.Skipped,
                "The task was deleted by an earlier action.");
        }

        var handler = HandlerRegistry.Find(action.Action.Type);

        if (handler is null)
        {
            Logger.LogError(
                "No automation action execution handler is registered for action type {ActionType}",
                action.Action.Type);

            return new ActionOutcome(
                AutomationActionResultStatus.Skipped,
                "No execution handler is registered for the action type.");
        }

        return await handler.Execute(action, state, cancellationToken);
    }

    private async Task RemoveDeletedTasksFromSearch(List<PlannedAutomationAction> taskDeletions)
    {
        var workspaceGroups = taskDeletions.GroupBy(action => action.Execution.Task.Workspace.Slug);

        foreach (var workspaceGroup in workspaceGroups)
        {
            var taskIds = workspaceGroup
                .Select(action => action.Execution.Task.Id)
                .Distinct()
                .ToList();

            await EventPublisher.Dispatch(new SearchIndexEvent
            {
                Operation = SearchIndexOperation.Delete,
                EntityType = "task",
                EntityIds = taskIds,
                WorkspaceSlug = workspaceGroup.Key,
            });
        }
    }

    private async Task PublishTaskMutations(List<TaskMutationOutcome> taskMutations)
    {
        foreach (var taskMutation in taskMutations)
        {
            await TaskMutationPipeline.Publish(taskMutation);
        }
    }

    private static int CountActions(
        List<PlannedAutomationAction> actions,
        Func<PlannedAutomationAction, bool> predicate)
    {
        return actions.Count(predicate);
    }
}
