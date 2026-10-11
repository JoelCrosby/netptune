using Mediator;

using Netptune.App.Configuration;
using Netptune.App.Utility;
using Netptune.Core.Authorization;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Services.Realtime;
using Netptune.Handlers.Pages.Commands;
using Netptune.Handlers.Pages.Queries;

namespace Netptune.App.Endpoints;

public static class PagesEndpoints
{
    public static RouteGroupBuilder MapPagesEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("pages");

        group.MapGet("/tree", HandleGetTree).RequireAuthorization(NetptunePermissions.Pages.Read);
        group.MapGet("/archived", HandleGetArchived).RequireAuthorization(NetptunePermissions.Pages.Read);
        group.MapGet("/linked-to-task/{systemId}", HandleGetTaskPages)
            .RequireAuthorization(NetptunePermissions.Pages.Read, NetptunePermissions.Tasks.Read);

        group.MapGet("/{slug}", HandleGet).RequireAuthorization(NetptunePermissions.Pages.Read);
        group.MapGet("/{id:int}/links", HandleGetLinks).RequireAuthorization(NetptunePermissions.Pages.Read);

        group.MapPost("/", HandleCreate)
            .RequireAuthorization(NetptunePermissions.Pages.Create)
            .Broadcasts(WorkspaceEventScopes.Page);

        group.MapPut("/{id:int}", HandleSave)
            .RequireAuthorization(NetptunePermissions.Pages.Update)
            .Broadcasts(WorkspaceEventScopes.Page);

        group.MapPatch("/{id:int}", HandleUpdate)
            .RequireAuthorization(NetptunePermissions.Pages.Update)
            .Broadcasts(WorkspaceEventScopes.Page);

        group.MapGet("/{id:int}/versions", HandleGetVersions).RequireAuthorization(NetptunePermissions.Pages.Read);
        group.MapGet("/{id:int}/versions/{versionId:int}", HandleGetVersion).RequireAuthorization(NetptunePermissions.Pages.Read);

        group.MapPost("/{id:int}/versions/{versionId:int}/restore", HandleRestoreVersion)
            .RequireAuthorization(NetptunePermissions.Pages.Update)
            .Broadcasts(WorkspaceEventScopes.Page);

        group.MapPatch("/{id:int}/move", HandleMove)
            .RequireAuthorization(NetptunePermissions.Pages.Update)
            .Broadcasts(WorkspaceEventScopes.Page);

        group.MapDelete("/{id:int}", HandleArchive)
            .RequireAuthorization(NetptunePermissions.Pages.Delete)
            .Broadcasts(WorkspaceEventScopes.Page);

        group.MapPost("/{id:int}/restore", HandleRestore)
            .RequireAuthorization(NetptunePermissions.Pages.Delete)
            .Broadcasts(WorkspaceEventScopes.Page);

        group.MapPut("/{id:int}/favourite", HandleFavourite).RequireAuthorization(NetptunePermissions.Pages.Read);
        group.MapDelete("/{id:int}/favourite", HandleUnfavourite).RequireAuthorization(NetptunePermissions.Pages.Read);
        group.MapPost("/{id:int}/visits", HandleVisit).RequireAuthorization(NetptunePermissions.Pages.Read);

        return group;
    }

    private static async Task<IResult> HandleGetTree(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPageTreeQuery(), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetArchived(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetArchivedPagesQuery(), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGet(IMediator mediator, string slug, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPageQuery(slug), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetLinks(IMediator mediator, int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPageLinksQuery(id), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetTaskPages(IMediator mediator, string systemId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskPagesQuery(systemId), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleCreate(IMediator mediator, CreatePageRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreatePageCommand(request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleSave(IMediator mediator, int id, SavePageRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SavePageCommand(id, request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleUpdate(IMediator mediator, int id, UpdatePageRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdatePageCommand(id, request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetVersions(IMediator mediator, int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPageVersionsQuery(id), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetVersion(IMediator mediator, int id, int versionId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPageVersionQuery(id, versionId), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleRestoreVersion(IMediator mediator, int id, int versionId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RestorePageVersionCommand(id, versionId), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleMove(IMediator mediator, int id, MovePageRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new MovePageCommand(id, request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleArchive(IMediator mediator, int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ArchivePageCommand(id), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleRestore(IMediator mediator, int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RestorePageCommand(id), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleFavourite(IMediator mediator, int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SetPageFavouriteCommand(id, true), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleUnfavourite(IMediator mediator, int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SetPageFavouriteCommand(id, false), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleVisit(IMediator mediator, int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RecordPageVisitCommand(id), cancellationToken);

        return result.ToResult();
    }
}
