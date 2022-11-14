using AutoFixture;

using FluentAssertions;

using Netptune.Core.Encoding;
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
    private readonly Fixture Fixture = new();

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

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<UpdateBoardRequest>()
            .Create();

        var board = AutoFixtures.Board;

        UnitOfWork.Boards.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(board);

        var result = await Service.Update(request);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Identifier.Should().Be(request.Identifier!.ToUrlSlug());
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture
            .Build<UpdateBoardRequest>()
            .Create();

        var board = AutoFixtures.Board;

        UnitOfWork.Boards.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(board);

        await Service.Update(request);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenNotFound()
    {
        var request = Fixture
            .Build<UpdateBoardRequest>()
            .Create();

        UnitOfWork.Boards.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).ReturnsNull();

        var result = await Service.Update(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<AddBoardRequest>()
            .Create();

        var project = AutoFixtures.Project;

        UnitOfWork.Boards.AddAsync(Arg.Any<Board>()).Returns(x => x.Arg<Board>());
        UnitOfWork.Projects.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(project);

        var result = await Service.Create(request);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Identifier.Should().Be(request.Identifier.ToUrlSlug());
    }

    [Fact]
    public async Task Create_CallCompleteAsync_WhenInputValid()
    {
        var request = Fixture
            .Build<AddBoardRequest>()
            .Create();

        var project = AutoFixtures.Project;

        UnitOfWork.Boards.AddAsync(Arg.Any<Board>()).Returns(x => x.Arg<Board>());
        UnitOfWork.Projects.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(project);

        await Service.Create(request);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenProjectNotFound()
    {
        var request = Fixture
            .Build<AddBoardRequest>()
            .Create();

        UnitOfWork.Boards.AddAsync(Arg.Any<Board>()).Returns(x => x.Arg<Board>());
        UnitOfWork.Projects.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).ReturnsNull();

        var result = await Service.Create(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        var board = AutoFixtures.Board;

        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetAsync(1).Returns(board);

        var result = await Service.Delete(1);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        var board = AutoFixtures.Board;

        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetAsync(1).Returns(board);

        await Service.Delete(1);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetAsync(1).ReturnsNull();

        var result = await Service.Delete(1);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallDeletePermanent_WhenValidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetAsync(1).ReturnsNull();

        await Service.Delete(1);

        await UnitOfWork.Boards.Received(0).DeletePermanent(Arg.Any<int>());
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenValidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetAsync(1).ReturnsNull();

        await Service.Delete(1);

        await UnitOfWork.Received(0).CompleteAsync();
    }

    [Fact]
    public async Task GetBoardsInWorkspace_ShouldReturnCorrectly_WhenValidId()
    {
        var viewModels = new List<BoardsViewModel> { AutoFixtures.BoardsViewModel };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.Exists("key").Returns(true);
        UnitOfWork.Boards.GetBoardViewModels("key").Returns(viewModels);

        var result = await Service.GetBoardsInWorkspace();

        result.Should().NotBeEmpty();
        result.Should().BeEquivalentTo(viewModels);
    }

    [Fact]
    public async Task GetBoardsInWorkspace_ShouldReturnNull_WhenWorkspaceNotExists()
    {
        var viewModels = new List<BoardsViewModel> { AutoFixtures.BoardsViewModel };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.Exists("key").Returns(false);
        UnitOfWork.Boards.GetBoardViewModels("key").Returns(viewModels);

        var result = await Service.GetBoardsInWorkspace();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBoardsInProject_ShouldReturnCorrectly_WhenValidId()
    {
        var boards = new List<Board> { AutoFixtures.Board };

        UnitOfWork.Boards.GetBoardsInProject(1, Arg.Any<bool>()).Returns(boards);

        var result = await Service.GetBoardsInProject(1);

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task IsIdentifierUnique_ShouldReturnCorrectly_WhenNotExists()
    {
        UnitOfWork.Boards.Exists("identifier").Returns(false);

        var result = await Service.IsIdentifierUnique("identifier");

        result.IsSuccess.Should().BeTrue();
        result.Payload!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task IsIdentifierUnique_ShouldReturnCorrectly_WhenExists()
    {
        UnitOfWork.Boards.Exists("identifier").Returns(true);

        var result = await Service.IsIdentifierUnique("identifier");

        result.IsSuccess.Should().BeTrue();
        result.Payload!.IsUnique.Should().BeFalse();
    }
}
