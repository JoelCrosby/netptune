using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Notifications;
using Netptune.Services.Notifications.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Notifications.Queries;

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
