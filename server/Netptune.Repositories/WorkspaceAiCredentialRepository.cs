using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class WorkspaceAiCredentialRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, WorkspaceAiCredential, Guid>(context, connectionFactory), IWorkspaceAiCredentialRepository
{
    public Task<List<WorkspaceAiCredential>> GetForWorkspace(
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(credential => credential.WorkspaceId == workspaceId)
            .OrderBy(credential => credential.Provider)
            .ToListAsync(cancellationToken);
    }

    public Task<WorkspaceAiCredential?> GetForProvider(
        int workspaceId,
        AiProvider provider,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(credential => credential.WorkspaceId == workspaceId && credential.Provider == provider)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<WorkspaceAiCredential?> GetOwned(
        Guid credentialId,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(credential => credential.Id == credentialId && credential.WorkspaceId == workspaceId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
