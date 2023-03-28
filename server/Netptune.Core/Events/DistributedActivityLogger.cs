using System;
using System.Linq;
using System.Text.Json;

using Netptune.Core.Encoding;
using Netptune.Core.Models.Activity;
using Netptune.Core.Services;

namespace Netptune.Core.Events;

public class DistributedActivityLogger : IActivityLogger
{
    private readonly IEventPublisher EventPublisher;
    private readonly IIdentityService Identity;

    public DistributedActivityLogger(IEventPublisher eventPublisher, IIdentityService identity)
    {
        EventPublisher = eventPublisher;
        Identity = identity;
    }

    public void Log(Action<ActivityOptions> options)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = Identity.GetWorkspaceId().GetAwaiter().GetResult();

        var activityOptions = new ActivityOptions
        {
            UserId = userId,
            WorkspaceId = workspaceId,
        };

        options.Invoke(activityOptions);

        if (activityOptions.EntityId is null)
        {
            throw new Exception($"Cannot call log with null {nameof(activityOptions.EntityId)}.");
        }

        if (activityOptions.WorkspaceId is null)
        {
            throw new Exception($"Cannot call log with null {nameof(activityOptions.WorkspaceId)}.");
        }

        var activity = new ActivityEvent
        {
            Type = activityOptions.Type,
            EntityType = activityOptions.EntityType,
            UserId = activityOptions.UserId,
            EntityId = activityOptions.EntityId.Value,
            WorkspaceId = activityOptions.WorkspaceId.Value,
            Time = DateTime.UtcNow,
        };

        EventPublisher.Dispatch(NetptuneEvent.Activity, new []{ activity });
    }

    public void LogMany(Action<ActivityMultipleOptions> options)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = Identity.GetWorkspaceId().GetAwaiter().GetResult();

        var activityOptions = new ActivityMultipleOptions
        {
            UserId = userId,
            WorkspaceId = workspaceId,
        };

        options.Invoke(activityOptions);

        if (activityOptions.WorkspaceId is null)
        {
            throw new Exception($"Cannot call log with null {nameof(activityOptions.WorkspaceId)}.");
        }

        var activities = activityOptions.EntityIds
            .Select(entityId => new ActivityEvent
            {
                Type = activityOptions.Type,
                EntityType = activityOptions.EntityType,
                UserId = activityOptions.UserId,
                EntityId = entityId,
                WorkspaceId = activityOptions.WorkspaceId.Value,
                Time = DateTime.UtcNow,
            });

        EventPublisher.Dispatch(NetptuneEvent.Activity, activities);
    }

    public void LogWith<TMeta>(Action<ActivityOptions<TMeta>> options) where TMeta : class
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = Identity.GetWorkspaceId().GetAwaiter().GetResult();

        var activityOptions = new ActivityOptions<TMeta>
        {
            UserId = userId,
            WorkspaceId = workspaceId,
        };

        options.Invoke(activityOptions);

        if (activityOptions.EntityId is null)
        {
            throw new Exception($"Cannot call log with null {nameof(activityOptions.EntityId)}.");
        }

        if (activityOptions.WorkspaceId is null)
        {
            throw new Exception($"Cannot call log with null {nameof(activityOptions.WorkspaceId)}.");
        }

        var activity = new ActivityEvent
        {
            Type = activityOptions.Type,
            EntityType = activityOptions.EntityType,
            UserId = activityOptions.UserId,
            EntityId = activityOptions.EntityId.Value,
            WorkspaceId = activityOptions.WorkspaceId.Value,
            Time = DateTime.UtcNow,
            Meta = JsonSerializer.Serialize(activityOptions.Meta, JsonOptions.Default),
        };

        EventPublisher.Dispatch(NetptuneEvent.Activity, new []{ activity });
    }

    public void LogWithMany<TMeta>(Action<ActivityMultipleOptions<TMeta>> options) where TMeta : class
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = Identity.GetWorkspaceId().GetAwaiter().GetResult();

        var activityOptions = new ActivityMultipleOptions<TMeta>
        {
            UserId = userId,
            WorkspaceId = workspaceId,
        };

        options.Invoke(activityOptions);

        if (activityOptions.WorkspaceId is null)
        {
            throw new Exception($"Cannot call log with null {nameof(activityOptions.WorkspaceId)}.");
        }

        var activities = activityOptions.EntityIds
            .Select(entityId => new ActivityEvent
            {
                Type = activityOptions.Type,
                EntityType = activityOptions.EntityType,
                UserId = activityOptions.UserId,
                EntityId = entityId,
                WorkspaceId = activityOptions.WorkspaceId.Value,
                Time = DateTime.UtcNow,
                Meta = JsonSerializer.Serialize(activityOptions.Meta, JsonOptions.Default),
            });

        EventPublisher.Dispatch(NetptuneEvent.Activity, activities);
    }
}
