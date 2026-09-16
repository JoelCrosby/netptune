using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.ViewModels.Search;
using Netptune.Handlers.Search.Queries;

namespace Netptune.Api.Endpoints;

public static class SearchEndpoints
{
    public static RouteGroupBuilder MapSearchEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/search", Search)
            .WithSummary("Search the workspace")
            .WithDescription(
                "Searches tasks, projects, boards and other entities in the credential's workspace. "
                + "Supply types to restrict the search to particular entity types. "
                + $"limit defaults to {SearchQueryHandler.DefaultLimit} and is capped at {SearchQueryHandler.MaximumLimit}.")
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

        var query = new SearchQuery(q, types, limit ?? SearchQueryHandler.DefaultLimit);
        var result = await mediator.Send(query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
