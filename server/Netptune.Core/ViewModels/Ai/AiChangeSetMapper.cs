using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Services.Ai;

namespace Netptune.Core.ViewModels.Ai;

public static class AiChangeSetMapper
{
    public static AiChangeSetViewModel ToViewModel(
        AiChangeSet changeSet,
        List<AiProposedChange> changes,
        AiChangeRouteIds routeIds,
        IAiUndoCatalog undoCatalog)
    {
        return new AiChangeSetViewModel
        {
            Id = changeSet.Id,
            ConversationId = changeSet.ConversationId,
            Status = changeSet.Status,
            AppliedAt = changeSet.AppliedAt,
            UndoneAt = changeSet.UndoneAt,
            Changes = changes.Select(change => ToViewModel(change, routeIds, undoCatalog)).ToList(),
        };
    }

    private static AiProposedChangeViewModel ToViewModel(
        AiProposedChange change,
        AiChangeRouteIds routeIds,
        IAiUndoCatalog undoCatalog)
    {
        var entityId = change.AppliedEntityId ?? change.EntityId;

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
            EntitySystemId = routeIds.Find(AiChangeRouteIds.Task, entityId),
            EntityRouteId = routeIds.Find(change.EntityType, entityId),
            UndoneAt = change.UndoneAt,
            CanUndo = undoCatalog.CanUndo(change.ToolName),
        };
    }

    private static List<AiChangeFieldViewModel> ParseFields(JsonDocument fields)
    {
        return AiChangeFieldSerializer.Deserialize(fields);
    }
}
