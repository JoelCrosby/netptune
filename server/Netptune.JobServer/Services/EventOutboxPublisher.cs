using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Events;
using Netptune.Core.Services.Activity;
using Netptune.Entities.Contexts;

namespace Netptune.JobServer.Services;

public sealed class EventOutboxPublisher : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory ScopeFactory;
    private readonly ILogger<EventOutboxPublisher> Logger;

    public EventOutboxPublisher(IServiceScopeFactory scopeFactory, ILogger<EventOutboxPublisher> logger)
    {
        ScopeFactory = scopeFactory;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var claimed = await ClaimBatch(stoppingToken);

                if (claimed.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                foreach (var claim in claimed)
                {
                    await Publish(claim, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "The canonical event outbox loop failed");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task<List<OutboxClaim>> ClaimBatch(CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var now = DateTime.UtcNow;
        var leaseId = Guid.NewGuid();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var rows = await context.EventOutbox
            .FromSqlInterpolated($$"""
                SELECT *
                FROM event_outbox
                WHERE available_at <= {{now}}
                  AND dead_lettered_at IS NULL
                  AND (lease_expires_at IS NULL OR lease_expires_at < {{now}})
                ORDER BY available_at, event_record_id
                LIMIT {{BatchSize}}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            row.LeaseId = leaseId;
            row.LeaseExpiresAt = now.Add(LeaseDuration);
            row.AttemptCount++;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return rows.Select(row => new OutboxClaim(row.EventRecordId, leaseId)).ToList();
    }

    private async Task Publish(OutboxClaim claim, CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var outbox = await context.EventOutbox
            .Include(row => row.EventRecord)
            .ThenInclude(record => record.References)
            .SingleOrDefaultAsync(row => row.EventRecordId == claim.EventRecordId && row.LeaseId == claim.LeaseId, cancellationToken);

        if (outbox is null)
        {
            return;
        }

        try
        {
            await publisher.DispatchCanonical(ToEnvelope(outbox.EventRecord), cancellationToken);
            context.EventOutbox.Remove(outbox);
        }
        catch (Exception exception)
        {
            outbox.LastError = exception.Message;
            outbox.LeaseId = null;
            outbox.LeaseExpiresAt = null;

            if (outbox.AttemptCount >= 12)
            {
                outbox.DeadLetteredAt = DateTime.UtcNow;
            }
            else
            {
                outbox.AvailableAt = DateTime.UtcNow.Add(RetryDelay(outbox.AttemptCount));
            }

            Logger.LogWarning(exception, "Publishing canonical event {EventId} failed", outbox.EventRecord.EventId);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static CanonicalEventEnvelope ToEnvelope(EventRecord record) => new()
    {
        EventId = record.EventId,
        EventRecordId = record.Id,
        EventKey = record.EventKey,
        SchemaVersion = record.SchemaVersion,
        WorkspaceId = record.WorkspaceId,
        SubjectType = record.SubjectType,
        SubjectId = record.SubjectId,
        SubjectSequence = record.SubjectSequence,
        OccurredAt = record.OccurredAt,
        RecordedAt = record.RecordedAt,
        ActorUserId = record.ActorUserId,
        CorrelationId = record.CorrelationId,
        CausationEventId = record.CausationEventId,
        RetentionClass = record.RetentionClass,
        Payload = record.Payload.RootElement.Clone(),
        References = record.References
            .Select(reference => new CanonicalEventReference(reference.Role, reference.EntityType, reference.EntityId))
            .ToList(),
    };

    private static TimeSpan RetryDelay(int attempt) =>
        TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, Math.Min(attempt, 8))));

    private sealed record OutboxClaim(long EventRecordId, Guid LeaseId);
}
