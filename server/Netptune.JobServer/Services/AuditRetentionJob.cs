using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Entities.Contexts;

namespace Netptune.JobServer.Services;

public sealed class AuditRetentionJob : BackgroundService
{
    private readonly IServiceScopeFactory ScopeFactory;
    private readonly ILogger<AuditRetentionJob> Logger;

    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(365);
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);

    public AuditRetentionJob(IServiceScopeFactory scopeFactory, ILogger<AuditRetentionJob> logger)
    {
        ScopeFactory = scopeFactory;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Stagger start to avoid hammering the DB at startup
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogError(ex, "AuditRetentionJob failed");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.Subtract(RetentionPeriod);

        using var scope = ScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        var expiredLogs = await db.ActivityLogs
            .AsNoTracking()
            .Where(x => x.OccurredAt < cutoff)
            .ToListAsync(cancellationToken);

        if (expiredLogs.Count == 0)
        {
            Logger.LogDebug("AuditRetentionJob: nothing to archive");
            return;
        }

        Logger.LogInformation("AuditRetentionJob: archiving {Count} rows older than {Cutoff}", expiredLogs.Count, cutoff);

        var archiveKey = $"audit-archive/{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.ndjson";
        var ndjson = SerializeToNdjson(expiredLogs);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjson));
        var uploaded = await storage.UploadFileAsync(stream, archiveKey, archiveKey);

        if (!uploaded.IsSuccess)
        {
            Logger.LogError("AuditRetentionJob: S3 upload failed, skipping deletion");
            return;
        }

        // Hard-delete after successful archive — bypass the immutability interceptor via bulk delete
        var deleted = await db.ActivityLogs
            .Where(x => x.OccurredAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        Logger.LogInformation("AuditRetentionJob: archived and deleted {Count} rows", deleted);
    }

    private static string SerializeToNdjson(IEnumerable<ActivityLog> logs)
    {
        var sb = new StringBuilder();

        foreach (var log in logs)
        {
            sb.AppendLine(JsonSerializer.Serialize(new
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

        return sb.ToString();
    }
}
