using Netptune.Core.Entities;

namespace Netptune.Core.SourceControl.History;

public static class ScmHistorySyncState
{
    // Longer than any sync takes, so a claim older than this belongs to a worker that died.
    public static readonly TimeSpan Lease = TimeSpan.FromMinutes(10);

    // A request no worker picked up in this long was lost, and the repo can be synced again.
    public static readonly TimeSpan StallAfter = TimeSpan.FromMinutes(15);

    public static SourceControlHistorySyncStatus Of(SourceControlRepo repo, DateTime now)
    {
        var hasFailed = repo.HistorySyncError is not null;
        var isRequested = repo.HistorySyncRequestedAt is not null
            && (repo.HistorySyncedAt is null || repo.HistorySyncRequestedAt > repo.HistorySyncedAt);
        var isRunning = repo.HistorySyncStartedAt > now - Lease;
        var isFreshRequest = repo.HistorySyncRequestedAt > now - StallAfter;
        var isInProgress = isRequested && (isRunning || isFreshRequest);

        if (hasFailed)
        {
            return SourceControlHistorySyncStatus.Failed;
        }

        if (isInProgress)
        {
            return SourceControlHistorySyncStatus.Syncing;
        }

        if (isRequested)
        {
            return SourceControlHistorySyncStatus.Failed;
        }

        return repo.HistorySyncedAt is null
            ? SourceControlHistorySyncStatus.NotSynced
            : SourceControlHistorySyncStatus.Synced;
    }
}
