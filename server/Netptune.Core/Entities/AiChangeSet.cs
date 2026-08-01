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

    [JsonIgnore]
    public AiConversation Conversation { get; init; } = null!;

    [JsonIgnore]
    public ICollection<AiProposedChange> Changes { get; init; } = new HashSet<AiProposedChange>();
}
