using Netptune.Core.Enums;

namespace Netptune.Core.Models.Ai;

public sealed record ApplyAiChangeSetRequest
{
    public List<long> ChangeIds { get; init; } = [];
}

public sealed record AiAppliedChangeResult
{
    public long ChangeId { get; init; }

    public AiChangeApplyStatus Status { get; init; }

    public int? AppliedEntityId { get; init; }

    public string? Error { get; init; }
}

public sealed record AiApplyResult
{
    public Guid ChangeSetId { get; init; }

    public AiChangeSetStatus Status { get; init; }

    public List<AiAppliedChangeResult> Results { get; init; } = [];
}
