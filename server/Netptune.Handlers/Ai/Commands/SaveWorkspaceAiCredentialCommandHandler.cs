using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
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
    private const int MinimumSecretLength = 8;
    private const int MaximumLabelLength = 128;

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
        var secret = request.Secret.Trim();
        var label = request.Label.Trim();
        var isKnownProvider = Enum.IsDefined(request.Provider);

        if (!isKnownProvider)
        {
            return ClientResponse<AiCredentialViewModel>.Failed("Unknown AI provider.");
        }

        if (secret.Length < MinimumSecretLength)
        {
            return ClientResponse<AiCredentialViewModel>.Failed("API key is not valid.");
        }

        if (label.Length is 0 or > MaximumLabelLength)
        {
            return ClientResponse<AiCredentialViewModel>.Failed($"Label must be between 1 and {MaximumLabelLength} characters.");
        }

        var model = request.Model?.Trim();
        var hasModel = !string.IsNullOrWhiteSpace(model);
        var isUnsupportedModel = hasModel && !AiModels.IsSupported(request.Provider, model);

        if (isUnsupportedModel)
        {
            return ClientResponse<AiCredentialViewModel>.Failed("Model is not supported for this provider.");
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var existing = await UnitOfWork.WorkspaceAiCredentials.GetForProvider(
            workspaceId,
            request.Provider,
            cancellationToken);

        var credential = existing ?? await CreateCredential(workspaceId, request.Provider, cancellationToken);

        credential.Label = label;
        credential.Secret = Protector.Protect(secret);
        credential.SecretHint = Protector.CreateHint(secret);
        credential.Model = hasModel ? model : null;
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
