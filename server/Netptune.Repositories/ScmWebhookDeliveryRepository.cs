using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.SourceControl;
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

    public Task RecordFailure(long id, string error, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(delivery => delivery.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(delivery => delivery.Attempts, delivery => delivery.Attempts + 1)
                    .SetProperty(delivery => delivery.LastError, error),
                cancellationToken);
    }

    public Task<int> DeleteAbandonedBefore(
        DateTime receivedBefore,
        int maxAttempts,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(delivery => delivery.ProcessedAt == null && delivery.Attempts >= maxAttempts)
            .Where(delivery => delivery.ReceivedAt < receivedBefore)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<long?> ReviveUnprocessed(
        SourceControlProvider provider,
        string deliveryId,
        CancellationToken cancellationToken = default)
    {
        var unprocessedId = await Entities
            .Where(delivery => delivery.Provider == provider && delivery.DeliveryId == deliveryId)
            .Where(delivery => delivery.ProcessedAt == null)
            .Select(delivery => (long?)delivery.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (unprocessedId is null)
        {
            return null;
        }

        await Entities
            .Where(delivery => delivery.Id == unprocessedId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(delivery => delivery.Attempts, 0)
                    .SetProperty(delivery => delivery.LastError, (string?)null),
                cancellationToken);

        return unprocessedId;
    }

    public Task<int> DeleteProcessedBefore(DateTime processedBefore, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(delivery => delivery.ProcessedAt != null && delivery.ProcessedAt < processedBefore)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
