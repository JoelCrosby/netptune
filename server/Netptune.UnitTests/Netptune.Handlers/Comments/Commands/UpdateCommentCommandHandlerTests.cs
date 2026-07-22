using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Events;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;
using Netptune.Handlers.Comments.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Comments.Commands;

public class UpdateCommentCommandHandlerTests
{
    private readonly UpdateCommentCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IEventRecordWriter EventRecords = Substitute.For<IEventRecordWriter>();

    public UpdateCommentCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, EventRecords);
        Identity.GetCurrentUserId().Returns("user-id");
        Identity.GetWorkspaceId().Returns(2);
    }

    [Fact]
    public async Task Update_ShouldChangeBodyAndMentions_WhenCommentBelongsToCurrentUser()
    {
        var comment = NewComment();
        var updated = new CommentViewModel { Id = comment.Id, Body = "Updated comment" };
        var request = new UpdateCommentRequest
        {
            Comment = "  Updated comment  ",
            Mentions = ["mentioned-user", "user-id", "unknown-user"],
        };

        UnitOfWork.Comments.GetCommentForUpdate(comment.Id, comment.WorkspaceId, TestContext.Current.CancellationToken).Returns(comment);
        UnitOfWork.WorkspaceUsers.GetWorkspaceUserIds(comment.WorkspaceId, TestContext.Current.CancellationToken)
            .Returns(["user-id", "mentioned-user"]);
        UnitOfWork.Comments.GetCommentViewModel(comment.Id, TestContext.Current.CancellationToken).Returns(updated);

        var result = await Handler.Handle(new UpdateCommentCommand(comment.Id, request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().BeSameAs(updated);
        comment.Body.Should().Be("Updated comment");
        comment.Mentions.Select(mention => mention.UserId).Should().Equal("mentioned-user");
        await EventRecords.Received(1).Append(
            Arg.Is<EventWriteRequest<CommentEventPayload>>(eventRequest =>
                eventRequest.EventKey == EventKeys.CommentUpdated &&
                eventRequest.SubjectType == "task" &&
                eventRequest.SubjectId == comment.EntityId.ToString() &&
                eventRequest.Payload.CommentId == comment.Id &&
                eventRequest.Payload.RecipientUserIds.SequenceEqual(new[] { "mentioned-user" })),
            TestContext.Current.CancellationToken);
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Update_ShouldReturnForbidden_WhenCommentBelongsToAnotherUser()
    {
        var comment = NewComment() with { OwnerId = "another-user" };
        UnitOfWork.Comments.GetCommentForUpdate(comment.Id, comment.WorkspaceId, TestContext.Current.CancellationToken).Returns(comment);

        var result = await Handler.Handle(
            new UpdateCommentCommand(comment.Id, new UpdateCommentRequest { Comment = "Updated" }),
            TestContext.Current.CancellationToken);

        result.IsForbidden.Should().BeTrue();
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenCommentDoesNotExist()
    {
        UnitOfWork.Comments.GetCommentForUpdate(42, 2, TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(
            new UpdateCommentCommand(42, new UpdateCommentRequest { Comment = "Updated" }),
            TestContext.Current.CancellationToken);

        result.IsNotFound.Should().BeTrue();
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ShouldRejectWhitespaceOnlyComment()
    {
        var comment = NewComment();
        UnitOfWork.Comments.GetCommentForUpdate(comment.Id, comment.WorkspaceId, TestContext.Current.CancellationToken).Returns(comment);

        var result = await Handler.Handle(
            new UpdateCommentCommand(comment.Id, new UpdateCommentRequest { Comment = "   " }),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Comment cannot be empty.");
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    private static Comment NewComment() => new()
    {
        Id = 7,
        Body = "Original comment",
        OwnerId = "user-id",
        WorkspaceId = 2,
        Mentions = [],
    };
}
