using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.Sql;

using Npgsql;

namespace Netptune.Repositories;

public class ReactionRepository : WorkspaceEntityRepository<DataContext, Reaction, int>, IReactionRepository
{
    public ReactionRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public async Task<bool> TryAdd(Reaction reaction, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reaction.OwnerId);

        var parameters = new object[]
        {
            new NpgsqlParameter("comment_id", reaction.CommentId),
            new NpgsqlParameter("workspace_id", reaction.WorkspaceId),
            new NpgsqlParameter("owner_id", reaction.OwnerId),
            new NpgsqlParameter("value", reaction.Value),
        };

        var inserted = await Context.Database.ExecuteSqlRawAsync(SqlScripts.InsertReaction, parameters, cancellationToken);

        return inserted == 1;
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
                           && reaction.OwnerId == userId;
    }
}
