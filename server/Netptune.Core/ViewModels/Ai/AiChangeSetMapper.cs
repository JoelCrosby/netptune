using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.ViewModels.Ai;

public static class AiChangeSetMapper
{
    private const string TaskEntityType = "task";

    // The task ids a change set points at, so the caller can read the matching tasks before mapping.
    public static List<int> CollectTaskIds(IEnumerable<AiProposedChange> changes)
    {
        return changes
            .Where(change => string.Equals(change.EntityType, TaskEntityType, StringComparison.Ordinal))
            .Select(change => change.AppliedEntityId ?? change.EntityId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
    }

    public static AiChangeSetViewModel ToViewModel(
        AiChangeSet changeSet,
        List<AiProposedChange> changes,
        IReadOnlyCollection<TaskViewModel> tasks,
        IAiUndoCatalog undoCatalog)
    {
        var systemIds = tasks.ToDictionary(task => task.Id, task => task.SystemId);

        return new AiChangeSetViewModel
        {
            Id = changeSet.Id,
            ConversationId = changeSet.ConversationId,
            Status = changeSet.Status,
            AppliedAt = changeSet.AppliedAt,
            UndoneAt = changeSet.UndoneAt,
            Changes = changes.Select(change => ToViewModel(change, systemIds, undoCatalog)).ToList(),
        };
    }

    private static AiProposedChangeViewModel ToViewModel(
        AiProposedChange change,
        IReadOnlyDictionary<int, string> systemIds,
        IAiUndoCatalog undoCatalog)
    {
        var entityId = change.AppliedEntityId ?? change.EntityId;
        var systemId = entityId.HasValue && systemIds.TryGetValue(entityId.Value, out var found) ? found : null;

        return new AiProposedChangeViewModel
        {
            Id = change.Id,
            Sequence = change.Sequence,
            ToolName = change.ToolName,
            EntityType = change.EntityType,
            EntityId = change.EntityId,
            RefKey = change.RefKey,
            Summary = change.Summary,
            Fields = ParseFields(change.Fields),
            ValidationStatus = change.ValidationStatus,
            ValidationMessage = change.ValidationMessage,
            ApplyStatus = change.ApplyStatus,
            ApplyError = change.ApplyError,
            AppliedEntityId = change.AppliedEntityId,
            EntitySystemId = systemId,
            UndoneAt = change.UndoneAt,
            CanUndo = undoCatalog.CanUndo(change.ToolName),
        };
    }

    private static List<AiChangeFieldViewModel> ParseFields(JsonDocument fields)
    {
        return AiChangeFieldSerializer.Deserialize(fields);
    }
}
