using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class WorkspacesEndpoints
{
    public static RouteGroupBuilder Map(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("workspaces")
            .RequireAuthorization();

        group.MapGet("/", HandleGetWorkspaces);
        group.MapGet("/{key}", HandleGetWorkspace);
        group.MapPut("/", HandlePut);
        group.MapPost("/", HandlePost);
        group.MapDelete("/{key}", HandleDelete);
        group.MapDelete("/permanent/{key}", HandleDeletePermanent);
        group.MapGet("/all", HandleGetAllWorkspaces);
        group.MapGet("/is-unique/{slug}", HandleIsSlugUnique);

        return group;
    }

    public static async Task<IResult> HandleGetWorkspaces(
        IWorkspaceService workspaceService)
    {
        var result = await workspaceService.GetUserWorkspaces();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetWorkspace(
        IWorkspaceService workspaceService,
        IAuthorizationService authorizationService,
        HttpContext context,
        string key)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(
            context.User, key, NetptunePolicies.Workspace);

        if (!authorizationResult.Succeeded) return Results.Forbid();

        var result = await workspaceService.GetWorkspace(key);

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePut(
        IWorkspaceService workspaceService,
        IAuthorizationService authorizationService,
        HttpContext context,
        UpdateWorkspaceRequest request)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(
            context.User, request.Slug!, NetptunePolicies.Workspace);

        if (!authorizationResult.Succeeded) return Results.Forbid();

        var result = await workspaceService.Update(request);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePost(
        IWorkspaceService workspaceService,
        AddWorkspaceRequest request)
    {
        var result = await workspaceService.Create(request);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(
        IWorkspaceService workspaceService,
        IAuthorizationService authorizationService,
        HttpContext context,
        string key)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(
            context.User, key, NetptunePolicies.Workspace);

        if (!authorizationResult.Succeeded) return Results.Forbid();

        var result = await workspaceService.Delete(key);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDeletePermanent(
        IWorkspaceService workspaceService,
        IAuthorizationService authorizationService,
        HttpContext context,
        string key)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(
            context.User, key, NetptunePolicies.Workspace);

        if (!authorizationResult.Succeeded) return Results.Forbid();

        var result = await workspaceService.DeletePermanent(key);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetAllWorkspaces(
        IWorkspaceService workspaceService)
    {
        var result = await workspaceService.GetAll();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleIsSlugUnique(
        IWorkspaceService workspaceService,
        string slug)
    {
        var result = await workspaceService.IsSlugUnique(slug);

        return Results.Ok(result);
    }
}
