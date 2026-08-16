using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.BoardGroups.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.BoardGroups.Queries;

public class GetBoardGroupQueryHandlerTests
{
    private const int WorkspaceId = 7;

    private readonly GetBoardGroupQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetBoardGroupQueryHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);

        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetBoardGroup_ShouldReturnCorrectly_WhenInputValid()
    {
        var boardGroup = AutoFixtures.BoardGroup;

        UnitOfWork.BoardGroups.GetInWorkspace(Arg.Any<int>(), WorkspaceId, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(boardGroup);

        var result = await Handler.Handle(new GetBoardGroupQuery(1), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(boardGroup);
    }
}
