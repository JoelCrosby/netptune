using Netptune.Transfer.Enums;
using Mediator;

using Microsoft.AspNetCore.Authorization;

using Netptune.App.Services;
using Netptune.Core.Authorization;
using Netptune.Transfer.Definitions;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Handlers.Transfer.Commands;
using Netptune.Handlers.Transfer.Queries;

namespace Netptune.App.Endpoints;

public static class ExportEndpoints
{
    public static RouteGroupBuilder MapExportEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("export");

        group.MapPost("/preview", HandlePreview)
            .RequireAuthorization(NetptunePermissions.Tasks.Export);

        group.MapGet("/preview/rows", HandlePreviewRows)
            .RequireAuthorization(NetptunePermissions.Tasks.Export);

        group.MapPost("/run", HandleRunInline)
            .RequireAuthorization(NetptunePermissions.Tasks.Export);

        group.MapGet("/definitions", HandleGetDefinitions)
            .RequireAuthorization(NetptunePermissions.Tasks.Export);

        group.MapPost("/definitions", HandleSaveDefinition)
            .RequireAuthorization(NetptunePermissions.Tasks.Export);

        group.MapPut("/definitions", HandleSaveDefinition)
            .RequireAuthorization(NetptunePermissions.Tasks.Export);

        group.MapDelete("/definitions/{id:int}", HandleDeleteDefinition)
            .RequireAuthorization(NetptunePermissions.Tasks.Export);

        group.MapPost("/jobs", HandleCreateJob)
            .RequireAuthorization(NetptunePermissions.Tasks.Export);

        group.MapGet("/jobs", HandleGetJobs)
            .RequireAuthorization(NetptunePermissions.Tasks.Export);

        group.MapGet("/jobs/{publicId:guid}", HandleGetJob)
            .RequireAuthorization(NetptunePermissions.Tasks.Export);

        group.MapPost("/jobs/{publicId:guid}/cancel", HandleCancelJob)
            .RequireAuthorization(NetptunePermissions.Tasks.Export);

        group.MapGet("/jobs/{publicId:guid}/download", HandleDownloadJob)
            .RequireAuthorization(NetptunePermissions.Tasks.Export);

        builder.MapGet("/hubs/export-jobs", HandleSse)
            .RequireAuthorization(NetptunePermissions.Tasks.Export);

        return group;
    }

    private static async Task HandleSse(
        HttpContext context,
        IIdentityService identity,
        IExportJobEventService exportJobEvents)
    {
        var workspaceKey = identity.GetWorkspaceKey();

        await exportJobEvents.SubscribeAsync(workspaceKey, context.Response, context.RequestAborted);
    }

    private static async Task<IResult> HandleGetDefinitions(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetExportDefinitionsQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleSaveDefinition(
        IMediator mediator,
        SaveExportDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SaveExportDefinitionCommand(request), cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound(result);
        }

        if (!result.IsSuccess)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDeleteDefinition(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteExportDefinitionCommand(id), cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound(result);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleCreateJob(
        IMediator mediator,
        IAuthorizationService authorization,
        HttpContext http,
        CreateExportJobRequest request,
        CancellationToken cancellationToken)
    {
        var isArchive = request.Definition.Format == ExportFormat.Archive;

        if (isArchive)
        {
            var archiveAuthorization = await authorization.AuthorizeAsync(http.User, NetptunePermissions.Data.ExportArchive);

            if (!archiveAuthorization.Succeeded)
            {
                return Results.Forbid();
            }
        }

        var result = await mediator.Send(new CreateExportJobCommand(request), cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(result);
        }

        return Results.Accepted($"/api/export/jobs/{result.Payload!.PublicId}", result);
    }

    private static async Task<IResult> HandleGetJobs(
        IMediator mediator,
        [AsParameters] PageRequest page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetExportJobsQuery(page), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetJob(
        IMediator mediator,
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetExportJobQuery(publicId), cancellationToken);

        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> HandleCancelJob(
        IMediator mediator,
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelExportJobCommand(publicId), cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound(result);
        }

        if (!result.IsSuccess)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }

    // Hands back the signed storage URL rather than redirecting to it. A redirect can only be followed
    // by a top level navigation, and a navigation cannot carry the workspace header the permission
    // check needs, so every download came back 403.
    private static async Task<IResult> HandleDownloadJob(
        IMediator mediator,
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetExportJobDownloadQuery(publicId), cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound();
        }

        if (!result.IsSuccess)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> HandlePreviewRows(
        IMediator mediator,
        [AsParameters] ExportPreviewRowsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetExportPreviewRowsQuery(request), cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> HandlePreview(
        IMediator mediator,
        ExportPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetExportPreviewQuery(request), cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleRunInline(
        IMediator mediator,
        RunExportInlineRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RunExportInlineCommand(request), cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(result);
        }

        var export = result.Payload!;

        return Results.File(export.Content, export.ContentType, export.FileName);
    }
}
