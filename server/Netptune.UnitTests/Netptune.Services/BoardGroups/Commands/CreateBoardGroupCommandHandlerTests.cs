using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.BoardGroups.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.BoardGroups.Commands;

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
        var request = Fixture.Build<AddBoardGroupRequest>().Create();
        var board = AutoFixtures.Board;

        UnitOfWork.Boards.GetAsync(request.BoardId!.Value, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(board);
        UnitOfWork.BoardGroups.AddAsync(Arg.Any<BoardGroup>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<BoardGroup>());
        UnitOfWork.BoardGroups.GetBoardGroupDefaultSortOrder(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(0);

        var result = await Handler.Handle(new CreateBoardGroupCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.SortOrder.Should().Be(request.SortOrder);
    }

    [Fact]
    public async Task Create_CallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<AddBoardGroupRequest>().Create();

        UnitOfWork.Boards.GetAsync(request.BoardId!.Value, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.Board);
        UnitOfWork.BoardGroups.AddAsync(Arg.Any<BoardGroup>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<BoardGroup>());
        UnitOfWork.BoardGroups.GetBoardGroupDefaultSortOrder(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(0);

        await Handler.Handle(new CreateBoardGroupCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenBoardNotFound()
    {
        var request = Fixture.Build<AddBoardGroupRequest>().Create();

        UnitOfWork.Boards.GetAsync(request.BoardId!.Value, Arg.Any<bool>(), TestContext.Current.CancellationToken).ReturnsNull();
        UnitOfWork.BoardGroups.AddAsync(Arg.Any<BoardGroup>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<BoardGroup>());
        UnitOfWork.BoardGroups.GetBoardGroupDefaultSortOrder(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(0);

        var result = await Handler.Handle(new CreateBoardGroupCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
