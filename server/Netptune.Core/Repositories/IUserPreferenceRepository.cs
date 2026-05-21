using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IUserPreferenceRepository : IRepository<UserPreferenceValue, int>
{
    Task<List<UserPreferenceValue>> GetValues(
        string userId,
        string key,
        int? workspaceId,
        CancellationToken cancellationToken = default);

    Task<UserPreferenceValue?> GetScopedValue(
        string userId,
        string key,
        int? workspaceId,
        CancellationToken cancellationToken = default);
}
