using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Core.Repositories;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Notifications;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Notifications;

public sealed class NotificationSubscriptionMatcherTests
{
    private const int WorkspaceId = 10;
    private const int TaskId = 55;
    private const int ProjectId = 1;
    private const int BoardId = 2;
    private const int BoardGroupId = 3;
    private const int SprintId = 4;
    private const string SubscriberUserId = "subscriber";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    [Fact]
    public async Task Match_ReturnsNoOne_WhenTheEventIsNotAboutATask()
    {
        var request = Request(ActivityType.Create) with { EntityType = EntityType.Board };

        var recipients = await Match(request);

        recipients.Should().BeEmpty();
        await UnitOfWork.Ancestors.DidNotReceiveWithAnyArgs().GetTaskScopes(
            Arg.Any<IReadOnlyCollection<int>>(),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Match_ReturnsSubscriber_WhenTheBoardSubscriptionCoversTheEvent()
    {
        GivenScopes(CurrentScopes());
        GivenSubscriptions(Subscription(NotificationScope.Board, BoardId, NotificationSubscriptionEvents.TaskUpdated));

        var recipients = await Match(Request(ActivityType.ModifyName));

        recipients.Should().Equal(SubscriberUserId);
    }

    [Fact]
    public async Task Match_ExcludesSubscriber_WhenTheSubscriptionDoesNotCoverTheEvent()
    {
        GivenScopes(CurrentScopes());
        GivenSubscriptions(Subscription(NotificationScope.Board, BoardId, NotificationSubscriptionEvents.TaskAdded));

        var recipients = await Match(Request(ActivityType.ModifyName));

        recipients.Should().BeEmpty();
    }

    [Fact]
    public async Task Match_ReturnsBoardSubscriber_WhenTheTaskJoinsThatBoard()
    {
        GivenScopes(CurrentScopes());
        GivenSubscriptions(Subscription(NotificationScope.Board, BoardId, NotificationSubscriptionEvents.TaskAdded));

        var payload = Payload(new { boardId = BoardId, groupId = BoardGroupId });
        var request = Request(ActivityType.Move) with { Payload = payload };

        var recipients = await Match(request);

        recipients.Should().Equal(SubscriberUserId);
    }

    [Fact]
    public async Task Match_ExcludesBoardSubscriber_WhenTheTaskOnlyMovesBetweenGroupsOfThatBoard()
    {
        GivenScopes(CurrentScopes());
        GivenSubscriptions(Subscription(NotificationScope.Board, BoardId, NotificationSubscriptionEvents.TaskAdded));

        var payload = Payload(new { groupId = BoardGroupId });
        var request = Request(ActivityType.Move) with { Payload = payload };

        var recipients = await Match(request);

        recipients.Should().BeEmpty();
    }

    [Fact]
    public async Task Match_ReturnsGroupSubscriber_WhenTheTaskLeavesThatGroup()
    {
        GivenScopes(CurrentScopes());
        GivenSubscriptions(Subscription(NotificationScope.BoardGroup, 77, NotificationSubscriptionEvents.TaskRemoved));

        var payload = Payload(new { groupId = BoardGroupId, fromGroupId = 77 });
        var request = Request(ActivityType.Move) with { Payload = payload };

        var recipients = await Match(request);

        recipients.Should().Equal(SubscriberUserId);
    }

    [Fact]
    public async Task Match_ReturnsGroupSubscriber_WhenTheTaskIsRemovedFromTheBoardItSatOn()
    {
        GivenScopes(CurrentScopes() with { BoardIds = [], BoardGroupIds = [] });
        GivenSubscriptions(Subscription(NotificationScope.BoardGroup, BoardGroupId, NotificationSubscriptionEvents.TaskRemoved));

        var payload = Payload(new { boardId = BoardId, groupId = BoardGroupId });
        var request = Request(ActivityType.Remove) with { Payload = payload };

        var recipients = await Match(request);

        recipients.Should().Equal(SubscriberUserId);
    }

    [Fact]
    public async Task Match_ReturnsSprintSubscriber_WhenTheTaskJoinsThatSprint()
    {
        GivenScopes(CurrentScopes());
        GivenSubscriptions(Subscription(NotificationScope.Sprint, SprintId, NotificationSubscriptionEvents.TaskAdded));

        var payload = Payload(new { field = nameof(TaskChangeField.Sprint), oldValue = "99", newValue = "4" });
        var request = Request(ActivityType.Move) with { Payload = payload };

        var recipients = await Match(request);

        recipients.Should().Equal(SubscriberUserId);
    }

    [Fact]
    public async Task Match_ReturnsSprintSubscriber_WhenTheTaskLeavesThatSprint()
    {
        GivenScopes(CurrentScopes() with { SprintId = null });
        GivenSubscriptions(Subscription(NotificationScope.Sprint, SprintId, NotificationSubscriptionEvents.TaskRemoved));

        var payload = Payload(new { field = nameof(TaskChangeField.Sprint), oldValue = "4", newValue = (string?)null });
        var request = Request(ActivityType.Move) with { Payload = payload };

        var recipients = await Match(request);

        recipients.Should().Equal(SubscriberUserId);
    }

    [Fact]
    public async Task Match_ReturnsBoardSubscriber_WhenTheTaskIsRemovedFromThatBoard()
    {
        GivenScopes(CurrentScopes() with { BoardIds = [], BoardGroupIds = [] });
        GivenSubscriptions(Subscription(NotificationScope.Board, BoardId, NotificationSubscriptionEvents.TaskRemoved));

        var payload = Payload(new { boardId = BoardId });
        var request = Request(ActivityType.Remove) with { Payload = payload };

        var recipients = await Match(request);

        recipients.Should().Equal(SubscriberUserId);
    }

    [Fact]
    public async Task Match_ReturnsOneRecipient_WhenSeveralSubscriptionsOfTheSamePersonMatch()
    {
        GivenScopes(CurrentScopes());
        GivenSubscriptions(
            Subscription(NotificationScope.Project, ProjectId, NotificationSubscriptionEvents.All),
            Subscription(NotificationScope.Board, BoardId, NotificationSubscriptionEvents.All),
            Subscription(NotificationScope.Sprint, SprintId, NotificationSubscriptionEvents.All));

        var recipients = await Match(Request(ActivityType.Create));

        recipients.Should().Equal(SubscriberUserId);
    }

    [Fact]
    public async Task Match_QueriesEveryScopeTheTaskSitsIn()
    {
        GivenScopes(CurrentScopes());
        GivenSubscriptions();

        await Match(Request(ActivityType.Create));

        await UnitOfWork.NotificationSubscriptions.Received(1).GetForScopes(
            Arg.Is<NotificationSubscriptionScopeQuery>(query =>
                query.WorkspaceId == WorkspaceId &&
                query.ProjectIds.Contains(ProjectId) &&
                query.BoardIds.Contains(BoardId) &&
                query.BoardGroupIds.Contains(BoardGroupId) &&
                query.SprintIds.Contains(SprintId)),
            Arg.Any<CancellationToken>());
    }

    private async Task<IReadOnlyList<string>> Match(NotificationSubscriptionMatchRequest request)
    {
        var fanOut = await NotificationSubscriptionFanOut.Build(
            UnitOfWork,
            [request],
            TestContext.Current.CancellationToken);

        return fanOut.Recipients(request);
    }

    private static NotificationSubscriptionMatchRequest Request(ActivityType activityType)
    {
        return new NotificationSubscriptionMatchRequest
        {
            WorkspaceId = WorkspaceId,
            EntityType = EntityType.Task,
            EntityId = TaskId,
            ActivityType = activityType,
        };
    }

    private static TaskScopes CurrentScopes()
    {
        return new TaskScopes
        {
            ProjectId = ProjectId,
            SprintId = SprintId,
            BoardIds = [BoardId],
            BoardGroupIds = [BoardGroupId],
        };
    }

    private static JsonElement Payload(object value)
    {
        return JsonSerializer.SerializeToElement(value);
    }

    private static NotificationSubscription Subscription(
        NotificationScope scope,
        int scopeEntityId,
        NotificationSubscriptionEvents events)
    {
        return new NotificationSubscription
        {
            UserId = SubscriberUserId,
            Scope = scope,
            ScopeEntityId = scopeEntityId,
            Events = events,
            WorkspaceId = WorkspaceId,
        };
    }

    private void GivenScopes(TaskScopes scopes)
    {
        UnitOfWork.Ancestors
            .GetTaskScopes(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, TaskScopes> { [TaskId] = scopes });
    }

    private void GivenSubscriptions(params NotificationSubscription[] subscriptions)
    {
        UnitOfWork.NotificationSubscriptions
            .GetForScopes(Arg.Any<NotificationSubscriptionScopeQuery>(), Arg.Any<CancellationToken>())
            .Returns(subscriptions.ToList());
    }
}
