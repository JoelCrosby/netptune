using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Pages;

public sealed record PageNeedingReviewViewModel
{
    public int Id { get; init; }

    public int? ParentId { get; init; }

    public required string Title { get; init; }

    public required string Slug { get; init; }

    public DateTime UpdatedAt { get; init; }

    public PageReviewReason Reason { get; init; }
}
