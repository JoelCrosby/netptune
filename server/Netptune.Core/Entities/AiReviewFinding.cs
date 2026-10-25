using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public sealed record AiReviewFinding : KeyedEntity<long>
{
    public int RunId { get; init; }

    public int Sequence { get; init; }

    public EntityType TargetType { get; init; }

    public int TargetId { get; init; }

    public AiReviewFindingCategory Category { get; init; }

    public required string Title { get; init; }

    public required string Explanation { get; init; }

    public string? Quote { get; init; }

    public JsonDocument? Suggestion { get; init; }

    public AiReviewFindingStatus Status { get; set; }

    public string? DecidedByUserId { get; set; }

    public DateTime? DecidedAt { get; set; }

    public string? DismissReason { get; set; }

    public JsonDocument? AppliedChange { get; set; }

    [JsonIgnore]
    public AiReviewRun Run { get; init; } = null!;

    [JsonIgnore]
    public ICollection<AiReviewFindingReference> References { get; init; } =
        new HashSet<AiReviewFindingReference>(ReferenceEqualityComparer.Instance);
}
