using FluentAssertions;

using Netptune.Core.UnitOfWork;
using Netptune.Services.Workspaces.Queries.GetWorkspace;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Workspaces.Queries;

public class GetWorkspaceQueryHandlerTests
{
    private readonly GetWorkspaceQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public GetWorkspaceQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task GetWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        var workspace = AutoFixtures.Workspace;
        UnitOfWork.Workspaces.GetBySlug("slug").Returns(workspace);

        var result = await Handler.Handle(new GetWorkspaceQuery("slug"), CancellationToken.None);

        result.Should().BeEquivalentTo(workspace);
    }
}
