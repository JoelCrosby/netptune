using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Notifications.Commands.MarkAsRead;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Notifications.Commands;

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
