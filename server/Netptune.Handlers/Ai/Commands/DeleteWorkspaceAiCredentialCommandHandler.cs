using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Ai.Commands;

public sealed record DeleteWorkspaceAiCredentialCommand(Guid CredentialId) : IRequest<ClientResponse>;

public sealed class DeleteWorkspaceAiCredentialCommandHandler
    : IRequestHandler<DeleteWorkspaceAiCredentialCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public DeleteWorkspaceAiCredentialCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(
        DeleteWorkspaceAiCredentialCommand command,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var credential = await UnitOfWork.WorkspaceAiCredentials.GetOwned(
            command.CredentialId,
            workspaceId,
            cancellationToken);

        if (credential is null)
        {
            return ClientResponse.NotFound;
        }

        await UnitOfWork.WorkspaceAiCredentials.DeletePermanent(credential.Id, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
