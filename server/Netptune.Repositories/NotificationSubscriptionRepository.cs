using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Notifications;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public sealed class NotificationSubscriptionRepository
    : WorkspaceEntityRepository<DataContext, NotificationSubscription, int>, INotificationSubscriptionRepository
{
    public NotificationSubscriptionRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory) { }

    public Task<List<NotificationSubscription>> GetForUser(int workspaceId, string userId, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(subscription => subscription.WorkspaceId == workspaceId && !subscription.IsDeleted)
            .Where(subscription => subscription.UserId == userId)
            .OrderBy(subscription => subscription.Scope)
            .ThenBy(subscription => subscription.ScopeEntityId)
            .ToListAsync(cancellationToken);
    }

    // Includes tombstoned rows so a re-subscribe revives one: the unique index only covers live rows,
    // so a blind insert would pile up tombstoned duplicates.
    public Task<NotificationSubscription?> Find(
        int workspaceId,
        string userId,
        NotificationScope scope,
        int scopeEntityId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(subscription => subscription.WorkspaceId == workspaceId)
            .Where(subscription => subscription.UserId == userId)
            .Where(subscription => subscription.Scope == scope && subscription.ScopeEntityId == scopeEntityId)
            .OrderBy(subscription => subscription.IsDeleted)
            .ThenByDescending(subscription => subscription.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<NotificationSubscription>> GetForScopes(
        NotificationSubscriptionScopeQuery query,
        CancellationToken cancellationToken = default)
    {
        var hasScopes = query.ProjectIds.Count > 0
            || query.BoardIds.Count > 0
            || query.BoardGroupIds.Count > 0
            || query.SprintIds.Count > 0;

        if (!hasScopes)
        {
            return Task.FromResult(new List<NotificationSubscription>());
        }

        var projectIds = query.ProjectIds.ToList();
        var boardIds = query.BoardIds.ToList();
        var boardGroupIds = query.BoardGroupIds.ToList();
        var sprintIds = query.SprintIds.ToList();

        return Entities
            .AsNoTracking()
            .Where(subscription => subscription.WorkspaceId == query.WorkspaceId && !subscription.IsDeleted)
            .Where(subscription => subscription.Events != NotificationSubscriptionEvents.None)
            .Where(subscription =>
                (subscription.Scope == NotificationScope.Project && projectIds.Contains(subscription.ScopeEntityId)) ||
                (subscription.Scope == NotificationScope.Board && boardIds.Contains(subscription.ScopeEntityId)) ||
                (subscription.Scope == NotificationScope.BoardGroup && boardGroupIds.Contains(subscription.ScopeEntityId)) ||
                (subscription.Scope == NotificationScope.Sprint && sprintIds.Contains(subscription.ScopeEntityId)))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<NotificationSubscriptionViewModel>> GetViewModelsForUser(
        int workspaceId,
        string userId,
        string workspaceSlug,
        CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var results = await connection.QueryAsync<NotificationSubscriptionViewModel>(new CommandDefinition(
            SqlScripts.GetNotificationSubscriptions, new
            {
                userId,
                workspaceId,
                projectScope = NotificationScope.Project,
                boardScope = NotificationScope.Board,
                boardGroupScope = NotificationScope.BoardGroup,
                sprintScope = NotificationScope.Sprint,
            }, cancellationToken: cancellationToken));

        var subscriptions = results.AsList();

        foreach (var subscription in subscriptions)
        {
            subscription.Link = NotificationSubscriptionLink.Build(
                workspaceSlug,
                subscription.Scope,
                subscription.LinkIdentifier);
        }

        return subscriptions;
    }
}
