using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class ProjectsEndpoints
{
    public static RouteGroupBuilder MapProjectsEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("projects");

        group.MapGet("/", HandleGetProjects).RequireAuthorization(NetptunePermissions.Projects.Read);
        group.MapGet("/{key}", HandleGetProject).RequireAuthorization(NetptunePermissions.Projects.Read);
        group.MapPut("/", HandlePut).RequireAuthorization(NetptunePermissions.Projects.Update);
        group.MapPost("/", HandlePost).RequireAuthorization(NetptunePermissions.Projects.Create);
        group.MapDelete("/{id}", HandleDelete).RequireAuthorization(NetptunePermissions.Projects.Delete);

        return group;
    }

    public static async Task<IResult> HandleGetProjects(
        IProjectService projectService)
    {
        var result = await projectService.GetProjects();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetProject(
        IProjectService projectService,
        string key)
    {
        var result = await projectService.GetProject(key);

        if (result is null) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePut(
        IProjectService projectService,
        UpdateProjectRequest request)
    {
        var result = await projectService.Update(request);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePost(
        IProjectService projectService,
        AddProjectRequest request)
    {
        var result = await projectService.Create(request);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(
        IProjectService projectService,
        int id)
    {
        var result = await projectService.Delete(id);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }
}
