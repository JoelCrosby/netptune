using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Notifications;
using Netptune.Services.Notifications.Commands;
using Netptune.Services.Notifications.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class GetUserNotificationsQueryHandlerTests
{
    private readonly GetUserNotificationsQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    private const string UserId = "user-id";
    private const int WorkspaceId = 1;

    public GetUserNotificationsQueryHandlerTests()
    {
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.GetWorkspaceId().Returns(WorkspaceId);

        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetUserNotifications_ShouldReturnSuccess_WithNotifications()
    {
        var notifications = new List<NotificationViewModel> { new(), new() };

        UnitOfWork.Notifications
            .GetUserNotifications(UserId, WorkspaceId)
            .Returns(notifications);

        var result = await Handler.Handle(new GetUserNotificationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserNotifications_ShouldQueryWithCorrectUserAndWorkspace()
    {
        UnitOfWork.Notifications
            .GetUserNotifications(Arg.Any<string>(), Arg.Any<int>())
            .Returns(new List<NotificationViewModel>());

        await Handler.Handle(new GetUserNotificationsQuery(), CancellationToken.None);

        await UnitOfWork.Notifications.Received(1).GetUserNotifications(UserId, WorkspaceId);
    }
}

public class GetUnreadCountQueryHandlerTests
{
    private readonly GetUnreadCountQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    private const string UserId = "user-id";
    private const int WorkspaceId = 1;

    public GetUnreadCountQueryHandlerTests()
    {
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.GetWorkspaceId().Returns(WorkspaceId);

        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetUnreadCount_ShouldReturnSuccess_WithCount()
    {
        UnitOfWork.Notifications.GetUnreadCount(UserId, WorkspaceId).Returns(5);

        var result = await Handler.Handle(new GetUnreadCountQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().Be(5);
    }
}

public class MarkAsReadCommandHandlerTests
{
    private readonly MarkAsReadCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    private const string UserId = "user-id";
    private const int WorkspaceId = 1;

    public MarkAsReadCommandHandlerTests()
    {
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.GetWorkspaceId().Returns(WorkspaceId);

        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnSuccess_WhenNotificationBelongsToUser()
    {
        var notification = AutoFixtures.Notification with { UserId = UserId };

        UnitOfWork.Notifications.GetAsync(notification.Id).Returns(notification);

        var result = await Handler.Handle(new MarkAsReadCommand(notification.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnNotFound_WhenNotificationDoesNotExist()
    {
        UnitOfWork.Notifications.GetAsync(Arg.Any<int>()).ReturnsNull();

        var result = await Handler.Handle(new MarkAsReadCommand(42), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await UnitOfWork.DidNotReceive().CompleteAsync();
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnNotFound_WhenNotificationBelongsToDifferentUser()
    {
        var notification = AutoFixtures.Notification with { UserId = "different-user-id" };

        UnitOfWork.Notifications.GetAsync(notification.Id).Returns(notification);

        var result = await Handler.Handle(new MarkAsReadCommand(notification.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        notification.IsRead.Should().BeFalse();
        await UnitOfWork.DidNotReceive().CompleteAsync();
    }
}

public class MarkAllAsReadCommandHandlerTests
{
    private readonly MarkAllAsReadCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    private const string UserId = "user-id";
    private const int WorkspaceId = 1;

    public MarkAllAsReadCommandHandlerTests()
    {
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.GetWorkspaceId().Returns(WorkspaceId);

        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task MarkAllAsRead_ShouldReturnSuccess_AndCallRepository()
    {
        UnitOfWork.Notifications
            .MarkAllAsRead(UserId, WorkspaceId)
            .Returns(Task.CompletedTask);

        var result = await Handler.Handle(new MarkAllAsReadCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await UnitOfWork.Notifications.Received(1).MarkAllAsRead(UserId, WorkspaceId);
    }
}
