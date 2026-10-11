using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IAiUsageRecordRepository : IRepository<AiUsageRecord, long>
{
    Task<List<AiSpendSlice>> GetSpendSlices(int workspaceId, DateTime fromUtc, CancellationToken cancellationToken = default);

    Task<List<AiModelTokens>> GetModelTokens(int workspaceId, DateTime fromUtc, CancellationToken cancellationToken = default);
}
