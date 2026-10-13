using Netptune.Core.Models.Search;

namespace Netptune.Core.Models.Pages;

public sealed record PageTextSearch
{
    public required TextPattern Pattern { get; init; }

    public IReadOnlyCollection<int>? ScopeIds { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
