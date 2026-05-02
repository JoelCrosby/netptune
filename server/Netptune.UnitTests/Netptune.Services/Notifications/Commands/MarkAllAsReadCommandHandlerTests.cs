using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Notifications.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Notifications.Commands;

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
            .MarkAllAsRead(UserId, WorkspaceId, TestContext.Current.CancellationToken).Returns(Task.CompletedTask);

        var result = await Handler.Handle(new MarkAllAsReadCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await UnitOfWork.Notifications.Received(1).MarkAllAsRead(UserId, WorkspaceId, TestContext.Current.CancellationToken);
    }
}
