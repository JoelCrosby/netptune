using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Automations;
using Netptune.Core.Models.Search;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
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
    }

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger<RunPersistenceService> Logger;
    private readonly IEventRecordWriter EventRecords;
    private readonly IEventPublisher EventPublisher;

    public RunPersistenceService(
        INetptuneUnitOfWork unitOfWork,
        ILogger<RunPersistenceService> logger,
        IEventRecordWriter eventRecords,
        IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
        EventRecords = eventRecords;
        EventPublisher = eventPublisher;
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
            await ApplyTaskUpdate(action, taskUpdate, cancellationToken);
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
        CancellationToken cancellationToken)
    {
        var execution = action.Execution;
        var taskId = execution.Task.Id;
        var task = await UnitOfWork.Tasks.GetTaskForUpdate(taskId, cancellationToken);

        if (task is null)
        {
            Logger.LogWarning(
                "Automation rule {RuleId} could not update missing or deleted task {TaskId}",
                execution.Rule.Id,
                taskId);

            return;
        }

        var taskUpdated = false;
        var status = contribution.StatusId.HasValue
            ? await UnitOfWork.Statuses.GetInWorkspace(
                contribution.StatusId.Value,
                execution.Rule.WorkspaceId,
                cancellationToken: cancellationToken)
            : null;

        if (status is not null && task.StatusId != status.Id)
        {
            var oldStatusId = task.StatusId;
            var oldCategory = task.Status.Category;

            await UnitOfWork.Tasks.UpdateTaskStatus(taskId, status.Id, cancellationToken);

            task.StatusId = status.Id;
            task.Status = status;
            execution.Task.StatusId = status.Id;
            execution.Task.Status = status;
            taskUpdated = true;

            var references = BuildTaskScopeReferences(task);
            var statusEvent = new EventWriteRequest<FieldTransitionedPayload>
            {
                WorkspaceId = execution.Rule.WorkspaceId,
                EventKey = EventKeys.EntityFieldTransitioned,
                SubjectType = EventEntityTypes.From(EntityType.Task),
                SubjectId = task.Id.ToString(),
                ActorUserId = execution.ActorUserId,
                Payload = new FieldTransitionedPayload
                {
                    Field = "status",
                    OldValue = oldStatusId.ToString(),
                    NewValue = status.Id.ToString(),
                    OldCategory = oldCategory.ToString(),
                    NewCategory = status.Category.ToString(),
                },
                References = references,
            };

            await EventRecords.Append(statusEvent, cancellationToken);
        }

        if (contribution.Priority.HasValue && task.Priority != contribution.Priority.Value)
        {
            task.Priority = contribution.Priority.Value;
            execution.Task.Priority = contribution.Priority.Value;
            taskUpdated = true;
        }

        if (taskUpdated)
        {
            task.UpdatedAt = DateTime.UtcNow;
            task.ModifiedByUserId = execution.ActorUserId;
            Activity.Current?.AddTag("automation.task_update.applied", taskId);
        }
    }

    private static List<EventReferenceInput> BuildTaskScopeReferences(ProjectTask task)
    {
        var references = new List<EventReferenceInput>();

        if (task.ProjectId.HasValue)
        {
            references.Add(new EventReferenceInput
            {
                Role = EventReferenceRoles.Scope,
                EntityType = EventEntityTypes.From(EntityType.Project),
                EntityId = task.ProjectId.Value.ToString(),
            });
        }

        if (task.SprintId.HasValue)
        {
            references.Add(new EventReferenceInput
            {
                Role = EventReferenceRoles.Scope,
                EntityType = EventEntityTypes.From(EntityType.Sprint),
                EntityId = task.SprintId.Value.ToString(),
            });
        }

        return references;
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

    private static ScheduledAutomationAction BuildScheduledAction(
        PlannedAutomationAction action,
        TimeSpan delay)
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

    private static int CountActions(
        List<PlannedAutomationAction> actions,
        Func<PlannedAutomationAction, bool> predicate)
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
