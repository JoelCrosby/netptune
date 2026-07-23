using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Requests;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Automations;

internal static class AutomationPrincipalValidation
{
    public static async Task<string?> Validate(
        AutomationRuleRequest request,
        int workspaceId,
        string userId,
        string workspaceKey,
        INetptuneUnitOfWork unitOfWork,
        IWorkspacePermissionCache permissionCache,
        IAutomationActionRegistry actionRegistry,
        CancellationToken cancellationToken)
    {
        var requiredPermissions = AutomationPermissionPolicy.GetRequiredPermissions(
            request.Actions.Select(action => action.Type),
            actionRegistry);
        var author = await permissionCache.GetUserPermissions(userId, workspaceKey);

        if (author is null)
        {
            return "The rule author is not a member of this workspace.";
        }

        var authorPermissions = author.Role == WorkspaceRole.Owner
            ? NetptunePermissions.All
            : author.Permissions;

        if (!AutomationPermissionPolicy.HasRequiredPermissions(authorPermissions, requiredPermissions))
        {
            return "You cannot configure actions that require permissions you do not have.";
        }

        var principal = await unitOfWork.ServiceAccounts.GetAutomationPrincipal(
            request.ExecutionUserId!,
            workspaceId,
            cancellationToken);

        if (principal is null)
        {
            return "The selected automation service account does not belong to this workspace.";
        }

        if (!principal.IsEnabled)
        {
            return "The selected automation service account is disabled.";
        }

        if (!AutomationPermissionPolicy.HasRequiredPermissions(principal.Permissions, requiredPermissions))
        {
            return "The selected automation service account does not have every permission required by these actions.";
        }

        return null;
    }
}
