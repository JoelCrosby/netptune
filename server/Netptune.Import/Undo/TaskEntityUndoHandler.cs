using System.Text.Json;

using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Undo;

namespace Netptune.Import.Undo;

public sealed class TaskEntityUndoHandler : IEntityUndoHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public TaskEntityUndoHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
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

        await UnitOfWork.CompleteAsync(cancellationToken);

        return EntityUndoResult.Success;
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
