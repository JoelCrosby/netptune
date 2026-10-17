using Mediator;

using Netptune.App.Configuration;
using Netptune.App.Utility;
using Netptune.Core.Authorization;
using Netptune.Core.Requests.SourceControl;
using Netptune.Core.Services.Realtime;
using Netptune.Handlers.SourceControl.Commands;
using Netptune.Handlers.SourceControl.Queries;

namespace Netptune.App.Endpoints;

public static class SourceControlEndpoints
{
    public static RouteGroupBuilder MapSourceControlEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("source-control")
            .WithTags("Source control")
            .RequireAuthorization();

        group.MapGet("/providers", HandleGetProviders)
            .RequireAuthorization(NetptunePermissions.SourceControl.Read);

        group.MapGet("/connections", HandleGetConnections)
            .RequireAuthorization(NetptunePermissions.SourceControl.Read);

        group.MapPost("/connections/begin", HandleBeginConnect)
            .RequireAuthorization(NetptunePermissions.SourceControl.Manage);

        group.MapPost("/connections/complete", HandleCompleteConnect)
            .RequireAuthorization(NetptunePermissions.SourceControl.Manage)
            .Broadcasts(WorkspaceEventScopes.SourceControl);

        group.MapDelete("/connections/{connectionId:int}", HandleDisconnect)
            .RequireAuthorization(NetptunePermissions.SourceControl.Manage)
            .Broadcasts(WorkspaceEventScopes.SourceControl, WorkspaceEventScopes.CodeLinks);

        group.MapGet("/connections/{connectionId:int}/repos", HandleGetRepos)
            .RequireAuthorization(NetptunePermissions.SourceControl.Read);

        group.MapPost("/connections/{connectionId:int}/sync", HandleSyncRepos)
            .RequireAuthorization(NetptunePermissions.SourceControl.Manage)
            .Broadcasts(WorkspaceEventScopes.SourceControl, WorkspaceEventScopes.CodeLinks);

        group.MapPut("/repos/{repoId:int}/linked", HandleSetRepoLinked)
            .RequireAuthorization(NetptunePermissions.SourceControl.Manage)
            .Broadcasts(WorkspaceEventScopes.SourceControl, WorkspaceEventScopes.CodeLinks);

        group.MapGet("/identities", HandleGetIdentities);

        group.MapPost("/identities", HandleAddIdentity)
            .Broadcasts(WorkspaceEventScopes.CodeLinks);

        group.MapDelete("/identities/{identityId:int}", HandleRemoveIdentity)
            .Broadcasts(WorkspaceEventScopes.CodeLinks);

        group.MapGet("/tasks/{systemId}/links", HandleGetTaskLinks)
            .RequireAuthorization(NetptunePermissions.SourceControl.Read, NetptunePermissions.Tasks.Read);

        return builder;
    }

    private static async Task<IResult> HandleGetProviders(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSourceControlProvidersQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetConnections(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSourceControlConnectionsQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleBeginConnect(
        BeginSourceControlConnectRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new BeginSourceControlConnectCommand(request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleCompleteConnect(
        CompleteSourceControlConnectRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CompleteSourceControlConnectCommand(request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleDisconnect(
        int connectionId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DisconnectSourceControlConnectionCommand(connectionId), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetRepos(
        int connectionId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSourceControlReposQuery(connectionId), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleSyncRepos(
        int connectionId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SyncSourceControlReposCommand(connectionId), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleSetRepoLinked(
        int repoId,
        SetSourceControlRepoLinkedRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SetSourceControlRepoLinkedCommand(repoId, request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetIdentities(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSourceControlIdentitiesQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleAddIdentity(
        AddSourceControlIdentityRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddSourceControlIdentityCommand(request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleRemoveIdentity(
        int identityId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RemoveSourceControlIdentityCommand(identityId), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetTaskLinks(
        string systemId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskCodeLinksQuery(systemId), cancellationToken);

        return result.ToResult();
    }
}
