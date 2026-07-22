using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Comments.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Comments.Commands;

public class DeleteCommentCommandHandlerTests
{
    private const int WorkspaceId = 2;
    private const string WorkspaceKey = "workspace";
    private const string UserId = "user-id";

    private readonly DeleteCommentCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache PermissionCache = Substitute.For<IWorkspacePermissionCache>();
    private readonly IEventRecordWriter EventRecords = Substitute.For<IEventRecordWriter>();

    public DeleteCommentCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, PermissionCache, EventRecords);
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
    }

    [Fact]
    public async Task Delete_ShouldPersistCanonicalEvent_WhenDeletingOwnComment()
    {
        var comment = NewComment(UserId);
        UnitOfWork.Comments.GetCommentForUpdate(comment.Id, WorkspaceId, TestContext.Current.CancellationToken)
            .Returns(comment);
        PermissionCache.GetUserPermissions(UserId, WorkspaceKey)
            .Returns(Permissions(WorkspaceRole.Member, NetptunePermissions.Comments.DeleteOwn));

        var result = await Handler.Handle(new DeleteCommentCommand(comment.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await EventRecords.Received(1).Append(
            Arg.Is<EventWriteRequest<CommentEventPayload>>(eventRequest =>
                eventRequest.EventKey == EventKeys.CommentDeleted &&
                eventRequest.SubjectType == "task" &&
                eventRequest.SubjectId == comment.EntityId.ToString() &&
                eventRequest.Payload.CommentId == comment.Id &&
                eventRequest.Payload.RecipientUserIds.Count == 0),
            TestContext.Current.CancellationToken);
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldReturnForbidden_WhenDeletingAnotherUsersCommentWithoutDeleteAny()
    {
        var comment = NewComment("another-user");
        UnitOfWork.Comments.GetCommentForUpdate(comment.Id, WorkspaceId, TestContext.Current.CancellationToken)
            .Returns(comment);
        PermissionCache.GetUserPermissions(UserId, WorkspaceKey)
            .Returns(Permissions(WorkspaceRole.Member, NetptunePermissions.Comments.DeleteOwn));

        var result = await Handler.Handle(new DeleteCommentCommand(comment.Id), TestContext.Current.CancellationToken);

        result.IsForbidden.Should().BeTrue();
        await EventRecords.DidNotReceive().Append(
            Arg.Any<EventWriteRequest<CommentEventPayload>>(),
            Arg.Any<CancellationToken>());
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ShouldAllowAdminToDeleteAnotherUsersComment()
    {
        var comment = NewComment("another-user");
        UnitOfWork.Comments.GetCommentForUpdate(comment.Id, WorkspaceId, TestContext.Current.CancellationToken)
            .Returns(comment);
        PermissionCache.GetUserPermissions(UserId, WorkspaceKey).Returns(Permissions(WorkspaceRole.Admin));

        var result = await Handler.Handle(new DeleteCommentCommand(comment.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenCommentDoesNotExistInWorkspace()
    {
        UnitOfWork.Comments.GetCommentForUpdate(42, WorkspaceId, TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new DeleteCommentCommand(42), TestContext.Current.CancellationToken);

        result.IsNotFound.Should().BeTrue();
        await EventRecords.DidNotReceive().Append(
            Arg.Any<EventWriteRequest<CommentEventPayload>>(),
            Arg.Any<CancellationToken>());
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    private static Comment NewComment(string ownerId) => new()
    {
        Id = 7,
        Body = "Comment",
        EntityId = 10,
        EntityType = EntityType.Task,
        OwnerId = ownerId,
        WorkspaceId = WorkspaceId,
    };

    private static UserPermissions Permissions(WorkspaceRole role, params string[] permissions) => new()
    {
        UserId = UserId,
        WorkspaceKey = WorkspaceKey,
        Role = role,
        Permissions = [.. permissions],
    };
}
