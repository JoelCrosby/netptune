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

    private string? GetUserAgent() =>
        HttpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

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
            OccurredAt = DateTime.UtcNow,
            IpAddress = GetIpAddress(),
            UserAgent = GetUserAgent(),
            RecipientUserIds = activityOptions.RecipientUserIds,
        };

        EventPublisher.Dispatch(new ActivityMessage(activity));
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

        var ipAddress = GetIpAddress();
        var userAgent = GetUserAgent();

        var activities = activityOptions.EntityIds
            .Select(entityId => new ActivityEvent
            {
                Type = activityOptions.Type,
                EntityType = activityOptions.EntityType,
                UserId = activityOptions.UserId,
                EntityId = entityId,
                WorkspaceId = activityOptions.WorkspaceId.Value,
                OccurredAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
            });

        EventPublisher.Dispatch(new ActivityMessage(activities));
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
                OccurredAt = DateTime.UtcNow,
                Meta = JsonSerializer.Serialize(activityOptions.Meta, JsonOptions.Default),
            });

        EventPublisher.Dispatch(new ActivityMessage(activities));
    }
}
