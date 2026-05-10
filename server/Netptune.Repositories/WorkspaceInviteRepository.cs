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

    public Task<List<WorkspaceInvite>> GetPendingByWorkspace(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(x => x.WorkspaceId == workspaceId && x.AcceptedAt == null)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
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

    public async Task Accept(string code, CancellationToken cancellationToken = default)
    {
        var invite = await Entities
            .Where(x => x.Code == code)
            .FirstOrDefaultAsync(cancellationToken);

        if (invite is not null)
        {
            invite.AcceptedAt = DateTime.UtcNow;
        }
    }

    public async Task DeleteByEmail(string email, int workspaceId, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().IdentityNormalize();

        var invites = await Entities
            .Where(x => x.Email.ToUpper() == normalized && x.WorkspaceId == workspaceId && x.AcceptedAt == null)
            .ToListAsync(cancellationToken);

        Entities.RemoveRange(invites);
    }
}
