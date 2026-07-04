using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.BoardGroups.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.BoardGroups.Commands;

public class CreateBoardGroupCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly CreateBoardGroupCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public CreateBoardGroupCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<AddBoardGroupRequest>().Without(p => p.StatusId).Create();
        var board = AutoFixtures.Board;

        UnitOfWork.Boards.GetAsync(request.BoardId!.Value, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(board);
        UnitOfWork.BoardGroups.AddAsync(Arg.Any<BoardGroup>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<BoardGroup>());
        UnitOfWork.BoardGroups.GetBoardGroupDefaultSortOrder(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(0);

        var result = await Handler.Handle(new CreateBoardGroupCommand(request), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.SortOrder.Should().Be(request.SortOrder);
    }

    [Fact]
    public async Task Create_CallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<AddBoardGroupRequest>().Without(p => p.StatusId).Create();

        UnitOfWork.Boards.GetAsync(request.BoardId!.Value, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.Board);
        UnitOfWork.BoardGroups.AddAsync(Arg.Any<BoardGroup>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<BoardGroup>());
        UnitOfWork.BoardGroups.GetBoardGroupDefaultSortOrder(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(0);

        await Handler.Handle(new CreateBoardGroupCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ShouldAssignStatus_WhenStatusValid()
    {
        var request = Fixture.Build<AddBoardGroupRequest>().With(p => p.StatusId, 5).Create();

        UnitOfWork.Boards.GetAsync(request.BoardId!.Value, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.Board);
        UnitOfWork.BoardGroups.AddAsync(Arg.Any<BoardGroup>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<BoardGroup>());
        UnitOfWork.BoardGroups.GetBoardGroupDefaultSortOrder(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(0);
        UnitOfWork.Statuses.GetInWorkspace(5, Arg.Any<int>(), Arg.Any<bool>(), TestContext.Current.CancellationToken)
            .Returns(AutoFixtures.TaskStatus with { Id = 5 });

        var result = await Handler.Handle(new CreateBoardGroupCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.StatusId.Should().Be(5);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenStatusInvalid()
    {
        var request = Fixture.Build<AddBoardGroupRequest>().With(p => p.StatusId, 5).Create();

        UnitOfWork.Boards.GetAsync(request.BoardId!.Value, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.Board);
        UnitOfWork.Statuses.GetInWorkspace(5, Arg.Any<int>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new CreateBoardGroupCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenBoardNotFound()
    {
        var request = Fixture.Build<AddBoardGroupRequest>().Without(p => p.StatusId).Create();

        UnitOfWork.Boards.GetAsync(request.BoardId!.Value, Arg.Any<bool>(), TestContext.Current.CancellationToken).ReturnsNull();
        UnitOfWork.BoardGroups.AddAsync(Arg.Any<BoardGroup>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<BoardGroup>());
        UnitOfWork.BoardGroups.GetBoardGroupDefaultSortOrder(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(0);

        var result = await Handler.Handle(new CreateBoardGroupCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }
}
