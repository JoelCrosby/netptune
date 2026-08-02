using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetWorkspaceAiCredentialsQuery : IRequest<List<AiCredentialViewModel>>;

public sealed class GetWorkspaceAiCredentialsQueryHandler
    : IRequestHandler<GetWorkspaceAiCredentialsQuery, List<AiCredentialViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetWorkspaceAiCredentialsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<AiCredentialViewModel>> Handle(
        GetWorkspaceAiCredentialsQuery query,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var credentials = await UnitOfWork.WorkspaceAiCredentials.GetForWorkspace(workspaceId, cancellationToken);

        return credentials.Select(ToViewModel).ToList();
    }

    private static AiCredentialViewModel ToViewModel(WorkspaceAiCredential credential)
    {
        return new AiCredentialViewModel
        {
            Id = credential.Id,
            Provider = credential.Provider,
            Label = credential.Label,
            SecretHint = credential.SecretHint,
            Model = credential.Model,
            CreatedAt = credential.CreatedAt,
            LastUsedAt = credential.LastUsedAt,
        };
    }
}
