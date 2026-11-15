using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public interface IAiCredentialResolver
{
    Task<List<AiResolvedCredential>> Resolve(string userId, int workspaceId, CancellationToken cancellationToken = default);

    Task MarkUsed(AiResolvedCredential credential, CancellationToken cancellationToken = default);
}
