using System;
using System.Linq;
using System.Text.Json;

using Netptune.Core.Encoding;
using Netptune.Core.Models.Activity;

namespace Netptune.Core.Events;

public class ActivityLogger : IActivityLogger
{
    private readonly IEventPublisher EventPublisher;

    public ActivityLogger(IEventPublisher eventPublisher)
    {
        EventPublisher = eventPublisher;
    }

    public void Log(Action<ActivityOptions> options)
    {
        var activityOptions = new ActivityOptions();

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

        EventPublisher.Dispatch(NetptuneEvent.Activity, new [] { activity });
    }

    public void LogMany(Action<ActivityMultipleOptions> options)
    {
        var activityOptions = new ActivityMultipleOptions();

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
        var activityOptions = new ActivityOptions<TMeta>();

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

        EventPublisher.Dispatch(NetptuneEvent.Activity, new [] { activity });
    }

    public void LogWithMany<TMeta>(Action<ActivityMultipleOptions<TMeta>> options) where TMeta : class
    {
        var activityOptions = new ActivityMultipleOptions<TMeta>();

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
