using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IActivityEntryRepository : IWorkspaceEntityRepository<ActivityEntry, int>
{
    Task<UpsertEntryResult> UpsertEntry(ActivityEntryUpsert upsert, DateTime now, TimeSpan windowDuration, TimeSpan maxWindowDuration, CancellationToken cancellationToken = default);

    Task<int> CloseStaleEntry(int workspaceId, EntityType entityType, int entityId, string userId, string agent, DateTime now, CancellationToken cancellationToken = default);

    Task<ActivityEntry?> FindMergeCandidate(int workspaceId, EntityType entityType, int entityId, string userId, DateTime now, CancellationToken cancellationToken = default);

    Task<int> ExpireEntriesForOtherUsers(int workspaceId, EntityType entityType, int entityId, string userId, DateTime now, CancellationToken cancellationToken = default);

    Task<List<ActivityEntry>> ClaimExpiredEntries(int limit, CancellationToken cancellationToken = default);
}
