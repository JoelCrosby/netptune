using Mediator;
using Microsoft.AspNetCore.Mvc;
using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Models.Audit;
using Netptune.Services.Audit.Commands;
using Netptune.Services.Audit.Queries;

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

        group.MapGet("/export", HandleExport)
            .RequireAuthorization(NetptunePermissions.Audit.Export);

        group.MapDelete("/user/{userId}", HandleAnonymiseUser)
            .RequireAuthorization(NetptunePermissions.Audit.Anonymise);

        return group;
    }

    private static async Task<IResult> HandleGetAuditLog(
        IMediator mediator,
        [FromQuery] string? userId,
        [FromQuery] EntityType? entityType,
        [FromQuery] ActivityType? activityType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var filter = new AuditLogFilter
        {
            UserId = userId,
            EntityType = entityType,
            ActivityType = activityType,
            From = from,
            To = to,
            Page = page,
            PageSize = Math.Clamp(pageSize, 1, 200),
        };

        var result = await mediator.Send(new GetAuditLogQuery(filter), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetActivitySummary(
        IMediator mediator,
        [FromQuery] string? userId,
        [FromQuery] EntityType? entityType,
        [FromQuery] ActivityType? activityType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var filter = new AuditLogFilter
        {
            UserId = userId,
            EntityType = entityType,
            ActivityType = activityType,
            From = from,
            To = to,
        };

        var result = await mediator.Send(new GetActivitySummaryQuery(filter), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleExport(
        IMediator mediator,
        [FromQuery] string? userId,
        [FromQuery] EntityType? entityType,
        [FromQuery] ActivityType? activityType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var filter = new AuditLogFilter
        {
            UserId = userId,
            EntityType = entityType,
            ActivityType = activityType,
            From = from,
            To = to,
        };

        var result = await mediator.Send(new ExportAuditLogQuery(filter), cancellationToken);

        return Results.File(result.Stream, result.ContentType, result.Filename);
    }

    private static async Task<IResult> HandleAnonymiseUser(
        IMediator mediator,
        [FromRoute] string userId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AnonymiseUserCommand(userId), cancellationToken);

        return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Message);
    }
}
