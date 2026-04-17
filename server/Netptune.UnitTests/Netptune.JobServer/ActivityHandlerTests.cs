using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Activity;
using Netptune.Core.Repositories;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.JobServer.Handlers;

using NSubstitute;

using StackExchange.Redis;

using Xunit;

namespace Netptune.UnitTests.Netptune.JobServer;

public class ActivityHandlerTests
{
    private readonly Fixture Fixture = new();

    private readonly ActivityHandler Handler;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IAncestorService AncestorService = Substitute.For<IAncestorService>();
    private readonly IConnectionMultiplexer Redis = Substitute.For<IConnectionMultiplexer>();
    private readonly ISubscriber Subscriber = Substitute.For<ISubscriber>();

    private const int WorkspaceId = 1;
    private const string ActorUserId = "actor-user-id";
    private const string OtherUserId1 = "other-user-id-1";
    private const string OtherUserId2 = "other-user-id-2";

    public ActivityHandlerTests()
    {
        Redis.GetSubscriber(Arg.Any<object?>()).Returns(Subscriber);

        Subscriber
            .PublishAsync(Arg.Any<RedisChannel>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(1L);

        AncestorService
            .GetTaskAncestors(Arg.Any<int>())
            .Returns(new ActivityAncestors { WorkspaceId = WorkspaceId, ProjectId = 1, BoardId = 1, BoardGroupId = 1 });

        UnitOfWork.ActivityLogs
            .AddAsync(Arg.Any<ActivityLog>())
            .Returns(x => x.Arg<ActivityLog>());

        UnitOfWork.Workspaces
            .GetSlugsByIds(Arg.Any<IEnumerable<int>>())
            .Returns(new Dictionary<int, string> { [WorkspaceId] = "test-workspace" });

        UnitOfWork.WorkspaceUsers
            .GetWorkspaceUserIdsByWorkspaceIds(Arg.Any<IEnumerable<int>>())
            .Returns(new Dictionary<int, List<string>>
            {
                [WorkspaceId] = [ActorUserId, OtherUserId1, OtherUserId2],
            });

        UnitOfWork.Notifications
            .AddRangeAsync(Arg.Any<IEnumerable<Notification>>())
            .Returns(Task.CompletedTask);

        Handler = new(UnitOfWork, AncestorService, Redis);
    }

    private ActivityEvent BuildEvent(string? userId = null, int? workspaceId = null) =>
        Fixture.Build<ActivityEvent>()
            .With(e => e.UserId, userId ?? ActorUserId)
            .With(e => e.WorkspaceId, workspaceId ?? WorkspaceId)
            .With(e => e.EntityId, Fixture.Create<int>())
            .Without(e => e.Meta)
            .Create();

    [Fact]
    public async Task Handle_ShouldPersistActivityLog_ForEachEvent()
    {
        var message = new ActivityMessage([BuildEvent(), BuildEvent()]);

        await Handler.Handle(message, CancellationToken.None);

        await UnitOfWork.ActivityLogs.Received(2).AddAsync(Arg.Any<ActivityLog>());
        await UnitOfWork.Received(2).CompleteAsync();
    }

    [Fact]
    public async Task Handle_ShouldCreateNotifications_ForAllRecipientsExcludingActor()
    {
        var message = new ActivityMessage(BuildEvent());

        await Handler.Handle(message, CancellationToken.None);

        await UnitOfWork.Notifications.Received(1).AddRangeAsync(Arg.Is<IEnumerable<Notification>>(notifications =>
            notifications.Count() == 2 &&
            notifications.All(n => n.UserId != ActorUserId) &&
            notifications.Any(n => n.UserId == OtherUserId1) &&
            notifications.Any(n => n.UserId == OtherUserId2)));
    }

    [Fact]
    public async Task Handle_ShouldNotCreateNotifications_WhenActorIsOnlyWorkspaceMember()
    {
        UnitOfWork.WorkspaceUsers
            .GetWorkspaceUserIdsByWorkspaceIds(Arg.Any<IEnumerable<int>>())
            .Returns(new Dictionary<int, List<string>> { [WorkspaceId] = [ActorUserId] });

        var message = new ActivityMessage(BuildEvent());

        await Handler.Handle(message, CancellationToken.None);

        await UnitOfWork.Notifications.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<Notification>>());
        await Subscriber.DidNotReceive().PublishAsync(Arg.Any<RedisChannel>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task Handle_ShouldSetCorrectNotificationFields()
    {
        var @event = Fixture.Build<ActivityEvent>()
            .With(e => e.UserId, ActorUserId)
            .With(e => e.WorkspaceId, WorkspaceId)
            .With(e => e.EntityId, Fixture.Create<int>())
            .With(e => e.EntityType, EntityType.Task)
            .Without(e => e.Meta)
            .Create();

        var message = new ActivityMessage(@event);

        await Handler.Handle(message, CancellationToken.None);

        await UnitOfWork.Notifications.Received(1).AddRangeAsync(Arg.Is<IEnumerable<Notification>>(notifications =>
            notifications.All(n =>
                n.WorkspaceId == WorkspaceId &&
                n.EntityType == EntityType.Task &&
                n.ActivityType == @event.Type &&
                n.IsRead == false &&
                n.Link == "/test-workspace/tasks" &&
                n.CreatedByUserId == ActorUserId)));
    }

    [Theory]
    [InlineData(EntityType.Task, "/test-workspace/tasks")]
    [InlineData(EntityType.Board, "/test-workspace/boards")]
    [InlineData(EntityType.Project, "/test-workspace/projects")]
    [InlineData(EntityType.Workspace, "/test-workspace")]
    public async Task Handle_ShouldBuildCorrectLink_ForEntityType(EntityType entityType, string expectedLink)
    {
        var @event = Fixture.Build<ActivityEvent>()
            .With(e => e.UserId, ActorUserId)
            .With(e => e.WorkspaceId, WorkspaceId)
            .With(e => e.EntityId, Fixture.Create<int>())
            .With(e => e.EntityType, entityType)
            .Without(e => e.Meta)
            .Create();

        var message = new ActivityMessage(@event);

        await Handler.Handle(message, CancellationToken.None);

        await UnitOfWork.Notifications.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<Notification>>(notifications =>
                notifications.All(n => n.Link == expectedLink)));
    }

    [Fact]
    public async Task Handle_ShouldPublishToRedis_ForEachRecipient()
    {
        var message = new ActivityMessage(BuildEvent());

        await Handler.Handle(message, CancellationToken.None);

        await Subscriber.Received(1).PublishAsync(
            Arg.Is<RedisChannel>(c => c == RedisChannel.Literal($"notifications:{OtherUserId1}")),
            Arg.Any<RedisValue>(),
            Arg.Any<CommandFlags>());

        await Subscriber.Received(1).PublishAsync(
            Arg.Is<RedisChannel>(c => c == RedisChannel.Literal($"notifications:{OtherUserId2}")),
            Arg.Any<RedisValue>(),
            Arg.Any<CommandFlags>());

        await Subscriber.DidNotReceive().PublishAsync(
            Arg.Is<RedisChannel>(c => c == RedisChannel.Literal($"notifications:{ActorUserId}")),
            Arg.Any<RedisValue>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task Handle_ShouldQueryWorkspacesOnce_ForMultipleEventsInSameWorkspace()
    {
        var message = new ActivityMessage([BuildEvent(), BuildEvent(), BuildEvent()]);

        await Handler.Handle(message, CancellationToken.None);

        await UnitOfWork.Workspaces.Received(1).GetSlugsByIds(Arg.Any<IEnumerable<int>>());
        await UnitOfWork.WorkspaceUsers.Received(1).GetWorkspaceUserIdsByWorkspaceIds(Arg.Any<IEnumerable<int>>());
    }

    [Fact]
    public async Task Handle_ShouldSkipWorkspace_WhenSlugNotFound()
    {
        UnitOfWork.Workspaces
            .GetSlugsByIds(Arg.Any<IEnumerable<int>>())
            .Returns(new Dictionary<int, string>());

        var message = new ActivityMessage(BuildEvent());

        await Handler.Handle(message, CancellationToken.None);

        await UnitOfWork.Notifications.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<Notification>>());
    }
}
