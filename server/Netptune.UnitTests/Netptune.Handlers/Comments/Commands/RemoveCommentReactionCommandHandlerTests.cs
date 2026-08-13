using FluentAssertions;

using Netptune.Core.Constants;
using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;
using Netptune.Handlers.Comments.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Comments.Commands;

public class RemoveCommentReactionCommandHandlerTests
{
    private readonly RemoveCommentReactionCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public RemoveCommentReactionCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
        Identity.GetCurrentUserId().Returns("user-id");
        Identity.GetWorkspaceId().Returns(2);
    }

    [Fact]
    public async Task Remove_ShouldDeleteOnlyTheCallersReaction()
    {
        var comment = NewComment();
        var updated = new CommentViewModel { Id = comment.Id };

        UnitOfWork.Comments.GetCommentForUpdate(comment.Id, comment.WorkspaceId, TestContext.Current.CancellationToken).Returns(comment);
        UnitOfWork.Reactions.DeleteUserReaction(comment.Id, "user-id", ReactionValues.ThumbsUp, TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Comments.GetCommentViewModel(comment.Id, TestContext.Current.CancellationToken).Returns(updated);

        var result = await Handler.Handle(
            new RemoveCommentReactionCommand(comment.Id, $" {ReactionValues.ThumbsUp} "),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().BeSameAs(updated);
        await UnitOfWork.Reactions.Received(1).DeleteUserReaction(
            comment.Id,
            "user-id",
            ReactionValues.ThumbsUp,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Remove_ShouldReturnNotFound_WhenCommentIsOutsideTheWorkspace()
    {
        UnitOfWork.Comments.GetCommentForUpdate(42, 2, TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(
            new RemoveCommentReactionCommand(42, ReactionValues.ThumbsUp),
            TestContext.Current.CancellationToken);

        result.IsNotFound.Should().BeTrue();
        await UnitOfWork.Reactions.DidNotReceive().DeleteUserReaction(
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Remove_ShouldFail_WhenValueIsEmpty()
    {
        var result = await Handler.Handle(new RemoveCommentReactionCommand(7, "  "), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Reaction is not supported.");
        await UnitOfWork.Reactions.DidNotReceive().DeleteUserReaction(
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private static Comment NewComment() => new()
    {
        Id = 7,
        Body = "Original comment",
        OwnerId = "another-user",
        WorkspaceId = 2,
    };
}
