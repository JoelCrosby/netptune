using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class TagsEndpoints
{
    public static RouteGroupBuilder Map(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("tags")
            .RequireAuthorization(NetptunePolicies.Workspace);

        group.MapPost("/", HandlePost);
        group.MapPost("/task", HandlePostTaskTag);
        group.MapGet("/task/{systemId}", HandleGetTagsForTask);
        group.MapGet("/workspace", HandleGetTagsForWorkspace);
        group.MapDelete("/", HandleDelete);
        group.MapDelete("/task", HandleDeleteFromTask);
        group.MapPatch("/", HandleUpdateTag);

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
        [FromBody] AddTagToTaskRequest request)
    {
        var result = await tagService.AddToTask(request);

        if (result.IsNotFound) return Results.NotFound();

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
        [FromBody] DeleteTagFromTaskRequest request)
    {
        var result = await tagService.DeleteFromTask(request);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
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
