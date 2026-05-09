using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Relationships;

public record WorkspaceInvite : KeyedEntity<int>
{
    public string Email { get; set; } = null!;

    public int WorkspaceId { get; set; }

    public string Code { get; set; } = null!;

    public string? InvitedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    [JsonIgnore]
    public Workspace Workspace { get; set; } = null!;

    [JsonIgnore]
    public AppUser? InvitedBy { get; set; }
}
