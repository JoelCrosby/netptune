using Mediator;
using Microsoft.AspNetCore.Authorization;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Services.Workspaces.Commands;
using Netptune.Services.Workspaces.Queries;

namespace Netptune.App.Endpoints;

public static class WorkspacesEndpoints
{
    public static RouteGroupBuilder MapWorkspacesEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("workspaces")
            .RequireAuthorization();

        group.MapGet("/", HandleGetWorkspaces);
        group.MapGet("/{key}", HandleGetWorkspace).RequireAuthorization(NetptunePermissions.Workspace.Read);
        group.MapPut("/", HandlePut).RequireAuthorization(NetptunePermissions.Workspace.Update);
        group.MapPost("/", HandlePost);
        group.MapDelete("/{key}", HandleDelete).RequireAuthorization(NetptunePermissions.Workspace.Delete);
        group.MapDelete("/permanent/{key}", HandleDeletePermanent).RequireAuthorization(NetptunePermissions.Workspace.DeletePermanent);
        group.MapGet("/all", HandleGetAllWorkspaces);
        group.MapGet("/is-unique/{slug}", HandleIsSlugUnique);

        return builder;
    }

    public static async Task<IResult> HandleGetWorkspaces(IMediator mediator)
    {
        var result = await mediator.Send(new GetUserWorkspacesQuery());

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetWorkspace(
        IMediator mediator,
        IAuthorizationService authorizationService,
        HttpContext context,
        string key)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(
            context.User, key, NetptunePermissions.Workspace.Read);

        if (!authorizationResult.Succeeded) return Results.Forbid();

        var result = await mediator.Send(new GetWorkspaceQuery(key));

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePut(
        IMediator mediator,
        IAuthorizationService authorizationService,
        HttpContext context,
        UpdateWorkspaceRequest request)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(
            context.User, request.Slug!, NetptunePermissions.Workspace.Update);

        if (!authorizationResult.Succeeded) return Results.Forbid();

        var result = await mediator.Send(new UpdateWorkspaceCommand(request));

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePost(IMediator mediator, AddWorkspaceRequest request)
    {
        var result = await mediator.Send(new CreateWorkspaceCommand(request));

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(
        IMediator mediator,
        IAuthorizationService authorizationService,
        HttpContext context,
        string key)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(
            context.User, key, NetptunePermissions.Workspace.Delete);

        if (!authorizationResult.Succeeded) return Results.Forbid();

        var result = await mediator.Send(new DeleteWorkspaceCommand(key));

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDeletePermanent(
        IMediator mediator,
        IAuthorizationService authorizationService,
        HttpContext context,
        string key)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(
            context.User, key, NetptunePermissions.Workspace.DeletePermanent);

        if (!authorizationResult.Succeeded) return Results.Forbid();

        var result = await mediator.Send(new DeleteWorkspacePermanentCommand(key));

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetAllWorkspaces(IMediator mediator)
    {
        var result = await mediator.Send(new GetAllWorkspacesQuery());

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleIsSlugUnique(IMediator mediator, string slug)
    {
        var result = await mediator.Send(new IsWorkspaceSlugUniqueQuery(slug));

        return Results.Ok(result);
    }
}
