using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class WorkspaceSearchCredentialRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, WorkspaceSearchCredential, Guid>(context, connectionFactory), IWorkspaceSearchCredentialRepository
{
    public Task<WorkspaceSearchCredential?> GetForWorkspace(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities.FirstOrDefaultAsync(credential => credential.WorkspaceId == workspaceId, cancellationToken);
    }
}
