using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;
using Netptune.Services.Boards.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Boards.Queries;

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
