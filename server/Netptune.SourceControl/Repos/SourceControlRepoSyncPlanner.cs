using Netptune.Core.Entities;
using Netptune.Core.SourceControl.Models;

namespace Netptune.SourceControl.Repos;

internal static class SourceControlRepoSyncPlanner
{
    public static SourceControlRepoSyncPlan Plan(
        SourceControlConnection connection,
        IReadOnlyList<SourceControlRepo> existing,
        IReadOnlyList<ScmRepository> remote,
        HashSet<int> reposWithTaskLinks,
        DateTime syncedAt)
    {
        var remoteById = remote
            .GroupBy(repository => repository.Ref.ExternalId)
            .ToDictionary(group => group.Key, group => group.First());

        var existingIds = existing.Select(repo => repo.ExternalId).ToHashSet();
        var removed = new List<SourceControlRepo>();
        var refreshed = 0;
        var markedInaccessible = 0;

        foreach (var repo in existing)
        {
            var isStillVisible = remoteById.TryGetValue(repo.ExternalId, out var repository);

            if (isStillVisible)
            {
                Refresh(repo, repository!, syncedAt);
                refreshed++;
            }
            else if (repo.IsLinked || reposWithTaskLinks.Contains(repo.Id))
            {
                // Deleting the row would cascade to its task links and project associations, so a
                // repository that has any is kept, in case it's only temporarily out of the installation.
                repo.IsAccessible = false;
                repo.SyncedAt = syncedAt;
                markedInaccessible++;
            }
            else
            {
                removed.Add(repo);
            }
        }

        var added = remoteById.Values
            .Where(repository => !existingIds.Contains(repository.Ref.ExternalId))
            .Select(repository => Create(connection, repository, syncedAt))
            .ToList();

        return new SourceControlRepoSyncPlan
        {
            Added = added,
            Removed = removed,
            Refreshed = refreshed,
            MarkedInaccessible = markedInaccessible,
        };
    }

    private static void Refresh(SourceControlRepo repo, ScmRepository repository, DateTime syncedAt)
    {
        repo.Owner = repository.Ref.Owner;
        repo.Name = repository.Ref.Name;
        repo.FullName = repository.FullName;
        repo.DefaultBranch = repository.DefaultBranch;
        repo.WebUrl = repository.WebUrl;
        repo.IsPrivate = repository.IsPrivate;
        repo.IsArchived = repository.IsArchived;
        repo.IsAccessible = true;
        repo.SyncedAt = syncedAt;
    }

    private static SourceControlRepo Create(
        SourceControlConnection connection,
        ScmRepository repository,
        DateTime syncedAt)
    {
        return new SourceControlRepo
        {
            WorkspaceId = connection.WorkspaceId,
            ConnectionId = connection.Id,
            ExternalId = repository.Ref.ExternalId,
            Owner = repository.Ref.Owner,
            Name = repository.Ref.Name,
            FullName = repository.FullName,
            DefaultBranch = repository.DefaultBranch,
            WebUrl = repository.WebUrl,
            IsPrivate = repository.IsPrivate,
            IsArchived = repository.IsArchived,
            IsAccessible = true,
            SyncedAt = syncedAt,
        };
    }
}
