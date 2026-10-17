using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class SourceControlCheckRunRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, SourceControlCheckRun, long>(context, connectionFactory), ISourceControlCheckRunRepository
{
    public Task<SourceControlCheckRun?> GetByExternalId(
        int repoId,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        return Entities.FirstOrDefaultAsync(
            checkRun => checkRun.RepoId == repoId && checkRun.ExternalId == externalId,
            cancellationToken);
    }

    public Task<List<SourceControlCheckRun>> GetForCommits(
        IReadOnlyCollection<int> repoIds,
        IReadOnlyCollection<string> commitShas,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(checkRun => repoIds.Contains(checkRun.RepoId) && commitShas.Contains(checkRun.CommitSha))
            .ToListAsync(cancellationToken);
    }

    public Task<int> DeleteUpdatedBefore(DateTime updatedBefore, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(checkRun => checkRun.UpdatedAt < updatedBefore)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
