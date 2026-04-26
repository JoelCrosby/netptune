using AutoFixture;

using FluentAssertions;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;
using Netptune.Services.Boards.Commands;
using Netptune.Services.Boards.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class GetBoardQueryHandlerTests
{
    private readonly GetBoardQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public GetBoardQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task GetBoard_ShouldReturnCorrectly_WhenInputValid()
    {
        var board = AutoFixtures.Board;
        UnitOfWork.Boards.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(board);

        var result = await Handler.Handle(new GetBoardQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetBoard_ShouldReturnFailure_WhenNotFound()
    {
        UnitOfWork.Boards.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).ReturnsNull();

        var result = await Handler.Handle(new GetBoardQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}

public class GetBoardViewQueryHandlerTests
{
    private readonly GetBoardViewQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetBoardViewQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
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
        var filter = BoardGroupsFilter.Empty();

        Identity.GetWorkspaceId().Returns(workspaceId);
        UnitOfWork.Boards.GetIdByIdentifier(identifier, workspaceId).Returns(boardId);
        UnitOfWork.BoardGroups.GetBoardViewGroups(boardId, Arg.Any<string>()).Returns(groups);
        UnitOfWork.Boards.GetViewModel(boardId, Arg.Any<bool>()).Returns(boardViewModel);
        UnitOfWork.Users.GetAllByIdAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);

        var result = await Handler.Handle(new GetBoardViewQuery(identifier, filter), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetBoardView_ShouldFailure_WhenBoardNotFound()
    {
        const string identifier = "board";
        const int workspaceId = 1;
        var filter = BoardGroupsFilter.Empty();

        Identity.GetWorkspaceId().Returns(workspaceId);
        UnitOfWork.Boards.GetIdByIdentifier(identifier, workspaceId).ReturnsNull();
        UnitOfWork.BoardGroups.GetBoardViewGroups(Arg.Any<int>(), Arg.Any<string>()).Returns(new List<BoardViewGroup> { AutoFixtures.BoardViewGroup });
        UnitOfWork.Boards.GetViewModel(Arg.Any<int>(), Arg.Any<bool>()).Returns(AutoFixtures.BoardViewModel);
        UnitOfWork.Users.GetAllByIdAsync(Arg.Any<List<string>>(), Arg.Any<bool>()).Returns(new List<AppUser> { AutoFixtures.AppUser });

        var result = await Handler.Handle(new GetBoardViewQuery(identifier, filter), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetBoardView_ShouldFailure_WhenBoardViewNotFound()
    {
        const string identifier = "board";
        const int workspaceId = 1;
        const int boardId = 1;
        var filter = BoardGroupsFilter.Empty();

        Identity.GetWorkspaceId().Returns(workspaceId);
        UnitOfWork.Boards.GetIdByIdentifier(identifier, workspaceId).Returns(boardId);
        UnitOfWork.BoardGroups.GetBoardViewGroups(Arg.Any<int>(), Arg.Any<string>()).Returns(new List<BoardViewGroup> { AutoFixtures.BoardViewGroup });
        UnitOfWork.Boards.GetViewModel(Arg.Any<int>(), Arg.Any<bool>()).ReturnsNull();
        UnitOfWork.Users.GetAllByIdAsync(Arg.Any<List<string>>(), Arg.Any<bool>()).Returns(new List<AppUser> { AutoFixtures.AppUser });

        var result = await Handler.Handle(new GetBoardViewQuery(identifier, filter), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetBoardView_ShouldFailure_WhenGroupsNotFound()
    {
        const string identifier = "board";
        const int workspaceId = 1;
        const int boardId = 1;
        var filter = BoardGroupsFilter.Empty();

        Identity.GetWorkspaceId().Returns(workspaceId);
        UnitOfWork.Boards.GetIdByIdentifier(identifier, workspaceId).Returns(boardId);
        UnitOfWork.BoardGroups.GetBoardViewGroups(Arg.Any<int>(), Arg.Any<string>()).ReturnsNull();
        UnitOfWork.Boards.GetViewModel(Arg.Any<int>(), Arg.Any<bool>()).Returns(AutoFixtures.BoardViewModel);
        UnitOfWork.Users.GetAllByIdAsync(Arg.Any<List<string>>(), Arg.Any<bool>()).Returns(new List<AppUser> { AutoFixtures.AppUser });

        var result = await Handler.Handle(new GetBoardViewQuery(identifier, filter), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}

public class UpdateBoardCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateBoardCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public UpdateBoardCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateBoardRequest>().Create();
        var board = AutoFixtures.Board;

        UnitOfWork.Boards.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(board);

        var result = await Handler.Handle(new UpdateBoardCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Identifier.Should().Be(request.Identifier!.ToUrlSlug());
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<UpdateBoardRequest>().Create();
        UnitOfWork.Boards.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(AutoFixtures.Board);

        await Handler.Handle(new UpdateBoardCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenNotFound()
    {
        var request = Fixture.Build<UpdateBoardRequest>().Create();
        UnitOfWork.Boards.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).ReturnsNull();

        var result = await Handler.Handle(new UpdateBoardCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}

public class CreateBoardCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly CreateBoardCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public CreateBoardCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<AddBoardRequest>().Create();
        var project = AutoFixtures.Project;

        UnitOfWork.Boards.AddAsync(Arg.Any<Board>()).Returns(x => x.Arg<Board>());
        UnitOfWork.Projects.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(project);

        var result = await Handler.Handle(new CreateBoardCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Identifier.Should().Be(request.Identifier.ToUrlSlug());
    }

    [Fact]
    public async Task Create_CallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<AddBoardRequest>().Create();

        UnitOfWork.Boards.AddAsync(Arg.Any<Board>()).Returns(x => x.Arg<Board>());
        UnitOfWork.Projects.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(AutoFixtures.Project);

        await Handler.Handle(new CreateBoardCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenProjectNotFound()
    {
        var request = Fixture.Build<AddBoardRequest>().Create();

        UnitOfWork.Boards.AddAsync(Arg.Any<Board>()).Returns(x => x.Arg<Board>());
        UnitOfWork.Projects.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).ReturnsNull();

        var result = await Handler.Handle(new CreateBoardCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}

public class DeleteBoardCommandHandlerTests
{
    private readonly DeleteBoardCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteBoardCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetAsync(1).Returns(AutoFixtures.Board);

        var result = await Handler.Handle(new DeleteBoardCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetAsync(1).Returns(AutoFixtures.Board);

        await Handler.Handle(new DeleteBoardCommand(1), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetAsync(1).ReturnsNull();

        var result = await Handler.Handle(new DeleteBoardCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallDeletePermanent_WhenInvalidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetAsync(1).ReturnsNull();

        await Handler.Handle(new DeleteBoardCommand(1), CancellationToken.None);

        await UnitOfWork.Boards.Received(0).DeletePermanent(Arg.Any<int>());
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenInvalidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetAsync(1).ReturnsNull();

        await Handler.Handle(new DeleteBoardCommand(1), CancellationToken.None);

        await UnitOfWork.Received(0).CompleteAsync();
    }
}

public class GetBoardsInWorkspaceQueryHandlerTests
{
    private readonly GetBoardsInWorkspaceQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetBoardsInWorkspaceQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetBoardsInWorkspace_ShouldReturnCorrectly_WhenValidId()
    {
        var viewModels = new List<BoardsViewModel> { AutoFixtures.BoardsViewModel };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.Exists("key").Returns(true);
        UnitOfWork.Boards.GetBoardViewModels("key").Returns(viewModels);

        var result = await Handler.Handle(new GetBoardsInWorkspaceQuery(), CancellationToken.None);

        result.Should().NotBeEmpty();
        result.Should().BeEquivalentTo(viewModels);
    }

    [Fact]
    public async Task GetBoardsInWorkspace_ShouldReturnNull_WhenWorkspaceNotExists()
    {
        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.Exists("key").Returns(false);

        var result = await Handler.Handle(new GetBoardsInWorkspaceQuery(), CancellationToken.None);

        result.Should().BeNull();
    }
}

public class GetBoardsInProjectQueryHandlerTests
{
    private readonly GetBoardsInProjectQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public GetBoardsInProjectQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task GetBoardsInProject_ShouldReturnCorrectly_WhenValidId()
    {
        var boards = new List<Board> { AutoFixtures.Board };

        UnitOfWork.Boards.GetBoardsInProject(1, Arg.Any<bool>()).Returns(boards);

        var result = await Handler.Handle(new GetBoardsInProjectQuery(1), CancellationToken.None);

        result.Should().NotBeEmpty();
    }
}

public class IsBoardIdentifierUniqueQueryHandlerTests
{
    private readonly IsBoardIdentifierUniqueQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public IsBoardIdentifierUniqueQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task IsIdentifierUnique_ShouldReturnCorrectly_WhenNotExists()
    {
        UnitOfWork.Boards.Exists("identifier").Returns(false);

        var result = await Handler.Handle(new IsBoardIdentifierUniqueQuery("identifier"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task IsIdentifierUnique_ShouldReturnCorrectly_WhenExists()
    {
        UnitOfWork.Boards.Exists("identifier").Returns(true);

        var result = await Handler.Handle(new IsBoardIdentifierUniqueQuery("identifier"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.IsUnique.Should().BeFalse();
    }
}
