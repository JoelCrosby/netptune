using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Persistence;

internal sealed class RunPersistenceService
{
    private sealed record PlannedComment(Comment Entity, CommentPlan Plan);

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger<RunPersistenceService> Logger;
    private readonly IEventRecordWriter EventRecords;

    public RunPersistenceService(
        INetptuneUnitOfWork unitOfWork,
        ILogger<RunPersistenceService> logger,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
        EventRecords = eventRecords;
    }

    internal async Task<List<Notification>> Persist(AutomationPersistencePlan plan, CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        var activityLogs = plan.NotificationPlans.Select(notificationPlan => notificationPlan.Activity).ToList();
        var comments = BuildComments(plan.CommentPlans);
        var commentEntities = comments.Select(comment => comment.Entity).ToList();

        activity?.SetTag("automation.flags.created", plan.Flags.Count);
        activity?.SetTag("automation.event_records.created", activityLogs.Count);
        activity?.SetTag("automation.task_updates.planned", plan.TaskUpdatePlans.Count);
        activity?.SetTag("automation.comments.planned", comments.Count);

        Logger.LogInformation(
            "Persisting automation results for trigger {TriggerType}: {RunCount} runs, {EventRecordCount} activity logs, {FlagCount} flags, {TaskUpdateCount} task updates, {CommentCount} comments",
            plan.TriggerType,
            plan.Runs.Count,
            activityLogs.Count,
            plan.Flags.Count,
            plan.TaskUpdatePlans.Count,
            comments.Count);

        List<Notification> notifications = [];

        await UnitOfWork.Transaction(async () =>
        {
            await ApplyTaskUpdates(plan.TaskUpdatePlans, cancellationToken);
            await UnitOfWork.Comments.AddRangeAsync(commentEntities, cancellationToken);
            await UnitOfWork.EventRecords.AddRangeAsync(activityLogs, cancellationToken);
            await UnitOfWork.Flags.AddRangeAsync(plan.Flags, cancellationToken);
            await UnitOfWork.Automations.AddRunsAsync(plan.Runs, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);

            await AppendCommentEvents(comments, cancellationToken);

            notifications = BuildNotifications(plan.NotificationPlans);
            activity?.SetTag("automation.notifications.created", notifications.Count);

            await UnitOfWork.Notifications.AddRangeAsync(notifications, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        Logger.LogInformation(
            "Persisted automation results for trigger {TriggerType}: {NotificationCount} notifications",
            plan.TriggerType,
            notifications.Count);

        return notifications;
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

    private static List<Notification> BuildNotifications(List<NotificationActivityPlan> activityPlans)
    {
        return activityPlans
            .SelectMany(plan =>
            {
                var task = plan.Execution.Task;
                var actorUserId = plan.Execution.ActorUserId;
                var link = BuildTaskLink(task);

                return plan.RecipientUserIds.Select(userId => new Notification
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
                });
            })
            .ToList();
    }

    private static string BuildTaskLink(ProjectTask task)
    {
        var identifier = task.Project is null
            ? task.Id.ToString()
            : $"{task.Project.Key}-{task.ProjectScopeId}";

        return $"/{task.Workspace.Slug}/tasks/{identifier}";
    }
}
