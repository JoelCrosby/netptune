using System.Text.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Preferences;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Activity;

public class NotificationSubscriptionTests(NotificationSubscriptionFixture fixture)
    : IClassFixture<NotificationSubscriptionFixture>, IAsyncLifetime
{
    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        using var scope = fixture.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        await db.NotificationSubscriptions.ExecuteDeleteAsync(CancellationToken);
        await db.Notifications.ExecuteDeleteAsync(CancellationToken);
        await db.UserPreferenceValues.ExecuteDeleteAsync(CancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Handle_ShouldNotifyBoardSubscriber_WhenATaskJoinsThatBoard()
    {
        await Subscribe(NotificationScope.Board, fixture.BoardId, NotificationSubscriptionEvents.TaskAdded);

        await Handle(Move(new MoveTaskActivityMeta
        {
            Group = "Backlog",
            GroupId = fixture.BoardGroupId,
            BoardId = fixture.BoardId,
        }));

        var recipients = await Recipients();

        recipients.Should().Equal(fixture.SubscriberUserId);
    }

    [Fact]
    public async Task Handle_ShouldNotNotifyBoardSubscriber_WhenTheTaskJoinsAnotherBoard()
    {
        await Subscribe(NotificationScope.Board, fixture.BoardId, NotificationSubscriptionEvents.TaskAdded);

        await Handle(Move(new MoveTaskActivityMeta
        {
            Group = "Backlog",
            GroupId = 9999,
            BoardId = fixture.OtherBoardId,
        }));

        var recipients = await Recipients();

        recipients.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldNotifyGroupSubscriber_WhenATaskIsDraggedOutOfThatGroup()
    {
        await Subscribe(
            NotificationScope.BoardGroup,
            fixture.BoardGroupId,
            NotificationSubscriptionEvents.TaskRemoved);

        await Handle(Move(new MoveTaskActivityMeta
        {
            Group = "Doing",
            GroupId = fixture.OtherBoardGroupId,
            FromGroupId = fixture.BoardGroupId,
        }));

        var recipients = await Recipients();

        recipients.Should().Equal(fixture.SubscriberUserId);
    }

    [Fact]
    public async Task Handle_ShouldNotifyGroupSubscriber_WhenATaskIsRemovedFromTheBoard()
    {
        await Subscribe(
            NotificationScope.BoardGroup,
            fixture.BoardGroupId,
            NotificationSubscriptionEvents.TaskRemoved);

        await Handle(Remove(new RemoveTaskFromBoardActivityMeta
        {
            Board = "Board",
            BoardId = fixture.BoardId,
            GroupId = fixture.BoardGroupId,
        }));

        var recipients = await Recipients();

        recipients.Should().Equal(fixture.SubscriberUserId);
    }

    [Fact]
    public async Task Handle_ShouldNotifySprintSubscriber_WhenATaskJoinsThatSprint()
    {
        await Subscribe(NotificationScope.Sprint, fixture.SprintId, NotificationSubscriptionEvents.TaskAdded);

        await Handle(SprintChange(null, fixture.SprintId.ToString()));

        var recipients = await Recipients();

        recipients.Should().Equal(fixture.SubscriberUserId);
    }

    [Fact]
    public async Task Handle_ShouldNotifySprintSubscriber_WhenATaskLeavesThatSprint()
    {
        await Subscribe(NotificationScope.Sprint, fixture.SprintId, NotificationSubscriptionEvents.TaskRemoved);

        await Handle(SprintChange(fixture.SprintId.ToString(), null));

        var recipients = await Recipients();

        recipients.Should().Equal(fixture.SubscriberUserId);
    }

    [Fact]
    public async Task Handle_ShouldNotifyOnce_WhenSeveralSubscriptionsOfOnePersonMatch()
    {
        await Subscribe(NotificationScope.Project, fixture.ProjectId, NotificationSubscriptionEvents.All);
        await Subscribe(NotificationScope.Board, fixture.BoardId, NotificationSubscriptionEvents.All);
        await Subscribe(NotificationScope.Sprint, fixture.SprintId, NotificationSubscriptionEvents.All);

        await Handle(Discrete(ActivityType.Create));

        var notifications = await Notifications();

        notifications.Should().ContainSingle();
        notifications[0].UserId.Should().Be(fixture.SubscriberUserId);
    }

    [Fact]
    public async Task Handle_ShouldStopNotifying_AfterTheSubscriptionIsRemoved()
    {
        var subscription = await Subscribe(
            NotificationScope.Board,
            fixture.BoardId,
            NotificationSubscriptionEvents.All);

        await Unsubscribe(subscription);
        await Handle(Discrete(ActivityType.Create));

        var recipients = await Recipients();

        recipients.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldNotNotifySubscriber_WhenTheEventIsNotOneTheySelected()
    {
        await Subscribe(NotificationScope.Board, fixture.BoardId, NotificationSubscriptionEvents.TaskRemoved);

        await Handle(Discrete(ActivityType.Create));

        var recipients = await Recipients();

        recipients.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldNotNotifyTheActor_WhenTheySubscribedThemselves()
    {
        await Subscribe(
            NotificationScope.Board,
            fixture.BoardId,
            NotificationSubscriptionEvents.All,
            fixture.ActorUserId);

        await Handle(Discrete(ActivityType.Create));

        var recipients = await Recipients();

        recipients.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldNotNotifySubscriber_WhenTheyTurnedTheEventTypeOff()
    {
        await Subscribe(NotificationScope.Board, fixture.BoardId, NotificationSubscriptionEvents.All);
        await DisableEventPreference(ActivityType.Create);

        await Handle(Discrete(ActivityType.Create));

        var recipients = await Recipients();

        recipients.Should().BeEmpty();
    }

    private async Task<NotificationSubscription> Subscribe(
        NotificationScope scope,
        int scopeEntityId,
        NotificationSubscriptionEvents events,
        string? userId = null)
    {
        using var serviceScope = fixture.CreateScope();

        var db = serviceScope.ServiceProvider.GetRequiredService<DataContext>();

        var subscription = new NotificationSubscription
        {
            UserId = userId ?? fixture.SubscriberUserId,
            Scope = scope,
            ScopeEntityId = scopeEntityId,
            Events = events,
            WorkspaceId = fixture.WorkspaceId,
        };

        db.NotificationSubscriptions.Add(subscription);

        await db.SaveChangesAsync(CancellationToken);

        return subscription;
    }

    private async Task Unsubscribe(NotificationSubscription subscription)
    {
        using var serviceScope = fixture.CreateScope();

        var db = serviceScope.ServiceProvider.GetRequiredService<DataContext>();

        await db.NotificationSubscriptions
            .Where(candidate => candidate.Id == subscription.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(candidate => candidate.IsDeleted, true), CancellationToken);
    }

    private async Task DisableEventPreference(ActivityType activityType)
    {
        using var serviceScope = fixture.CreateScope();

        var db = serviceScope.ServiceProvider.GetRequiredService<DataContext>();

        db.UserPreferenceValues.Add(new UserPreferenceValue
        {
            UserId = fixture.SubscriberUserId,
            Key = PreferenceKeys.NotificationEvent(activityType),
            WorkspaceId = fixture.WorkspaceId,
            Value = JsonDocument.Parse("false"),
        });

        await db.SaveChangesAsync(CancellationToken);
    }

    private ActivityEvent Discrete(ActivityType activityType)
    {
        return TaskEvent(activityType);
    }

    private ActivityEvent Move(MoveTaskActivityMeta meta)
    {
        var serialised = JsonSerializer.Serialize(meta, JsonOptions.Default);

        return TaskEvent(ActivityType.Move, meta: serialised);
    }

    private ActivityEvent Remove(RemoveTaskFromBoardActivityMeta meta)
    {
        var serialised = JsonSerializer.Serialize(meta, JsonOptions.Default);

        return TaskEvent(ActivityType.Remove, meta: serialised);
    }

    private ActivityEvent SprintChange(string? oldValue, string? newValue)
    {
        return TaskEvent(
            ActivityType.Move,
            field: TaskChangeField.Sprint,
            oldValue: oldValue,
            newValue: newValue);
    }

    private ActivityEvent TaskEvent(
        ActivityType activityType,
        string? meta = null,
        TaskChangeField? field = null,
        string? oldValue = null,
        string? newValue = null)
    {
        return new ActivityEvent
        {
            EventId = Guid.NewGuid(),
            Type = activityType,
            EntityType = EntityType.Task,
            EntityId = fixture.TaskId,
            WorkspaceId = fixture.WorkspaceId,
            UserId = fixture.ActorUserId,
            OccurredAt = DateTime.UtcNow,
            Meta = meta,
            Field = field,
            OldValue = oldValue,
            NewValue = newValue,
        };
    }

    private async Task Handle(ActivityEvent activity)
    {
        var (scope, handler) = fixture.CreateHandler();

        using (scope)
        {
            await handler.Handle(new ActivityMessage(activity), CancellationToken);
        }
    }

    private async Task<List<Notification>> Notifications()
    {
        using var scope = fixture.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await db.Notifications
            .AsNoTracking()
            .Where(notification => notification.WorkspaceId == fixture.WorkspaceId)
            .ToListAsync(CancellationToken);
    }

    private async Task<List<string>> Recipients()
    {
        var notifications = await Notifications();

        return notifications.Select(notification => notification.UserId).Distinct().ToList();
    }
}
