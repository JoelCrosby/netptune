using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.RelationTypes;

public sealed record RelationTypeViewModel
{
    public int Id { get; init; }

    public int WorkspaceId { get; init; }

    public string Name { get; init; } = null!;

    public string InverseName { get; init; } = null!;

    public string Key { get; init; } = null!;

    public string? Description { get; init; }

    public string? Color { get; init; }

    public double SortOrder { get; init; }

    public RelationCategory Category { get; init; }

    public bool IsSystem { get; init; }
}
