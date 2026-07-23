using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Automations;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Notifications;

namespace Netptune.Automation.Persistence;

internal sealed class RunPersistenceService
{
    private sealed record PlannedComment(Comment Entity, PlannedAutomationAction Action);

    private sealed record PersistenceState
    {
        public required Dictionary<PlannedAutomationAction, Flag> Flags { get; init; }

        public required List<Notification> Notifications { get; init; }

        public required List<PlannedAutomationAction> AppliedTaskDeletions { get; init; }

        public required HashSet<int> DeletedTaskIds { get; init; }

        public required List<TaskMutationOutcome> TaskMutations { get; init; }
    }

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger<RunPersistenceService> Logger;
    private readonly IEventRecordWriter EventRecords;
    private readonly IEventPublisher EventPublisher;
    private readonly ITaskMutationPipeline TaskMutationPipeline;

    public RunPersistenceService(
        INetptuneUnitOfWork unitOfWork,
        ILogger<RunPersistenceService> logger,
        IEventRecordWriter eventRecords,
        IEventPublisher eventPublisher,
        ITaskMutationPipeline taskMutationPipeline)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
        EventRecords = eventRecords;
        EventPublisher = eventPublisher;
        TaskMutationPipeline = taskMutationPipeline;
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

        var state = new PersistenceState
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
                await ApplyAction(action, state, cancellationToken);
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

    private async Task ApplyAction(
        PlannedAutomationAction action,
        PersistenceState state,
        CancellationToken cancellationToken)
    {
        var taskId = action.Execution.Task.Id;

        if (state.DeletedTaskIds.Contains(taskId))
        {
            Logger.LogInformation(
                "Skipping automation action {ActionId} because task {TaskId} was deleted by an earlier action",
                action.Action.Id,
                taskId);

            return;
        }

        var contribution = action.Contribution;

        if (contribution.Notification is { } notification)
        {
            await ApplyNotification(action, notification, state.Notifications, cancellationToken);
        }

        if (contribution.Flag is not null && state.Flags.TryGetValue(action, out var flag))
        {
            await UnitOfWork.Flags.AddRangeAsync([flag], cancellationToken);
        }

        if (contribution.TaskUpdate is { } taskUpdate)
        {
            await ApplyTaskUpdate(action, taskUpdate, state.TaskMutations, cancellationToken);
        }

        if (contribution.CommentBody is { } commentBody)
        {
            await ApplyComment(action, commentBody, cancellationToken);
        }

        if (contribution.TaskDeletion is { } taskDeletion)
        {
            await ApplyTaskDeletion(action, taskDeletion, state, cancellationToken);
        }
    }

    private async Task ApplyNotification(
        PlannedAutomationAction action,
        AutomationNotificationContribution contribution,
        List<Notification> notifications,
        CancellationToken cancellationToken)
    {
        await UnitOfWork.EventRecords.AddRangeAsync([contribution.Activity], cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        var actionNotifications = await BuildNotifications(action, contribution, cancellationToken);

        await UnitOfWork.Notifications.AddRangeAsync(actionNotifications, cancellationToken);
        notifications.AddRange(actionNotifications);
    }

    private async Task ApplyComment(
        PlannedAutomationAction action,
        string body,
        CancellationToken cancellationToken)
    {
        var comment = BuildComment(action, body);

        await UnitOfWork.Comments.AddRangeAsync([comment.Entity], cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);
        await AppendCommentEvent(comment, cancellationToken);
    }

    private static PlannedComment BuildComment(PlannedAutomationAction action, string body)
    {
        var execution = action.Execution;
        var comment = new Comment
        {
            Body = body,
            EntityId = execution.Task.Id,
            EntityType = EntityType.Task,
            WorkspaceId = execution.Rule.WorkspaceId,
            OwnerId = execution.ActorUserId,
            CreatedByUserId = execution.ActorUserId,
        };

        return new PlannedComment(comment, action);
    }

    private async Task AppendCommentEvent(PlannedComment comment, CancellationToken cancellationToken)
    {
        var commentEvent = BuildCommentEvent(comment);

        await EventRecords.Append(commentEvent, cancellationToken);
    }

    private static EventWriteRequest<CommentEventPayload> BuildCommentEvent(PlannedComment comment)
    {
        var execution = comment.Action.Execution;

        return new EventWriteRequest<CommentEventPayload>
        {
            WorkspaceId = comment.Entity.WorkspaceId,
            EventKey = EventKeys.CommentCreated,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = comment.Entity.EntityId.ToString(),
            ActorUserId = execution.ActorUserId,
            Payload = new CommentEventPayload
            {
                CommentId = comment.Entity.Id,
                RecipientUserIds = [],
            },
            References =
            [
                new EventReferenceInput
                {
                    Role = EventReferenceRoles.Member,
                    EntityType = EventEntityTypes.From(EntityType.Comment),
                    EntityId = comment.Entity.Id.ToString(),
                },
            ],
        };
    }

    private async Task ApplyTaskUpdate(
        PlannedAutomationAction action,
        AutomationTaskUpdateContribution contribution,
        List<TaskMutationOutcome> taskMutations,
        CancellationToken cancellationToken)
    {
        var execution = action.Execution;
        var taskId = execution.Task.Id;
        var previous = await UnitOfWork.Tasks.GetTaskViewModel(taskId, cancellationToken);
        var task = await UnitOfWork.Tasks.GetTaskForUpdate(taskId, cancellationToken);

        if (previous is null || task is null)
        {
            Logger.LogWarning(
                "Automation rule {RuleId} could not update missing or deleted task {TaskId}",
                execution.Rule.Id,
                taskId);

            return;
        }

        var status = contribution.StatusId.HasValue
            ? await UnitOfWork.Statuses.GetInWorkspace(
                contribution.StatusId.Value,
                execution.Rule.WorkspaceId,
                cancellationToken: cancellationToken)
            : null;

        var taskUpdated = TaskMutationPipeline.Apply(
            task,
            new TaskMutationValues(status, contribution.Priority));

        if (!taskUpdated)
        {
            return;
        }

        execution.Task.StatusId = task.StatusId;
        execution.Task.Status = task.Status;
        execution.Task.Priority = task.Priority;
        task.UpdatedAt = DateTime.UtcNow;
        task.ModifiedByUserId = execution.ActorUserId;

        await UnitOfWork.CompleteAsync(cancellationToken);

        var current = await UnitOfWork.Tasks.GetTaskViewModel(taskId, cancellationToken);

        if (current is null)
        {
            Logger.LogWarning(
                "Automation rule {RuleId} could not record the update for task {TaskId}",
                execution.Rule.Id,
                taskId);

            return;
        }

        var diff = ProjectTaskDiff.Create(previous, current);
        var outcome = await TaskMutationPipeline.Record(new TaskMutationRequest
        {
            Previous = previous,
            Current = current,
            Diff = diff,
            ActorUserId = execution.ActorUserId,
        }, cancellationToken);

        taskMutations.Add(outcome);
        Activity.Current?.AddTag("automation.task_update.applied", taskId);
    }

    private async Task ApplyTaskDeletion(
        PlannedAutomationAction action,
        AutomationTaskDeletionContribution contribution,
        PersistenceState state,
        CancellationToken cancellationToken)
    {
        if (contribution.Delay > TimeSpan.Zero)
        {
            var scheduledAction = BuildScheduledAction(action, contribution.Delay);

            await UnitOfWork.Automations.AddScheduledActionsAsync([scheduledAction], cancellationToken);

            return;
        }

        var execution = action.Execution;
        var affected = await UnitOfWork.Tasks.SoftDelete(
            execution.Task.Id,
            execution.ActorUserId,
            cancellationToken);

        if (affected == 0)
        {
            return;
        }

        state.AppliedTaskDeletions.Add(action);
        state.DeletedTaskIds.Add(execution.Task.Id);

        Activity.Current?.AddTag("automation.task_deletion.applied", execution.Task.Id);
    }

    private static ScheduledAutomationAction BuildScheduledAction(PlannedAutomationAction action, TimeSpan delay)
    {
        var execution = action.Execution;

        return new ScheduledAutomationAction
        {
            AutomationRuleId = execution.Rule.Id,
            AutomationActionId = action.Action.Id,
            TaskId = execution.Task.Id,
            ActionType = AutomationActionType.DeleteTask,
            Status = ScheduledAutomationActionStatus.Pending,
            ExpectedStatusId = execution.Task.StatusId,
            ExecuteAt = execution.TriggeredAt.Add(delay),
            IdempotencyKey = $"{execution.IdempotencyKey}:action:{action.Action.Id}",
            OwnerId = execution.ActorUserId,
            CreatedByUserId = execution.ActorUserId,
        };
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

    private async Task<List<Notification>> BuildNotifications(
        PlannedAutomationAction action,
        AutomationNotificationContribution contribution,
        CancellationToken cancellationToken)
    {
        var task = action.Execution.Task;
        var actorUserId = action.Execution.ActorUserId;

        var recipients = await NotificationRecipientResolver.Resolve(
            UnitOfWork,
            new NotificationRecipientRequest
            {
                RequestedUserIds = contribution.RecipientUserIds,
                WorkspaceUserIds = contribution.RecipientUserIds,
                ActorUserId = actorUserId,
                WorkspaceId = task.WorkspaceId,
                ActivityType = ActivityType.Modify,
                ExcludeActor = false,
            },
            cancellationToken);

        var link = BuildTaskLink(task);
        var notifications = recipients.Select(userId => new Notification
        {
            UserId = userId,
            EventRecordId = contribution.Activity.Id,
            IsRead = false,
            Link = link,
            WorkspaceId = task.WorkspaceId,
            EntityType = EntityType.Task,
            ActivityType = ActivityType.Modify,
            CreatedByUserId = actorUserId,
            OwnerId = actorUserId,
        }).ToList();

        return notifications;
    }

    private static int CountActions(List<PlannedAutomationAction> actions, Func<PlannedAutomationAction, bool> predicate)
    {
        return actions.Count(predicate);
    }

    private static string BuildTaskLink(ProjectTask task)
    {
        var identifier = task.Project is null
            ? task.Id.ToString()
            : $"{task.Project.Key}-{task.ProjectScopeId}";

        return $"/{task.Workspace.Slug}/tasks/{identifier}";
    }
}
