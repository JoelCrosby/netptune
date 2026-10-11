using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Pages;

public sealed record ArchivedPageViewModel
{
    public int Id { get; init; }

    public PageKind Kind { get; init; }

    public required string Title { get; init; }

    public required string Slug { get; init; }

    public DateTime? ArchivedAt { get; init; }

    public string? ArchivedByDisplayName { get; init; }

    public string? CreatedByUserId { get; init; }
}
