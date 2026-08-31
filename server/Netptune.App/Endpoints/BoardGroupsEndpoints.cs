using Mediator;

using Netptune.App.Configuration;
using Netptune.App.Utility;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Realtime;
using Netptune.Handlers.BoardGroups.Commands;
using Netptune.Handlers.BoardGroups.Queries;

namespace Netptune.App.Endpoints;

public static class BoardGroupsEndpoints
{
    public static RouteGroupBuilder MapBoardGroupsEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("boardgroups");

        group.MapGet("/options", HandleGetOptions).RequireAuthorization(NetptunePermissions.BoardGroups.Read);
        group.MapGet("/{id}", HandleGet).RequireAuthorization(NetptunePermissions.BoardGroups.Read);
        group.MapPut("/", HandlePut).RequireAuthorization(NetptunePermissions.BoardGroups.Update)
            .Broadcasts(WorkspaceEventScopes.Board);
        group.MapPost("/", HandlePost).RequireAuthorization(NetptunePermissions.BoardGroups.Create)
            .Broadcasts(WorkspaceEventScopes.Board);
        group.MapDelete("/{id}", HandleDelete).RequireAuthorization(NetptunePermissions.BoardGroups.Delete)
            .Broadcasts(WorkspaceEventScopes.Board);

        return group;
    }

    public static async Task<IResult> HandleGetOptions(IMediator mediator, CancellationToken cancellationToken)
    {
        var options = await mediator.Send(new GetBoardGroupOptionsQuery(), cancellationToken);

        return Results.Ok(options);
    }

    public static async Task<IResult> HandleGet(IMediator mediator, int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBoardGroupQuery(id), cancellationToken);

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePut(
        IMediator mediator,
        UpdateBoardGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateBoardGroupCommand(request), cancellationToken);

        return result.ToResult();
    }

    public static async Task<IResult> HandlePost(
        IMediator mediator,
        AddBoardGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateBoardGroupCommand(request), cancellationToken);

        return result.ToResult();
    }

    public static async Task<IResult> HandleDelete(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteBoardGroupCommand(id), cancellationToken);

        return result.ToResult();
    }

}
