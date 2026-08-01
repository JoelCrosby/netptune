using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class AiChangeSetRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, AiChangeSet, Guid>(context, connectionFactory), IAiChangeSetRepository
{
    public async Task Add(
        AiChangeSet changeSet,
        IEnumerable<AiProposedChange> changes,
        CancellationToken cancellationToken = default)
    {
        await Entities.AddAsync(changeSet, cancellationToken);
        await Context.AiProposedChanges.AddRangeAsync(changes, cancellationToken);
    }

    public Task<AiChangeSet?> GetOwned(
        Guid changeSetId,
        string userId,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(changeSet =>
                changeSet.Id == changeSetId &&
                changeSet.UserId == userId &&
                changeSet.WorkspaceId == workspaceId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<AiProposedChange>> GetChanges(
        Guid changeSetId,
        CancellationToken cancellationToken = default)
    {
        return Context.AiProposedChanges
            .Where(change => change.ChangeSetId == changeSetId)
            .OrderBy(change => change.Sequence)
            .ToListAsync(cancellationToken);
    }
}
