using System.Text.Json.Serialization;

namespace Netptune.Core.Entities;

public sealed record ServiceAccountOwner
{
    public int ServiceAccountId { get; init; }

    public required string UserId { get; init; }

    public DateTime CreatedAt { get; init; }

    [JsonIgnore]
    public ServiceAccount ServiceAccount { get; init; } = null!;

    [JsonIgnore]
    public AppUser User { get; init; } = null!;
}
