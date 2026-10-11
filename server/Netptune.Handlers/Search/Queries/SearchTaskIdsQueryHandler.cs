using Mediator;

using Netptune.Core.Services;
using Netptune.Core.Services.Search;

namespace Netptune.Handlers.Search.Queries;

public sealed record SearchTaskIdsQuery(string Q, int Limit, int? ProjectId = null, string? AssigneeId = null)
    : IRequest<TaskIdSearchResult?>;

public sealed class SearchTaskIdsQueryHandler : IRequestHandler<SearchTaskIdsQuery, TaskIdSearchResult?>
{
    public const int MaximumLimit = 100;

    private readonly IMeilisearchService Search;
    private readonly IIdentityService Identity;

    public SearchTaskIdsQueryHandler(IMeilisearchService search, IIdentityService identity)
    {
        Search = search;
        Identity = identity;
    }

    public async ValueTask<TaskIdSearchResult?> Handle(SearchTaskIdsQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var limit = Math.Clamp(request.Limit, 1, MaximumLimit);
        var query = new TaskIdSearchQuery(request.Q, workspaceKey, limit, request.ProjectId, request.AssigneeId);
        var result = await Search.SearchTaskIdsAsync(query, cancellationToken);

        return result;
    }
}
