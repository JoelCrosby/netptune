using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;

namespace Netptune.Core.Services.Automations;

public static class AutomationPermissionPolicy
{
    public static IReadOnlySet<string> GetRequiredPermissions(IEnumerable<AutomationActionType> actionTypes, IAutomationActionRegistry actionRegistry)
    {
        var permissions = actionTypes
            .Select(actionRegistry.Find)
            .Where(action => action is not null)
            .SelectMany(action => action!.RequiredPermissions)
            .ToHashSet(StringComparer.Ordinal);

        permissions.Add(NetptunePermissions.Tasks.Read);

        return permissions;
    }

    public static IReadOnlySet<string> GetRequiredPermissions(
        IEnumerable<AutomationActionRequest> actions,
        IAutomationActionRegistry actionRegistry)
    {
        var permissions = actions
            .Select(action => (Request: action, Action: actionRegistry.Find(action.Type)))
            .Where(item => item.Action is not null)
            .SelectMany(item => item.Action!.GetRequiredPermissions(item.Request))
            .ToHashSet(StringComparer.Ordinal);

        permissions.Add(NetptunePermissions.Tasks.Read);

        return permissions;
    }

    public static IReadOnlySet<string> GetRequiredPermissions(IEnumerable<AutomationAction> actions, IAutomationActionRegistry actionRegistry)
    {
        var permissions = actions
            .Select(action => (Entity: action, Action: actionRegistry.Find(action.Type)))
            .Where(item => item.Action is not null)
            .SelectMany(item => item.Action!.GetRequiredPermissions(item.Entity))
            .ToHashSet(StringComparer.Ordinal);

        permissions.Add(NetptunePermissions.Tasks.Read);

        return permissions;
    }

    public static bool HasRequiredPermissions(IEnumerable<string> permissions, IReadOnlySet<string> requiredPermissions)
    {
        var permissionSet = permissions.ToHashSet(StringComparer.Ordinal);

        return requiredPermissions.All(permissionSet.Contains);
    }
}
