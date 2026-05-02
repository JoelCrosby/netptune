using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;
using Netptune.Services.Boards.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Boards.Queries;

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
        UnitOfWork.Boards.GetIdByIdentifier(identifier, workspaceId, TestContext.Current.CancellationToken).Returns(boardId);
        UnitOfWork.BoardGroups.GetBoardViewGroups(boardId, Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(groups);
        UnitOfWork.Boards.GetViewModel(boardId, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(boardViewModel);
        UnitOfWork.Users.GetAllByIdAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(users);

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
        UnitOfWork.Boards.GetIdByIdentifier(identifier, workspaceId, TestContext.Current.CancellationToken).ReturnsNull();
        UnitOfWork.BoardGroups.GetBoardViewGroups(Arg.Any<int>(), Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(new List<BoardViewGroup> { AutoFixtures.BoardViewGroup });
        UnitOfWork.Boards.GetViewModel(Arg.Any<int>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.BoardViewModel);
        UnitOfWork.Users.GetAllByIdAsync(Arg.Any<List<string>>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(new List<AppUser> { AutoFixtures.AppUser });

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
        UnitOfWork.Boards.GetIdByIdentifier(identifier, workspaceId, TestContext.Current.CancellationToken).Returns(boardId);
        UnitOfWork.BoardGroups.GetBoardViewGroups(Arg.Any<int>(), Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(new List<BoardViewGroup> { AutoFixtures.BoardViewGroup });
        UnitOfWork.Boards.GetViewModel(Arg.Any<int>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).ReturnsNull();
        UnitOfWork.Users.GetAllByIdAsync(Arg.Any<List<string>>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(new List<AppUser> { AutoFixtures.AppUser });

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
        UnitOfWork.Boards.GetIdByIdentifier(identifier, workspaceId, TestContext.Current.CancellationToken).Returns(boardId);
        UnitOfWork.BoardGroups.GetBoardViewGroups(Arg.Any<int>(), Arg.Any<string>(), TestContext.Current.CancellationToken).ReturnsNull();
        UnitOfWork.Boards.GetViewModel(Arg.Any<int>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.BoardViewModel);
        UnitOfWork.Users.GetAllByIdAsync(Arg.Any<List<string>>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(new List<AppUser> { AutoFixtures.AppUser });

        var result = await Handler.Handle(new GetBoardViewQuery(identifier, filter), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
