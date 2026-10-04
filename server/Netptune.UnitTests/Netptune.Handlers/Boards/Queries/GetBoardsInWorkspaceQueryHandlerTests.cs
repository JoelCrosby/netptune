using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;
using Netptune.Handlers.Boards.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Boards.Queries;

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
        var filter = new BoardFilter { Search = "neo" };
        var page = new PagedResponse<BoardViewModel>([AutoFixtures.BoardViewModel], 1, 50, 1);

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.Exists("key", TestContext.Current.CancellationToken).Returns(true);
        UnitOfWork.Boards.GetBoardsPage("key", filter, TestContext.Current.CancellationToken).Returns(page);

        var result = await Handler.Handle(new GetBoardsInWorkspaceQuery(filter), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().BeEquivalentTo(page);
    }

    [Fact]
    public async Task GetBoardsInWorkspace_ShouldReturnNotFound_WhenWorkspaceNotExists()
    {
        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.Exists("key", TestContext.Current.CancellationToken).Returns(false);

        var result = await Handler.Handle(new GetBoardsInWorkspaceQuery(new BoardFilter()), TestContext.Current.CancellationToken);

        result.IsNotFound.Should().BeTrue();
    }
}
