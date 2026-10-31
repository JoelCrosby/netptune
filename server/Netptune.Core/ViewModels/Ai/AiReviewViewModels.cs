using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Ai;

public enum AiReviewReadiness
{
    Unreviewed = 0,
    NeedsWork = 1,
    Ready = 2,
    Stale = 3,
}

public sealed record AiReviewRunViewModel
{
    public int Id { get; init; }

    public EntityType SubjectType { get; init; }

    public int SubjectId { get; init; }

    public AiReviewRunStatus Status { get; init; }

    public AiProvider Provider { get; init; }

    public required string Model { get; init; }

    public string? Summary { get; init; }

    public string? ErrorCode { get; init; }

    public DateTime StartedAt { get; init; }

    public DateTime CreatedAt { get; init; }

    public string? RunByUserId { get; init; }

    public string? RunByDisplayName { get; init; }

    public string? RunByPictureUrl { get; init; }

    public int OpenCount { get; init; }

    public int AcceptedCount { get; init; }

    public int DismissedCount { get; init; }

    public List<AiReviewToolCallViewModel>? ToolCalls { get; init; }

    [JsonIgnore]
    public string BaselineFingerprint { get; init; } = string.Empty;
}

public sealed record AiReviewToolCallViewModel
{
    public required string Name { get; init; }

    public required string Label { get; init; }

    public bool IsError { get; init; }

    public bool Truncated { get; init; }
}

public sealed record AiReviewReferenceViewModel
{
    public AiReviewReferenceRole Role { get; init; }

    public EntityType EntityType { get; init; }

    public int EntityId { get; init; }

    public string? SystemId { get; init; }

    public string? Name { get; init; }
}

public sealed record AiReviewFindingViewModel
{
    public long Id { get; init; }

    public int Sequence { get; init; }

    public EntityType TargetType { get; init; }

    public int TargetId { get; init; }

    public AiReviewFindingCategory Category { get; init; }

    public required string Title { get; init; }

    public required string Explanation { get; init; }

    public string? Quote { get; init; }

    public JsonElement? Suggestion { get; init; }

    public AiReviewFindingStatus Status { get; init; }

    public string? DecidedByUserId { get; init; }

    public string? DecidedByDisplayName { get; init; }

    public DateTime? DecidedAt { get; init; }

    public string? DismissReason { get; init; }

    public JsonElement? AppliedChange { get; init; }

    public List<AiReviewReferenceViewModel> References { get; init; } = [];
}

public sealed record AiReviewRunDetailViewModel
{
    public required AiReviewRunViewModel Run { get; init; }

    public List<AiReviewFindingViewModel> Findings { get; init; } = [];
}

public sealed record AiReviewMentionViewModel
{
    public int RunId { get; init; }

    public EntityType SubjectType { get; init; }

    public int SubjectId { get; init; }

    public string? SubjectSystemId { get; init; }

    public string? SubjectName { get; init; }

    public long FindingId { get; init; }

    public required string FindingTitle { get; init; }

    public AiReviewFindingCategory Category { get; init; }
}

public sealed record AiReviewOriginViewModel
{
    public int RunId { get; init; }

    public required string FindingTitle { get; init; }

    public int ParentTaskId { get; init; }

    public string? ParentSystemId { get; init; }

    public string? ParentName { get; init; }

    public string? AcceptedByDisplayName { get; init; }

    public DateTime? AcceptedAt { get; init; }
}

public sealed record AiReviewLatestViewModel
{
    public AiReviewRunDetailViewModel? Current { get; init; }

    public AiReviewRunViewModel? LatestFailure { get; init; }

    public bool IsStale { get; init; }

    public AiReviewReadiness Readiness { get; init; }

    public int OpenCount { get; init; }

    public List<AiReviewMentionViewModel> MentionedIn { get; init; } = [];

    public AiReviewOriginViewModel? Origin { get; init; }
}
