using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Ai;

public sealed record AiUsageOrigin(int WorkspaceId, string UserId, string Purpose);

public static class AiUsageRecorder
{
    public static Task Record(
        INetptuneUnitOfWork unitOfWork,
        AiUsageOrigin origin,
        AiResolvedCredential credential,
        string model,
        AiUsage usage,
        CancellationToken cancellationToken)
    {
        var record = new AiUsageRecord
        {
            WorkspaceId = origin.WorkspaceId,
            UserId = origin.UserId,
            Provider = credential.Provider,
            Model = model,
            Purpose = origin.Purpose,
            InputTokens = usage.InputTokens,
            OutputTokens = usage.OutputTokens,
            CacheReadTokens = usage.CacheReadTokens,
            CacheCreationTokens = usage.CacheCreationTokens,
            CreatedAt = DateTime.UtcNow,
        };

        return unitOfWork.AiUsageRecords.AddAsync(record, cancellationToken);
    }
}
