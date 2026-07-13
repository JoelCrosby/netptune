using System.ComponentModel.DataAnnotations;

using Netptune.Core.Enums;

namespace Netptune.Core.Requests;

public sealed record CreateRelationTypeRequest
{
    [Required]
    [MaxLength(128)]
    public string Name { get; init; } = null!;

    [MaxLength(128)]
    public string? InverseName { get; init; }

    [MaxLength(512)]
    public string? Description { get; init; }

    [MaxLength(32)]
    public string? Color { get; init; }

    public RelationCategory Category { get; init; } = RelationCategory.Related;
}

public sealed record UpdateRelationTypeRequest
{
    public int Id { get; init; }

    [Required]
    [MaxLength(128)]
    public string Name { get; init; } = null!;

    [MaxLength(128)]
    public string? InverseName { get; init; }

    [MaxLength(512)]
    public string? Description { get; init; }

    [MaxLength(32)]
    public string? Color { get; init; }
}

public sealed record ReorderRelationTypesRequest
{
    public IReadOnlyList<int> RelationTypeIds { get; init; } = [];
}
