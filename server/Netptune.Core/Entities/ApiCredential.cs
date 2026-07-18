using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public sealed record ApiCredential : KeyedEntity<Guid>
{
    public int ServiceAccountId { get; init; }

    public required string Name { get; init; }

    public required string TokenPrefix { get; init; }

    public required byte[] SecretHash { get; init; }

    public List<string> Scopes { get; init; } = [];

    public DateTime CreatedAt { get; init; }

    public required string CreatedByUserId { get; init; }

    public DateTime ExpiresAt { get; init; }

    public DateTime? RevokedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    [JsonIgnore]
    public ServiceAccount ServiceAccount { get; init; } = null!;

    [JsonIgnore]
    public AppUser CreatedByUser { get; init; } = null!;
}
