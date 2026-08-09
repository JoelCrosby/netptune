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

public sealed record SaveWorkspaceSearchCredentialCommand(SaveWorkspaceSearchCredentialRequest Request)
    : IRequest<ClientResponse<WorkspaceSearchCredentialViewModel>>;

public sealed class SaveWorkspaceSearchCredentialCommandHandler
    : IRequestHandler<SaveWorkspaceSearchCredentialCommand, ClientResponse<WorkspaceSearchCredentialViewModel>>
{
    private const int MinimumSecretLength = 8;
    private const int MaximumEngineIdLength = 128;
    private const int MaximumEndpointLength = 2048;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiCredentialProtector Protector;

    public SaveWorkspaceSearchCredentialCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiCredentialProtector protector)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Protector = protector;
    }

    public async ValueTask<ClientResponse<WorkspaceSearchCredentialViewModel>> Handle(
        SaveWorkspaceSearchCredentialCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        var isKnownProvider = Enum.IsDefined(request.Provider);

        if (!isKnownProvider)
        {
            return ClientResponse<WorkspaceSearchCredentialViewModel>.Failed("Unknown search provider.");
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var existing = await UnitOfWork.WorkspaceSearchCredentials.GetForWorkspace(workspaceId, cancellationToken);
        var secret = request.Secret?.Trim();
        var engineId = request.EngineId?.Trim();
        var endpoint = request.Endpoint?.Trim();
        var isProviderChanging = existing is not null && existing.Provider != request.Provider;
        var keepsExistingSecret = existing?.Secret is not null && !isProviderChanging && string.IsNullOrWhiteSpace(secret);
        var invalid = Validate(request.Provider, secret, engineId, endpoint, keepsExistingSecret);

        if (invalid is not null)
        {
            return ClientResponse<WorkspaceSearchCredentialViewModel>.Failed(invalid);
        }

        var credential = existing ?? await CreateCredential(workspaceId, cancellationToken);

        credential.Provider = request.Provider;
        credential.EngineId = string.IsNullOrWhiteSpace(engineId) ? null : engineId;
        credential.Endpoint = string.IsNullOrWhiteSpace(endpoint) ? null : endpoint;
        credential.CreatedByUserId = Identity.GetCurrentUserId();

        if (!keepsExistingSecret)
        {
            var hasSecret = !string.IsNullOrWhiteSpace(secret);

            credential.Secret = hasSecret ? Protector.Protect(secret!) : null;
            credential.SecretHint = hasSecret ? Protector.CreateHint(secret!) : string.Empty;
            credential.LastUsedAt = null;
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse<WorkspaceSearchCredentialViewModel>.Success(ToViewModel(credential));
    }

    private static string? Validate(
        WebSearchProvider provider,
        string? secret,
        string? engineId,
        string? endpoint,
        bool keepsExistingSecret)
    {
        var hasSecret = !string.IsNullOrWhiteSpace(secret);
        var needsKey = provider is WebSearchProvider.Brave or WebSearchProvider.Google;
        var isMissingKey = needsKey && !hasSecret && !keepsExistingSecret;

        if (isMissingKey)
        {
            return "An API key is required for this provider.";
        }

        var isShortKey = hasSecret && secret!.Length < MinimumSecretLength;

        if (isShortKey)
        {
            return "API key is not valid.";
        }

        var isMissingEngineId = provider == WebSearchProvider.Google && string.IsNullOrWhiteSpace(engineId);

        if (isMissingEngineId)
        {
            return "Google search needs a search engine id.";
        }

        var isLongEngineId = engineId?.Length > MaximumEngineIdLength;

        if (isLongEngineId)
        {
            return $"Search engine id must be {MaximumEngineIdLength} characters or fewer.";
        }

        return ValidateEndpoint(provider, endpoint);
    }

    private static string? ValidateEndpoint(WebSearchProvider provider, string? endpoint)
    {
        var hasEndpoint = !string.IsNullOrWhiteSpace(endpoint);
        var isMissingEndpoint = provider == WebSearchProvider.Searxng && !hasEndpoint;

        if (isMissingEndpoint)
        {
            return "SearXNG needs the base URL of an instance.";
        }

        if (!hasEndpoint)
        {
            return null;
        }

        var isLongEndpoint = endpoint!.Length > MaximumEndpointLength;

        if (isLongEndpoint)
        {
            return $"Endpoint must be {MaximumEndpointLength} characters or fewer.";
        }

        var isAbsolute = Uri.TryCreate(endpoint, UriKind.Absolute, out var uri);
        var isHttp = isAbsolute && (uri!.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        return isHttp ? null : "Endpoint must be an absolute http or https URL.";
    }

    private static WorkspaceSearchCredentialViewModel ToViewModel(WorkspaceSearchCredential credential)
    {
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

    private async Task<WorkspaceSearchCredential> CreateCredential(int workspaceId, CancellationToken cancellationToken)
    {
        var credential = new WorkspaceSearchCredential
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            CreatedAt = DateTime.UtcNow,
        };

        return await UnitOfWork.WorkspaceSearchCredentials.AddAsync(credential, cancellationToken);
    }
}
