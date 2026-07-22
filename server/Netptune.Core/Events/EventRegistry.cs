using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Core.Events;

public static class EventEntityTypes
{
    public static string From(EntityType entityType) => entityType.ToString().ToLowerInvariant();

    public static bool TryParse(string? value, out EntityType entityType)
    {
        return Enum.TryParse(value, true, out entityType);
    }
}

public static class EventKeys
{
    public const string EntityCreated = "entity.created";
    public const string EntityDeleted = "entity.deleted";
    public const string EntityRestored = "entity.restored";
    public const string EntityFieldTransitioned = "entity.field-transitioned";
    public const string ScopeMemberChanged = "scope.member-changed";
    public const string ScopeMemberAttributeChanged = "scope.member-attribute-changed";
    public const string ScopeLifecycleTransitioned = "scope.lifecycle-transitioned";
    public const string SecurityLoginSucceeded = "security.login-succeeded";
    public const string SecurityLoginFailed = "security.login-failed";
    public const string ExportRequested = "export.requested";
    public const string WorkspaceRoleChanged = "workspace.member-role-changed";
    public const string WorkspaceSettingsChanged = "workspace.settings-changed";
    public const string CommentCreated = "comment.created";
    public const string CommentUpdated = "comment.updated";
    public const string CommentDeleted = "comment.deleted";
    public const string EntityActivityRecorded = "entity.activity-recorded";

    public static string From(ActivityType type) => type switch
    {
        ActivityType.Create => EntityCreated,
        ActivityType.Delete => EntityDeleted,
        ActivityType.Restore => EntityRestored,
        ActivityType.Modify or
        ActivityType.ModifyName or
        ActivityType.ModifyDescription or
        ActivityType.ModifyStatus or
        ActivityType.ModifyPriority or
        ActivityType.ModifyEstimate or
        ActivityType.ModifyDueDate or
        ActivityType.ModifyStartDate or
        ActivityType.PermissionChanged => EntityFieldTransitioned,
        ActivityType.Assign or ActivityType.Unassign => ScopeMemberChanged,
        ActivityType.RoleChanged => WorkspaceRoleChanged,
        ActivityType.WorkspaceSettingsChanged => WorkspaceSettingsChanged,
        ActivityType.ExportRequested => ExportRequested,
        ActivityType.LoginSuccess => SecurityLoginSucceeded,
        ActivityType.LoginFailed => SecurityLoginFailed,
        ActivityType.AddComment => CommentCreated,
        ActivityType.ModifyComment => CommentUpdated,
        ActivityType.RemoveComment => CommentDeleted,
        _ => EntityActivityRecorded,
    };

    public static string RetentionFor(string eventKey) => eventKey switch
    {
        EntityCreated
            or EntityDeleted
            or EntityRestored
            or EntityFieldTransitioned
            or ScopeMemberChanged
            or ScopeMemberAttributeChanged
            or ScopeLifecycleTransitioned
            or WorkspaceRoleChanged
            or WorkspaceSettingsChanged
            or CommentCreated
            or CommentUpdated
            or CommentDeleted
            => EventRetentionClasses.Permanent,
        _ => EventRetentionClasses.Audit,
    };

    public static ActivityType ActivityTypeFor(string eventKey, JsonElement payload)
    {
        var directActivityType = eventKey switch
        {
            SecurityLoginSucceeded => ActivityType.LoginSuccess,
            SecurityLoginFailed => ActivityType.LoginFailed,
            ExportRequested => ActivityType.ExportRequested,
            WorkspaceRoleChanged => ActivityType.RoleChanged,
            WorkspaceSettingsChanged => ActivityType.WorkspaceSettingsChanged,
            CommentCreated => ActivityType.AddComment,
            CommentUpdated => ActivityType.ModifyComment,
            CommentDeleted => ActivityType.RemoveComment,
            _ => (ActivityType?)null,
        };

        if (directActivityType.HasValue)
        {
            return directActivityType.Value;
        }

        var field = ReadString(payload, "field");
        var isEntityCreation = eventKey == EntityCreated;
        var isScopeMembershipChange = eventKey == ScopeMemberChanged;
        var isScopeLifecycleChange = eventKey == ScopeLifecycleTransitioned;
        var isMemberAttributeChange = eventKey == ScopeMemberAttributeChanged;
        var isEstimateTransition = field == "estimate";
        var isEstimateChange = isMemberAttributeChange || isEstimateTransition;
        var isStartDateTransition = field == "startdate";
        var isDueDateTransition = field == "duedate";

        if (isEntityCreation)
        {
            return ActivityType.Create;
        }

        if (isScopeMembershipChange)
        {
            var memberWasRemoved = ReadString(payload, "change") == "removed";

            return memberWasRemoved
                ? ActivityType.Unassign
                : ActivityType.Assign;
        }

        if (isScopeLifecycleChange)
        {
            return ActivityType.ModifyStatus;
        }

        if (isEstimateChange)
        {
            return ActivityType.ModifyEstimate;
        }

        if (isStartDateTransition)
        {
            return ActivityType.ModifyStartDate;
        }

        if (isDueDateTransition)
        {
            return ActivityType.ModifyDueDate;
        }

        return field == "status"
            ? ActivityType.ModifyStatus
            : ActivityType.Modify;
    }

    private static string? ReadString(JsonElement payload, string propertyName)
    {
        var propertyExists = payload.TryGetProperty(propertyName, out var value);
        var propertyHasValue = propertyExists && value.ValueKind is not JsonValueKind.Null;

        return propertyHasValue ? value.ToString() : null;
    }
}

public static class EventDefinitionRegistry
{
    private static readonly IReadOnlyDictionary<(string Key, short Version), Type> Definitions =
        new Dictionary<(string, short), Type>
        {
            [(EventKeys.EntityCreated, 1)] = typeof(EntityCreatedPayload),
            [(EventKeys.EntityFieldTransitioned, 1)] = typeof(FieldTransitionedPayload),
            [(EventKeys.ScopeMemberChanged, 1)] = typeof(ScopeMemberChangedPayload),
            [(EventKeys.ScopeMemberAttributeChanged, 1)] = typeof(ScopeMemberAttributeChangedPayload),
            [(EventKeys.ScopeLifecycleTransitioned, 1)] = typeof(ScopeLifecyclePayload),
            [(EventKeys.SecurityLoginSucceeded, 1)] = typeof(AuthenticationEventPayload),
            [(EventKeys.SecurityLoginFailed, 1)] = typeof(AuthenticationEventPayload),
            [(EventKeys.ExportRequested, 1)] = typeof(ExportRequestedPayload),
            [(EventKeys.WorkspaceRoleChanged, 1)] = typeof(WorkspaceRoleChangedPayload),
            [(EventKeys.WorkspaceSettingsChanged, 1)] = typeof(WorkspaceSettingsChangedPayload),
            [(EventKeys.CommentCreated, 1)] = typeof(CommentEventPayload),
            [(EventKeys.CommentUpdated, 1)] = typeof(CommentEventPayload),
            [(EventKeys.CommentDeleted, 1)] = typeof(CommentEventPayload),
        };

    public static void Validate<TPayload>(EventWriteRequest<TPayload> request) where TPayload : class
    {
        if (!Definitions.TryGetValue((request.EventKey, request.SchemaVersion), out var payloadType))
        {
            throw new InvalidOperationException($"Event {request.EventKey} v{request.SchemaVersion} is not registered.");
        }

        if (payloadType != typeof(TPayload))
        {
            throw new InvalidOperationException($"Event {request.EventKey} v{request.SchemaVersion} requires payload {payloadType.Name}.");
        }

        var hasSubjectType = !string.IsNullOrWhiteSpace(request.SubjectType);
        var hasSubjectId = !string.IsNullOrWhiteSpace(request.SubjectId);
        var isSecurityEvent = request.EventKey is EventKeys.SecurityLoginSucceeded or EventKeys.SecurityLoginFailed;
        var hasRequiredWorkspace = request.WorkspaceId.HasValue || isSecurityEvent;
        var hasRequiredDomainContext = hasRequiredWorkspace && hasSubjectType && hasSubjectId;

        if (!hasRequiredDomainContext)
        {
            throw new InvalidOperationException($"Event {request.EventKey} requires a workspace and subject.");
        }
    }
}
