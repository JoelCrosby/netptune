using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Pages;

public sealed record PageViewModel
{
    public int Id { get; init; }

    public int? ParentId { get; init; }

    public int? ProjectId { get; init; }

    public PageKind Kind { get; init; }

    public required string Title { get; init; }

    public required string Slug { get; init; }

    public string? Icon { get; init; }

    public string? Body { get; init; }

    public PageStatus Status { get; init; }

    public int Revision { get; init; }

    public bool IsFavourite { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public string? CreatedByUserId { get; init; }

    public string? CreatedByDisplayName { get; init; }

    public string? ModifiedByDisplayName { get; init; }

    public List<PageTagViewModel> Tags { get; init; } = [];
}
