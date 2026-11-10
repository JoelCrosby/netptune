using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Sprints;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Undo;

namespace Netptune.Import.Undo;

public sealed class TaskEntityUndoHandler : IEntityUndoHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IEventRecordWriter EventRecords;

    public TaskEntityUndoHandler(INetptuneUnitOfWork unitOfWork, IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        EventRecords = eventRecords;
    }

    public string EntityType => EventEntityTypes.From(Core.Enums.EntityType.Task);

    public async Task<EntityUndoResult> RevertCreate(EntityUndoContext context, CancellationToken cancellationToken = default)
    {
        var task = await UnitOfWork.Tasks.GetAsync(context.EntityId, cancellationToken: cancellationToken);

        if (task is null || task.WorkspaceId != context.WorkspaceId)
        {
            return EntityUndoResult.Success;
        }

        if (task.IsDeleted)
        {
            return EntityUndoResult.Success;
        }

        var wasTouchedByAnotherUser = HasBeenModifiedSince(task.UpdatedAt, context.ExpectedUpdatedAt);

        if (wasTouchedByAnotherUser)
        {
            return EntityUndoResult.Blocked($"Task {task.Id} changed after the import and was left alone.");
        }

        task.Delete(context.UserId);

        await UnitOfWork.CompleteAsync(cancellationToken);

        return EntityUndoResult.Success;
    }

    public async Task<EntityUndoResult> RevertUpdate(EntityUndoContext context, CancellationToken cancellationToken = default)
    {
        var task = await UnitOfWork.Tasks.GetAsync(context.EntityId, cancellationToken: cancellationToken);

        if (task is null || task.WorkspaceId != context.WorkspaceId)
        {
            return EntityUndoResult.Success;
        }

        if (context.PreviousValues is null)
        {
            return EntityUndoResult.Blocked($"Task {task.Id} has no recorded previous values.");
        }

        var wasTouchedByAnotherUser = HasBeenModifiedSince(task.UpdatedAt, context.ExpectedUpdatedAt);

        if (wasTouchedByAnotherUser)
        {
            return EntityUndoResult.Blocked($"Task {task.Id} changed after the import and was left alone.");
        }

        var previous = context.PreviousValues.RootElement;

        task.Name = ReadString(previous, "name") ?? task.Name;
        task.Description = ReadString(previous, "description");
        task.StatusId = ReadInt(previous, "statusId") ?? task.StatusId;
        task.Priority = ReadEnum<TaskPriority>(previous, "priority");
        task.EstimateValue = ReadDecimal(previous, "estimateValue");
        task.StartDate = ReadDate(previous, "startDate");
        task.DueDate = ReadDate(previous, "dueDate");
        task.ModifiedByUserId = context.UserId;

        // Entries written before imports set sprints don't record one, and those must keep theirs.
        var recordsSprint = previous.TryGetProperty("sprintId", out var sprintId);
        var restoredSprintId = sprintId.ValueKind == JsonValueKind.Number ? sprintId.GetInt32() : (int?)null;
        List<IEventWriteRequest> membershipEvents = recordsSprint
            ? await RestoreSprint(task, restoredSprintId, context, cancellationToken)
            : [];

        await UnitOfWork.Transaction(async () =>
        {
            await EventRecords.AppendRange(membershipEvents, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        return EntityUndoResult.Success;
    }

    // A closed sprint keeps its membership, as it would through the sprint endpoints. The one the task
    // sits in now is let go only when it hasn't changed since the import, which is the import's own
    // historical sprint; one completed after the import keeps the task.
    private async Task<List<IEventWriteRequest>> RestoreSprint(
        ProjectTask task,
        int? restoredSprintId,
        EntityUndoContext context,
        CancellationToken cancellationToken)
    {
        var currentSprintId = task.SprintId;

        if (currentSprintId == restoredSprintId)
        {
            return [];
        }

        var sprintIds = new[] { currentSprintId, restoredSprintId }.OfType<int>().ToList();
        var sprints = await UnitOfWork.Sprints.GetAllByIdAsync(sprintIds, true, cancellationToken);
        var current = sprints.FirstOrDefault(sprint => sprint.Id == currentSprintId);
        var restored = sprints.FirstOrDefault(sprint => sprint.Id == restoredSprintId);
        var currentIsClosed = current is not null && IsClosed(current);
        var importedAt = context.ExpectedUpdatedAt;
        var currentChangedSinceImport = importedAt is null || current?.UpdatedAt > importedAt;
        var keepsCurrentSprint = currentIsClosed && currentChangedSinceImport;
        var restoredIsClosed = restored is not null && IsClosed(restored);

        if (keepsCurrentSprint || restoredIsClosed)
        {
            return [];
        }

        task.SprintId = restoredSprintId;

        var leftActiveSprint = current?.Status == SprintStatus.Active;
        var joinedActiveSprint = restored?.Status == SprintStatus.Active;
        var events = new List<IEventWriteRequest>();

        if (!leftActiveSprint && !joinedActiveSprint)
        {
            return events;
        }

        var status = await UnitOfWork.Statuses.GetAsync(task.StatusId, true, cancellationToken);
        var membership = new SprintMembership(task, status, context.UserId);

        if (leftActiveSprint)
        {
            events.Add(MemberChange(membership, current!, SprintMemberChanges.Removed));
        }

        if (joinedActiveSprint)
        {
            events.Add(MemberChange(membership, restored!, SprintMemberChanges.Added));
        }

        return events;
    }

    private static bool IsClosed(Sprint sprint)
    {
        return sprint.Status is SprintStatus.Completed or SprintStatus.Cancelled;
    }

    private static EventWriteRequest<ScopeMemberChangedPayload> MemberChange(
        SprintMembership membership,
        Sprint sprint,
        string change)
    {
        var task = membership.Task;
        var scope = new SprintScope(sprint.WorkspaceId, sprint.Id, sprint.ProjectId);
        var member = new SprintMember
        {
            TaskId = task.Id,
            StatusId = task.StatusId,
            StatusCategory = membership.Status?.Category.ToString(),
            EstimateType = task.EstimateType?.ToString(),
            EstimateValue = task.EstimateValue,
        };
        var changed = SprintMemberEvents.Changed(scope, member, change);

        return changed with { ActorUserId = membership.ActorUserId };
    }

    private static bool HasBeenModifiedSince(DateTime? actual, DateTime? expected)
    {
        if (expected is null || actual is null)
        {
            return false;
        }

        return actual.Value > expected.Value.AddSeconds(1);
    }

    private static string? ReadString(JsonElement element, string name)
    {
        var found = element.TryGetProperty(name, out var property);

        if (!found || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private static int? ReadInt(JsonElement element, string name)
    {
        var found = element.TryGetProperty(name, out var property);

        if (!found || property.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return property.GetInt32();
    }

    private static decimal? ReadDecimal(JsonElement element, string name)
    {
        var found = element.TryGetProperty(name, out var property);

        if (!found || property.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return property.GetDecimal();
    }

    private static DateOnly? ReadDate(JsonElement element, string name)
    {
        var value = ReadString(element, name);

        if (value is null)
        {
            return null;
        }

        var parsed = DateOnly.TryParse(value, out var date);

        return parsed ? date : null;
    }

    private sealed record SprintMembership(ProjectTask Task, Status? Status, string ActorUserId);

    private static TEnum? ReadEnum<TEnum>(JsonElement element, string name) where TEnum : struct, Enum
    {
        var found = element.TryGetProperty(name, out var property);

        if (!found)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number)
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), property.GetInt32());
        }

        var value = property.ValueKind == JsonValueKind.String ? property.GetString() : null;

        if (value is null)
        {
            return null;
        }

        var parsed = Enum.TryParse<TEnum>(value, true, out var result);

        return parsed ? result : null;
    }
}
