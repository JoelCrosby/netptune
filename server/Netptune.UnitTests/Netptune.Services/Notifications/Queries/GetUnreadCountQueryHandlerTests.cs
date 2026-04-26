using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Notifications.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Notifications.Queries;

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
