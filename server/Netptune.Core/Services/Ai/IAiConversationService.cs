using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public sealed record AiClientContext
{
    public string? View { get; init; }

    public int? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public int? BoardId { get; init; }

    public int? SprintId { get; init; }

    public string? TaskSystemId { get; init; }

    public string? TaskName { get; init; }
}

public sealed record AiSendMessageRequest
{
    public Guid? ConversationId { get; init; }

    public required string Text { get; init; }

    public AiProvider? Provider { get; init; }

    public string? Model { get; init; }

    public AiClientContext? Context { get; init; }

    public string? Locale { get; init; }
}

public interface IAiConversationService
{
    IAsyncEnumerable<AiStreamEvent> SendMessage(AiSendMessageRequest request, CancellationToken cancellationToken);
}
