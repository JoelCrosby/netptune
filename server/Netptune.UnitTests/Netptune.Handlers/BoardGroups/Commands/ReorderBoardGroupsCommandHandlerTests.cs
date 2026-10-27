using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Localization;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.BoardGroups.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.BoardGroups.Commands;

public class ReorderBoardGroupsCommandHandlerTests
{
    private const int BoardId = 1;
    private const int WorkspaceId = 7;

    private readonly ReorderBoardGroupsCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly List<BoardGroup> Groups = [CreateGroup(10, 1), CreateGroup(11, 2), CreateGroup(12, 3)];

    public ReorderBoardGroupsCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);

        Identity.GetWorkspaceId().Returns(WorkspaceId);
        UnitOfWork.Boards
            .GetInWorkspace(BoardId, WorkspaceId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new Board { Id = BoardId, Name = "Netptune", Identifier = "netptune" });
        UnitOfWork.BoardGroups
            .GetBoardGroupsInBoard(BoardId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Groups);
    }

    [Fact]
    public async Task Handle_ShouldSortEveryGroup_WithOneSave()
    {
        var result = await Handler.Handle(
            new ReorderBoardGroupsCommand(BoardId, [12, 10, 11]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        Groups.OrderBy(group => group.SortOrder).Select(group => group.Id).Should().Equal(12, 10, 11);
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(new[] { 12, 10 })]
    [InlineData(new[] { 12, 10, 10 })]
    [InlineData(new[] { 12, 10, 99 })]
    public async Task Handle_ShouldChangeNothing_WhenTheOrderDoesNotListEveryGroupOnce(int[] groupIds)
    {
        var result = await Handler.Handle(
            new ReorderBoardGroupsCommand(BoardId, [.. groupIds]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(AppText.ErrorBoardGroupOrderMustListEveryGroup);
        Groups.Select(group => group.SortOrder).Should().Equal(1, 2, 3);
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTheBoardIsNotInTheWorkspace()
    {
        var result = await Handler.Handle(
            new ReorderBoardGroupsCommand(404, [10, 11, 12]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    private static BoardGroup CreateGroup(int id, double sortOrder)
    {
        return new BoardGroup { Id = id, Name = $"Group {id}", BoardId = BoardId, SortOrder = sortOrder };
    }
}
