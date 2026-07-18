using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Responses.Common;
using Netptune.Handlers.Reporting.Queries;

namespace Netptune.App.Endpoints;

public static class ReportingEndpoints
{
    public static RouteGroupBuilder MapReportingEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("reports");

        group.MapGet("/flow", GetFlow)
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        group.MapGet("/workload", GetWorkload)
            .RequireAuthorization(NetptunePermissions.Tasks.Read)
            .RequireAuthorization(NetptunePermissions.Members.Read);

        group.MapGet("/sprints/{sprintId:int}/burndown", GetBurndown)
            .RequireAuthorization(NetptunePermissions.Tasks.Read)
            .RequireAuthorization(NetptunePermissions.Sprints.Read);

        group.MapGet("/velocity", GetVelocity)
            .RequireAuthorization(NetptunePermissions.Tasks.Read)
            .RequireAuthorization(NetptunePermissions.Sprints.Read);

        return group;
    }

    private static async Task<IResult> GetFlow(
        IMediator mediator,
        int? projectId = null,
        DateTime? from = null,
        DateTime? to = null,
        ReportingUnit unit = ReportingUnit.Tasks,
        string timeZone = "UTC",
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetFlowReportQuery(
            new ReportingFilter { ProjectId = projectId, From = from, To = to, Unit = unit, TimeZone = timeZone }), cancellationToken);

        return ToResult(result);
    }

    private static async Task<IResult> GetWorkload(
        IMediator mediator,
        int? projectId = null,
        ReportingUnit unit = ReportingUnit.Tasks,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetWorkloadReportQuery(
            new ReportingFilter { ProjectId = projectId, Unit = unit }), cancellationToken);

        return ToResult(result);
    }

    private static async Task<IResult> GetBurndown(
        IMediator mediator,
        int sprintId,
        ReportingUnit unit = ReportingUnit.Tasks,
        string timeZone = "UTC",
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetSprintBurndownReportQuery(sprintId, unit, timeZone), cancellationToken);

        return ToResult(result);
    }

    private static async Task<IResult> GetVelocity(
        IMediator mediator,
        int projectId,
        ReportingUnit unit = ReportingUnit.Tasks,
        int take = 12,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetVelocityReportQuery(projectId, unit, take), cancellationToken);

        return ToResult(result);
    }

    private static IResult ToResult<T>(ClientResponse<T> result)
    {

        if (result.IsNotFound)
        {
            return Results.NotFound();
        }

        if (!result.IsSuccess)
        {
            return Results.BadRequest(new { message = result.Message });
        }

        return Results.Ok(result.Payload);
    }
}
