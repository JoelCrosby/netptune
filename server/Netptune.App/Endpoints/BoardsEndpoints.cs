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

    public static async Task<IResult> HandleGet(IMediator mediator, int id)
    {
        var result = await mediator.Send(new GetBoardQuery(id));

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetBoardsInWorkspace(IMediator mediator)
    {
        var result = await mediator.Send(new GetBoardsInWorkspaceQuery());

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetBoardsInProject(IMediator mediator, int? projectId)
    {
        if (!projectId.HasValue) return Results.BadRequest();

        var result = await mediator.Send(new GetBoardsInProjectQuery(projectId.Value));

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetBoardView(
        IMediator mediator,
        string identifier,
        [AsParameters] BoardGroupsFilter filter)
    {
        var result = await mediator.Send(new GetBoardViewQuery(identifier, filter));

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePut(IMediator mediator, UpdateBoardRequest request)
    {
        var result = await mediator.Send(new UpdateBoardCommand(request));

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePost(IMediator mediator, AddBoardRequest request)
    {
        var result = await mediator.Send(new CreateBoardCommand(request));

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(IMediator mediator, int id)
    {
        var result = await mediator.Send(new DeleteBoardCommand(id));

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleIsSlugUnique(IMediator mediator, string identifier)
    {
        var result = await mediator.Send(new IsBoardIdentifierUniqueQuery(identifier));

        return Results.Ok(result);
    }
}
