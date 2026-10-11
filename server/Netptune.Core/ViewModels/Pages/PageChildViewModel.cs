using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Pages;

public sealed record PageChildViewModel
{
    public int Id { get; init; }

    public PageKind Kind { get; init; }

    public required string Title { get; init; }

    public required string Slug { get; init; }

    public PageStatus Status { get; init; }

    public string? Excerpt { get; init; }

    public List<PageTagViewModel> Tags { get; init; } = [];

    public DateTime UpdatedAt { get; init; }

    public string? EditorDisplayName { get; init; }

    public string? EditorPictureUrl { get; init; }

    public int ReadMinutes { get; init; }

    public int ChildCount { get; init; }
}
