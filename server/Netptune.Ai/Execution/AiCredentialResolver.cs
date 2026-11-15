using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;

namespace Netptune.Ai.Execution;

public sealed class AiCredentialResolver : IAiCredentialResolver
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IAiCredentialProtector Protector;
    private readonly AiDevelopmentKeys DevelopmentKeys;

    public AiCredentialResolver(
        INetptuneUnitOfWork unitOfWork,
        IAiCredentialProtector protector,
        IOptions<AiOptions> options)
    {
        UnitOfWork = unitOfWork;
        Protector = protector;
        DevelopmentKeys = options.Value.DevelopmentKeys;
    }

    public async Task<List<AiResolvedCredential>> Resolve(
        string userId,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        var userCredentials = await UnitOfWork.AiCredentials.GetForUser(userId, cancellationToken);
        var workspaceCredentials = await UnitOfWork.WorkspaceAiCredentials.GetForWorkspace(
            workspaceId,
            cancellationToken);

        var storedCredentials = AiCredentialResolution.Resolve(userCredentials, workspaceCredentials);
        var developmentCredentials = CreateDevelopmentCredentials();

        return AiCredentialResolution.WithFallback(storedCredentials, developmentCredentials);
    }

    public async Task MarkUsed(AiResolvedCredential credential, CancellationToken cancellationToken = default)
    {
        if (credential.Source == AiCredentialSource.Development)
        {
            return;
        }

        if (credential.Source == AiCredentialSource.Workspace)
        {
            var workspaceCredential = await UnitOfWork.WorkspaceAiCredentials.GetAsync(
                credential.Id,
                cancellationToken: cancellationToken);

            if (workspaceCredential is not null)
            {
                workspaceCredential.LastUsedAt = DateTime.UtcNow;
            }

            return;
        }

        var userCredential = await UnitOfWork.AiCredentials.GetAsync(credential.Id, cancellationToken: cancellationToken);

        if (userCredential is not null)
        {
            userCredential.LastUsedAt = DateTime.UtcNow;
        }
    }

    private List<AiResolvedCredential> CreateDevelopmentCredentials()
    {
        var keys = new Dictionary<AiProvider, string?>
        {
            [AiProvider.Anthropic] = DevelopmentKeys.Anthropic,
            [AiProvider.OpenAi] = DevelopmentKeys.OpenAi,
        };

        return keys
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => CreateDevelopmentCredential(pair.Key, pair.Value!))
            .ToList();
    }

    private AiResolvedCredential CreateDevelopmentCredential(AiProvider provider, string apiKey)
    {
        var secret = Protector.Protect(apiKey.Trim());

        return new AiResolvedCredential
        {
            Provider = provider,
            Source = AiCredentialSource.Development,
            Secret = secret,
        };
    }
}
