using Netptune.App.Services;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class BoardGroupsEndpoints
{
    public static RouteGroupBuilder MapBoardGroupsEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("boardgroups");

        group.MapGet("/{id}", HandleGet).RequireAuthorization(NetptunePermissions.BoardGroups.Read);
        group.MapPut("/", HandlePut).RequireAuthorization(NetptunePermissions.BoardGroups.Update);
        group.MapPost("/", HandlePost).RequireAuthorization(NetptunePermissions.BoardGroups.Create);
        group.MapDelete("/{id}", HandleDelete).RequireAuthorization(NetptunePermissions.BoardGroups.Delete);

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
        IBoardEventService boardEventService,
        HttpContext context,
        UpdateBoardGroupRequest request)
    {
        var result = await boardGroupService.Update(request);

        if (result.IsNotFound) return Results.NotFound();

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePost(
        IBoardGroupService boardGroupService,
        IBoardEventService boardEventService,
        HttpContext context,
        AddBoardGroupRequest request)
    {
        var result = await boardGroupService.Create(request);

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(
        IBoardGroupService boardGroupService,
        IBoardEventService boardEventService,
        HttpContext context,
        int id)
    {
        var result = await boardGroupService.Delete(id);

        if (result.IsNotFound) return Results.NotFound(result);

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    private static Task BroadcastAsync(IBoardEventService boardEventService, HttpContext context)
    {
        var group = context.Request.Headers["X-Group"].ToString();
        var clientId = context.Connection.Id;

        if (string.IsNullOrEmpty(group)) return Task.CompletedTask;

        return boardEventService.BroadcastAsync(group, clientId);
    }
}
