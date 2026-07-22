using FluentAssertions;

using Microsoft.Extensions.Options;

using Netptune.Activity.Handlers;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Activity;
using Netptune.Core.Services.Notifications;
using Netptune.Core.UnitOfWork;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Activity;

public class ActivityHandlerMergeTests
{
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly INotificationEventPublisher NotificationEvents = Substitute.For<INotificationEventPublisher>();

    private const int WorkspaceId = 1;
    private const int EntityId = 99;
    private const string ActorUserId = "actor-user-id";
    private const string OtherUserId = "other-user-id";

    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private int NextLogId = 1;

    public ActivityHandlerMergeTests()
    {
        UnitOfWork.Ancestors
            .GetProjectTaskAncestors(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new ActivityAncestors { WorkspaceId = WorkspaceId, TaskId = 10, ProjectKey = "PROJ", TaskScopeId = 42 });

        UnitOfWork.EventRecords
            .AddAsync(Arg.Any<EventRecord>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                var log = x.Arg<EventRecord>();

                // Stand in for the identity column.
                log.Id = NextLogId++;

                return log;
            });

        UnitOfWork.EventRecords
            .GetExistingEventIds(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid>());

        UnitOfWork.ActivityEntries
            .AddAsync(Arg.Any<ActivityEntry>(), Arg.Any<CancellationToken>())
            .Returns(x => x.Arg<ActivityEntry>());

        UnitOfWork.ActivityEntries
            .UpsertEntry(
            Arg.Any<ActivityEntryUpsert>(),
            Arg.Any<DateTime>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>())
            .Returns(new UpsertEntryResult.Upserted(new()));

        UnitOfWork.Workspaces
            .GetSlugsByIds(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, string> { [WorkspaceId] = "test-workspace" });

        UnitOfWork.WorkspaceUsers
            .GetWorkspaceUserIdsByWorkspaceIds(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, List<string>> { [WorkspaceId] = [ActorUserId, OtherUserId] });

        UnitOfWork.InvokeTransaction();
    }

    private ActivityHandler Handler(ActivityMergeOptions? merge = null) =>
        new(UnitOfWork, NotificationEvents, Options.Create(merge ?? new ActivityMergeOptions()));

    [Fact]
    public async Task Handle_ShouldNotNotify_ForMergeableTypes()
    {
        await Handler().Handle(new ActivityMessage([
            Change(TaskChangeField.Description, "a", "b"),
            Change(TaskChangeField.Description, "b", "c"),
        ]), CancellationToken);

        await UnitOfWork.Notifications.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<Notification>>(), Arg.Any<CancellationToken>());
        await NotificationEvents.DidNotReceive().PublishManyAsync(Arg.Any<IEnumerable<UserNotificationEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldWriteOneLedgerRowPerEvent_AndOneUpsert()
    {
        await Handler().Handle(new ActivityMessage([
            Change(TaskChangeField.Description, "a", "b"),
            Change(
                TaskChangeField.Name,
                "one",
                "two",
                ActivityType.ModifyName),
            Change(
                TaskChangeField.Priority,
                "None",
                "High",
                ActivityType.ModifyPriority),
        ]), CancellationToken);

        await UnitOfWork.EventRecords.Received(3).AddAsync(Arg.Any<EventRecord>(), Arg.Any<CancellationToken>());

        await UnitOfWork.ActivityEntries.Received(1).UpsertEntry(
            Arg.Is<ActivityEntryUpsert>(upsert =>
                upsert.RevisionCount == 3
                && upsert.ActivityType == ActivityType.Modify
                && upsert.ChangedFields.SequenceEqual(new[] { "description", "name", "priority" })
                && upsert.LastEventRecordId == 3),
            Arg.Any<DateTime>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNeverMergeDiscreteTypes_AndShouldNotifyThemImmediately()
    {
        await Handler().Handle(new ActivityMessage([
            Change(TaskChangeField.Description, "a", "b"),
            Discrete(ActivityType.Mention),
        ]), CancellationToken);

        await UnitOfWork.ActivityEntries.Received(1).UpsertEntry(
            Arg.Any<ActivityEntryUpsert>(),
            Arg.Any<DateTime>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());

        await UnitOfWork.ActivityEntries.Received(1).AddAsync(
            Arg.Is<ActivityEntry>(entry =>
                entry.ActivityType == ActivityType.Mention
                && !entry.IsOpen
                && entry.NotifiedAt != null),
            Arg.Any<CancellationToken>());

        await UnitOfWork.Notifications.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<Notification>>(notifications => notifications.All(n => n.ActivityType == ActivityType.Mention)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldEndOtherUsersBursts_OnEveryEvent()
    {
        await Handler().Handle(new ActivityMessage(Change(TaskChangeField.Description, "a", "b")), CancellationToken);

        await UnitOfWork.ActivityEntries.Received(1).ExpireEntriesForOtherUsers(
            WorkspaceId,
            EntityType.Task,
            EntityId,
            ActorUserId,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCloseTheStaleEntry_AndRetry_WhenTheSlotIsBlocked()
    {
        var attempts = 0;

        UnitOfWork.ActivityEntries
            .UpsertEntry(
            Arg.Any<ActivityEntryUpsert>(),
            Arg.Any<DateTime>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>())
            .Returns(UpsertEntryResult (_) => attempts++ == 0
                ? new UpsertEntryResult.SlotHeldByStaleEntry()
                : new UpsertEntryResult.Upserted(new()));

        await Handler().Handle(new ActivityMessage(Change(TaskChangeField.Description, "a", "b")), CancellationToken);

        await UnitOfWork.ActivityEntries.Received(1).CloseStaleEntry(
            WorkspaceId,
            EntityType.Task,
            EntityId,
            ActorUserId,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());

        attempts.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldGiveUp_RatherThanSpin_WhenTheSlotNeverFrees()
    {
        UnitOfWork.ActivityEntries
            .UpsertEntry(
            Arg.Any<ActivityEntryUpsert>(),
            Arg.Any<DateTime>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>())
            .Returns(new UpsertEntryResult.SlotHeldByStaleEntry());

        var act = () => Handler().Handle(new ActivityMessage(Change(TaskChangeField.Description, "a", "b")), CancellationToken).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>("a nak and a redelivery is better than a spinning handler");
    }


    private static ActivityEvent Change(
        TaskChangeField field,
        string? oldValue,
        string? newValue,
        ActivityType type = ActivityType.ModifyDescription) =>
        new()
        {
            EventId = Guid.NewGuid(),
            Type = type,
            EntityType = EntityType.Task,
            EntityId = EntityId,
            WorkspaceId = WorkspaceId,
            UserId = ActorUserId,
            OccurredAt = DateTime.UtcNow,
            Field = field,
            OldValue = oldValue,
            NewValue = newValue,
        };

    private static ActivityEvent Discrete(ActivityType type) =>
        new()
        {
            EventId = Guid.NewGuid(),
            Type = type,
            EntityType = EntityType.Task,
            EntityId = EntityId,
            WorkspaceId = WorkspaceId,
            UserId = ActorUserId,
            OccurredAt = DateTime.UtcNow,
            RecipientUserIds = [OtherUserId],
        };
}
