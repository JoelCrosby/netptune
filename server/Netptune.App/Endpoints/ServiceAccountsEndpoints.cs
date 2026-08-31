using Mediator;

using Netptune.App.Utility;
using Netptune.Core.Authorization;
using Netptune.Core.Requests.ServiceAccounts;
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
        group.MapPut("/{serviceAccountId:int}", UpdateServiceAccount)
            .RequireAuthorization(NetptunePermissions.ServiceAccounts.Update);
        group.MapDelete("/{serviceAccountId:int}", DeleteServiceAccount)
            .RequireAuthorization(NetptunePermissions.ServiceAccounts.Delete);
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
        return result.ToPayloadResult();
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

        return result.ToPayloadResult();
    }

    private static async Task<IResult> UpdateServiceAccount(
        IMediator mediator,
        int serviceAccountId,
        UpdateServiceAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateServiceAccountCommand(serviceAccountId, request),
            cancellationToken);

        return result.ToPayloadResult();
    }

    private static async Task<IResult> DeleteServiceAccount(
        IMediator mediator,
        int serviceAccountId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new DeleteServiceAccountCommand(serviceAccountId),
            cancellationToken);

        return result.ToNoContentResult();
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

        return result.ToNoContentResult();
    }
}
