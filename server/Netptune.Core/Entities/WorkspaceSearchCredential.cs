using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public sealed record WorkspaceSearchCredential : KeyedEntity<Guid>
{
    public int WorkspaceId { get; init; }

    public WebSearchProvider Provider { get; set; }

    public byte[]? Secret { get; set; }

    public string SecretHint { get; set; } = string.Empty;

    // Google calls this the search engine id (cx); unused by the other providers.
    public string? EngineId { get; set; }

    // Base URL of a self-hosted instance. SearXNG has no key, so this is what identifies it.
    public string? Endpoint { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; init; }

    public DateTime? LastUsedAt { get; set; }

    [JsonIgnore]
    public Workspace Workspace { get; init; } = null!;
}
