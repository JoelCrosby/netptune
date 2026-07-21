using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Notifications;

namespace Netptune.Handlers.Notifications.Queries;

public sealed record GetUserNotificationsPagedQuery(NotificationFilter? Filter = null) : IRequest<ClientResponse<PagedResponse<NotificationViewModel>>>;

public sealed class GetUserNotificationsPagedQueryHandler : IRequestHandler<GetUserNotificationsPagedQuery, ClientResponse<PagedResponse<NotificationViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetUserNotificationsPagedQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<NotificationViewModel>>> Handle(GetUserNotificationsPagedQuery request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();

        var filter = request.Filter ?? new NotificationFilter();
        var pagination = filter.GetPagination();

        var search = filter.Search;
        var actorId = filter.UserId;

        var notifications = await UnitOfWork.Notifications.GetUserNotifications(
            userId,
            workspaceId,
            pagination,
            search,
            actorId,
            cancellationToken);
        var totalCount = await UnitOfWork.Notifications.GetUserNotificationsCount(userId, workspaceId, search, actorId, cancellationToken);

        var page = new PagedResponse<NotificationViewModel>(
            notifications,
            pagination.Page,
            pagination.PageSize,
            totalCount);

        return ClientResponse<PagedResponse<NotificationViewModel>>.Success(page);
    }
}
