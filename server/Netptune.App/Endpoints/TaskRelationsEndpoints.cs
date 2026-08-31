using Mediator;

using Microsoft.AspNetCore.Mvc;

using Netptune.App.Configuration;
using Netptune.App.Utility;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Realtime;
using Netptune.Handlers.Relations.Commands;
using Netptune.Handlers.Relations.Queries;

namespace Netptune.App.Endpoints;

public static class TaskRelationsEndpoints
{
    public static RouteGroupBuilder MapTaskRelationsEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("task-relations");

        group.MapGet("/{systemId}", HandleGet).RequireAuthorization(NetptunePermissions.Tasks.Read);
        group.MapPost("/", HandlePost).RequireAuthorization(NetptunePermissions.Tasks.Update)
            .Broadcasts(WorkspaceEventScopes.Task);
        group.MapDelete("/{id:int}", HandleDelete).RequireAuthorization(NetptunePermissions.Tasks.Update)
            .Broadcasts(WorkspaceEventScopes.Task);

        return builder;
    }

    private static async Task<IResult> HandleGet(
        IMediator mediator,
        string systemId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskRelationsQuery(systemId), cancellationToken);

        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> HandlePost(
        IMediator mediator,
        [FromBody] CreateTaskRelationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateTaskRelationCommand(request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleDelete(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTaskRelationCommand(id), cancellationToken);

        return result.ToResult();
    }

}
