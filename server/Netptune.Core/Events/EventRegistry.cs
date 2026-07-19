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
        ActivityType.WorkspaceSettingsChanged or
        ActivityType.RoleChanged or
        ActivityType.PermissionChanged => EntityFieldTransitioned,
        ActivityType.Assign or ActivityType.Unassign => ScopeMemberChanged,
        ActivityType.LoginSuccess => SecurityLoginSucceeded,
        ActivityType.LoginFailed => SecurityLoginFailed,
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
            => EventRetentionClasses.Permanent,
        _ => EventRetentionClasses.Audit,
    };

    public static ActivityType ActivityTypeFor(string eventKey, JsonElement payload)
    {
        var field = ReadString(payload, "field");
        var isEntityCreation = eventKey == EntityCreated;
        var isScopeMembershipChange = eventKey == ScopeMemberChanged;
        var isScopeLifecycleChange = eventKey == ScopeLifecycleTransitioned;
        var isMemberAttributeChange = eventKey == ScopeMemberAttributeChanged;
        var isEstimateTransition = field == "estimate";
        var isEstimateChange = isMemberAttributeChange || isEstimateTransition;

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

        var hasWorkspace = request.WorkspaceId.HasValue;
        var hasSubjectType = !string.IsNullOrWhiteSpace(request.SubjectType);
        var hasSubjectId = !string.IsNullOrWhiteSpace(request.SubjectId);
        var hasRequiredDomainContext = hasWorkspace && hasSubjectType && hasSubjectId;

        if (!hasRequiredDomainContext)
        {
            throw new InvalidOperationException($"Event {request.EventKey} requires a workspace and subject.");
        }
    }
}
