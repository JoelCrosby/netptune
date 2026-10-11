namespace Netptune.Core.ViewModels.Pages;

public sealed record PageContributorViewModel
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public string? PictureUrl { get; init; }

    public DateTime LastEditedAt { get; init; }
}
