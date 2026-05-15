using Mediator;

using Netptune.Core.Services;
using Netptune.Core.Services.Search;
using Netptune.Core.ViewModels.Search;

namespace Netptune.Handlers.Search;

public sealed record SearchQuery(string Q, string[]? Types = null, int Limit = 20) : IRequest<SearchResponse>;

public sealed class SearchQueryHandler : IRequestHandler<SearchQuery, SearchResponse>
{
    private readonly IMeilisearchService Search;
    private readonly IIdentityService Identity;

    public SearchQueryHandler(IMeilisearchService search, IIdentityService identity)
    {
        Search = search;
        Identity = identity;
    }

    public ValueTask<SearchResponse> Handle(SearchQuery request, CancellationToken cancellationToken)
    {
        var workspaceSlug = Identity.GetWorkspaceKey();
        var query = new GlobalSearchQuery(request.Q, workspaceSlug, request.Types, request.Limit);

        return new ValueTask<SearchResponse>(Search.SearchAsync(query, cancellationToken));
    }
}
