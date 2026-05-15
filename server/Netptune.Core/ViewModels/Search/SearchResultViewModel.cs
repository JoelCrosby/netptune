namespace Netptune.Core.ViewModels.Search;

public record SearchResultViewModel
{
    public required string Type { get; init; }

    public required int Id { get; init; }

    public required string Title { get; init; }

    public required string Subtitle { get; init; }

    public required string Url { get; init; }

    public Dictionary<string, object?> Metadata { get; init; } = [];
}

public record SearchResponse
{
    public required List<SearchResultViewModel> Results { get; init; }

    public long ProcessingTimeMs { get; init; }
}
