using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Comments;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Comments.Commands;

public sealed record DeleteCommentCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;
    private readonly IEventRecordWriter EventRecords;

    public DeleteCommentCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IWorkspacePermissionCache permissionCache,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        PermissionCache = permissionCache;
        EventRecords = eventRecords;
    }

    public async ValueTask<ClientResponse> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var comment = await UnitOfWork.Comments.GetCommentForUpdate(request.Id, workspaceId, cancellationToken);

        if (comment is null || comment.EntityType != EntityType.Task)
        {
            return ClientResponse.NotFound;
        }

        var userId = Identity.GetCurrentUserId();

        if (!await CanDelete(comment.OwnerId, userId))
        {
            return ClientResponse.Forbidden;
        }

        var eventContext = new CommentEventContext(comment.WorkspaceId, comment.EntityId, comment.Id);
        var commentEvent = CommentEvents.Create(eventContext, EventKeys.CommentDeleted, []);

        await EventRecords.Append(commentEvent, cancellationToken);
        await UnitOfWork.Comments.DeletePermanent(comment.Id, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }

    private async Task<bool> CanDelete(string? ownerId, string userId)
    {
        var permissions = await PermissionCache.GetUserPermissions(userId, Identity.TryGetWorkspaceKey());
        var permission = ownerId == userId
            ? NetptunePermissions.Comments.DeleteOwn
            : NetptunePermissions.Comments.DeleteAny;
        var credentialAllows = !Identity.IsRestrictedByCredential(permission);
        var isAdministrator = permissions?.Role is WorkspaceRole.Owner or WorkspaceRole.Admin;
        var memberHoldsPermission = permissions?.Permissions.Contains(permission) == true;
        var memberAllows = isAdministrator || memberHoldsPermission;

        return memberAllows && credentialAllows;
    }
}
