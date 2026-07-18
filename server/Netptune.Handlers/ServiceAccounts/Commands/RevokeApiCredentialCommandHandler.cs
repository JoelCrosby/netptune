using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.ServiceAccounts.Commands;

public sealed record RevokeApiCredentialCommand(int ServiceAccountId, Guid CredentialId) : IRequest<ClientResponse>;

public sealed class RevokeApiCredentialCommandHandler : IRequestHandler<RevokeApiCredentialCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public RevokeApiCredentialCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(RevokeApiCredentialCommand command, CancellationToken cancellationToken)
    {
        var currentUser = await Identity.GetCurrentUser();

        if (currentUser.UserType != AppUserType.User)
        {
            return ClientResponse.Forbidden;
        }

        var credential = await UnitOfWork.ServiceAccounts.GetCredentialForManagement(
            command.CredentialId,
            command.ServiceAccountId,
            await Identity.GetWorkspaceId(),
            cancellationToken);

        if (credential is null) return ClientResponse.NotFound;

        credential.RevokedAt ??= DateTime.UtcNow;
        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
