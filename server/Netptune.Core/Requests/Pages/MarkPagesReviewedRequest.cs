namespace Netptune.Core.Requests.Pages;

public sealed record MarkPagesReviewedRequest
{
    public List<int> Ids { get; init; } = [];
}
