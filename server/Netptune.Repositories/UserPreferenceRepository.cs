using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class UserPreferenceRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, UserPreferenceValue, int>(context, connectionFactory), IUserPreferenceRepository
{
    public Task<List<UserPreferenceValue>> GetValues(
        string userId,
        string key,
        int? workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(preference =>
                preference.UserId == userId &&
                preference.Key == key &&
                (preference.WorkspaceId == null || preference.WorkspaceId == workspaceId))
            .ToListAsync(cancellationToken);
    }

    public Task<UserPreferenceValue?> GetScopedValue(
        string userId,
        string key,
        int? workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .FirstOrDefaultAsync(
                preference =>
                    preference.UserId == userId &&
                    preference.Key == key &&
                    preference.WorkspaceId == workspaceId,
                cancellationToken);
    }
}
