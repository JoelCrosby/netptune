using Microsoft.AspNetCore.Authorization;

namespace Netptune.Services.Authorization.Requirements;

public class WorkspacePermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public WorkspacePermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
