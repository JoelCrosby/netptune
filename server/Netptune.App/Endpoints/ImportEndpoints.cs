using Mediator;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

using Netptune.App.Configuration;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Transfer.Archive;
using Netptune.Transfer.Services;
using Netptune.Transfer.Mapping;
using Netptune.Handlers.Transfer.Commands;
using Netptune.Handlers.Transfer.Queries;

namespace Netptune.App.Endpoints;

public static class ImportEndpoints
{
    private const long MaxFileSize = 50L * 1024 * 1024;
    private const long MaxRequestSize = MaxFileSize + 1024 * 1024;

    private const long MaxArchiveSize = 2L * 1024 * 1024 * 1024;
    private const long MaxArchiveRequestSize = MaxArchiveSize + 1024 * 1024;

    public static RouteGroupBuilder MapImportEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("import");

        group.MapPost("/sessions", HandleUpload)
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestSize))
            .RequireRateLimiting(RateLimiterConfiguration.TransferPolicyName)
            .RequireAuthorization(NetptunePermissions.Tasks.Import);

        group.MapGet("/sessions", HandleGetSessions)
            .RequireAuthorization(NetptunePermissions.Tasks.Import);

        group.MapGet("/sessions/{publicId:guid}", HandleGetSession)
            .RequireAuthorization(NetptunePermissions.Tasks.Import);

        group.MapGet("/sessions/{publicId:guid}/state", HandleGetSessionState)
            .RequireAuthorization(NetptunePermissions.Tasks.Import);

        group.MapPost("/sessions/{publicId:guid}/inspect", HandleInspect)
            .RequireRateLimiting(RateLimiterConfiguration.TransferPolicyName)
            .RequireAuthorization(NetptunePermissions.Tasks.Import);

        group.MapPost("/sessions/{publicId:guid}/suggest", HandleSuggest)
            .RequireRateLimiting(RateLimiterConfiguration.TransferPolicyName)
            .RequireAuthorization(NetptunePermissions.Tasks.Import);

        group.MapPost("/sessions/{publicId:guid}/suggest/assistant", HandleImprove)
            .RequireRateLimiting(RateLimiterConfiguration.AiPolicyName)
            .RequireAuthorization(NetptunePermissions.Tasks.Import);

        group.MapPut("/sessions/{publicId:guid}/mapping", HandleSetMapping)
            .RequireAuthorization(NetptunePermissions.Tasks.Import);

        group.MapPost("/sessions/{publicId:guid}/preview", HandlePreview)
            .RequireRateLimiting(RateLimiterConfiguration.TransferPolicyName)
            .RequireAuthorization(NetptunePermissions.Tasks.Import);

        group.MapPost("/sessions/{publicId:guid}/commit", HandleCommit)
            .RequireRateLimiting(RateLimiterConfiguration.TransferPolicyName)
            .RequireAuthorization(NetptunePermissions.Tasks.Import);

        group.MapPost("/sessions/{publicId:guid}/undo", HandleUndo)
            .RequireRateLimiting(RateLimiterConfiguration.TransferPolicyName)
            .RequireAuthorization(NetptunePermissions.Tasks.Import);

        group.MapDelete("/sessions/{publicId:guid}", HandleDeleteSession)
            .RequireAuthorization(NetptunePermissions.Tasks.Import);

        group.MapPost("/archive/preview", HandlePreviewArchive)
            .WithMetadata(new RequestSizeLimitAttribute(MaxArchiveRequestSize))
            .RequireAuthorization(NetptunePermissions.Data.ImportArchive);

        group.MapPost("/archive", HandleImportArchive)
            .WithMetadata(new RequestSizeLimitAttribute(MaxArchiveRequestSize))
            .RequireAuthorization(NetptunePermissions.Data.ImportArchive);

        return group;
    }

    private static async Task<IResult> HandlePreviewArchive(
        IMediator mediator,
        IAuthorizationService authorization,
        HttpRequest request,
        string? mode,
        string? targetSlug,
        CancellationToken cancellationToken)
    {
        var cloneRefusal = await RefuseUnauthorizedClone(authorization, request, mode);

        if (cloneRefusal is not null)
        {
            return cloneRefusal;
        }

        return await WithArchive(request, mode, targetSlug, false, async archive =>
        {
            var result = await mediator.Send(new PreviewArchiveImportCommand(archive), cancellationToken);

            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        });
    }

    private static async Task<IResult> HandleImportArchive(
        IMediator mediator,
        IAuthorizationService authorization,
        HttpRequest request,
        string? mode,
        string? targetSlug,
        bool? inviteUnmatchedMembers,
        CancellationToken cancellationToken)
    {
        var cloneRefusal = await RefuseUnauthorizedClone(authorization, request, mode);

        if (cloneRefusal is not null)
        {
            return cloneRefusal;
        }

        return await WithArchive(request, mode, targetSlug, inviteUnmatchedMembers ?? false, async archive =>
        {
            var result = await mediator.Send(new ImportArchiveCommand(archive), cancellationToken);

            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        });
    }

    private static async Task<IResult?> RefuseUnauthorizedClone(
        IAuthorizationService authorization,
        HttpRequest request,
        string? mode)
    {
        var isClone = ParseMode(mode) == ArchiveImportMode.Clone;

        if (!isClone)
        {
            return null;
        }

        var cloneAuthorization = await authorization.AuthorizeAsync(request.HttpContext.User, NetptunePermissions.Workspace.Create);

        return cloneAuthorization.Succeeded ? null : Results.Forbid();
    }

    private static ArchiveImportMode ParseMode(string? mode)
    {
        return Enum.TryParse<ArchiveImportMode>(mode, true, out var value) ? value : ArchiveImportMode.Clone;
    }

    // Spools the upload to a temporary file: reading a zip means seeking, which an upload stream does
    // not support, and an archive is far too large to hold in memory.
    private static async Task<IResult> WithArchive(
        HttpRequest request,
        string? mode,
        string? targetSlug,
        bool inviteUnmatchedMembers,
        Func<ImportArchiveRequest, Task<IResult>> handle)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest("Multipart form data is required.");
        }

        var parsedMode = ParseMode(mode);

        if (parsedMode == ArchiveImportMode.Clone && string.IsNullOrWhiteSpace(targetSlug))
        {
            return Results.BadRequest("A target workspace slug is required when cloning an archive.");
        }

        // The default multipart section limit is 128 MB, so without this an archive between that and
        // the endpoint's own limit would throw out of ReadFormAsync before the size check below ever
        // ran. Scoped to this request so the other upload endpoints keep the framework default.
        request.HttpContext.Features.Set<IFormFeature>(new FormFeature(request, new FormOptions
        {
            MultipartBodyLengthLimit = MaxArchiveSize,
        }));

        var form = await request.ReadFormAsync(request.HttpContext.RequestAborted);

        if (form.Files.Count != 1)
        {
            return Results.BadRequest("Exactly one file is required.");
        }

        var file = form.Files.Single();

        if (file.Length > MaxArchiveSize)
        {
            return Results.BadRequest("The archive is larger than the maximum supported size.");
        }

        await using var spool = new FileStream(
            Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()),
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);

        await using (var upload = file.OpenReadStream())
        {
            await upload.CopyToAsync(spool, request.HttpContext.RequestAborted);
        }

        spool.Seek(0, SeekOrigin.Begin);

        return await handle(new ImportArchiveRequest
        {
            Archive = spool,
            Mode = parsedMode,
            TargetSlug = targetSlug,
            InviteUnmatchedMembers = inviteUnmatchedMembers,
        });
    }

    private static async Task<IResult> HandleUpload(
        IMediator mediator,
        HttpRequest request,
        string? recordType,
        string? projectKey,
        string? boardIdentifier,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest("Multipart form data is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);

        if (form.Files.Count != 1)
        {
            return Results.BadRequest("Exactly one file is required.");
        }

        var file = form.Files.Single();

        await using var stream = file.OpenReadStream();

        var upload = new ImportUpload
        {
            Content = stream,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
        };
        var destination = new ImportDestination
        {
            RecordType = recordType ?? Transfer.EntityRefTypes.Task,
            ProjectKey = projectKey,
            BoardIdentifier = boardIdentifier,
        };
        var result = await mediator.Send(new CreateImportSessionCommand(upload, destination), cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetSessions(
        IMediator mediator,
        [AsParameters] PageRequest page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetImportSessionsQuery(page), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetSession(
        IMediator mediator,
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetImportSessionQuery(publicId), cancellationToken);

        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> HandleDeleteSession(
        IMediator mediator,
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteImportSessionCommand(publicId), cancellationToken);

        return Respond(result.IsNotFound, result.IsSuccess, result);
    }

    private static async Task<IResult> HandleGetSessionState(
        IMediator mediator,
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetImportSessionStateQuery(publicId), cancellationToken);

        return Respond(result.IsNotFound, result.IsSuccess, result);
    }

    private static async Task<IResult> HandleInspect(
        IMediator mediator,
        Guid publicId,
        InspectImportSessionRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new InspectImportSessionCommand(publicId, request), cancellationToken);

        return Respond(result.IsNotFound, result.IsSuccess, result);
    }

    private static async Task<IResult> HandleSuggest(
        IMediator mediator,
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SuggestImportMappingQuery(publicId), cancellationToken);

        return Respond(result.IsNotFound, result.IsSuccess, result);
    }

    private static async Task<IResult> HandleImprove(
        IMediator mediator,
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ImproveImportMappingCommand(publicId), cancellationToken);

        return Respond(result.IsNotFound, result.IsSuccess, result);
    }

    private static async Task<IResult> HandleSetMapping(
        IMediator mediator,
        Guid publicId,
        ImportMappingModel mapping,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SetImportMappingCommand(publicId, mapping), cancellationToken);

        return Respond(result.IsNotFound, result.IsSuccess, result);
    }

    private static async Task<IResult> HandlePreview(
        IMediator mediator,
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PreviewImportSessionCommand(publicId), cancellationToken);

        return Respond(result.IsNotFound, result.IsSuccess, result);
    }

    private static async Task<IResult> HandleCommit(
        IMediator mediator,
        Guid publicId,
        bool? skipFailingRows,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CommitImportSessionCommand(publicId, skipFailingRows ?? false), cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound(result);
        }

        if (!result.IsSuccess)
        {
            return Results.BadRequest(result);
        }

        return Results.Accepted($"/api/import/sessions/{publicId}", result);
    }

    private static async Task<IResult> HandleUndo(
        IMediator mediator,
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UndoImportSessionCommand(publicId), cancellationToken);

        return Respond(result.IsNotFound, result.IsSuccess, result);
    }

    private static IResult Respond<TResponse>(bool isNotFound, bool isSuccess, TResponse result)
    {
        if (isNotFound)
        {
            return Results.NotFound(result);
        }

        if (!isSuccess)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }
}
