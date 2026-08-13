using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class ReactionRepository : WorkspaceEntityRepository<DataContext, Reaction, int>, IReactionRepository
{
    public ReactionRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<bool> HasUserReaction(int commentId, string userId, string value, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .AnyAsync(UserReaction(commentId, userId, value), cancellationToken);
    }

    // Deleted through the query rather than the change tracker — the comment the caller has already
    // loaded holds the reaction in its navigation, and tracked-graph fixup turns the delete into an update.
    public Task<int> DeleteUserReaction(int commentId, string userId, string value, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(UserReaction(commentId, userId, value))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static Expression<Func<Reaction, bool>> UserReaction(int commentId, string userId, string value)
    {
        return reaction => reaction.CommentId == commentId
                           && reaction.Value == value
                           && (reaction.OwnerId ?? reaction.CreatedByUserId) == userId;
    }
}
