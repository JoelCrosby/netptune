using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class BoardServiceUnitTests
{
    private readonly BoardService Service;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public BoardServiceUnitTests()
    {
        Service = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetBoards_ShouldReturnCorrectly_WhenInputValid()
    {
        var boards = new List<Board> { AutoFixtures.Board };

        UnitOfWork.Boards.GetBoardsInProject(Arg.Any<int>(), Arg.Any<bool>()).Returns(boards);

        var result = await Service.GetBoards(1);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetBoard_ShouldReturnCorrectly_WhenInputValid()
    {
        var board = AutoFixtures.Board;

        UnitOfWork.Boards.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(board);

        var result = await Service.GetBoard(1);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetBoard_ShouldReturnFailure_WhenNotFound()
    {
        UnitOfWork.Boards.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).ReturnsNull();

        var result = await Service.GetBoard(1);

        result.IsSuccess.Should().BeFalse();
    }
}
