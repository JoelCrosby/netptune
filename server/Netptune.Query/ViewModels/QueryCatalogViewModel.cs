namespace Netptune.Query.ViewModels;

public sealed record QueryCatalogViewModel
{
    public required IReadOnlyList<QueryFieldViewModel> Fields { get; init; }

    public int MaximumDepth { get; init; }

    public int MaximumConditionCount { get; init; }
}
