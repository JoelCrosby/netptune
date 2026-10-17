using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public class ScmWebhookDeliveryRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, ScmWebhookDelivery, long>(context, connectionFactory), IScmWebhookDeliveryRepository
{
    // One statement, so two concurrent redeliveries of the same webhook can't both insert.
    public async Task<long?> AddIfNew(ScmWebhookDelivery delivery, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var parameters = new
        {
            Provider = (int)delivery.Provider,
            delivery.DeliveryId,
            delivery.EventName,
            delivery.Action,
            delivery.ExternalAccountId,
            delivery.Body,
            delivery.ReceivedAt,
        };

        var command = new CommandDefinition(
            SqlScripts.InsertScmWebhookDelivery,
            parameters,
            cancellationToken: cancellationToken);
        var insertedId = await connection.QuerySingleOrDefaultAsync<long?>(command);

        return insertedId;
    }

    public Task<List<long>> GetStalled(
        DateTime receivedBefore,
        int maxAttempts,
        int take,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(delivery => delivery.ProcessedAt == null && delivery.ReceivedAt < receivedBefore)
            .Where(delivery => delivery.Attempts < maxAttempts)
            .OrderBy(delivery => delivery.ReceivedAt)
            .Select(delivery => delivery.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> DeleteProcessedBefore(DateTime processedBefore, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(delivery => delivery.ProcessedAt != null && delivery.ProcessedAt < processedBefore)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
