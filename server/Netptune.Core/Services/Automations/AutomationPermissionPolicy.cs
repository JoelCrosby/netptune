using Netptune.Core.Authorization;
using Netptune.Core.Enums;

namespace Netptune.Core.Services.Automations;

public static class AutomationPermissionPolicy
{
    public static IReadOnlySet<string> GetRequiredPermissions(
        IEnumerable<AutomationActionType> actionTypes,
        IAutomationActionRegistry actionRegistry)
    {
        var permissions = actionTypes
            .Select(actionRegistry.Find)
            .Where(action => action is not null)
            .SelectMany(action => action!.RequiredPermissions)
            .ToHashSet(StringComparer.Ordinal);

        permissions.Add(NetptunePermissions.Tasks.Read);

        return permissions;
    }

    public static bool HasRequiredPermissions(
        IEnumerable<string> permissions,
        IReadOnlySet<string> requiredPermissions)
    {
        var permissionSet = permissions.ToHashSet(StringComparer.Ordinal);

        return requiredPermissions.All(permissionSet.Contains);
    }
}
