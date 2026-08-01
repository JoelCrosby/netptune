using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Ai.Commands;

public sealed record DeleteAiCredentialCommand(Guid CredentialId) : IRequest<ClientResponse>;

public sealed class DeleteAiCredentialCommandHandler : IRequestHandler<DeleteAiCredentialCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public DeleteAiCredentialCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(
        DeleteAiCredentialCommand command,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var credential = await UnitOfWork.AiCredentials.GetOwned(command.CredentialId, userId, cancellationToken);

        if (credential is null)
        {
            return ClientResponse.NotFound;
        }

        await UnitOfWork.AiCredentials.DeletePermanent(credential.Id, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
