using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;
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

    [Fact]
    public async Task GetBoardView_ShouldReturnCorrectly_WhenInputValid()
    {
        const string identifier = "board";
        const int workspaceId = 1;
        const int boardId = 1;

        var boardViewModel = AutoFixtures.BoardViewModel;
        var groups = new List<BoardViewGroup> { AutoFixtures.BoardViewGroup };
        var users = new List<AppUser> { AutoFixtures.AppUser };

        var filter = new BoardGroupsFilter();

        Identity.GetWorkspaceId().Returns(workspaceId);
        UnitOfWork.Boards.GetIdByIdentifier(identifier, workspaceId).Returns(boardId);
        UnitOfWork.BoardGroups.GetBoardViewGroups(boardId, Arg.Any<string>()).Returns(groups);
        UnitOfWork.Boards.GetViewModel(boardId, Arg.Any<bool>()).Returns(boardViewModel);
        UnitOfWork.Users.GetAllByIdAsync(Arg.Any<List<string>>(), Arg.Any<bool>()).Returns(users);

        var result = await Service.GetBoardView(identifier, filter);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetBoardView_ShouldFailure_WhenBoardNotFound()
    {
        const string identifier = "board";
        const int workspaceId = 1;

        var boardViewModel = AutoFixtures.BoardViewModel;
        var groups = new List<BoardViewGroup> { AutoFixtures.BoardViewGroup };
        var users = new List<AppUser> { AutoFixtures.AppUser };

        var filter = new BoardGroupsFilter();

        Identity.GetWorkspaceId().Returns(workspaceId);
        UnitOfWork.Boards.GetIdByIdentifier(identifier, workspaceId).ReturnsNull();
        UnitOfWork.BoardGroups.GetBoardViewGroups(Arg.Any<int>(), Arg.Any<string>()).Returns(groups);
        UnitOfWork.Boards.GetViewModel(Arg.Any<int>(), Arg.Any<bool>()).Returns(boardViewModel);
        UnitOfWork.Users.GetAllByIdAsync(Arg.Any<List<string>>(), Arg.Any<bool>()).Returns(users);

        var result = await Service.GetBoardView(identifier, filter);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetBoardView_ShouldFailure_WhenBoardViewNotFound()
    {
        const string identifier = "board";
        const int workspaceId = 1;
        const int boardId = 1;

        var groups = new List<BoardViewGroup> { AutoFixtures.BoardViewGroup };
        var users = new List<AppUser> { AutoFixtures.AppUser };

        var filter = new BoardGroupsFilter();

        Identity.GetWorkspaceId().Returns(workspaceId);
        UnitOfWork.Boards.GetIdByIdentifier(identifier, workspaceId).Returns(boardId);
        UnitOfWork.BoardGroups.GetBoardViewGroups(Arg.Any<int>(), Arg.Any<string>()).Returns(groups);
        UnitOfWork.Boards.GetViewModel(Arg.Any<int>(), Arg.Any<bool>()).ReturnsNull();
        UnitOfWork.Users.GetAllByIdAsync(Arg.Any<List<string>>(), Arg.Any<bool>()).Returns(users);

        var result = await Service.GetBoardView(identifier, filter);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetBoardView_ShouldFailure_WhenGroupsNotFound()
    {
        const string identifier = "board";
        const int workspaceId = 1;
        const int boardId = 1;

        var boardViewModel = AutoFixtures.BoardViewModel;
        var users = new List<AppUser> { AutoFixtures.AppUser };

        var filter = new BoardGroupsFilter();

        Identity.GetWorkspaceId().Returns(workspaceId);
        UnitOfWork.Boards.GetIdByIdentifier(identifier, workspaceId).Returns(boardId);
        UnitOfWork.BoardGroups.GetBoardViewGroups(Arg.Any<int>(), Arg.Any<string>()).ReturnsNull();
        UnitOfWork.Boards.GetViewModel(Arg.Any<int>(), Arg.Any<bool>()).Returns(boardViewModel);
        UnitOfWork.Users.GetAllByIdAsync(Arg.Any<List<string>>(), Arg.Any<bool>()).Returns(users);

        var result = await Service.GetBoardView(identifier, filter);

        result.IsSuccess.Should().BeFalse();
    }
}
