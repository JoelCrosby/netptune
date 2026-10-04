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
    private const string WorkspaceKey = "workspace";

    private readonly GetBoardQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetBoardQueryHandlerTests()
    {
        Identity.GetWorkspaceKey().Returns(WorkspaceKey);

        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetBoard_ShouldReturnCorrectly_WhenInputValid()
    {
        var board = AutoFixtures.BoardViewModel;

        UnitOfWork.Boards.GetWorkspaceBoardViewModel(WorkspaceKey, 1, TestContext.Current.CancellationToken).Returns(board);

        var result = await Handler.Handle(new GetBoardQuery(1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().BeEquivalentTo(board);
    }

    [Fact]
    public async Task GetBoard_ShouldReturnFailure_WhenNotFound()
    {
        UnitOfWork.Boards.GetWorkspaceBoardViewModel(WorkspaceKey, Arg.Any<int>(), TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new GetBoardQuery(1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }
}
