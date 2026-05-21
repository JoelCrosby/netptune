using Mediator;

using Netptune.Core.Preferences;
using Netptune.Handlers.CommandPalette.Commands;
using Netptune.Handlers.CommandPalette.Queries;

namespace Netptune.App.Endpoints;

public static class CommandPaletteEndpoints
{
    public static RouteGroupBuilder MapCommandPaletteEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("command-palette")
            .RequireAuthorization();

        group.MapGet("/recent", HandleGetRecent);
        group.MapPost("/recent", HandlePostRecent);
        group.MapDelete("/recent", HandleDeleteRecent);

        return builder;
    }

    private static async Task<IResult> HandleGetRecent(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCommandPaletteRecentItemsQuery(), cancellationToken);

        if (!result.IsSuccess) return Results.BadRequest(result);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandlePostRecent(
        UpsertCommandPaletteRecentItemRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpsertCommandPaletteRecentItemCommand(request),
            cancellationToken);

        if (!result.IsSuccess) return Results.BadRequest(result);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDeleteRecent(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ClearCommandPaletteRecentItemsCommand(), cancellationToken);

        if (!result.IsSuccess) return Results.BadRequest(result);

        return Results.Ok(result);
    }
}
