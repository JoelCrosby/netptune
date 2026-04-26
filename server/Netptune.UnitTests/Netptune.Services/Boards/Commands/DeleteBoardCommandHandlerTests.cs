using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Boards.Commands.DeleteBoard;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Boards.Commands;

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
