using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Ai;

public static class AiHandlerCredentials
{
    public static async Task<AiResolvedCredential?> Resolve(
        INetptuneUnitOfWork unitOfWork,
        string userId,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        var userCredentials = await unitOfWork.AiCredentials.GetForUser(userId, cancellationToken);
        var workspaceCredentials = await unitOfWork.WorkspaceAiCredentials.GetForWorkspace(workspaceId, cancellationToken);

        return AiCredentialResolution.Resolve(userCredentials, workspaceCredentials).FirstOrDefault();
    }

    public static async Task MarkUsed(
        INetptuneUnitOfWork unitOfWork,
        AiResolvedCredential credential,
        CancellationToken cancellationToken)
    {
        if (credential.Source == AiCredentialSource.Workspace)
        {
            var workspaceCredential = await unitOfWork.WorkspaceAiCredentials.GetAsync(
                credential.Id,
                cancellationToken: cancellationToken);

            if (workspaceCredential is not null)
            {
                workspaceCredential.LastUsedAt = DateTime.UtcNow;
            }

            return;
        }

        var userCredential = await unitOfWork.AiCredentials.GetAsync(credential.Id, cancellationToken: cancellationToken);

        if (userCredential is not null)
        {
            userCredential.LastUsedAt = DateTime.UtcNow;
        }
    }
}
