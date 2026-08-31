using Mediator;

using Microsoft.AspNetCore.Mvc;

using Netptune.App.Configuration;
using Netptune.App.Utility;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Realtime;
using Netptune.Handlers.Tags.Commands;
using Netptune.Handlers.Tags.Queries;

namespace Netptune.App.Endpoints;

public static class TagsEndpoints
{
    public static RouteGroupBuilder MapTagsEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("tags");

        group.MapPost("/", HandlePost).RequireAuthorization(NetptunePermissions.Tags.Create);
        group.MapPost("/task", HandlePostTaskTag).RequireAuthorization(NetptunePermissions.Tags.Assign)
            .Broadcasts(WorkspaceEventScopes.Tag, WorkspaceEventScopes.Task);
        group.MapGet("/task/{systemId}", HandleGetTagsForTask).RequireAuthorization(NetptunePermissions.Tags.Read);
        group.MapGet("/workspace", HandleGetTagsForWorkspace).RequireAuthorization(NetptunePermissions.Tags.Read);
        group.MapGet("/page", HandleGetTagsPage).RequireAuthorization(NetptunePermissions.Tags.Read);
        group.MapGet("/{id:int}/usage", HandleGetTagUsage).RequireAuthorization(NetptunePermissions.Tags.Read);
        group.MapDelete("/", HandleDelete).RequireAuthorization(NetptunePermissions.Tags.Delete);
        group.MapDelete("/task", HandleDeleteFromTask).RequireAuthorization(NetptunePermissions.Tags.Assign)
            .Broadcasts(WorkspaceEventScopes.Tag, WorkspaceEventScopes.Task);
        group.MapPatch("/", HandleUpdateTag).RequireAuthorization(NetptunePermissions.Tags.Update);

        return builder;
    }

    public static async Task<IResult> HandlePost(
        IMediator mediator,
        [FromBody] AddTagRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateTagCommand(request), cancellationToken);

        return result.ToResult();
    }

    public static async Task<IResult> HandlePostTaskTag(
        IMediator mediator,
        [FromBody] AddTagToTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddTagToTaskCommand(request), cancellationToken);

        return result.ToResult();
    }

    public static async Task<IResult> HandleGetTagsForTask(
        IMediator mediator,
        string systemId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTagsForTaskQuery(systemId), cancellationToken);

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetTagsForWorkspace(
        IMediator mediator,
        [AsParameters] PageRequest page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTagsForWorkspaceQuery(page), cancellationToken);

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetTagsPage(
        IMediator mediator,
        [AsParameters] TagFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTagsPageQuery(filter), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetTagUsage(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTagUsageQuery(id), cancellationToken);

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(
        IMediator mediator,
        [FromBody] DeleteTagsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTagsCommand(request), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDeleteFromTask(
        IMediator mediator,
        [FromBody] DeleteTagFromTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTagFromTaskCommand(request), cancellationToken);

        return result.ToResult();
    }

    public static async Task<IResult> HandleUpdateTag(
        IMediator mediator,
        [FromBody] UpdateTagRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateTagCommand(request), cancellationToken);

        return result.ToResult();
    }

}
