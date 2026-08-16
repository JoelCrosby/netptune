using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Boards.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Boards.Commands;

public class DeleteBoardCommandHandlerTests
{
    private const int WorkspaceId = 7;

    private readonly DeleteBoardCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteBoardCommandHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);

        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Delete_ShouldScopeLookupToCallerWorkspace()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetInWorkspace(1, WorkspaceId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.Board);

        await Handler.Handle(new DeleteBoardCommand(1), TestContext.Current.CancellationToken);

        await UnitOfWork.Boards.Received(1).GetInWorkspace(1, WorkspaceId, cancellationToken: TestContext.Current.CancellationToken);
        await UnitOfWork.Boards.Received(0).GetAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetInWorkspace(1, WorkspaceId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.Board);

        var result = await Handler.Handle(new DeleteBoardCommand(1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetInWorkspace(1, WorkspaceId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.Board);

        await Handler.Handle(new DeleteBoardCommand(1), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetInWorkspace(1, WorkspaceId, cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new DeleteBoardCommand(1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallDeletePermanent_WhenInvalidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetInWorkspace(1, WorkspaceId, cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        await Handler.Handle(new DeleteBoardCommand(1), TestContext.Current.CancellationToken);

        await UnitOfWork.Boards.Received(0).DeletePermanent(Arg.Any<int>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenInvalidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Boards.GetInWorkspace(1, WorkspaceId, cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        await Handler.Handle(new DeleteBoardCommand(1), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(0).CompleteAsync(TestContext.Current.CancellationToken);
    }
}
