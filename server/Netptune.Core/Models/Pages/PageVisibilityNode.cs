using Netptune.Core.Enums;

namespace Netptune.Core.Models.Pages;

public sealed record PageVisibilityNode
{
    public int Id { get; init; }

    public int? ParentId { get; init; }

    public PageKind Kind { get; init; }

    public PageStatus Status { get; init; }
}
