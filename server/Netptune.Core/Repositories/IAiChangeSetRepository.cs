using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IAiChangeSetRepository : IRepository<AiChangeSet, Guid>
{
    Task Add(AiChangeSet changeSet, IEnumerable<AiProposedChange> changes, CancellationToken cancellationToken = default);

    Task<AiChangeSet?> GetOwned(Guid changeSetId, string userId, int workspaceId, CancellationToken cancellationToken = default);

    Task<AiChangeSet?> GetPending(Guid conversationId, string userId, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<AiProposedChange>> GetChanges(Guid changeSetId, CancellationToken cancellationToken = default);
}
