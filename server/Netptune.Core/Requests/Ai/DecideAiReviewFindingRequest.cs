using Netptune.Core.Models.Ai;

namespace Netptune.Core.Requests.Ai;

public enum AiReviewDecision
{
    Accept = 0,
    Dismiss = 1,
    Reopen = 2,
}

public sealed record DecideAiReviewFindingRequest
{
    public AiReviewDecision Decision { get; init; }

    public string? Reason { get; init; }

    public string? Replace { get; init; }

    public bool Revert { get; init; }

    public List<AiReviewSplitTask>? SplitTasks { get; init; }

    public List<string>? Tags { get; init; }

    public bool ApplyEstimate { get; init; } = true;

    public bool ApplyPriority { get; init; } = true;
}
