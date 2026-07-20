using System.Text.Json;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class SprintReportingSeeder : ISeeder
{
    private const int ActiveCompletedTaskCount = 2;
    private const int ActiveInProgressTaskCount = 3;

    public int Phase => 3;

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        var events = context.Sprints.SelectMany(sprint => BuildSprintEvents(context, sprint)).ToList();

        context.EventRecords.AddRange(events);

        await dbContext.EventRecords.AddRangeAsync(events, ct);
    }

    private static IEnumerable<EventRecord> BuildSprintEvents(SeedContext context, Sprint sprint)
    {
        var tasks = context.Tasks.Where(task => task.Sprint == sprint).OrderBy(task => task.Id).ToList();
        var todoStatus = context.Statuses.First(status =>
            status.Workspace == sprint.Workspace &&
            status.EntityType == EntityType.Task &&
            status.Category == StatusCategory.Todo);
        var activeStatus = context.Statuses.First(status =>
            status.Workspace == sprint.Workspace &&
            status.EntityType == EntityType.Task &&
            status.Category == StatusCategory.Active);
        var doneStatus = context.Statuses.First(status =>
            status.Workspace == sprint.Workspace &&
            status.EntityType == EntityType.Task &&
            status.Category == StatusCategory.Done);
        var actorId = sprint.OwnerId ?? sprint.Owner?.Id ??
            throw new InvalidOperationException($"Sprint '{sprint.Name}' requires an owner");
        var startedAt = sprint.StartedAt!.Value;

        foreach (var task in tasks)
        {
            yield return CreateTaskCreatedEvent(sprint, task, todoStatus, actorId, startedAt.AddDays(-7));
        }

        yield return CreateLifecycleEvent(
            sprint,
            tasks,
            todoStatus,
            actorId,
            startedAt,
            "started");

        foreach (var taskEvent in BuildTaskProgressEvents(
                     sprint,
                     tasks,
                     todoStatus,
                     activeStatus,
                     doneStatus,
                     actorId))
        {
            yield return taskEvent;
        }

        if (sprint.CompletedAt is not null)
        {
            yield return CreateLifecycleEvent(
                sprint,
                tasks,
                doneStatus,
                actorId,
                sprint.CompletedAt.Value,
                "completed");
        }
    }

    private static IEnumerable<EventRecord> BuildTaskProgressEvents(
        Sprint sprint,
        IReadOnlyList<ProjectTask> tasks,
        Status todoStatus,
        Status activeStatus,
        Status doneStatus,
        string actorId)
    {
        var startedAt = sprint.StartedAt!.Value;
        var completedTasks = sprint.Status == SprintStatus.Completed
            ? tasks
            : tasks.Take(ActiveCompletedTaskCount).ToList();
        var activeTasks = sprint.Status == SprintStatus.Active
            ? tasks.Skip(ActiveCompletedTaskCount).Take(ActiveInProgressTaskCount).ToList()
            : [];

        foreach (var (task, index) in completedTasks.Select((task, index) => (task, index)))
        {
            var doneAt = SprintProgressTime(sprint, index, completedTasks.Count);
            var activeAt = startedAt.AddTicks(Math.Max(1, (doneAt - startedAt).Ticks / 2));

            yield return CreateStatusEvent(sprint, task, todoStatus, activeStatus, actorId, activeAt);
            yield return CreateStatusEvent(sprint, task, activeStatus, doneStatus, actorId, doneAt);
        }

        foreach (var (task, index) in activeTasks.Select((task, index) => (task, index)))
        {
            var occurredAt = SprintProgressTime(sprint, index, activeTasks.Count);

            yield return CreateStatusEvent(sprint, task, todoStatus, activeStatus, actorId, occurredAt);
        }
    }

    private static DateTime SprintProgressTime(Sprint sprint, int index, int taskCount)
    {
        var startedAt = sprint.StartedAt!.Value;
        var progressEnd = sprint.CompletedAt ?? DateTime.UtcNow;
        var availableTicks = Math.Max(0, (progressEnd - startedAt).Ticks);

        return startedAt.AddTicks(availableTicks * (index + 1) / (taskCount + 1));
    }

    private static EventRecord CreateTaskCreatedEvent(
        Sprint sprint,
        ProjectTask task,
        Status todoStatus,
        string actorId,
        DateTime occurredAt)
    {
        return CreateEvent(
            sprint,
            EventKeys.EntityCreated,
            EntityType.Task,
            task.Id,
            actorId,
            occurredAt,
            new EntityCreatedPayload
            {
                Name = task.Name,
                StatusId = todoStatus.Id,
                StatusCategory = todoStatus.Category.ToString(),
                SprintId = sprint.Id,
                EstimateType = task.EstimateType?.ToString(),
                EstimateValue = task.EstimateValue,
            });
    }

    private static EventRecord CreateStatusEvent(
        Sprint sprint,
        ProjectTask task,
        Status oldStatus,
        Status newStatus,
        string actorId,
        DateTime occurredAt)
    {
        return CreateEvent(
            sprint,
            EventKeys.EntityFieldTransitioned,
            EntityType.Task,
            task.Id,
            actorId,
            occurredAt,
            new FieldTransitionedPayload
            {
                Field = "status",
                OldValue = oldStatus.Id.ToString(),
                NewValue = newStatus.Id.ToString(),
                OldCategory = oldStatus.Category.ToString(),
                NewCategory = newStatus.Category.ToString(),
            });
    }

    private static EventRecord CreateLifecycleEvent(
        Sprint sprint,
        IReadOnlyCollection<ProjectTask> tasks,
        Status commitmentStatus,
        string actorId,
        DateTime occurredAt,
        string state)
    {
        var sprintStarted = state == "started";
        var commitment = tasks.Select(task => new SprintCommitmentMember
        {
            TaskId = task.Id,
            StatusId = commitmentStatus.Id,
            StatusCategory = commitmentStatus.Category.ToString(),
            EstimateType = task.EstimateType?.ToString(),
            EstimateValue = task.EstimateValue,
        }).ToList();

        return CreateEvent(
            sprint,
            EventKeys.ScopeLifecycleTransitioned,
            EntityType.Sprint,
            sprint.Id,
            actorId,
            occurredAt,
            new ScopeLifecyclePayload
            {
                State = state,
                PlannedStart = sprint.StartDate,
                PlannedEnd = sprint.EndDate,
                ActualStart = sprint.StartedAt,
                CompletedAt = sprintStarted ? null : sprint.CompletedAt,
                Commitment = commitment,
            });
    }

    private static EventRecord CreateEvent<TPayload>(
        Sprint sprint,
        string eventKey,
        EntityType subjectType,
        int subjectId,
        string actorId,
        DateTime occurredAt,
        TPayload payload)
        where TPayload : class
    {
        var record = new EventRecord
        {
            EventId = Guid.NewGuid(),
            WorkspaceId = sprint.WorkspaceId,
            EventKey = eventKey,
            SubjectType = EventEntityTypes.From(subjectType),
            SubjectId = subjectId.ToString(),
            OccurredAt = occurredAt,
            RecordedAt = occurredAt,
            ActorUserId = actorId,
            RetentionClass = EventRetentionClasses.Permanent,
            Payload = JsonSerializer.SerializeToDocument(payload, JsonOptions.Default),
        };

        record.References.Add(CreateReference(record, EntityType.Project, sprint.ProjectId));

        if (subjectType == EntityType.Task)
        {
            record.References.Add(CreateReference(record, EntityType.Sprint, sprint.Id));
        }

        return record;
    }

    private static EventReference CreateReference(EventRecord record, EntityType entityType, int entityId)
    {
        return new EventReference
        {
            EventRecord = record,
            Role = EventReferenceRoles.Scope,
            EntityType = EventEntityTypes.From(entityType),
            EntityId = entityId.ToString(),
        };
    }
}
