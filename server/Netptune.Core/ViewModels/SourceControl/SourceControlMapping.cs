using Netptune.Core.Entities;
using Netptune.Core.SourceControl.History;

namespace Netptune.Core.ViewModels.SourceControl;

public static class SourceControlMapping
{
    public static SourceControlRepoViewModel ToViewModel(SourceControlRepo repo)
    {
        return new SourceControlRepoViewModel
        {
            Id = repo.Id,
            ConnectionId = repo.ConnectionId,
            Owner = repo.Owner,
            Name = repo.Name,
            FullName = repo.FullName,
            DefaultBranch = repo.DefaultBranch,
            WebUrl = repo.WebUrl,
            IsPrivate = repo.IsPrivate,
            IsArchived = repo.IsArchived,
            IsAccessible = repo.IsAccessible,
            IsLinked = repo.IsLinked,
            LinkedAt = repo.LinkedAt,
            SyncedAt = repo.SyncedAt,
            HistorySyncStatus = ScmHistorySyncState.Of(repo, DateTime.UtcNow),
            HistorySyncedAt = repo.HistorySyncedAt,
        };
    }

    public static SourceControlIdentityViewModel ToViewModel(UserSourceControlIdentity identity)
    {
        return new SourceControlIdentityViewModel
        {
            Id = identity.Id,
            Provider = identity.Provider,
            Username = identity.Username,
            IsVerified = identity.IsVerified,
        };
    }
}
