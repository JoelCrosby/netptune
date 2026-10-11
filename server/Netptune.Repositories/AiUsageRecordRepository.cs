using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public sealed class AiUsageRecordRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, AiUsageRecord, long>(context, connectionFactory), IAiUsageRecordRepository
{
    public Task<List<AiSpendSlice>> GetSpendSlices(int workspaceId, DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        return Since(workspaceId, fromUtc)
            .GroupBy(record => new
            {
                record.UserId,
                DisplayName = record.User!.DisplayName,
                record.Model,
                Day = record.CreatedAt.Date,
            })
            .Select(group => new AiSpendSlice
            {
                UserId = group.Key.UserId,
                UserDisplayName = group.Key.DisplayName,
                Model = group.Key.Model,
                Day = group.Key.Day,
                InputTokens = group.Sum(record => record.InputTokens),
                OutputTokens = group.Sum(record => record.OutputTokens),
                CacheReadTokens = group.Sum(record => record.CacheReadTokens),
                CacheCreationTokens = group.Sum(record => record.CacheCreationTokens),
            })
            .ToListAsync(cancellationToken);
    }

    public Task<List<AiModelTokens>> GetModelTokens(int workspaceId, DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        return Since(workspaceId, fromUtc)
            .GroupBy(record => record.Model)
            .Select(group => new AiModelTokens
            {
                Model = group.Key,
                InputTokens = group.Sum(record => record.InputTokens),
                OutputTokens = group.Sum(record => record.OutputTokens),
                CacheReadTokens = group.Sum(record => record.CacheReadTokens),
                CacheCreationTokens = group.Sum(record => record.CacheCreationTokens),
            })
            .ToListAsync(cancellationToken);
    }

    private IQueryable<AiUsageRecord> Since(int workspaceId, DateTime fromUtc)
    {
        return Entities
            .AsNoTracking()
            .Where(record => record.WorkspaceId == workspaceId && record.CreatedAt >= fromUtc);
    }
}
