namespace Netptune.Core.ViewModels.Pages;

public sealed record RecentPageViewModel
{
    public int Id { get; init; }

    public int? ParentId { get; init; }

    public required string Title { get; init; }

    public required string Slug { get; init; }

    public DateTime VisitedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public string? UpdatedByDisplayName { get; init; }
}
