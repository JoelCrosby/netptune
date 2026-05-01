using Mediator;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Services.Projects.Commands;
using Netptune.Services.Projects.Queries;

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

    public static async Task<IResult> HandleGetProjects(IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProjectsQuery(), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetProject(IMediator mediator, string key,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProjectQuery(key), cancellationToken);

        if (result is null) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePut(IMediator mediator, UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateProjectCommand(request), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePost(IMediator mediator, AddProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateProjectCommand(request), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(IMediator mediator, int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteProjectCommand(id), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }
}
