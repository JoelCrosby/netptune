using FluentAssertions;

using Netptune.Core.Constants;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;
using Netptune.Handlers.Comments.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Comments.Commands;

public class AddCommentReactionCommandHandlerTests
{
    private readonly AddCommentReactionCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public AddCommentReactionCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
        Identity.GetCurrentUserId().Returns("user-id");
        Identity.GetWorkspaceId().Returns(2);
    }

    [Fact]
    public async Task Add_ShouldPersistReaction_WhenUserHasNotReactedWithThatValue()
    {
        var comment = NewComment();
        var updated = new CommentViewModel { Id = comment.Id };

        UnitOfWork.Comments.GetCommentForUpdate(comment.Id, comment.WorkspaceId, TestContext.Current.CancellationToken).Returns(comment);
        UnitOfWork.Reactions.HasUserReaction(comment.Id, "user-id", ReactionValues.ThumbsUp, TestContext.Current.CancellationToken).Returns(false);
        UnitOfWork.Comments.GetCommentViewModel(comment.Id, TestContext.Current.CancellationToken).Returns(updated);

        var result = await Handler.Handle(NewCommand(comment.Id, $" {ReactionValues.ThumbsUp} "), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().BeSameAs(updated);
        await UnitOfWork.Reactions.Received(1).AddAsync(
            Arg.Is<Reaction>(reaction =>
                reaction.CommentId == comment.Id &&
                reaction.Value == ReactionValues.ThumbsUp &&
                reaction.OwnerId == "user-id" &&
                reaction.WorkspaceId == comment.WorkspaceId),
            TestContext.Current.CancellationToken);
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Add_ShouldNotDuplicate_WhenUserAlreadyReactedWithThatValue()
    {
        var comment = NewComment();
        var updated = new CommentViewModel { Id = comment.Id };

        UnitOfWork.Comments.GetCommentForUpdate(comment.Id, comment.WorkspaceId, TestContext.Current.CancellationToken).Returns(comment);
        UnitOfWork.Reactions.HasUserReaction(comment.Id, "user-id", ReactionValues.ThumbsUp, TestContext.Current.CancellationToken).Returns(true);
        UnitOfWork.Comments.GetCommentViewModel(comment.Id, TestContext.Current.CancellationToken).Returns(updated);

        var result = await Handler.Handle(NewCommand(comment.Id, ReactionValues.ThumbsUp), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await UnitOfWork.Reactions.DidNotReceive().AddAsync(Arg.Any<Reaction>(), Arg.Any<CancellationToken>());
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_ShouldFail_WhenValueIsNotSupported()
    {
        var result = await Handler.Handle(NewCommand(7, "not-an-emoji"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Reaction is not supported.");
        await UnitOfWork.Reactions.DidNotReceive().AddAsync(Arg.Any<Reaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_ShouldReturnNotFound_WhenCommentIsOutsideTheWorkspace()
    {
        UnitOfWork.Comments.GetCommentForUpdate(42, 2, TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(NewCommand(42, ReactionValues.Heart), TestContext.Current.CancellationToken);

        result.IsNotFound.Should().BeTrue();
        await UnitOfWork.Reactions.DidNotReceive().AddAsync(Arg.Any<Reaction>(), Arg.Any<CancellationToken>());
    }

    private static AddCommentReactionCommand NewCommand(int commentId, string value)
    {
        return new(commentId, new CommentReactionRequest { Value = value });
    }

    private static Comment NewComment() => new()
    {
        Id = 7,
        Body = "Original comment",
        OwnerId = "another-user",
        WorkspaceId = 2,
    };
}
