using Mediator;

using Netptune.Core.Authorization;
using Netptune.Handlers.Search;

namespace Netptune.App.Endpoints;

public static class SearchEndpoints
{
    public static RouteGroupBuilder MapSearchEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("search");

        group.MapGet("/", HandleSearch).RequireAuthorization(NetptunePermissions.Projects.Read);
        group.MapPost("/reindex", HandleReindex).RequireAuthorization(NetptunePermissions.Projects.Read);

        return group;
    }

    public static async Task<IResult> HandleSearch(
        IMediator mediator,
        string? q,
        [Microsoft.AspNetCore.Mvc.FromQuery(Name = "types")] string[]? types,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q)) return Results.Ok(new { results = Array.Empty<object>(), processingTimeMs = 0 });

        var result = await mediator.Send(new SearchQuery(q, types, limit), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleReindex(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new ReindexCommand(), cancellationToken);

        return Results.Ok();
    }
}
