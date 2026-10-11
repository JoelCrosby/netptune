namespace Netptune.Core.Requests.Pages;

public sealed record TransformPageTextRequest
{
    public int PageId { get; init; }

    public string? Action { get; init; }

    public string? Text { get; init; }

    public string? Language { get; init; }
}
