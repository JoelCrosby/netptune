using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.BoardGroups.Commands;
using Netptune.Services.BoardGroups.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class GetBoardGroupQueryHandlerTests
{
    private readonly GetBoardGroupQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public GetBoardGroupQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task GetBoardGroup_ShouldReturnCorrectly_WhenInputValid()
    {
        var boardGroup = AutoFixtures.BoardGroup;

        UnitOfWork.BoardGroups.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(boardGroup);

        var result = await Handler.Handle(new GetBoardGroupQuery(1), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(boardGroup);
    }
}

public class UpdateBoardGroupCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateBoardGroupCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public UpdateBoardGroupCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateBoardGroupRequest>().Create();
        var boardGroup = AutoFixtures.BoardGroup;

        UnitOfWork.BoardGroups.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(boardGroup);

        var result = await Handler.Handle(new UpdateBoardGroupCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload!.SortOrder.Should().Be(request.SortOrder);
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<UpdateBoardGroupRequest>().Create();
        UnitOfWork.BoardGroups.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(AutoFixtures.BoardGroup);

        await Handler.Handle(new UpdateBoardGroupCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenNotFound()
    {
        var request = Fixture.Build<UpdateBoardGroupRequest>().Create();
        UnitOfWork.BoardGroups.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).ReturnsNull();

        var result = await Handler.Handle(new UpdateBoardGroupCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}

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

        UnitOfWork.Boards.GetAsync(request.BoardId!.Value, Arg.Any<bool>()).Returns(board);
        UnitOfWork.BoardGroups.AddAsync(Arg.Any<BoardGroup>()).Returns(x => x.Arg<BoardGroup>());
        UnitOfWork.BoardGroups.GetBoardGroupDefaultSortOrder(Arg.Any<int>()).Returns(0);

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

        UnitOfWork.Boards.GetAsync(request.BoardId!.Value, Arg.Any<bool>()).Returns(AutoFixtures.Board);
        UnitOfWork.BoardGroups.AddAsync(Arg.Any<BoardGroup>()).Returns(x => x.Arg<BoardGroup>());
        UnitOfWork.BoardGroups.GetBoardGroupDefaultSortOrder(Arg.Any<int>()).Returns(0);

        await Handler.Handle(new CreateBoardGroupCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenBoardNotFound()
    {
        var request = Fixture.Build<AddBoardGroupRequest>().Create();

        UnitOfWork.Boards.GetAsync(request.BoardId!.Value, Arg.Any<bool>()).ReturnsNull();
        UnitOfWork.BoardGroups.AddAsync(Arg.Any<BoardGroup>()).Returns(x => x.Arg<BoardGroup>());
        UnitOfWork.BoardGroups.GetBoardGroupDefaultSortOrder(Arg.Any<int>()).Returns(0);

        var result = await Handler.Handle(new CreateBoardGroupCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}

public class DeleteBoardGroupCommandHandlerTests
{
    private readonly DeleteBoardGroupCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteBoardGroupCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.BoardGroups.GetAsync(1).Returns(AutoFixtures.BoardGroup);

        var result = await Handler.Handle(new DeleteBoardGroupCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.BoardGroups.GetAsync(1).Returns(AutoFixtures.BoardGroup);

        await Handler.Handle(new DeleteBoardGroupCommand(1), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.BoardGroups.GetAsync(1).ReturnsNull();

        var result = await Handler.Handle(new DeleteBoardGroupCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenInvalidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.BoardGroups.GetAsync(1).ReturnsNull();

        await Handler.Handle(new DeleteBoardGroupCommand(1), CancellationToken.None);

        await UnitOfWork.Received(0).CompleteAsync();
    }
}
