using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Responses.Common;
using Netptune.Handlers.Reporting.Queries;
using Netptune.PublicApi.Requests;

namespace Netptune.PublicApi.Endpoints;

public static class ReportingEndpoints
{
    public static RouteGroupBuilder MapReportingEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/reports/flow", GetFlow)
            .WithSummary("Get the flow report")
            .WithDescription(
                "Returns throughput, cycle time and work in progress over a date range, grouped by day, week or "
                + "month.")
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        group.MapGet("/reports/workload", GetWorkload)
            .WithSummary("Get the workload report")
            .WithDescription("Returns how much open work each workspace member currently carries.")
            .RequireAuthorization(NetptunePermissions.Tasks.Read)
            .RequireAuthorization(NetptunePermissions.Members.Read);

        group.MapGet("/reports/sprints/{sprintId:int}/burndown", GetBurndown)
            .WithSummary("Get a sprint burndown report")
            .WithDescription("Returns the remaining and ideal burndown lines for a sprint.")
            .RequireAuthorization(NetptunePermissions.Tasks.Read)
            .RequireAuthorization(NetptunePermissions.Sprints.Read);

        group.MapGet("/reports/velocity", GetVelocity)
            .WithSummary("Get the velocity report")
            .WithDescription("Returns committed against completed work for a project's recent sprints.")
            .RequireAuthorization(NetptunePermissions.Tasks.Read)
            .RequireAuthorization(NetptunePermissions.Sprints.Read);

        return group;
    }

    private static async Task<Results<Ok<FlowReport>, NotFound, BadRequest<ClientResponse<FlowReport>>>> GetFlow(
        IMediator mediator,
        [AsParameters] PublicFlowReportFilter filter,
        CancellationToken cancellationToken)
    {
        var reportingFilter = filter.ToFilter();
        var result = await mediator.Send(new GetFlowReportQuery(reportingFilter), cancellationToken);

        return ToResult(result);
    }

    private static async Task<Results<Ok<WorkloadReport>, NotFound, BadRequest<ClientResponse<WorkloadReport>>>> GetWorkload(
        IMediator mediator,
        [AsParameters] PublicWorkloadReportFilter filter,
        CancellationToken cancellationToken)
    {
        var reportingFilter = filter.ToFilter();
        var result = await mediator.Send(new GetWorkloadReportQuery(reportingFilter), cancellationToken);

        return ToResult(result);
    }

    private static async Task<Results<Ok<SprintBurndownReport>, NotFound, BadRequest<ClientResponse<SprintBurndownReport>>>> GetBurndown(
        IMediator mediator,
        int sprintId,
        [AsParameters] PublicSprintBurndownFilter filter,
        CancellationToken cancellationToken)
    {
        var burndownFilter = filter.ToFilter(sprintId);
        var result = await mediator.Send(new GetSprintBurndownReportQuery(burndownFilter), cancellationToken);

        return ToResult(result);
    }

    private static async Task<Results<Ok<VelocityReport>, NotFound, BadRequest<ClientResponse<VelocityReport>>>> GetVelocity(
        IMediator mediator,
        [AsParameters] PublicVelocityReportFilter filter,
        CancellationToken cancellationToken)
    {
        var velocityFilter = filter.ToFilter();
        var result = await mediator.Send(new GetVelocityReportQuery(velocityFilter), cancellationToken);

        return ToResult(result);
    }

    private static Results<Ok<TReport>, NotFound, BadRequest<ClientResponse<TReport>>> ToResult<TReport>(
        ClientResponse<TReport> result)
    {
        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }
}
