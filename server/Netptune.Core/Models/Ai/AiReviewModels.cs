using System.Text.Json;

using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Requests.Ai;

namespace Netptune.Core.Models.Ai;

public enum AiReviewStreamEventType
{
    Progress = 0,
    Completed = 1,
    Failed = 2,
}

public sealed record AiReviewStreamEvent
{
    public AiReviewStreamEventType Type { get; init; }

    public int? Characters { get; init; }

    public int? RunId { get; init; }

    public string? Message { get; init; }

    public static AiReviewStreamEvent Progress(int characters)
    {
        return new AiReviewStreamEvent { Type = AiReviewStreamEventType.Progress, Characters = characters };
    }

    public static AiReviewStreamEvent Completed(int runId)
    {
        return new AiReviewStreamEvent { Type = AiReviewStreamEventType.Completed, RunId = runId };
    }

    public static AiReviewStreamEvent Failed(string message, int? runId = null)
    {
        return new AiReviewStreamEvent { Type = AiReviewStreamEventType.Failed, Message = message, RunId = runId };
    }
}

public static class AiReviewErrorCodes
{
    public const string TimedOut = "timed_out";

    public const string ProviderError = "provider_error";

    public const string UnreadableReply = "unreadable_reply";

    public const string OutputLimit = "output_limit";
}

public sealed record AiReviewLoadRequest
{
    public int WorkspaceId { get; init; }

    public int SubjectId { get; init; }
}

public sealed record AiReviewDismissal(AiReviewFindingCategory Category, string Title, string? Reason);

public sealed record AiReviewContext
{
    public required string Document { get; init; }

    public required string Fingerprint { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public required string UsagePurpose { get; init; }

    public IReadOnlyDictionary<string, string> EditableFields { get; init; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, int> CandidateTasks { get; init; } = new Dictionary<string, int>();

    public IReadOnlyList<string> WorkspaceTags { get; init; } = [];
}

public sealed record AiReviewApplyRequest
{
    public int WorkspaceId { get; init; }

    public int TargetId { get; init; }

    public required JsonElement Suggestion { get; init; }

    public required DecideAiReviewFindingRequest Decision { get; init; }
}

public sealed record AiReviewCreatedEntity(EntityType EntityType, int EntityId);

public sealed record AiReviewApplication
{
    public bool IsSuccess { get; init; }

    public string? Message { get; init; }

    public JsonDocument? AppliedChange { get; init; }

    public List<AssistantReviewAppliedEntity> Changes { get; init; } = [];

    public List<AiReviewCreatedEntity> Created { get; init; } = [];

    public static AiReviewApplication Failed(string? message)
    {
        return new AiReviewApplication { IsSuccess = false, Message = message };
    }
}
