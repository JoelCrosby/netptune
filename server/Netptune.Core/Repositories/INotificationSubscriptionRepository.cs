using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Notifications;

namespace Netptune.Core.Repositories;

public sealed record NotificationSubscriptionScopeQuery
{
    public required int WorkspaceId { get; init; }

    public IReadOnlyCollection<int> ProjectIds { get; init; } = [];

    public IReadOnlyCollection<int> BoardIds { get; init; } = [];

    public IReadOnlyCollection<int> BoardGroupIds { get; init; } = [];

    public IReadOnlyCollection<int> SprintIds { get; init; } = [];
}

public interface INotificationSubscriptionRepository : IWorkspaceEntityRepository<NotificationSubscription, int>
{
    Task<List<NotificationSubscription>> GetForUser(int workspaceId, string userId, CancellationToken cancellationToken = default);

    Task<NotificationSubscription?> Find(int workspaceId, string userId, NotificationScope scope, int scopeEntityId, CancellationToken cancellationToken = default);

    Task<List<NotificationSubscription>> GetForScopes(NotificationSubscriptionScopeQuery query, CancellationToken cancellationToken = default);

    Task<List<NotificationSubscriptionViewModel>> GetViewModelsForUser(int workspaceId, string userId, string workspaceSlug, CancellationToken cancellationToken = default);
}
