using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Workspaces.Queries.GetUserWorkspaces;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Workspaces.Queries;

public class GetUserWorkspacesQueryHandlerTests
{
    private readonly GetUserWorkspacesQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetUserWorkspacesQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetUserWorkspaces_ShouldReturnCorrectly_WhenInputValid()
    {
        var workspaces = new List<Workspace> { AutoFixtures.Workspace };
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Workspaces.GetUserWorkspaces("userId").Returns(workspaces);

        var result = await Handler.Handle(new GetUserWorkspacesQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(workspaces);
    }
}
