using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IReactionRepository : IWorkspaceEntityRepository<Reaction, int>
{
    Task<bool> HasUserReaction(int commentId, string userId, string value, CancellationToken cancellationToken = default);

    Task<int> DeleteUserReaction(int commentId, string userId, string value, CancellationToken cancellationToken = default);
}
