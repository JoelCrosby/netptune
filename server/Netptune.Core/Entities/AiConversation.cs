using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public sealed record AiConversation : WorkspaceEntity<Guid>
{
    public required string UserId { get; init; }

    public required string Title { get; set; }

    public AiProvider Provider { get; set; }

    public required string Model { get; set; }

    public DateTime LastMessageAt { get; set; }

    public int MessageCount { get; set; }

    [JsonIgnore]
    public AppUser User { get; init; } = null!;

    [JsonIgnore]
    public ICollection<AiMessage> Messages { get; init; } = new HashSet<AiMessage>();
}
