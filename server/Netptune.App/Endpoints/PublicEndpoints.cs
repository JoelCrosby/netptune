using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class PublicEndpoints
{
    public static RouteGroupBuilder MapPublicEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("public");

        group.MapGet("/workspaces/{workspaceKey}", HandleGetWorkspace);

        return group;
    }

    public static async Task<IResult> HandleGetWorkspace(
        IPublicWorkspaceService publicWorkspaceService,
        string workspaceKey)
    {
        var result = await publicWorkspaceService.GetPublicWorkspace(workspaceKey);

        if (result is null) return Results.NotFound();

        return Results.Ok(result.ToViewModel());
    }
}
