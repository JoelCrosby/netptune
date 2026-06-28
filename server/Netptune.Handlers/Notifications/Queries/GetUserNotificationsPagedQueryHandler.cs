using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Notifications;

namespace Netptune.Handlers.Notifications.Queries;

public sealed record GetUserNotificationsPagedQuery(PageRequest? Page = null) : IRequest<ClientResponse<PagedResponse<NotificationViewModel>>>;

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

        var pageRequest = request.Page ?? new PageRequest();
        var skip = pageRequest.GetSkip();
        var take = pageRequest.GetPageSize();

        var notifications = await UnitOfWork.Notifications.GetUserNotifications(userId, workspaceId, skip, take, cancellationToken);
        var totalCount = await UnitOfWork.Notifications.GetUserNotificationsCount(userId, workspaceId, cancellationToken);

        var page = new PagedResponse<NotificationViewModel>(notifications, pageRequest.GetPage(), take, totalCount);

        return ClientResponse<PagedResponse<NotificationViewModel>>.Success(page);
    }
}
