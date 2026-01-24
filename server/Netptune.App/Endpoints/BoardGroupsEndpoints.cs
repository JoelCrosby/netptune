using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class BoardGroupsEndpoints
{
    public static RouteGroupBuilder Map(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("boardgroups")
            .RequireAuthorization(NetptunePolicies.Workspace);

        group.MapGet("/{id}", HandleGet);
        group.MapPut("/", HandlePut);
        group.MapPost("/", HandlePost);
        group.MapDelete("/{id}", HandleDelete);

        return group;
    }

    public static async Task<IResult> HandleGet(
        IBoardGroupService boardGroupService,
        int id)
    {
        var result = await boardGroupService.GetBoardGroup(id);

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePut(
        IBoardGroupService boardGroupService,
        UpdateBoardGroupRequest request)
    {
        var result = await boardGroupService.Update(request);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePost(
        IBoardGroupService boardGroupService,
        AddBoardGroupRequest request)
    {
        var result = await boardGroupService.Create(request);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(
        IBoardGroupService boardGroupService,
        int id)
    {
        var result = await boardGroupService.Delete(id);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }
}
