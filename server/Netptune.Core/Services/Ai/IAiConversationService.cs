using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public sealed record AiClientContext
{
    public string? View { get; init; }

    public int? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public int? BoardId { get; init; }

    public string? BoardName { get; init; }

    public int? SprintId { get; init; }

    public string? SprintName { get; init; }

    public string? TaskSystemId { get; init; }

    public string? TaskName { get; init; }
}

public sealed record AiAnswerRequest
{
    public Guid QuestionId { get; init; }

    public List<string> SelectedLabels { get; init; } = [];

    public string? Text { get; init; }
}

// Points the assistant at one proposal it already made, so a request to rework it does not
// depend on the reviewer describing which change they mean.
public sealed record AiReviseRequest
{
    public Guid ChangeSetId { get; init; }

    public long ChangeId { get; init; }
}

public sealed record AiSendMessageRequest
{
    public Guid? ConversationId { get; init; }

    public required string Text { get; init; }

    public AiAnswerRequest? Answer { get; init; }

    public AiProvider? Provider { get; init; }

    public string? Model { get; init; }

    public AiEffort? Effort { get; init; }

    public AiClientContext? Context { get; init; }

    public string? Locale { get; init; }

    public bool Retry { get; init; }

    public AiReviseRequest? Revise { get; init; }
}

public interface IAiConversationService
{
    IAsyncEnumerable<AiStreamEvent> SendMessage(AiSendMessageRequest request, CancellationToken cancellationToken);
}
