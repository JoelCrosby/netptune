using Netptune.Core.Requests;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class PublicEndpoints
{
    public static RouteGroupBuilder MapPublicEndpoints(this RouteGroupBuilder builder)
    {
        // Reached before sign-in, so an invite link can show the workspace it belongs to.
        var group = builder.MapGroup("public")
            .AllowAnonymous();

        group.MapGet("/workspaces/{workspaceKey}", HandleGetWorkspace);
        group.MapGet("/workspaces/{workspaceKey}/members", HandleGetWorkspaceMembers);

        return group;
    }

    public static async Task<IResult> HandleGetWorkspace(
        IPublicWorkspaceService publicWorkspaceService,
        string workspaceKey,
        CancellationToken cancellationToken)
    {
        var workspace = await publicWorkspaceService.GetPublicWorkspace(workspaceKey, cancellationToken);

        if (workspace is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(workspace);
    }

    public static async Task<IResult> HandleGetWorkspaceMembers(
        IPublicWorkspaceService publicWorkspaceService,
        string workspaceKey,
        [AsParameters] AssigneeFilter filter,
        CancellationToken cancellationToken)
    {
        var members = await publicWorkspaceService.GetPublicWorkspaceMembers(workspaceKey, filter, cancellationToken);

        if (members is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(members);
    }
}
