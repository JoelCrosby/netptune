using Mediator;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Services.Boards.Commands;
using Netptune.Services.Boards.Queries;

namespace Netptune.App.Endpoints;

public static class BoardsEndpoints
{
    public static RouteGroupBuilder MapBoardsEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("boards");

        group.MapGet("/{id}", HandleGet).RequireAuthorization(NetptunePermissions.Boards.Read);
        group.MapGet("/workspace", HandleGetBoardsInWorkspace).RequireAuthorization(NetptunePermissions.Boards.Read);
        group.MapGet("/project/{projectId}", HandleGetBoardsInProject).RequireAuthorization(NetptunePermissions.Boards.Read);
        group.MapGet("/view/{identifier}", HandleGetBoardView).RequireAuthorization(NetptunePermissions.Boards.Read);
        group.MapPut("/", HandlePut).RequireAuthorization(NetptunePermissions.Boards.Update);
        group.MapPost("/", HandlePost).RequireAuthorization(NetptunePermissions.Boards.Create);
        group.MapDelete("/{id}", HandleDelete).RequireAuthorization(NetptunePermissions.Boards.Delete);
        group.MapGet("/is-unique/{identifier}", HandleIsSlugUnique).RequireAuthorization(NetptunePermissions.Boards.Read);

        return group;
    }

    public static async Task<IResult> HandleGet(IMediator mediator, int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBoardQuery(id), cancellationToken);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetBoardsInWorkspace(IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBoardsInWorkspaceQuery(), cancellationToken);

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetBoardsInProject(IMediator mediator, int? projectId,
        CancellationToken cancellationToken)
    {
        if (!projectId.HasValue) return Results.BadRequest();

        var result = await mediator.Send(new GetBoardsInProjectQuery(projectId.Value), cancellationToken);

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetBoardView(
        IMediator mediator,
        string identifier,
        [AsParameters] BoardGroupsFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBoardViewQuery(identifier, filter), cancellationToken);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePut(IMediator mediator, UpdateBoardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateBoardCommand(request), cancellationToken);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePost(IMediator mediator, AddBoardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateBoardCommand(request), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(IMediator mediator, int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteBoardCommand(id), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleIsSlugUnique(IMediator mediator, string identifier,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new IsBoardIdentifierUniqueQuery(identifier), cancellationToken);

        return Results.Ok(result);
    }
}
