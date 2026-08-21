using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.ViewModels.Search;
using Netptune.Handlers.Search;

namespace Netptune.PublicApi.Endpoints;

public static class SearchEndpoints
{
    private const int DefaultLimit = 20;

    public static RouteGroupBuilder MapSearchEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/search", Search)
            .WithSummary("Search the workspace")
            .WithDescription(
                "Searches tasks, projects, boards and other entities in the credential's workspace. "
                + "Supply types to restrict the search to particular entity types.")
            .RequireAuthorization(NetptunePermissions.Projects.Read);

        return group;
    }

    private static async Task<Results<Ok<SearchResponse>, BadRequest<string>>> Search(
        IMediator mediator,
        string q,
        string[]? types,
        int? limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return TypedResults.BadRequest("A search term is required.");
        }

        var result = await mediator.Send(new SearchQuery(q, types, limit ?? DefaultLimit), cancellationToken);

        return TypedResults.Ok(result);
    }
}
