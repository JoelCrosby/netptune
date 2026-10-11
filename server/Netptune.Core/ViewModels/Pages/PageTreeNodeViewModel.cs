using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Pages;

public sealed record PageTreeNodeViewModel
{
    public int Id { get; init; }

    public int? ParentId { get; init; }

    public int? ProjectId { get; init; }

    public PageKind Kind { get; init; }

    public required string Title { get; init; }

    public required string Slug { get; init; }

    public string? Icon { get; init; }

    public double SortOrder { get; init; }

    public PageStatus Status { get; init; }

    public bool IsFavourite { get; init; }
}
