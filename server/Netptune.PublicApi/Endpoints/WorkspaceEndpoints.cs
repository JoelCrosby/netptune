using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.Services;
using Netptune.Core.ViewModels.Workspace;
using Netptune.Handlers.Workspaces.Queries;

namespace Netptune.PublicApi.Endpoints;

public static class WorkspaceEndpoints
{
    public static RouteGroupBuilder MapWorkspaceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/workspace", GetWorkspace)
            .WithSummary("Get the credential's workspace")
            .WithDescription(
                "Returns the workspace the credential is restricted to. Every other endpoint reads and writes "
                + "within this workspace.")
            .RequireAuthorization(NetptunePermissions.Workspace.Read);

        return group;
    }

    private static async Task<Results<Ok<WorkspaceViewModel>, NotFound>> GetWorkspace(
        IMediator mediator,
        IIdentityService identity,
        CancellationToken cancellationToken)
    {
        var workspaceKey = identity.GetWorkspaceKey();
        var result = await mediator.Send(new GetWorkspaceQuery(workspaceKey), cancellationToken);

        if (result is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(result.ToViewModel());
    }
}
