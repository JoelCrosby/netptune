using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public static class AiChangeSetMapper
{
    private const string TaskEntityType = "task";

    private static readonly JsonSerializerOptions FieldSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<AiChangeSetViewModel> ToViewModel(
        AiChangeSet changeSet,
        List<AiProposedChange> changes,
        ITaskRepository tasks,
        IAiUndoCatalog undoCatalog,
        CancellationToken cancellationToken)
    {
        var systemIds = await ReadTaskSystemIds(changes, tasks, cancellationToken);

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

    private static async Task<Dictionary<int, string>> ReadTaskSystemIds(
        List<AiProposedChange> changes,
        ITaskRepository tasks,
        CancellationToken cancellationToken)
    {
        var taskIds = changes
            .Where(change => string.Equals(change.EntityType, TaskEntityType, StringComparison.Ordinal))
            .Select(change => change.AppliedEntityId ?? change.EntityId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (taskIds.Count == 0)
        {
            return [];
        }

        var models = await tasks.GetTaskViewModels(taskIds, cancellationToken);

        return models.ToDictionary(model => model.Id, model => model.SystemId);
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
        var parsed = fields.Deserialize<List<AiChangeFieldViewModel>>(FieldSerializerOptions);

        return parsed ?? [];
    }
}
