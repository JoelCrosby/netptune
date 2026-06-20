using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Handlers.Statuses.Commands;
using Netptune.Handlers.Statuses.Queries;

namespace Netptune.App.Endpoints;

public static class StatusesEndpoints
{
    public static RouteGroupBuilder MapStatusesEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("statuses");

        group.MapGet("/", HandleGet).RequireAuthorization(NetptunePermissions.Statuses.Read);
        group.MapPost("/", HandlePost).RequireAuthorization(NetptunePermissions.Statuses.Manage);
        group.MapPut("/", HandlePut).RequireAuthorization(NetptunePermissions.Statuses.Manage);
        group.MapDelete("/{id:int}", HandleDelete).RequireAuthorization(NetptunePermissions.Statuses.Manage);
        group.MapPost("/reorder", HandleReorder).RequireAuthorization(NetptunePermissions.Statuses.Manage);

        return builder;
    }

    private static async Task<IResult> HandleGet(
        IMediator mediator,
        [AsParameters] StatusFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetStatusesQuery(filter), cancellationToken);

        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> HandlePost(
        IMediator mediator,
        CreateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateStatusCommand(request), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandlePut(
        IMediator mediator,
        UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateStatusCommand(request), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDelete(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteStatusCommand(id), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleReorder(
        IMediator mediator,
        ReorderStatusesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReorderStatusesCommand(request), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }
}
