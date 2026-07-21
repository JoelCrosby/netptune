using Mediator;
using Netptune.Core.Authorization;
using Netptune.Core.Models.Audit;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Handlers.Audit.Queries;

namespace Netptune.App.Endpoints;

public static class AuditEndpoints
{
    public static RouteGroupBuilder MapAuditEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("audit");

        group.MapGet("/", HandleGetAuditLog)
            .RequireAuthorization(NetptunePermissions.Audit.Read);

        group.MapGet("/summary", HandleGetActivitySummary)
            .RequireAuthorization(NetptunePermissions.Audit.Read);

        group.MapGet("/{id:long}", HandleGetAuditLogDetail)
            .RequireAuthorization(NetptunePermissions.Audit.Read);

        group.MapGet("/export", HandleExport)
            .RequireAuthorization(NetptunePermissions.Audit.Export);

        return group;
    }

    private static async Task<IResult> HandleGetAuditLog(
        IMediator mediator,
        [AsParameters] AuditLogFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAuditLogQuery(filter), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetActivitySummary(
        IMediator mediator,
        [AsParameters] AuditLogFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetActivitySummaryQuery(filter), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetAuditLogDetail(
        IMediator mediator,
        long id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAuditLogDetailQuery(id), cancellationToken);

        return result.IsNotFound
            ? Results.NotFound(result)
            : Results.Ok(result);
    }

    private static async Task<IResult> HandleExport(
        IMediator mediator,
        IActivityLogger activity,
        IIdentityService identity,
        [AsParameters] AuditLogFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ExportAuditLogQuery(filter), cancellationToken);

        await ExportEndpoints.LogExportRequested(activity, identity, "audit-log");

        return Results.File(result.Stream, result.ContentType, result.Filename);
    }
}
