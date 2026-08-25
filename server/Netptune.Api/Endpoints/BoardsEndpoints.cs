using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Realtime;
using Netptune.Core.ViewModels.Boards;
using Netptune.Handlers.Boards.Commands;
using Netptune.Handlers.Boards.Queries;
using Netptune.Api.Configuration;
using Netptune.Api.Requests;

namespace Netptune.Api.Endpoints;

public static class BoardsEndpoints
{
    public static RouteGroupBuilder MapBoardsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/boards", GetBoards)
            .WithSummary("List boards")
            .WithDescription(
                "Returns the boards in the credential's workspace, each carrying the project it belongs to. "
                + "Supply projectId to list only the boards belonging to that project.")
            .RequireAuthorization(NetptunePermissions.Boards.Read);

        group.MapGet("/boards/{id:int}", GetBoard)
            .WithSummary("Get a board")
            .WithDescription("Returns a board by its numeric identifier, including its columns.")
            .RequireAuthorization(NetptunePermissions.Boards.Read);

        group.MapGet("/boards/{identifier}/view", GetBoardView)
            .WithSummary("Get a board view")
            .WithDescription(
                "Returns a board addressed by its identifier with the tasks in each column, filtered the same way "
                + "the board is filtered in the app.")
            .RequireAuthorization(NetptunePermissions.Boards.Read);

        group.MapPost("/boards", CreateBoard)
            .WithSummary("Create a board")
            .WithDescription("Creates a board in an existing project. The identifier must be unique in the workspace.")
            .RequireAuthorization(NetptunePermissions.Boards.Create)
            .Broadcasts(WorkspaceEventScopes.Board);

        group.MapPatch("/boards/{id:int}", UpdateBoard)
            .WithSummary("Update a board")
            .WithDescription("Updates the supplied fields on an existing board.")
            .RequireAuthorization(NetptunePermissions.Boards.Update)
            .Broadcasts(WorkspaceEventScopes.Board);

        group.MapDelete("/boards/{id:int}", DeleteBoard)
            .WithSummary("Delete a board")
            .WithDescription("Deletes a board and the columns belonging to it.")
            .RequireAuthorization(NetptunePermissions.Boards.Delete)
            .Broadcasts(WorkspaceEventScopes.Board);

        return group;
    }

    private static async Task<Results<Ok<List<BoardViewModel>>, NotFound>> GetBoards(
        IMediator mediator,
        int? projectId,
        [AsParameters] PageRequest page,
        CancellationToken cancellationToken)
    {
        if (projectId.HasValue)
        {
            var boardsInProject = await mediator.Send(
                new GetBoardsInProjectQuery(projectId.Value, page),
                cancellationToken);

            return boardsInProject is null ? TypedResults.NotFound() : TypedResults.Ok(boardsInProject);
        }

        var groupedBoards = await mediator.Send(new GetBoardsInWorkspaceQuery(page), cancellationToken);

        if (groupedBoards is null)
        {
            return TypedResults.NotFound();
        }

        var boards = groupedBoards.SelectMany(project => project.Boards).ToList();

        return TypedResults.Ok(boards);
    }

    private static async Task<Results<Ok<BoardViewModel>, NotFound>> GetBoard(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBoardQuery(id), cancellationToken);

        return result.IsNotFound ? TypedResults.NotFound() : TypedResults.Ok(result.Payload);
    }

    private static async Task<Results<Ok<BoardView>, NotFound>> GetBoardView(
        IMediator mediator,
        string identifier,
        [AsParameters] PublicBoardViewFilter filter,
        CancellationToken cancellationToken)
    {
        var boardGroupsFilter = filter.ToBoardGroupsFilter();
        var result = await mediator.Send(new GetBoardViewQuery(identifier, boardGroupsFilter), cancellationToken);

        return result.IsNotFound ? TypedResults.NotFound() : TypedResults.Ok(result.Payload);
    }

    private static async Task<Results<Created<BoardViewModel>, NotFound, BadRequest<ClientResponse<BoardViewModel>>>> CreateBoard(
        IMediator mediator,
        AddBoardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateBoardCommand(request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(result);
        }

        return TypedResults.Created($"/api/v1/boards/{result.Payload!.Id}", result.Payload);
    }

    private static async Task<Results<Ok<BoardViewModel>, NotFound, BadRequest<ClientResponse<BoardViewModel>>>> UpdateBoard(
        IMediator mediator,
        int id,
        PublicUpdateBoardRequest request,
        CancellationToken cancellationToken)
    {
        var updateRequest = request.ToRequest(id);
        var result = await mediator.Send(new UpdateBoardCommand(updateRequest), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ClientResponse>>> DeleteBoard(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteBoardCommand(id), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }
}
