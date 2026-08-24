using Mediator;

using Netptune.App.Utility;
using Netptune.Core.Authorization;
using Netptune.Handlers.Pins.Commands;
using Netptune.Handlers.Pins.Queries;

namespace Netptune.App.Endpoints;

public static class PinsEndpoints
{
    public static RouteGroupBuilder MapPinsEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("pins");

        group.MapGet("/", HandleGetAll)
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        group.MapGet("/board/{boardId}", HandleGetBoard)
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        group.MapPost("/", HandleCreate)
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        group.MapDelete("/{id}", HandleDelete)
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        group.MapPut("/reorder", HandleReorder)
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        return group;
    }

    private static async Task<IResult> HandleGetAll(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPinnedTasksQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetBoard(IMediator mediator, int boardId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBoardPinsQuery(boardId), cancellationToken);

        return result.ToPayloadResult();
    }

    private static async Task<IResult> HandleCreate(
        IMediator mediator,
        CreateTaskPinRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateTaskPinCommand(request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleDelete(IMediator mediator, int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTaskPinCommand(id), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleReorder(
        IMediator mediator,
        ReorderTaskPinsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReorderTaskPinsCommand(request), cancellationToken);

        return result.ToResult();
    }
}
