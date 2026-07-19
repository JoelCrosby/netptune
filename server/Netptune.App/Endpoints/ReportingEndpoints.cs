using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Responses.Common;
using Netptune.Handlers.Reporting.Queries;

namespace Netptune.App.Endpoints;

public static class ReportingEndpoints
{
    private sealed record FlowReportRequest
    {
        public int? ProjectId { get; init; }

        public DateTime? From { get; init; }

        public DateTime? To { get; init; }

        public ReportingUnit? Unit { get; init; }

        public string? TimeZone { get; init; }

        public ReportingGrouping? Grouping { get; init; }

        public ReportingFilter ToFilter() => new()
        {
            ProjectId = ProjectId,
            From = From,
            To = To,
            Unit = Unit ?? ReportingUnit.Tasks,
            TimeZone = TimeZone ?? "UTC",
            Grouping = Grouping ?? ReportingGrouping.Day,
        };
    }

    private sealed record WorkloadReportRequest(int? ProjectId, ReportingUnit? Unit)
    {
        public ReportingFilter ToFilter() => new()
        {
            ProjectId = ProjectId,
            Unit = Unit ?? ReportingUnit.Tasks,
        };
    }

    private sealed record SprintBurndownReportRequest(int SprintId, ReportingUnit? Unit, string? TimeZone)
    {
        public SprintBurndownFilter ToFilter() => new()
        {
            SprintId = SprintId,
            Unit = Unit ?? ReportingUnit.Tasks,
            TimeZone = TimeZone ?? "UTC",
        };
    }

    private sealed record VelocityReportRequest(int ProjectId, ReportingUnit? Unit, int? Take)
    {
        public VelocityFilter ToFilter() => new()
        {
            ProjectId = ProjectId,
            Unit = Unit ?? ReportingUnit.Tasks,
            Take = Take ?? 12,
        };
    }

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
        [AsParameters] FlowReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var filter = request.ToFilter();
        var result = await mediator.Send(new GetFlowReportQuery(filter), cancellationToken);

        return ToResult(result);
    }

    private static async Task<IResult> GetWorkload(
        IMediator mediator,
        [AsParameters] WorkloadReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var filter = request.ToFilter();
        var result = await mediator.Send(new GetWorkloadReportQuery(filter), cancellationToken);

        return ToResult(result);
    }

    private static async Task<IResult> GetBurndown(
        IMediator mediator,
        [AsParameters] SprintBurndownReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var filter = request.ToFilter();
        var query = new GetSprintBurndownReportQuery(filter);
        var result = await mediator.Send(query, cancellationToken);

        return ToResult(result);
    }

    private static async Task<IResult> GetVelocity(
        IMediator mediator,
        [AsParameters] VelocityReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var filter = request.ToFilter();
        var query = new GetVelocityReportQuery(filter);
        var result = await mediator.Send(query, cancellationToken);

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
