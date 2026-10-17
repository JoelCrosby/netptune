using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class UserSourceControlIdentityRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, UserSourceControlIdentity, int>(context, connectionFactory), IUserSourceControlIdentityRepository
{
    public Task<List<UserSourceControlIdentity>> GetForUser(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(identity => identity.UserId == userId)
            .OrderBy(identity => identity.Provider)
            .ThenBy(identity => identity.NormalizedUsername)
            .ToListAsync(cancellationToken);
    }

    public Task<UserSourceControlIdentity?> GetOwned(
        int id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return Entities.FirstOrDefaultAsync(
            identity => identity.Id == id && identity.UserId == userId,
            cancellationToken);
    }

    public Task<UserSourceControlIdentity?> GetByUsername(
        SourceControlProvider provider,
        string username,
        CancellationToken cancellationToken = default)
    {
        var normalized = UserSourceControlIdentity.Normalize(username);

        return Entities.FirstOrDefaultAsync(
            identity => identity.Provider == provider && identity.NormalizedUsername == normalized,
            cancellationToken);
    }

    public async Task RecordVerified(
        string userId,
        SourceControlProvider provider,
        string externalId,
        string? username,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var normalized = username is null ? null : UserSourceControlIdentity.Normalize(username);

        var related = await Entities
            .Where(identity => identity.Provider == provider)
            .Where(identity => identity.ExternalId == externalId
                || (normalized != null && identity.NormalizedUsername == normalized))
            .ToListAsync(cancellationToken);

        var existing = related.FirstOrDefault(identity => identity.ExternalId == externalId);
        var belongsToSomeoneElse = existing is not null && existing.UserId != userId;

        if (belongsToSomeoneElse)
        {
            Entities.Remove(existing!);
        }

        var verified = belongsToSomeoneElse ? null : existing;

        ReleaseUsername(related, verified, normalized, now);

        if (verified is null)
        {
            verified = new UserSourceControlIdentity
            {
                UserId = userId,
                Provider = provider,
                ExternalId = externalId,
                CreatedAt = now,
            };

            await Entities.AddAsync(verified, cancellationToken);
        }

        if (username is not null)
        {
            verified.SetUsername(username);
        }

        verified.IsVerified = true;
        verified.UpdatedAt = now;
    }

    public async Task RecordObserved(
        SourceControlProvider provider,
        IReadOnlyCollection<ScmIdentity> accounts,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var observed = accounts
            .Where(account => !string.IsNullOrEmpty(account.ExternalId))
            .Where(account => !string.IsNullOrWhiteSpace(account.Username))
            .DistinctBy(account => account.ExternalId)
            .ToList();

        if (observed.Count == 0)
        {
            return;
        }

        var externalIds = observed.Select(account => account.ExternalId!).ToList();
        var usernames = observed.Select(account => UserSourceControlIdentity.Normalize(account.Username)).ToList();

        var related = await Entities
            .Where(identity => identity.Provider == provider)
            .Where(identity => externalIds.Contains(identity.ExternalId!)
                || usernames.Contains(identity.NormalizedUsername!))
            .ToListAsync(cancellationToken);

        foreach (var account in observed)
        {
            var owner = related.FirstOrDefault(identity => identity.ExternalId == account.ExternalId);
            var normalized = UserSourceControlIdentity.Normalize(account.Username);
            var isCurrent = owner?.NormalizedUsername == normalized;

            if (owner is null || isCurrent)
            {
                continue;
            }

            ReleaseUsername(related, owner, normalized, now);

            owner.SetUsername(account.Username);
            owner.UpdatedAt = now;
        }
    }

    public Task<List<UserSourceControlIdentity>> GetMemberIdentities(
        int workspaceId,
        SourceControlProvider provider,
        IReadOnlyCollection<string> externalIds,
        IReadOnlyCollection<string> normalizedUsernames,
        CancellationToken cancellationToken = default)
    {
        var members = Context.WorkspaceAppUsers
            .Where(member => member.WorkspaceId == workspaceId)
            .Select(member => member.UserId);

        return Entities
            .AsNoTracking()
            .Include(identity => identity.User)
            .Where(identity => identity.Provider == provider && members.Contains(identity.UserId))
            .Where(identity => externalIds.Contains(identity.ExternalId!)
                || normalizedUsernames.Contains(identity.NormalizedUsername!))
            .ToListAsync(cancellationToken);
    }

    public Task<List<AppUser>> GetMembersByEmail(
        int workspaceId,
        IReadOnlyCollection<string> normalizedEmails,
        CancellationToken cancellationToken = default)
    {
        return Context.WorkspaceAppUsers
            .AsNoTracking()
            .Where(member => member.WorkspaceId == workspaceId)
            .Select(member => member.User)
            .Where(user => normalizedEmails.Contains(user.NormalizedEmail!))
            .ToListAsync(cancellationToken);
    }

    // A username belongs to one account at a time: when it moves (a rename, or a verified owner
    // turning up), stale verified rows lose it and manual claims on it are dropped.
    private void ReleaseUsername(
        List<UserSourceControlIdentity> related,
        UserSourceControlIdentity? owner,
        string? normalized,
        DateTime now)
    {
        if (normalized is null)
        {
            return;
        }

        var holders = related
            .Where(identity => identity != owner && identity.NormalizedUsername == normalized)
            .ToList();

        foreach (var holder in holders)
        {
            if (holder.IsVerified)
            {
                holder.SetUsername(null);
                holder.UpdatedAt = now;
            }
            else
            {
                Entities.Remove(holder);
            }
        }
    }
}
