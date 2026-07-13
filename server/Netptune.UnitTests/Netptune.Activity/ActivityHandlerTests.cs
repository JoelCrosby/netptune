using AutoFixture;

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

public class ActivityHandlerTests
{
    private readonly Fixture Fixture = new();

    private readonly ActivityHandler Handler;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly INotificationEventPublisher NotificationEvents = Substitute.For<INotificationEventPublisher>();

    private readonly HashSet<Guid> PersistedEventIds = [];

    private const int WorkspaceId = 1;
    private const int EntityId = 99;
    private const string ActorUserId = "actor-user-id";
    private const string OtherUserId1 = "other-user-id-1";
    private const string OtherUserId2 = "other-user-id-2";

    private static readonly ActivityAncestors DefaultAncestors = new()
    {
        WorkspaceId = WorkspaceId, ProjectId = 1, BoardId = 1, BoardGroupId = 1,
        TaskId = 10, TaskScopeId = 42, ProjectKey = "PROJ", BoardKey = "board-1",
    };

    public ActivityHandlerTests()
    {
        UnitOfWork.Ancestors
            .GetProjectTaskAncestors(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(DefaultAncestors);

        UnitOfWork.Ancestors
            .GetBoardGroupAncestors(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(DefaultAncestors);

        UnitOfWork.Ancestors
            .GetBoardAncestors(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(DefaultAncestors);

        UnitOfWork.Ancestors
            .GetProjectAncestors(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(DefaultAncestors);

        UnitOfWork.Ancestors
            .GetSprintAncestors(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(DefaultAncestors);

        UnitOfWork.ActivityLogs
            .AddAsync(Arg.Any<ActivityLog>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                var log = x.Arg<ActivityLog>();

                if (log.EventId is { } eventId) PersistedEventIds.Add(eventId);

                return log;
            });

        UnitOfWork.ActivityLogs
            .GetExistingEventIds(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(x => x.Arg<IEnumerable<Guid>>().Where(PersistedEventIds.Contains).ToHashSet());

        UnitOfWork.Workspaces
            .GetSlugsByIds(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, string> { [WorkspaceId] = "test-workspace" });

        UnitOfWork.WorkspaceUsers
            .GetWorkspaceUserIdsByWorkspaceIds(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, List<string>>
            {
                [WorkspaceId] = [ActorUserId, OtherUserId1, OtherUserId2],
            });

        UnitOfWork.Notifications
            .AddRangeAsync(Arg.Any<IEnumerable<Notification>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        NotificationEvents
            .PublishManyAsync(Arg.Any<IEnumerable<UserNotificationEvent>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        UnitOfWork.ActivityEntries
            .AddAsync(Arg.Any<ActivityEntry>(), Arg.Any<CancellationToken>())
            .Returns(x => x.Arg<ActivityEntry>());

        UnitOfWork.InvokeTransaction();

        Handler = new(UnitOfWork, NotificationEvents, Options.Create(new ActivityMergeOptions()));
    }

    private ActivityEvent BuildEvent(string? userId = null, int? workspaceId = null) =>
        Fixture.Build<ActivityEvent>()
            .With(e => e.Type, ActivityType.Mention)
            .With(e => e.UserId, userId ?? ActorUserId)
            .With(e => e.WorkspaceId, workspaceId ?? WorkspaceId)
            .With(e => e.EntityId, EntityId)
            .With(e => e.RecipientUserIds, [OtherUserId1, OtherUserId2])
            .Without(e => e.Meta)
            .Create();

    [Fact]
    public async Task Handle_ShouldPersistActivityLog_ForEachEvent()
    {
        var message = new ActivityMessage([BuildEvent(), BuildEvent()]);

        await Handler.Handle(message, TestContext.Current.CancellationToken);

        await UnitOfWork.ActivityLogs.Received(2).AddAsync(Arg.Any<ActivityLog>(), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(3).CompleteAsync(TestContext.Current.CancellationToken);
        await UnitOfWork.Received(1).Transaction(Arg.Any<Func<Task>>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task Handle_ShouldPersistEventId_OnActivityLog()
    {
        var @event = BuildEvent();
        var message = new ActivityMessage(@event);

        await Handler.Handle(message, TestContext.Current.CancellationToken);

        await UnitOfWork.ActivityLogs.Received(1).AddAsync(
            Arg.Is<ActivityLog>(log => log.EventId == @event.EventId),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldWriteOneLedgerRow_WhenMessageIsRedelivered()
    {
        var message = new ActivityMessage(BuildEvent());

        await Handler.Handle(message, TestContext.Current.CancellationToken);
        await Handler.Handle(message, TestContext.Current.CancellationToken);

        await UnitOfWork.ActivityLogs.Received(1).AddAsync(Arg.Any<ActivityLog>(), TestContext.Current.CancellationToken);
        await UnitOfWork.Notifications.Received(1).AddRangeAsync(Arg.Any<IEnumerable<Notification>>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldWriteOneLedgerRow_WhenTheSameEventAppearsTwiceInOneMessage()
    {
        var @event = BuildEvent();
        var message = new ActivityMessage([@event, @event]);

        await Handler.Handle(message, TestContext.Current.CancellationToken);

        await UnitOfWork.ActivityLogs.Received(1).AddAsync(Arg.Any<ActivityLog>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldCreateNotifications_ForAllRecipientsExcludingActor()
    {
        var message = new ActivityMessage(BuildEvent());

        await Handler.Handle(message, TestContext.Current.CancellationToken);

        await UnitOfWork.Notifications.Received(1).AddRangeAsync(Arg.Is<IEnumerable<Notification>>(notifications =>
            // ReSharper disable PossibleMultipleEnumeration
            notifications.Count() == 2 &&
            notifications.All(n => n.UserId != ActorUserId) &&
            notifications.Any(n => n.UserId == OtherUserId1) &&
            notifications.Any(n => n.UserId == OtherUserId2)), TestContext.Current.CancellationToken);
            // ReSharper enable PossibleMultipleEnumeration
    }

    [Fact]
    public async Task Handle_ShouldNotCreateNotifications_WhenActorIsOnlyWorkspaceMember()
    {
        UnitOfWork.WorkspaceUsers
            .GetWorkspaceUserIdsByWorkspaceIds(Arg.Any<IEnumerable<int>>(), TestContext.Current.CancellationToken).Returns(new Dictionary<int, List<string>> { [WorkspaceId] = [ActorUserId] });

        var message = new ActivityMessage(BuildEvent());

        await Handler.Handle(message, TestContext.Current.CancellationToken);

        await UnitOfWork.Notifications.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<Notification>>(), TestContext.Current.CancellationToken);
        await NotificationEvents.DidNotReceive().PublishManyAsync(
            Arg.Any<IEnumerable<UserNotificationEvent>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSetCorrectNotificationFields()
    {
        var @event = Fixture.Build<ActivityEvent>()
            .With(e => e.Type, ActivityType.Mention)
            .With(e => e.UserId, ActorUserId)
            .With(e => e.WorkspaceId, WorkspaceId)
            .With(e => e.EntityId, Fixture.Create<int>())
            .With(e => e.EntityType, EntityType.Task)
            .With(e => e.RecipientUserIds, [OtherUserId1, OtherUserId2])
            .Without(e => e.Meta)
            .Create();

        var message = new ActivityMessage(@event);

        await Handler.Handle(message, TestContext.Current.CancellationToken);

        await UnitOfWork.Notifications.Received(1).AddRangeAsync(Arg.Is<IEnumerable<Notification>>(notifications =>
            notifications.All(n =>
                n.WorkspaceId == WorkspaceId &&
                n.EntityType == EntityType.Task &&
                n.ActivityType == @event.Type &&
                n.IsRead == false &&
                n.Link == "/test-workspace/tasks/PROJ-42" &&
                n.CreatedByUserId == ActorUserId)), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(EntityType.Task, "/test-workspace/tasks/PROJ-42")]
    [InlineData(EntityType.Board, "/test-workspace/boards/board-1")]
    [InlineData(EntityType.Project, "/test-workspace/projects/99")]
    [InlineData(EntityType.Sprint, "/test-workspace/sprints/99")]
    [InlineData(EntityType.Status, "/test-workspace/settings")]
    [InlineData(EntityType.Workspace, "/test-workspace")]
    public async Task Handle_ShouldBuildCorrectLink_ForEntityType(EntityType entityType, string expectedLink)
    {
        var @event = Fixture.Build<ActivityEvent>()
            .With(e => e.Type, ActivityType.Mention)
            .With(e => e.UserId, ActorUserId)
            .With(e => e.WorkspaceId, WorkspaceId)
            .With(e => e.EntityId, EntityId)
            .With(e => e.EntityType, entityType)
            .With(e => e.RecipientUserIds, [OtherUserId1, OtherUserId2])
            .Without(e => e.Meta)
            .Create();

        var message = new ActivityMessage(@event);

        await Handler.Handle(message, TestContext.Current.CancellationToken);

        await UnitOfWork.Notifications.Received(1).AddRangeAsync(Arg.Is<IEnumerable<Notification>>(notifications =>
                notifications.All(n => n.Link == expectedLink)), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldPublishNotificationEvent_ForEachRecipient()
    {
        var message = new ActivityMessage(BuildEvent());

        await Handler.Handle(message, TestContext.Current.CancellationToken);

        await NotificationEvents.Received(1).PublishManyAsync(
            Arg.Is<IEnumerable<UserNotificationEvent>>(events =>
                events.Count() == 2 &&
                events.Any(e => e.UserId == OtherUserId1 && e.Event.IsRead == false) &&
                events.Any(e => e.UserId == OtherUserId2 && e.Event.IsRead == false) &&
                events.All(e => e.UserId != ActorUserId)),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldQueryWorkspacesOnce_ForMultipleEventsInSameWorkspace()
    {
        var message = new ActivityMessage([BuildEvent(), BuildEvent(), BuildEvent()]);

        await Handler.Handle(message, TestContext.Current.CancellationToken);

        await UnitOfWork.Workspaces.Received(1).GetSlugsByIds(Arg.Any<IEnumerable<int>>(), TestContext.Current.CancellationToken);
        await UnitOfWork.WorkspaceUsers.Received(1).GetWorkspaceUserIdsByWorkspaceIds(Arg.Any<IEnumerable<int>>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldSkipWorkspace_WhenSlugNotFound()
    {
        UnitOfWork.Workspaces
            .GetSlugsByIds(Arg.Any<IEnumerable<int>>(), TestContext.Current.CancellationToken).Returns(new Dictionary<int, string>());

        var message = new ActivityMessage(BuildEvent());

        await Handler.Handle(message, TestContext.Current.CancellationToken);

        await UnitOfWork.Notifications.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<Notification>>(), TestContext.Current.CancellationToken);
    }
}
