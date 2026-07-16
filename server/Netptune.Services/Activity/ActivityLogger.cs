using System.Text.Json;

using Microsoft.AspNetCore.Http;

using Netptune.Core.Encoding;
using Netptune.Core.Events;
using Netptune.Core.Models.Activity;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;

namespace Netptune.Services.Activity;

public class ActivityLogger : IActivityLogger
{
    private readonly IEventPublisher EventPublisher;
    private readonly IIdentityService Identity;
    private readonly IHttpContextAccessor HttpContextAccessor;

    public ActivityLogger(IEventPublisher eventPublisher, IIdentityService identity, IHttpContextAccessor httpContextAccessor)
    {
        EventPublisher = eventPublisher;
        Identity = identity;
        HttpContextAccessor = httpContextAccessor;
    }

    private string? GetIpAddress()
    {
        var context = HttpContextAccessor.HttpContext;
        if (context is null) return null;

        return context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded)
            ? forwarded.ToString().Split(',')[0].Trim()
            : context.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        return HttpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
    }


    private int GetWorkspaceId(int? workspaceId)
    {
        return workspaceId ?? Identity.GetWorkspaceId().GetAwaiter().GetResult();
    }

    public void Log(Action<ActivityOptions> options)
    {
        var userId = Identity.GetCurrentUserId();

        var activityOptions = new ActivityOptions
        {
            UserId = userId,
        };

        options.Invoke(activityOptions);

        if (activityOptions.EntityId is null)
        {
            throw new Exception($"Cannot call log with null {nameof(activityOptions.EntityId)}.");
        }

        var workspaceId = GetWorkspaceId(activityOptions.WorkspaceId);

        var activity = new ActivityEvent
        {
            EventId = Guid.NewGuid(),
            Type = activityOptions.Type,
            EntityType = activityOptions.EntityType,
            UserId = activityOptions.UserId,
            EntityId = activityOptions.EntityId.Value,
            WorkspaceId = workspaceId,
            OccurredAt = DateTime.UtcNow,
            IpAddress = GetIpAddress(),
            UserAgent = GetUserAgent(),
            RecipientUserIds = activityOptions.RecipientUserIds,
        };

        EventPublisher.Dispatch(new ActivityMessage(activity));
    }

    public void LogChanges(Action<ActivityChangeSetOptions> options)
    {
        var userId = Identity.GetCurrentUserId();

        var changeSetOptions = new ActivityChangeSetOptions
        {
            UserId = userId,
        };

        options.Invoke(changeSetOptions);

        if (changeSetOptions.EntityId is null)
        {
            throw new Exception($"Cannot call log with null {nameof(changeSetOptions.EntityId)}.");
        }

        if (changeSetOptions.Changes.Count == 0) return;

        var workspaceId = GetWorkspaceId(changeSetOptions.WorkspaceId);
        var ipAddress = GetIpAddress();
        var userAgent = GetUserAgent();
        var occurredAt = DateTime.UtcNow;

        var activities = changeSetOptions.Changes
            .Select(change => new ActivityEvent
            {
                EventId = Guid.NewGuid(),
                Type = change.Type,
                EntityType = changeSetOptions.EntityType,
                UserId = changeSetOptions.UserId,
                EntityId = changeSetOptions.EntityId.Value,
                WorkspaceId = workspaceId,
                OccurredAt = occurredAt,
                Field = change.Field,
                OldValue = ActivityValue.Truncate(change.OldValue),
                NewValue = ActivityValue.Truncate(change.NewValue),
                OldValueHash = ActivityValue.HashIfTruncated(change.OldValue),
                NewValueHash = ActivityValue.HashIfTruncated(change.NewValue),
                Meta = change.Meta,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                RecipientUserIds = changeSetOptions.RecipientUserIds,
            });

        EventPublisher.Dispatch(new ActivityMessage(activities));
    }

    public void LogMany(Action<ActivityMultipleOptions> options)
    {
        var userId = Identity.GetCurrentUserId();

        var activityOptions = new ActivityMultipleOptions
        {
            UserId = userId,
        };

        options.Invoke(activityOptions);

        var workspaceId = GetWorkspaceId(activityOptions.WorkspaceId);
        var ipAddress = GetIpAddress();
        var userAgent = GetUserAgent();

        var activities = activityOptions.EntityIds
            .Select(entityId => new ActivityEvent
            {
                EventId = Guid.NewGuid(),
                Type = activityOptions.Type,
                EntityType = activityOptions.EntityType,
                UserId = activityOptions.UserId,
                EntityId = entityId,
                WorkspaceId = workspaceId,
                OccurredAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
            });

        EventPublisher.Dispatch(new ActivityMessage(activities));
    }

    public void LogWith<TMeta>(Action<ActivityOptions<TMeta>> options) where TMeta : class
    {
        var userId = Identity.GetCurrentUserId();

        var activityOptions = new ActivityOptions<TMeta>
        {
            UserId = userId,
        };

        options.Invoke(activityOptions);

        if (activityOptions.EntityId is null)
        {
            throw new Exception($"Cannot call log with null {nameof(activityOptions.EntityId)}.");
        }

        var workspaceId = GetWorkspaceId(activityOptions.WorkspaceId);

        var activity = new ActivityEvent
        {
            EventId = Guid.NewGuid(),
            Type = activityOptions.Type,
            EntityType = activityOptions.EntityType,
            UserId = activityOptions.UserId,
            EntityId = activityOptions.EntityId.Value,
            WorkspaceId = workspaceId,
            OccurredAt = DateTime.UtcNow,
            Meta = JsonSerializer.Serialize(activityOptions.Meta, JsonOptions.Default),
            IpAddress = GetIpAddress(),
            UserAgent = GetUserAgent(),
        };

        EventPublisher.Dispatch(new ActivityMessage(activity));
    }

    public void LogWithMany<TMeta>(Action<ActivityMultipleOptions<TMeta>> options) where TMeta : class
    {
        var userId = Identity.GetCurrentUserId();

        var activityOptions = new ActivityMultipleOptions<TMeta>
        {
            UserId = userId,
        };

        options.Invoke(activityOptions);

        var workspaceId = GetWorkspaceId(activityOptions.WorkspaceId);

        var activities = activityOptions.EntityIds
            .Select(entityId => new ActivityEvent
            {
                EventId = Guid.NewGuid(),
                Type = activityOptions.Type,
                EntityType = activityOptions.EntityType,
                UserId = activityOptions.UserId,
                EntityId = entityId,
                WorkspaceId = workspaceId,
                OccurredAt = DateTime.UtcNow,
                Meta = JsonSerializer.Serialize(activityOptions.Meta, JsonOptions.Default),
            });

        EventPublisher.Dispatch(new ActivityMessage(activities));
    }
}
