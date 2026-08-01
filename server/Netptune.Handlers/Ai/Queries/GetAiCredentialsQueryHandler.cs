using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetAiCredentialsQuery : IRequest<List<AiCredentialViewModel>>;

public sealed class GetAiCredentialsQueryHandler : IRequestHandler<GetAiCredentialsQuery, List<AiCredentialViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAiCredentialsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<AiCredentialViewModel>> Handle(
        GetAiCredentialsQuery query,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var credentials = await UnitOfWork.AiCredentials.GetForUser(userId, cancellationToken);

        return credentials.Select(ToViewModel).ToList();
    }

    private static AiCredentialViewModel ToViewModel(UserAiCredential credential)
    {
        return new AiCredentialViewModel
        {
            Id = credential.Id,
            Provider = credential.Provider,
            Label = credential.Label,
            SecretHint = credential.SecretHint,
            CreatedAt = credential.CreatedAt,
            LastUsedAt = credential.LastUsedAt,
        };
    }
}
