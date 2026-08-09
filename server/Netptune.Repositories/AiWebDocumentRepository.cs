using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class AiWebDocumentRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, AiWebDocument, Guid>(context, connectionFactory), IAiWebDocumentRepository
{
    public Task<AiWebDocument?> GetInWorkspace(Guid id, int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(
                document => document.Id == id && document.WorkspaceId == workspaceId,
                cancellationToken);
    }

    public Task<int> DeleteExpired(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(document => document.ExpiresAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
