using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Ai.Commands;

public sealed record DeleteWorkspaceSearchCredentialCommand : IRequest<ClientResponse>;

public sealed class DeleteWorkspaceSearchCredentialCommandHandler
    : IRequestHandler<DeleteWorkspaceSearchCredentialCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public DeleteWorkspaceSearchCredentialCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(
        DeleteWorkspaceSearchCredentialCommand command,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var credential = await UnitOfWork.WorkspaceSearchCredentials.GetForWorkspace(workspaceId, cancellationToken);

        if (credential is null)
        {
            return ClientResponse.NotFound;
        }

        await UnitOfWork.WorkspaceSearchCredentials.DeletePermanent(credential.Id, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
