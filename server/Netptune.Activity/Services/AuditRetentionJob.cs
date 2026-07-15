using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Entities.Contexts;

namespace Netptune.Activity.Services;

// Archives expired audit ledger rows to S3 and deletes them. Runs once and returns; it is scheduled by the
// retention CronJob in the chart (`--job retention`), whose concurrencyPolicy: Forbid is the only thing
// keeping two runs from overlapping.
public sealed class AuditRetentionJob
{
    private const int DefaultBatchSize = 500;

    private readonly IServiceScopeFactory ScopeFactory;
    private readonly ILogger<AuditRetentionJob> Logger;
    private readonly int BatchSize;

    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(365);

    public AuditRetentionJob(IServiceScopeFactory scopeFactory, ILogger<AuditRetentionJob> logger)
        : this(scopeFactory, logger, DefaultBatchSize)
    {
    }

    internal AuditRetentionJob(IServiceScopeFactory scopeFactory, ILogger<AuditRetentionJob> logger, int batchSize)
    {
        ScopeFactory = scopeFactory;
        Logger = logger;
        BatchSize = batchSize;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.Subtract(RetentionPeriod);

        using var scope = ScopeFactory.CreateScope();

        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        await ArchiveAsync(db, storage, cutoff, cancellationToken);
    }

    private async Task ArchiveAsync(
        DataContext db,
        IStorageService storage,
        DateTime cutoff,
        CancellationToken cancellationToken)
    {
        var runId = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");
        var batchNumber = 0;
        var archived = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var batch = await db.ActivityLogs
                .AsNoTracking()
                .Where(x => x.OccurredAt < cutoff)
                .OrderBy(x => x.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0) break;

            batchNumber++;

            var archiveKey = $"audit-archive/{runId}/{batchNumber:D6}.ndjson";

            using var stream = WriteNdjson(batch);

            var uploadOptions = new StorageUploadOptions
            {
                Name = archiveKey,
                Key = archiveKey,
                ContentType = "application/x-ndjson",
            };
            var uploaded = await storage.UploadFileAsync(stream, uploadOptions, cancellationToken);

            if (!uploaded.IsSuccess)
            {
                Logger.LogError(
                    "AuditRetentionJob: upload of {ArchiveKey} failed, leaving {Count} rows in place",
                    archiveKey,
                    batch.Count);

                break;
            }

            var ids = batch.Select(x => x.Id).ToList();

            // Deletes exactly what the upload above carried, and bypasses AuditLogImmutabilityInterceptor —
            // both deliberate. A row must never be deleted unless the archive holding it reached S3.
            var deleted = await db.ActivityLogs
                .Where(x => ids.Contains(x.Id))
                .ExecuteDeleteAsync(cancellationToken);

            if (deleted == 0)
            {
                Logger.LogError("AuditRetentionJob: batch {BatchNumber} archived but deleted no rows, aborting", batchNumber);

                break;
            }

            archived += deleted;
        }

        if (archived == 0)
        {
            Logger.LogInformation("AuditRetentionJob: nothing to archive");
            return;
        }

        Logger.LogInformation(
            "AuditRetentionJob: archived and deleted {Count} rows older than {Cutoff} across {BatchCount} batches",
            archived,
            cutoff,
            batchNumber);
    }

    private static MemoryStream WriteNdjson(IEnumerable<ActivityLog> logs)
    {
        var stream = new MemoryStream();

        using (var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true))
        {
            foreach (var log in logs)
            {
                writer.WriteLine(JsonSerializer.Serialize(new
                {
                    log.Id,
                    log.OccurredAt,
                    log.UserId,
                    log.WorkspaceId,
                    log.WorkspaceSlug,
                    log.EntityType,
                    log.Type,
                    log.EntityId,
                    log.ProjectId,
                    log.ProjectSlug,
                    log.BoardId,
                    log.BoardSlug,
                    log.BoardGroupId,
                    log.TaskId,
                    Meta = log.Meta?.RootElement.ToString(),
                }));
            }
        }

        stream.Position = 0;

        return stream;
    }
}
