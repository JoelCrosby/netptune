using FluentAssertions;

using Netptune.Core.Models;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Users.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Users.Queries;

public class GetWorkspaceMembersByIdQueryHandlerTests
{
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly GetWorkspaceMembersByIdQueryHandler Handler;

    public GetWorkspaceMembersByIdQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task Handle_ShouldReadTheMembersFromTheCurrentWorkspace()
    {
        var member = new UserAvatar { Id = "u1", DisplayName = "Ada" };

        Identity.GetWorkspaceId().Returns(7);
        UnitOfWork.Users
            .GetUserAvatars(Arg.Is<IEnumerable<string>>(ids => ids.Single() == "u1"), 7, TestContext.Current.CancellationToken)
            .Returns([member]);

        var result = await Handler.Handle(new GetWorkspaceMembersByIdQuery(["u1"]), TestContext.Current.CancellationToken);

        result.Should().Equal(member);
    }

    [Fact]
    public async Task Handle_ShouldNotQuery_WhenNoIdsAreGiven()
    {
        var result = await Handler.Handle(new GetWorkspaceMembersByIdQuery([]), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
        await UnitOfWork.Users.DidNotReceiveWithAnyArgs().GetUserAvatars(default!, default, default);
    }
}
