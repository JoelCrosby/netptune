using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Boards.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Boards.Queries;

public class GetBoardQueryHandlerTests
{
    private const int WorkspaceId = 7;

    private readonly GetBoardQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetBoardQueryHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);

        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetBoard_ShouldReturnCorrectly_WhenInputValid()
    {
        var board = AutoFixtures.Board;
        UnitOfWork.Boards.GetInWorkspace(Arg.Any<int>(), WorkspaceId, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(board);

        var result = await Handler.Handle(new GetBoardQuery(1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetBoard_ShouldReturnFailure_WhenNotFound()
    {
        UnitOfWork.Boards.GetInWorkspace(Arg.Any<int>(), WorkspaceId, Arg.Any<bool>(), TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new GetBoardQuery(1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }
}
