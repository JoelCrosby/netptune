using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetWorkspaceSearchCredentialQuery : IRequest<WorkspaceSearchCredentialViewModel?>;

public sealed class GetWorkspaceSearchCredentialQueryHandler
    : IRequestHandler<GetWorkspaceSearchCredentialQuery, WorkspaceSearchCredentialViewModel?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetWorkspaceSearchCredentialQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<WorkspaceSearchCredentialViewModel?> Handle(
        GetWorkspaceSearchCredentialQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var credential = await UnitOfWork.WorkspaceSearchCredentials.GetForWorkspace(workspaceId, cancellationToken);

        if (credential is null)
        {
            return null;
        }

        return new WorkspaceSearchCredentialViewModel
        {
            Id = credential.Id,
            Provider = credential.Provider,
            SecretHint = credential.SecretHint,
            EngineId = credential.EngineId,
            Endpoint = credential.Endpoint,
            CreatedAt = credential.CreatedAt,
            LastUsedAt = credential.LastUsedAt,
        };
    }
}
