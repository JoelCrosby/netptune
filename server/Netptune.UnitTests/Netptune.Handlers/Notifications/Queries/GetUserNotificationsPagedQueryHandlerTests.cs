using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Notifications;
using Netptune.Handlers.Notifications.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Notifications.Queries;

public class GetUserNotificationsPagedQueryHandlerTests
{
    private readonly GetUserNotificationsPagedQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    private const string UserId = "user-id";
    private const int WorkspaceId = 1;

    public GetUserNotificationsPagedQueryHandlerTests()
    {
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.GetWorkspaceId().Returns(WorkspaceId);

        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetUserNotificationsPaged_ShouldReturnPagedResponse()
    {
        var notifications = new List<NotificationViewModel> { new(), new() };

        UnitOfWork.Notifications
            .GetUserNotifications(
                UserId,
                WorkspaceId,
                new PageRequest { Page = 1, PageSize = 25 }.GetPagination(),
                null,
                null,
                TestContext.Current.CancellationToken)
            .Returns(notifications);
        UnitOfWork.Notifications
            .GetUserNotificationsCount(UserId, WorkspaceId, null, null, TestContext.Current.CancellationToken).Returns(40);

        var query = new GetUserNotificationsPagedQuery(new NotificationFilter { Page = 1, PageSize = 25 });

        var result = await Handler.Handle(query, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().HaveCount(2);
        result.Payload.Page.Should().Be(1);
        result.Payload.PageSize.Should().Be(25);
        result.Payload.TotalCount.Should().Be(40);
        result.Payload.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetUserNotificationsPaged_ShouldQueryWithCorrectSkipAndTake()
    {
        UnitOfWork.Notifications
            .GetUserNotifications(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<Pagination>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                TestContext.Current.CancellationToken)
            .Returns(new List<NotificationViewModel>());

        var query = new GetUserNotificationsPagedQuery(new NotificationFilter { Page = 3, PageSize = 10 });

        await Handler.Handle(query, TestContext.Current.CancellationToken);

        await UnitOfWork.Notifications.Received(1)
            .GetUserNotifications(
                UserId,
                WorkspaceId,
                new PageRequest { Page = 3, PageSize = 10 }.GetPagination(),
                null,
                null,
                TestContext.Current.CancellationToken);
        await UnitOfWork.Notifications.Received(1)
            .GetUserNotificationsCount(UserId, WorkspaceId, null, null, TestContext.Current.CancellationToken);
    }
}
