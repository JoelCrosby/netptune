using Microsoft.AspNetCore.Mvc;

using Netptune.App.Services;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class TagsEndpoints
{
    public static RouteGroupBuilder MapTagsEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("tags");

        group.MapPost("/", HandlePost).RequireAuthorization(NetptunePermissions.Tags.Create);
        group.MapPost("/task", HandlePostTaskTag).RequireAuthorization(NetptunePermissions.Tags.Assign);
        group.MapGet("/task/{systemId}", HandleGetTagsForTask).RequireAuthorization(NetptunePermissions.Tags.Read);
        group.MapGet("/workspace", HandleGetTagsForWorkspace).RequireAuthorization(NetptunePermissions.Tags.Read);
        group.MapDelete("/", HandleDelete).RequireAuthorization(NetptunePermissions.Tags.Delete);
        group.MapDelete("/task", HandleDeleteFromTask).RequireAuthorization(NetptunePermissions.Tags.Assign);
        group.MapPatch("/", HandleUpdateTag).RequireAuthorization(NetptunePermissions.Tags.Update);

        return group;
    }

    public static async Task<IResult> HandlePost(
        ITagService tagService,
        [FromBody] AddTagRequest request)
    {
        var result = await tagService.Create(request);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePostTaskTag(
        ITagService tagService,
        IBoardEventService boardEventService,
        HttpContext context,
        [FromBody] AddTagToTaskRequest request)
    {
        var result = await tagService.AddToTask(request);

        if (result.IsNotFound) return Results.NotFound();

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetTagsForTask(
        ITagService tagService,
        string systemId)
    {
        var result = await tagService.GetTagsForTask(systemId);

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetTagsForWorkspace(
        ITagService tagService)
    {
        var result = await tagService.GetTagsForWorkspace();

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(
        ITagService tagService,
        [FromBody] DeleteTagsRequest request)
    {
        var result = await tagService.Delete(request);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDeleteFromTask(
        ITagService tagService,
        IBoardEventService boardEventService,
        HttpContext context,
        [FromBody] DeleteTagFromTaskRequest request)
    {
        var result = await tagService.DeleteFromTask(request);

        if (result.IsNotFound) return Results.NotFound();

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

    public static async Task<IResult> HandleUpdateTag(
        ITagService tagService,
        [FromBody] UpdateTagRequest request)
    {
        var result = await tagService.Update(request);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }
}
