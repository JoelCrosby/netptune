using Mediator;

using Microsoft.AspNetCore.Mvc;

using Netptune.App.Services;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Services.Tags.Commands.AddTagToTask;
using Netptune.Services.Tags.Commands.CreateTag;
using Netptune.Services.Tags.Commands.DeleteTagFromTask;
using Netptune.Services.Tags.Commands.DeleteTags;
using Netptune.Services.Tags.Commands.UpdateTag;
using Netptune.Services.Tags.Queries.GetTagsForTask;
using Netptune.Services.Tags.Queries.GetTagsForWorkspace;

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

        return builder;
    }

    public static async Task<IResult> HandlePost(
        IMediator mediator,
        [FromBody] AddTagRequest request)
    {
        var result = await mediator.Send(new CreateTagCommand(request));

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePostTaskTag(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        [FromBody] AddTagToTaskRequest request)
    {
        var result = await mediator.Send(new AddTagToTaskCommand(request));

        if (result.IsNotFound) return Results.NotFound();

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetTagsForTask(
        IMediator mediator,
        string systemId)
    {
        var result = await mediator.Send(new GetTagsForTaskQuery(systemId));

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetTagsForWorkspace(
        IMediator mediator)
    {
        var result = await mediator.Send(new GetTagsForWorkspaceQuery());

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(
        IMediator mediator,
        [FromBody] DeleteTagsRequest request)
    {
        var result = await mediator.Send(new DeleteTagsCommand(request));

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDeleteFromTask(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        [FromBody] DeleteTagFromTaskRequest request)
    {
        var result = await mediator.Send(new DeleteTagFromTaskCommand(request));

        if (result.IsNotFound) return Results.NotFound();

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleUpdateTag(
        IMediator mediator,
        [FromBody] UpdateTagRequest request)
    {
        var result = await mediator.Send(new UpdateTagCommand(request));

        if (result.IsNotFound) return Results.NotFound();

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
