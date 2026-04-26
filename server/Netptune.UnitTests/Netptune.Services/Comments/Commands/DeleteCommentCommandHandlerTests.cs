using FluentAssertions;

using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Comments.Commands.DeleteComment;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Comments.Commands;

public class DeleteCommentCommandHandlerTests
{
    private readonly DeleteCommentCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteCommentCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        UnitOfWork.Comments.GetAsync(1).Returns(AutoFixtures.Comment);

        var result = await Handler.Handle(new DeleteCommentCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        UnitOfWork.Comments.GetAsync(1).Returns(AutoFixtures.Comment);

        await Handler.Handle(new DeleteCommentCommand(1), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        UnitOfWork.Comments.GetAsync(1).ReturnsNull();

        var result = await Handler.Handle(new DeleteCommentCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenInvalidId()
    {
        UnitOfWork.Comments.GetAsync(1).ReturnsNull();

        await Handler.Handle(new DeleteCommentCommand(1), CancellationToken.None);

        await UnitOfWork.Received(0).CompleteAsync();
    }
}
