using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class BoardsEndpoints
{
    public static RouteGroupBuilder Map(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("boards")
            .RequireAuthorization(NetptunePolicies.Workspace);

        group.MapGet("/{id}", HandleGet);
        group.MapGet("/workspace", HandleGetBoardsInWorkspace);
        group.MapGet("/project/{projectId}", HandleGetBoardsInProject);
        group.MapGet("/view/{identifier}", HandleGetBoardView);
        group.MapPut("/", HandlePut);
        group.MapPost("/", HandlePost);
        group.MapDelete("/{id}", HandleDelete);
        group.MapGet("/is-unique/{identifier}", HandleIsSlugUnique);

        return group;
    }

    public static async Task<IResult> HandleGet(
        IBoardService boardService,
        int id)
    {
        var result = await boardService.GetBoard(id);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetBoardsInWorkspace(
        IBoardService boardService)
    {
        var result = await boardService.GetBoardsInWorkspace();

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetBoardsInProject(
        IBoardService boardService,
        int? projectId)
    {
        if (!projectId.HasValue) return Results.BadRequest();

        var result = await boardService.GetBoardsInProject(projectId.Value);

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetBoardView(
        IBoardService boardService,
        string identifier,
        [AsParameters] BoardGroupsFilter filter)
    {
        var result = await boardService.GetBoardView(identifier, filter);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePut(
        IBoardService boardService,
        UpdateBoardRequest request)
    {
        var result = await boardService.Update(request);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePost(
        IBoardService boardService,
        AddBoardRequest request)
    {
        var result = await boardService.Create(request);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(
        IBoardService boardService,
        int id)
    {
        var result = await boardService.Delete(id);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleIsSlugUnique(
        IBoardService boardService,
        string identifier)
    {
        var result = await boardService.IsIdentifierUnique(identifier);

        return Results.Ok(result);
    }
}
