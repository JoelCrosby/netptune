namespace Netptune.Core.Services.Ai;

public sealed record WebFetchResult
{
    public required bool IsSuccess { get; init; }

    public string? Error { get; init; }

    public string? FinalUrl { get; init; }

    public string? Title { get; init; }

    public string? ContentType { get; init; }

    public string Content { get; init; } = string.Empty;

    public static WebFetchResult Failed(string error)
    {
        return new WebFetchResult { IsSuccess = false, Error = error };
    }
}

public sealed record WebSearchHit
{
    public required string Title { get; init; }

    public required string Url { get; init; }

    public string? Snippet { get; init; }
}

public sealed record WebSearchResult
{
    public required bool IsSuccess { get; init; }

    public string? Error { get; init; }

    public IReadOnlyList<WebSearchHit> Hits { get; init; } = [];

    public static WebSearchResult Failed(string error)
    {
        return new WebSearchResult { IsSuccess = false, Error = error };
    }
}

public interface IWebContentFetcher
{
    Task<WebFetchResult> Fetch(string url, CancellationToken cancellationToken);
}

public interface IWebSearchProvider
{
    bool IsConfigured { get; }

    Task<WebSearchResult> Search(string query, int take, CancellationToken cancellationToken);
}
