using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public static class AiChangeSetMapper
{
    private static readonly JsonSerializerOptions FieldSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static AiChangeSetViewModel ToViewModel(AiChangeSet changeSet, List<AiProposedChange> changes)
    {
        return new AiChangeSetViewModel
        {
            Id = changeSet.Id,
            ConversationId = changeSet.ConversationId,
            Status = changeSet.Status,
            AppliedAt = changeSet.AppliedAt,
            Changes = changes.Select(ToViewModel).ToList(),
        };
    }

    private static AiProposedChangeViewModel ToViewModel(AiProposedChange change)
    {
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
        };
    }

    private static List<AiChangeFieldViewModel> ParseFields(JsonDocument fields)
    {
        var parsed = fields.Deserialize<List<AiChangeFieldViewModel>>(FieldSerializerOptions);

        return parsed ?? [];
    }
}
