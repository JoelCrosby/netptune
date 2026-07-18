using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.ServiceAccounts.Commands;

public sealed record DeleteServiceAccountCommand(int ServiceAccountId) : IRequest<ClientResponse>;

public sealed class DeleteServiceAccountCommandHandler
    : IRequestHandler<DeleteServiceAccountCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public DeleteServiceAccountCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(
        DeleteServiceAccountCommand command,
        CancellationToken cancellationToken)
    {
        var currentUser = await Identity.GetCurrentUser();

        if (currentUser.UserType != AppUserType.User)
        {
            return ClientResponse.Forbidden;
        }

        var serviceAccount = await UnitOfWork.ServiceAccounts.GetForManagement(
            command.ServiceAccountId,
            await Identity.GetWorkspaceId(),
            cancellationToken);

        if (serviceAccount is null) return ClientResponse.NotFound;

        var deletedAt = serviceAccount.DisabledAt ?? DateTime.UtcNow;
        serviceAccount.DisabledAt = deletedAt;

        foreach (var credential in serviceAccount.Credentials)
        {
            credential.RevokedAt ??= deletedAt;
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
