using Netptune.Core.Enums;

namespace Netptune.Core.Models.Ai;

public sealed record ApplyAiChangeSetRequest
{
    public List<long> ChangeIds { get; init; } = [];
}

public sealed record AiAppliedChangeResult
{
    public long ChangeId { get; init; }

    public string? EntityType { get; init; }

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

public sealed record AiApplyProgress
{
    public AiApplyProgressType Type { get; init; }

    public int Total { get; init; }

    public int Completed { get; init; }

    public long? ChangeId { get; init; }

    public AiChangeApplyStatus? Status { get; init; }

    public AiChangeSetStatus? ChangeSetStatus { get; init; }

    public string? Message { get; init; }

    public static AiApplyProgress Started(int total)
    {
        return new AiApplyProgress { Type = AiApplyProgressType.Started, Total = total };
    }

    public static AiApplyProgress ChangeStarted(long changeId, int completed, int total)
    {
        return new AiApplyProgress
        {
            Type = AiApplyProgressType.ChangeStarted,
            ChangeId = changeId,
            Completed = completed,
            Total = total,
        };
    }

    public static AiApplyProgress ChangeCompleted(AiAppliedChangeResult result, int completed, int total)
    {
        return new AiApplyProgress
        {
            Type = AiApplyProgressType.ChangeCompleted,
            ChangeId = result.ChangeId,
            Status = result.Status,
            Completed = completed,
            Total = total,
            Message = result.Error,
        };
    }

    public static AiApplyProgress Finished(AiApplyResult result, int total)
    {
        return new AiApplyProgress
        {
            Type = AiApplyProgressType.Completed,
            ChangeSetStatus = result.Status,
            Completed = result.Results.Count,
            Total = total,
        };
    }

    public static AiApplyProgress Failed(string message)
    {
        return new AiApplyProgress { Type = AiApplyProgressType.Failed, Message = message };
    }
}
