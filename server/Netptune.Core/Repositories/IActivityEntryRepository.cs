using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IActivityEntryRepository : IWorkspaceEntityRepository<ActivityEntry, int>
{
    Task<UpsertEntryResult> UpsertEntry(
        ActivityEntryUpsert upsert,
        DateTime now,
        TimeSpan windowDuration,
        TimeSpan maxWindowDuration,
        CancellationToken cancellationToken = default);

    // Frees the unique-index slot held by an entry whose window has already expired. Must not stamp
    // NotifiedAt: the entry is still owed its notifications, and the sweeper's claim keys on
    // notified_at IS NULL, not on is_open, so it is still picked up on the next tick.
    Task<int> CloseStaleEntry(
        int workspaceId,
        EntityType entityType,
        int entityId,
        string userId,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task<ActivityEntry?> FindMergeCandidate(
        int workspaceId,
        EntityType entityType,
        int entityId,
        string userId,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task<int> ExpireEntriesForOtherUsers(
        int workspaceId,
        EntityType entityType,
        int entityId,
        string userId,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task<List<ActivityEntry>> ClaimExpiredEntries(int limit, CancellationToken cancellationToken = default);
}
