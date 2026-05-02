using FluentAssertions;

using Netptune.Core.Relationships;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Users.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Users.Queries;

public class GetWorkspaceUsersQueryHandlerTests
{
    private readonly GetWorkspaceUsersQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetWorkspaceUsersQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldReturnCorrectly_WhenInputValid()
    {
        const string workspaceKey = "workspaceKey";
        var users = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetWorkspaceAppUsers(workspaceKey, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(users);

        var result = await Handler.Handle(new GetWorkspaceUsersQuery(), TestContext.Current.CancellationToken);

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldReturnEmpty_WhenNoUsers()
    {
        const string workspaceKey = "workspaceKey";

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetWorkspaceAppUsers(workspaceKey, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns([]);

        var result = await Handler.Handle(new GetWorkspaceUsersQuery(), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }
}
