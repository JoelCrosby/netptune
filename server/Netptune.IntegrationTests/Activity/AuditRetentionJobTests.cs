using System.Text.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Netptune.Activity.Services;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Activity;

public class AuditRetentionJobTests(AuditRetentionFixture fixture) : IClassFixture<AuditRetentionFixture>
{
    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Run_ShouldArchiveEverythingExpired_AndExit()
    {
        await SeedExpiredLogs(count: 4);

        var job = new AuditRetentionJob(fixture.ScopeFactory, NullLogger<AuditRetentionJob>.Instance);

        var before = fixture.Storage.Uploads.Count;

        await job.RunAsync(CancellationToken);

        (fixture.Storage.Uploads.Count - before).Should().Be(1);

        (await CountExpired()).Should().Be(0);
    }

    [Fact]
    public async Task Run_ShouldLeaveLogsInsideTheRetentionWindow()
    {
        await SeedLog(DateTime.UtcNow.AddDays(-10));

        var job = new AuditRetentionJob(fixture.ScopeFactory, NullLogger<AuditRetentionJob>.Instance);

        await job.RunAsync(CancellationToken);

        using var scope = fixture.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        var cutoff = DateTime.UtcNow.AddDays(-365);

        (await db.EventRecords.CountAsync(log => log.OccurredAt >= cutoff, CancellationToken))
            .Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Run_ShouldArchiveAndDeleteEveryRow_WhenThereAreMoreRowsThanOneBatch()
    {
        await SeedExpiredLogs(count: 5);

        var expectedIds = await ExpiredIds();

        expectedIds.Should().HaveCount(5);

        var job = new AuditRetentionJob(fixture.ScopeFactory, NullLogger<AuditRetentionJob>.Instance, batchSize: 2);

        var uploadsBefore = fixture.Storage.Uploads.Count;

        fixture.Storage.ArchivedLines.Clear();

        await job.RunAsync(CancellationToken);

        (fixture.Storage.Uploads.Count - uploadsBefore)
            .Should().Be(3, "five rows at two per batch is three archives, not one");

        (await CountExpired()).Should().Be(0, "every batch that was archived must also have been deleted");

        var archivedIds = fixture.Storage.ArchivedLines
            .Select(line => JsonDocument.Parse(line).RootElement.GetProperty("Id").GetInt32())
            .ToList();

        archivedIds.Should().BeEquivalentTo(expectedIds, "batching must not drop or duplicate a row");
    }

    [Fact]
    public async Task Run_ShouldDeleteNothing_WhenTheUploadFails()
    {
        await SeedExpiredLogs(count: 5);

        var expected = await CountExpired();

        expected.Should().Be(5);

        var job = new AuditRetentionJob(fixture.ScopeFactory, NullLogger<AuditRetentionJob>.Instance, batchSize: 2);

        using (fixture.Storage.FailUploads())
        {
            await job.RunAsync(CancellationToken);
        }

        (await CountExpired()).Should().Be(5, "an unarchived row must survive — it is the only copy left");

        await job.RunAsync(CancellationToken);

        (await CountExpired()).Should().Be(0);
    }

    private async Task<List<long>> ExpiredIds()
    {
        using var scope = fixture.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        var cutoff = DateTime.UtcNow.AddDays(-365);

        var eventRecordIds = await db.EventRecords
            .Where(log => log.OccurredAt < cutoff)
            .Select(log => log.Id)
            .ToListAsync(CancellationToken);

        return eventRecordIds;
    }

    private async Task<int> CountExpired()
    {
        using var scope = fixture.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        var cutoff = DateTime.UtcNow.AddDays(-365);

        var count = await db.EventRecords.CountAsync(log => log.OccurredAt < cutoff, CancellationToken);

        return count;
    }

    private async Task SeedExpiredLogs(int count)
    {
        for (var index = 0; index < count; index++)
        {
            await SeedLog(DateTime.UtcNow.AddDays(-500 - index));
        }
    }

    private async Task SeedLog(DateTime occurredAt)
    {
        using var scope = fixture.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        db.EventRecords.Add(new EventRecord
        {
            EventId = Guid.NewGuid(),
            ActorUserId = fixture.UserId,
            WorkspaceId = fixture.WorkspaceId,
            EventKey = EventKeys.EntityActivityRecorded,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = "1",
            OccurredAt = occurredAt,
            RecordedAt = occurredAt,
            RetentionClass = EventRetentionClasses.Audit,
            Payload = JsonSerializer.SerializeToDocument(new { activityType = (int)ActivityType.Modify }),
        });

        await db.SaveChangesAsync(CancellationToken);
    }
}
