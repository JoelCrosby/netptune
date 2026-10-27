using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public sealed record AiChangeSet : WorkspaceEntity<Guid>
{
    public Guid ConversationId { get; init; }

    public long MessageId { get; init; }

    public required string UserId { get; init; }

    public AiChangeSetStatus Status { get; set; }

    public Guid CorrelationId { get; init; }

    public DateTime? AppliedAt { get; set; }

    public DateTime? UndoneAt { get; set; }

    // Taken for the length of an apply, retry or undo so a second request cannot run the same changes
    // at the same time. A lease rather than a flag, so a run that dies part way does not lock the set.
    public DateTime? ClaimedUntil { get; set; }

    [JsonIgnore]
    public AiConversation Conversation { get; init; } = null!;

    [JsonIgnore]
    public ICollection<AiProposedChange> Changes { get; init; } = new HashSet<AiProposedChange>();
}
