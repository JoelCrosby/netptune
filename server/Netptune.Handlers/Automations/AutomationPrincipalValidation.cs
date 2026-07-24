using Netptune.Core.Authorization;
using Netptune.Core.Services.Automations;

namespace Netptune.Handlers.Automations;

internal static class AutomationPrincipalValidation
{
    public static async Task<string?> Validate(
        AutomationValidationContext context,
        CancellationToken cancellationToken)
    {
        var requiredPermissions = AutomationPermissionPolicy.GetRequiredPermissions(
            context.Request.Actions,
            context.ActionRegistry);
        var author = await context.PermissionCache.GetUserPermissions(context.UserId, context.WorkspaceKey);

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

        var principal = await context.UnitOfWork.ServiceAccounts.GetAutomationPrincipal(
            context.Request.ExecutionUserId!,
            context.WorkspaceId,
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
