using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;

namespace Netptune.Core.Repositories;

public interface IUserSourceControlIdentityRepository : IRepository<UserSourceControlIdentity, int>
{
    Task<List<UserSourceControlIdentity>> GetForUser(string userId, CancellationToken cancellationToken = default);

    Task<UserSourceControlIdentity?> GetOwned(int id, string userId, CancellationToken cancellationToken = default);

    Task<UserSourceControlIdentity?> GetByUsername(SourceControlProvider provider, string username, CancellationToken cancellationToken = default);

    Task RecordVerified(string userId, SourceControlProvider provider, string externalId, string? username, DateTime now, CancellationToken cancellationToken = default);

    Task RecordObserved(SourceControlProvider provider, IReadOnlyCollection<ScmIdentity> accounts, DateTime now, CancellationToken cancellationToken = default);

    Task<List<UserSourceControlIdentity>> GetMemberIdentities(int workspaceId, SourceControlProvider provider, IReadOnlyCollection<string> externalIds, IReadOnlyCollection<string> normalizedUsernames, CancellationToken cancellationToken = default);

    Task<List<AppUser>> GetMembersByEmail(int workspaceId, IReadOnlyCollection<string> normalizedEmails, CancellationToken cancellationToken = default);
}
