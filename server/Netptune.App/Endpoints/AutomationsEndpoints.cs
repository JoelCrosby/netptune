using Mediator;

using Netptune.Core.Authorization;
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
        group.MapGet("/{id:int}", HandleGetById).RequireAuthorization(NetptunePermissions.Automations.Read);
        group.MapGet("/{id:int}/runs", HandleGetRuns).RequireAuthorization(NetptunePermissions.Automations.Read);
        group.MapPost("/", HandlePost).RequireAuthorization(NetptunePermissions.Automations.Manage);
        group.MapPut("/{id:int}", HandlePut).RequireAuthorization(NetptunePermissions.Automations.Manage);
        group.MapPost("/{id:int}/enable", HandleEnable).RequireAuthorization(NetptunePermissions.Automations.Manage);
        group.MapPost("/{id:int}/disable", HandleDisable).RequireAuthorization(NetptunePermissions.Automations.Manage);
        group.MapDelete("/{id:int}", HandleDelete).RequireAuthorization(NetptunePermissions.Automations.Manage);

        return group;
    }

    private static async Task<IResult> HandleGet(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAutomationRulesQuery(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetById(int id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAutomationRuleQuery(id), cancellationToken);
        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }

    private static async Task<IResult> HandleGetRuns(int id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAutomationRunsQuery(id), cancellationToken);
        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
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

        if (result.IsNotFound) return Results.NotFound(result);

        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleEnable(int id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SetAutomationRuleEnabledCommand(id, true), cancellationToken);
        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }

    private static async Task<IResult> HandleDisable(int id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SetAutomationRuleEnabledCommand(id, false), cancellationToken);
        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }

    private static async Task<IResult> HandleDelete(int id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteAutomationRuleCommand(id), cancellationToken);
        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }
}
