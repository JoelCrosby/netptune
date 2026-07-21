using System.Text.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Entities;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Audit;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Activity;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Activity;

public class ActivityFeedTests(ActivityFeedFixture fixture) : IClassFixture<ActivityFeedFixture>
{
    private static int NextEntityId = 1000;

    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static readonly DateTime Base = new(
        2026,
        3,
        1,
        9,
        0,
        0,
        DateTimeKind.Utc);

    [Fact]
    public async Task GetActivities_ShouldRenderMergedEntry_WithChangedFieldsRevisionCountAndFirstTime()
    {
        var entityId = Interlocked.Increment(ref NextEntityId);

        using var scope = NewScope(out var db);

        db.ActivityEntries.Add(NewEntry(new ActivityEntryInput
        {
            EntityId = entityId,
            UserId = fixture.UserId,
            Type = ActivityType.Modify,
            ChangedFields = ["description", "priority"],
            First = Base,
            Last = Base.AddMinutes(4),
            Revisions = 12,
            Meta = Meta(("description", "before", "after"), ("priority", "None", "High")),
        }));

        await db.SaveChangesAsync(CancellationToken);

        var activities = await fixture.CreateRepository(db)
            .GetActivities(EntityType.Task, entityId, CancellationToken);

        activities.Should().HaveCount(1);

        var activity = activities[0];

        activity.Type.Should().Be(ActivityType.Modify);
        activity.ChangedFields.Should().Equal("description", "priority");
        activity.RevisionCount.Should().Be(12);
        activity.FirstTime.Should().Be(Base);
        activity.Time.Should().Be(Base.AddMinutes(4));
        activity.UserUsername.Should().Be("Feed Reader");
    }

    [Fact]
    public async Task GetActivities_ShouldKeepSpecificType_WhenOnlyOneFieldChanged()
    {
        var entityId = Interlocked.Increment(ref NextEntityId);

        using var scope = NewScope(out var db);

        db.ActivityEntries.Add(NewEntry(new ActivityEntryInput
        {
            EntityId = entityId,
            UserId = fixture.UserId,
            Type = ActivityType.ModifyDescription,
            ChangedFields = ["description"],
            First = Base,
            Last = Base,
            Revisions = 1,
            Meta = Meta(("description", "a", "b")),
        }));

        await db.SaveChangesAsync(CancellationToken);

        var activities = await fixture.CreateRepository(db)
            .GetActivities(EntityType.Task, entityId, CancellationToken);

        activities.Should().HaveCount(1);
        activities[0].Type.Should().Be(ActivityType.ModifyDescription);
        activities[0].RevisionCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAuditLog_ShouldReturnEveryEvent_AfterAMergedBurst()
    {
        var entityId = Interlocked.Increment(ref NextEntityId);

        using var scope = NewScope(out var db);

        var logs = Enumerable.Range(0, 12)
            .Select(i => NewLog(
                entityId,
                fixture.UserId,
                ActivityType.ModifyDescription,
                Base.AddSeconds(i * 20)))
            .ToList();

        db.EventRecords.AddRange(logs);

        db.ActivityEntries.Add(NewEntry(new ActivityEntryInput
        {
            EntityId = entityId,
            UserId = fixture.UserId,
            Type = ActivityType.ModifyDescription,
            ChangedFields = ["description"],
            First = Base,
            Last = Base.AddSeconds(11 * 20),
            Revisions = 12,
            Meta = Meta(("description", "before", "after")),
        }));

        await db.SaveChangesAsync(CancellationToken);

        var repository = fixture.CreateRepository(db);

        var feed = await repository.GetActivities(EntityType.Task, entityId, CancellationToken);

        feed.Should().HaveCount(1);
        feed[0].RevisionCount.Should().Be(12);

        var audit = await repository.GetAuditLog(fixture.WorkspaceId, new AuditLogFilter
        {
            EntityType = EntityType.Task,
            From = Base.AddMinutes(-1),
            To = Base.AddMinutes(30),
            Page = 1,
            PageSize = 100,
        }, CancellationToken);

        audit.Items.Where(item => item.EntityId == entityId).Should().HaveCount(12);

        var summary = await repository.GetActivitySummary(fixture.WorkspaceId, new AuditLogFilter
        {
            From = Base.AddMinutes(-1),
            To = Base.AddMinutes(30),
            Page = 1,
            PageSize = 100,
        }, CancellationToken);

        summary.Sum(point => point.Count).Should().BeGreaterThanOrEqualTo(12);
    }

    [Fact]
    public async Task GetAuditLog_ShouldReturnSummaryAndWorkspaceScopedDetail()
    {
        var entityId = Interlocked.Increment(ref NextEntityId);

        using var scope = NewScope(out var db);

        var oldStatus = new Status
        {
            WorkspaceId = fixture.WorkspaceId,
            EntityType = EntityType.Task,
            Name = "To do",
            Key = $"audit-todo-{entityId}",
            Category = StatusCategory.Todo,
            SortOrder = 1,
        };
        var newStatus = new Status
        {
            WorkspaceId = fixture.WorkspaceId,
            EntityType = EntityType.Task,
            Name = "In progress",
            Key = $"audit-active-{entityId}",
            Category = StatusCategory.Active,
            SortOrder = 2,
        };
        db.Statuses.AddRange(oldStatus, newStatus);

        await db.SaveChangesAsync(CancellationToken);

        var log = new EventRecord
        {
            EventId = Guid.NewGuid(),
            WorkspaceId = fixture.WorkspaceId,
            EventKey = EventKeys.EntityFieldTransitioned,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = entityId.ToString(),
            ActorUserId = fixture.UserId,
            OccurredAt = Base,
            RecordedAt = Base.AddSeconds(1),
            RetentionClass = EventRetentionClasses.Permanent,
            Payload = JsonSerializer.SerializeToDocument(new
            {
                field = "status",
                oldValue = oldStatus.Id.ToString(),
                newValue = newStatus.Id.ToString(),
            }),
        };
        log.References.Add(new EventReference
        {
            Role = EventReferenceRoles.Parent,
            EntityType = EventEntityTypes.From(EntityType.Project),
            EntityId = "42",
        });
        db.EventRecords.Add(log);

        await db.SaveChangesAsync(CancellationToken);

        var repository = fixture.CreateRepository(db);
        var page = await repository.GetAuditLog(
            fixture.WorkspaceId,
            new AuditLogFilter { EntityType = EntityType.Task },
            CancellationToken);
        var detail = await repository.GetAuditLogDetail(fixture.WorkspaceId, log.Id, CancellationToken);
        var otherWorkspaceDetail = await repository.GetAuditLogDetail(
            fixture.WorkspaceId + 1,
            log.Id,
            CancellationToken);

        page.Items.Should().Contain(item => item.Id == log.Id && item.Summary == "Status: To do → In progress");
        detail.Should().NotBeNull();
        detail!.EventId.Should().Be(log.EventId);
        detail.EventKey.Should().Be(EventKeys.EntityFieldTransitioned);
        detail.Meta!.RootElement.GetProperty("newValue").GetString().Should().Be(newStatus.Id.ToString());
        detail.References.Should().ContainSingle(reference =>
            reference.Role == EventReferenceRoles.Parent &&
            reference.EntityType == EventEntityTypes.From(EntityType.Project) &&
            reference.EntityId == "42");
        otherWorkspaceDetail.Should().BeNull();
    }

    [Fact]
    public async Task GetAuditLog_ShouldReturnAndFilterCanonicalAuditActivityTypes()
    {
        using var scope = NewScope(out var db);

        var events = new[]
        {
            NewCanonicalAuditLog(
                EventKeys.SecurityLoginSucceeded,
                new AuthenticationEventPayload { Method = "password", Email = "user@example.com" }),
            NewCanonicalAuditLog(
                EventKeys.SecurityLoginFailed,
                new AuthenticationEventPayload { Method = "password", Email = "user@example.com" }),
            NewCanonicalAuditLog(
                EventKeys.ExportRequested,
                new ExportRequestedPayload { ExportType = "audit-log" }),
            NewCanonicalAuditLog(
                EventKeys.WorkspaceRoleChanged,
                new WorkspaceRoleChangedPayload
                {
                    TargetUserId = fixture.OtherUserId,
                    OldRole = "Member",
                    NewRole = "Viewer",
                }),
            NewCanonicalAuditLog(
                EventKeys.WorkspaceSettingsChanged,
                new WorkspaceSettingsChangedPayload { Fields = ["name"] }),
        };

        db.EventRecords.AddRange(events);

        await db.SaveChangesAsync(CancellationToken);

        var repository = fixture.CreateRepository(db);
        var expected = new[]
        {
            ActivityType.LoginSuccess,
            ActivityType.LoginFailed,
            ActivityType.ExportRequested,
            ActivityType.RoleChanged,
            ActivityType.WorkspaceSettingsChanged,
        };

        foreach (var (eventRecord, activityType) in events.Zip(expected))
        {
            var page = await repository.GetAuditLog(
                fixture.WorkspaceId,
                new AuditLogFilter { ActivityType = activityType },
                CancellationToken);

            page.Items.Should().Contain(item =>
                item.Id == eventRecord.Id &&
                item.Type == activityType &&
                item.Summary != "No additional details");
        }
    }

    [Fact]
    public async Task GetActivities_ShouldReturnEmpty_WhenEntriesTableHasNothingForTheEntity()
    {
        var entityId = Interlocked.Increment(ref NextEntityId);

        using var scope = NewScope(out var db);

        db.EventRecords.AddRange(Enumerable.Range(0, 5)
            .Select(i => NewLog(
                entityId,
                fixture.UserId,
                ActivityType.ModifyDescription,
                Base.AddSeconds(i))));

        await db.SaveChangesAsync(CancellationToken);

        var activities = await fixture.CreateRepository(db)
            .GetActivities(EntityType.Task, entityId, CancellationToken);

        activities.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActivities_ShouldPageWithoutRepeatingOrSkipping_WhenCursorIsFollowed()
    {
        var entityId = Interlocked.Increment(ref NextEntityId);

        using var scope = NewScope(out var db);

        db.ActivityEntries.AddRange(
            NewEntry(new ActivityEntryInput
            {
                EntityId = entityId,
                UserId = fixture.UserId,
                Type = ActivityType.ModifyDescription,
                ChangedFields = ["description"],
                First = Base.AddMinutes(1),
                Last = Base.AddMinutes(1),
                Revisions = 1,
            }),
            NewEntry(new ActivityEntryInput
            {
                EntityId = entityId,
                UserId = fixture.UserId,
                Type = ActivityType.ModifyName,
                ChangedFields = ["name"],
                First = Base.AddMinutes(2),
                Last = Base.AddMinutes(2),
                Revisions = 1,
            }),
            NewEntry(new ActivityEntryInput
            {
                EntityId = entityId,
                UserId = fixture.UserId,
                Type = ActivityType.AddTag,
                ChangedFields = [],
                First = Base.AddMinutes(3),
                Last = Base.AddMinutes(3),
                Revisions = 1,
            }),
            NewEntry(new ActivityEntryInput
            {
                EntityId = entityId,
                UserId = fixture.UserId,
                Type = ActivityType.Assign,
                ChangedFields = [],
                First = Base.AddMinutes(4),
                Last = Base.AddMinutes(4),
                Revisions = 1,
            }),
            NewEntry(new ActivityEntryInput
            {
                EntityId = entityId,
                UserId = fixture.UserId,
                Type = ActivityType.Move,
                ChangedFields = [],
                First = Base.AddMinutes(4),
                Last = Base.AddMinutes(4),
                Revisions = 1,
            }),
            NewEntry(new ActivityEntryInput
            {
                EntityId = entityId,
                UserId = fixture.UserId,
                Type = ActivityType.Create,
                ChangedFields = [],
                First = Base.AddMinutes(6),
                Last = Base.AddMinutes(6),
                Revisions = 1,
            }));

        await db.SaveChangesAsync(CancellationToken);

        var repository = fixture.CreateRepository(db);

        var pages = new List<List<ActivityViewModel>>();
        string? cursor = null;

        for (var page = 0; page < 3; page++)
        {
            var items = await repository.GetActivities(
                EntityType.Task,
                entityId,
                CancellationToken,
                take: 2,
                cursor: cursor);

            items.Should().HaveCount(2);

            pages.Add(items);

            cursor = CursorRequest.Create(items[^1].Time, items[^1].Id);
        }

        (await repository.GetActivities(
            EntityType.Task,
            entityId,
            CancellationToken,
            take: 2,
            cursor: cursor))
            .Should().BeEmpty("six entries read two at a time is exactly three pages");

        var ids = pages.SelectMany(page => page).Select(activity => activity.Id).ToList();

        ids.Should().HaveCount(6);
        ids.Should().OnlyHaveUniqueItems("a page must never repeat an entry the previous one returned");
        var seeded = await db.ActivityEntries
            .Where(entry => entry.EntityId == entityId)
            .Select(entry => entry.Id)
            .ToListAsync(CancellationToken);

        ids.Should().BeEquivalentTo(seeded, "and it must never skip one either");
    }

    private IServiceScope NewScope(out DataContext db)
    {
        var scope = fixture.CreateScope();

        db = scope.ServiceProvider.GetRequiredService<DataContext>();

        return scope;
    }

    private EventRecord NewLog(int entityId, string userId, ActivityType type, DateTime occurredAt) => new()
    {
        EventId = Guid.NewGuid(),
        WorkspaceId = fixture.WorkspaceId,
        EventKey = EventKeys.EntityActivityRecorded,
        SubjectType = EventEntityTypes.From(EntityType.Task),
        SubjectId = entityId.ToString(),
        ActorUserId = userId,
        OccurredAt = occurredAt,
        RecordedAt = occurredAt,
        RetentionClass = EventRetentionClasses.Audit,
        Payload = JsonSerializer.SerializeToDocument(new { activityType = (int)type }),
    };

    private EventRecord NewCanonicalAuditLog<TPayload>(string eventKey, TPayload payload) where TPayload : class
    {
        return new()
        {
            EventId = Guid.NewGuid(),
            WorkspaceId = fixture.WorkspaceId,
            EventKey = eventKey,
            SubjectType = EventEntityTypes.From(EntityType.Workspace),
            SubjectId = fixture.WorkspaceId.ToString(),
            ActorUserId = fixture.UserId,
            OccurredAt = Base.AddHours(1),
            RecordedAt = Base.AddHours(1),
            RetentionClass = EventKeys.RetentionFor(eventKey),
            Payload = JsonSerializer.SerializeToDocument(payload, JsonOptions.Default),
        };
    }

    private ActivityEntry NewEntry(ActivityEntryInput input) => new()
    {
        WorkspaceId = fixture.WorkspaceId,
        EntityType = EntityType.Task,
        EntityId = input.EntityId,
        UserId = input.UserId,
        ActivityType = input.Type,
        ChangedFields = input.ChangedFields,
        Meta = input.Meta,
        FirstOccurredAt = input.First,
        LastOccurredAt = input.Last,
        RevisionCount = input.Revisions,
        IsOpen = false,
        WindowExpiresAt = input.Last.AddMinutes(5),
        NotifiedAt = input.Last,
    };

    private sealed record ActivityEntryInput
    {
        public int EntityId { get; init; }

        public required string UserId { get; init; }

        public ActivityType Type { get; init; }

        public required List<string> ChangedFields { get; init; }

        public DateTime First { get; init; }

        public DateTime Last { get; init; }

        public int Revisions { get; init; }

        public JsonDocument? Meta { get; init; }
    }

    private static JsonDocument Meta(params (string Field, string Old, string New)[] fields)
    {
        var payload = new Dictionary<string, object>
        {
            ["fields"] = fields.ToDictionary(field => field.Field, field => new { old = field.Old, @new = field.New }),
        };

        return JsonSerializer.SerializeToDocument(payload);
    }
}
