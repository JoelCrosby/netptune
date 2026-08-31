using Mediator;

using Netptune.App.Utility;
using Netptune.Core.Authorization;
using Netptune.Core.Models.Automations;
using Netptune.Core.Requests;
using Netptune.Handlers.Automations.Commands;
using Netptune.Handlers.Automations.Queries;

namespace Netptune.App.Endpoints;

public static class AutomationsEndpoints
{
    public static RouteGroupBuilder MapAutomationsEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("automations");

        group.MapGet("/", HandleGet).RequireAuthorization(NetptunePermissions.Automations.Read);
        group.MapGet("/summary", HandleGetSummary).RequireAuthorization(NetptunePermissions.Automations.Read);
        group.MapGet("/{id:int}", HandleGetById).RequireAuthorization(NetptunePermissions.Automations.Read);
        group.MapGet("/{id:int}/runs", HandleGetRuns).RequireAuthorization(NetptunePermissions.Automations.Read);
        group.MapGet("/{id:int}/dry-run/{taskId:int}", HandleGetDryRun).RequireAuthorization(NetptunePermissions.Automations.Read);
        group.MapPost("/", HandlePost).RequireAuthorization(NetptunePermissions.Automations.Manage);
        group.MapPut("/{id:int}", HandlePut).RequireAuthorization(NetptunePermissions.Automations.Manage);
        group.MapPost("/{id:int}/run", HandleRun).RequireAuthorization(NetptunePermissions.Automations.Manage);
        group.MapPost("/{id:int}/clone", HandleClone).RequireAuthorization(NetptunePermissions.Automations.Manage);
        group.MapPost("/{id:int}/enable", HandleEnable).RequireAuthorization(NetptunePermissions.Automations.Manage);
        group.MapPost("/{id:int}/disable", HandleDisable).RequireAuthorization(NetptunePermissions.Automations.Manage);
        group.MapDelete("/{id:int}", HandleDelete).RequireAuthorization(NetptunePermissions.Automations.Manage);

        return group;
    }

    private static async Task<IResult> HandleGet(
        IMediator mediator,
        [AsParameters] AutomationRuleFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAutomationRulesPagedQuery(filter), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetSummary(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAutomationRuleSummaryQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetById(int id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAutomationRuleQuery(id), cancellationToken);
        return result.ToResult();
    }

    private static async Task<IResult> HandleGetRuns(
        int id,
        IMediator mediator,
        [AsParameters] PageRequest page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAutomationRunsQuery(id, page), cancellationToken);
        return result.ToResult();
    }

    private static async Task<IResult> HandleGetDryRun(
        int id,
        int taskId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAutomationDryRunQuery(id, taskId), cancellationToken);
        return result.ToResult();
    }

    private static async Task<IResult> HandleRun(
        int id,
        AutomationManualRunRequestBody request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RunAutomationRuleCommand(id, request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleClone(
        int id,
        AutomationCloneRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CloneAutomationRuleCommand(id, request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandlePost(
        AutomationRuleRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateAutomationRuleCommand(request), cancellationToken);
        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandlePut(
        int id,
        AutomationRuleRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateAutomationRuleCommand(id, request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleEnable(int id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SetAutomationRuleEnabledCommand(id, true), cancellationToken);
        return result.ToResult();
    }

    private static async Task<IResult> HandleDisable(int id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SetAutomationRuleEnabledCommand(id, false), cancellationToken);
        return result.ToResult();
    }

    private static async Task<IResult> HandleDelete(int id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteAutomationRuleCommand(id), cancellationToken);
        return result.ToResult();
    }
}
