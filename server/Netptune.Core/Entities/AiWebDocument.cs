using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public sealed record AiWebDocument : KeyedEntity<Guid>
{
    public int WorkspaceId { get; init; }

    public Guid? ConversationId { get; init; }

    public required string RequestedUrl { get; init; }

    public required string FinalUrl { get; init; }

    public string? Title { get; init; }

    public string? ContentType { get; init; }

    public required string Content { get; init; }

    public int CharacterCount { get; init; }

    public DateTime FetchedAt { get; init; }

    public DateTime ExpiresAt { get; init; }
}
