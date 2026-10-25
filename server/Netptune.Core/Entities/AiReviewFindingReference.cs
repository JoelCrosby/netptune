using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public sealed record AiReviewFindingReference : KeyedEntity<long>
{
    public long FindingId { get; init; }

    public AiReviewReferenceRole Role { get; init; }

    public EntityType EntityType { get; init; }

    public int EntityId { get; init; }

    [JsonIgnore]
    public AiReviewFinding Finding { get; init; } = null!;
}
