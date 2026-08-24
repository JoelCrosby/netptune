using Mediator;

using Microsoft.AspNetCore.Mvc;

using Netptune.App.Services;
using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Realtime;
using Netptune.Core.Storage;
using Netptune.Handlers.Storage.Commands;

namespace Netptune.App.Endpoints;

public static class BrandingEndpoints
{
    private const long BrandingRequestBytes = UploadLimits.BrandingImageMaxBytes + UploadLimits.RequestOverheadBytes;

    public static RouteGroupBuilder MapBrandingEndpoints(this RouteGroupBuilder builder)
    {
        var workspaces = builder.MapGroup("workspaces/branding");

        workspaces.MapPost("/logo", UploadWorkspaceLogo)
            .WithMetadata(new RequestSizeLimitAttribute(BrandingRequestBytes))
            .RequireAuthorization(NetptunePermissions.Workspace.Update, NetptunePermissions.Files.Upload);

        workspaces.MapDelete("/logo", RemoveWorkspaceLogo)
            .RequireAuthorization(NetptunePermissions.Workspace.Update);

        var projects = builder.MapGroup("projects/{id:int}/branding");

        projects.MapPost("/logo", UploadProjectLogo)
            .WithMetadata(new RequestSizeLimitAttribute(BrandingRequestBytes))
            .RequireAuthorization(NetptunePermissions.Projects.Update, NetptunePermissions.Files.Upload);

        projects.MapDelete("/logo", RemoveProjectLogo)
            .RequireAuthorization(NetptunePermissions.Projects.Update);

        var boards = builder.MapGroup("boards/{id:int}/branding");

        boards.MapPost("/logo", UploadBoardLogo)
            .WithMetadata(new RequestSizeLimitAttribute(BrandingRequestBytes))
            .RequireAuthorization(NetptunePermissions.Boards.Update, NetptunePermissions.Files.Upload);

        boards.MapDelete("/logo", RemoveBoardLogo)
            .RequireAuthorization(NetptunePermissions.Boards.Update);

        boards.MapPost("/background", UploadBoardBackground)
            .WithMetadata(new RequestSizeLimitAttribute(BrandingRequestBytes))
            .RequireAuthorization(NetptunePermissions.Boards.Update, NetptunePermissions.Files.Upload);

        boards.MapDelete("/background", RemoveBoardBackground)
            .RequireAuthorization(NetptunePermissions.Boards.Update);

        return builder;
    }

    private static async Task<IResult> UploadWorkspaceLogo(IMediator mediator, HttpRequest request, CancellationToken cancellationToken)
    {
        var outcome = await Upload(mediator, request, BrandingImageTarget.WorkspaceLogo, null, cancellationToken);

        return outcome.Response;
    }

    private static async Task<IResult> RemoveWorkspaceLogo(IMediator mediator, CancellationToken cancellationToken)
    {
        var outcome = await Remove(mediator, BrandingImageTarget.WorkspaceLogo, null, cancellationToken);

        return outcome.Response;
    }

    private static async Task<IResult> UploadProjectLogo(int id, IMediator mediator, HttpRequest request, CancellationToken cancellationToken)
    {
        var outcome = await Upload(mediator, request, BrandingImageTarget.ProjectLogo, id, cancellationToken);

        return outcome.Response;
    }

    private static async Task<IResult> RemoveProjectLogo(int id, IMediator mediator, CancellationToken cancellationToken)
    {
        var outcome = await Remove(mediator, BrandingImageTarget.ProjectLogo, id, cancellationToken);

        return outcome.Response;
    }

    private static async Task<IResult> UploadBoardLogo(
        int id,
        IMediator mediator,
        HttpRequest request,
        IBoardEventService boardEvents,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var outcome = await Upload(mediator, request, BrandingImageTarget.BoardLogo, id, cancellationToken);

        return await BroadcastBoardChange(outcome, boardEvents, http);
    }

    private static async Task<IResult> RemoveBoardLogo(
        int id,
        IMediator mediator,
        IBoardEventService boardEvents,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var outcome = await Remove(mediator, BrandingImageTarget.BoardLogo, id, cancellationToken);

        return await BroadcastBoardChange(outcome, boardEvents, http);
    }

    private static async Task<IResult> UploadBoardBackground(
        int id,
        IMediator mediator,
        HttpRequest request,
        IBoardEventService boardEvents,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var outcome = await Upload(mediator, request, BrandingImageTarget.BoardBackground, id, cancellationToken);

        return await BroadcastBoardChange(outcome, boardEvents, http);
    }

    private static async Task<IResult> RemoveBoardBackground(
        int id,
        IMediator mediator,
        IBoardEventService boardEvents,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var outcome = await Remove(mediator, BrandingImageTarget.BoardBackground, id, cancellationToken);

        return await BroadcastBoardChange(outcome, boardEvents, http);
    }

    private static async Task<BrandingOutcome> Upload(
        IMediator mediator,
        HttpRequest request,
        BrandingImageTarget target,
        int? targetId,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return new (Results.BadRequest("Multipart form data is required."), false);
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.FirstOrDefault();

        if (file is null)
        {
            return new (Results.BadRequest("A file is required."), false);
        }

        await using var stream = file.OpenReadStream();

        var upload = new BrandingImageUpload
        {
            Target = target,
            TargetId = targetId,
            Content = stream,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
        };

        var result = await mediator.Send(new UploadBrandingImageCommand(upload), cancellationToken);

        if (result.IsNotFound)
        {
            return new (Results.NotFound(result), false);
        }

        if (!result.IsSuccess)
        {
            return new (Results.BadRequest(result), false);
        }

        return new (Results.Ok(result), true);
    }

    private static async Task<BrandingOutcome> Remove(
        IMediator mediator,
        BrandingImageTarget target,
        int? targetId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RemoveBrandingImageCommand(target, targetId), cancellationToken);

        if (result.IsNotFound)
        {
            return new (Results.NotFound(), false);
        }

        return new (Results.NoContent(), true);
    }

    private static async Task<IResult> BroadcastBoardChange(BrandingOutcome outcome, IBoardEventService boardEvents, HttpContext http)
    {
        if (outcome.Changed)
        {
            await boardEvents.BroadcastRequestAsync(http, WorkspaceEventScopes.Board);
        }

        return outcome.Response;
    }

    private sealed record BrandingOutcome(IResult Response, bool Changed);
}
