using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Search;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Notifications;

namespace Netptune.Automation.Persistence;

internal sealed class RunPersistenceService
{
    private sealed record PlannedComment(Comment Entity, CommentPlan Plan);

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
        var activityLogs = plan.NotificationPlans.Select(notificationPlan => notificationPlan.Activity).ToList();
        var comments = BuildComments(plan.CommentPlans);
        var commentEntities = comments.Select(comment => comment.Entity).ToList();
        var immediateTaskDeletions = plan.TaskDeletionPlans.Where(deletion => deletion.Delay <= TimeSpan.Zero).ToList();
        var scheduledActions = BuildScheduledActions(plan.TaskDeletionPlans);

        activity?.SetTag("automation.flags.created", plan.Flags.Count);
        activity?.SetTag("automation.event_records.created", activityLogs.Count);
        activity?.SetTag("automation.task_updates.planned", plan.TaskUpdatePlans.Count);
        activity?.SetTag("automation.comments.planned", comments.Count);
        activity?.SetTag("automation.task_deletions.planned", plan.TaskDeletionPlans.Count);
        activity?.SetTag("automation.actions.scheduled", scheduledActions.Count);

        Logger.LogInformation(
            "Persisting automation results for trigger {TriggerType}: {RunCount} runs, {EventRecordCount} activity logs, {FlagCount} flags, {TaskUpdateCount} task updates, {CommentCount} comments, {TaskDeletionCount} task deletions",
            plan.TriggerType,
            plan.Runs.Count,
            activityLogs.Count,
            plan.Flags.Count,
            plan.TaskUpdatePlans.Count,
            comments.Count,
            plan.TaskDeletionPlans.Count);

        List<Notification> notifications = [];
        List<TaskDeletionPlan> appliedTaskDeletions = [];

        await UnitOfWork.Transaction(async () =>
        {
            await ApplyTaskUpdates(plan.TaskUpdatePlans, cancellationToken);
            await UnitOfWork.Comments.AddRangeAsync(commentEntities, cancellationToken);
            await UnitOfWork.EventRecords.AddRangeAsync(activityLogs, cancellationToken);
            await UnitOfWork.Flags.AddRangeAsync(plan.Flags, cancellationToken);
            await UnitOfWork.Automations.AddRunsAsync(plan.Runs, cancellationToken);
            await UnitOfWork.Automations.AddScheduledActionsAsync(scheduledActions, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);

            await AppendCommentEvents(comments, cancellationToken);

            notifications = await BuildNotifications(plan.NotificationPlans, cancellationToken);
            activity?.SetTag("automation.notifications.created", notifications.Count);

            await UnitOfWork.Notifications.AddRangeAsync(notifications, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);

            appliedTaskDeletions = await ApplyTaskDeletions(immediateTaskDeletions, cancellationToken);
        });

        await RemoveDeletedTasksFromSearch(appliedTaskDeletions);

        Logger.LogInformation(
            "Persisted automation results for trigger {TriggerType}: {NotificationCount} notifications",
            plan.TriggerType,
            notifications.Count);

        return notifications;
    }

    private static List<ScheduledAutomationAction> BuildScheduledActions(List<TaskDeletionPlan> deletionPlans)
    {
        return deletionPlans
            .Where(plan => plan.Delay > TimeSpan.Zero)
            .Select(plan => new ScheduledAutomationAction
            {
                AutomationRuleId = plan.Execution.Rule.Id,
                AutomationActionId = plan.Action.Id,
                TaskId = plan.Execution.Task.Id,
                ActionType = AutomationActionType.DeleteTask,
                Status = ScheduledAutomationActionStatus.Pending,
                ExpectedStatusId = plan.Execution.Task.StatusId,
                ExecuteAt = plan.Execution.TriggeredAt.Add(plan.Delay),
                IdempotencyKey = $"{plan.Execution.IdempotencyKey}:action:{plan.Action.Id}",
                OwnerId = plan.Execution.ActorUserId,
                CreatedByUserId = plan.Execution.ActorUserId,
            })
            .ToList();
    }

    private async Task<List<TaskDeletionPlan>> ApplyTaskDeletions(
        List<TaskDeletionPlan> taskDeletionPlans,
        CancellationToken cancellationToken)
    {
        var appliedPlans = new List<TaskDeletionPlan>();

        foreach (var plan in taskDeletionPlans)
        {
            var taskId = plan.Execution.Task.Id;
            var affected = await UnitOfWork.Tasks.SoftDelete(taskId, plan.Execution.ActorUserId, cancellationToken);

            if (affected == 0)
            {
                continue;
            }

            appliedPlans.Add(plan);
        }

        Activity.Current?.SetTag("automation.task_deletions.applied", appliedPlans.Count);
        Logger.LogInformation("Applied automation task deletions to {DeletedTaskCount} tasks", appliedPlans.Count);

        return appliedPlans;
    }

    private async Task RemoveDeletedTasksFromSearch(List<TaskDeletionPlan> taskDeletionPlans)
    {
        foreach (var workspaceGroup in taskDeletionPlans.GroupBy(plan => plan.Execution.Task.Workspace.Slug))
        {
            var taskIds = workspaceGroup.Select(plan => plan.Execution.Task.Id).Distinct().ToList();

            await EventPublisher.Dispatch(new SearchIndexEvent
            {
                Operation = SearchIndexOperation.Delete,
                EntityType = "task",
                EntityIds = taskIds,
                WorkspaceSlug = workspaceGroup.Key,
            });
        }
    }

    private static List<PlannedComment> BuildComments(List<CommentPlan> plans)
    {
        var comments = new List<PlannedComment>(plans.Count);

        foreach (var plan in plans)
        {
            var comment = new Comment
            {
                Body = plan.Body,
                EntityId = plan.Execution.Task.Id,
                EntityType = EntityType.Task,
                WorkspaceId = plan.Execution.Rule.WorkspaceId,
                OwnerId = plan.Execution.ActorUserId,
                CreatedByUserId = plan.Execution.ActorUserId,
            };

            comments.Add(new PlannedComment(comment, plan));
        }

        return comments;
    }

    private async Task AppendCommentEvents(List<PlannedComment> comments, CancellationToken cancellationToken)
    {
        foreach (var comment in comments)
        {
            var commentEvent = BuildCommentEvent(comment);

            await EventRecords.Append(commentEvent, cancellationToken);
        }
    }

    private static EventWriteRequest<CommentEventPayload> BuildCommentEvent(PlannedComment comment)
    {
        return new EventWriteRequest<CommentEventPayload>
        {
            WorkspaceId = comment.Entity.WorkspaceId,
            EventKey = EventKeys.CommentCreated,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = comment.Entity.EntityId.ToString(),
            ActorUserId = comment.Plan.Execution.ActorUserId,
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

    private async Task ApplyTaskUpdates(List<TaskUpdatePlan> taskUpdatePlans, CancellationToken cancellationToken)
    {
        if (taskUpdatePlans.Count == 0)
        {
            return;
        }

        var tasks = new Dictionary<int, ProjectTask>();
        var updatedTaskIds = new HashSet<int>();

        foreach (var plan in taskUpdatePlans)
        {
            var taskId = plan.Execution.Task.Id;

            if (!tasks.TryGetValue(taskId, out var task))
            {
                task = await UnitOfWork.Tasks.GetTaskForUpdate(taskId, cancellationToken);

                if (task is null)
                {
                    Logger.LogWarning("Automation rule {RuleId} could not update missing task {TaskId}", plan.Execution.Rule.Id, taskId);
                    continue;
                }

                tasks[taskId] = task;
            }

            var taskUpdated = false;

            var status = plan.StatusId.HasValue
                ? await UnitOfWork.Statuses.GetInWorkspace(
                    plan.StatusId.Value,
                    plan.Execution.Rule.WorkspaceId,
                    cancellationToken: cancellationToken)
                : null;

            if (status is not null && task.StatusId != status.Id)
            {
                var oldStatusId = task.StatusId;
                var oldCategory = task.Status.Category;

                await UnitOfWork.Tasks.UpdateTaskStatus(taskId, status.Id, cancellationToken);

                task.StatusId = status.Id;
                task.Status = status;
                taskUpdated = true;

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

                var statusEvent = new EventWriteRequest<FieldTransitionedPayload>
                {
                    WorkspaceId = plan.Execution.Rule.WorkspaceId,
                    EventKey = EventKeys.EntityFieldTransitioned,
                    SubjectType = EventEntityTypes.From(EntityType.Task),
                    SubjectId = task.Id.ToString(),
                    ActorUserId = plan.Execution.ActorUserId,
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

            if (plan.Priority.HasValue && task.Priority != plan.Priority.Value)
            {
                task.Priority = plan.Priority.Value;
                taskUpdated = true;
            }

            if (taskUpdated)
            {
                updatedTaskIds.Add(taskId);
                task.UpdatedAt = DateTime.UtcNow;
                task.ModifiedByUserId = plan.Execution.ActorUserId;
            }
        }

        Activity.Current?.SetTag("automation.task_updates.applied", updatedTaskIds.Count);

        Logger.LogInformation("Applied automation task updates to {UpdatedTaskCount} tasks", updatedTaskIds.Count);
    }

    private async Task<List<Notification>> BuildNotifications(List<NotificationActivityPlan> activityPlans, CancellationToken cancellationToken)
    {
        var notifications = new List<Notification>();

        foreach (var plan in activityPlans)
        {
            var task = plan.Execution.Task;
            var actorUserId = plan.Execution.ActorUserId;

            var recipients = await NotificationRecipientResolver.Resolve(
                UnitOfWork,
                new NotificationRecipientRequest
                {
                    RequestedUserIds = plan.RecipientUserIds,
                    WorkspaceUserIds = plan.RecipientUserIds,
                    ActorUserId = actorUserId,
                    WorkspaceId = task.WorkspaceId,
                    ActivityType = ActivityType.Modify,
                    ExcludeActor = false,
                },
                cancellationToken);

            var link = BuildTaskLink(task);

            notifications.AddRange(recipients.Select(userId => new Notification
            {
                UserId = userId,
                EventRecordId = plan.Activity.Id,
                IsRead = false,
                Link = link,
                WorkspaceId = task.WorkspaceId,
                EntityType = EntityType.Task,
                ActivityType = ActivityType.Modify,
                CreatedByUserId = actorUserId,
                OwnerId = actorUserId,
            }));
        }

        return notifications;
    }

    private static string BuildTaskLink(ProjectTask task)
    {
        var identifier = task.Project is null
            ? task.Id.ToString()
            : $"{task.Project.Key}-{task.ProjectScopeId}";

        return $"/{task.Workspace.Slug}/tasks/{identifier}";
    }
}
