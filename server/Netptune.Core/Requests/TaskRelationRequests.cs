using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public sealed record CreateTaskRelationRequest
{
    [Required]
    public string SourceSystemId { get; init; } = null!;

    [Required]
    public string TargetSystemId { get; init; } = null!;

    public int RelationTypeId { get; init; }
}
