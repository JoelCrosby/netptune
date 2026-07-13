using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Activity;
using Netptune.Core.Services.Notifications;
using Netptune.Core.UnitOfWork;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Activity;

public class ActivityMergeTests(ActivityMergeFixture fixture) : IClassFixture<ActivityMergeFixture>
{
    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    #region Merging

    [Fact]
    public async Task Handle_ShouldMergeAutosaves_IntoOneOpenEntry_WithinTheWindow()
    {
        const int entityId = 1001;

        for (var index = 0; index < 12; index++)
        {
            await Handle(Change(entityId, TaskChangeField.Description, $"v{index}", $"v{index + 1}"));
        }

        var entries = await Entries(entityId);

        entries.Should().ContainSingle("twelve autosaves inside the window are one burst");

        var entry = entries[0];

        entry.IsOpen.Should().BeTrue();
        entry.RevisionCount.Should().Be(12, "every one of the twelve autosaves was folded into the burst");
        entry.ChangedFields.Should().Equal("description");
        entry.ActivityType.Should().Be(ActivityType.ModifyDescription);

        FieldValue(entry, "description", "old").Should().Be("v0");
        FieldValue(entry, "description", "new").Should().Be("v12");
    }

    [Fact]
    public async Task Handle_ShouldStillWriteOneLedgerRowPerEvent_AfterAMergedBurst()
    {
        const int entityId = 1002;

        for (var index = 0; index < 20; index++)
        {
            await Handle(Change(entityId, TaskChangeField.Description, $"v{index}", $"v{index + 1}"));
        }

        (await Entries(entityId)).Should().ContainSingle();
        (await LedgerRows(entityId)).Should().Be(20, "every individual autosave stays in the immutable ledger");
    }

    [Fact]
    public async Task Handle_ShouldMergeAcrossFields_AndRenderAsAGenericModify()
    {
        const int entityId = 1003;

        await Handle(Change(entityId, TaskChangeField.Description, "a", "b"));
        await Handle(
            Change(entityId, TaskChangeField.Priority, "None", "High", ActivityType.ModifyPriority),
            Change(entityId, TaskChangeField.Name, "one", "two", ActivityType.ModifyName));

        var entry = (await Entries(entityId)).Should().ContainSingle().Subject;

        entry.ChangedFields.Should().BeEquivalentTo("description", "priority", "name");
        entry.ActivityType.Should().Be(ActivityType.Modify, "a burst spanning several kinds of field is a generic update (§4.1)");
        entry.RevisionCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldOpenANewEntry_WhenTheWindowHasExpired()
    {
        const int entityId = 1004;

        await Handle(Change(entityId, TaskChangeField.Description, "a", "b"));

        await Rewind(entityId, TimeSpan.FromMinutes(6));

        await Handle(Change(entityId, TaskChangeField.Description, "b", "c"));

        var entries = await Entries(entityId);

        entries.Should().HaveCount(2, "an expired burst cannot absorb new events, even before the sweeper reaches it");
        entries.Count(entry => entry.IsOpen).Should().Be(1, "the stale entry is closed to free the unique index slot");

        var stale = entries.Single(entry => !entry.IsOpen);

        stale.NotifiedAt.Should().BeNull("the sweeper still owes it its notifications — closing must never drop them");
    }

    [Fact]
    public async Task Handle_ShouldOpenANewEntry_WhenAnotherUserActsOnTheEntity()
    {
        const int entityId = 1005;

        await Handle(Change(entityId, TaskChangeField.Description, "a", "b"));

        await Handle(Change(entityId, TaskChangeField.Status, "Todo", "Done", ActivityType.ModifyStatus, fixture.OtherUserId));

        await Handle(Change(entityId, TaskChangeField.Description, "b", "c"));

        var entries = await Entries(entityId);

        entries.Should().HaveCount(3);

        entries.Where(entry => entry.UserId == fixture.ActorUserId).Should().HaveCount(2, "the actor's burst was ended by the other user");
        entries.Count(entry => entry.UserId == fixture.OtherUserId).Should().Be(1);

        var ended = entries
            .Where(entry => entry.UserId == fixture.ActorUserId)
            .OrderBy(entry => entry.Id)
            .First();

        ended.NotifiedAt.Should().BeNull("ending a burst hands it to the sweeper — it does not silently drop it");
    }

    [Fact]
    public async Task Handle_ShouldNeverMergeDiscreteTypes()
    {
        const int entityId = 1006;

        await Handle(Discrete(entityId, ActivityType.Mention));
        await Handle(Discrete(entityId, ActivityType.Assign));
        await Handle(Discrete(entityId, ActivityType.AddTag));

        var entries = await Entries(entityId);

        entries.Should().HaveCount(3, "a discrete event is always its own entry");
        entries.Should().OnlyContain(entry => !entry.IsOpen, "a discrete entry is complete on arrival and never holds the open slot");
        entries.Should().OnlyContain(entry => entry.NotifiedAt != null, "it has already been notified, so the sweeper must skip it");
        entries.Select(entry => entry.RevisionCount).Should().AllBeEquivalentTo(1);
    }


    #endregion

    #region Window

    [Fact]
    public async Task Handle_ShouldSlideTheWindow_OnEveryMerge()
    {
        const int entityId = 1008;

        await Handle(Change(entityId, TaskChangeField.Description, "a", "b"));

        var first = (await Entries(entityId)).Single().WindowExpiresAt;

        await Rewind(entityId, TimeSpan.FromMinutes(2));

        await Handle(Change(entityId, TaskChangeField.Description, "b", "c"));

        var second = (await Entries(entityId)).Single().WindowExpiresAt;

        second.Should().BeAfter(first, "each merge pushes the window out by another WindowDuration");
    }

    [Fact]
    public async Task Handle_ShouldNeverSlideTheWindowPast_FirstOccurredAtPlusMaxWindow()
    {
        const int entityId = 1009;

        // Unbroken editing: an edit every two minutes, so the window slides every time and never lapses. One
        // 28 minute rewind would be a gap instead, and would expire the burst rather than stretch it.
        await Handle(Change(entityId, TaskChangeField.Description, "v0", "v1"));

        for (var index = 1; index <= 14; index++)
        {
            await Rewind(entityId, TimeSpan.FromMinutes(2));

            await Handle(Change(entityId, TaskChangeField.Description, $"v{index}", $"v{index + 1}"));
        }

        var entry = (await Entries(entityId)).Should().ContainSingle().Subject;

        entry.RevisionCount.Should().Be(15, "the window slid on every one of them");

        entry.WindowExpiresAt
            .Should().BeCloseTo(entry.FirstOccurredAt.AddMinutes(30), TimeSpan.FromSeconds(10))
            .And.BeBefore(DateTime.UtcNow.AddMinutes(5), "the cap beat the slide — a five minute slide from now would land later than this");
    }

    [Fact]
    public async Task Handle_ShouldYieldTwoEntries_ForAnUnbrokenHourOfEditing()
    {
        const int entityId = 1010;

        for (var index = 0; index < 30; index++)
        {
            if (index > 0) await Rewind(entityId, TimeSpan.FromMinutes(2));

            await Handle(Change(entityId, TaskChangeField.Description, $"v{index}", $"v{index + 1}"));
        }

        var entries = await Entries(entityId);

        entries.Should().HaveCount(2, "the 30 minute cap splits the hour, and nothing else does");
        (await LedgerRows(entityId)).Should().Be(30);

        entries.Sum(entry => entry.RevisionCount).Should().Be(30, "every event is accounted for in one entry or the other");
    }

    #endregion

    #region Concurrency

    [Fact]
    public async Task Handle_ShouldProduceOneOpenEntry_WhenReplicasRaceOnTheSameTask()
    {
        const int entityId = 1011;
        const int replicas = 8;

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var races = Enumerable.Range(0, replicas).Select(async index =>
        {
            var (scope, handler) = fixture.CreateHandler();

            using (scope)
            {
                await gate.Task;

                await handler.Handle(
                    new ActivityMessage(Change(entityId, TaskChangeField.Description, $"v{index}", $"v{index + 1}")),
                    CancellationToken);
            }
        }).ToList();

        gate.SetResult();

        await Task.WhenAll(races);

        var entries = await Entries(entityId);

        entries.Count(entry => entry.IsOpen).Should().Be(1, "two replicas racing must merge, not duplicate");
        entries.Should().ContainSingle();

        entries[0].RevisionCount.Should().Be(replicas, "and no replica's event may be lost in the race");
        (await LedgerRows(entityId)).Should().Be(replicas);
    }

    #endregion

    #region Redelivery

    [Fact]
    public async Task Handle_ShouldNotLoseTheEntry_WhenTheProjectionCrashesAfterTheLedgerWrite_AndTheMessageIsRedelivered()
    {
        const int entityId = 1301;

        var message = new[]
        {
            Change(entityId, TaskChangeField.Description, "a", "b"),
            Change(entityId, TaskChangeField.Description, "b", "c"),
        };

        await FailOnceInserting("activity_entries", $"NEW.entity_id = {entityId}", entityId, async () =>
        {
            var crashed = async () => await Handle(message, null);

            await crashed.Should().ThrowAsync<Exception>("the projection insert is killed mid-message");

            await Handle(message, null);
        });

        (await LedgerRows(entityId)).Should().Be(2, "the rolled-back ledger rows are written once, by the redelivery");

        var entry = (await Entries(entityId, includeDeleted: true)).Should().ContainSingle(
            "the redelivery must rebuild the feed entry the crash rolled back").Subject;

        entry.RevisionCount.Should().Be(2, "and it holds the burst exactly once — no event lost, none counted twice");

        await Rewind(entityId, TimeSpan.FromMinutes(6));
        await fixture.CreateSweeper().RunAsync(CancellationToken);

        (await NotificationsFor(entityId)).Should().HaveCount(2, "the burst still notifies both recipients");
    }

    [Fact]
    public async Task Handle_ShouldNotLoseNotifications_WhenTheFanOutCrashesAfterTheProjection_AndTheMessageIsRedelivered()
    {
        const int entityId = 1302;

        var message = new[]
        {
            Change(entityId, TaskChangeField.Description, "a", "b"),
            Change(entityId, TaskChangeField.Description, "b", "c"),
            Discrete(entityId, ActivityType.Restore),
        };

        var restore = (int) ActivityType.Restore;

        await FailOnceInserting("notifications", $"NEW.activity_type = {restore}", entityId, async () =>
        {
            var crashed = async () => await Handle(message, null);

            await crashed.Should().ThrowAsync<Exception>("the notification fan-out is killed mid-message");

            await Handle(message, null);
        });

        (await LedgerRows(entityId)).Should().Be(3);

        var entries = await Entries(entityId, includeDeleted: true);

        entries.Should().HaveCount(2, "one merged burst and the discrete Restore");

        var merged = entries.Single(entry => entry.ActivityType == ActivityType.ModifyDescription);

        merged.RevisionCount.Should().Be(2,
            "the crashed attempt's upsert rolled back with everything else — a second connection would have left it at 4");

        var notifications = await AllNotifications(entityId);

        notifications.Should().HaveCount(2, "the discrete Restore notifies both recipients, and the redelivery is what does it");
        notifications.Should().OnlyContain(notification => notification.ActivityType == ActivityType.Restore);
    }

    #endregion

    #region Sweeper

    [Fact]
    public async Task Sweep_ShouldClaimOnlyExpiredEntries()
    {
        const int liveEntityId = 1012;
        const int expiredEntityId = 1013;

        await Handle(Change(liveEntityId, TaskChangeField.Description, "a", "b"));
        await Handle(Change(expiredEntityId, TaskChangeField.Description, "a", "b"));

        await Rewind(expiredEntityId, TimeSpan.FromMinutes(6));

        await fixture.CreateSweeper().RunAsync(CancellationToken);

        var live = (await Entries(liveEntityId)).Single();
        var expired = (await Entries(expiredEntityId)).Single();

        live.IsOpen.Should().BeTrue("a burst still inside its window is still accepting merges");
        live.NotifiedAt.Should().BeNull();

        expired.IsOpen.Should().BeFalse();
        expired.NotifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Sweep_ShouldFanOutExactlyOneNotificationPerRecipient_PerEntry()
    {
        const int entityId = 1014;

        for (var index = 0; index < 15; index++)
        {
            await Handle(Change(entityId, TaskChangeField.Description, $"v{index}", $"v{index + 1}"));
        }

        (await NotificationCount(entityId)).Should().Be(0, "an open entry is not yet notifiable — the burst has not finished");

        await Rewind(entityId, TimeSpan.FromMinutes(6));

        await fixture.CreateSweeper().RunAsync(CancellationToken);

        var entry = (await Entries(entityId)).Single();

        var notifications = await NotificationsFor(entityId);

        notifications.Should().HaveCount(2);
        notifications.Select(notification => notification.UserId)
            .Should().BeEquivalentTo([fixture.OtherUserId, fixture.ThirdUserId]);

        var lastLogId = await LastLedgerRowId(entityId);

        entry.LastActivityLogId.Should().Be(lastLogId);

        notifications.Should().OnlyContain(notification => notification.ActivityEntryId == entry.Id);
        notifications.Should().OnlyContain(notification => notification.ActivityLogId == lastLogId,
            "ActivityLogId stays non-nullable and points at the last ledger row folded into the entry");
    }

    [Fact]
    public async Task Sweep_ShouldNotifyOnce_HoweverManyTimesItRuns()
    {
        const int entityId = 1015;

        await Handle(Change(entityId, TaskChangeField.Description, "a", "b"));
        await Rewind(entityId, TimeSpan.FromMinutes(6));

        for (var tick = 0; tick < 5; tick++)
        {
            await fixture.CreateSweeper().RunAsync(CancellationToken);
        }

        (await NotificationsFor(entityId)).Should().HaveCount(2, "one per recipient, however many ticks ran");
    }

    [Fact]
    public async Task Sweep_ShouldClaimEachEntryOnce_WhenSixReplicasSweepABacklogTogether()
    {
        const int firstEntityId = 1100;
        const int entries = 40;
        const int replicas = 6;

        for (var index = 0; index < entries; index++)
        {
            await Handle(Change(firstEntityId + index, TaskChangeField.Description, "a", "b"));
        }

        for (var index = 0; index < entries; index++)
        {
            await Rewind(firstEntityId + index, TimeSpan.FromMinutes(6));
        }

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scopes = new List<IServiceScope>();

        // The scopes, units of work and pooled connections are built before the gate on purpose. Build them
        // inside the Task.Run below and the replicas trickle into the claim one at a time — a claim with no
        // atomicity at all then passes this test. The backlog above serves the same purpose.
        var sweeps = Enumerable.Range(0, replicas).Select(_ =>
        {
            var scope = fixture.CreateScope();

            scopes.Add(scope);

            var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
            var events = scope.ServiceProvider.GetRequiredService<INotificationEventPublisher>();

            var sweeper = fixture.CreateSweeper();

            return Task.Run(async () =>
            {
                await gate.Task;

                await sweeper.SweepAsync(unitOfWork, events, CancellationToken);
            });
        }).ToList();

        gate.SetResult();

        await Task.WhenAll(sweeps);

        foreach (var scope in scopes) scope.Dispose();

        for (var index = 0; index < entries; index++)
        {
            (await NotificationsFor(firstEntityId + index))
                .Should().HaveCount(2, "entry {0} must be claimed, and notified, exactly once", firstEntityId + index);
        }
    }

    [Fact]
    public async Task Sweep_ShouldDiscardANoOpBurst_AndNotifyNobody()
    {
        const int entityId = 1016;

        await Handle(Change(entityId, TaskChangeField.Description, "original", "original typo"));
        await Handle(Change(entityId, TaskChangeField.Description, "original typo", "original typoo"));
        await Handle(Change(entityId, TaskChangeField.Description, "original typoo", "original"));

        await Rewind(entityId, TimeSpan.FromMinutes(6));

        await fixture.CreateSweeper().RunAsync(CancellationToken);

        var entries = await Entries(entityId, includeDeleted: true);

        entries.Should().ContainSingle();

        var entry = entries[0];

        entry.IsDeleted.Should().BeTrue("the burst ended where it started, so there is nothing to show");
        entry.IsOpen.Should().BeFalse("a discarded entry that stayed open would hold the unique index slot forever and wedge the entity");

        (await NotificationsFor(entityId)).Should().BeEmpty();
        (await NotificationCount(entityId)).Should().Be(0);

        (await LedgerRows(entityId)).Should().Be(3, "the audit trail still shows exactly what happened");
    }

    [Fact]
    public async Task Sweep_ShouldNotDiscardALongEdit_ThatOnlyLooksLikeANoOpAfterTruncation()
    {
        const int entityId = 1017;

        var prefix = new string('x', ActivityValue.MaxLength);

        var before = prefix + " the original fifth paragraph";
        var after = prefix + " a completely rewritten fifth paragraph";

        await Handle(Change(entityId, TaskChangeField.Description, before, after, hash: true));

        await Rewind(entityId, TimeSpan.FromMinutes(6));

        await fixture.CreateSweeper().RunAsync(CancellationToken);

        var entry = (await Entries(entityId, includeDeleted: true)).Single();

        FieldValue(entry, "description", "old").Should().Be(FieldValue(entry, "description", "new"),
            "the stored prefixes are identical — this is exactly the trap");

        entry.IsDeleted.Should().BeFalse("the hashes of the full values differ, so this is a real change");

        (await NotificationsFor(entityId)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldNotifyImmediately_WhenAMentionLandsMidBurst()
    {
        const int entityId = 1018;

        await Handle(Change(entityId, TaskChangeField.Description, "a", "b"));
        await Handle(Change(entityId, TaskChangeField.Description, "b", "c"));

        (await NotificationCount(entityId)).Should().Be(0, "the field edits are deferred until the window closes");

        await Handle(Discrete(entityId, ActivityType.Mention));
        await Handle(Discrete(entityId, ActivityType.Assign));

        var notifications = await AllNotifications(entityId);

        notifications.Should().HaveCount(4, "two recipients × the mention and the assign, and nothing else");
        notifications.Select(notification => notification.ActivityType)
            .Should().BeEquivalentTo([ActivityType.Mention, ActivityType.Mention, ActivityType.Assign, ActivityType.Assign]);

        (await Entries(entityId)).Count(entry => entry.IsOpen).Should().Be(1);
    }

    #endregion

    #region SoftDelete

    [Fact]
    public async Task Handle_ShouldOpenAFreshEntry_WhenASoftDeletedEntryStillHoldsTheSlotOpen()
    {
        const int entityId = 1303;

        await Handle(Change(entityId, TaskChangeField.Description, "a", "b"));

        await ExecuteSql($"UPDATE activity_entries SET is_deleted = TRUE WHERE entity_id = {entityId}");

        await Handle(Change(entityId, TaskChangeField.Description, "b", "c"));

        var visible = await Entries(entityId);

        visible.Should().ContainSingle("the edit must land in an entry the feed can actually see");
        visible[0].RevisionCount.Should().Be(1, "it is a fresh burst, not a merge into a row nobody can read");
        visible[0].IsOpen.Should().BeTrue();

        var all = await Entries(entityId, includeDeleted: true);

        all.Should().HaveCount(2);
        all.Count(entry => entry.IsOpen && !entry.IsDeleted).Should().Be(1, "the unique partial index still admits exactly one");
    }

    #endregion

    #region Helpers

    private ActivityEvent Change(
        int entityId,
        TaskChangeField field,
        string? oldValue,
        string? newValue,
        ActivityType type = ActivityType.ModifyDescription,
        string? userId = null,
        bool hash = false)
    {
        return new ()
        {
            EventId = Guid.NewGuid(),
            Type = type,
            EntityType = EntityType.Task,
            EntityId = entityId,
            WorkspaceId = fixture.WorkspaceId,
            UserId = userId ?? fixture.ActorUserId,
            OccurredAt = DateTime.UtcNow,
            Field = field,
            OldValue = ActivityValue.Truncate(oldValue),
            NewValue = ActivityValue.Truncate(newValue),
            OldValueHash = hash ? ActivityValue.HashIfTruncated(oldValue) : null,
            NewValueHash = hash ? ActivityValue.HashIfTruncated(newValue) : null,
        };
    }

    private ActivityEvent Discrete(int entityId, ActivityType type, string? userId = null)
    {
        return new ()
        {
            EventId = Guid.NewGuid(),
            Type = type,
            EntityType = EntityType.Task,
            EntityId = entityId,
            WorkspaceId = fixture.WorkspaceId,
            UserId = userId ?? fixture.ActorUserId,
            OccurredAt = DateTime.UtcNow,
        };
    }

    private Task Handle(params ActivityEvent[] events) => Handle(events, null);

    private async Task Handle(ActivityEvent[] events, ActivityMergeOptions? merge)
    {
        var (scope, handler) = fixture.CreateHandler(merge);

        using (scope)
        {
            await handler.Handle(new ActivityMessage(events), CancellationToken);
        }
    }

    private async Task ExecuteSql(string sql)
    {
        using var scope = fixture.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        await db.Database.ExecuteSqlRawAsync(sql, CancellationToken);
    }

    // Kills the first INSERT into `table` matching `when`, and only the first. nextval is deliberate: it is
    // non-transactional, so it keeps counting through the rollback the RAISE causes and the redelivery gets
    // through. A counter table would be rolled back with everything else and every attempt would die.
    private async Task FailOnceInserting(string table, string when, int id, Func<Task> act)
    {
        await ExecuteSql(
            $"""
             CREATE SEQUENCE crash_seq_{id};

             CREATE FUNCTION crash_fn_{id}() RETURNS trigger LANGUAGE plpgsql AS $$
             BEGIN
                 IF nextval('crash_seq_{id}') = 1 THEN
                     RAISE EXCEPTION 'simulated pod kill';
                 END IF;

                 RETURN NEW;
             END;
             $$;

             CREATE TRIGGER crash_trg_{id} BEFORE INSERT ON {table}
             FOR EACH ROW WHEN ({when}) EXECUTE FUNCTION crash_fn_{id}();
             """);

        try
        {
            await act();
        }
        finally
        {
            await ExecuteSql(
                $"""
                 DROP TRIGGER IF EXISTS crash_trg_{id} ON {table};
                 DROP FUNCTION IF EXISTS crash_fn_{id}();
                 DROP SEQUENCE IF EXISTS crash_seq_{id};
                 """);
        }
    }

    // Moves an entity's open entries backwards in time — the same thing as the world moving forwards.
    private async Task Rewind(int entityId, TimeSpan by)
    {
        using var scope = fixture.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE activity_entries
            SET first_occurred_at = first_occurred_at - CAST({1} AS interval)
              , last_occurred_at  = last_occurred_at  - CAST({1} AS interval)
              , window_expires_at = window_expires_at - CAST({1} AS interval)
            WHERE entity_id = {0} AND is_open
            """,
            [entityId, $"{by.TotalSeconds} seconds"],
            CancellationToken);
    }

    private async Task<List<ActivityEntry>> Entries(int entityId, bool includeDeleted = false)
    {
        using var scope = fixture.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await db.ActivityEntries
            .AsNoTracking()
            .Where(entry => entry.EntityId == entityId && (includeDeleted || !entry.IsDeleted))
            .OrderBy(entry => entry.Id)
            .ToListAsync(CancellationToken);
    }

    private async Task<int> LedgerRows(int entityId)
    {
        using var scope = fixture.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await db.ActivityLogs.CountAsync(log => log.EntityId == entityId, CancellationToken);
    }

    private async Task<int> LastLedgerRowId(int entityId)
    {
        using var scope = fixture.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await db.ActivityLogs
            .Where(log => log.EntityId == entityId)
            .MaxAsync(log => log.Id, CancellationToken);
    }

    private async Task<List<Notification>> NotificationsFor(int entityId)
    {
        var entryIds = (await Entries(entityId, includeDeleted: true)).Select(entry => entry.Id).ToList();

        using var scope = fixture.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await db.Notifications
            .AsNoTracking()
            .Where(notification => notification.ActivityEntryId != null && entryIds.Contains(notification.ActivityEntryId.Value))
            .ToListAsync(CancellationToken);
    }

    private async Task<List<Notification>> AllNotifications(int entityId)
    {
        using var scope = fixture.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await db.Notifications
            .AsNoTracking()
            .Join(db.ActivityLogs, notification => notification.ActivityLogId, log => log.Id, (notification, log) => new { notification, log })
            .Where(row => row.log.EntityId == entityId)
            .Select(row => row.notification)
            .ToListAsync(CancellationToken);
    }

    private async Task<int> NotificationCount(int entityId) => (await AllNotifications(entityId)).Count;

    private static string? FieldValue(ActivityEntry entry, string field, string key)
    {
        var value = entry.Meta!.RootElement
            .GetProperty("fields")
            .GetProperty(field)
            .GetProperty(key);

        return value.ValueKind is System.Text.Json.JsonValueKind.String ? value.GetString() : null;
    }

    #endregion
}
