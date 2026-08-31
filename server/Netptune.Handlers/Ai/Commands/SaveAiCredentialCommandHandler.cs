using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Commands;

public sealed record SaveAiCredentialCommand(SaveAiCredentialRequest Request)
    : IRequest<ClientResponse<AiCredentialViewModel>>;

public sealed class SaveAiCredentialCommandHandler
    : IRequestHandler<SaveAiCredentialCommand, ClientResponse<AiCredentialViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiCredentialProtector Protector;

    public SaveAiCredentialCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiCredentialProtector protector)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Protector = protector;
    }

    public async ValueTask<ClientResponse<AiCredentialViewModel>> Handle(
        SaveAiCredentialCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        var (validated, error) = SaveAiCredentialValidation.Validate(request);

        if (validated is null)
        {
            return ClientResponse<AiCredentialViewModel>.Failed(error);
        }

        var userId = Identity.GetCurrentUserId();
        var existing = await UnitOfWork.AiCredentials.GetForProvider(userId, request.Provider, cancellationToken);
        var credential = existing ?? await CreateCredential(userId, request.Provider, cancellationToken);

        credential.Label = validated.Label;
        credential.Secret = Protector.Protect(validated.Secret);
        credential.SecretHint = Protector.CreateHint(validated.Secret);
        credential.Model = validated.Model;
        credential.LastUsedAt = null;

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse<AiCredentialViewModel>.Success(new AiCredentialViewModel
        {
            Id = credential.Id,
            Provider = credential.Provider,
            Label = credential.Label,
            SecretHint = credential.SecretHint,
            Model = credential.Model,
            CreatedAt = credential.CreatedAt,
            LastUsedAt = credential.LastUsedAt,
        });
    }

    private async Task<UserAiCredential> CreateCredential(
        string userId,
        AiProvider provider,
        CancellationToken cancellationToken)
    {
        var credential = new UserAiCredential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            Label = string.Empty,
            Secret = [],
            SecretHint = string.Empty,
            CreatedAt = DateTime.UtcNow,
        };

        return await UnitOfWork.AiCredentials.AddAsync(credential, cancellationToken);
    }
}
