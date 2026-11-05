using Microsoft.EntityFrameworkCore;

using Netptune.Core.Extensions;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class WorkspaceInviteRepository : Repository<DataContext, WorkspaceInvite, int>, IWorkspaceInviteRepository
{
    public WorkspaceInviteRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<WorkspaceInvite?> GetByCode(string code, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(x => x.Code == code && x.AcceptedAt == null && x.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<WorkspaceInvite?> GetPendingByEmail(string email, int workspaceId, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().IdentityNormalize();

        return Entities
            .Where(x => x.Email.ToUpper() == normalized && x.WorkspaceId == workspaceId && x.AcceptedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<WorkspaceInvite>> GetPendingByEmailRange(IEnumerable<string> emails, int workspaceId, CancellationToken cancellationToken = default)
    {
        var normalized = emails.Select(e => e.Trim().IdentityNormalize()).ToHashSet();

        return Entities
            .Where(x => normalized.Contains(x.Email.ToUpper()) && x.WorkspaceId == workspaceId && x.AcceptedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryAccept(string code, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var accepted = await Entities
            .Where(x => x.Code == code && x.AcceptedAt == null && x.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.AcceptedAt, now), cancellationToken);

        return accepted > 0;
    }

    public async Task<List<string>> RemovePending(IEnumerable<string> emails, int workspaceId, CancellationToken cancellationToken = default)
    {
        var normalized = emails.Select(e => e.Trim().IdentityNormalize()).ToHashSet();
        var pending = Entities.Where(x => normalized.Contains(x.Email.ToUpper()) && x.WorkspaceId == workspaceId && x.AcceptedAt == null);
        var removedEmails = await pending.Select(x => x.Email).Distinct().ToListAsync(cancellationToken);

        await pending.ExecuteDeleteAsync(cancellationToken);

        return removedEmails;
    }
}
