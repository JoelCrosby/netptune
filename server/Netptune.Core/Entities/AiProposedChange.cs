using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public sealed record AiProposedChange : KeyedEntity<long>
{
    public Guid ChangeSetId { get; init; }

    public int Sequence { get; init; }

    public required string ToolName { get; init; }

    public required string EntityType { get; init; }

    public int? EntityId { get; init; }

    public string? RefKey { get; init; }

    public required string Summary { get; set; }

    public JsonDocument Fields { get; set; } = JsonDocument.Parse("[]");

    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");

    public AiChangeValidationStatus ValidationStatus { get; init; }

    public string? ValidationMessage { get; init; }

    public AiChangeApplyStatus ApplyStatus { get; set; }

    public string? ApplyError { get; set; }

    public int? AppliedEntityId { get; set; }

    public JsonDocument? UndoPayload { get; set; }

    public DateTime? UndoneAt { get; set; }

    [JsonIgnore]
    public AiChangeSet ChangeSet { get; init; } = null!;
}
