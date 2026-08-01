using System.Text.Json;

using Netptune.Core.Enums;

namespace Netptune.Core.Services.Ai;

public sealed record AiChangeField
{
    public required string Name { get; init; }

    public string? Before { get; init; }

    public string? After { get; init; }
}

public sealed record AiChangeDraft
{
    public required string ToolName { get; init; }

    public required string EntityType { get; init; }

    public int? EntityId { get; init; }

    public string? RefKey { get; init; }

    public required string Summary { get; init; }

    public List<AiChangeField> Fields { get; init; } = [];

    public required JsonDocument Payload { get; init; }

    public AiChangeValidationStatus ValidationStatus { get; init; }

    public string? ValidationMessage { get; init; }
}

public interface IAiChangeSetBuilder
{
    IReadOnlyList<AiChangeDraft> Changes { get; }

    string CreateRefKey();

    void Add(AiChangeDraft draft);
}
