using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Notifications;

namespace Netptune.Services.Notifications.Queries.GetUserNotifications;

public sealed record GetUserNotificationsQuery : IRequest<ClientResponse<List<NotificationViewModel>>>;
