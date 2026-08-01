using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public sealed record AiSendMessageRequest
{
    public Guid? ConversationId { get; init; }

    public required string Text { get; init; }
}

public interface IAiConversationService
{
    IAsyncEnumerable<AiStreamEvent> SendMessage(AiSendMessageRequest request, CancellationToken cancellationToken);
}
