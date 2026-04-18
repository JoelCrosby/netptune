using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Notifications;
using Netptune.Services;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class NotificationServiceTests
{
    private readonly NotificationService Service;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    private const string UserId = "user-id";
    private const int WorkspaceId = 1;

    public NotificationServiceTests()
    {
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.GetWorkspaceId().Returns(WorkspaceId);

        Service = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetUserNotifications_ShouldReturnSuccess_WithNotifications()
    {
        var notifications = new List<NotificationViewModel> { new(), new() };

        UnitOfWork.Notifications
            .GetUserNotifications(UserId, WorkspaceId)
            .Returns(notifications);

        var result = await Service.GetUserNotifications();

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserNotifications_ShouldQueryWithCorrectUserAndWorkspace()
    {
        UnitOfWork.Notifications
            .GetUserNotifications(Arg.Any<string>(), Arg.Any<int>())
            .Returns(new List<NotificationViewModel>());

        await Service.GetUserNotifications();

        await UnitOfWork.Notifications.Received(1).GetUserNotifications(UserId, WorkspaceId);
    }

    [Fact]
    public async Task GetUnreadCount_ShouldReturnSuccess_WithCount()
    {
        UnitOfWork.Notifications.GetUnreadCount(UserId, WorkspaceId).Returns(5);

        var result = await Service.GetUnreadCount();

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().Be(5);
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnSuccess_WhenNotificationBelongsToUser()
    {
        var notification = AutoFixtures.Notification with { UserId = UserId };

        UnitOfWork.Notifications.GetAsync(notification.Id).Returns(notification);

        var result = await Service.MarkAsRead(notification.Id);

        result.IsSuccess.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnNotFound_WhenNotificationDoesNotExist()
    {
        UnitOfWork.Notifications.GetAsync(Arg.Any<int>()).ReturnsNull();

        var result = await Service.MarkAsRead(42);

        result.IsSuccess.Should().BeFalse();
        await UnitOfWork.DidNotReceive().CompleteAsync();
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnNotFound_WhenNotificationBelongsToDifferentUser()
    {
        var notification = AutoFixtures.Notification with { UserId = "different-user-id" };

        UnitOfWork.Notifications.GetAsync(notification.Id).Returns(notification);

        var result = await Service.MarkAsRead(notification.Id);

        result.IsSuccess.Should().BeFalse();
        notification.IsRead.Should().BeFalse();
        await UnitOfWork.DidNotReceive().CompleteAsync();
    }

    [Fact]
    public async Task MarkAllAsRead_ShouldReturnSuccess_AndCallRepository()
    {
        UnitOfWork.Notifications
            .MarkAllAsRead(UserId, WorkspaceId)
            .Returns(Task.CompletedTask);

        var result = await Service.MarkAllAsRead();

        result.IsSuccess.Should().BeTrue();
        await UnitOfWork.Notifications.Received(1).MarkAllAsRead(UserId, WorkspaceId);
    }
}
