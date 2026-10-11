using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public sealed class PageFavouriteRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, PageFavourite, int>(context, connectionFactory), IPageFavouriteRepository
{
    public async Task Add(int pageId, string userId, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var command = new CommandDefinition(
            SqlScripts.InsertPageFavourite,
            new { PageId = pageId, UserId = userId },
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    public Task<int> Remove(int pageId, string userId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(favourite => favourite.PageId == pageId && favourite.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
