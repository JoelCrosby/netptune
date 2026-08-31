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

public sealed record SaveWorkspaceAiCredentialCommand(SaveAiCredentialRequest Request)
    : IRequest<ClientResponse<AiCredentialViewModel>>;

public sealed class SaveWorkspaceAiCredentialCommandHandler
    : IRequestHandler<SaveWorkspaceAiCredentialCommand, ClientResponse<AiCredentialViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiCredentialProtector Protector;

    public SaveWorkspaceAiCredentialCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiCredentialProtector protector)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Protector = protector;
    }

    public async ValueTask<ClientResponse<AiCredentialViewModel>> Handle(
        SaveWorkspaceAiCredentialCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        var (validated, error) = SaveAiCredentialValidation.Validate(request);

        if (validated is null)
        {
            return ClientResponse<AiCredentialViewModel>.Failed(error);
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var existing = await UnitOfWork.WorkspaceAiCredentials.GetForProvider(
            workspaceId,
            request.Provider,
            cancellationToken);

        var credential = existing ?? await CreateCredential(workspaceId, request.Provider, cancellationToken);

        credential.Label = validated.Label;
        credential.Secret = Protector.Protect(validated.Secret);
        credential.SecretHint = Protector.CreateHint(validated.Secret);
        credential.Model = validated.Model;
        credential.CreatedByUserId = Identity.GetCurrentUserId();
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

    private async Task<WorkspaceAiCredential> CreateCredential(
        int workspaceId,
        AiProvider provider,
        CancellationToken cancellationToken)
    {
        var credential = new WorkspaceAiCredential
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Provider = provider,
            Label = string.Empty,
            Secret = [],
            SecretHint = string.Empty,
            CreatedAt = DateTime.UtcNow,
        };

        return await UnitOfWork.WorkspaceAiCredentials.AddAsync(credential, cancellationToken);
    }
}
