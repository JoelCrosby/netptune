using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests.ServiceAccounts;
using Netptune.Core.Responses.Common;
using Netptune.Handlers.ServiceAccounts.Commands;
using Netptune.Handlers.ServiceAccounts.Queries;

namespace Netptune.App.Endpoints;

public static class ServiceAccountsEndpoints
{
    public static RouteGroupBuilder MapServiceAccountsEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("service-accounts")
            .WithTags("Service accounts");

        group.MapGet("/", GetServiceAccounts)
            .RequireAuthorization(NetptunePermissions.ServiceAccounts.Read);
        group.MapPost("/", CreateServiceAccount)
            .RequireAuthorization(NetptunePermissions.ServiceAccounts.Create);
        group.MapPost("/{serviceAccountId:int}/credentials", CreateCredential)
            .RequireAuthorization(NetptunePermissions.ServiceAccounts.ManageCredentials);
        group.MapDelete("/{serviceAccountId:int}/credentials/{credentialId:guid}", RevokeCredential)
            .RequireAuthorization(NetptunePermissions.ServiceAccounts.ManageCredentials);

        return group;
    }

    private static async Task<IResult> GetServiceAccounts(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await mediator.Send(new GetServiceAccountsQuery(), cancellationToken));
    }

    private static async Task<IResult> CreateServiceAccount(
        IMediator mediator,
        CreateServiceAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateServiceAccountCommand(request), cancellationToken);
        return ToResult(result);
    }

    private static async Task<IResult> CreateCredential(
        IMediator mediator,
        int serviceAccountId,
        CreateApiCredentialRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateApiCredentialCommand(serviceAccountId, request),
            cancellationToken);
        return ToResult(result);
    }

    private static async Task<IResult> RevokeCredential(
        IMediator mediator,
        int serviceAccountId,
        Guid credentialId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RevokeApiCredentialCommand(serviceAccountId, credentialId),
            cancellationToken);
        return ToResult(result);
    }

    private static IResult ToResult<T>(ClientResponse<T> response)
    {
        if (response.IsNotFound) return Results.NotFound(response);
        if (response.IsForbidden) return Results.Forbid();
        if (!response.IsSuccess) return Results.BadRequest(response);

        return Results.Ok(response.Payload);
    }

    private static IResult ToResult(ClientResponse response)
    {
        if (response.IsNotFound) return Results.NotFound(response);
        if (response.IsForbidden) return Results.Forbid();
        if (!response.IsSuccess) return Results.BadRequest(response);

        return Results.NoContent();
    }
}
