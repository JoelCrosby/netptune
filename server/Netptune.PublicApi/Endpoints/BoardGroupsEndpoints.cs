using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Realtime;
using Netptune.Core.ViewModels.Boards;
using Netptune.Handlers.BoardGroups.Commands;
using Netptune.Handlers.BoardGroups.Queries;
using Netptune.PublicApi.Configuration;
using Netptune.PublicApi.Requests;

namespace Netptune.PublicApi.Endpoints;

public static class BoardGroupsEndpoints
{
    public static RouteGroupBuilder MapBoardGroupsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/board-groups", GetBoardGroups)
            .WithSummary("List board groups")
            .WithDescription(
                "Returns the board columns in the credential's workspace, with the board and project each belongs to. "
                + "Use the returned id as boardGroupId when moving a task between columns.")
            .RequireAuthorization(NetptunePermissions.BoardGroups.Read);

        group.MapGet("/board-groups/{id:int}", GetBoardGroup)
            .WithSummary("Get a board group")
            .WithDescription("Returns a board column by its numeric identifier.")
            .RequireAuthorization(NetptunePermissions.BoardGroups.Read);

        group.MapPost("/board-groups", CreateBoardGroup)
            .WithSummary("Create a board group")
            .WithDescription("Adds a column to an existing board, optionally bound to a workspace status.")
            .RequireAuthorization(NetptunePermissions.BoardGroups.Create)
            .Broadcasts(WorkspaceEventScopes.Board);

        group.MapPatch("/board-groups/{id:int}", UpdateBoardGroup)
            .WithSummary("Update a board group")
            .WithDescription(
                "Updates the supplied fields on an existing board column. Send clearStatus to unbind the column from "
                + "its status rather than leaving it unchanged.")
            .RequireAuthorization(NetptunePermissions.BoardGroups.Update)
            .Broadcasts(WorkspaceEventScopes.Board);

        group.MapDelete("/board-groups/{id:int}", DeleteBoardGroup)
            .WithSummary("Delete a board group")
            .WithDescription("Deletes a board column.")
            .RequireAuthorization(NetptunePermissions.BoardGroups.Delete)
            .Broadcasts(WorkspaceEventScopes.Board);

        return group;
    }

    private static async Task<Ok<List<BoardGroupOptionViewModel>>> GetBoardGroups(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBoardGroupOptionsQuery(), cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<BoardGroupViewModel>, NotFound>> GetBoardGroup(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBoardGroupQuery(id), cancellationToken);

        if (result is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(result.ToViewModel());
    }

    private static async Task<Results<Created<BoardGroupViewModel>, NotFound, BadRequest<ClientResponse<BoardGroupViewModel>>>> CreateBoardGroup(
        IMediator mediator,
        AddBoardGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateBoardGroupCommand(request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(result);
        }

        return TypedResults.Created($"/api/v1/board-groups/{result.Payload!.Id}", result.Payload);
    }

    private static async Task<Results<Ok<BoardGroupViewModel>, NotFound, BadRequest<ClientResponse<BoardGroupViewModel>>>> UpdateBoardGroup(
        IMediator mediator,
        int id,
        PublicUpdateBoardGroupRequest request,
        CancellationToken cancellationToken)
    {
        var updateRequest = request.ToRequest(id);
        var result = await mediator.Send(new UpdateBoardGroupCommand(updateRequest), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ClientResponse>>> DeleteBoardGroup(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteBoardGroupCommand(id), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }
}
